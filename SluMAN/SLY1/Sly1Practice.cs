using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Sly 1 Practice Mode: load a level at an entrance, reload, set or hold coins, lives and charms,
    /// and skip cutscenes, with the memory tools on tabs. The controls are built in code, in the
    /// other games' two-column layout.
    /// </summary>
    public class Sly1Practice : Form
    {
        public sly1 game;
        public string gameNameId = sly1.addr.GameId;

        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";
        private const string LevelStart = "Level start";
        private const string EntranceFile = "data/s1_entrances.txt";

        // The status refreshes every second; on a PS3 every read is a network round trip.
        private const int RefreshIntervalMs = 1000;

        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly ComboBox levelComboBox = new ComboBox();
        private readonly ComboBox entranceComboBox = new ComboBox();
        private readonly TextBox coinsTextBox = new TextBox();
        private readonly TextBox livesTextBox = new TextBox();
        private readonly ComboBox charmsComboBox = new ComboBox();
        private readonly CheckBox freezeLivesCheckBox = new CheckBox();
        private readonly CheckBox freezeCharmsCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Label levelValueLabel = new Label();
        private readonly Label livesValueLabel = new Label();
        private readonly Label charmsValueLabel = new Label();
        private readonly Label coinsValueLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        private StatusLine statusLine;
        private GameSession session;
        private PracticeTabs practiceTabs;
        private SpawnPoints namedEntrances;

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
            Name = "Sly1Practice";
            Text = "SluMAN :: Sly Cooper and the Thievius Raccoonus (Practice Mode)";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft Sans Serif", 8.25F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            BuildMenu();
            BuildControls();
            ClientSize = new Size(660, 554);
            statusLine = new StatusLine(this, true);

            namedEntrances = new SpawnPoints(EntranceFile);
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

            session = new GameSession(this, game, gameNameId, "Sly 1", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += (sender, e) => RefreshStatus();
            refreshTimer.Start();
            RefreshStatus();
            SelectCurrentLevel();

            FormClosing += Sly1Practice_FormClosing;

            // Every tab is the Practice tab's size, so the window doesn't jump between tabs.
            practiceTabs = new PracticeTabs(this, "Practice", true);
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            // View menu: which tabs show, saved per user. Practice always shows.
            practiceTabs.AddViewMenu("sly1_visible_tabs");
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
            GroupBox levelGroupBox = AddGroup("Level", 12, 32, 80);
            levelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            levelComboBox.Location = new Point(10, 20);
            levelComboBox.Size = new Size(292, 21);
            levelGroupBox.Controls.Add(levelComboBox);
            entranceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            entranceComboBox.Location = new Point(10, 49);
            entranceComboBox.Size = new Size(193, 21);
            levelGroupBox.Controls.Add(entranceComboBox);
            AddButton(levelGroupBox, "Load", 208, 48, 94, (sender, e) => LoadSelectedLevel());

            GroupBox reloadGroupBox = AddGroup("Reload", 12, 118, 54);
            AddButton(reloadGroupBox, "Reload Level", 10, 19, 143, (sender, e) => Run("Reloading.", game.ReloadLevel));
            AddButton(reloadGroupBox, "Skip Cinematic", 159, 19, 143, (sender, e) => Run(null, game.SkipCinematic));

            GroupBox utilityGroupBox = AddGroup("Utility", 12, 178, 110);
            AddLabel(utilityGroupBox, "Coins:", 10, 23);
            coinsTextBox.Location = new Point(60, 20);
            coinsTextBox.Size = new Size(143, 20);
            utilityGroupBox.Controls.Add(coinsTextBox);
            AddButton(utilityGroupBox, "Set Coins", 208, 18, 94, (sender, e) => SetFromTextBox(coinsTextBox, "coins", game.SetCoinCount));
            AddLabel(utilityGroupBox, "Lives:", 10, 52);
            livesTextBox.Location = new Point(60, 49);
            livesTextBox.Size = new Size(143, 20);
            utilityGroupBox.Controls.Add(livesTextBox);
            AddButton(utilityGroupBox, "Set Lives", 208, 47, 94, (sender, e) => SetFromTextBox(livesTextBox, "lives", game.SetLives));
            AddLabel(utilityGroupBox, "Charms:", 10, 81);
            charmsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            charmsComboBox.Items.AddRange(new object[] { "None", "Blue", "Gold" });
            charmsComboBox.SelectedIndex = 2;
            charmsComboBox.Location = new Point(60, 78);
            charmsComboBox.Size = new Size(143, 21);
            utilityGroupBox.Controls.Add(charmsComboBox);
            AddButton(utilityGroupBox, "Set Charms", 208, 76, 94,
                (sender, e) => Run(null, () => game.SetCharms(charmsComboBox.SelectedIndex)));

            // Right column
            GroupBox togglesGroupBox = AddGroup("Toggles", 336, 32, 92);
            AddCheckBox(togglesGroupBox, freezeLivesCheckBox, "Freeze Lives", 10, 20,
                (sender, e) => Toggle(freezeLivesCheckBox, () => game.SetLivesFrozen(freezeLivesCheckBox.Checked)));
            AddCheckBox(togglesGroupBox, freezeCharmsCheckBox, "Freeze Gold Charm", 10, 43,
                (sender, e) => Toggle(freezeCharmsCheckBox, () => game.SetCharmsFrozen(freezeCharmsCheckBox.Checked)));
            AddCheckBox(togglesGroupBox, alwaysOnTopCheckBox, "Always On Top", 10, 66, null);

            GroupBox statusGroupBox = AddGroup("Game", 336, 130, 112);
            AddLabel(statusGroupBox, "Level:", 10, 20);
            AddValueLabel(statusGroupBox, levelValueLabel, 70, 20);
            AddLabel(statusGroupBox, "Lives:", 10, 42);
            AddValueLabel(statusGroupBox, livesValueLabel, 70, 42);
            AddLabel(statusGroupBox, "Charms:", 10, 64);
            AddValueLabel(statusGroupBox, charmsValueLabel, 70, 64);
            AddLabel(statusGroupBox, "Coins:", 10, 86);
            AddValueLabel(statusGroupBox, coinsValueLabel, 70, 86);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(entranceComboBox,
                "Entrances of the loaded level are read from the game. Names for them, and entrances of\n" +
                "other levels, come from data/s1_entrances.txt (level | entrance ID | name).");
            tip.SetToolTip(freezeCharmsCheckBox, "Holds a gold charm, so hits don't cost a life.");
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

        private static void AddCheckBox(Control parent, CheckBox checkBox, string text, int x, int y, EventHandler changed)
        {
            checkBox.Text = text;
            checkBox.AutoSize = true;
            checkBox.Location = new Point(x, y);
            if (changed != null)
            {
                checkBox.CheckedChanged += changed;
            }
            parent.Controls.Add(checkBox);
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
            Run($"Loading {level.name}.", () => game.LoadLevel(level.index, entrance == null ? (int?)null : entrance.id));
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
        }
    }
}
