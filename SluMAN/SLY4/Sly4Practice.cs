using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SluMAN
{
    public partial class Sly4Practice : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        // The game state is refreshed every few ticks: it takes a dozen reads, and on a PS3 every
        // read is a network round trip.
        private const int GameStateRefreshTicks = 5;

        // How long a queued warp may wait for the game's next frame before it's dropped.
        private static readonly TimeSpan GameCallTimeout = TimeSpan.FromSeconds(3);

        private const string PlayerTab = "Player";
        private const string CameraTab = "Camera";
        private const string JobsTab = "Jobs";
        private const string TunablesTab = "Tunables";
        private const string GuiTab = "GUI";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";

        /// <summary>Fly mode (Player tab) and the free camera (Camera tab): one runs at a time.</summary>
        public Sly4FreeMove FreeMove;

        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        public Sly4CameraApi CameraApi;

        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        private int refreshTick = 0;
        // Whether the last tick saw a load, for the Practice Mode popup after it.
        private bool wasLoading;
        private bool gameCallPending = false;
        private DateTime gameCallDeadline;
        private string gameCallDoneMessage;

        // A goal to warp to once the job started for it is running (see CheckQueuedGoalWarp).
        private static readonly TimeSpan QueuedGoalWarpTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan QueuedGoalWarpSettleTime = TimeSpan.FromSeconds(1);
        private sly4.JobEntry queuedGoalJob = null;
        private int queuedGoal;
        private DateTime queuedGoalDeadline;
        private DateTime queuedGoalReadySince = DateTime.MaxValue;

        // A job to start again once it has been abandoned, for Reload Job on Warp (see WarpToGoal).
        private sly4.JobEntry queuedJobStart = null;
        private int queuedJobStartGoal;

        // A level to load once the job being abandoned for it has ended (see loadLevelButton_Click).
        private sly4.LevelEntry queuedLevel = null;
        private sly4.CharacterEntry queuedCharacter = null;

        private sly4.SpawnPointEntry queuedSpawnPoint = null;

        private const string LevelCharacter = "Level's own character";
        private const string LevelStart = "Level start";

        public Sly4Practice(sly4 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            for (int slot = 1; slot <= IGame.PositionSlotCount; slot++)
            {
                slotComboBox.Items.Add($"Slot {slot}");
            }
            slotComboBox.SelectedIndex = game.selectedPositionIndex;

            foreach (sly4.LevelEntry level in sly4.GetWarpLevels())
            {
                levelComboBox.Items.Add(level);
            }
            // The first entry loads as the level's own character.
            characterComboBox.Items.Add(LevelCharacter);
            characterComboBox.Items.AddRange(sly4.Characters);
            characterComboBox.SelectedIndex = 0;
            levelComboBox.SelectedIndexChanged += (sender, e) => FillSpawnPoints();
            FillSpawnPoints();

            // Before the toggles below read the game, so they show any kept cheat tunables.
            ApplyKeptTunables();

            try
            {
                slowMotionCheckBox.Checked = game.IsSlowMotionOn();
            }
            catch
            {
                // Left unchecked; ticking it writes both values anyway.
            }

            try
            {
                invulnerableCheckBox.Checked = game.IsInvulnerable();
                undetectableCheckBox.Checked = game.IsUndetectable();
                skipCutscenesCheckBox.Checked = game.IsSkippingCutscenes();
            }
            catch
            {
                // Left unchecked; ticking them writes the values anyway.
            }

            // The controller feeds the combos and the input display.
            game.SetupInputDisplayMemorySubs();

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Sly 4", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            game.GoalsChanged += game_GoalsChanged;
            game.StartGoalWatcher();

            refreshTimer.Start();

            FreeMove = new Sly4FreeMove(game);
            FreeMove.Stopped += (message) =>
            {
                if (!IsDisposed)
                {
                    statusLine.Error($"Fly / free camera stopped: {message}");
                }
            };
            CameraApi = new Sly4CameraApi(game, FreeMove);
            CameraApi.StartFromConfig();

            ToolStripMenuItem inputDisplayItem = new ToolStripMenuItem("Input Display");
            inputDisplayItem.Click += (sender, e) => session.ShowInputDisplay();
            toolsToolStripMenuItem.DropDownItems.Insert(0, inputDisplayItem);

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            practiceTabs.AddTool(PlayerTab, () => new Sly4PositionForm(game, FreeMove));
            practiceTabs.AddTool(CameraTab, () => new Sly4CameraForm(game, FreeMove, CameraApi));
            practiceTabs.AddTool(JobsTab, () => new Sly4JobsForm(game, this));
            practiceTabs.AddTool(TunablesTab, () => new Sly4TunablesForm(game));
            practiceTabs.AddTool(GuiTab, () => new Sly4GuiForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice and Player show at first.
            practiceTabs.AddViewMenu("sly4_visible_tabs", PlayerTab);
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

        private void game_GoalsChanged()
        {
            // Raised on the goal watcher's thread when a goal is finished or a job starts or ends,
            // so the checkpoint shows it at once rather than on the next refresh.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed && gameGroupBox.Visible)
                    {
                        RefreshGameState();
                    }
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            // The labels are only read while the Practice tab shows. The timer keeps running for
            // the warps it tracks.
            if (refreshTick++ % GameStateRefreshTicks == 0 && gameGroupBox.Visible)
            {
                RefreshGameState();
            }
            ShowPopUpAfterLoad();

            if (gameCallPending)
            {
                CheckGameCall();
            }
            else if (queuedLevel != null)
            {
                LoadQueuedLevel();
            }
            else if (queuedJobStart != null)
            {
                StartQueuedJob();
            }
            else if (queuedGoalJob != null)
            {
                CheckQueuedGoalWarp();
            }
        }

        /// <summary>
        /// Shows webMAN's "SluMAN (Practice Mode)" popup on the PS3 when a load finishes, like the
        /// other games, so a run's video shows Practice Mode was on. Ratchetron only.
        /// </summary>
        private void ShowPopUpAfterLoad()
        {
            if (!(game.api is Ratchetron))
            {
                return;
            }
            bool loading;
            try
            {
                loading = game.IsLoading();
            }
            catch
            {
                // Not connected right now.
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

        /// <summary>
        /// Warps to the queued goal once its job is running, done loading and has the goal loaded,
        /// and has stayed that way for a moment, so the warp doesn't land in the job's setup.
        /// </summary>
        private void CheckQueuedGoalWarp()
        {
            if (DateTime.Now > queuedGoalDeadline)
            {
                statusLine.Error($"{queuedGoalJob.Title} didn't start in time, so it wasn't warped to goal {queuedGoal + 1}.");
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
                TrackGameCall($"Warping to goal {goal + 1}.");
            }
            catch (Exception ex)
            {
                queuedGoalJob = null;
                statusLine.Error($"Couldn't warp to the goal: {ex.Message}");
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
                    queuedLevel = null;
                    queuedJobStart = null;
                    game.CancelGameCall();
                    statusLine.Error("The game didn't run the warp. On RPCS3, set the PPU decoder to Interpreter (static).");
                }
            }
            catch
            {
                // Checked again on the next tick.
            }
        }

        /// <summary>Starts waiting for a queued game call, reporting the message once it has run.</summary>
        private void TrackGameCall(string doneMessage)
        {
            gameCallPending = true;
            gameCallDeadline = DateTime.Now + GameCallTimeout;
            gameCallDoneMessage = doneMessage;
        }

        private void jobComboBox_DropDown(object sender, EventArgs e)
        {
            if (jobComboBox.Items.Count > 0)
            {
                return;
            }

            try
            {
                foreach (sly4.JobEntry job in game.GetJobs())
                {
                    jobComboBox.Items.Add(job);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the jobs: {ex.Message}");
            }
        }

        private void jobComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillGoals(false);
        }

        private void goalComboBox_DropDown(object sender, EventArgs e)
        {
            // Read again every time: the running job, and so the goals read from the game, may
            // have changed since.
            FillGoals(true);
        }

        /// <summary>
        /// Fills the goal list with the selected job's goals, or the running job's when no job is
        /// selected. Keeps the selected goal when the list stays the same length.
        /// </summary>
        private void FillGoals(bool reportEmpty)
        {
            int selected = goalComboBox.SelectedIndex;
            goalComboBox.Items.Clear();
            try
            {
                sly4.JobEntry job = jobComboBox.SelectedItem as sly4.JobEntry;
                List<string> goals = job == null ? game.GetGoalNames() : game.GetGoalNames(job);
                for (int i = 0; i < goals.Count; i++)
                {
                    goalComboBox.Items.Add($"{i + 1}. {goals[i]}");
                }

                if (selected >= 0 && selected < goals.Count)
                {
                    goalComboBox.SelectedIndex = selected;
                }
                if (goals.Count == 0 && reportEmpty)
                {
                    statusLine.Error(job == null ? "Pick a job, or start one to list its goals." : $"{job.Title}'s goals aren't known.");
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the goals: {ex.Message}");
            }
        }

        private void startJobButton_Click(object sender, EventArgs e)
        {
            StartSelectedJob(false, false);
        }

        private void toHubButton_Click(object sender, EventArgs e)
        {
            StartSelectedJob(true, false);
        }

        private void toHideoutButton_Click(object sender, EventArgs e)
        {
            StartSelectedJob(false, true);
        }

        private void StartSelectedJob(bool toHub, bool toHideout)
        {
            sly4.JobEntry job = jobComboBox.SelectedItem as sly4.JobEntry;
            if (job == null)
            {
                statusLine.Error("Pick a job first.");
                return;
            }
            StartJob(job, toHub, toHideout);
        }

        /// <summary>
        /// Starts a job, or goes to its episode's hub or hideout. Used by the Warp controls and
        /// the Jobs tab; the result shows on the status bar.
        /// </summary>
        public void StartJob(sly4.JobEntry job, bool toHub, bool toHideout)
        {
            queuedGoalJob = null;
            queuedLevel = null;
            queuedJobStart = null;
            try
            {
                game.StartJob(job.index, toHub, toHideout);
                string where = toHub ? $"{job.EpisodeTitle}'s hub" : (toHideout ? $"{job.EpisodeTitle}'s hideout" : job.Title);
                TrackGameCall($"Going to {where}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't start the job: {ex.Message}");
            }
        }

        private void warpToGoalButton_Click(object sender, EventArgs e)
        {
            if (goalComboBox.SelectedIndex < 0)
            {
                statusLine.Error("Pick a goal first.");
                return;
            }
            WarpToGoal(jobComboBox.SelectedItem as sly4.JobEntry, goalComboBox.SelectedIndex);
        }

        /// <summary>
        /// Warps to a goal (0-based) of a job: in place if the job is running (or no job is
        /// given), otherwise by starting the job and warping once it has loaded. With Reload Job
        /// on Warp, a running job is abandoned and started again instead, as if coming from
        /// another job, for goals whose setup (such as a cutscene) a warp in place skips.
        /// </summary>
        public void WarpToGoal(sly4.JobEntry job, int goal)
        {
            queuedGoalJob = null;
            queuedLevel = null;
            queuedJobStart = null;
            try
            {
                if (job == null && reloadJobCheckBox.Checked)
                {
                    job = RunningJob();
                }

                if (job == null || (game.IsJobRunning(job.index) && !reloadJobCheckBox.Checked))
                {
                    game.WarpToGoal(goal);
                    TrackGameCall($"Warping to goal {goal + 1}.");
                }
                else if (game.IsJobRunning(job.index))
                {
                    // The game won't start the running job, so end it and start it once that has run.
                    game.AbandonJob();
                    queuedJobStart = job;
                    queuedJobStartGoal = goal;
                    TrackGameCall($"Left {job.Title}; starting it again.");
                }
                else
                {
                    StartJobAtGoal(job, goal);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't warp: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a job that isn't running and queues the warp to the goal (0-based): the game
        /// can't start a job at a later goal, so it warps once the job runs.
        /// </summary>
        private void StartJobAtGoal(sly4.JobEntry job, int goal)
        {
            game.StartJob(job.index, false, false);
            if (goal > 0)
            {
                queuedGoalJob = job;
                queuedGoal = goal;
                queuedGoalDeadline = DateTime.Now + QueuedGoalWarpTimeout;
                queuedGoalReadySince = DateTime.MaxValue;
            }
            TrackGameCall(goal > 0
                ? $"Starting {job.Title}; it warps to goal {goal + 1} once loaded."
                : $"Starting {job.Title}.");
        }

        private void StartQueuedJob()
        {
            sly4.JobEntry job = queuedJobStart;
            queuedJobStart = null;
            try
            {
                StartJobAtGoal(job, queuedJobStartGoal);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't start the job again: {ex.Message}");
            }
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

        private void retryCheckpointButton_Click(object sender, EventArgs e)
        {
            int checkpoint;
            try
            {
                game.GetCheckpoint(out checkpoint, out int goalCount, out string goalName);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the checkpoint: {ex.Message}");
                return;
            }
            if (checkpoint == 0)
            {
                statusLine.Error("Not in a job, so there's no checkpoint to retry.");
                return;
            }
            WarpToGoal(null, checkpoint - 1);
        }

        private void loadLevelButton_Click(object sender, EventArgs e)
        {
            sly4.LevelEntry level = levelComboBox.SelectedItem as sly4.LevelEntry;
            if (level == null)
            {
                statusLine.Error("Pick a level first.");
                return;
            }

            sly4.CharacterEntry character = characterComboBox.SelectedItem as sly4.CharacterEntry;
            sly4.SpawnPointEntry spawnPoint = spawnPointComboBox.SelectedItem as sly4.SpawnPointEntry;
            queuedGoalJob = null;
            queuedLevel = null;
            queuedJobStart = null;
            try
            {
                // A running job would load its own level instead, so end it first and load the
                // level once the game has run that call.
                if (game.IsInJob())
                {
                    game.AbandonJob();
                    queuedLevel = level;
                    queuedCharacter = character;
                    queuedSpawnPoint = spawnPoint;
                    TrackGameCall($"Left the job; loading {level.name}.");
                    return;
                }
                game.LoadLevel(level.hash, character, spawnPoint);
                TrackGameCall(LoadingMessage(level, character, spawnPoint));
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the level: {ex.Message}");
            }
        }

        private void LoadQueuedLevel()
        {
            sly4.LevelEntry level = queuedLevel;
            queuedLevel = null;
            try
            {
                game.LoadLevel(level.hash, queuedCharacter, queuedSpawnPoint);
                TrackGameCall(LoadingMessage(level, queuedCharacter, queuedSpawnPoint));
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the level: {ex.Message}");
            }
        }

        private static string LoadingMessage(sly4.LevelEntry level, sly4.CharacterEntry character, sly4.SpawnPointEntry spawnPoint)
        {
            string message = $"Loading {level.name}";
            if (spawnPoint != null)
            {
                message += $" at {spawnPoint.title}";
            }
            if (character != null)
            {
                message += $" as {character.title}";
            }
            return message + ".";
        }

        /// <summary>Lists the selected level's spawn points, after its default start.</summary>
        private void FillSpawnPoints()
        {
            spawnPointComboBox.Items.Clear();
            spawnPointComboBox.Items.Add(LevelStart);
            sly4.LevelEntry level = levelComboBox.SelectedItem as sly4.LevelEntry;
            if (level != null)
            {
                foreach (sly4.SpawnPointEntry spawnPoint in sly4.GetSpawnPoints(level.hash))
                {
                    spawnPointComboBox.Items.Add(spawnPoint);
                }
            }
            spawnPointComboBox.SelectedIndex = 0;
            spawnPointComboBox.Enabled = spawnPointComboBox.Items.Count > 1;
        }

        private void RefreshGameState()
        {
            try
            {
                uint levelHash = game.GetLevelHash();
                string level = levelHash == 0 ? "-" : sly4.GetLevelName(levelHash);
                if (game.IsLoading())
                {
                    level += " (loading)";
                }
                levelValueLabel.Text = level;
                savedPositionsGroupBox.Text = levelHash == 0 ? "Saved Positions" : $"Saved Positions ({sly4.GetLevelName(levelHash)})";

                string episode = game.GetEpisodeName();
                episodeValueLabel.Text = episode == null ? "-" : episode;

                string job = game.GetJobName();
                jobValueLabel.Text = job == null ? "-" : job;

                game.GetCheckpoint(out int checkpoint, out int goalCount, out string goalName);
                string checkpointText = goalCount == 0 ? "-" : $"{checkpoint} / {goalCount}";
                if (game.IsJobFailed())
                {
                    checkpointText += " (failed)";
                }
                checkpointValueLabel.Text = checkpointText;
                goalValueLabel.Text = string.IsNullOrEmpty(goalName) ? "-" : goalName;
            }
            catch
            {
                levelValueLabel.Text = "-";
                episodeValueLabel.Text = "-";
                jobValueLabel.Text = "-";
                checkpointValueLabel.Text = "-";
                goalValueLabel.Text = "-";
            }
        }

        private void slotComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            game.selectedPositionIndex = slotComboBox.SelectedIndex;
        }

        private void savePosButton_Click(object sender, EventArgs e)
        {
            try
            {
                game.SavePosition();
                statusLine.Info($"Saved position to slot {game.selectedPositionIndex + 1}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save the position: {ex.Message}");
            }
        }

        private void loadPosButton_Click(object sender, EventArgs e)
        {
            try
            {
                game.LoadPosition();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the position: {ex.Message}");
            }
        }

        private void slowMotionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetSlowMotion(slowMotionCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the game speed: {ex.Message}");
            }
        }

        private void invulnerableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetInvulnerable(invulnerableCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change invulnerability: {ex.Message}");
            }
        }

        private void undetectableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetUndetectable(undetectableCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change undetectability: {ex.Message}");
            }
        }

        private void skipCutscenesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetSkipCutscenes(skipCutscenesCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change cutscene skipping: {ex.Message}");
            }
        }

        private void session_Reconnected()
        {
            // A game reset puts the tunables back to their defaults.
            try
            {
                if (slowMotionCheckBox.Checked)
                {
                    game.SetSlowMotion(true);
                }
                if (invulnerableCheckBox.Checked)
                {
                    game.SetInvulnerable(true);
                }
                if (undetectableCheckBox.Checked)
                {
                    game.SetUndetectable(true);
                }
                if (skipCutscenesCheckBox.Checked)
                {
                    game.SetSkipCutscenes(true);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't turn the cheats back on: {ex.Message}");
            }

            ApplyKeptTunables();
            // The console dropped the watcher's subscriptions with the reset.
            game.StartGoalWatcher(true);
            practiceTabs.RefreshShownTool();
        }

        private void Sly4Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The tools' own cleanup (timers) runs when they close.
            practiceTabs.CloseAll();
            CameraApi.Stop();
            FreeMove.Stop();
            game.GoalsChanged -= game_GoalsChanged;
            game.StopGoalWatcher();
            refreshTimer.Stop();

            // Leave the game unmodified. This runs before the session disconnects.
            if (slowMotionCheckBox.Checked)
            {
                try { game.SetSlowMotion(false); } catch { }
            }
            if (invulnerableCheckBox.Checked)
            {
                try { game.SetInvulnerable(false); } catch { }
            }
            if (undetectableCheckBox.Checked)
            {
                try { game.SetUndetectable(false); } catch { }
            }
            if (skipCutscenesCheckBox.Checked)
            {
                try { game.SetSkipCutscenes(false); } catch { }
            }

            // Put the game's code back. Does nothing if no warp was used.
            try { game.RemoveCodeHook(); } catch { }
        }

        private void switchGameModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            practiceTabs.Show(MemoryTab);
        }

        private void openUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserData.OpenInExplorer();
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.RebootPS3();
        }
    }
}
