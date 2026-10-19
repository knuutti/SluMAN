using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The debug menu's Tuning browser: every Sly 4 tunable, by category, with its live value.
    /// Values are edited in place; "Keep" saves one so it's written again on every connect. The
    /// values on screen are read again a few times a second. The controls are built in code.
    /// </summary>
    public class Sly4TunablesForm : Form, IRefreshOnShow
    {
        private const int RefreshIntervalMs = 500;
        private const int SearchDelayMs = 300;
        private const int ScrollDelayMs = 150;

        private const int NameColumn = 0;
        private const int TypeColumn = 1;
        private const int ValueColumn = 2;
        private const int KeepColumn = 3;

        private readonly sly4 game;

        private readonly TextBox searchBox = new TextBox();
        private readonly Button refreshButton = new Button();
        private readonly Button importButton = new Button();
        private readonly Label countLabel = new Label();
        private readonly TreeView categoryTree = new TreeView();
        private readonly DataGridView grid = new DataGridView();
        private readonly Timer refreshTimer = new Timer();
        private readonly Timer searchTimer = new Timer();
        private readonly Timer scrollTimer = new Timer();
        private StatusLine statusLine;

        // The category selected in the tree, e.g. "Camera.Default", or "" for all of them.
        private string category = "";
        // Set while values are put into the grid, so that doesn't count as an edit.
        private bool updating;
        // Set while the tree is rebuilt, so selecting a node doesn't fill the grid each time.
        private bool buildingTree;

        public Sly4TunablesForm(sly4 game)
        {
            this.game = game;
            Name = "Sly4TunablesForm";
            Text = "SluMAN :: Tunables";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildToolbar();
            BuildTree();
            BuildGrid();
            ClientSize = new Size(grid.Right + 8, grid.Bottom + 8);
            statusLine = new StatusLine(this, false);

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += refreshTimer_Tick;
            searchTimer.Interval = SearchDelayMs;
            searchTimer.Tick += searchTimer_Tick;
            scrollTimer.Interval = ScrollDelayMs;
            scrollTimer.Tick += scrollTimer_Tick;
            FormClosing += (sender, e) =>
            {
                refreshTimer.Stop();
                searchTimer.Stop();
                scrollTimer.Stop();
            };
            FormClosed += (sender, e) =>
            {
                refreshTimer.Dispose();
                searchTimer.Dispose();
                scrollTimer.Dispose();
            };

            LoadTunables();
            refreshTimer.Start();
        }

        private void BuildToolbar()
        {
            Label searchLabel = new Label();
            searchLabel.Text = "Search:";
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(8, 12);
            Controls.Add(searchLabel);

            searchBox.Location = new Point(56, 9);
            searchBox.Size = new Size(220, 20);
            searchBox.TextChanged += (sender, e) =>
            {
                searchTimer.Stop();
                searchTimer.Start();
            };
            Controls.Add(searchBox);

            refreshButton.Text = "Reload List";
            refreshButton.Location = new Point(284, 8);
            refreshButton.Size = new Size(80, 23);
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += (sender, e) => LoadTunables();
            Controls.Add(refreshButton);

            importButton.Text = "Import debugmenu.cfg...";
            importButton.Location = new Point(370, 8);
            importButton.Size = new Size(140, 23);
            importButton.UseVisualStyleBackColor = true;
            importButton.Click += importButton_Click;
            Controls.Add(importButton);

            countLabel.AutoSize = true;
            countLabel.Location = new Point(518, 12);
            Controls.Add(countLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(searchBox, "Shows the tunables whose name contains this text, in the selected category.");
            tip.SetToolTip(refreshButton, "Reads the list of tunables from the game again.");
            tip.SetToolTip(importButton,
                "Applies and keeps the values from a debug menu settings file\n" +
                "(/dev_hdd0/tmp/Sly4DebugMenu/debugmenu.cfg).");
            FormClosed += (sender, e) => tip.Dispose();
        }

        private void BuildTree()
        {
            categoryTree.Location = new Point(8, 38);
            categoryTree.Size = new Size(210, 420);
            categoryTree.HideSelection = false;
            categoryTree.AfterSelect += categoryTree_AfterSelect;
            Controls.Add(categoryTree);
        }

        private void BuildGrid()
        {
            grid.Location = new Point(224, 38);
            grid.Size = new Size(500, 420);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 18;
            grid.BackgroundColor = SystemColors.Window;

            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Name";
            nameColumn.ReadOnly = true;
            nameColumn.Width = 250;
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(nameColumn);

            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn();
            typeColumn.HeaderText = "Type";
            typeColumn.ReadOnly = true;
            typeColumn.Width = 45;
            typeColumn.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            typeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(typeColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.HeaderText = "Value";
            valueColumn.Width = 145;
            valueColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(valueColumn);

            DataGridViewCheckBoxColumn keepColumn = new DataGridViewCheckBoxColumn();
            keepColumn.HeaderText = "Keep";
            keepColumn.ToolTipText = "Writes this value again every time SluMAN connects to the game.";
            keepColumn.Width = 38;
            keepColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(keepColumn);

            grid.CellEndEdit += grid_CellEndEdit;
            grid.CurrentCellDirtyStateChanged += grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += grid_CellValueChanged;
            grid.Scroll += grid_Scroll;
            // A cell the grid can't draw shows blank instead of a dialog.
            grid.DataError += (sender, e) => e.ThrowException = false;
            Controls.Add(grid);
        }

        /// <summary>Reads the tunable list from the game and shows it.</summary>
        private void LoadTunables()
        {
            Cursor previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                game.tunables.Scan();
                BuildCategories(game.tunables.GetAll());
                ShowTunables();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the tunables: {ex.Message}");
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        private void BuildCategories(List<Sly4Tunables.Tunable> tunables)
        {
            buildingTree = true;
            categoryTree.BeginUpdate();
            categoryTree.Nodes.Clear();
            TreeNode root = new TreeNode("All tunables");
            root.Tag = "";
            categoryTree.Nodes.Add(root);

            Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (Sly4Tunables.Tunable tunable in tunables)
            {
                string[] parts = tunable.name.Split('.');
                TreeNode parent = root;
                string prefix = "";
                // The last part is the tunable itself.
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    prefix = prefix.Length == 0 ? parts[i] : prefix + "." + parts[i];
                    TreeNode node;
                    if (!nodes.TryGetValue(prefix, out node))
                    {
                        node = new TreeNode(parts[i]);
                        node.Tag = prefix;
                        parent.Nodes.Add(node);
                        nodes[prefix] = node;
                    }
                    parent = node;
                }
            }

            root.Expand();
            categoryTree.EndUpdate();

            // Keep the selected category if it still exists.
            TreeNode selected;
            if (category.Length > 0 && nodes.TryGetValue(category, out selected))
            {
                categoryTree.SelectedNode = selected;
            }
            else
            {
                category = "";
                categoryTree.SelectedNode = root;
            }
            buildingTree = false;
        }

        private List<Sly4Tunables.Tunable> FilteredTunables()
        {
            string search = searchBox.Text.Trim();
            string prefix = category.Length > 0 ? category + "." : "";
            return game.tunables.GetAll()
                .Where(t => t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Where(t => search.Length == 0 || t.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>Fills the grid with the tunables in the selected category that match the search.</summary>
        private void ShowTunables()
        {
            List<Sly4Tunables.Tunable> shown;
            try
            {
                shown = FilteredTunables();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the tunables: {ex.Message}");
                return;
            }

            string prefix = category.Length > 0 ? category + "." : "";
            updating = true;
            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (Sly4Tunables.Tunable tunable in shown)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(grid);
                row.Cells[NameColumn].Value = tunable.name.Substring(prefix.Length);
                row.Cells[NameColumn].ToolTipText = tunable.name;
                row.Cells[TypeColumn].Value = Sly4Tunables.TypeName(tunable.type);
                if (tunable.type == Sly4Tunables.TunableType.boolean)
                {
                    // The value column is a text column, whose empty value is "". A check box
                    // can't show that, so until the value is read it shows unticked instead.
                    DataGridViewCheckBoxCell checkBox = new DataGridViewCheckBoxCell();
                    checkBox.Style.NullValue = false;
                    row.Cells[ValueColumn] = checkBox;
                }
                else if (tunable.type == Sly4Tunables.TunableType.unknown)
                {
                    row.Cells[ValueColumn].ReadOnly = true;
                }
                row.Cells[KeepColumn].Value = game.tunables.IsKept(tunable);
                row.Tag = tunable;
                grid.Rows.Add(row);
            }
            grid.ResumeLayout();
            updating = false;

            countLabel.Text = $"{shown.Count} tunables, {game.tunables.KeptCount} kept";

            // Only the rows on screen: on a PS3 each read is a network round trip, and reading a
            // few thousand values would stall the window on every search. Others are read when
            // scrolled to.
            grid.PerformLayout();
            try
            {
                RefreshValues(DisplayedRows());
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the values: {ex.Message}");
            }
        }

        /// <summary>Reads the given rows' values from the game and shows any that changed.</summary>
        private void RefreshValues(IEnumerable<DataGridViewRow> rows)
        {
            List<DataGridViewRow> list = rows.Where(r => r.Tag is Sly4Tunables.Tunable).ToList();
            if (list.Count == 0)
            {
                return;
            }

            Dictionary<Sly4Tunables.Tunable, string> values = game.tunables.ReadValues(list.Select(r => (Sly4Tunables.Tunable)r.Tag));
            updating = true;
            try
            {
                foreach (DataGridViewRow row in list)
                {
                    Sly4Tunables.Tunable tunable = (Sly4Tunables.Tunable)row.Tag;
                    string value;
                    if (!values.TryGetValue(tunable, out value))
                    {
                        continue;
                    }
                    DataGridViewCell cell = row.Cells[ValueColumn];
                    if (cell.IsInEditMode)
                    {
                        continue;
                    }
                    object shown = tunable.type == Sly4Tunables.TunableType.boolean ? (object)(value == "1") : value;
                    if (!Equals(cell.Value, shown))
                    {
                        cell.Value = shown;
                    }
                }
            }
            finally
            {
                updating = false;
            }
        }

        private IEnumerable<DataGridViewRow> DisplayedRows()
        {
            int first = grid.FirstDisplayedScrollingRowIndex;
            if (first < 0)
            {
                return Enumerable.Empty<DataGridViewRow>();
            }
            int count = grid.DisplayedRowCount(true);
            return Enumerable.Range(first, Math.Min(count, grid.Rows.Count - first)).Select(i => grid.Rows[i]);
        }

        public void RefreshFromGame()
        {
            if (categoryTree.Nodes.Count == 0)
            {
                // The list couldn't be read when the tab opened; try again now.
                LoadTunables();
                return;
            }
            try
            {
                RefreshValues(DisplayedRows());
            }
            catch
            {
                // The game isn't readable right now; the timer tries again.
            }
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            // Only while this tab is the one showing.
            if (!Visible || (Parent != null && !Parent.Visible) || grid.IsCurrentCellInEditMode)
            {
                return;
            }
            try
            {
                RefreshValues(DisplayedRows());
            }
            catch
            {
                // The game is loading or reconnecting; the next tick tries again.
            }
        }

        private void grid_Scroll(object sender, ScrollEventArgs e)
        {
            // Read the rows scrolled to once scrolling pauses, not on every step of it.
            scrollTimer.Stop();
            scrollTimer.Start();
        }

        private void scrollTimer_Tick(object sender, EventArgs e)
        {
            scrollTimer.Stop();
            try
            {
                RefreshValues(DisplayedRows());
            }
            catch
            {
                // The refresh timer tries again.
            }
        }

        private void searchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            ShowTunables();
        }

        private void categoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string selected = e.Node.Tag as string;
            if (buildingTree || selected == null)
            {
                return;
            }
            category = selected;
            ShowTunables();
        }

        private void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Check boxes only report a change once committed, which normally waits for the cell to
            // lose focus. Commit right away so a click takes effect.
            if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (updating || e.RowIndex < 0)
            {
                return;
            }
            DataGridViewRow row = grid.Rows[e.RowIndex];
            Sly4Tunables.Tunable tunable = row.Tag as Sly4Tunables.Tunable;
            if (tunable == null)
            {
                return;
            }

            if (e.ColumnIndex == KeepColumn)
            {
                bool keep = Equals(row.Cells[KeepColumn].Value, true);
                try
                {
                    game.tunables.SetKept(tunable, keep);
                    countLabel.Text = $"{grid.Rows.Count} tunables, {game.tunables.KeptCount} kept";
                    statusLine.Info(keep
                        ? $"{tunable.name} will be set to its current value on every connect."
                        : $"{tunable.name} is no longer kept.");
                }
                catch (Exception ex)
                {
                    statusLine.Error($"Couldn't save {tunable.name}: {ex.Message}");
                }
            }
            else if (e.ColumnIndex == ValueColumn && tunable.type == Sly4Tunables.TunableType.boolean)
            {
                Write(row, tunable, Equals(row.Cells[ValueColumn].Value, true) ? "1" : "0");
            }
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != ValueColumn)
            {
                return;
            }
            DataGridViewRow row = grid.Rows[e.RowIndex];
            Sly4Tunables.Tunable tunable = row.Tag as Sly4Tunables.Tunable;
            if (tunable == null || tunable.type == Sly4Tunables.TunableType.boolean)
            {
                return;
            }
            string text = row.Cells[ValueColumn].Value as string;
            Write(row, tunable, text == null ? "" : text);
        }

        /// <summary>Writes a value, then shows what the game holds now (the old value if it failed).</summary>
        private void Write(DataGridViewRow row, Sly4Tunables.Tunable tunable, string value)
        {
            try
            {
                game.tunables.WriteValue(tunable, value);
                if (game.tunables.IsKept(tunable))
                {
                    // Keep the new value rather than the one kept before.
                    game.tunables.SetKept(tunable, true);
                }
            }
            catch (FormatException ex)
            {
                statusLine.Error($"{tunable.name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't write {tunable.name}: {ex.Message}");
            }

            try
            {
                RefreshValues(new[] { row });
            }
            catch
            {
                // The refresh timer tries again.
            }
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Import debug menu settings";
                dialog.Filter = "Debug menu settings (*.cfg)|*.cfg|All files (*.*)|*.*";
                dialog.FileName = "debugmenu.cfg";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    List<string> unknown;
                    int applied = game.tunables.Import(dialog.FileName, out unknown);
                    ShowTunables();
                    if (unknown.Count == 0)
                    {
                        statusLine.Info($"Applied and kept {applied} tunables.");
                    }
                    else
                    {
                        statusLine.Error($"Applied and kept {applied} tunables. Skipped {unknown.Count}: {string.Join(", ", unknown.Take(5))}{(unknown.Count > 5 ? ", ..." : "")}");
                    }
                }
                catch (Exception ex)
                {
                    statusLine.Error($"Couldn't import the settings: {ex.Message}");
                }
            }
        }
    }
}
