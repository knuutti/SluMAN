using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Sly 4 Practice Mode: load a level (at a spawn point, as a character) or a job at a goal,
    /// retry the checkpoint, saved positions, coins and the toggles, with the player, the camera,
    /// the jobs, the tunables, the GUI and the memory tools on tabs. The controls are built in
    /// code (PracticeLayout), in the layout every game shares.
    /// </summary>
    public class Sly4Practice : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        // The timer that refreshes the game state.
        private const int RefreshIntervalMs = 200;

        private readonly ComboBox levelComboBox = new ComboBox();
        private readonly ComboBox characterComboBox = new ComboBox();
        private readonly ComboBox spawnPointComboBox = new ComboBox();
        private readonly ComboBox jobComboBox = new ComboBox();
        private readonly ComboBox goalComboBox = new ComboBox();
        private readonly ComboBox slotComboBox = new ComboBox();
        private readonly TextBox coinsTextBox = new TextBox();
        private readonly CheckBox invulnerableCheckBox = new CheckBox();
        private readonly CheckBox undetectableCheckBox = new CheckBox();
        private readonly CheckBox gameClockCheckBox = new CheckBox();
        private readonly CheckBox slowMotionCheckBox = new CheckBox();
        private readonly CheckBox flyModeCheckBox = new CheckBox();
        private readonly CheckBox skipCutscenesCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label episodeValueLabel = new Label();
        private readonly Label jobValueLabel = new Label();
        private readonly Label checkpointValueLabel = new Label();
        private readonly Label goalValueLabel = new Label();
        private readonly Label coinsValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        private PracticeLayout layout;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private GroupBox gameGroupBox;
        private GroupBox savedPositionsGroupBox;

        // The game state is refreshed every few ticks: it takes a dozen reads, and on a PS3 every
        // read is a network round trip.
        private const int GameStateRefreshTicks = 5;

        private const string PlayerTab = "Player";
        private const string CameraTab = "Camera";
        private const string JobsTab = "Jobs";
        private const string GadgetsTab = "Gadgets";
        private const string RunFilesTab = "Run Files";
        private const string TunablesTab = "Tunables";
        private const string GuiTab = "GUI";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";

        /// <summary>Fly mode (Player tab) and the free camera (Camera tab): one runs at a time.</summary>
        public Sly4FreeMove FreeMove;

        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        public CameraScriptApi CameraApi;

        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        private int refreshTick = 0;
        // Whether the last tick saw a load, for the Practice Mode popup after it.
        private bool wasLoading;
        // Job starts, goal warps and level loads (shared with the Hackpack and the Jobs tab).
        private Sly4JobQueue jobQueue;

        private const string LevelCharacter = "Level's own character";
        private const string LevelStart = "Level start";

        public Sly4Practice(sly4 game)
        {
            this.game = game;
            PracticeLayout.SetUpWindow(this, "Sly4Practice", "SluMAN :: Sly Cooper: Thieves in Time (Practice Mode)");
            layout = new PracticeLayout(this);
            BuildControls();
            statusLine = new StatusLine(this, true);
            jobQueue = new Sly4JobQueue(game, statusLine);
            // Before the session's own handler, which disconnects: this one still writes to the game.
            FormClosing += Sly4Practice_FormClosing;

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
                // Before Slow Motion: a frozen clock keeps Slow Motion's clamps for when it runs again.
                gameClockCheckBox.Checked = game.IsGameClockFrozen();
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
            // After the game's state is shown, so showing it writes nothing.
            gameClockCheckBox.CheckedChanged += gameClockCheckBox_CheckedChanged;
            slowMotionCheckBox.CheckedChanged += slowMotionCheckBox_CheckedChanged;
            invulnerableCheckBox.CheckedChanged += invulnerableCheckBox_CheckedChanged;
            undetectableCheckBox.CheckedChanged += undetectableCheckBox_CheckedChanged;
            skipCutscenesCheckBox.CheckedChanged += skipCutscenesCheckBox_CheckedChanged;

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

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();

            FreeMove = new Sly4FreeMove(game);
            FreeMove.Stopped += (message) =>
            {
                if (!IsDisposed)
                {
                    statusLine.Error($"Fly / free camera stopped: {message}");
                }
            };
            CameraApi = new CameraScriptApi(game.GetCameraTabLayout(FreeMove));
            CameraScriptApi.BindClockToggle(CameraApi.Layout, gameClockCheckBox, game.SetGameClockFrozen);
            CameraApi.StartFromConfig();
            // The free camera stops the fly, and the fly the free camera.
            FreeMove.ModeChanged += ShowFlyMode;

            session.FillPracticeToolsMenu(toolsToolStripMenuItem, () => practiceTabs.Show(MemoryTab));

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            practiceTabs.AddTool(PlayerTab, () => new PositionEditor(game, game.GetPositionEditorLayout(FreeMove), LevelDisplayName));
            practiceTabs.AddTool(CameraTab, () => new CameraForm(CameraApi.Layout));
            practiceTabs.AddTool(JobsTab, () => new Sly4JobsForm(game, jobQueue));
            practiceTabs.AddTool(GadgetsTab, () => new Sly4GadgetsForm(game, jobQueue));
            practiceTabs.AddTool(RunFilesTab, () => new Sly4RunFilesForm(game, jobQueue));
            practiceTabs.AddTool(TunablesTab, () => new Sly4TunablesForm(game));
            practiceTabs.AddTool(GuiTab, () => new Sly4GuiForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly4_visible_tabs", PlayerTab, CameraTab, JobsTab, GadgetsTab, RunFilesTab);
        }

        /// <summary>The Practice tab, in the slots every game shares (PracticeLayout).</summary>
        private void BuildControls()
        {
            toolsToolStripMenuItem = layout.AddMenu();

            // Left column
            levelComboBox.MaxDropDownItems = 20;
            characterComboBox.MaxDropDownItems = 20;
            spawnPointComboBox.MaxDropDownItems = 20;
            layout.AddPickerGroup("Level", levelComboBox, spawnPointComboBox, loadLevelButton_Click, characterComboBox);
            jobComboBox.MaxDropDownItems = 20;
            goalComboBox.MaxDropDownItems = 20;
            goalComboBox.DropDownWidth = 300;
            jobComboBox.DropDown += jobComboBox_DropDown;
            jobComboBox.SelectedIndexChanged += jobComboBox_SelectedIndexChanged;
            goalComboBox.DropDown += goalComboBox_DropDown;
            layout.AddPickerGroup("Job", jobComboBox, goalComboBox, (sender, e) => LoadSelectedJob());
            layout.AddReloadGroup(new Button[]
            {
                layout.PlaceholderButton("Reload"),
                PracticeLayout.MakeButton("Retry Checkpoint", retryCheckpointButton_Click),
                layout.PlaceholderButton("Skip Cinematic"),
            }, null);
            savedPositionsGroupBox = layout.AddSavedPositionsGroup(game, slotComboBox, savePosButton_Click, loadPosButton_Click);

            // Right column
            gameGroupBox = layout.AddStatusGroup(new string[] { "Level", "Episode", "Job", "Checkpoint", "Goal", "Coins" },
                new Label[] { levelValueLabel, episodeValueLabel, jobValueLabel, checkpointValueLabel, goalValueLabel, coinsValueLabel });
            layout.AddUtilityGroup(
                PracticeLayout.Setter("Coins", coinsTextBox, setCoinsButton_Click),
                PracticeLayout.PlaceholderSetter("Health"));
            // Every game's toggles in the same order, Always On Top last.
            layout.AddTogglesGroup(
                PracticeLayout.Toggle(invulnerableCheckBox, "Invulnerability", null),
                layout.PlaceholderToggle("Infinite Gadget Power"),
                PracticeLayout.Toggle(undetectableCheckBox, "Undetectable", null),
                PracticeLayout.Toggle(gameClockCheckBox, "Freeze Game Clock", null),
                PracticeLayout.Toggle(slowMotionCheckBox, "Slow Motion", null),
                PracticeLayout.Toggle(flyModeCheckBox, "Fly Mode", null),
                layout.PlaceholderToggle("Infinite Jump"),
                PracticeLayout.Toggle(skipCutscenesCheckBox, "Auto-skip Cutscenes", null),
                PracticeLayout.Toggle(alwaysOnTopCheckBox, "Always On Top", null));

            // Click, not CheckedChanged: showing the fly's mode sets Checked too.
            flyModeCheckBox.Click += flyModeCheckBox_Click;
            layout.ToolTip.SetToolTip(gameClockCheckBox, "Holds the game's time nearly still (about 1,700 times slower), through the same clock clamps as Slow Motion.");
            layout.ToolTip.SetToolTip(flyModeCheckBox,"Left stick moves along the camera's facing, R1 / L1 go up and down. The speed is on the Player tab.");
            layout.ToolTip.SetToolTip(goalComboBox, "Load starts the job at this goal; with no job picked, the running job's.");
        }

        private void flyModeCheckBox_Click(object sender, EventArgs e)
        {
            try
            {
                FreeMove.Start(flyModeCheckBox.Checked ? Sly4FreeMove.Mode.Fly : Sly4FreeMove.Mode.Off);
            }
            catch (Exception ex)
            {
                FreeMove.Stop();
                statusLine.Error($"Couldn't start flying: {ex.Message}");
            }
            ShowFlyMode();
        }

        private void ShowFlyMode()
        {
            if (!IsDisposed)
            {
                flyModeCheckBox.Checked = FreeMove.Current == Sly4FreeMove.Mode.Fly;
            }
        }

        /// <summary>The Player tab's map name: positions are kept per level, as "sly4_" and its hash.</summary>
        private static string LevelDisplayName(string indicator)
        {
            uint hash;
            if (indicator.StartsWith("sly4_") && uint.TryParse(indicator.Substring(5), System.Globalization.NumberStyles.HexNumber, null, out hash))
            {
                return sly4.GetLevelName(hash);
            }
            return null;
        }

        private void setCoinsButton_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(coinsTextBox.Text.Trim(), out int coins))
            {
                statusLine.Error("Coins must be a whole number.");
                return;
            }
            try
            {
                game.SetCoinCount(coins);
                coinsValueLabel.Text = game.GetCoinCount().ToString();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't set coins: {ex.Message}");
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
            // the load popup.
            if (refreshTick++ % GameStateRefreshTicks == 0 && gameGroupBox.Visible)
            {
                RefreshGameState();
            }
            ShowPopUpAfterLoad();
        }

        /// <summary>
        /// Shows webMAN's "SluMAN (Practice Mode)" popup on the PS3 when a load finishes, like the
        /// other games, so a run's video shows Practice Mode was on (Ratchetron only), and drops
        /// the Player tab's position freezes when a load starts.
        /// </summary>
        private void ShowPopUpAfterLoad()
        {
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
            if (!wasLoading && loading)
            {
                // The Player tab's position freezes hold coordinates from the level being left.
                PositionEditor positionEditor = practiceTabs.Get(PlayerTab) as PositionEditor;
                if (positionEditor != null)
                {
                    positionEditor.ClearPositionFreezes();
                }
            }
            if (wasLoading && !loading && game.api is Ratchetron)
            {
                // A web request: off the UI thread.
                string ip = game.api.GetIP();
                System.Threading.ThreadPool.QueueUserWorkItem(_ => WebMAN.DisplayVersionPopUp(ip));
            }
            wasLoading = loading;
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

        /// <summary>
        /// The Job group's Load: the selected job at the selected goal (its first without one),
        /// or the running job's goal when no job is picked. Going to an episode's hub or
        /// hideout is on the Jobs tab.
        /// </summary>
        private void LoadSelectedJob()
        {
            sly4.JobEntry job = jobComboBox.SelectedItem as sly4.JobEntry;
            int goal = goalComboBox.SelectedIndex;
            if (job == null && goal < 0)
            {
                statusLine.Error("Pick a job first.");
                return;
            }
            jobQueue.WarpToGoal(job, Math.Max(goal, 0));
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
            jobQueue.WarpToGoal(null, checkpoint - 1);
        }

        private void loadLevelButton_Click(object sender, EventArgs e)
        {
            sly4.LevelEntry level = levelComboBox.SelectedItem as sly4.LevelEntry;
            if (level == null)
            {
                statusLine.Error("Pick a level first.");
                return;
            }
            jobQueue.LoadLevel(level.hash, level.name,
                characterComboBox.SelectedItem as sly4.CharacterEntry,
                spawnPointComboBox.SelectedItem as sly4.SpawnPointEntry);
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
                try
                {
                    coinsValueLabel.Text = game.GetCoinCount().ToString();
                }
                catch
                {
                    // In the title screen, there's no game manager yet.
                    coinsValueLabel.Text = "-";
                }
            }
            catch
            {
                levelValueLabel.Text = "-";
                episodeValueLabel.Text = "-";
                jobValueLabel.Text = "-";
                checkpointValueLabel.Text = "-";
                goalValueLabel.Text = "-";
                coinsValueLabel.Text = "-";
            }
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

        private void gameClockCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetGameClockFrozen(gameClockCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the game clock: {ex.Message}");
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
                // Before Slow Motion, which a frozen clock keeps for when it runs again.
                game.RefreezeClockAfterReset();
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
            FreeMove.ModeChanged -= ShowFlyMode;
            FreeMove.Stop();
            game.GoalsChanged -= game_GoalsChanged;
            game.StopGoalWatcher();
            refreshTimer.Stop();
            jobQueue.Stop();

            // Leave the game unmodified. This runs before the session disconnects. The clock runs
            // again first, at what it ran at before; then Slow Motion goes.
            if (gameClockCheckBox.Checked)
            {
                try { game.SetGameClockFrozen(false); } catch { }
            }
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

            // Put back what the Tunables tab changed; kept tunables are written again next time.
            try { game.tunables.ResetToDefaults(false); } catch { }

            // Put the game's code back. Does nothing if no warp was used.
            try { game.RemoveCodeHook(); } catch { }
        }
    }
}
