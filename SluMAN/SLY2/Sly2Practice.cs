using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Net.Http;
using System.Threading;
using System.Reflection;

namespace SluMAN
{
    public partial class Sly2Practice : Form
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

        private StatusLine statusLine;
        private JobPicker jobPicker;
        private MapPicker mapPicker;
        private GameSession session;
        private Sly2FreeCamera freeCamera;

        public Sly2Practice(sly2 game)
        {
            this.game = game;
            InitializeComponent();
            // The designer holds the flat job list; the picker splits it into jobs and checkpoints.
            jobPicker = new JobPicker(jobComboBox, checkpointComboBox, jobComboBox.Items.Cast<string>().ToList());
            statusLine = new StatusLine(this, true);
            func.BindEnter(healthTextBox, setHealthButton);

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

            session.FillPracticeToolsMenu(menuToolStripMenuItem, () => practiceTabs.Show(MemoryTab));
            // Only the PAL versions have more than one language.
            if (sly2.addr.language != 0)
            {
                GameLanguages.AddMenu(menuToolStripMenuItem, GameLanguages.Sly2, statusLine, game.GetLanguage, game.SetLanguage);
            }
            AddCopyEpisodeStateMenu();

            reapplyTimer.Interval = ReapplyDelayMs;
            reapplyTimer.Tick += reapplyTimer_Tick;
            game.LoadFinished += game_LoadFinished;
            game.LoadStarted += game_LoadStarted;
            game.SetupLoadWatcher();

            freeCamera = new Sly2FreeCamera(game);
            freeCamera.Stopped += freeCamera_Stopped;
            freeCamera.Notice += freeCamera_Notice;

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools.
            practiceTabs.AddTool(PositionTab, () => new PositionEditor(game, game.GetPositionEditorLayout(), game.GetMapDisplayName));
            practiceTabs.AddTool(CameraTab, () => new Sly2CameraForm(game, freeCamera));
            practiceTabs.AddTool(GadgetsTab, () => new Sly2Gadgets(game));
            practiceTabs.AddTool(RunFilesTab, () => new Sly2RunFilesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly2_visible_tabs", PositionTab, CameraTab, GadgetsTab, RunFilesTab);
            practiceTabs.ToolCreated += practiceTabs_ToolCreated;
            practiceTabs.ToolClosed += practiceTabs_ToolClosed;
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

            GameSession.AddGameMenuItem(menuToolStripMenuItem, copyMenu);
        }

        private void infiniteJumpCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteJumpCheckBox, () => game.SetInfiniteJump(infiniteJumpCheckBox.Checked));
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

        private void invulnerabilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(invulnerabilityCheckBox, () => game.SetInvulnerability(invulnerabilityCheckBox.Checked));
        }

        private void infiniteHealthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteHealthCheckBox, () => game.SetInfiniteHealth(infiniteHealthCheckBox.Checked));
        }

        private void infiniteGadgetPowerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteGadgetPowerCheckBox, () => game.SetInfiniteGadgetPower(infiniteGadgetPowerCheckBox.Checked));
        }

        private void gameClockCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(gameClockCheckBox, () => game.SetGameClockFrozen(gameClockCheckBox.Checked));
        }

        private void autoSkipCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(autoSkipCheckBox, () => game.SetAutoSkip(autoSkipCheckBox.Checked));
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

        private void coinsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SetCoinsFromTextBox();
            }
        }

        private void Sly2Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
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

        private void skipCinematicsButton_Click(object sender, EventArgs e)
        {
            game.SkipCinematic();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 1:
                    game.SetActiveCharacter(7);
                    break;
                case 2:
                    game.SetActiveCharacter(8);
                    break;
                case 3:
                    game.SetActiveCharacter(9);
                    break;
            }
            game.TriggerGameLoad((uint)0);
        }

        private void killButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 0:
                    game.SetActiveCharacter(7);
                    break;
                case 1:
                    game.SetActiveCharacter(8);
                    break;
                case 2:
                    game.SetActiveCharacter(9);
                    break;
            }
            game.KillActiveCharacter();
        }

        private void setCoinsButton_Click(object sender, EventArgs e)
        {
            SetCoinsFromTextBox();
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

        private void SetCoinsFromTextBox()
        {
            var coinsText = coinsTextBox.Text;
            // try to parse the input as an integer
            if (int.TryParse(coinsText, out int coins))
            {
                game.SetCoinCount(coins);
            }
            else
            {
                statusLine.Error("Coins must be a whole number.");
            }
        }

        private void SetHealthFromTextBox()
        {
            var healthText = healthTextBox.Text;
            // try to parse the input as an integer
            if (int.TryParse(healthText, out int health))
            {
                game.SetHealth(health);
            }
            else
            {
                statusLine.Error("Health must be a whole number.");
            }
        }

        private void setHealthButton_Click(object sender, EventArgs e)
        {
            SetHealthFromTextBox();
        }

        private void loadMapButton_Click(object sender, EventArgs e)
        {
            game.LoadMap(mapComboBox.SelectedIndex, mapPicker.SelectedSpawn);
        }

        private void loadJobButton_Click(object sender, EventArgs e)
        {
            string entry = jobPicker.SelectedEntry;
            if (entry == null)
            {
                statusLine.Error("Select a job to load first.");
                return;
            }
            game.LoadJob(entry);
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
