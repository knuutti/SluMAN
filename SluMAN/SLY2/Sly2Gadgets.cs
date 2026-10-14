using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SluMAN
{
    public partial class Sly2Gadgets : Form, IRefreshOnShow
    {
        private sly2 game;
        private ItemCheckEventHandler slyItemCheckHandler;
        private ItemCheckEventHandler bentleyItemCheckHandler;
        private ItemCheckEventHandler murrayItemCheckHandler;

        private StatusLine statusLine;

        public Sly2Gadgets(sly2 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, false);
            LoadGadgets();
            PopulateBindingComboBoxes();
            LoadGadgetBindings();

            slyItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateSlyBindingComboBoxes())); };
            bentleyItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateBentleyBindingComboBoxes())); };
            murrayItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateMurrayBindingComboBoxes())); };

            // Wire up events
            slyGadgetsCheckedList.ItemCheck += slyItemCheckHandler;
            bentleyGadgetsCheckedList.ItemCheck += bentleyItemCheckHandler;
            murrayGadgetsCheckedList.ItemCheck += murrayItemCheckHandler;
        }

        private void slyGadgetsToggleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SuspendComboBoxUpdates();
            for (var i = 0; i < slyGadgetsCheckedList.Items.Count; i++)
            {
                slyGadgetsCheckedList.SetItemChecked(i, slyGadgetsToggleCheckBox.Checked);
            }
            ResumeComboBoxUpdates();
            PopulateSlyBindingComboBoxes();
        }

        private void PopulateBindingComboBoxes()
        {
            PopulateSlyBindingComboBoxes();
            PopulateBentleyBindingComboBoxes();
            PopulateMurrayBindingComboBoxes();
        }

        // Populate Sly binding comboboxes with checked gadgets
        private void PopulateSlyBindingComboBoxes()
        {
            var checkedGadgets = GetCheckedGadgetNames(slyGadgetsCheckedList)
                .Concat(GetCheckedGadgetNames(vaultGadgetsCheckedList))
                .ToList();

            PopulateComboBox(slyGadgetsL1ComboBox, checkedGadgets);
            PopulateComboBox(slyGadgetsL2ComboBox, checkedGadgets);
            PopulateComboBox(slyGadgetsR2ComboBox, checkedGadgets);
        }

        // Populate Bentley binding comboboxes with checked gadgets
        private void PopulateBentleyBindingComboBoxes()
        {
            var checkedGadgets = GetCheckedGadgetNames(bentleyGadgetsCheckedList);

            PopulateComboBox(bentleyGadgetsR2ComboBox, checkedGadgets);
            PopulateComboBox(bentleyGadgetsL2ComboBox, checkedGadgets);
            PopulateComboBox(bentleyGadgetsL1ComboBox, checkedGadgets);
        }

        // Populate Murray binding comboboxes with checked gadgets
        private void PopulateMurrayBindingComboBoxes()
        {
            var checkedGadgets = GetCheckedGadgetNames(murrayGadgetsCheckedList);

            PopulateComboBox(murrayGadgetsR2ComboBox, checkedGadgets);
            PopulateComboBox(murrayGadgetsL2ComboBox, checkedGadgets);
            PopulateComboBox(murrayGadgetsL1ComboBox, checkedGadgets);
        }

        private void PopulateComboBox(ComboBox comboBox, List<string> gadgetNames)
        {
            string currentSelection = comboBox.SelectedItem?.ToString();

            comboBox.Items.Clear();
            comboBox.Items.Add("None"); // Add "None" option to unbind

            foreach (var gadgetName in gadgetNames)
            {
                comboBox.Items.Add(gadgetName);
            }

            // Restore previous selection if it still exists
            if (!string.IsNullOrEmpty(currentSelection) && comboBox.Items.Contains(currentSelection))
            {
                comboBox.SelectedItem = currentSelection;
            }
            else
            {
                comboBox.SelectedIndex = 0; // Default to "None"
            }
        }

        private void SuspendComboBoxUpdates()
        {
            slyGadgetsCheckedList.ItemCheck -= slyItemCheckHandler;
            bentleyGadgetsCheckedList.ItemCheck -= bentleyItemCheckHandler;
            murrayGadgetsCheckedList.ItemCheck -= murrayItemCheckHandler;
        }

        // Re-hook event handlers after bulk operations
        private void ResumeComboBoxUpdates()
        {
            slyGadgetsCheckedList.ItemCheck += slyItemCheckHandler;
            bentleyGadgetsCheckedList.ItemCheck += bentleyItemCheckHandler;
            murrayGadgetsCheckedList.ItemCheck += murrayItemCheckHandler;
        }

        private void SaveGadgetsToRunFile()
        {
            string episodeKey = GetEpisodeKey(runFileComboBox.SelectedItem.ToString());

            // Get current gadget states (same logic as SaveGadgets())
            var gadgetBytes = GetCurrentGadgetBytes();
            var bindingBytes = GetCurrentBindingBytes();

            // Save to config
            func.ChangeFileLines("config.txt",
                BitConverter.ToString(gadgetBytes).Replace("-", ""),
                episodeKey + "_Sly2GadgetUnlocks");

            func.ChangeFileLines("config.txt",
                BitConverter.ToString(bindingBytes).Replace("-", ""),
                episodeKey + "_Sly2GadgetBindings");
        }

        private string GetEpisodeKey(string episodeName)
        {
            switch (episodeName)
            {
                case "Episode 1": return "Episode1";
                case "Episode 2": return "Episode2";
                case "Episode 3": return "Episode3";
                case "Episode 4": return "Episode4";
                case "Episode 5": return "Episode5";
                case "Episode 6": return "Episode6";
                case "Episode 7": return "Episode7";
                case "Episode 8": return "Episode8";
                default: return "Episode1"; // fallback
            }
        }

        private void LoadGadgetBindings()
        {
            // Load Sly bindings (L1=0, L2=1, R2=2)
            LoadBindingToComboBox(slyGadgetsL1ComboBox, sly2.addr.gadgetBindsSly, 0);
            LoadBindingToComboBox(slyGadgetsL2ComboBox, sly2.addr.gadgetBindsSly, 1);
            LoadBindingToComboBox(slyGadgetsR2ComboBox, sly2.addr.gadgetBindsSly, 2);

            // Load Bentley bindings
            LoadBindingToComboBox(bentleyGadgetsL1ComboBox, sly2.addr.gadgetBindsBentley, 0);
            LoadBindingToComboBox(bentleyGadgetsL2ComboBox, sly2.addr.gadgetBindsBentley, 1);
            LoadBindingToComboBox(bentleyGadgetsR2ComboBox, sly2.addr.gadgetBindsBentley, 2);

            // Load Murray bindings
            LoadBindingToComboBox(murrayGadgetsL1ComboBox, sly2.addr.gadgetBindsMurray, 0);
            LoadBindingToComboBox(murrayGadgetsL2ComboBox, sly2.addr.gadgetBindsMurray, 1);
            LoadBindingToComboBox(murrayGadgetsR2ComboBox, sly2.addr.gadgetBindsMurray, 2);
        }

        private void LoadBindingToComboBox(ComboBox comboBox, uint baseAddress, int buttonOffset)
        {
            SelectBinding(comboBox, game.GetGadgetBinding(baseAddress, buttonOffset));
        }

        private void SelectBinding(ComboBox comboBox, int buttonBindingIndex)
        {
            // Find gadget with this binding index
            string gadgetName = FindGadgetByBindingIndex(buttonBindingIndex);

            if (gadgetName != null && comboBox.Items.Contains(gadgetName))
            {
                comboBox.SelectedItem = gadgetName;
            }
            else
            {
                comboBox.SelectedIndex = 0; // Select "None"
            }
        }

        private string FindGadgetByBindingIndex(int buttonBindingIndex)
        {
            if (buttonBindingIndex == -1)
            {
                return null; // Unbound
            }

            foreach (var kvp in AllGadgets)
            {
                if (kvp.Value.ButtonBindingIndex == buttonBindingIndex)
                {
                    return kvp.Key;
                }
            }

            return null; // Not found
        }

        /// <summary>
        /// Reads the unlocks and bindings again, since the tab keeps this form while saves are
        /// loaded. Unsaved changes are dropped, as closing the old gadget window did.
        /// </summary>
        public void RefreshFromGame()
        {
            SuspendComboBoxUpdates();
            try
            {
                LoadGadgets();
                PopulateBindingComboBoxes();
                LoadGadgetBindings();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the gadgets: {ex.Message}");
            }
            finally
            {
                ResumeComboBoxUpdates();
            }
        }

        public void LoadGadgets()
        {
            byte[] gadgetBytes = game.GetGadgetUnlocks();

            LoadGadgetsToList(slyGadgetsCheckedList, gadgetBytes);
            LoadGadgetsToList(bentleyGadgetsCheckedList, gadgetBytes);
            LoadGadgetsToList(murrayGadgetsCheckedList, gadgetBytes);
            LoadGadgetsToList(vaultGadgetsCheckedList, gadgetBytes);
        }

        private void LoadGadgetsToList(CheckedListBox checkedListBox, byte[] gadgetBytes)
        {
            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                string gadgetName = checkedListBox.Items[i].ToString();

                if (AllGadgets.ContainsKey(gadgetName))
                {
                    int bitIndex = AllGadgets[gadgetName].UnlockBitIndex;
                    bool isUnlocked = GetBit(gadgetBytes, bitIndex);
                    checkedListBox.SetItemChecked(i, isUnlocked);
                }
            }
        }

        private byte[] GetCurrentGadgetBytes()
        {
            var updatedGadgetStates = GetGadgetStates(slyGadgetsCheckedList)
                .Concat(GetGadgetStates(bentleyGadgetsCheckedList))
                .Concat(GetGadgetStates(murrayGadgetsCheckedList));

            // Gadget states are stored as bits in 8-byte array
            // Default values: FE 00 02 00 00 02 00 00
            var gadgetBytes = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var gadget in updatedGadgetStates)
            {
                if (AllGadgets.ContainsKey(gadget.Name))
                {
                    int bitIndex = AllGadgets[gadget.Name].UnlockBitIndex;
                    SetBit(gadgetBytes, bitIndex, gadget.IsUnlocked);
                }
            }

            return gadgetBytes;
        }

        private List<GadgetState> GetGadgetStates(CheckedListBox checkedListBox)
        {
            var gadgetStates = new List<GadgetState>();

            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                string gadgetName = checkedListBox.Items[i].ToString();
                bool isUnlocked = checkedListBox.GetItemChecked(i);

                gadgetStates.Add(new GadgetState
                {
                    Name = gadgetName,
                    IsUnlocked = isUnlocked
                });
            }

            return gadgetStates;
        }

        private class GadgetState
        {
            public string Name { get; set; }
            public bool IsUnlocked { get; set; }
        }

        private class GadgetInfo
        {
            public string Name { get; set; }
            public int UnlockBitIndex { get; set; }
            public int? ButtonBindingIndex { get; set; }
        }

        private static readonly Dictionary<string, GadgetInfo> AllGadgets = new Dictionary<string, GadgetInfo>
        {
            { "Smoke Bomb", new GadgetInfo { Name = "Smoke Bomb", UnlockBitIndex = 17, ButtonBindingIndex = 23 } },
            { "Combat Dodge", new GadgetInfo { Name = "Combat Dodge", UnlockBitIndex = 32, ButtonBindingIndex = 24 } },
            { "Stealth Slide", new GadgetInfo { Name = "Stealth Slide", UnlockBitIndex = 31, ButtonBindingIndex = 25 } },
            { "Alarm Clock", new GadgetInfo { Name = "Alarm Clock", UnlockBitIndex = 30, ButtonBindingIndex = 26 } },
            { "Paraglider", new GadgetInfo { Name = "Paraglider", UnlockBitIndex = 29, ButtonBindingIndex = null } },
            { "Silent Obliteration", new GadgetInfo { Name = "Silent Obliteration", UnlockBitIndex = 28, ButtonBindingIndex = null } },
            { "Thief Reflexes", new GadgetInfo { Name = "Thief Reflexes", UnlockBitIndex = 27, ButtonBindingIndex = 29 } },
            { "Feral Pounce", new GadgetInfo { Name = "Feral Pounce", UnlockBitIndex = 26, ButtonBindingIndex = 30 } },

            { "Trigger Bomb", new GadgetInfo { Name = "Trigger Bomb", UnlockBitIndex = 8, ButtonBindingIndex = 7 } },
            { "Size Destabilizer", new GadgetInfo { Name = "Size Destabilizer", UnlockBitIndex = 16, ButtonBindingIndex = 8 } },
            { "Snooze Bomb", new GadgetInfo { Name = "Snooze Bomb", UnlockBitIndex = 15, ButtonBindingIndex = 9 } },
            { "Adrenaline Burst", new GadgetInfo { Name = "Adrenaline Burst", UnlockBitIndex = 14, ButtonBindingIndex = 10 } },
            { "Health Extractor", new GadgetInfo { Name = "Health Extractor", UnlockBitIndex = 13, ButtonBindingIndex = 11 } },
            { "Hoverpack", new GadgetInfo { Name = "Hoverpack", UnlockBitIndex = 12, ButtonBindingIndex = 12 } },
            { "Reduction Bomb", new GadgetInfo { Name = "Reduction Bomb", UnlockBitIndex = 11, ButtonBindingIndex = 13 } },
            { "Temporal Lock", new GadgetInfo { Name = "Temporal Lock", UnlockBitIndex = 10, ButtonBindingIndex = 14 } },

            { "Fists of Flame", new GadgetInfo { Name = "Fists of Flame", UnlockBitIndex = 9, ButtonBindingIndex = 15 } },
            { "Turnbuckle Launch", new GadgetInfo { Name = "Turnbuckle Launch", UnlockBitIndex = 24, ButtonBindingIndex = 16 } },
            { "Juggernaut Throw", new GadgetInfo { Name = "Juggernaut Throw", UnlockBitIndex = 23, ButtonBindingIndex = null } },
            { "Atlas Strength", new GadgetInfo { Name = "Atlas Strength", UnlockBitIndex = 22, ButtonBindingIndex = null } },
            { "Raging Inferno Flop", new GadgetInfo { Name = "Raging Inferno Flop", UnlockBitIndex = 18, ButtonBindingIndex = 22 } },
            { "Berserker Charge", new GadgetInfo { Name = "Berserker Charge", UnlockBitIndex = 20, ButtonBindingIndex = 20 } },
            { "Guttural Roar", new GadgetInfo { Name = "Guttural Roar", UnlockBitIndex = 19, ButtonBindingIndex = 21 } },
            { "Diablo Fire Slam", new GadgetInfo { Name = "Diablo Fire Slam", UnlockBitIndex = 21, ButtonBindingIndex = 19 } },

            { "Knockout Dive", new GadgetInfo { Name = "Knockout Dive", UnlockBitIndex = 39, ButtonBindingIndex = 33 } },
            { "Insanity Strike", new GadgetInfo { Name = "Insanity Strike", UnlockBitIndex = 38, ButtonBindingIndex = 34 } },
            { "Voltage Attack", new GadgetInfo { Name = "Voltage Attack", UnlockBitIndex = 37, ButtonBindingIndex = 35 } },
            { "Long Toss", new GadgetInfo { Name = "Long Toss", UnlockBitIndex = 36, ButtonBindingIndex = null } },
            { "Rage Bomb", new GadgetInfo { Name = "Rage Bomb", UnlockBitIndex = 35, ButtonBindingIndex = 37 } },
            { "Music Box", new GadgetInfo { Name = "Music Box", UnlockBitIndex = 34, ButtonBindingIndex = 38 } },
            { "Lightning Spin", new GadgetInfo { Name = "Lightning Spin", UnlockBitIndex = 33, ButtonBindingIndex = 39 } },
            { "Shadow Power", new GadgetInfo { Name = "Shadow Power", UnlockBitIndex = 48, ButtonBindingIndex = 40 } }
        };

        private bool GetBit(byte[] bytes, int bitIndex)
        {
            int byteIndex = (bitIndex - 1) / 8;
            int bitPosition = 7 - (bitIndex - 1) % 8;
            return (bytes[byteIndex] & (1 << bitPosition)) != 0;
        }

        private void SetBit(byte[] bytes, int bitIndex, bool state)
        {

            int byteIndex = (bitIndex - 1) / 8;
            int bitPosition = 7 - (bitIndex - 1) % 8;

            if (state)
            {
                bytes[byteIndex] |= (byte)(1 << bitPosition);  // Set bit to 1
            }
            else
            {
                bytes[byteIndex] &= (byte)~(1 << bitPosition); // Clear bit to 0
            }
        }

        private List<string> GetCheckedGadgetNames(CheckedListBox checkedListBox)
        {
            var gadgetNames = new List<string>();

            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                if (checkedListBox.GetItemChecked(i))
                {
                    string gadgetName = checkedListBox.Items[i].ToString();

                    // Only include gadgets that exist in AllGadgets and have valid binding index
                    if (AllGadgets.ContainsKey(gadgetName) && AllGadgets[gadgetName].ButtonBindingIndex > 0)
                    {
                        gadgetNames.Add(gadgetName);
                    }
                }
            }

            return gadgetNames;
        }

        private byte[] GetCurrentBindingBytes()
        {
            byte[] gadgetBindingBytes = new byte[48];

            // Read the binding values for all comboboxes
            var bindingCombos = new List<ComboBox>
            {
                slyGadgetsL1ComboBox, slyGadgetsL2ComboBox, slyGadgetsR2ComboBox,
                bentleyGadgetsL1ComboBox, bentleyGadgetsL2ComboBox, bentleyGadgetsR2ComboBox,
                murrayGadgetsL1ComboBox, murrayGadgetsL2ComboBox, murrayGadgetsR2ComboBox
            };

            for (int i = 0; i < bindingCombos.Count; i++)
            {
                string selectedGadget = bindingCombos[i].SelectedItem?.ToString();

                int buttonBindingIndex = -1; // Default to -1 (unbound)

                if (!string.IsNullOrEmpty(selectedGadget) && AllGadgets.ContainsKey(selectedGadget))
                {
                    buttonBindingIndex = AllGadgets[selectedGadget].ButtonBindingIndex ?? -1;
                }

                // Each binding is stored as a 4-byte integer
                byte[] bindingBytes = BitConverter.GetBytes(buttonBindingIndex);
                // reverse for big-endian
                Array.Reverse(bindingBytes);
                Array.Copy(bindingBytes, 0, gadgetBindingBytes, i * 4, 4);
            }

            return gadgetBindingBytes;
        }

        private void Sly2Gadgets_Load(object sender, EventArgs e)
        {

        }

        private void bentleyGadgetsToggleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SuspendComboBoxUpdates();
            for (var i = 0; i < bentleyGadgetsCheckedList.Items.Count; i++)
            {
                bentleyGadgetsCheckedList.SetItemChecked(i, bentleyGadgetsToggleCheckBox.Checked);
            }
            ResumeComboBoxUpdates();
            PopulateBentleyBindingComboBoxes();
        }

        private void SaveGadgets()
        {
            var updatedGadgetStates = GetGadgetStates(slyGadgetsCheckedList)
                .Concat(GetGadgetStates(bentleyGadgetsCheckedList))
                .Concat(GetGadgetStates(murrayGadgetsCheckedList))
                .Concat(GetGadgetStates(vaultGadgetsCheckedList));

            // Gadget states are stored as bits in 8-byte array
            // Default values: FE 00 02 00 00 02 00 00
            var gadgetBytes = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var gadget in updatedGadgetStates)
            {
                if (AllGadgets.ContainsKey(gadget.Name))
                {
                    int bitIndex = AllGadgets[gadget.Name].UnlockBitIndex;
                    SetBit(gadgetBytes, bitIndex, gadget.IsUnlocked);
                }
            }

            game.SetGadgetUnlocks(gadgetBytes);
        }

        private void SaveBindings()
        {

            byte[] gadgetBindingBytes = new byte[48];

            // Read the binding values for all comboboxes
            var bindingCombos = new List<ComboBox>
            {
                slyGadgetsL1ComboBox, slyGadgetsL2ComboBox, slyGadgetsR2ComboBox,
                bentleyGadgetsL1ComboBox, bentleyGadgetsL2ComboBox, bentleyGadgetsR2ComboBox,
                murrayGadgetsL1ComboBox, murrayGadgetsL2ComboBox, murrayGadgetsR2ComboBox
            };

            for (int i = 0; i < bindingCombos.Count; i++)
            {
                string selectedGadget = bindingCombos[i].SelectedItem?.ToString();

                int buttonBindingIndex = -1; // Default to -1 (unbound)

                if (!string.IsNullOrEmpty(selectedGadget) && AllGadgets.ContainsKey(selectedGadget))
                {
                    buttonBindingIndex = AllGadgets[selectedGadget].ButtonBindingIndex ?? -1;
                }

                // Each binding is stored as a 4-byte integer
                byte[] bindingBytes = BitConverter.GetBytes(buttonBindingIndex);
                // reverse for big-endian
                Array.Reverse(bindingBytes);
                Array.Copy(bindingBytes, 0, gadgetBindingBytes, i * 4, 4);
            }

            game.SetGadgetBindings(gadgetBindingBytes);
        }

        private void murrayGadgetsToggleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SuspendComboBoxUpdates();
            for (var i = 0; i < murrayGadgetsCheckedList.Items.Count; i++)
            {
                murrayGadgetsCheckedList.SetItemChecked(i, murrayGadgetsToggleCheckBox.Checked);
            }
            ResumeComboBoxUpdates();
            PopulateMurrayBindingComboBoxes();
        }

        private void loadRunFileGadgetsButton_Click(object sender, EventArgs e)
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to load from first.");
                return;
            }

            string runFile = runFileComboBox.SelectedItem.ToString();
            string episodeKey = GetEpisodeKey(runFile);
            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetBindings");

            byte[] gadgetBytes;
            byte[] bindingBytes;
            try
            {
                gadgetBytes = IPS3API.HexToBytes(gadgetHex);
                bindingBytes = IPS3API.HexToBytes(bindingHex);
            }
            catch (FormatException)
            {
                statusLine.Error($"The {runFile} run file in config.txt isn't valid hex.");
                return;
            }
            if (gadgetBytes.Length < 8 || bindingBytes.Length < 36)
            {
                statusLine.Error($"No gadgets are saved for the {runFile} run file.");
                return;
            }

            // Fills in the form only. Save or Save and Reload writes it to the game.
            SuspendComboBoxUpdates();
            try
            {
                LoadGadgetsToList(slyGadgetsCheckedList, gadgetBytes);
                LoadGadgetsToList(bentleyGadgetsCheckedList, gadgetBytes);
                LoadGadgetsToList(murrayGadgetsCheckedList, gadgetBytes);
                LoadGadgetsToList(vaultGadgetsCheckedList, gadgetBytes);
                PopulateBindingComboBoxes();

                ComboBox[] bindingCombos =
                {
                    slyGadgetsL1ComboBox, slyGadgetsL2ComboBox, slyGadgetsR2ComboBox,
                    bentleyGadgetsL1ComboBox, bentleyGadgetsL2ComboBox, bentleyGadgetsR2ComboBox,
                    murrayGadgetsL1ComboBox, murrayGadgetsL2ComboBox, murrayGadgetsR2ComboBox
                };
                for (int i = 0; i < bindingCombos.Length; i++)
                {
                    // Each binding is a big-endian 4-byte integer, as GetCurrentBindingBytes writes it.
                    int buttonBindingIndex = BitConverter.ToInt32(bindingBytes.Skip(i * 4).Take(4).Reverse().ToArray(), 0);
                    SelectBinding(bindingCombos[i], buttonBindingIndex);
                }
            }
            finally
            {
                ResumeComboBoxUpdates();
            }

            statusLine.Info($"Loaded the {runFile} run file. Save to apply it in game.");
        }

        private void saveRunFileGadgetsButton_Click(object sender, EventArgs e)
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to save to first.");
                return;
            }

            SaveGadgetsToRunFile();
            game.api.Notify($"SluMAN {func.VersionText}: Gadget settings saved to {runFileComboBox.SelectedItem} run file.");
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            game.api.Notify("GadgetForm :: Save");
            SaveGadgets();
            SaveBindings();
            this.Close();
        }

        private void saveAndReloadButton_Click(object sender, EventArgs e)
        {
            game.api.Notify("GadgetForm :: Save and Reload");
            SaveGadgets();
            SaveBindings();
            game.Load();
            this.Close();
        }

        private void vaultGadgetsToggleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SuspendComboBoxUpdates();
            for (var i = 0; i < vaultGadgetsCheckedList.Items.Count; i++)
            {
                vaultGadgetsCheckedList.SetItemChecked(i, vaultGadgetsToggleCheckBox.Checked);
            }
            ResumeComboBoxUpdates();
            PopulateSlyBindingComboBoxes();
        }
    }
}
