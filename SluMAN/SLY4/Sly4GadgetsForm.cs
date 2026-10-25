using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Gadgets tab: every gadget's state (locked, in ThiefNet or owned), and the gadgets each
    /// run file loads with. The game only reads its gadgets on a load (a checkpoint or a level),
    /// so changes stay in the list until Apply writes them and reloads. The controls are built in
    /// code.
    /// </summary>
    public class Sly4GadgetsForm : Form, IRefreshOnShow
    {
        private const int CharacterColumn = 0;
        private const int NameColumn = 1;
        private const int AvailableColumn = 2;
        private const int StateColumn = 3;

        private const string AllCharacters = "All characters";

        // The states the State column offers, in order.
        private static readonly byte[] PickableStates = { Sly4Gadgets.Locked, Sly4Gadgets.InThiefNet, Sly4Gadgets.Owned };

        private readonly sly4 game;
        private readonly Sly4Practice practice;
        private readonly ComboBox characterComboBox = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly ComboBox runFileComboBox = new ComboBox();
        private readonly GroupBox runFileGroupBox = new GroupBox();
        // Says which gadgets saving a run file left out, as they aren't available yet.
        private readonly Label runFileWarningLabel = new Label();
        // How many changes the list has that aren't in the game yet.
        private readonly Label changesLabel = new Label();
        private Font changedFont;
        private StatusLine statusLine;
        // Set while states are put into the grid, so that doesn't count as an edit.
        private bool updating;

        // The gadgets as last read from the game, and as the list has them; null until read.
        private byte[] gameStates;
        private byte[] states;

        public Sly4GadgetsForm(sly4 game, Sly4Practice practice)
        {
            this.game = game;
            this.practice = practice;
            Name = "Sly4GadgetsForm";
            Text = "SluMAN :: Gadgets";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(grid.Right + 8, Math.Max(runFileGroupBox.Bottom, runFileWarningLabel.Bottom) + 8);
            // The grid grows with the tab when the Practice window has a fixed size. Anchored
            // only now: an anchor keeps the distance to the window's edge it has when set.
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            runFileGroupBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            runFileWarningLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            changesLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLine = new StatusLine(this, false);
            changedFont = new Font(grid.Font, FontStyle.Bold);
            FormClosed += (sender, e) => changedFont.Dispose();

            RefreshFromGame();
        }

        private void BuildControls()
        {
            ToolTip tip = new ToolTip();
            FormClosed += (sender, e) => tip.Dispose();

            Label showLabel = new Label();
            showLabel.Text = "Show:";
            showLabel.AutoSize = true;
            showLabel.Location = new Point(8, 13);
            Controls.Add(showLabel);

            characterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            characterComboBox.Location = new Point(48, 9);
            characterComboBox.Size = new Size(110, 21);
            characterComboBox.Items.Add(AllCharacters);
            characterComboBox.Items.AddRange(Sly4Gadgets.CharacterNames);
            characterComboBox.SelectedIndex = 0;
            characterComboBox.SelectedIndexChanged += (sender, e) => ShowCharacter();
            Controls.Add(characterComboBox);

            Label setLabel = new Label();
            setLabel.Text = "Set shown to:";
            setLabel.AutoSize = true;
            setLabel.Location = new Point(170, 13);
            Controls.Add(setLabel);

            tip.SetToolTip(AddButton("Owned", 246, 60, (sender, e) => SetShown(Sly4Gadgets.Owned)),
                "Gives every gadget in the list, as buying it in ThiefNet does.");
            tip.SetToolTip(AddButton("In ThiefNet", 310, 75, (sender, e) => SetShown(Sly4Gadgets.InThiefNet)),
                "Takes every gadget in the list away, but leaves it for sale in ThiefNet.");
            tip.SetToolTip(AddButton("Locked", 389, 60, (sender, e) => SetShown(Sly4Gadgets.Locked)),
                "Takes every gadget in the list away, and out of ThiefNet.");
            tip.SetToolTip(AddButton("Discard", 470, 60, (sender, e) => ReadGame(false)),
                "Drops the changes not applied and reads the gadgets from the game again.");
            tip.SetToolTip(AddButton("Apply", 534, 90, (sender, e) => Apply()),
                "Writes the changes to the game and reloads so they take effect: the checkpoint in a job, otherwise the level from its start.");

            BuildGrid();

            runFileGroupBox.Text = "Run file gadgets";
            runFileGroupBox.Location = new Point(8, grid.Bottom + 6);
            runFileGroupBox.Size = new Size(312, 54);
            Controls.Add(runFileGroupBox);

            runFileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runFileComboBox.Location = new Point(10, 20);
            runFileComboBox.Size = new Size(120, 21);
            runFileComboBox.Items.AddRange(Sly4Gadgets.RunFiles);
            runFileComboBox.SelectedIndex = 0;
            runFileComboBox.SelectedIndexChanged += (sender, e) => runFileWarningLabel.Text = "";
            runFileGroupBox.Controls.Add(runFileComboBox);

            Button loadButton = new Button();
            loadButton.Text = "Load";
            loadButton.Location = new Point(136, 19);
            loadButton.Size = new Size(80, 23);
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += loadRunFileButton_Click;
            runFileGroupBox.Controls.Add(loadButton);
            tip.SetToolTip(loadButton, "Puts the gadgets saved for this run file in the list. Apply to use them in game.");

            Button saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(222, 19);
            saveButton.Size = new Size(80, 23);
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += saveRunFileButton_Click;
            runFileGroupBox.Controls.Add(saveButton);
            tip.SetToolTip(saveButton, "Saves the list's gadgets as this run file's. Gadgets that aren't in ThiefNet yet at the start of its episode are locked first.");

            changesLabel.Location = new Point(328, runFileGroupBox.Top + 6);
            changesLabel.Size = new Size(296, 16);
            changesLabel.ForeColor = Color.DarkOrange;
            Controls.Add(changesLabel);

            runFileWarningLabel.Location = new Point(328, changesLabel.Bottom + 2);
            runFileWarningLabel.Size = new Size(296, 30);
            runFileWarningLabel.ForeColor = Color.Firebrick;
            Controls.Add(runFileWarningLabel);
        }

        private Button AddButton(string text, int x, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, 8);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            Controls.Add(button);
            return button;
        }

        private void BuildGrid()
        {
            grid.Location = new Point(8, 38);
            grid.Size = new Size(616, 380);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 20;
            grid.BackgroundColor = SystemColors.Window;

            DataGridViewTextBoxColumn characterColumn = new DataGridViewTextBoxColumn();
            characterColumn.HeaderText = "Character";
            characterColumn.ReadOnly = true;
            characterColumn.Width = 80;
            characterColumn.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            characterColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(characterColumn);

            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Gadget";
            nameColumn.ReadOnly = true;
            // Takes whatever width the other columns leave.
            nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(nameColumn);

            DataGridViewTextBoxColumn availableColumn = new DataGridViewTextBoxColumn();
            availableColumn.HeaderText = "Available from";
            availableColumn.ToolTipText = "The first episode whose run file may have the gadget.";
            availableColumn.ReadOnly = true;
            availableColumn.Width = 100;
            availableColumn.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            availableColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(availableColumn);

            DataGridViewComboBoxColumn stateColumn = new DataGridViewComboBoxColumn();
            stateColumn.HeaderText = "State";
            stateColumn.Width = 120;
            stateColumn.FlatStyle = FlatStyle.Flat;
            stateColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            foreach (byte state in PickableStates)
            {
                stateColumn.Items.Add(Sly4Gadgets.StateName(state));
            }
            grid.Columns.Add(stateColumn);

            foreach (Sly4Gadgets.Gadget gadget in Sly4Gadgets.All)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(grid);
                row.Cells[CharacterColumn].Value = gadget.character;
                row.Cells[NameColumn].Value = gadget.name;
                row.Cells[NameColumn].ToolTipText = $"Byte 0x{gadget.index:X2} of the gadget array";
                row.Cells[AvailableColumn].Value = GadgetAvailability.EpisodeName(gadget.availableFrom);
                row.Tag = gadget;
                grid.Rows.Add(row);
            }

            grid.CurrentCellDirtyStateChanged += grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += grid_CellValueChanged;
            // A cell the grid can't draw shows blank instead of a dialog.
            grid.DataError += (sender, e) => e.ThrowException = false;
            Controls.Add(grid);
        }

        /// <summary>Reads the gadgets from the game, keeping the changes not applied yet.</summary>
        public void RefreshFromGame()
        {
            ReadGame(true);
        }

        private void ReadGame(bool keepChanges)
        {
            byte[] read;
            try
            {
                read = game.GetGadgetStates();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the gadgets: {ex.Message}");
                return;
            }

            if (keepChanges && states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i] == gameStates[i])
                    {
                        states[i] = read[i];
                    }
                }
            }
            else
            {
                states = (byte[])read.Clone();
            }
            gameStates = read;
            ShowStates();
        }

        /// <summary>Shows the list's states, the changed ones in bold.</summary>
        private void ShowStates()
        {
            updating = true;
            grid.CancelEdit();
            int changes = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)row.Tag;
                DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)row.Cells[StateColumn];
                string name = Sly4Gadgets.StateName(states[gadget.index]);
                // A value SluMAN doesn't offer (such as 5, new in ThiefNet) still shows, in this
                // row only.
                if (!cell.Items.Contains(name))
                {
                    cell.Items.Add(name);
                }
                cell.Value = name;

                bool changed = states[gadget.index] != gameStates[gadget.index];
                cell.Style.Font = changed ? changedFont : null;
                if (changed)
                {
                    changes++;
                }
            }
            updating = false;

            changesLabel.Text = changes == 0 ? ""
                : $"{changes} {(changes == 1 ? "change" : "changes")} not in game yet. Apply reloads to use them.";
        }

        /// <summary>Whether the gadgets have been read, so the list can be changed.</summary>
        private bool HasStates()
        {
            if (states == null)
            {
                statusLine.Error("The gadgets haven't been read from the game. Click Discard to read them.");
                return false;
            }
            return true;
        }

        private void ShowCharacter()
        {
            string character = characterComboBox.SelectedItem.ToString();
            grid.CurrentCell = null;
            foreach (DataGridViewRow row in grid.Rows)
            {
                Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)row.Tag;
                row.Visible = character == AllCharacters || gadget.character == character;
            }
        }

        /// <summary>Sets every gadget the list shows to one state.</summary>
        private void SetShown(byte state)
        {
            if (!HasStates())
            {
                return;
            }
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Visible)
                {
                    states[((Sly4Gadgets.Gadget)row.Tag).index] = state;
                }
            }
            ShowStates();
        }

        /// <summary>Writes the list's changes to the game and reloads so they take effect.</summary>
        private void Apply()
        {
            if (!HasStates())
            {
                return;
            }
            try
            {
                // Only the changed gadgets, over what the game has now: it may have changed others
                // since they were read.
                byte[] current = game.GetGadgetStates();
                int changes = 0;
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i] != gameStates[i])
                    {
                        current[i] = states[i];
                        changes++;
                    }
                }
                if (changes == 0)
                {
                    statusLine.Info("There are no changes to apply.");
                    return;
                }

                game.SetGadgetStates(current);
                gameStates = current;
                states = (byte[])current.Clone();
                ShowStates();
                statusLine.Info($"Applied {changes} {(changes == 1 ? "change" : "changes")}; reloading.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't apply the gadgets: {ex.Message}");
                return;
            }
            practice.ReloadForGadgets();
        }

        private void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // A combo box cell only reports a change once committed, which normally waits for the
            // cell to lose focus. Commit right away so picking a state shows as a change.
            if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (updating || e.RowIndex < 0 || e.ColumnIndex != StateColumn || states == null)
            {
                return;
            }
            DataGridViewRow row = grid.Rows[e.RowIndex];
            Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)row.Tag;
            string name = row.Cells[StateColumn].Value as string;
            foreach (byte state in PickableStates)
            {
                if (name == Sly4Gadgets.StateName(state))
                {
                    states[gadget.index] = state;
                    // Called later: the grid is still committing this cell.
                    BeginInvoke((MethodInvoker)ShowStates);
                    return;
                }
            }
        }

        private void loadRunFileButton_Click(object sender, EventArgs e)
        {
            if (!HasStates())
            {
                return;
            }
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                byte[] saved = Sly4Gadgets.LoadRunFile(runFile);
                if (saved == null)
                {
                    statusLine.Error($"No gadgets are saved for {runFile}. Set them here and click Save.");
                    return;
                }
                states = saved;
                ShowStates();
                runFileWarningLabel.Text = "";
                statusLine.Info($"Put {runFile}'s gadgets in the list. Apply to use them in game.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load {runFile}'s gadgets: {ex.Message}");
            }
        }

        private void saveRunFileButton_Click(object sender, EventArgs e)
        {
            if (!HasStates())
            {
                return;
            }
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                // Locked in the list too, so it shows what was saved.
                List<string> removed = Sly4Gadgets.RemoveUnavailable(runFile, states);
                Sly4Gadgets.SaveRunFile(runFile, states);
                ShowStates();
                runFileWarningLabel.Text = GadgetAvailability.RemovedWarning(runFile, removed);
                statusLine.Info($"Saved the list's gadgets as {runFile}'s.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save {runFile}'s gadgets: {ex.Message}");
            }
        }
    }
}
