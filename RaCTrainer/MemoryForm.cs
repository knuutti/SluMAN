using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace racman
{
    /// <summary>
    /// Watches, edits and freezes values at memory addresses, with named watchlists per game.
    /// </summary>
    public partial class MemoryForm : Form
    {
        public class WatchedAddress
        {
            // What the user typed: a plain address or a pointer chain.
            public AddressExpression expression;
            // Where the value is right now. For a pointer chain this moves as the pointers change.
            public uint address;
            // False while a pointer on the chain is null.
            public bool resolved = true;
            public int subID = -1;
            public uint size;
            public bool isFloat;
            public bool hexRepresented;
            public bool isFrozen;
            public int freezeSub = -1;
            // The big-endian bytes the freeze holds, so it can be set up again after a reboot.
            public byte[] freezeBytes;
            public string type;
            public string name;
            // The last value shown, without the frozen marker.
            public string lastValue = "";
        }

        private const string FrozenMarker = "❄ ";

        private StatusLine statusLine;

        // Pointer chains can't use subscriptions, since the address they end at moves, so they're
        // followed on this timer instead.
        private readonly Timer pointerTimer = new Timer();
        private const int PointerPollIntervalMs = 100;

        private readonly ToolTip addressToolTip = new ToolTip();

        // Always the current connection, which changes when SluMAN attaches again.
        private static IPS3API api => func.api;

        /// <summary>
        /// Opens the memory window, or brings it forward when it's already open. It's owned by
        /// <paramref name="owner"/>, so it closes with that game window.
        /// </summary>
        public static void ShowFor(Form owner)
        {
            MemoryForm memoryForm = Application.OpenForms["MemoryForm"] as MemoryForm;
            if (memoryForm != null)
            {
                memoryForm.Activate();
                return;
            }

            memoryForm = new MemoryForm();
            memoryForm.Show(owner);
        }

        public MemoryForm()
        {
            InitializeComponent();
            // With no game form open (unsupported game), this window shows the app-wide messages.
            statusLine = new StatusLine(this, AttachPS3Form.notSupported);
            func.BindEnter(registerAddressTextBox, addMemoryWatchButton);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            watchedMemoryAddressesListView.DoubleBuffering(true);
            watchedMemoryAddressesListView.AfterLabelEdit += watchedMemoryAddressesListView_AfterLabelEdit;
            watchedMemoryAddressesListView.ShowItemToolTips = true;
            GameReconnect.GameReconnected += GameReconnect_GameReconnected;

            addressToolTip.SetToolTip(registerAddressTextBox,
                "A hex address such as 7B4CE0, or a pointer chain:\n" +
                "[5EC654]+168 reads the 4-byte big-endian pointer at 5EC654 and adds 168.\n" +
                "Chains can be nested: [[5EC654]+44]+130.");

            pointerTimer.Interval = PointerPollIntervalMs;
            pointerTimer.Tick += pointerTimer_Tick;
            pointerTimer.Start();
        }

        /// <summary>Reads a 4-byte big-endian pointer.</summary>
        private static uint ReadPointer(uint address)
        {
            byte[] bytes = api.ReadMemory(api.getCurrentPID(), address, 4);
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }

        /// <summary>
        /// Finds where a pointer watch points now. A frozen watch moves its freeze along with it.
        /// Returns false when a pointer on the chain is null.
        /// </summary>
        private bool ResolvePointerWatch(ListViewItem item, WatchedAddress watched)
        {
            uint address;
            bool resolved;
            try
            {
                resolved = watched.expression.TryResolve(ReadPointer, out address);
            }
            catch
            {
                resolved = false;
                address = 0;
            }

            watched.resolved = resolved;
            if (!resolved)
            {
                item.ToolTipText = "A pointer on the chain is null right now.";
                return false;
            }

            if (address != watched.address)
            {
                watched.address = address;
                if (watched.isFrozen && watched.freezeBytes != null)
                {
                    try
                    {
                        Freeze(watched, watched.freezeBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Couldn't move the freeze for {watched.expression}: {ex.Message}");
                    }
                }
            }
            item.ToolTipText = $"{watched.expression} -> {address:X}";
            return true;
        }

        private void pointerTimer_Tick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in watchedMemoryAddressesListView.Items)
            {
                WatchedAddress watched = Watched(item);
                if (watched == null || watched.expression.IsStatic)
                {
                    continue;
                }

                if (!ResolvePointerWatch(item, watched))
                {
                    SetItemValueText(item, "N/A");
                    continue;
                }

                try
                {
                    byte[] current = api.ReadMemory(api.getCurrentPID(), watched.address, watched.size);
                    SetItemValueText(item, FormatValue(watched, current.Reverse().ToArray()));
                }
                catch
                {
                    SetItemValueText(item, "N/A");
                }
            }
        }

        private static bool SupportsSubscriptions => !(api is WebMAN);

        private static WatchedAddress Watched(ListViewItem item)
        {
            return item == null ? null : item.Tag as WatchedAddress;
        }

        /// <summary>
        /// Shows a value in a row. Safe to call from the subscription thread.
        /// </summary>
        private void SetItemValueText(ListViewItem item, string value)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            Action update = () =>
            {
                WatchedAddress watched = Watched(item);
                if (watched == null || item.ListView == null)
                {
                    return;
                }
                watched.lastValue = value;
                item.SubItems[2].Text = (watched.isFrozen ? FrozenMarker : "") + value;
            };

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(update);
                }
                catch
                {
                    // The window closed in between.
                }
            }
            else
            {
                update();
            }
        }

        /// <summary>
        /// Formats a value. <paramref name="bytes"/> are little-endian, as subscriptions deliver them.
        /// </summary>
        private static string FormatValue(WatchedAddress watched, byte[] bytes)
        {
            if (watched.isFloat)
            {
                return BitConverter.ToSingle(bytes, 0).ToString(CultureInfo.InvariantCulture);
            }

            long value;
            switch (watched.size)
            {
                case 2:
                    value = BitConverter.ToInt16(bytes, 0);
                    break;
                case 4:
                    value = BitConverter.ToInt32(bytes, 0);
                    break;
                case 8:
                    value = BitConverter.ToInt64(bytes, 0);
                    break;
                default:
                    value = bytes[0];
                    break;
            }

            if (!watched.hexRepresented)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            // Only the value's own bytes, so a negative Int16 isn't shown as 16 hex digits.
            ulong mask = watched.size >= 8 ? ulong.MaxValue : (1UL << (int)(watched.size * 8)) - 1;
            return ((ulong)value & mask).ToString("X");
        }

        private static void SetSizeForType(WatchedAddress watched)
        {
            switch (watched.type)
            {
                case "Int32":
                    watched.size = 4;
                    break;
                case "Int64":
                    watched.size = 8;
                    break;
                case "Int16":
                    watched.size = 2;
                    break;
                case "Byte":
                    watched.size = 1;
                    break;
                case "Float":
                    watched.size = 4;
                    watched.isFloat = true;
                    break;
                default:
                    // "Pointer", and any unknown type from an old watchlist.
                    watched.size = 4;
                    watched.hexRepresented = true;
                    break;
            }
        }

        private void AddMemoryWatch(AddressExpression expression, string type, string name)
        {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(expression.ToString());
            item.SubItems.Add("Waiting...");

            WatchedAddress watched = new WatchedAddress
            {
                expression = expression,
                type = type,
                name = name
            };
            SetSizeForType(watched);

            item.Tag = watched;
            watchedMemoryAddressesListView.Items.Add(item);

            if (expression.IsStatic)
            {
                uint address;
                expression.TryResolve(ReadPointer, out address);
                watched.address = address;
                Subscribe(item);
            }
            // Pointer chains are followed by pointerTimer.
        }

        /// <summary>
        /// Starts updating a row as its value changes.
        /// </summary>
        private void Subscribe(ListViewItem item)
        {
            WatchedAddress watched = Watched(item);
            try
            {
                watched.subID = api.SubMemory(api.getCurrentPID(), watched.address, watched.size, (byte[] bytes) =>
                {
                    SetItemValueText(item, FormatValue(watched, bytes));
                });

                // Subscriptions only report changes, so read once to show the current value.
                byte[] current = api.ReadMemory(api.getCurrentPID(), watched.address, watched.size);
                SetItemValueText(item, FormatValue(watched, current.Reverse().ToArray()));
            }
            catch (Exception ex)
            {
                watched.subID = -1;
                SetItemValueText(item, "N/A");
                statusLine.Error($"Couldn't watch 0x{watched.address:X}: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Stops a row's subscription and its freeze, if any.
        /// </summary>
        private static void Release(WatchedAddress watched)
        {
            if (watched.subID != -1)
            {
                try { api.ReleaseSubID(watched.subID); } catch { }
                watched.subID = -1;
            }
            if (watched.freezeSub != -1)
            {
                try { api.ReleaseSubID(watched.freezeSub); } catch { }
                watched.freezeSub = -1;
            }
        }

        private void ReleaseAll()
        {
            foreach (ListViewItem item in watchedMemoryAddressesListView.Items)
            {
                WatchedAddress watched = Watched(item);
                if (watched != null)
                {
                    Release(watched);
                }
            }
        }

        /// <summary>
        /// A game reboot drops every subscription, so set the watches and freezes up again.
        /// </summary>
        private void GameReconnect_GameReconnected()
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    foreach (ListViewItem item in watchedMemoryAddressesListView.Items)
                    {
                        WatchedAddress watched = Watched(item);
                        if (watched == null)
                        {
                            continue;
                        }
                        // The old IDs belong to the dropped session.
                        watched.subID = -1;
                        watched.freezeSub = -1;
                        if (watched.expression.IsStatic)
                        {
                            Subscribe(item);
                        }
                        else if (!ResolvePointerWatch(item, watched))
                        {
                            // Its freeze is set up again once the chain resolves.
                            continue;
                        }
                        if (watched.isFrozen && watched.freezeBytes != null)
                        {
                            Freeze(watched, watched.freezeBytes);
                        }
                    }
                }));
            }
            catch
            {
                // The window closed in between.
            }
        }

        private void addMemoryWatchButton_Click(object sender, EventArgs e)
        {
            if (!SupportsSubscriptions)
            {
                statusLine.Error("Memory watches need Ratchetron or RPCS3. The old WebMAN API can't watch memory.");
                return;
            }

            AddressExpression expression;
            string error;
            if (!AddressExpression.TryParse(registerAddressTextBox.Text, out expression, out error))
            {
                statusLine.Error(error);
                return;
            }

            AddMemoryWatch(expression, registerAddressTypeCombo.Text, "New watch");
            SaveCurrentWatchlist();

            // Let the user name it right away.
            ListViewItem added = watchedMemoryAddressesListView.Items[watchedMemoryAddressesListView.Items.Count - 1];
            added.EnsureVisible();
            added.BeginEdit();
        }

        private void watchedMemoryAddressesListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
            {
                // Editing was cancelled.
                return;
            }

            // Commas separate the fields in a watchlist file.
            string name = e.Label.Replace(",", " ").Trim();
            if (name == "")
            {
                e.CancelEdit = true;
                return;
            }

            if (name != e.Label)
            {
                e.CancelEdit = true;
                watchedMemoryAddressesListView.Items[e.Item].Text = name;
            }

            WatchedAddress watched = Watched(watchedMemoryAddressesListView.Items[e.Item]);
            if (watched != null)
            {
                watched.name = name;
            }

            // The new text is applied after this event returns, so save once it has been.
            BeginInvoke(new Action(SaveCurrentWatchlist));
        }

        private void MemoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            pointerTimer.Stop();
            pointerTimer.Dispose();
            addressToolTip.Dispose();
            GameReconnect.GameReconnected -= GameReconnect_GameReconnected;
            ReleaseAll();

            if (AttachPS3Form.notSupported)
            {
                Application.Exit();
            }
        }

        private void watchedMemoryAddressesListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            ListViewItem focusedItem = watchedMemoryAddressesListView.FocusedItem;
            if (focusedItem == null || !focusedItem.Bounds.Contains(e.Location))
            {
                return;
            }

            WatchedAddress watched = Watched(focusedItem);
            ContextMenuStrip menuStrip = new ContextMenuStrip();
            menuStrip.Items.Add("Edit value...", null, MenuStripEditValue_Click);
            menuStrip.Items.Add(watched != null && watched.isFrozen ? "Unfreeze" : "Freeze", null, MenuStripEditValue_Freeze);
            menuStrip.Items.Add("Rename", null, (s, args) => focusedItem.BeginEdit());
            menuStrip.Items.Add("Copy address", null, MenuStripCopyAddress_Click);
            menuStrip.Items.Add(new ToolStripSeparator());
            menuStrip.Items.Add("Delete", null, MenuStripDelete_Click);
            menuStrip.Show(Cursor.Position);
        }

        /// <summary>
        /// Holds a watch at the given big-endian bytes.
        /// </summary>
        private void Freeze(WatchedAddress watched, byte[] bigEndianBytes)
        {
            if (watched.freezeSub != -1)
            {
                try { api.ReleaseSubID(watched.freezeSub); } catch { }
            }
            watched.freezeSub = api.FreezeMemory(api.getCurrentPID(), watched.address, watched.size, IPS3API.MemoryCondition.Any, bigEndianBytes);
            watched.freezeBytes = bigEndianBytes;
            watched.isFrozen = true;
        }

        private void MenuStripEditValue_Freeze(object sender, EventArgs e)
        {
            ListViewItem focusedItem = watchedMemoryAddressesListView.FocusedItem;
            WatchedAddress watched = Watched(focusedItem);
            if (watched == null)
            {
                return;
            }

            try
            {
                if (watched.isFrozen)
                {
                    if (watched.freezeSub != -1)
                    {
                        api.ReleaseSubID(watched.freezeSub);
                    }
                    watched.freezeSub = -1;
                    watched.freezeBytes = null;
                    watched.isFrozen = false;
                }
                else
                {
                    if (!watched.expression.IsStatic && !ResolvePointerWatch(focusedItem, watched))
                    {
                        statusLine.Error("A pointer on the chain is null right now, so there's nothing to freeze.");
                        return;
                    }
                    // Hold the value it has right now.
                    byte[] current = api.ReadMemory(api.getCurrentPID(), watched.address, watched.size);
                    Freeze(watched, current);
                }
                SetItemValueText(focusedItem, watched.lastValue);
            }
            catch (Exception ex)
            {
                watched.isFrozen = false;
                watched.freezeSub = -1;
                statusLine.Error($"Couldn't freeze 0x{watched.address:X}: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private void MenuStripEditValue_Click(object sender, EventArgs e)
        {
            ListViewItem focusedItem = watchedMemoryAddressesListView.FocusedItem;
            WatchedAddress watched = Watched(focusedItem);
            if (watched == null)
            {
                return;
            }

            if (!watched.expression.IsStatic && !ResolvePointerWatch(focusedItem, watched))
            {
                statusLine.Error("A pointer on the chain is null right now, so there's nothing to edit.");
                return;
            }

            SimpleInputDialogForm inputDialog = new SimpleInputDialogForm("Edit value", watched.lastValue);
            if (inputDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string text = inputDialog.inputTextBox.Text.Trim();
            try
            {
                byte[] littleEndian;
                if (watched.isFloat)
                {
                    littleEndian = BitConverter.GetBytes(float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
                }
                else if (watched.hexRepresented)
                {
                    littleEndian = BitConverter.GetBytes(long.Parse(text.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else
                {
                    littleEndian = BitConverter.GetBytes(long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture));
                }

                byte[] bigEndian = littleEndian.Take((int)watched.size).Reverse().ToArray();
                api.WriteMemory(api.getCurrentPID(), watched.address, watched.size, bigEndian);

                // A freeze would put the old value straight back, so hold the new one instead.
                if (watched.isFrozen)
                {
                    Freeze(watched, bigEndian);
                }
            }
            catch (FormatException)
            {
                if (watched.isFloat)
                {
                    statusLine.Error("Enter a number, for example 1.5.");
                }
                else if (watched.hexRepresented)
                {
                    statusLine.Error("Enter a hexadecimal value, for example 7B4CE0.");
                }
                else
                {
                    statusLine.Error("Enter a whole number.");
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't write to 0x{watched.address:X}: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private void MenuStripCopyAddress_Click(object sender, EventArgs e)
        {
            WatchedAddress watched = Watched(watchedMemoryAddressesListView.FocusedItem);
            if (watched == null)
            {
                return;
            }
            try
            {
                Clipboard.SetText(watched.address.ToString("X"));
                statusLine.Info($"Copied {watched.address:X}.");
            }
            catch
            {
                statusLine.Error("Couldn't copy to the clipboard.");
            }
        }

        private void MenuStripDelete_Click(object sender, EventArgs e)
        {
            ListViewItem focusedItem = watchedMemoryAddressesListView.FocusedItem;
            WatchedAddress watched = Watched(focusedItem);
            if (watched == null)
            {
                return;
            }

            Release(watched);
            watchedMemoryAddressesListView.Items.Remove(focusedItem);
            SaveCurrentWatchlist();
        }

        // --- Watchlists ---
        // Each game has its own watchlists in watchlists/{TITLEID}-{name}.mw. The list in use is
        // remembered per game, loaded when the window opens and saved after every change.

        private const string DefaultWatchlistName = "Default";

        private string currentWatchlist = DefaultWatchlistName;

        // True while a list is being loaded, so loading it doesn't save it again.
        private bool loadingWatchlist;

        // Title IDs such as NPEA00343 are letters and digits, so they are safe in a config key.
        private static string CurrentWatchlistKey => "watchlist_" + (AttachPS3Form.game ?? "");

        private static string WatchlistsFolder => UserData.Path("watchlists");

        private static string WatchlistPath(string listName)
        {
            return Path.Combine(WatchlistsFolder, $"{AttachPS3Form.game}-{listName}.mw");
        }

        private void MemoryForm_Load(object sender, EventArgs e)
        {
            string remembered = "";
            try
            {
                remembered = func.GetConfigData("config.txt", CurrentWatchlistKey);
            }
            catch
            {
                // No readable config.txt.
            }
            SwitchToWatchlist(remembered == "" ? DefaultWatchlistName : remembered);
        }

        /// <summary>
        /// Makes <paramref name="listName"/> the list in use: loads it if it exists, selects it in
        /// the box and remembers it for this game.
        /// </summary>
        private void SwitchToWatchlist(string listName)
        {
            loadingWatchlist = true;
            try
            {
                ReleaseAll();
                watchedMemoryAddressesListView.Items.Clear();

                currentWatchlist = listName;
                if (File.Exists(WatchlistPath(listName)))
                {
                    PopulateWatchlistFromFile(WatchlistPath(listName));
                }

                UpdateWatchlists();
                savedWatchlistsComboBox.SelectedItem = listName;
            }
            finally
            {
                loadingWatchlist = false;
            }

            try
            {
                func.ChangeFileLines("config.txt", listName, CurrentWatchlistKey);
            }
            catch
            {
                // Not remembered; nothing else depends on it.
            }
        }

        /// <summary>Writes the watches to the list in use.</summary>
        private void SaveCurrentWatchlist()
        {
            if (loadingWatchlist || IsDisposed)
            {
                return;
            }

            Directory.CreateDirectory(WatchlistsFolder);
            SaveWatchListToFile(WatchlistPath(currentWatchlist));
        }

        private void UpdateWatchlists()
        {
            var watchlistItems = savedWatchlistsComboBox.Items;
            string gamePrefix = $"{AttachPS3Form.game}-";

            watchlistItems.Clear();

            // The list in use is always offered, even before its file exists.
            watchlistItems.Add(currentWatchlist);

            if (!Directory.Exists(WatchlistsFolder))
                return;

            // Only the current game's lists.
            foreach (string filePath in Directory.GetFiles(WatchlistsFolder, $"{gamePrefix}*.mw"))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (fileName.StartsWith(gamePrefix))
                    fileName = fileName.Substring(gamePrefix.Length);

                if (!watchlistItems.Contains(fileName))
                    watchlistItems.Add(fileName);
            }
        }

        private bool SaveWatchListToFile(string filename)
        {
            // One "name,0xADDRESS,type" line per watch.
            var watchedAddressesData = watchedMemoryAddressesListView.Items
                .Cast<ListViewItem>()
                .Select(item =>
                {
                    var watched = (WatchedAddress)item.Tag;
                    // A plain address keeps the old "0x7B4CE0" form; a chain is saved as typed.
                    string address = watched.expression.IsStatic ? $"0x{watched.address:X}" : watched.expression.ToString();
                    return $"{item.Text},{address},{watched.type}";
                })
                .ToList();

            try
            {
                File.WriteAllLines(filename, watchedAddressesData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                statusLine.Error("Couldn't save the watchlist. Check that the name has no \\ / : * ? \" < > | characters.");
                return false;
            }
        }

        private void PopulateWatchlistFromFile(string filename)
        {
            try
            {
                var lines = File.ReadAllLines(filename);

                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    if (parts.Length == 3)
                    {
                        AddressExpression expression;
                        string error;
                        if (!AddressExpression.TryParse(parts[1], out expression, out error))
                        {
                            statusLine.Error($"Skipped \"{parts[0]}\" in the watchlist. {error}");
                            continue;
                        }
                        AddMemoryWatch(expression, parts[2], parts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the watchlist: {ex.Message}");
            }
        }

        /// <summary>
        /// Save As: copies the watches into a new list named in the box and switches to it.
        /// </summary>
        private void saveWatchListButton_Click(object sender, EventArgs e)
        {
            string listName = savedWatchlistsComboBox.Text.Trim();
            if (listName == "")
            {
                statusLine.Error("Type a name for the new watchlist in the box first.");
                return;
            }
            if (listName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                statusLine.Error("A watchlist name can't contain \\ / : * ? \" < > | characters.");
                return;
            }

            Directory.CreateDirectory(WatchlistsFolder);
            if (!SaveWatchListToFile(WatchlistPath(listName)))
            {
                return;
            }

            // The file now holds these watches, so switching to it reloads the same list.
            SwitchToWatchlist(listName);
            statusLine.Info($"Saved as \"{listName}\". Changes are saved to it automatically.");
        }

        private void savedWatchlistsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingWatchlist || savedWatchlistsComboBox.SelectedItem == null)
            {
                return;
            }

            string listName = savedWatchlistsComboBox.SelectedItem.ToString();
            if (listName == currentWatchlist)
            {
                return;
            }

            // The list being left is already saved, since every change saves it.
            SwitchToWatchlist(listName);
        }
    }
}
