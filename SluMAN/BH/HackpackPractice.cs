using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Bentley's Hackpack Practice Mode: start or restart a job (each minigame's challenges run in
    /// one) or go back to the hub, slow motion and cutscene skipping, with the jobs and their
    /// challenges (goals), the camera (free camera and camera paths), the tunables and the memory
    /// tools on tabs. The game runs on Sly 4's engine, so this drives the sly4 class and Sly 4's
    /// tools. The controls are built in code, in the other games' two-column layout.
    /// </summary>
    public class HackpackPractice : Form, ISly4JobHost
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
        // How often a queued game call, and a goal warp waiting for its job, is checked.
        private const int GameCallCheckMs = 100;

        // How long a queued call may wait for the game's next frame before it's dropped.
        private static readonly TimeSpan GameCallTimeout = TimeSpan.FromSeconds(3);

        // A goal warp waits this long for its job to start and load, and for it to have stayed
        // that way this long, so the warp doesn't land in the job's setup.
        private static readonly TimeSpan QueuedGoalWarpTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan QueuedGoalWarpSettleTime = TimeSpan.FromSeconds(1);

        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly ComboBox jobComboBox = new ComboBox();
        private readonly CheckBox slowMotionCheckBox = new CheckBox();
        private readonly CheckBox skipCutscenesCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label jobValueLabel = new Label();
        private readonly Label challengeValueLabel = new Label();
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
        private bool gameCallPending;
        private DateTime gameCallDeadline;
        private string gameCallDoneMessage;

        // A goal to warp to once the job started for it runs (see CheckQueuedGoalWarp).
        private sly4.JobEntry queuedGoalJob;
        private int queuedGoal;
        private DateTime queuedGoalDeadline;
        private DateTime queuedGoalReadySince = DateTime.MaxValue;

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

            // The Jobs tab colours the running job's goals as they change.
            game.StartGoalWatcher();

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();
            RefreshStatus();
            gameCallTimer.Interval = GameCallCheckMs;
            gameCallTimer.Tick += (sender, e) => CheckQueuedWork();

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
            practiceTabs.AddTool(JobsTab, () => new Sly4JobsForm(game, this));
            practiceTabs.AddTool(CameraTab, () => new Sly4CameraForm(game, FreeMove, CameraApi));
            practiceTabs.AddTool(TunablesTab, () => new Sly4TunablesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("hackpack_visible_tabs", JobsTab, CameraTab);
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
            AddButton(jobGroupBox, "Start Job", 10, 48, 143, (sender, e) => StartSelectedJob());
            AddButton(jobGroupBox, "Restart Job", 159, 48, 143, (sender, e) => RestartRunningJob());

            GroupBox hubGroupBox = AddGroup("Hub", 12, 118, 54);
            AddButton(hubGroupBox, "Load Hub", 10, 19, 292, (sender, e) => LoadHub());

            // Right column
            GroupBox statusGroupBox = AddGroup("Game", 336, 32, 90);
            AddLabel(statusGroupBox, "Level:", 10, 20);
            AddValueLabel(statusGroupBox, levelValueLabel, 70, 20);
            AddLabel(statusGroupBox, "Job:", 10, 42);
            AddValueLabel(statusGroupBox, jobValueLabel, 70, 42);
            AddLabel(statusGroupBox, "Challenge:", 10, 64);
            AddValueLabel(statusGroupBox, challengeValueLabel, 70, 64);

            GroupBox togglesGroupBox = AddGroup("Toggles", 336, 128, 70);
            AddCheckBox(togglesGroupBox, slowMotionCheckBox, "Slow Motion", 10, 20);
            AddCheckBox(togglesGroupBox, skipCutscenesCheckBox, "Skip Cutscenes", 10, 43);
            AddCheckBox(togglesGroupBox, alwaysOnTopCheckBox, "Always On Top", 160, 20);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(jobComboBox,
                "Every job in the game's job list. A minigame's challenges only work when it's\n" +
                "started as a job. The Jobs tab also shows each running job's challenges.");
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
            StartJob(job, false, false);
        }

        private void RestartRunningJob()
        {
            try
            {
                sly4.JobEntry job = RunningJob();
                if (job == null)
                {
                    statusLine.Error("No job is running.");
                    return;
                }
                StartJobAtGoal(job, 0, true);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't restart the job: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a job, from the Job controls or the Jobs tab; a running job starts again. The
        /// Hackpack has no hubs or hideouts per episode, so those are ignored.
        /// </summary>
        public void StartJob(sly4.JobEntry job, bool toHub, bool toHideout)
        {
            try
            {
                StartJobAtGoal(job, 0, game.IsJobRunning(job.index));
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't start the job: {ex.Message}");
            }
        }

        /// <summary>
        /// Warps to a goal (0-based) of a job, from the Jobs tab: in place if the job is running
        /// (or no job is given), otherwise by starting the job and warping once it has loaded.
        /// </summary>
        public void WarpToGoal(sly4.JobEntry job, int goal)
        {
            try
            {
                if (job == null || game.IsJobRunning(job.index))
                {
                    queuedGoalJob = null;
                    game.WarpToGoal(goal);
                    TrackGameCall($"Warping to challenge {goal + 1}.");
                }
                else
                {
                    StartJobAtGoal(job, goal, false);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't warp: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a job (again, if it runs) and queues the warp to one of its goals (0-based) for
        /// once it runs. The warp to the first goal matters too: the game's own job start
        /// teleports to the goal after starting the job, which sets the goal (and the minigame's
        /// challenge menu) up.
        /// </summary>
        private void StartJobAtGoal(sly4.JobEntry job, int goal, bool restart)
        {
            game.StartJob(job.index, false, false, 0, restart);
            queuedGoalJob = job;
            queuedGoal = goal;
            queuedGoalDeadline = DateTime.Now + QueuedGoalWarpTimeout;
            queuedGoalReadySince = DateTime.MaxValue;
            string verb = restart ? "Restarting" : "Starting";
            TrackGameCall(goal > 0 ? $"{verb} {job.Title}; it warps to challenge {goal + 1} once loaded." : $"{verb} {job.Title}.");
        }

        /// <summary>The running job's entry, or null outside jobs.</summary>
        private sly4.JobEntry RunningJob()
        {
            int index = game.GetRunningJobIndex();
            if (index < 0)
            {
                return null;
            }
            foreach (sly4.JobEntry job in game.GetJobs())
            {
                if (job.index == index)
                {
                    return job;
                }
            }
            return null;
        }

        private void LoadHub()
        {
            try
            {
                queuedGoalJob = null;
                game.LoadLevel(HubLevelHash, null, null);
                TrackGameCall("Loading the hub.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the hub: {ex.Message}");
            }
        }

        /// <summary>Waits for a call queued through the code hook, reporting the message once it has run.</summary>
        private void TrackGameCall(string doneMessage)
        {
            gameCallPending = true;
            gameCallDeadline = DateTime.Now + GameCallTimeout;
            gameCallDoneMessage = doneMessage;
            gameCallTimer.Start();
        }

        private void CheckQueuedWork()
        {
            if (gameCallPending)
            {
                CheckGameCall();
            }
            else if (queuedGoalJob != null)
            {
                CheckQueuedGoalWarp();
            }
            else
            {
                gameCallTimer.Stop();
            }
        }

        private void CheckGameCall()
        {
            try
            {
                if (!game.IsGameCallPending())
                {
                    gameCallPending = false;
                    statusLine.Info(gameCallDoneMessage);
                }
                else if (DateTime.Now > gameCallDeadline)
                {
                    gameCallPending = false;
                    queuedGoalJob = null;
                    game.CancelGameCall();
                    statusLine.Error("The game didn't run the call. On RPCS3, set the PPU decoder to Interpreter (static).");
                }
            }
            catch
            {
                // Checked again on the next tick.
            }
        }

        /// <summary>
        /// Warps to the queued goal once its job is running, done loading and has the goal loaded,
        /// and has stayed that way for a moment, so the warp doesn't land in the job's setup.
        /// </summary>
        private void CheckQueuedGoalWarp()
        {
            if (DateTime.Now > queuedGoalDeadline)
            {
                statusLine.Error($"{queuedGoalJob.Title} didn't start in time, so it wasn't warped to challenge {queuedGoal + 1}.");
                queuedGoalJob = null;
                return;
            }

            try
            {
                bool ready = !game.IsLoading() && game.IsJobRunning(queuedGoalJob.index) && game.GetGoalCount() > queuedGoal;
                if (!ready)
                {
                    queuedGoalReadySince = DateTime.MaxValue;
                    return;
                }
                if (queuedGoalReadySince == DateTime.MaxValue)
                {
                    queuedGoalReadySince = DateTime.Now;
                }
                if (DateTime.Now - queuedGoalReadySince < QueuedGoalWarpSettleTime)
                {
                    return;
                }

                int goal = queuedGoal;
                queuedGoalJob = null;
                game.WarpToGoal(goal);
                TrackGameCall(goal > 0 ? $"Warped to challenge {goal + 1}." : "Job started.");
            }
            catch (Exception ex)
            {
                queuedGoalJob = null;
                statusLine.Error($"Couldn't warp to the challenge: {ex.Message}");
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
                string job = game.GetJobName();
                jobValueLabel.Text = job == null ? "-" : job;

                game.GetCheckpoint(out int checkpoint, out int goalCount, out string goalName);
                string challenge = goalCount == 0 ? "-" : $"{checkpoint} / {goalCount}";
                if (goalCount > 0 && !string.IsNullOrEmpty(goalName))
                {
                    challenge += $" ({goalName})";
                }
                if (game.IsJobFailed())
                {
                    challenge += " (failed)";
                }
                challengeValueLabel.Text = challenge;

                ShowPopUpAfterLoad(loading);
            }
            catch
            {
                levelValueLabel.Text = "-";
                jobValueLabel.Text = "-";
                challengeValueLabel.Text = "-";
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

            // Put the game's code back. Does nothing if no job or load was started.
            try { game.RemoveCodeHook(); } catch { }
        }
    }
}
