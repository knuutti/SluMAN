using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Bentley's Hackpack Practice Mode: start or restart a job (each minigame stage runs in one,
    /// with its challenges) or go back to the hub, slow motion and cutscene skipping, with the
    /// jobs and their goals, the camera (free camera and camera paths), the tunables and the
    /// memory tools on tabs. The game runs on Sly 4's engine, so this drives the sly4 class and Sly 4's
    /// tools. The controls are built in code, in the other games' two-column layout.
    /// </summary>
    public class HackpackPractice : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        private const string JobsTab = "Jobs";
        private const string CameraTab = "Camera";
        private const string TunablesTab = "Tunables";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";

        // The hub's level, "FrontEnd".
        private const uint HubLevelHash = 0xE4927E12;

        // The status refreshes every second; on a PS3 every read is a network round trip.
        private const int RefreshIntervalMs = 1000;

        private readonly ComboBox jobComboBox = new ComboBox();
        private readonly CheckBox slowMotionCheckBox = new CheckBox();
        private readonly CheckBox skipCutscenesCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label jobValueLabel = new Label();
        private readonly Label goalValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        /// <summary>The free camera (Camera tab).</summary>
        public Sly4FreeMove FreeMove;

        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        public Sly4CameraApi CameraApi;

        private PracticeLayout layout;
        private ToolStripMenuItem toolsMenu;
        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        // Whether the last refresh saw a load, for the Practice Mode popup after it.
        private bool wasLoading;
        // Job starts, goal warps and the hub load (shared with Sly 4 and the Jobs tab).
        private Sly4JobQueue jobQueue;

        public HackpackPractice(sly4 game)
        {
            this.game = game;
            PracticeLayout.SetUpWindow(this, "HackpackPractice", "SluMAN :: Bentley's Hackpack (Practice Mode)");
            layout = new PracticeLayout(this);
            BuildControls();
            statusLine = new StatusLine(this, true);
            jobQueue = new Sly4JobQueue(game, statusLine);

            FillJobs(false);

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

            // Before the session's own handler, which disconnects: this one still writes to the game.
            FormClosing += HackpackPractice_FormClosing;

            session = new GameSession(this, game, gameNameId, "Hackpack", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;
            session.FillPracticeToolsMenu(toolsMenu, () => practiceTabs.Show(MemoryTab));

            // The Jobs tab colours the running job's goals as they change.
            game.StartGoalWatcher();

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();
            RefreshStatus();

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
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools;
            // the Hackpack has no player.
            practiceTabs.AddTool(CameraTab, () => new CameraForm(game.GetCameraTabLayout(FreeMove, CameraApi)));
            practiceTabs.AddTool(JobsTab, () => new Sly4JobsForm(game, jobQueue));
            practiceTabs.AddTool(TunablesTab, () => new Sly4TunablesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("hackpack_visible_tabs", CameraTab, JobsTab);
        }

        /// <summary>The Practice tab, in the slots every game shares (PracticeLayout).</summary>
        private void BuildControls()
        {
            toolsMenu = layout.AddMenu();

            // Left column
            GroupBox levelGroupBox = layout.AddLeft("Level", 1);
            PracticeLayout.AddButtonRow(levelGroupBox, PracticeLayout.Row(0) - 1, new Button[]
            {
                PracticeLayout.MakeButton("Load Hub", (sender, e) => LoadHub()),
            });
            // The game reads its jobs while it boots, so they're read again when still empty.
            jobComboBox.DropDown += (sender, e) =>
            {
                if (jobComboBox.Items.Count == 0)
                {
                    FillJobs(true);
                }
            };
            layout.AddPickerGroup("Job", jobComboBox, null, (sender, e) => StartSelectedJob());
            layout.AddReloadGroup(new Button[]
            {
                PracticeLayout.MakeButton("Restart Job", (sender, e) => jobQueue.RestartRunningJob()),
                layout.PlaceholderButton("Skip Cinematic"),
            }, null);

            // Right column
            layout.AddStatusGroup(new string[] { "Level", "Job", "Goal" },
                new Label[] { levelValueLabel, jobValueLabel, goalValueLabel });
            layout.AddTogglesGroup(
                PracticeLayout.Toggle(slowMotionCheckBox, "Slow Motion", null),
                PracticeLayout.Toggle(skipCutscenesCheckBox, "Auto-skip Cutscenes", null),
                PracticeLayout.Toggle(alwaysOnTopCheckBox, "Always On Top", null));

            layout.ToolTip.SetToolTip(jobComboBox,
                "Every job in the game's job list: each minigame stage is one. Jobs start the way the\n" +
                "arcade machines start them, so the stage's challenges are set up.");
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
            jobQueue.StartJob(job, false, false);
        }

        private void LoadHub()
        {
            jobQueue.LoadLevel(HubLevelHash, "the hub", null, null);
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
                string job = game.GetJobName();
                jobValueLabel.Text = job == null ? "-" : job;

                game.GetCheckpoint(out int checkpoint, out int goalCount, out string goalName);
                string goal = goalCount == 0 ? "-" : $"{checkpoint} / {goalCount}";
                if (goalCount > 0 && !string.IsNullOrEmpty(goalName))
                {
                    goal += $" ({goalName})";
                }
                if (game.IsJobFailed())
                {
                    goal += " (failed)";
                }
                goalValueLabel.Text = goal;

                ShowPopUpAfterLoad(loading);
            }
            catch
            {
                levelValueLabel.Text = "-";
                jobValueLabel.Text = "-";
                goalValueLabel.Text = "-";
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
            // The console dropped the watcher's subscriptions with the reset.
            game.StartGoalWatcher(true);
            practiceTabs.RefreshShownTool();
        }

        private void HackpackPractice_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The tools' own cleanup (timers) runs when they close.
            practiceTabs.CloseAll();
            CameraApi.Stop();
            FreeMove.Stop();
            game.StopGoalWatcher();
            refreshTimer.Stop();
            jobQueue.Stop();

            // Leave the game unmodified. This runs before the session disconnects.
            if (slowMotionCheckBox.Checked)
            {
                try { game.SetSlowMotion(false); } catch { }
            }
            if (skipCutscenesCheckBox.Checked)
            {
                try { game.SetSkipCutscenes(false); } catch { }
            }

            // Put back what the Tunables tab changed; kept tunables are written again next time.
            try { game.tunables.ResetToDefaults(false); } catch { }

            // Put the game's code back. Does nothing if no job or load was started.
            try { game.RemoveCodeHook(); } catch { }
        }
    }
}
