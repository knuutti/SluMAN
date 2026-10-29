using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Sly 1 Practice Mode: load a level at an entrance, reload, set or hold coins, lives and charms,
    /// and skip cutscenes, with the player (position editor), world states, powerups, the camera
    /// (free camera and camera paths) and the memory tools on tabs. The controls are built in
    /// code, in the other games' two-column layout.
    /// </summary>
    public class Sly1Practice : Form, IPositionEditorHost
    {
        public sly1 game;
        public string gameNameId = sly1.addr.GameId;

        private const string PlayerTab = "Player";
        private const string WorldStatesTab = "World States";
        private const string PowerupsTab = "Powerups";
        private const string CameraTab = "Camera";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";
        private const string LevelStart = "Level start";
        private const string EntranceFile = "data/s1_entrances.txt";
        // In the user data folder: every entrance read from the game, so a level's entrances
        // can be loaded from anywhere once it has been visited.
        private const string SeenEntranceFile = "s1_seen_entrances.txt";

        // The status refreshes every second; on a PS3 every read is a network round trip.
        private const int RefreshIntervalMs = 1000;

        private readonly ComboBox slotComboBox = new ComboBox();
        private readonly ComboBox levelComboBox = new ComboBox();
        private readonly ComboBox entranceComboBox = new ComboBox();
        private readonly TextBox coinsTextBox = new TextBox();
        private readonly TextBox livesTextBox = new TextBox();
        private readonly ComboBox charmsComboBox = new ComboBox();
        private readonly CheckBox freezeLivesCheckBox = new CheckBox();
        private readonly CheckBox freezeCharmsCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly CheckBox flyModeCheckBox = new CheckBox();
        private readonly CheckBox infiniteJumpCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label livesValueLabel = new Label();
        private readonly Label charmsValueLabel = new Label();
        private readonly Label coinsValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        private PracticeLayout layout;
        private ToolStripMenuItem toolsMenu;
        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        // The free camera, kept running whichever tab shows.
        private Sly1FreeCamera freeCamera;
        /// <summary>The local HTTP API scripts drive the free camera with (Camera tab).</summary>
        private CameraScriptApi cameraApi;
        private SpawnPoints namedEntrances;
        private SpawnPoints seenEntrances;

        // The current level when the entrances were last read; they're read again when it changes.
        private int entrancesLevel = -2;
        private List<int> currentEntrances = new List<int>();

        private class EntranceEntry
        {
            public int id;
            public string name;

            public override string ToString()
            {
                return string.IsNullOrEmpty(name) ? $"Entrance {id}" : $"{name} ({id})";
            }
        }

        public Sly1Practice(sly1 game)
        {
            this.game = game;
            PracticeLayout.SetUpWindow(this, "Sly1Practice", "SluMAN :: Sly Cooper and the Thievius Raccoonus (Practice Mode)");
            layout = new PracticeLayout(this);
            BuildControls();
            statusLine = new StatusLine(this, true);
            // Before the session's own handler, which disconnects: this one still writes to the game.
            FormClosing += Sly1Practice_FormClosing;

            namedEntrances = new SpawnPoints(EntranceFile);
            seenEntrances = new SpawnPoints(UserData.Path(SeenEntranceFile));
            foreach (sly1.LevelEntry level in sly1.GetLevels())
            {
                levelComboBox.Items.Add(level);
            }
            levelComboBox.SelectedIndexChanged += (sender, e) => FillEntrances();
            levelComboBox.SelectedIndex = 0;

            // The controller feeds the combos and the input display.
            game.SetupInputDisplayMemorySubs();
            game.combosActive = true;
            game.InputsTimer.Start();

            game.SetupWebManPopUp();

            session = new GameSession(this, game, gameNameId, "Sly 1", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;
            session.FillPracticeToolsMenu(toolsMenu, () => practiceTabs.Show(MemoryTab));

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();
            RefreshStatus();
            SelectCurrentLevel();

            freeCamera = new Sly1FreeCamera(game);
            freeCamera.Stopped += (message) =>
            {
                if (!IsDisposed)
                {
                    statusLine.Error($"Free camera stopped: {message}");
                }
            };

            cameraApi = new CameraScriptApi(game.GetCameraTabLayout(freeCamera));
            cameraApi.StartFromConfig();

            // Every tab is the Practice tab's width; the height follows the selected tab's content.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            // Every game's tabs go Practice, Player, Camera, the game's own, then the memory tools.
            practiceTabs.AddTool(PlayerTab, () => new PositionEditor(game, game.GetPositionEditorLayout(), null, this));
            practiceTabs.AddTool(CameraTab, () => new CameraForm(cameraApi.Layout));
            practiceTabs.AddTool(WorldStatesTab, () => new Sly1WorldStatesForm(game, statusLine));
            practiceTabs.AddTool(PowerupsTab, () => new Sly1PowerupsForm(game, statusLine));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly1_visible_tabs", PlayerTab, CameraTab, WorldStatesTab, PowerupsTab);
        }

        /// <summary>The Practice tab, in the slots every game shares (PracticeLayout).</summary>
        private void BuildControls()
        {
            toolsMenu = layout.AddMenu();

            // Left column
            layout.AddPickerGroup("Level", levelComboBox, entranceComboBox, (sender, e) => LoadSelectedLevel());
            layout.AddReloadGroup(new Button[]
            {
                PracticeLayout.MakeButton("Reload Level", (sender, e) => Run("Reloading.", () =>
                {
                    ClearPositionFreezes();
                    game.ReloadLevel();
                })),
                PracticeLayout.MakeButton("Skip Cinematic", (sender, e) => Run(null, game.SkipCinematic)),
            }, null);
            layout.AddSavedPositionsGroup(game, slotComboBox,
                (sender, e) => Run($"Saved position to slot {game.selectedPositionIndex + 1}.", game.SavePosition),
                (sender, e) => Run(null, game.LoadPosition));

            // Right column
            layout.AddStatusGroup(new string[] { "Level", "Lives", "Charms", "Coins" },
                new Label[] { levelValueLabel, livesValueLabel, charmsValueLabel, coinsValueLabel });
            charmsComboBox.Items.AddRange(new object[] { "None", "Blue", "Gold" });
            layout.AddUtilityGroup(
                PracticeLayout.Setter("Coins", coinsTextBox, (sender, e) => SetFromTextBox(coinsTextBox, "coins", game.SetCoinCount)),
                PracticeLayout.Setter("Lives", livesTextBox, (sender, e) => SetFromTextBox(livesTextBox, "lives", game.SetLives)),
                PracticeLayout.Setter("Charms", charmsComboBox, (sender, e) => Run(null, () => game.SetCharms(charmsComboBox.SelectedIndex))));
            charmsComboBox.SelectedIndex = 2;
            // Every game's toggles in the same order; Sly 1's own after the shared ones, Always On
            // Top last. Sly 1's lives and charms work unlike Sly 2-4's health, so they keep their names.
            layout.AddTogglesGroup(
                layout.PlaceholderToggle("Freeze Game Clock"),
                PracticeLayout.Toggle(flyModeCheckBox, "Fly Mode", (sender, e) => StartPlayerTool(flyModeCheckBox)),
                PracticeLayout.Toggle(infiniteJumpCheckBox, "Infinite Jump", (sender, e) => StartPlayerTool(infiniteJumpCheckBox)),
                layout.PlaceholderToggle("Auto-skip Cutscenes"),
                PracticeLayout.Toggle(freezeLivesCheckBox, "Freeze Lives",
                    (sender, e) => Toggle(freezeLivesCheckBox, () => game.SetLivesFrozen(freezeLivesCheckBox.Checked))),
                PracticeLayout.Toggle(freezeCharmsCheckBox, "Freeze Gold Charm",
                    (sender, e) => Toggle(freezeCharmsCheckBox, () => game.SetCharmsFrozen(freezeCharmsCheckBox.Checked))),
                PracticeLayout.Toggle(alwaysOnTopCheckBox, "Always On Top", null));

            ToolTip tip = layout.ToolTip;
            tip.SetToolTip(entranceComboBox,
                "Entrances are read from the game when you're in a level, and remembered, so a level's\n" +
                "entrances can be loaded from anywhere once you've visited it. Names for them come\n" +
                "from data/s1_entrances.txt (level | entrance ID | name).");
            tip.SetToolTip(freezeCharmsCheckBox, "Holds a gold charm, so hits don't cost a life.");
            tip.SetToolTip(flyModeCheckBox, "Holds Sly's height: R1 rises, L1 sinks, R2 speeds up horizontal movement.");
            tip.SetToolTip(infiniteJumpCheckBox, "Sly can double jump again and again.");
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

        private void SetFromTextBox(TextBox textBox, string what, Action<int> set)
        {
            int value;
            if (!int.TryParse(textBox.Text.Trim(), out value))
            {
                statusLine.Error($"Enter a number of {what}.");
                return;
            }
            Run(null, () => set(value));
        }

        private void LoadSelectedLevel()
        {
            sly1.LevelEntry level = levelComboBox.SelectedItem as sly1.LevelEntry;
            if (level == null)
            {
                statusLine.Error("Select a level to load first.");
                return;
            }
            EntranceEntry entrance = entranceComboBox.SelectedItem as EntranceEntry;
            Run($"Loading {level.name}.", () =>
            {
                ClearPositionFreezes();
                game.LoadLevel(level.index, entrance == null ? (int?)null : entrance.id);
            });
        }

        public bool FlyModeEnabled
        {
            get { return flyModeCheckBox.Checked; }
        }

        public bool InfiniteJumpEnabled
        {
            get { return infiniteJumpCheckBox.Checked; }
        }

        /// <summary>
        /// Fly Mode and Infinite Jump run in the Player tab's Position Editor, so it's created
        /// (without switching to its tab) when either is turned on.
        /// </summary>
        private void StartPlayerTool(CheckBox toggle)
        {
            if (toggle.Checked)
            {
                practiceTabs.Create(PlayerTab);
            }
        }

        /// <summary>
        /// Drops the Player tab's position freezes before a load: they hold coordinates from the
        /// old level, and writing them to the player while the level loads can crash the game.
        /// </summary>
        private void ClearPositionFreezes()
        {
            PositionEditor positionEditor = practiceTabs.Get(PlayerTab) as PositionEditor;
            if (positionEditor != null)
            {
                positionEditor.ClearPositionFreezes();
                // Fly Mode takes the new level's height.
                positionEditor.ResetFlyHeight();
            }
        }

        /// <summary>
        /// Lists the selected level's entrances: named ones from the entrance file, and, for the
        /// loaded level, every entrance the game has.
        /// </summary>
        private void FillEntrances()
        {
            entranceComboBox.Items.Clear();
            entranceComboBox.Items.Add(LevelStart);
            sly1.LevelEntry level = levelComboBox.SelectedItem as sly1.LevelEntry;
            if (level != null)
            {
                Dictionary<int, string> names = new Dictionary<int, string>();
                foreach (SpawnPoints.SpawnPoint named in namedEntrances.ForMap(sly1.LevelTitle(level.index)))
                {
                    names[(int)named.id] = named.name;
                }

                List<int> ids = new List<int>(names.Keys);
                foreach (SpawnPoints.SpawnPoint seen in seenEntrances.ForMap(sly1.LevelTitle(level.index)))
                {
                    if (!ids.Contains((int)seen.id))
                    {
                        ids.Add((int)seen.id);
                    }
                }
                if (level.index == entrancesLevel)
                {
                    foreach (int id in currentEntrances)
                    {
                        if (!ids.Contains(id))
                        {
                            ids.Add(id);
                        }
                    }
                }
                ids.Sort();
                foreach (int id in ids)
                {
                    string name;
                    names.TryGetValue(id, out name);
                    entranceComboBox.Items.Add(new EntranceEntry { id = id, name = name });
                }
            }
            entranceComboBox.SelectedIndex = 0;
            entranceComboBox.Enabled = entranceComboBox.Items.Count > 1;
        }

        /// <summary>Adds the level's entrances that aren't in the seen entrance file yet to it.</summary>
        private void RememberEntrances(int level, List<int> entrances)
        {
            if (level < 0 || entrances.Count == 0)
            {
                return;
            }
            string title = sly1.LevelTitle(level);
            List<uint> known = new List<uint>();
            foreach (SpawnPoints.SpawnPoint seen in seenEntrances.ForMap(title))
            {
                known.Add(seen.id);
            }
            List<string> lines = new List<string>();
            foreach (int id in entrances)
            {
                if (id >= 0 && !known.Contains((uint)id))
                {
                    lines.Add($"{title} | {id}");
                }
            }
            if (lines.Count == 0)
            {
                return;
            }

            try
            {
                string path = UserData.Path(SeenEntranceFile);
                if (!File.Exists(path))
                {
                    lines.Insert(0, "# Sly 1 entrances SluMAN has read from the game: level | entrance ID. Written by SluMAN.");
                }
                File.AppendAllLines(path, lines);
                seenEntrances = new SpawnPoints(path);
            }
            catch
            {
                // Not writable: the entrances still show while the level is loaded.
            }
        }

        private void SelectCurrentLevel()
        {
            foreach (sly1.LevelEntry level in levelComboBox.Items)
            {
                if (level.index == entrancesLevel)
                {
                    levelComboBox.SelectedItem = level;
                    return;
                }
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
                int level = game.GetCurrentLevelIndex();
                levelValueLabel.Text = sly1.LevelTitle(level);
                livesValueLabel.Text = game.GetLives().ToString();
                int charms = game.GetCharms();
                charmsValueLabel.Text = charms >= 0 && charms < charmsComboBox.Items.Count
                    ? charmsComboBox.Items[charms].ToString() : charms.ToString();
                coinsValueLabel.Text = game.GetCoins().ToString();

                // A new level: read its entrances once it has them.
                if (level != entrancesLevel || currentEntrances.Count == 0)
                {
                    List<int> entrances = game.GetEntrances();
                    if (entrances.Count > 0 || level != entrancesLevel)
                    {
                        entrancesLevel = level;
                        currentEntrances = entrances;
                        RememberEntrances(level, entrances);
                        sly1.LevelEntry selected = levelComboBox.SelectedItem as sly1.LevelEntry;
                        if (selected != null && selected.index == level)
                        {
                            FillEntrances();
                        }
                    }
                }
            }
            catch
            {
                // Not connected or reconnecting; the next tick tries again.
            }
        }

        private void session_Reconnected()
        {
            game.SetupWebManPopUp();
            // The game reset, so the console dropped the freezes: make them again.
            game.ForgetFreezes();
            if (freezeLivesCheckBox.Checked)
            {
                Toggle(freezeLivesCheckBox, () => game.SetLivesFrozen(true));
            }
            if (freezeCharmsCheckBox.Checked)
            {
                Toggle(freezeCharmsCheckBox, () => game.SetCharmsFrozen(true));
            }
        }

        private void Sly1Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop();
            try
            {
                game.SetLivesFrozen(false);
                game.SetCharmsFrozen(false);
            }
            catch
            {
                // The connection may already be closing.
            }
            practiceTabs.CloseAll();
            cameraApi.Stop();
            freeCamera.Stop();
        }
    }
}
