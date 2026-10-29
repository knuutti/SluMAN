using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Sly 3 Practice Mode: load a map at a spawn point or a job at a checkpoint, reload, saved
    /// positions, set coins and health, and the toggles, with the player (position editor), the
    /// camera, gadgets, run files and the memory tools on tabs. The controls are built in code
    /// (PracticeLayout), in the layout every game shares.
    /// </summary>
    public class Sly3Practice : Form, IPositionEditorHost
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

        public bool FlyModeEnabled => flyModeCheckBox.Checked;
        public bool InfiniteJumpEnabled => infiniteJumpCheckBox.Checked;
        public sly3 game;
        public string gameNameId;

        private System.Windows.Forms.Timer freezeTimer;

        private float flyFrozenZ;
        private float prevFlyPosX;
        private float prevFlyPosY;
        private bool prevFlyModeState;
        private uint flyEntityPtr;
        private const float FlyHeightStep = 20.0f;
        private const float FlyBoostMultiplier = 8.0f;

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
        private readonly CheckBox flyModeCheckBox = new CheckBox();
        private readonly CheckBox infiniteJumpCheckBox = new CheckBox();
        private readonly CheckBox autoSkipCheckBox = new CheckBox();
        private readonly CheckBox guardAICheckBox = new CheckBox();
        private readonly CheckBox deathBarriersCheckBox = new CheckBox();
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

        /// <summary>The free camera, kept running whichever tab shows.</summary>
        public Sly3FreeCamera FreeCamera;
        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        private CameraScriptApi cameraApi;

        public Sly3Practice(sly3 game)
        {
            this.game = game;
            PracticeLayout.SetUpWindow(this, "Sly3Practice", "SluMAN :: Sly 3: Honor Among Thieves (Practice Mode)");
            layout = new PracticeLayout(this);
            BuildControls();
            // The picker splits the flat job list into jobs and checkpoints.
            jobPicker = new JobPicker(jobComboBox, checkpointComboBox, Sly3Jobs.All.ToList());
            statusLine = new StatusLine(this, true);
            // Before the session's own handler, which disconnects: this one still writes to the game.
            FormClosing += Sly3Practice_FormClosing;

            mapPicker = new MapPicker(mapComboBox, spawnComboBox, game.GetMapNames(),
                game.maps.Select(m => m.indicator).ToArray(), game.maps.Select(m => m.defaultWarp).ToArray(),
                new SpawnPoints("data/s3_spawn_points.txt"), UserData.Path("s3_seen_spawn_points.txt"));

            gameNameId = sly3.addr.GameId;

            game.SetupInputDisplayMemorySubs();
            game.SetupWebManPopUp();

            game.CheckRunFileConfig();

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Sly 3", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            session.FillPracticeToolsMenu(toolsMenu, () => practiceTabs.Show(MemoryTab));
            // Only the PAL versions have more than one language.
            if (sly3.addr.language != 0)
            {
                GameLanguages.AddMenu(toolsMenu, GameLanguages.Sly3, statusLine, game.GetLanguage, game.SetLanguage);
            }

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();

            freezeTimer = new System.Windows.Forms.Timer();
            freezeTimer.Interval = 16;
            freezeTimer.Tick += FreezeTimer_Tick;
            freezeTimer.Start();

            reapplyTimer.Interval = ReapplyDelayMs;
            reapplyTimer.Tick += reapplyTimer_Tick;
            game.LoadFinished += game_LoadFinished;
            game.LoadStarted += game_LoadStarted;
            game.SetupLoadWatcher();

            FreeCamera = new Sly3FreeCamera(game);
            FreeCamera.Stopped += (message) =>
            {
                if (!IsDisposed)
                {
                    statusLine.Error($"Free camera stopped: {message}");
                }
            };

            cameraApi = new CameraScriptApi(game.GetCameraTabLayout(FreeCamera));
            CameraScriptApi.BindClockToggle(cameraApi.Layout, gameClockCheckBox, game.SetGameClockFrozen);
            cameraApi.StartFromConfig();

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools.
            practiceTabs.AddTool(PositionTab, () => new PositionEditor(game, game.GetPositionEditorLayout(), game.GetMapDisplayName, this));
            practiceTabs.AddTool(CameraTab, () => new CameraForm(cameraApi.Layout));
            practiceTabs.AddTool(GadgetsTab, () => new Sly3Gadgets(game));
            practiceTabs.AddTool(RunFilesTab, () => new Sly3RunFilesForm(game));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly3_visible_tabs", PositionTab, CameraTab, GadgetsTab, RunFilesTab);
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
            reloadAsCharacterComboBox.Items.AddRange(new object[] { "Default", "Sly", "Bentley", "Murray", "Carmelita", "Guru", "Panda King", "Penelope" });
            reloadAsCharacterComboBox.SelectedIndex = 0;
            layout.AddReloadGroup(new Button[]
            {
                PracticeLayout.MakeButton("Reload (Fast)", (sender, e) => Run(null, () => Reload(true))),
                PracticeLayout.MakeButton("Reload (Full)", (sender, e) => Run(null, () => Reload(false))),
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
            // Every game's toggles in the same order; Sly 3's own after the shared ones, Always On
            // Top last. Infinite Health and Gadget Power are held by the freeze timer.
            layout.AddTogglesGroup(
                PracticeLayout.Toggle(invulnerabilityCheckBox, "Invulnerability",
                    (sender, e) => ApplyToggle(invulnerabilityCheckBox, () => game.SetInvulnerability(invulnerabilityCheckBox.Checked))),
                PracticeLayout.Toggle(infiniteHealthCheckBox, "Infinite Health", null),
                PracticeLayout.Toggle(infiniteGadgetPowerCheckBox, "Infinite Gadget Power", null),
                layout.PlaceholderToggle("Undetectable"),
                PracticeLayout.Toggle(gameClockCheckBox, "Freeze Game Clock",
                    (sender, e) => ApplyToggle(gameClockCheckBox, () => game.SetGameClockFrozen(gameClockCheckBox.Checked))),
                PracticeLayout.Toggle(flyModeCheckBox, "Fly Mode", null),
                PracticeLayout.Toggle(infiniteJumpCheckBox, "Infinite Jump", null),
                PracticeLayout.Toggle(autoSkipCheckBox, "Auto-skip Cutscenes",
                    (sender, e) => ApplyToggle(autoSkipCheckBox, () => game.SetAutoSkip(autoSkipCheckBox.Checked))),
                PracticeLayout.Toggle(guardAICheckBox, "Disable Guard AI",
                    (sender, e) => ApplyToggle(guardAICheckBox, () => game.SetGuardAI(guardAICheckBox.Checked))),
                PracticeLayout.Toggle(deathBarriersCheckBox, "Disable Death Barriers",
                    (sender, e) => ApplyToggle(deathBarriersCheckBox, () => game.SetDeathBarriers(deathBarriersCheckBox.Checked))),
                PracticeLayout.Toggle(alwaysOnTopCheckBox, "Always On Top", null));

            layout.ToolTip.SetToolTip(flyModeCheckBox, "Holds the character's height: R1 rises, L1 sinks, R2 speeds up horizontal movement.");
        }

        private void game_LoadStarted()
        {
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
                    PositionEditor positionEditor = positionEditorWindow;
                    if (positionEditor != null && !positionEditor.IsDisposed)
                    {
                        positionEditor.ClearPositionFreezes();
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
        private readonly Timer reapplyTimer = new Timer();

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
        private int reapplyAttempts = 0;

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
        /// Applies the checked game toggles again after a load. A load can move the player entity
        /// and other dynamic addresses, so a value written or frozen before it may now be in the
        /// wrong place. Toggles that run every tick (infinite health, gadget power, jump) already
        /// look the address up each time and need nothing here.
        /// </summary>
        private void ReapplyToggles()
        {
            // Fly mode holds the height it had when switched on; take the new map's height instead.
            prevFlyModeState = false;
            PositionEditor positionEditor = positionEditorWindow;
            if (positionEditor != null && !positionEditor.IsDisposed)
            {
                positionEditor.ResetFlyHeight();
            }

            List<string> applied = new List<string>();
            Reapply(invulnerabilityCheckBox, () => game.SetInvulnerability(true), applied);
            Reapply(guardAICheckBox, () => game.SetGuardAI(true), applied);
            Reapply(deathBarriersCheckBox, () => game.SetDeathBarriers(true), applied);
            Reapply(gameClockCheckBox, () => game.SetGameClockFrozen(true), applied);

            if (applied.Count > 0)
            {
                Console.WriteLine($"Load finished, re-applied: {string.Join(", ", applied)}");
            }
        }

        private void Reapply(CheckBox toggle, Action apply, List<string> applied)
        {
            if (!toggle.Checked)
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

        private void FreezeTimer_Tick(object sender, EventArgs e)
        {
            if (infiniteHealthCheckBox.Checked)
            {
                try { game.SetHealth(100); } catch { }
            }
            if (infiniteGadgetPowerCheckBox.Checked)
            {
                try { game.SetGadgetPower(100); } catch { }
            }

            // Fly mode and infinite jump are handled by the Position Editor when it is open
            bool posEditorOpen = positionEditorWindow != null && !positionEditorWindow.IsDisposed;
            if (posEditorOpen)
            {
                prevFlyModeState = false;
                return;
            }

            // During a load (the fade to black included) the player entity is being freed or set
            // up, and writing to it crashes the game.
            if (game.IsLoading)
            {
                prevFlyModeState = false;
                return;
            }

            if (infiniteJumpCheckBox.Checked)
            {
                try
                {
                    byte[] epBytes = game.api.ReadMemory(game.pid, sly3.addr.activeCharacterPtr, 4);
                    uint entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                    if (entityPtr != 0)
                        game.api.WriteMemory(game.pid, entityPtr + 0x338, (uint)0);
                }
                catch { }
            }

            bool flyMode = flyModeCheckBox.Checked;
            if (!flyMode)
            {
                prevFlyModeState = false;
                return;
            }

            try
            {
                byte[] epBytes = game.api.ReadMemory(game.pid, sly3.addr.activeCharacterPtr, 4);
                uint entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                uint transformPtr = 0;
                if (entityPtr != 0)
                {
                    byte[] tpBytes = game.api.ReadMemory(game.pid, entityPtr + sly3.addr.transformOffset, 4);
                    transformPtr = BitConverter.ToUInt32(tpBytes.Reverse().ToArray(), 0);
                }
                // No player, e.g. in a hideout. Take the height again once there is one, rather
                // than writing the old map's height into a new entity.
                if (entityPtr == 0 || transformPtr == 0)
                {
                    prevFlyModeState = false;
                    return;
                }

                if (!prevFlyModeState || entityPtr != flyEntityPtr)
                {
                    flyFrozenZ = ReadFloat(transformPtr + sly3.addr.coordsOffsetZ);
                    prevFlyPosX = ReadFloat(transformPtr + sly3.addr.coordsOffsetX);
                    prevFlyPosY = ReadFloat(transformPtr + sly3.addr.coordsOffsetY);
                    flyEntityPtr = entityPtr;
                    // Not a position yet: the entity is still being set up.
                    if (!IsFinite(flyFrozenZ) || !IsFinite(prevFlyPosX) || !IsFinite(prevFlyPosY))
                    {
                        prevFlyModeState = false;
                        return;
                    }
                }
                prevFlyModeState = true;

                // R1 / L1 up and down, as every game's fly and free camera.
                if (Inputs.IsPressed(Inputs.Buttons.r1)) flyFrozenZ += FlyHeightStep;
                if (Inputs.IsPressed(Inputs.Buttons.l1)) flyFrozenZ -= FlyHeightStep;

                WriteFloat(transformPtr + sly3.addr.coordsOffsetZ, flyFrozenZ);
                WriteFloat(transformPtr + 0x1B8, 0f);  // zero Z velocity

                float curX = ReadFloat(transformPtr + sly3.addr.coordsOffsetX);
                float curY = ReadFloat(transformPtr + sly3.addr.coordsOffsetY);
                if (Inputs.IsPressed(Inputs.Buttons.r2))  // horizontal boost
                {
                    float deltaX = curX - prevFlyPosX;
                    float deltaY = curY - prevFlyPosY;
                    if (Math.Abs(deltaX) > 0.001f || Math.Abs(deltaY) > 0.001f)
                    {
                        float newX = prevFlyPosX + deltaX * FlyBoostMultiplier;
                        float newY = prevFlyPosY + deltaY * FlyBoostMultiplier;
                        WriteFloat(transformPtr + sly3.addr.coordsOffsetX, newX);
                        WriteFloat(transformPtr + sly3.addr.coordsOffsetY, newY);
                        curX = newX;
                        curY = newY;
                    }
                }
                prevFlyPosX = curX;
                prevFlyPosY = curY;
            }
            catch { }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private float ReadFloat(uint address)
        {
            byte[] b = game.api.ReadMemory(game.pid, address, 4);
            return BitConverter.ToSingle(b.Reverse().ToArray(), 0);
        }

        private void WriteFloat(uint address, float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            game.api.WriteMemory(game.pid, address, b);
        }

        private void Sly3Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop();
            // The tools' own cleanup (subscriptions, timers) runs when they close.
            practiceTabs.CloseAll();
            cameraApi.Stop();
            FreeCamera.Stop();
            game.LoadFinished -= game_LoadFinished;
            game.LoadStarted -= game_LoadStarted;
            reapplyTimer.Stop();
            freezeTimer.Stop();
            game.SetAutoSkip(false);
            // The session closes the other windows and disconnects after this.
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

        // The characters the Reload group's list picks, after Default.
        private static readonly int[] ReloadCharacters = { 24, 25, 26, 28, 29, 30, 31 };

        /// <summary>Reloads, as the character the Reload group's list picks; a fast reload keeps the map loaded.</summary>
        private void Reload(bool fast)
        {
            int selected = reloadAsCharacterComboBox.SelectedIndex;
            if (selected >= 1 && selected <= ReloadCharacters.Length)
            {
                game.SetActiveCharacter(ReloadCharacters[selected - 1]);
            }
            if (fast)
            {
                game.TriggerGameLoad((uint)0);
            }
            else
            {
                game.TriggerGameLoad();
            }
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
                int health = game.GetHealth();
                healthValueLabel.Text = health < 0 ? "-" : health.ToString();
            }
            catch
            {
                // Not connected or reconnecting; the next tick tries again.
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
    }
}
