using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace racman
{
    /// <summary>
    /// The connection to the game for one game form: what happens when the game closes and starts
    /// again, when the form closes, and the settings and menus every game form has. Each game form
    /// creates one in its constructor and keeps only its game-specific work.
    /// </summary>
    public class GameSession
    {
        private const string PrefersAutosplitterKey = "prefersAutosplitter";
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        // How long to wait after the game is back before subscribing, so it has finished starting.
        private const int SettleDelayMs = 2000;

        // Shared by every session, so the input display (and an OBS capture of it) stays open when
        // SluMAN switches to another game.
        private static Form inputDisplay;

        private readonly Form form;
        private readonly IGame game;
        private readonly string titleId;
        private readonly string gameName;
        private readonly bool speedrunMode;

        private CheckBox autosplitterCheckBox;
        private AutosplitterHelper autosplitter;

        // Windows that use this game's subscriptions; closed when the game closes or the form does.
        private readonly List<Form> gameWindows = new List<Form>();

        // Bumped on every game close, game start and form close. A reconnect started before the
        // latest bump is out of date and stops.
        private int generation = 0;
        private volatile bool closed = false;

        /// <summary>
        /// Raised on the UI thread when the game is running again, after the PID is updated and the
        /// input display subscriptions are back. Forms set up their own subscriptions here.
        /// </summary>
        public event Action Reconnected;

        /// <param name="gameName">For messages, such as "Sly 2".</param>
        public GameSession(Form form, IGame game, string titleId, string gameName, bool speedrunMode)
        {
            this.form = form;
            this.game = game;
            this.titleId = titleId;
            this.gameName = gameName;
            this.speedrunMode = speedrunMode;

            GameReconnect.WatchRpcs3(form, game.api, titleId, speedrunMode);

            if (game.api is Ratchetron r)
            {
                r.setDisconnectCallback(OnGameClosed);
                r.setReconnectCallback(OnGameStarted);
            }

            form.FormClosing += form_FormClosing;
            form.FormClosed += form_FormClosed;
        }

        private string ModeText
        {
            get { return speedrunMode ? "Speedrun Mode" : "Practice Mode"; }
        }

        /// <summary>
        /// Loads the saved always-on-top setting into <paramref name="checkBox"/> and keeps the form
        /// and the setting in step with it.
        /// </summary>
        public void BindAlwaysOnTop(CheckBox checkBox)
        {
            checkBox.Checked = ReadPreference(PrefersAlwaysOnTopKey);
            form.TopMost = checkBox.Checked;
            checkBox.CheckedChanged += (sender, e) =>
            {
                func.ChangeFileLines("config.txt", checkBox.Checked ? "true" : "false", PrefersAlwaysOnTopKey);
                form.TopMost = checkBox.Checked;
            };
        }

        /// <summary>
        /// Loads the saved autosplitter setting into <paramref name="checkBox"/>, starts the
        /// autosplitter if it's on, and starts or stops it when the box changes. The session restarts
        /// it after the game restarts.
        /// </summary>
        public void BindAutosplitter(CheckBox checkBox)
        {
            autosplitterCheckBox = checkBox;
            checkBox.Checked = ReadPreference(PrefersAutosplitterKey);
            checkBox.CheckedChanged += (sender, e) =>
            {
                func.ChangeFileLines("config.txt", checkBox.Checked ? "true" : "false", PrefersAutosplitterKey);
                StopAutosplitter();
                if (checkBox.Checked)
                {
                    StartAutosplitter();
                }
            };

            if (checkBox.Checked)
            {
                StartAutosplitter();
            }
        }

        /// <summary>
        /// Closes <paramref name="window"/> when the game closes or the form does. For windows that
        /// hold subscriptions to the game.
        /// </summary>
        public void AddGameWindow(Form window)
        {
            gameWindows.RemoveAll(w => w.IsDisposed);
            if (!gameWindows.Contains(window))
            {
                gameWindows.Add(window);
            }
        }

        public void ShowInputDisplay()
        {
            if (inputDisplay == null || inputDisplay.IsDisposed)
            {
                inputDisplay = new InputDisplay();
                inputDisplay.Show();
            }
            else
            {
                inputDisplay.Focus();
            }
        }

        /// <summary>Closes the form and goes back to the Attach window.</summary>
        public void SwitchGameOrMode()
        {
            // Show first: the form exits the app when it closes with the Attach window hidden.
            Program.AttachPS3Form.Show();
            form.Close();
        }

        public void PowerOffPS3()
        {
            ConfirmAndSend("Do you want to turn off your PS3?", "Power Off PS3", WebMAN.TurnOffPS3);
        }

        public void RebootPS3()
        {
            ConfirmAndSend("Do you want to reboot your PS3?", "Reboot PS3", WebMAN.RebootPS3);
        }

        private void ConfirmAndSend(string question, string title, Action<string> send)
        {
            if (!(game.api is Ratchetron))
            {
                return;
            }

            if (MessageBox.Show(question, title, MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            string ip = game.api.GetIP();
            // Disconnect (in FormClosing) before the PS3 goes away.
            SwitchGameOrMode();
            send(ip);
        }

        private void StartAutosplitter()
        {
            try
            {
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(game);
            }
            catch (Exception ex)
            {
                func.Status($"Couldn't start the autosplitter: {ex.Message}", true);
                Console.WriteLine(ex);
            }
        }

        private void StopAutosplitter()
        {
            if (autosplitter == null)
            {
                return;
            }

            try
            {
                if (autosplitter.IsRunning)
                {
                    autosplitter.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't stop the autosplitter: {ex.Message}");
            }
            autosplitter = null;
        }

        private void CloseGameWindows()
        {
            foreach (Form window in gameWindows.ToArray())
            {
                if (!window.IsDisposed)
                {
                    window.Close();
                }
            }
            gameWindows.Clear();
        }

        /// <summary>Ratchetron callback, on its UDP thread: the game closed.</summary>
        private void OnGameClosed()
        {
            Interlocked.Increment(ref generation);
            RunOnForm(() =>
            {
                if (closed)
                {
                    return;
                }

                StopAutosplitter();
                try
                {
                    ((Ratchetron)game.api).ReleaseAllSubs();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{gameName}: couldn't release subscriptions: {ex.Message}");
                }
                CloseGameWindows();
            });
        }

        /// <summary>
        /// Ratchetron callback, on its UDP thread: a game started. Waiting for it takes seconds, so
        /// it runs on its own thread and the UDP thread goes back to receiving.
        /// </summary>
        private void OnGameStarted()
        {
            int startedGeneration = Interlocked.Increment(ref generation);
            Thread thread = new Thread(() => Reconnect(startedGeneration));
            thread.IsBackground = true;
            thread.Name = "Reconnect " + gameName;
            thread.Start();
        }

        private bool IsOutOfDate(int startedGeneration)
        {
            return closed || startedGeneration != Volatile.Read(ref generation);
        }

        private void Reconnect(int startedGeneration)
        {
            GameReconnect.Result result = GameReconnect.WaitForGame(game.api, titleId, gameName, out int pid, out string runningTitleId,
                () => IsOutOfDate(startedGeneration));
            if (IsOutOfDate(startedGeneration) || GameReconnect.HandleOtherResult(result, form, gameName, runningTitleId, speedrunMode))
            {
                return;
            }

            Thread.Sleep(SettleDelayMs);

            RunOnForm(() =>
            {
                if (IsOutOfDate(startedGeneration))
                {
                    return;
                }

                AttachPS3Form.pid = pid;
                game.pid = pid;

                game.SetupInputDisplayMemorySubs();

                Action handler = Reconnected;
                if (handler != null)
                {
                    handler();
                }

                if (autosplitterCheckBox != null && autosplitterCheckBox.Checked)
                {
                    StopAutosplitter();
                    StartAutosplitter();
                }

                game.api.Notify($"SluMAN {func.VersionText} ({ModeText})");
                func.Status($"Reconnected to {gameName}.");
            });
        }

        /// <summary>Runs <paramref name="action"/> on the form's UI thread, unless the form is gone.</summary>
        private void RunOnForm(Action action)
        {
            if (form.IsDisposed || !form.IsHandleCreated)
            {
                return;
            }

            try
            {
                form.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        func.Status($"{gameName}: {ex.Message}", true);
                        Console.WriteLine(ex);
                    }
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        private void form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // A modal form can raise FormClosing twice; only the first one cleans up.
            if (e.Cancel || closed)
            {
                return;
            }

            closed = true;
            Interlocked.Increment(ref generation);

            if (game.api is Ratchetron r)
            {
                r.setDisconnectCallback(null);
                r.setReconnectCallback(null);
            }

            StopAutosplitter();
            CloseGameWindows();
            game.InputsTimer.Stop();

            // Keep the input display open when switching to another game.
            if (!AttachPS3Form.switchPending && inputDisplay != null && !inputDisplay.IsDisposed)
            {
                inputDisplay.Close();
            }

            // Ratchetron only tells one client when the game closes or starts, so a connection left
            // open here would take reconnects away from the next game form.
            try
            {
                game.api.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }

        private void form_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!Program.AttachPS3Form.Visible && !AttachPS3Form.switchPending)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
            }
        }

        private static bool ReadPreference(string key)
        {
            return bool.TryParse(func.GetConfigData("config.txt", key), out bool enabled) && enabled;
        }
    }
}
