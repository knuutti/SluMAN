using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace racman
{
    /// <summary>
    /// A live hex view of game memory: 32 rows of 16 bytes and their ASCII, refreshed a few times a
    /// second. The address can be a pointer chain such as [5EC654]+168, followed on every refresh.
    /// Bytes that just changed are highlighted, and typing two hex digits into a byte writes it.
    /// The controls are built in code: it is one grid and a toolbar.
    /// </summary>
    public class MemoryViewerForm : Form
    {
        private const int Columns = 16;
        private const int Rows = 32;
        private const int ViewSize = Columns * Rows;
        private const int RefreshIntervalMs = 200;
        private const int ChangedHighlightMs = 1000;
        private const int WheelRows = 4;

        private static readonly Color ChangedColor = Color.FromArgb(255, 236, 140);

        private readonly TextBox addressBox = new TextBox();
        private readonly Button goButton = new Button();
        private readonly Label resolvedLabel = new Label();
        private readonly DataGridView grid = new DataGridView();
        private readonly Timer refreshTimer = new Timer();
        private StatusLine statusLine;

        // Opens (or selects) the memory watch list, for "Add watch". Supplied by the window that
        // hosts the viewer, so a Practice window can switch tabs instead of opening a window.
        private readonly Func<MemoryForm> openMemoryForm;

        private AddressExpression expression;
        // How far the view has been scrolled from where the expression points.
        private long viewOffset;
        // Where the first byte on screen is, as of the last refresh. 0 when nothing is shown.
        private uint viewStart;
        private byte[] shown;
        private readonly DateTime[] changedAt = new DateTime[ViewSize];

        private static IPS3API api => func.api;

        /// <summary>
        /// Opens the viewer as its own window, or brings it forward. Used where there are no tabs.
        /// </summary>
        public static MemoryViewerForm ShowFor(Form owner)
        {
            MemoryViewerForm viewer = Application.OpenForms.OfType<MemoryViewerForm>().FirstOrDefault();
            if (viewer == null)
            {
                viewer = new MemoryViewerForm(() => MemoryForm.ShowFor(owner));
                viewer.Show(owner);
            }
            else
            {
                viewer.Activate();
            }
            return viewer;
        }

        public MemoryViewerForm(Func<MemoryForm> openMemoryForm)
        {
            this.openMemoryForm = openMemoryForm;
            Name = "MemoryViewerForm";
            Text = "SluMAN :: Memory Viewer";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildToolbar();
            BuildGrid();
            ClientSize = new Size(grid.Right + 8, grid.Bottom + 8);
            statusLine = new StatusLine(this, false);

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += refreshTimer_Tick;
            FormClosing += (sender, e) => refreshTimer.Stop();
            FormClosed += (sender, e) => refreshTimer.Dispose();
        }

        private void BuildToolbar()
        {
            Label addressLabel = new Label();
            addressLabel.Text = "Address or pointer:";
            addressLabel.AutoSize = true;
            addressLabel.Location = new Point(8, 12);
            Controls.Add(addressLabel);

            addressBox.Font = new Font("Consolas", 9F);
            addressBox.Location = new Point(118, 9);
            addressBox.Size = new Size(200, 20);
            Controls.Add(addressBox);

            goButton.Text = "Go";
            goButton.Location = new Point(324, 8);
            goButton.Size = new Size(50, 23);
            goButton.UseVisualStyleBackColor = true;
            goButton.Click += (sender, e) => GoToTypedAddress();
            Controls.Add(goButton);
            func.BindEnter(addressBox, goButton);

            resolvedLabel.AutoSize = true;
            resolvedLabel.Location = new Point(382, 12);
            resolvedLabel.Text = "Type an address and press Enter. Scroll with the mouse wheel or Page Up / Page Down.";
            Controls.Add(resolvedLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(addressBox,
                "A hex address such as 7B4CE0, or a pointer chain:\n" +
                "[5EC654]+168 reads the 4-byte big-endian pointer at 5EC654 and adds 168.");
            FormClosed += (sender, e) => tip.Dispose();
        }

        private void BuildGrid()
        {
            Font mono = new Font("Consolas", 9F);

            grid.Location = new Point(8, 38);
            grid.Font = mono;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToOrderColumns = false;
            grid.RowHeadersVisible = false;
            grid.ScrollBars = ScrollBars.None;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 18;
            grid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.BackgroundColor = SystemColors.Window;

            DataGridViewTextBoxColumn addressColumn = new DataGridViewTextBoxColumn();
            addressColumn.HeaderText = "Address";
            addressColumn.ReadOnly = true;
            addressColumn.Width = 80;
            addressColumn.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            addressColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(addressColumn);

            for (int i = 0; i < Columns; i++)
            {
                DataGridViewTextBoxColumn byteColumn = new DataGridViewTextBoxColumn();
                byteColumn.HeaderText = i.ToString("X2");
                byteColumn.Width = 26;
                byteColumn.MaxInputLength = 2;
                byteColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                byteColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns.Add(byteColumn);
            }

            DataGridViewTextBoxColumn asciiColumn = new DataGridViewTextBoxColumn();
            asciiColumn.HeaderText = "ASCII";
            asciiColumn.ReadOnly = true;
            asciiColumn.Width = 140;
            asciiColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(asciiColumn);

            grid.Rows.Add(Rows);
            int width = grid.Columns.Cast<DataGridViewColumn>().Sum(c => c.Width) + 3;
            int height = grid.ColumnHeadersHeight + Rows * grid.RowTemplate.Height + 3;
            grid.Size = new Size(width, height);

            grid.CellBeginEdit += grid_CellBeginEdit;
            grid.CellEndEdit += grid_CellEndEdit;
            grid.MouseWheel += grid_MouseWheel;
            grid.KeyDown += grid_KeyDown;
            grid.CellMouseDown += grid_CellMouseDown;
            Controls.Add(grid);
        }

        private static bool IsByteColumn(int columnIndex)
        {
            return columnIndex >= 1 && columnIndex <= Columns;
        }

        private static int ByteIndex(int rowIndex, int columnIndex)
        {
            return rowIndex * Columns + (columnIndex - 1);
        }

        /// <summary>Shows memory from <paramref name="address"/>, which may be a pointer chain.</summary>
        public void ShowAddress(string address)
        {
            addressBox.Text = address;
            GoToTypedAddress();
        }

        private void GoToTypedAddress()
        {
            AddressExpression parsed;
            string error;
            if (!AddressExpression.TryParse(addressBox.Text, out parsed, out error))
            {
                statusLine.Error(error);
                return;
            }

            expression = parsed;
            addressBox.Text = parsed.ToString();
            viewOffset = 0;
            shown = null;
            Array.Clear(changedAt, 0, changedAt.Length);
            RefreshView();
            refreshTimer.Start();
        }

        private void ScrollView(long bytes)
        {
            if (expression == null)
            {
                return;
            }
            viewOffset += bytes;
            // A different range: nothing on screen has "changed" yet.
            shown = null;
            Array.Clear(changedAt, 0, changedAt.Length);
            RefreshView();
        }

        private void grid_MouseWheel(object sender, MouseEventArgs e)
        {
            int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
            ScrollView(-notches * WheelRows * Columns);
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (grid.IsCurrentCellInEditMode)
            {
                return;
            }

            int row = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : 0;
            if (e.KeyCode == Keys.PageDown)
            {
                ScrollView(ViewSize);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                ScrollView(-ViewSize);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down && row == Rows - 1)
            {
                ScrollView(Columns);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && row == 0)
            {
                ScrollView(-Columns);
                e.Handled = true;
            }
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshView();
        }

        private static uint ReadPointer(uint address)
        {
            byte[] bytes = api.ReadMemory(api.getCurrentPID(), address, 4);
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }

        private void RefreshView()
        {
            if (expression == null || api == null)
            {
                return;
            }

            uint start;
            try
            {
                uint baseAddress;
                if (!expression.TryResolve(ReadPointer, out baseAddress))
                {
                    resolvedLabel.Text = "A pointer on the chain is null right now.";
                    ShowUnreadable();
                    return;
                }
                start = unchecked((uint)(baseAddress + viewOffset));
            }
            catch (Exception ex)
            {
                resolvedLabel.Text = $"Couldn't follow the pointer: {ex.Message}";
                ShowUnreadable();
                return;
            }

            byte[] bytes;
            try
            {
                // 512 bytes: one request, well under Ratchetron's ~2048-byte read limit.
                bytes = api.ReadMemory(api.getCurrentPID(), start, ViewSize);
            }
            catch (Exception ex)
            {
                resolvedLabel.Text = $"Couldn't read at {start:X8}: {ex.Message}";
                ShowUnreadable();
                return;
            }

            if (bytes == null || bytes.Length < ViewSize)
            {
                resolvedLabel.Text = $"Couldn't read at {start:X8}.";
                ShowUnreadable();
                return;
            }

            if (start != viewStart)
            {
                // The pointer moved: compare against nothing rather than against other memory.
                shown = null;
                Array.Clear(changedAt, 0, changedAt.Length);
            }
            viewStart = start;

            resolvedLabel.Text = expression.IsStatic && viewOffset == 0
                ? $"Showing {start:X8}"
                : $"Showing {start:X8} ({expression}{FormatOffset(viewOffset)})";

            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < ViewSize; i++)
            {
                if (shown != null && shown[i] != bytes[i])
                {
                    changedAt[i] = now;
                }
            }
            shown = bytes;
            Draw(now);
        }

        private static string FormatOffset(long offset)
        {
            if (offset == 0) return "";
            return offset > 0 ? $" +{offset:X}" : $" -{-offset:X}";
        }

        private void Draw(DateTime now)
        {
            for (int row = 0; row < Rows; row++)
            {
                DataGridViewRow gridRow = grid.Rows[row];
                gridRow.Cells[0].Value = unchecked(viewStart + (uint)(row * Columns)).ToString("X8");

                StringBuilder ascii = new StringBuilder(Columns);
                for (int column = 0; column < Columns; column++)
                {
                    int index = row * Columns + column;
                    byte value = shown[index];
                    ascii.Append(value >= 0x20 && value < 0x7F ? (char)value : '.');

                    DataGridViewCell cell = gridRow.Cells[column + 1];
                    if (grid.IsCurrentCellInEditMode && grid.CurrentCell == cell)
                    {
                        // Don't overwrite what is being typed.
                        continue;
                    }
                    cell.Value = value.ToString("X2");
                    bool recentlyChanged = (now - changedAt[index]).TotalMilliseconds < ChangedHighlightMs;
                    cell.Style.BackColor = recentlyChanged ? ChangedColor : Color.Empty;
                }
                gridRow.Cells[Columns + 1].Value = ascii.ToString();
            }
        }

        private void ShowUnreadable()
        {
            shown = null;
            viewStart = 0;
            for (int row = 0; row < Rows; row++)
            {
                DataGridViewRow gridRow = grid.Rows[row];
                gridRow.Cells[0].Value = "";
                for (int column = 1; column <= Columns + 1; column++)
                {
                    if (grid.IsCurrentCellInEditMode && grid.CurrentCell == gridRow.Cells[column])
                    {
                        continue;
                    }
                    gridRow.Cells[column].Value = column <= Columns ? "??" : "";
                    gridRow.Cells[column].Style.BackColor = Color.Empty;
                }
            }
        }

        private void grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Only bytes on screen can be edited.
            if (!IsByteColumn(e.ColumnIndex) || shown == null)
            {
                e.Cancel = true;
            }
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (!IsByteColumn(e.ColumnIndex) || shown == null)
            {
                return;
            }

            int index = ByteIndex(e.RowIndex, e.ColumnIndex);
            object typed = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            string text = typed == null ? "" : typed.ToString().Trim();

            byte value;
            if (!byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                statusLine.Error("Type a byte as two hex digits, for example 3F.");
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = shown[index].ToString("X2");
                return;
            }

            if (value == shown[index])
            {
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = value.ToString("X2");
                return;
            }

            uint address = unchecked(viewStart + (uint)index);
            try
            {
                api.WriteMemory(api.getCurrentPID(), address, 1, new byte[] { value });
                shown[index] = value;
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = value.ToString("X2");
                statusLine.Info($"Wrote {value:X2} to {address:X8}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't write to {address:X8}: {ex.Message}");
            }
        }

        private void grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || !IsByteColumn(e.ColumnIndex) || shown == null)
            {
                return;
            }

            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            uint address = unchecked(viewStart + (uint)ByteIndex(e.RowIndex, e.ColumnIndex));

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add($"Address {address:X8}").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            foreach (string type in new[] { "Int32", "Int16", "Byte", "Float", "Pointer" })
            {
                string watchType = type;
                menu.Items.Add($"Add watch as {type}", null, (s, args) => AddWatch(address, watchType));
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Copy address", null, (s, args) => CopyAddress(address));
            menu.Show(Cursor.Position);
        }

        private void AddWatch(uint address, string type)
        {
            MemoryForm memoryForm = openMemoryForm != null ? openMemoryForm() : null;
            if (memoryForm == null)
            {
                statusLine.Error("Couldn't open Memory Utilities.");
                return;
            }
            memoryForm.AddWatchAt(address, type);
        }

        private void CopyAddress(uint address)
        {
            try
            {
                Clipboard.SetText(address.ToString("X"));
                statusLine.Info($"Copied {address:X}.");
            }
            catch
            {
                statusLine.Error("Couldn't copy to the clipboard.");
            }
        }
    }
}
