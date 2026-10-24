using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Gadgets tab: every gadget's state (locked, in ThiefNet or owned), written to the game
    /// as soon as it's picked, and the gadgets each run file loads with. The controls are built
    /// in code.
    /// </summary>
    public class Sly4GadgetsForm : Form, IRefreshOnShow
    {
        private const int CharacterColumn = 0;
        private const int NameColumn = 1;
        private const int StateColumn = 2;

        private const string AllCharacters = "All characters";

        // The states the State column offers, in order.
        private static readonly byte[] PickableStates = { Sly4Gadgets.Locked, Sly4Gadgets.InThiefNet, Sly4Gadgets.Owned };

        private readonly sly4 game;
        private readonly ComboBox characterComboBox = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly ComboBox runFileComboBox = new ComboBox();
        private readonly GroupBox runFileGroupBox = new GroupBox();
        private StatusLine statusLine;
        // Set while states are put into the grid, so that doesn't count as an edit.
        private bool updating;

        public Sly4GadgetsForm(sly4 game)
        {
            this.game = game;
            Name = "Sly4GadgetsForm";
            Text = "SluMAN :: Gadgets";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(grid.Right + 8, runFileGroupBox.Bottom + 8);
            // The grid grows with the tab when the Practice window has a fixed size. Anchored
            // only now: an anchor keeps the distance to the window's edge it has when set.
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            runFileGroupBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLine = new StatusLine(this, false);

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
            tip.SetToolTip(AddButton("Reload", 564, 60, (sender, e) => RefreshFromGame()),
                "Reads the gadgets from the game again.");

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
            runFileGroupBox.Controls.Add(runFileComboBox);

            Button loadButton = new Button();
            loadButton.Text = "Load";
            loadButton.Location = new Point(136, 19);
            loadButton.Size = new Size(80, 23);
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += loadRunFileButton_Click;
            runFileGroupBox.Controls.Add(loadButton);
            tip.SetToolTip(loadButton, "Sets the game's gadgets to the ones saved for this run file.");

            Button saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(222, 19);
            saveButton.Size = new Size(80, 23);
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += saveRunFileButton_Click;
            runFileGroupBox.Controls.Add(saveButton);
            tip.SetToolTip(saveButton, "Saves the game's gadgets as they are now as this run file's.");
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
                row.Tag = gadget;
                grid.Rows.Add(row);
            }

            grid.CurrentCellDirtyStateChanged += grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += grid_CellValueChanged;
            // A cell the grid can't draw shows blank instead of a dialog.
            grid.DataError += (sender, e) => e.ThrowException = false;
            Controls.Add(grid);
        }

        /// <summary>Reads every gadget's state from the game and shows it.</summary>
        public void RefreshFromGame()
        {
            byte[] states;
            try
            {
                states = game.GetGadgetStates();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the gadgets: {ex.Message}");
                return;
            }
            ShowStates(states);
        }

        private void ShowStates(byte[] states)
        {
            updating = true;
            grid.CancelEdit();
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
            }
            updating = false;
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

        /// <summary>Sets every gadget the list shows to one state, in one write.</summary>
        private void SetShown(byte state)
        {
            try
            {
                byte[] states = game.GetGadgetStates();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Visible)
                    {
                        states[((Sly4Gadgets.Gadget)row.Tag).index] = state;
                    }
                }
                game.SetGadgetStates(states);
                ShowStates(states);
                string which = characterComboBox.SelectedIndex == 0 ? "every gadget" : $"{characterComboBox.SelectedItem}'s gadgets";
                statusLine.Info($"Set {which} to {Sly4Gadgets.StateName(state)}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't set the gadgets: {ex.Message}");
            }
        }

        private void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // A combo box cell only reports a change once committed, which normally waits for the
            // cell to lose focus. Commit right away so picking a state takes effect.
            if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (updating || e.RowIndex < 0 || e.ColumnIndex != StateColumn)
            {
                return;
            }
            DataGridViewRow row = grid.Rows[e.RowIndex];
            Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)row.Tag;
            string name = row.Cells[StateColumn].Value as string;
            foreach (byte state in PickableStates)
            {
                if (name != Sly4Gadgets.StateName(state))
                {
                    continue;
                }
                try
                {
                    game.SetGadgetState(gadget.index, state);
                    statusLine.Info($"{gadget.character}'s {gadget.name}: {name}.");
                }
                catch (Exception ex)
                {
                    statusLine.Error($"Couldn't set {gadget.name}: {ex.Message}");
                }
                return;
            }
        }

        private void loadRunFileButton_Click(object sender, EventArgs e)
        {
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                byte[] states = Sly4Gadgets.LoadRunFile(runFile);
                if (states == null)
                {
                    statusLine.Error($"No gadgets are saved for {runFile}. Set them here and click Save.");
                    return;
                }
                game.SetGadgetStates(states);
                ShowStates(states);
                statusLine.Info($"Set the gadgets to {runFile}'s.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load {runFile}'s gadgets: {ex.Message}");
            }
        }

        private void saveRunFileButton_Click(object sender, EventArgs e)
        {
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                byte[] states = game.GetGadgetStates();
                Sly4Gadgets.SaveRunFile(runFile, states);
                ShowStates(states);
                statusLine.Info($"Saved the game's gadgets as {runFile}'s.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save {runFile}'s gadgets: {ex.Message}");
            }
        }
    }
}
