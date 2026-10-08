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

namespace racman
{
    public partial class SLY3GadgetsForm : Form, IRefreshOnShow
    {
        private sly3 game;
        private ItemCheckEventHandler slyItemCheckHandler;
        private ItemCheckEventHandler bentleyItemCheckHandler;
        private ItemCheckEventHandler murrayItemCheckHandler;

        private StatusLine statusLine;

        public SLY3GadgetsForm(sly3 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, false);
            LoadGadgets();
            PopulateBindingComboBoxes();
            LoadGadgetBindings();
            
            // Store event handlers so we can unhook them during bulk operations
            slyItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateSlyBindingComboBoxes())); };
            bentleyItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateBentleyBindingComboBoxes())); };
            murrayItemCheckHandler = (s, e) => { BeginInvoke((MethodInvoker)(() => PopulateMurrayBindingComboBoxes())); };
            
            // Wire up events
            slyGadgetsCheckedList.ItemCheck += slyItemCheckHandler;
            bentleyGadgetsCheckedList.ItemCheck += bentleyItemCheckHandler;
            murrayGadgetsCheckedList.ItemCheck += murrayItemCheckHandler;
        }

        private void slyGadgetsCheckedList_SelectedIndexChanged(object sender, EventArgs e)
        {

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

        // Discard Changes button - just close without saving
        private void discardChangesButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Save button - write to memory and close
        private void saveButton_Click(object sender, EventArgs e)
        {
            game.api.Notify("GadgetForm :: Save");
            SaveGadgets();
            SaveBindings();
            this.Close();
        }

        // Save and Reload button - write to memory, trigger reload, and close
        private void saveAndReloadButton_Click(object sender, EventArgs e)
        {
            game.api.Notify("GadgetForm :: Save and Reload");
            SaveGadgets();
            SaveBindings();
            game.Load();
            this.Close();
        }

        // Write gadget unlock states to memory
        private void SaveGadgets()
        {
            var updatedGadgetStates = GetGadgetStates(slyGadgetsCheckedList)
                .Concat(GetGadgetStates(bentleyGadgetsCheckedList))
                .Concat(GetGadgetStates(murrayGadgetsCheckedList));

            // Gadget states are stored as bits in 8-byte array
            // Default values: FE 00 02 00 00 02 00 00
            var gadgetBytes = new byte[8] { 0xFE, 0x00, 0x02, 0x00, 0x00, 0x02, 0x00, 0x00 };

            foreach (var gadget in updatedGadgetStates)
            {
                if (AllGadgets.ContainsKey(gadget.Name))
                {
                    int bitIndex = AllGadgets[gadget.Name].UnlockBitIndex;
                    SetBit(gadgetBytes, bitIndex, gadget.IsUnlocked);
                }
            }

            int spinAttackLevel = (int)spinAttackLevelSelector.Value;
            int pushAttackLevel = (int)pushAttackLevelSelector.Value;
            int jumpAttackLevel = (int)jumpAttackLevelSelector.Value;

            if (spinAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 1"].UnlockBitIndex, true);
            if (spinAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 2"].UnlockBitIndex, true);
            if (spinAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 3"].UnlockBitIndex, true);

            if (pushAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Push Attack 1"].UnlockBitIndex, true);
            if (pushAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Push Attack 2"].UnlockBitIndex, true);
            if (pushAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Push Attack 3"].UnlockBitIndex, true);

            if (jumpAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 1"].UnlockBitIndex, true);
            if (jumpAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 2"].UnlockBitIndex, true);
            if (jumpAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 3"].UnlockBitIndex, true);

            game.SetGadgetUnlocks(gadgetBytes);
        }

        private void SaveBindings() {

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

        // Load current gadget unlock states from memory
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
            LoadSpecialMoveLevels(spinAttackLevelSelector, pushAttackLevelSelector, jumpAttackLevelSelector, gadgetBytes);
        }

        // Helper method to load gadgets into a specific CheckedListBox
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

        private void LoadSpecialMoveLevels(NumericUpDown spinAttackLevelSelector, NumericUpDown pushAttackLevelSelector, NumericUpDown jumpAttackLevelSelector, byte[] gadgetBytes)
        {
            int spinAttackLevel = 0;
            if (GetBit(gadgetBytes, AllGadgets["Spin Attack 1"].UnlockBitIndex)) spinAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Spin Attack 2"].UnlockBitIndex)) spinAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Spin Attack 3"].UnlockBitIndex)) spinAttackLevel++;
            if (spinAttackLevel == 0)
            {
                spinAttackLevel++;
            }
            spinAttackLevelSelector.Value = spinAttackLevel;

            int pushAttackLevel = 0;
            if (GetBit(gadgetBytes, AllGadgets["Push Attack 1"].UnlockBitIndex)) pushAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Push Attack 2"].UnlockBitIndex)) pushAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Push Attack 3"].UnlockBitIndex)) pushAttackLevel++;
            pushAttackLevelSelector.Value = pushAttackLevel;

            int jumpAttackLevel = 0;
            if (GetBit(gadgetBytes, AllGadgets["Jump Attack 1"].UnlockBitIndex)) jumpAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Jump Attack 2"].UnlockBitIndex)) jumpAttackLevel++;
            if (GetBit(gadgetBytes, AllGadgets["Jump Attack 3"].UnlockBitIndex)) jumpAttackLevel++;
            jumpAttackLevelSelector.Value = jumpAttackLevel;
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
            { "Knockout Dive", new GadgetInfo { Name = "Knockout Dive", UnlockBitIndex = 25, ButtonBindingIndex = 31 } },
            { "Mega Jump", new GadgetInfo { Name = "Mega Jump", UnlockBitIndex = 26, ButtonBindingIndex = 30 } },
            { "Feral Pounce", new GadgetInfo { Name = "Feral Pounce", UnlockBitIndex = 27, ButtonBindingIndex = 29 } },
            { "Silent Obliteration", new GadgetInfo { Name = "Silent Obliteration", UnlockBitIndex = 28, ButtonBindingIndex = null } },
            { "Paraglider", new GadgetInfo { Name = "Paraglider", UnlockBitIndex = 29, ButtonBindingIndex = null } },
            { "Combat Dodge", new GadgetInfo { Name = "Combat Dodge", UnlockBitIndex = 30, ButtonBindingIndex = 26 } },
            { "Smoke Bomb", new GadgetInfo { Name = "Smoke Bomb", UnlockBitIndex = 31, ButtonBindingIndex = 25 } },
            { "Venice Disguise", new GadgetInfo { Name = "Venice Disguise", UnlockBitIndex = 34, ButtonBindingIndex = 38 } },
            { "Photographer Disguise", new GadgetInfo { Name = "Photographer Disguise", UnlockBitIndex = 33, ButtonBindingIndex = 39 } },
            { "Pirate Disguise", new GadgetInfo { Name = "Pirate Disguise", UnlockBitIndex = 48, ButtonBindingIndex = 40 } },
            { "Treasure Map", new GadgetInfo { Name = "Treasure Map", UnlockBitIndex = 36, ButtonBindingIndex = 36 } },
            { "Rocket Boots", new GadgetInfo { Name = "Rocket Boots", UnlockBitIndex = 37, ButtonBindingIndex = 35 } },
            { "Shadow Power", new GadgetInfo { Name = "Shadow Power", UnlockBitIndex = 40, ButtonBindingIndex = 32 } },
            { "Shadow Power 2", new GadgetInfo { Name = "Shadow Power 2", UnlockBitIndex = 38, ButtonBindingIndex = 34 } },
            { "Thief Reflexes", new GadgetInfo { Name = "Thief Reflexes", UnlockBitIndex = 39, ButtonBindingIndex = 33 } },

            { "Push Attack 1", new GadgetInfo { Name = "Push Attack 1", UnlockBitIndex = 41, ButtonBindingIndex = null } },
            { "Push Attack 2", new GadgetInfo { Name = "Push Attack 2", UnlockBitIndex = 56, ButtonBindingIndex = null } },
            { "Push Attack 3", new GadgetInfo { Name = "Push Attack 3", UnlockBitIndex = 55, ButtonBindingIndex = null } },
            { "Jump Attack 1", new GadgetInfo { Name = "Jump Attack 1", UnlockBitIndex = 44, ButtonBindingIndex = null } },
            { "Jump Attack 2", new GadgetInfo { Name = "Jump Attack 2", UnlockBitIndex = 43, ButtonBindingIndex = null } },
            { "Jump Attack 3", new GadgetInfo { Name = "Jump Attack 3", UnlockBitIndex = 42, ButtonBindingIndex = null } },
            { "Spin Attack 1", new GadgetInfo { Name = "Spin Attack 1", UnlockBitIndex = 47, ButtonBindingIndex = null } },
            { "Spin Attack 2", new GadgetInfo { Name = "Spin Attack 2", UnlockBitIndex = 46, ButtonBindingIndex = null } },
            { "Spin Attack 3", new GadgetInfo { Name = "Spin Attack 3", UnlockBitIndex = 45, ButtonBindingIndex = null } },

            { "Rage Bomb", new GadgetInfo { Name = "Rage Bomb", UnlockBitIndex = 9, ButtonBindingIndex = 15 } },
            { "Size Destabilizer", new GadgetInfo { Name = "Size Destabilizer", UnlockBitIndex = 10, ButtonBindingIndex = 14 } },
            { "Reduction Bomb", new GadgetInfo { Name = "Reduction Bomb", UnlockBitIndex = 24, ButtonBindingIndex = 16 } },
            { "Grapple-Cam", new GadgetInfo { Name = "Grapple-Cam", UnlockBitIndex = 11, ButtonBindingIndex = 13 } },
            { "Insanity Strike", new GadgetInfo { Name = "Insanity Strike", UnlockBitIndex = 12, ButtonBindingIndex = 12 } },
            { "Hover Pack", new GadgetInfo { Name = "Hover Pack", UnlockBitIndex = 13, ButtonBindingIndex = null } },
            { "Health Extractor", new GadgetInfo { Name = "Health Extractor", UnlockBitIndex = 14, ButtonBindingIndex = 10 } },
            { "Adrenaline Burst", new GadgetInfo { Name = "Adrenaline Burst", UnlockBitIndex = 15, ButtonBindingIndex = 9 } },
            { "Alarm Clock", new GadgetInfo { Name = "Alarm Clock", UnlockBitIndex = 16, ButtonBindingIndex = 8 } },
            { "Fishing Pole", new GadgetInfo { Name = "Fishing Pole", UnlockBitIndex = 1, ButtonBindingIndex = 7 } },
            { "Trigger Bomb", new GadgetInfo { Name = "Trigger Bomb", UnlockBitIndex = 2, ButtonBindingIndex = 6 } },

            { "Be The Ball", new GadgetInfo { Name = "Be The Ball", UnlockBitIndex = 23, ButtonBindingIndex = 17 } },
            { "Raging Inferno Flop", new GadgetInfo { Name = "Raging Inferno Flop", UnlockBitIndex = 17, ButtonBindingIndex = 23 } },
            { "Temporal Lock", new GadgetInfo { Name = "Temporal Lock", UnlockBitIndex = 18, ButtonBindingIndex = 22 } },
            { "Fists of Flame", new GadgetInfo { Name = "Fists of Flame", UnlockBitIndex = 19, ButtonBindingIndex = 21 } },
            { "Guttural Roar", new GadgetInfo { Name = "Guttural Roar", UnlockBitIndex = 20, ButtonBindingIndex = 20 } },
            { "Juggernaut Throw", new GadgetInfo { Name = "Juggernaut Throw", UnlockBitIndex = 21, ButtonBindingIndex = null } },
            { "Berserker Charge", new GadgetInfo { Name = "Berserker Charge", UnlockBitIndex = 22, ButtonBindingIndex = 18 } },
            { "Diablo Fire Slam", new GadgetInfo { Name = "Diablo Fire Slam", UnlockBitIndex = 32, ButtonBindingIndex = 24 } }
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

        // Populate all binding comboboxes
        private void PopulateBindingComboBoxes()
        {
            PopulateSlyBindingComboBoxes();
            PopulateBentleyBindingComboBoxes();
            PopulateMurrayBindingComboBoxes();
        }

        // Populate Sly binding comboboxes with checked gadgets
        private void PopulateSlyBindingComboBoxes()
        {
            var checkedGadgets = GetCheckedGadgetNames(slyGadgetsCheckedList);
            
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

        // Get list of checked gadget names from a CheckedListBox, filtering out those without binding indices
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

        // Populate a single combobox with gadget names
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

        // Temporarily unhook event handlers during bulk operations
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

        // Load current gadget bindings from memory and select them in comboboxes
        private void LoadGadgetBindings()
        {
            // Load Sly bindings (L1=0, L2=1, R2=2)
            LoadBindingToComboBox(slyGadgetsL1ComboBox, sly3.addr.gadgetBindsSly, 0);
            LoadBindingToComboBox(slyGadgetsL2ComboBox, sly3.addr.gadgetBindsSly, 1);
            LoadBindingToComboBox(slyGadgetsR2ComboBox, sly3.addr.gadgetBindsSly, 2);

            // Load Bentley bindings
            LoadBindingToComboBox(bentleyGadgetsL1ComboBox, sly3.addr.gadgetBindsBentley, 0);
            LoadBindingToComboBox(bentleyGadgetsL2ComboBox, sly3.addr.gadgetBindsBentley, 1);
            LoadBindingToComboBox(bentleyGadgetsR2ComboBox, sly3.addr.gadgetBindsBentley, 2);

            // Load Murray bindings
            LoadBindingToComboBox(murrayGadgetsL1ComboBox, sly3.addr.gadgetBindsMurray, 0);
            LoadBindingToComboBox(murrayGadgetsL2ComboBox, sly3.addr.gadgetBindsMurray, 1);
            LoadBindingToComboBox(murrayGadgetsR2ComboBox, sly3.addr.gadgetBindsMurray, 2);
        }

        // Load a single binding from memory and select it in the combobox
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

        // Find gadget name by its button binding index (returns null if not found or -1)
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

        private void loadRunFileGadgetsButton_Click(object sender, EventArgs e)
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to load from first.");
                return;
            }

            string runFile = runFileComboBox.SelectedItem.ToString();
            string episodeKey = GetEpisodeKey(runFile);
            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_GadgetBindings");

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
                LoadSpecialMoveLevels(spinAttackLevelSelector, pushAttackLevelSelector, jumpAttackLevelSelector, gadgetBytes);
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

        private void SaveGadgetsToRunFile()
        {
            string episodeKey = GetEpisodeKey(runFileComboBox.SelectedItem.ToString());

            // Get current gadget states (same logic as SaveGadgets())
            var gadgetBytes = GetCurrentGadgetBytes();
            var bindingBytes = GetCurrentBindingBytes();

            // Save to config
            func.ChangeFileLines("config.txt",
                BitConverter.ToString(gadgetBytes).Replace("-", ""),
                episodeKey + "_GadgetUnlocks");

            func.ChangeFileLines("config.txt",
                BitConverter.ToString(bindingBytes).Replace("-", ""),
                episodeKey + "_GadgetBindings");

            func.ChangeFileLines("config.txt",
                $"{spinAttackLevelSelector.Value},{pushAttackLevelSelector.Value},{jumpAttackLevelSelector.Value}",
                episodeKey + "_SpecialAttacks");
        }

        // Convert episode display name to a safe config key prefix
        private string GetEpisodeKey(string episodeName)
        {
            switch (episodeName)
            {
                case "Episode 1": return "Episode1";
                case "Episode 2": return "Episode2";
                case "Episode 3": return "Episode3";
                case "Episode 4": return "Episode4";
                case "Episode 5": return "Episode5";
                case "Episode 6 (No CE)": return "Episode6_NoCE";
                case "Episode 6 (CE)": return "Episode6_CE";
                default: return "Episode1"; // fallback
            }
        }

        // Get current gadget unlock states as byte array (extracted from SaveGadgets logic)
        private byte[] GetCurrentGadgetBytes()
        {
            var updatedGadgetStates = GetGadgetStates(slyGadgetsCheckedList)
                .Concat(GetGadgetStates(bentleyGadgetsCheckedList))
                .Concat(GetGadgetStates(murrayGadgetsCheckedList));

            // Gadget states are stored as bits in 8-byte array
            // Default values: FE 00 02 00 00 02 00 00
            var gadgetBytes = new byte[8] { 0xFE, 0x00, 0x02, 0x00, 0x00, 0x02, 0x00, 0x00 };

            foreach (var gadget in updatedGadgetStates)
            {
                if (AllGadgets.ContainsKey(gadget.Name))
                {
                    int bitIndex = AllGadgets[gadget.Name].UnlockBitIndex;
                    SetBit(gadgetBytes, bitIndex, gadget.IsUnlocked);
                }
            }

            int spinAttackLevel = (int)spinAttackLevelSelector.Value;
            int pushAttackLevel = (int)pushAttackLevelSelector.Value;
            int jumpAttackLevel = (int)jumpAttackLevelSelector.Value;

            if (spinAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 1"].UnlockBitIndex, true);
            if (spinAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 2"].UnlockBitIndex, true);
            if (spinAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Spin Attack 3"].UnlockBitIndex, true);

            if (pushAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Push Attack 1"].UnlockBitIndex, true);
            if (pushAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Push Attack 2"].UnlockBitIndex, true);
            if (pushAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Push Attack 3"].UnlockBitIndex, true);

            if (jumpAttackLevel >= 1)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 1"].UnlockBitIndex, true);
            if (jumpAttackLevel >= 2)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 2"].UnlockBitIndex, true);
            if (jumpAttackLevel >= 3)
                SetBit(gadgetBytes, AllGadgets["Jump Attack 3"].UnlockBitIndex, true);

            return gadgetBytes;
        }

        // Get current gadget binding states as byte array (extracted from SaveBindings logic)
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
    }
}