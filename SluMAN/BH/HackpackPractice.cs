using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Bentley's Hackpack Practice Mode: start a job (each minigame's challenges run in one) or
    /// load any level from the game's level table, slow motion
    /// and cutscene skipping, with the camera (free camera and camera paths), the tunables and
    /// the memory tools on tabs. The game runs on Sly 4's engine, so this drives the sly4 class
    /// and Sly 4's tools. The controls are built in code, in the other games' two-column layout.
    /// </summary>
    public class HackpackPractice : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        private const string CameraTab = "Camera";
        private const string TunablesTab = "Tunables";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";

        // The status refreshes every second; on a PS3 every read is a network round trip.
        private const int RefreshIntervalMs = 1000;
        // How often a queued job start or load is checked for having run.
        private const int GameCallCheckMs = 100;

        // How long a queued job start or load may wait for the game's next frame before it's dropped.
        private static readonly TimeSpan GameCallTimeout = TimeSpan.FromSeconds(3);

        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly ComboBox jobComboBox = new ComboBox();
        private readonly ComboBox levelComboBox = new ComboBox();
        private readonly CheckBox slowMotionCheckBox = new CheckBox();
        private readonly CheckBox skipCutscenesCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label episodeValueLabel = new Label();
        private readonly Label jobValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();
        private readonly Timer gameCallTimer = new Timer();

        /// <summary>The free camera (Camera tab).</summary>
        public Sly4FreeMove FreeMove;

        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        public Sly4CameraApi CameraApi;

        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        // Whether the last refresh saw a load, for the Practice Mode popup after it.
        private bool wasLoading;
        private DateTime gameCallDeadline;
        private string gameCallDoneMessage;

        public HackpackPractice(sly4 game)
        {
            this.game = game;
            Name = "HackpackPractice";
            Text = "SluMAN :: Bentley's Hackpack (Practice Mode)";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft Sans Serif", 8.25F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            BuildMenu();
            BuildControls();
            ClientSize = new Size(660, 220);
            statusLine = new StatusLine(this, true);

            FillJobs(false);
            FillLevels(false);

            // Before the toggles below read the game, so they show any kept tunables.
            ApplyKeptTunables();
            try
            {
                slowMotionCheckBox.Checked = game.IsSlowMotionOn();
                skipCutscenesCheckBox.Checked = game.IsSkippingCutscenes();
            }
            catch
            {
                // Left unchecked; ticking them writes the values anyway.
            }
            slowMotionCheckBox.CheckedChanged += (sender, e) => Toggle(slowMotionCheckBox, () => game.SetSlowMotion(slowMotionCheckBox.Checked));
            skipCutscenesCheckBox.CheckedChanged += (sender, e) => Toggle(skipCutscenesCheckBox, () => game.SetSkipCutscenes(skipCutscenesCheckBox.Checked));

            // The controller feeds the combos and the input display.
            game.SetupInputDisplayMemorySubs();
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Hackpack", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();
            RefreshStatus();
            gameCallTimer.Interval = GameCallCheckMs;
            gameCallTimer.Tick += (sender, e) => CheckGameCall();

            FormClosing += HackpackPractice_FormClosing;

            FreeMove = new Sly4FreeMove(game);
            FreeMove.Stopped += (message) =>
            {
                if (!IsDisposed)
                {
                    statusLine.Error($"Free camera stopped: {message}");
                }
            };
            CameraApi = new Sly4CameraApi(game, FreeMove);
            CameraApi.StartFromConfig();

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            practiceTabs.AddTool(CameraTab, () => new Sly4CameraForm(game, FreeMove, CameraApi));
            practiceTabs.AddTool(TunablesTab, () => new Sly4TunablesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("hackpack_visible_tabs", CameraTab);
        }

        private void BuildMenu()
        {
            ToolStripMenuItem tools = new ToolStripMenuItem("Tools");
            tools.DropDownItems.Add("Switch Game/Mode", null, (sender, e) => session.SwitchGameOrMode());
            tools.DropDownItems.Add("Input Display", null, (sender, e) => session.ShowInputDisplay());
            tools.DropDownItems.Add("Controller Combos...", null, (sender, e) =>
            {
                ConfigureCombos configureCombos = new ConfigureCombos();
                configureCombos.ShowDialog(this);
            });
            tools.DropDownItems.Add("Memory Utilities...", null, (sender, e) => practiceTabs.Show(MemoryTab));
            tools.DropDownItems.Add("Open User Data Folder", null, (sender, e) => UserData.OpenInExplorer());
            tools.DropDownItems.Add(new ToolStripSeparator());
            tools.DropDownItems.Add("Power Off (PS3)", null, (sender, e) => session.PowerOffPS3());
            tools.DropDownItems.Add("Reboot (PS3)", null, (sender, e) => session.RebootPS3());
            menuStrip.Items.Add(tools);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void BuildControls()
        {
            // Left column
            GroupBox jobGroupBox = AddGroup("Job", 12, 32, 80);
            jobComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            jobComboBox.Location = new Point(10, 20);
            jobComboBox.Size = new Size(292, 21);
            // The game reads its jobs while it boots, so they're read again when still empty.
            jobComboBox.DropDown += (sender, e) =>
            {
                if (jobComboBox.Items.Count == 0)
                {
                    FillJobs(true);
                }
            };
            jobGroupBox.Controls.Add(jobComboBox);
            AddButton(jobGroupBox, "Start Job", 10, 48, 292, (sender, e) => StartSelectedJob());

            GroupBox levelGroupBox = AddGroup("Level", 12, 118, 80);
            levelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            levelComboBox.Location = new Point(10, 20);
            levelComboBox.Size = new Size(292, 21);
            // The game reads its level list while it boots, so it's read again when still empty.
            levelComboBox.DropDown += (sender, e) =>
            {
                if (levelComboBox.Items.Count == 0)
                {
                    FillLevels(true);
                }
            };
            levelGroupBox.Controls.Add(levelComboBox);
            AddButton(levelGroupBox, "Load", 10, 48, 143, (sender, e) => LoadSelectedLevel());
            AddButton(levelGroupBox, "Reload Level", 159, 48, 143, (sender, e) => ReloadLevel());

            // Right column
            GroupBox statusGroupBox = AddGroup("Game", 336, 32, 90);
            AddLabel(statusGroupBox, "Level:", 10, 20);
            AddValueLabel(statusGroupBox, levelValueLabel, 70, 20);
            AddLabel(statusGroupBox, "Episode:", 10, 42);
            AddValueLabel(statusGroupBox, episodeValueLabel, 70, 42);
            AddLabel(statusGroupBox, "Job:", 10, 64);
            AddValueLabel(statusGroupBox, jobValueLabel, 70, 64);

            GroupBox togglesGroupBox = AddGroup("Toggles", 336, 128, 70);
            AddCheckBox(togglesGroupBox, slowMotionCheckBox, "Slow Motion", 10, 20);
            AddCheckBox(togglesGroupBox, skipCutscenesCheckBox, "Skip Cutscenes", 10, 43);
            AddCheckBox(togglesGroupBox, alwaysOnTopCheckBox, "Always On Top", 160, 20);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(jobComboBox,
                "Every job in the game's job list. A minigame's challenges only work when it's\n" +
                "started as a job; loading its level on its own doesn't set them up.");
            tip.SetToolTip(levelComboBox,
                "Every level in the game's level table. Names come from data/hackpack_levels.txt\n" +
                "(level | English name); the others show as their hash and the path the game keeps.");
            FormClosed += (sender, e) => tip.Dispose();
        }

        private GroupBox AddGroup(string text, int x, int y, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(312, height);
            Controls.Add(group);
            return group;
        }

        private static void AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            parent.Controls.Add(button);
        }

        private static void AddLabel(Control parent, string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            parent.Controls.Add(label);
        }

        private static void AddValueLabel(Control parent, Label label, int x, int y)
        {
            label.Text = "-";
            label.Location = new Point(x, y);
            label.Size = new Size(232, 13);
            parent.Controls.Add(label);
        }

        private static void AddCheckBox(Control parent, CheckBox checkBox, string text, int x, int y)
        {
            checkBox.Text = text;
            checkBox.AutoSize = true;
            checkBox.Location = new Point(x, y);
            parent.Controls.Add(checkBox);
        }

        private void Toggle(CheckBox toggle, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change {toggle.Text}: {ex.Message}");
            }
        }

        /// <summary>Writes the tunables kept in the Tunables tab. A game reset puts them back to their defaults.</summary>
        private void ApplyKeptTunables()
        {
            if (game.tunables.KeptCount == 0)
            {
                return;
            }
            try
            {
                int applied = game.tunables.ApplyKept();
                statusLine.Info($"Applied {applied} kept tunables.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't apply the kept tunables: {ex.Message}");
            }
        }

        /// <summary>Lists the game's jobs, keeping the selection.</summary>
        private void FillJobs(bool reportError)
        {
            sly4.JobEntry selected = jobComboBox.SelectedItem as sly4.JobEntry;
            List<sly4.JobEntry> jobs;
            try
            {
                jobs = game.GetJobs();
            }
            catch (Exception ex)
            {
                if (reportError)
                {
                    statusLine.Error($"Couldn't read the job list: {ex.Message}");
                }
                return;
            }

            jobComboBox.BeginUpdate();
            jobComboBox.Items.Clear();
            foreach (sly4.JobEntry job in jobs)
            {
                jobComboBox.Items.Add(job);
                if (selected != null && job.index == selected.index)
                {
                    jobComboBox.SelectedItem = job;
                }
            }
            jobComboBox.EndUpdate();
        }

        private void StartSelectedJob()
        {
            sly4.JobEntry job = jobComboBox.SelectedItem as sly4.JobEntry;
            if (job == null)
            {
                statusLine.Error("Pick a job first.");
                return;
            }
            try
            {
                game.StartJob(job.index, false, false);
                TrackGameCall($"Starting {job.Title}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't start the job: {ex.Message}");
            }
        }

        /// <summary>Lists the game's levels, keeping the selection.</summary>
        private void FillLevels(bool reportError)
        {
            sly4.LevelEntry selected = levelComboBox.SelectedItem as sly4.LevelEntry;
            List<sly4.LevelEntry> levels;
            try
            {
                levels = game.ReadHackpackLevels();
            }
            catch (Exception ex)
            {
                if (reportError)
                {
                    statusLine.Error($"Couldn't read the level list: {ex.Message}");
                }
                return;
            }

            levelComboBox.BeginUpdate();
            levelComboBox.Items.Clear();
            foreach (sly4.LevelEntry level in levels)
            {
                levelComboBox.Items.Add(level);
                if (selected != null && level.hash == selected.hash)
                {
                    levelComboBox.SelectedItem = level;
                }
            }
            levelComboBox.EndUpdate();
        }

        private void LoadSelectedLevel()
        {
            sly4.LevelEntry level = levelComboBox.SelectedItem as sly4.LevelEntry;
            if (level == null)
            {
                statusLine.Error("Pick a level first.");
                return;
            }
            QueueLoad(level.hash, $"Loading {level.name}.");
        }

        private void ReloadLevel()
        {
            uint levelHash;
            try
            {
                levelHash = game.GetLevelHash();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the level: {ex.Message}");
                return;
            }
            if (levelHash == 0)
            {
                statusLine.Error("No level is loaded yet.");
                return;
            }
            QueueLoad(levelHash, $"Reloading {sly4.GetLevelName(levelHash)}.");
        }

        /// <summary>Queues the load through the code hook and reports once the game has run it.</summary>
        private void QueueLoad(uint levelHash, string doneMessage)
        {
            try
            {
                game.LoadLevel(levelHash, null, null);
                TrackGameCall(doneMessage);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the level: {ex.Message}");
            }
        }

        /// <summary>Waits for a call queued through the code hook, reporting the message once it has run.</summary>
        private void TrackGameCall(string doneMessage)
        {
            gameCallDeadline = DateTime.Now + GameCallTimeout;
            gameCallDoneMessage = doneMessage;
            gameCallTimer.Start();
        }

        private void CheckGameCall()
        {
            try
            {
                if (!game.IsGameCallPending())
                {
                    gameCallTimer.Stop();
                    statusLine.Info(gameCallDoneMessage);
                }
                else if (DateTime.Now > gameCallDeadline)
                {
                    gameCallTimer.Stop();
                    game.CancelGameCall();
                    statusLine.Error("The game didn't run the call. On RPCS3, set the PPU decoder to Interpreter (static).");
                }
            }
            catch
            {
                // Checked again on the next tick.
            }
        }

        private void RefreshStatus()
        {
            if (IsDisposed)
            {
                return;
            }
            try
            {
                uint levelHash = game.GetLevelHash();
                bool loading = game.IsLoading();
                string level = levelHash == 0 ? "-" : sly4.GetLevelName(levelHash);
                levelValueLabel.Text = loading ? level + " (loading)" : level;
                string episode = game.GetEpisodeName();
                episodeValueLabel.Text = episode == null ? "-" : episode;
                string job = game.GetJobName();
                jobValueLabel.Text = job == null ? "-" : job;

                ShowPopUpAfterLoad(loading);
            }
            catch
            {
                levelValueLabel.Text = "-";
                episodeValueLabel.Text = "-";
                jobValueLabel.Text = "-";
            }
        }

        /// <summary>
        /// Shows webMAN's "SluMAN (Practice Mode)" popup on the PS3 when a load finishes, like the
        /// other games, so a run's video shows Practice Mode was on. Ratchetron only.
        /// </summary>
        private void ShowPopUpAfterLoad(bool loading)
        {
            if (!(game.api is Ratchetron))
            {
                return;
            }
            if (wasLoading && !loading)
            {
                // A web request: off the UI thread.
                string ip = game.api.GetIP();
                System.Threading.ThreadPool.QueueUserWorkItem(_ => WebMAN.DisplayVersionPopUp(ip));
            }
            wasLoading = loading;
        }

        private void session_Reconnected()
        {
            // A game reset puts the tunables back to their defaults.
            if (slowMotionCheckBox.Checked)
            {
                Toggle(slowMotionCheckBox, () => game.SetSlowMotion(true));
            }
            if (skipCutscenesCheckBox.Checked)
            {
                Toggle(skipCutscenesCheckBox, () => game.SetSkipCutscenes(true));
            }
            ApplyKeptTunables();
            practiceTabs.RefreshShownTool();
        }

        private void HackpackPractice_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The tools' own cleanup (timers) runs when they close.
            practiceTabs.CloseAll();
            CameraApi.Stop();
            FreeMove.Stop();
            refreshTimer.Stop();
            gameCallTimer.Stop();

            // Leave the game unmodified. This runs before the session disconnects.
            if (slowMotionCheckBox.Checked)
            {
                try { game.SetSlowMotion(false); } catch { }
            }
            if (skipCutscenesCheckBox.Checked)
            {
                try { game.SetSkipCutscenes(false); } catch { }
            }

            // Put the game's code back. Does nothing if no load was used.
            try { game.RemoveCodeHook(); } catch { }
        }
    }
}
