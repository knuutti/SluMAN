using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Sly 1's world states: for each world, which levels are unlocked and have their key, vault
    /// and sprint done, the world's key, vault and sprint counts and its flags. Every change is
    /// written to the game straight away; the game shows it after a reload. The controls are built
    /// in code.
    /// </summary>
    public class Sly1WorldStatesForm : Form, IRefreshOnShow
    {
        private const int ColumnX = 300;
        private const int ColumnWidth = 80;
        private const int RowsY = 46;
        private const int RowHeight = 24;

        // The level save flag each column of checkboxes stands for.
        private static readonly int[] ColumnFlags = new int[]
        {
            sly1.LevelUnlocked, sly1.LevelKey, sly1.LevelVault, sly1.LevelSprint,
        };
        private static readonly string[] ColumnTitles = new string[] { "Unlocked", "Key", "Vault", "Sprint" };

        private readonly sly1 game;
        private readonly StatusLine statusLine;

        private readonly ComboBox worldComboBox = new ComboBox();
        private readonly Label[] levelLabels = new Label[sly1.LevelsPerWorld];
        private readonly CheckBox[,] levelCheckBoxes = new CheckBox[sly1.LevelsPerWorld, 4];
        private readonly CheckBox startedCheckBox = new CheckBox();
        private readonly CheckBox key1CheckBox = new CheckBox();
        private readonly CheckBox key3CheckBox = new CheckBox();
        private readonly CheckBox key7CheckBox = new CheckBox();
        private readonly CheckBox bossCheckBox = new CheckBox();
        private readonly TextBox keysTextBox = new TextBox();
        private readonly TextBox vaultsTextBox = new TextBox();
        private readonly TextBox sprintsTextBox = new TextBox();
        private Label noteLabel;

        public Sly1WorldStatesForm(sly1 game, StatusLine statusLine)
        {
            this.game = game;
            this.statusLine = statusLine;
            Name = "Sly1WorldStatesForm";
            Text = "SluMAN :: World States";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(652, noteLabel.Bottom + 8);

            for (int world = 1; world <= sly1.WorldCount; world++)
            {
                worldComboBox.Items.Add(sly1.WorldName(world));
            }
            worldComboBox.SelectedIndex = CurrentWorld() - 1;
            worldComboBox.SelectedIndexChanged += (sender, e) => RefreshFromGame();
            RefreshFromGame();
        }

        private int SelectedWorld
        {
            get { return worldComboBox.SelectedIndex + 1; }
        }

        /// <summary>The world being played, or the first world in Paris, the hideout or the intro.</summary>
        private int CurrentWorld()
        {
            try
            {
                int index = game.GetCurrentLevelIndex();
                int world = index < 3 ? 1 : (index - 3) / sly1.LevelsPerWorld + 1;
                return Math.Max(1, Math.Min(sly1.WorldCount, world));
            }
            catch
            {
                return 1;
            }
        }

        private void BuildControls()
        {
            AddLabel(this, "World:", 8, 11);
            worldComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            worldComboBox.Location = new Point(52, 8);
            worldComboBox.Size = new Size(220, 21);
            Controls.Add(worldComboBox);
            AddButton(this, "Refresh", 280, 7, 80, (sender, e) => RefreshFromGame());

            GroupBox levelsGroupBox = AddGroup("Levels", 8, 38, 636, RowsY + sly1.LevelsPerWorld * RowHeight + 4);
            ToolTip tip = new ToolTip();
            FormClosed += (sender, e) => tip.Dispose();
            AddLabel(levelsGroupBox, "Level", 10, 23);
            for (int column = 0; column < ColumnFlags.Length; column++)
            {
                int flag = ColumnFlags[column];
                Button toggleAll = AddButton(levelsGroupBox, ColumnTitles[column], ColumnX + column * ColumnWidth, 18,
                    ColumnWidth - 4, (sender, e) => ToggleColumn(flag));
                tip.SetToolTip(toggleAll, $"Ticks {ColumnTitles[column]} for every level of the world, or clears it if all have it.");
            }
            for (int level = 0; level < sly1.LevelsPerWorld; level++)
            {
                int y = RowsY + level * RowHeight;
                Label label = new Label();
                label.Location = new Point(10, y + 3);
                label.Size = new Size(ColumnX - 20, 13);
                levelsGroupBox.Controls.Add(label);
                levelLabels[level] = label;

                for (int column = 0; column < ColumnFlags.Length; column++)
                {
                    int currentLevel = level;
                    int flag = ColumnFlags[column];
                    CheckBox checkBox = new CheckBox();
                    checkBox.AutoSize = true;
                    checkBox.Location = new Point(ColumnX + column * ColumnWidth + (ColumnWidth - 4) / 2 - 7, y + 2);
                    checkBox.Click += (sender, e) => SetLevelFlag(currentLevel, flag, ((CheckBox)sender).Checked);
                    levelsGroupBox.Controls.Add(checkBox);
                    levelCheckBoxes[level, column] = checkBox;
                }
            }

            int groupsY = levelsGroupBox.Bottom + 6;
            GroupBox flagsGroupBox = AddGroup("World", 8, groupsY, 312, 96);
            AddWorldFlag(flagsGroupBox, startedCheckBox, "Started", 10, 20, sly1.WorldStarted);
            AddWorldFlag(flagsGroupBox, bossCheckBox, "Boss defeated", 10, 43, sly1.WorldBossDefeated);
            AddWorldFlag(flagsGroupBox, key1CheckBox, "1 key collected", 160, 20, sly1.WorldKey1);
            AddWorldFlag(flagsGroupBox, key3CheckBox, "3 keys collected", 160, 43, sly1.WorldKey3);
            AddWorldFlag(flagsGroupBox, key7CheckBox, "7 keys collected", 160, 66, sly1.WorldKey7);

            GroupBox countsGroupBox = AddGroup("Counts", 332, groupsY, 312, 96);
            AddCounter(countsGroupBox, keysTextBox, "Keys:", 18, sly1.WorldCounter.Keys);
            AddCounter(countsGroupBox, vaultsTextBox, "Vaults:", 43, sly1.WorldCounter.Vaults);
            AddCounter(countsGroupBox, sprintsTextBox, "Sprints:", 68, sly1.WorldCounter.Sprints);
            Button recount = AddButton(countsGroupBox, "Recount", 214, 42, 88, (sender, e) => Recount());

            noteLabel = new Label();
            noteLabel.AutoSize = true;
            noteLabel.Location = new Point(8, flagsGroupBox.Bottom + 8);
            noteLabel.ForeColor = SystemColors.GrayText;
            noteLabel.Text = "Changes are written to the game straight away. Reload the level (Practice tab) to see them.";
            Controls.Add(noteLabel);

            tip.SetToolTip(key1CheckBox, "Set when the world's first key is collected.");
            tip.SetToolTip(key3CheckBox, "Set when the world's third key is collected.");
            tip.SetToolTip(key7CheckBox, "Set when the world's seventh key is collected.");
            tip.SetToolTip(recount, "Sets the counts to the number of levels ticked for Key, Vault and Sprint.");
        }

        private void AddWorldFlag(Control parent, CheckBox checkBox, string text, int x, int y, int flag)
        {
            checkBox.Text = text;
            checkBox.AutoSize = true;
            checkBox.Location = new Point(x, y);
            checkBox.Click += (sender, e) => SetWorldFlag(flag, checkBox.Checked);
            parent.Controls.Add(checkBox);
        }

        private void AddCounter(Control parent, TextBox textBox, string text, int y, sly1.WorldCounter counter)
        {
            AddLabel(parent, text, 10, y + 3);
            textBox.Location = new Point(60, y);
            textBox.Size = new Size(80, 20);
            parent.Controls.Add(textBox);
            AddButton(parent, "Set", 146, y - 1, 60, (sender, e) => SetCounter(textBox, counter));
        }

        private GroupBox AddGroup(string text, int x, int y, int width, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            Controls.Add(group);
            return group;
        }

        private static Button AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            parent.Controls.Add(button);
            return button;
        }

        private static void AddLabel(Control parent, string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            parent.Controls.Add(label);
        }

        private static bool IsUsed(int world, int level)
        {
            return sly1.WorldLevelName(world, level) != null;
        }

        /// <summary>Reads the selected world's levels, flags and counts from the game.</summary>
        public void RefreshFromGame()
        {
            int world = SelectedWorld;
            try
            {
                // Unused level slots (two in A Cold Heart of Hate) are hidden, and the rows below
                // move up to close the gap.
                int row = 0;
                for (int level = 0; level < sly1.LevelsPerWorld; level++)
                {
                    bool used = IsUsed(world, level);
                    int y = RowsY + row * RowHeight;
                    levelLabels[level].Visible = used;
                    levelLabels[level].Top = y + 3;
                    levelLabels[level].Text = used ? sly1.WorldLevelName(world, level) : "";
                    int flags = used ? game.GetLevelFlags(world, level) : 0;
                    for (int column = 0; column < ColumnFlags.Length; column++)
                    {
                        levelCheckBoxes[level, column].Visible = used;
                        levelCheckBoxes[level, column].Top = y + 2;
                        levelCheckBoxes[level, column].Checked = (flags & ColumnFlags[column]) != 0;
                    }
                    if (used)
                    {
                        row++;
                    }
                }

                int worldFlags = game.GetWorldFlags(world);
                startedCheckBox.Checked = (worldFlags & sly1.WorldStarted) != 0;
                key1CheckBox.Checked = (worldFlags & sly1.WorldKey1) != 0;
                key3CheckBox.Checked = (worldFlags & sly1.WorldKey3) != 0;
                key7CheckBox.Checked = (worldFlags & sly1.WorldKey7) != 0;
                bossCheckBox.Checked = (worldFlags & sly1.WorldBossDefeated) != 0;

                keysTextBox.Text = game.GetWorldCounter(world, sly1.WorldCounter.Keys).ToString();
                vaultsTextBox.Text = game.GetWorldCounter(world, sly1.WorldCounter.Vaults).ToString();
                sprintsTextBox.Text = game.GetWorldCounter(world, sly1.WorldCounter.Sprints).ToString();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the world state: {ex.Message}");
            }
        }

        private void SetLevelFlag(int level, int flag, bool set)
        {
            int world = SelectedWorld;
            try
            {
                int flags = game.GetLevelFlags(world, level);
                game.SetLevelFlags(world, level, set ? flags | flag : flags & ~flag);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the level: {ex.Message}");
                RefreshFromGame();
            }
        }

        /// <summary>Ticks the flag for every level of the world, or clears it if all have it.</summary>
        private void ToggleColumn(int flag)
        {
            int world = SelectedWorld;
            try
            {
                bool all = true;
                for (int level = 0; level < sly1.LevelsPerWorld; level++)
                {
                    if (IsUsed(world, level) && (game.GetLevelFlags(world, level) & flag) == 0)
                    {
                        all = false;
                    }
                }
                for (int level = 0; level < sly1.LevelsPerWorld; level++)
                {
                    if (IsUsed(world, level))
                    {
                        int flags = game.GetLevelFlags(world, level);
                        game.SetLevelFlags(world, level, all ? flags & ~flag : flags | flag);
                    }
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the levels: {ex.Message}");
            }
            RefreshFromGame();
        }

        private void SetWorldFlag(int flag, bool set)
        {
            int world = SelectedWorld;
            try
            {
                int flags = game.GetWorldFlags(world);
                game.SetWorldFlags(world, set ? flags | flag : flags & ~flag);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the world: {ex.Message}");
                RefreshFromGame();
            }
        }

        private void SetCounter(TextBox textBox, sly1.WorldCounter counter)
        {
            int value;
            if (!int.TryParse(textBox.Text.Trim(), out value) || value < 0)
            {
                statusLine.Error("Enter a count of 0 or more.");
                return;
            }
            try
            {
                game.SetWorldCounter(SelectedWorld, counter, value);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't set the count: {ex.Message}");
            }
        }

        /// <summary>Sets the key, vault and sprint counts to the number of levels that have them.</summary>
        private void Recount()
        {
            int world = SelectedWorld;
            try
            {
                int keys = 0;
                int vaults = 0;
                int sprints = 0;
                for (int level = 0; level < sly1.LevelsPerWorld; level++)
                {
                    if (!IsUsed(world, level))
                    {
                        continue;
                    }
                    int flags = game.GetLevelFlags(world, level);
                    keys += (flags & sly1.LevelKey) != 0 ? 1 : 0;
                    vaults += (flags & sly1.LevelVault) != 0 ? 1 : 0;
                    sprints += (flags & sly1.LevelSprint) != 0 ? 1 : 0;
                }
                game.SetWorldCounter(world, sly1.WorldCounter.Keys, keys);
                game.SetWorldCounter(world, sly1.WorldCounter.Vaults, vaults);
                game.SetWorldCounter(world, sly1.WorldCounter.Sprints, sprints);
                statusLine.Info($"{sly1.WorldName(world)}: {keys} keys, {vaults} vaults, {sprints} sprints.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't recount: {ex.Message}");
            }
            RefreshFromGame();
        }
    }
}
