using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{

    public partial class ConfigureCombos : Form
    {
        private const string SaveComboKey = "savePosCombo";
        private const string LoadComboKey = "loadPosCombo";
        private const string LoadGameComboKey = "loadGameCombo";
        private const string RunScriptComboKey = "runScriptCombo";
        private const string CombosEnabledKey = "combosEnabled";

        private const string WaitingText = "Press a combo...";

        // L2 + R2 + R1, L2 + R2 + L1 and L3 + R3. 0 means no combo.
        private const int DefaultSaveCombo = 0xb;
        private const int DefaultLoadCombo = 0x7;
        private const int DefaultLoadGameCombo = 0x600;
        private const int DefaultRunScriptCombo = 0;

        public static int saveCombo, loadCombo, loadGameCombo, runScriptCombo;
        public static bool combosEnabled = true;

        /// <summary>
        /// True while the dialog is recording a combo, so pressing buttons doesn't run the
        /// combos that are already set.
        /// </summary>
        public static bool capturing = false;

        private Timer captureTimer = new Timer();
        private TextBox captureTextBox = null;
        private int capturedMask = 0;

        public ConfigureCombos()
        {
            InitializeComponent();
            GetCombos();

            combosEnabledCheckBox.Checked = combosEnabled;
            ShowCombos();

            captureTimer.Interval = 16;
            captureTimer.Tick += new EventHandler(captureTimer_Tick);
        }

        public static void GetCombos()
        {
            saveCombo = ReadCombo(SaveComboKey, DefaultSaveCombo);
            loadCombo = ReadCombo(LoadComboKey, DefaultLoadCombo);
            loadGameCombo = ReadCombo(LoadGameComboKey, DefaultLoadGameCombo);
            runScriptCombo = ReadCombo(RunScriptComboKey, DefaultRunScriptCombo);

            string enabled = func.GetConfigData("config.txt", CombosEnabledKey);
            combosEnabled = enabled != "false";
        }

        private static int ReadCombo(string key, int defaultCombo)
        {
            string value = func.GetConfigData("config.txt", key);
            int combo;
            if (int.TryParse(value, out combo))
            {
                return combo;
            }
            return defaultCombo;
        }

        private static void SaveCombos()
        {
            func.ChangeFileLines("config.txt", saveCombo.ToString(), SaveComboKey);
            func.ChangeFileLines("config.txt", loadCombo.ToString(), LoadComboKey);
            func.ChangeFileLines("config.txt", loadGameCombo.ToString(), LoadGameComboKey);
            func.ChangeFileLines("config.txt", runScriptCombo.ToString(), RunScriptComboKey);
        }

        public static string Describe(int combo)
        {
            if (combo == 0)
            {
                return "None";
            }
            return String.Join(" + ", Inputs.DecodeMask(combo));
        }

        private void ShowCombos()
        {
            savePositionTextBox.Text = Describe(saveCombo);
            loadPositionTextBox.Text = Describe(loadCombo);
            loadGameTextBox.Text = Describe(loadGameCombo);
            runScriptTextBox.Text = Describe(runScriptCombo);
        }

        private string ActionName(TextBox textBox)
        {
            if (textBox == savePositionTextBox) return "Save Position";
            if (textBox == loadPositionTextBox) return "Load Position";
            if (textBox == loadGameTextBox) return "Load Game";
            return "Run Script";
        }

        private int GetCombo(TextBox textBox)
        {
            if (textBox == savePositionTextBox) return saveCombo;
            if (textBox == loadPositionTextBox) return loadCombo;
            if (textBox == loadGameTextBox) return loadGameCombo;
            return runScriptCombo;
        }

        private void SetCombo(TextBox textBox, int combo)
        {
            if (textBox == savePositionTextBox) saveCombo = combo;
            else if (textBox == loadPositionTextBox) loadCombo = combo;
            else if (textBox == loadGameTextBox) loadGameCombo = combo;
            else runScriptCombo = combo;
        }

        private List<TextBox> ComboTextBoxes()
        {
            return new List<TextBox> { savePositionTextBox, loadPositionTextBox, loadGameTextBox, runScriptTextBox };
        }

        private void StartCapture(TextBox textBox)
        {
            StopCapture();

            captureTextBox = textBox;
            capturedMask = 0;
            capturing = true;
            captureTextBox.Text = WaitingText;
            statusLabel.Text = $"Recording {ActionName(textBox)}. Press Esc to cancel.";
            captureTimer.Start();
        }

        private void StopCapture()
        {
            captureTimer.Stop();
            captureTextBox = null;
            capturedMask = 0;
            capturing = false;
            ShowCombos();
        }

        private static int CountButtons(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }
            return count;
        }

        /// <summary>
        /// Keeps the fullest mask seen during the press and saves it when every button is released.
        /// Fingers rarely press or release together, so the first or last mask alone is usually
        /// missing a button.
        /// </summary>
        private void captureTimer_Tick(object sender, EventArgs e)
        {
            if (captureTextBox == null)
            {
                return;
            }

            int mask = Inputs.RawInputs;

            if (mask != 0)
            {
                if (CountButtons(mask) >= CountButtons(capturedMask))
                {
                    capturedMask = mask;
                    captureTextBox.Text = Describe(capturedMask);
                }
                return;
            }

            if (capturedMask == 0)
            {
                return;
            }

            CommitCapture(captureTextBox, capturedMask);
        }

        private void CommitCapture(TextBox textBox, int combo)
        {
            string message = $"{ActionName(textBox)} set to {Describe(combo)}.";

            foreach (TextBox other in ComboTextBoxes())
            {
                if (other != textBox && GetCombo(other) == combo)
                {
                    SetCombo(other, 0);
                    message += $" Removed it from {ActionName(other)}.";
                }
            }

            if (CountButtons(combo) == 1)
            {
                message += " A single button also fires during normal play.";
            }

            SetCombo(textBox, combo);
            SaveCombos();
            StopCapture();
            statusLabel.Text = message;
        }

        private void comboTextBox_Click(object sender, EventArgs e)
        {
            StartCapture((TextBox)sender);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            TextBox textBox;
            if (sender == savePositionClearButton) textBox = savePositionTextBox;
            else if (sender == loadPositionClearButton) textBox = loadPositionTextBox;
            else if (sender == loadGameClearButton) textBox = loadGameTextBox;
            else textBox = runScriptTextBox;

            StopCapture();
            SetCombo(textBox, 0);
            SaveCombos();
            ShowCombos();
            statusLabel.Text = $"{ActionName(textBox)} has no combo.";
        }

        private void combosEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            combosEnabled = combosEnabledCheckBox.Checked;
            func.ChangeFileLines("config.txt", combosEnabled ? "true" : "false", CombosEnabledKey);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && captureTextBox != null)
            {
                StopCapture();
                statusLabel.Text = "Recording cancelled.";
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ConfigureCombos_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCapture();
        }
    }
}
