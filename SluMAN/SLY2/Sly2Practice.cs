using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Sly 2 Practice Mode: load a map at a spawn point or a job at a checkpoint, reload, saved
    /// positions, set coins and health, and the toggles, with the player (position editor), the
    /// camera, gadgets, run files and the memory tools on tabs. The controls are built in code
    /// (PracticeLayout), in the layout every game shares.
    /// </summary>
    public class Sly2Practice : Form
    {
        public Form GadgetsWindow;
        private PositionEditor positionEditorWindow;

        // One window: the Practice controls on the first tab, the tools on the others.
        private PracticeTabs practiceTabs;
        private const string PositionTab = "Player";
        private const string GadgetsTab = "Gadgets";
        private const string RunFilesTab = "Run Files";
        private const string CameraTab = "Camera";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";
        public sly2 game;
        public string gameNameId;

        // The Game group refreshes every second; on a PS3 every read is a network round trip.
        private const int RefreshIntervalMs = 1000;

        private readonly ComboBox mapComboBox = new ComboBox();
        private readonly ComboBox spawnComboBox = new ComboBox();
        private readonly ComboBox jobComboBox = new ComboBox();
        private readonly ComboBox checkpointComboBox = new ComboBox();
        private readonly ComboBox reloadAsCharacterComboBox = new ComboBox();
        private readonly ComboBox slotComboBox = new ComboBox();
        private readonly TextBox coinsTextBox = new TextBox();
        private readonly TextBox healthTextBox = new TextBox();
        private readonly CheckBox invulnerabilityCheckBox = new CheckBox();
        private readonly CheckBox infiniteHealthCheckBox = new CheckBox();
        private readonly CheckBox infiniteGadgetPowerCheckBox = new CheckBox();
        private readonly CheckBox gameClockCheckBox = new CheckBox();
        private readonly CheckBox infiniteJumpCheckBox = new CheckBox();
        private readonly CheckBox autoSkipCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label mapValueLabel = new Label();
        private readonly Label coinsValueLabel = new Label();
        private readonly Label healthValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        private PracticeLayout layout;
        private ToolStripMenuItem toolsMenu;
        private GroupBox statusGroupBox;
        private StatusLine statusLine;
        private JobPicker jobPicker;
        private MapPicker mapPicker;
        private GameSession session;
        private Sly2FreeCamera freeCamera;

        public Sly2Practice(sly2 game)
        {
            this.game = game;
            PracticeLayout.SetUpWindow(this, "Sly2Practice", "SluMAN :: Sly 2: Band of Thieves (Practice Mode)");
            layout = new PracticeLayout(this);
            BuildControls();
            // The picker splits the flat job list into jobs and checkpoints.
            jobPicker = new JobPicker(jobComboBox, checkpointComboBox, Sly2Jobs.All.ToList());
            statusLine = new StatusLine(this, true);
            // Before the session's own handler, which disconnects: this one still writes to the game.
            FormClosing += Sly2Practice_FormClosing;

            mapPicker = new MapPicker(mapComboBox, spawnComboBox, game.GetMapNames(),
                game.maps.Select(m => m.indicator).ToArray(), game.maps.Select(m => m.defaultWarp).ToArray(),
                new SpawnPoints("data/s2_spawn_points.txt"), UserData.Path("s2_seen_spawn_points.txt"));

            gameNameId = sly2.addr.GameId;

            game.SetupInputDisplayMemorySubs();
            game.SetupWebManPopUp();

            game.CheckRunFileConfig();

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Sly 2", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            session.FillPracticeToolsMenu(toolsMenu, () => practiceTabs.Show(MemoryTab));
            // Only the PAL versions have more than one language.
            if (sly2.addr.language != 0)
            {
                GameLanguages.AddMenu(toolsMenu, GameLanguages.Sly2, statusLine, game.GetLanguage, game.SetLanguage);
            }
            AddCopyEpisodeStateMenu();

            reapplyTimer.Interval = ReapplyDelayMs;
            reapplyTimer.Tick += reapplyTimer_Tick;
            game.LoadFinished += game_LoadFinished;
            game.LoadStarted += game_LoadStarted;
            game.SetupLoadWatcher();

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();

            freeCamera = new Sly2FreeCamera(game);
            freeCamera.Stopped += freeCamera_Stopped;
            freeCamera.Notice += freeCamera_Notice;

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools.
            practiceTabs.AddTool(PositionTab, () => new PositionEditor(game, game.GetPositionEditorLayout(), game.GetMapDisplayName));
            practiceTabs.AddTool(CameraTab, () => new CameraForm(game.GetCameraTabLayout(freeCamera)));
            practiceTabs.AddTool(GadgetsTab, () => new Sly2Gadgets(game));
            practiceTabs.AddTool(RunFilesTab, () => new Sly2RunFilesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly2_visible_tabs", PositionTab, CameraTab, GadgetsTab, RunFilesTab);
            practiceTabs.ToolCreated += practiceTabs_ToolCreated;
            practiceTabs.ToolClosed += practiceTabs_ToolClosed;
            RefreshStatus();
        }

        /// <summary>The Practice tab, in the slots every game shares (PracticeLayout).</summary>
        private void BuildControls()
        {
            toolsMenu = layout.AddMenu();

            // Left column
            layout.AddPickerGroup("Map", mapComboBox, spawnComboBox, (sender, e) => Run(null, () => game.LoadMap(mapComboBox.SelectedIndex, mapPicker.SelectedSpawn)));
            layout.AddPickerGroup("Job", jobComboBox, checkpointComboBox, (sender, e) => LoadSelectedJob());
            reloadAsCharacterComboBox.Items.AddRange(new object[] { "Default", "Sly", "Bentley", "Murray" });
            reloadAsCharacterComboBox.SelectedIndex = 0;
            layout.AddReloadGroup(new Button[]
            {
                PracticeLayout.MakeButton("Reload", (sender, e) => Run(null, Reload)),
                PracticeLayout.MakeButton("Kill", (sender, e) => Run(null, Kill)),
                PracticeLayout.MakeButton("Skip Cinematic", (sender, e) => Run(null, game.SkipCinematic)),
            }, reloadAsCharacterComboBox);
            layout.AddSavedPositionsGroup(game, slotComboBox,
                (sender, e) => Run($"Saved position to slot {game.selectedPositionIndex + 1}.", game.SavePosition),
                (sender, e) => Run(null, game.LoadPosition));

            // Right column
            statusGroupBox = layout.AddStatusGroup(new string[] { "Map", "Coins", "Health" },
                new Label[] { mapValueLabel, coinsValueLabel, healthValueLabel });
            layout.AddUtilityGroup(
                PracticeLayout.Setter("Coins", coinsTextBox, (sender, e) => SetFromTextBox(coinsTextBox, "Coins", game.SetCoinCount)),
                PracticeLayout.Setter("Health", healthTextBox, (sender, e) => SetFromTextBox(healthTextBox, "Health", game.SetHealth)));
            // Every game's toggles in the same order, Always On Top last.
            layout.AddTogglesGroup(
                PracticeLayout.Toggle(invulnerabilityCheckBox, "Invulnerability",
                    (sender, e) => ApplyToggle(invulnerabilityCheckBox, () => game.SetInvulnerability(invulnerabilityCheckBox.Checked))),
                PracticeLayout.Toggle(infiniteHealthCheckBox, "Infinite Health",
                    (sender, e) => ApplyToggle(infiniteHealthCheckBox, () => game.SetInfiniteHealth(infiniteHealthCheckBox.Checked))),
                PracticeLayout.Toggle(infiniteGadgetPowerCheckBox, "Infinite Gadget Power",
                    (sender, e) => ApplyToggle(infiniteGadgetPowerCheckBox, () => game.SetInfiniteGadgetPower(infiniteGadgetPowerCheckBox.Checked))),
                PracticeLayout.Toggle(gameClockCheckBox, "Freeze Game Clock",
                    (sender, e) => ApplyToggle(gameClockCheckBox, () => game.SetGameClockFrozen(gameClockCheckBox.Checked))),
                layout.PlaceholderToggle("Fly Mode"),
                PracticeLayout.Toggle(infiniteJumpCheckBox, "Infinite Jump",
                    (sender, e) => ApplyToggle(infiniteJumpCheckBox, () => game.SetInfiniteJump(infiniteJumpCheckBox.Checked))),
                PracticeLayout.Toggle(autoSkipCheckBox, "Auto-skip Cutscenes",
                    (sender, e) => ApplyToggle(autoSkipCheckBox, () => game.SetAutoSkip(autoSkipCheckBox.Checked))),
                PracticeLayout.Toggle(alwaysOnTopCheckBox, "Always On Top", null));
        }

        /// <summary>
        /// Tools -> Copy Episode State: copies an episode's save data as it is now, in
        /// data/s2_job_config.txt's format, to record a job state or compare one.
        /// </summary>
        private void AddCopyEpisodeStateMenu()
        {
            ToolStripMenuItem copyMenu = new ToolStripMenuItem("Copy Episode State");
            for (int episode = 0; episode < sly2.EpisodeStateSizes.Length; episode++)
            {
                int current = episode;
                copyMenu.DropDownItems.Add(episode == 0 ? "Prologue" : $"Episode {episode}", null, (sender, e) =>
                {
                    try
                    {
                        Clipboard.SetText(game.ReadEpisodeState(current));
                        statusLine.Info($"Copied episode {current}'s state ({sly2.EpisodeStateSizes[current]} bytes from {sly2.addr.episodeData[current]:X}).");
                    }
                    catch (Exception ex)
                    {
                        statusLine.Error($"Couldn't copy the episode state: {ex.Message}");
                    }
                });
            }

            GameSession.AddGameMenuItem(toolsMenu, copyMenu);
        }

        private void Run(string done, Action action)
        {
            try
            {
                action();
                if (done != null)
                {
                    statusLine.Info(done);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error(ex.Message);
            }
        }

        private void SetFromTextBox(TextBox textBox, string what, Action<int> set)
        {
            int value;
            if (!int.TryParse(textBox.Text.Trim(), out value))
            {
                statusLine.Error($"{what} must be a whole number.");
                return;
            }
            Run(null, () => set(value));
        }

        private void ApplyToggle(CheckBox toggle, Action apply)
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

        /// <summary>The character the Reload group's list picks (Sly 7, Bentley 8, Murray 9), or -1 for the default.</summary>
        private int SelectedCharacter()
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 1: return 7;
                case 2: return 8;
                case 3: return 9;
                default: return -1;
            }
        }

        private void Reload()
        {
            int character = SelectedCharacter();
            if (character >= 0)
            {
                game.SetActiveCharacter(character);
            }
            game.TriggerGameLoad((uint)0);
        }

        private void Kill()
        {
            int character = SelectedCharacter();
            if (character >= 0)
            {
                game.SetActiveCharacter(character);
            }
            game.KillActiveCharacter();
        }

        private void LoadSelectedJob()
        {
            string entry = jobPicker.SelectedEntry;
            if (entry == null)
            {
                statusLine.Error("Select a job to load first.");
                return;
            }
            Run(null, () => game.LoadJob(entry));
        }

        private void RefreshStatus()
        {
            // Only read while the Practice tab shows.
            if (IsDisposed || !statusGroupBox.Visible)
            {
                return;
            }
            try
            {
                mapValueLabel.Text = game.GetMapDisplayName(game.GetMapIndicator());
                coinsValueLabel.Text = game.GetCoins().ToString();
                healthValueLabel.Text = game.GetHealth().ToString();
            }
            catch
            {
                // Not connected or reconnecting; the next tick tries again.
            }
        }

        private void game_LoadStarted()
        {
            if (freeCamera != null)
            {
                freeCamera.Stop();
            }
            // Raised on the subscription thread. Position freezes hold coordinates from the old
            // map, so drop them as soon as the load begins rather than writing them all through it.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (positionEditorWindow != null && !positionEditorWindow.IsDisposed)
                    {
                        positionEditorWindow.ClearPositionFreezes();
                    }
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        // How long to wait after a load finishes before re-applying, so the game has set up the
        // player entity.
        private const int ReapplyDelayMs = 500;
        /// <summary>
        /// Adds the spawn point the load arrived at to the Spawn point dropdown's remembered ones,
        /// so it can be loaded from then on.
        /// </summary>
        private void RememberSpawnPoint()
        {
            try
            {
                mapPicker.Remember(game.GetMapIndicator(), game.GetSpawnLocation());
            }
            catch
            {
                // Not readable right now; the next load tries again.
            }
        }

        // Retries while the player entity doesn't exist yet: 10 x 500 ms.
        private const int MaxReapplyAttempts = 10;
        private readonly Timer reapplyTimer = new Timer();
        private int reapplyAttempts = 0;

        private void game_LoadFinished()
        {
            // Raised on the subscription thread.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    // The load may have been another save, so an open tool such as the gadgets
                    // reads it again. Save data doesn't wait for the player like the toggles do.
                    practiceTabs.RefreshShownTool();
                    RememberSpawnPoint();

                    reapplyAttempts = 0;
                    reapplyTimer.Stop();
                    reapplyTimer.Start();
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        private void reapplyTimer_Tick(object sender, EventArgs e)
        {
            reapplyTimer.Stop();

            if (!game.IsPlayerLoaded())
            {
                reapplyAttempts++;
                if (reapplyAttempts < MaxReapplyAttempts)
                {
                    reapplyTimer.Start();
                }
                else
                {
                    reapplyAttempts = 0;
                    Console.WriteLine("Load finished, but the player didn't appear; toggles not re-applied.");
                }
                return;
            }

            reapplyAttempts = 0;
            ReapplyToggles();
        }

        /// <summary>
        /// Applies the checked game toggles again after a load. A load can move the player entity,
        /// so a freeze set up before it may now point at the wrong address.
        /// </summary>
        private void ReapplyToggles()
        {
            List<string> applied = new List<string>();
            Reapply(invulnerabilityCheckBox, () => game.SetInvulnerability(true), applied);
            Reapply(infiniteHealthCheckBox, () => game.SetInfiniteHealth(true), applied);
            Reapply(infiniteGadgetPowerCheckBox, () => game.SetInfiniteGadgetPower(true), applied);
            Reapply(gameClockCheckBox, () => game.SetGameClockFrozen(true), applied);
            Reapply(infiniteJumpCheckBox, () => game.SetInfiniteJump(true), applied);

            if (applied.Count > 0)
            {
                Console.WriteLine($"Load finished, re-applied: {string.Join(", ", applied)}");
            }
        }

        private void Reapply(CheckBox toggle, Action apply, List<string> applied)
        {
            if (!toggle.Checked || !toggle.Enabled)
            {
                return;
            }
            try
            {
                apply();
                applied.Add(toggle.Text);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't re-apply {toggle.Text} after load: {ex.Message}");
            }
        }

        private void Sly2Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop();
            if (freeCamera != null)
            {
                freeCamera.Stopped -= freeCamera_Stopped;
                freeCamera.Notice -= freeCamera_Notice;
                freeCamera.Stop();
            }
            // The tools' own cleanup (subscriptions, timers) runs when they close.
            practiceTabs.CloseAll();
            game.LoadFinished -= game_LoadFinished;
            game.LoadStarted -= game_LoadStarted;
            reapplyTimer.Stop();
            game.SetAutoSkip(false);
            // The session closes the other windows and disconnects after this.
        }

        private void freeCamera_Notice(string message, bool isError)
        {
            // Raised on the camera's thread; the status line takes messages from any thread.
            if (!IsDisposed)
            {
                statusLine.Show(message, isError);
            }
        }

        private void freeCamera_Stopped(string message)
        {
            if (!IsDisposed)
            {
                statusLine.Error($"Free camera stopped: {message}");
            }
        }

        private void session_Reconnected()
        {
            game.SetupWebManPopUp();
            game.SetupLoadWatcher();
            // The game reset dropped the auto-skipper's subscriptions.
            if (autoSkipCheckBox.Checked)
            {
                ApplyToggle(autoSkipCheckBox, () => game.RestartAutoSkip());
            }
        }

        private void practiceTabs_ToolCreated(string title, Form form)
        {
            if (title == GadgetsTab)
            {
                GadgetsWindow = form;
                session.AddGameWindow(form);
            }
            else if (title == PositionTab)
            {
                positionEditorWindow = (PositionEditor)form;
                session.AddGameWindow(form);
            }
        }

        private void practiceTabs_ToolClosed(string title)
        {
            if (title == GadgetsTab)
            {
                GadgetsWindow = null;
            }
            else if (title == PositionTab)
            {
                positionEditorWindow = null;
            }
        }
    }
}
