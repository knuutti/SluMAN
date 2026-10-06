using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Net.Http;
using System.Threading;
using System.Reflection;

namespace racman
{
    public partial class SLY3Form : Form
    {
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        public Form InputDisplay;
        public Form GadgetsWindow;
        public Form PositionEditor;
        public sly3 game;
        public string gameNameId;

        private System.Windows.Forms.Timer freezeTimer;

        private float flyFrozenZ;
        private float prevFlyPosX;
        private float prevFlyPosY;
        private bool prevFlyModeState;
        private const float FlyHeightStep = 20.0f;
        private const float FlyBoostMultiplier = 8.0f;

        private StatusLine statusLine;

        public SLY3Form(sly3 game, string gameNameId = "NPEA00343")
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);
            func.BindEnter(healthTextBox, setHealthButton);

            mapComboBox.Items.AddRange(game.GetMapNames());
            mapComboBox.SelectedIndex = 0;

            ApplySavedPreferences();

            game.SetupInputDisplayMemorySubs();
            game.SetupWebManPopUp();

            game.CheckRunFileConfig();

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            if (func.api is Ratchetron r)
            {
                r.setDisconnectCallback(() => { DisconnectGame(false); });

                r.setReconnectCallback(() => { ReconnectGame(); });
            }

            this.gameNameId = gameNameId;

            freezeTimer = new System.Windows.Forms.Timer();
            freezeTimer.Interval = 16;
            freezeTimer.Tick += FreezeTimer_Tick;
            freezeTimer.Start();
        }

        private void FreezeTimer_Tick(object sender, EventArgs e)
        {
            if (infiniteHealthCheckBox.Checked)
            {
                try { game.SetHealth(100); } catch { }
            }
            if (infiniteGadgetPowerCheckBox.Checked)
            {
                try { game.SetGadgetPower(100); } catch { }
            }

            // Fly mode and infinite jump are handled by the Position Editor when it is open
            bool posEditorOpen = PositionEditor != null && !PositionEditor.IsDisposed;
            if (posEditorOpen)
            {
                prevFlyModeState = false;
                return;
            }

            if (infiniteJumpCheckBox.Checked)
            {
                try
                {
                    byte[] epBytes = game.api.ReadMemory(game.pid, sly3.addr.activeCharacterPtr, 4);
                    uint entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                    if (entityPtr != 0)
                        game.api.WriteMemory(game.pid, entityPtr + 0x338, (uint)0);
                }
                catch { }
            }

            bool flyMode = flyModeCheckBox.Checked;
            if (!flyMode)
            {
                prevFlyModeState = false;
                return;
            }

            try
            {
                byte[] epBytes = game.api.ReadMemory(game.pid, sly3.addr.activeCharacterPtr, 4);
                uint entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                if (entityPtr == 0) return;

                byte[] tpBytes = game.api.ReadMemory(game.pid, entityPtr + sly3.addr.transformOffset, 4);
                uint transformPtr = BitConverter.ToUInt32(tpBytes.Reverse().ToArray(), 0);
                if (transformPtr == 0) return;

                if (!prevFlyModeState)
                {
                    flyFrozenZ = ReadFloat(transformPtr + sly3.addr.coordsOffsetZ);
                    prevFlyPosX = ReadFloat(transformPtr + sly3.addr.coordsOffsetX);
                    prevFlyPosY = ReadFloat(transformPtr + sly3.addr.coordsOffsetY);
                }
                prevFlyModeState = true;

                if ((Inputs.RawInputs & 0x1) != 0) flyFrozenZ += FlyHeightStep;  // L2 = up
                if ((Inputs.RawInputs & 0x2) != 0) flyFrozenZ -= FlyHeightStep;  // R2 = down

                WriteFloat(transformPtr + sly3.addr.coordsOffsetZ, flyFrozenZ);
                WriteFloat(transformPtr + 0x1B8, 0f);  // zero Z velocity

                float curX = ReadFloat(transformPtr + sly3.addr.coordsOffsetX);
                float curY = ReadFloat(transformPtr + sly3.addr.coordsOffsetY);
                if ((Inputs.RawInputs & 0x8) != 0)  // R1 = horizontal boost
                {
                    float deltaX = curX - prevFlyPosX;
                    float deltaY = curY - prevFlyPosY;
                    if (Math.Abs(deltaX) > 0.001f || Math.Abs(deltaY) > 0.001f)
                    {
                        float newX = prevFlyPosX + deltaX * FlyBoostMultiplier;
                        float newY = prevFlyPosY + deltaY * FlyBoostMultiplier;
                        WriteFloat(transformPtr + sly3.addr.coordsOffsetX, newX);
                        WriteFloat(transformPtr + sly3.addr.coordsOffsetY, newY);
                        curX = newX;
                        curY = newY;
                    }
                }
                prevFlyPosX = curX;
                prevFlyPosY = curY;
            }
            catch { }
        }

        private float ReadFloat(uint address)
        {
            byte[] b = game.api.ReadMemory(game.pid, address, 4);
            return BitConverter.ToSingle(b.Reverse().ToArray(), 0);
        }

        private void WriteFloat(uint address, float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            game.api.WriteMemory(game.pid, address, b);
        }

        private void ApplySavedPreferences()
        {
            var prefersAlwaysOnTop = bool.TryParse(func.GetConfigData("config.txt", PrefersAlwaysOnTopKey), out bool alwaysOnTopEnabled) && alwaysOnTopEnabled;
            alwaysOnTopCheckBox.Checked = prefersAlwaysOnTop;
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            if (InputDisplay == null || InputDisplay.IsDisposed)
            {
                InputDisplay = new InputDisplay();
                InputDisplay.Show();
                game.InputsTimer.Start();
            }
            else
            {
                InputDisplay.Focus();
            }
        }

        private void loadPosButton_Click(object sender, EventArgs e)
        {
        }

        private void savePosButton_Click(object sender, EventArgs e)
        {
        }

        private void coinsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SetCoinsFromTextBox();
            }
        }

        private void positionsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void SLY3Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Make sure all child forms are closed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                InputDisplay.Close();
            }
            if (GadgetsWindow != null && !GadgetsWindow.IsDisposed)
            {
                GadgetsWindow.Close();
            }

            // Stop timers
            if (freezeTimer != null)
            {
                freezeTimer.Stop();
            }
            if (game.InputsTimer != null)
            {
                game.InputsTimer.Stop();
            }

            try
            {
                if (game.api is Ratchetron r)
                {
                    r.ReleaseAllSubs();
                }
                game.api.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }

        private void HandleDisconnect()
        {
            if (game.api is Ratchetron r)
            {
                r.ReleaseAllSubs();
            }
            game.api.Disconnect();
            Console.WriteLine("Sly 3: Full cleanup on form close");
        }

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void switchGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            Program.AttachPS3Form.Show();
            this.Close();
        }

        private void configureButtonCombosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureCombos configureCombos = new ConfigureCombos();
            configureCombos.ShowDialog(this);
        }

        private void inputDisplayToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryForm memoryForm = Application.OpenForms["MemoryForm"] as MemoryForm;

            if (memoryForm != null)
            {
                memoryForm.Activate();
            }
            else
            {
                memoryForm = new MemoryForm();
                memoryForm.Show();
            }
        }

        private void loadMapButton_Click(object sender, EventArgs e)
        {
            game.LoadMap(mapComboBox.SelectedIndex);
        }

        private void gadgetsButton_Click(object sender, EventArgs e)
        {
            if (GadgetsWindow == null || GadgetsWindow.IsDisposed)
            {
                GadgetsWindow = new SLY3GadgetsForm(game);
                GadgetsWindow.FormClosed += GadgetsWindow_FormClosed;
                GadgetsWindow.Show();
            }
            else
            {
                GadgetsWindow.Focus();
            }
        }

        private void GadgetsWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            GadgetsWindow = null;
        }

        private void positionEditorButton_Click(object sender, EventArgs e)
        {
            if (PositionEditor == null || PositionEditor.IsDisposed)
            {
                PositionEditor = new SLY3PositionEditor(game, this);
                PositionEditor.FormClosed += PositionEditor_FormClosed;
                PositionEditor.Show();
            }
            else
            {
                PositionEditor.Focus();
            }
        }

        private void PositionEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            PositionEditor = null;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void fastReloadButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 1:
                    game.SetActiveCharacter(24);
                    break;
                case 2:
                    game.SetActiveCharacter(25);
                    break;
                case 3:
                    game.SetActiveCharacter(26);
                    break;
                case 4:
                    game.SetActiveCharacter(28);
                    break;
                case 5:
                    game.SetActiveCharacter(29);
                    break;
                case 6:
                    game.SetActiveCharacter(30);
                    break;
                case 7:
                    game.SetActiveCharacter(31);
                    break;
            }
            game.TriggerGameLoad((uint)0);
        }

        private void fullReloadButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 1:
                    game.SetActiveCharacter(24);
                    break;
                case 2:
                    game.SetActiveCharacter(25);
                    break;
                case 3:
                    game.SetActiveCharacter(26);
                    break;
                case 4:
                    game.SetActiveCharacter(28);
                    break;
                case 5:
                    game.SetActiveCharacter(29);
                    break;
                case 6:
                    game.SetActiveCharacter(30);
                    break;
                case 7:
                    game.SetActiveCharacter(31);
                    break;
            }
            game.TriggerGameLoad();
        }

        private void loadRunFileButton_Click(object sender, EventArgs e)
        {

        }

        private void webMANShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void skipCinematicsButton_Click(object sender, EventArgs e)
        {
            game.SkipCinematic();
        }

        private void alwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            func.ChangeFileLines("config.txt", alwaysOnTopCheckBox.Checked ? "true" : "false", PrefersAlwaysOnTopKey);
            if (!alwaysOnTopCheckBox.Checked)
            {
                this.TopMost = false;

            }
            else
            {
                this.TopMost = true;
            }
        }

        private void loadJobButton_Click(object sender, EventArgs e)
        {
            game.LoadJob(jobComboBox.Text);
        }

        private void abandonJobButton_Click(object sender, EventArgs e)
        {
            game.AbandonJob(jobComboBox.Text);
        }

        private void setCoinsButton_Click(object sender, EventArgs e)
        {
            SetCoinsFromTextBox();

        }

        private void coinsTextBox_TextChanged(object sender, EventArgs e)
        {
            

        }

        private void DisconnectGame(bool closeInputDisplay = true)
        {
            if (game.api is Ratchetron ratchetron)
            {
                ratchetron.ReleaseAllSubs();
            }
            if (closeInputDisplay)
            {
                try { game.api.Disconnect(); } catch { }
            }
            CloseAdditionalWindows(closeInputDisplay);
        }

        private void CloseAdditionalWindows(bool closeInputDisplay = true)
        {
            if (closeInputDisplay && InputDisplay != null && !InputDisplay.IsDisposed)
            {
                InputDisplay.Close();
            }
            if (GadgetsWindow != null && !GadgetsWindow.IsDisposed)
            {
                GadgetsWindow.Close();
            }
            if (PositionEditor != null && !PositionEditor.IsDisposed)
            {
                PositionEditor.Close();
            }
        }

        private void ReconnectGame()
        {
            int pid = 0;
            int attempts = 0;
            int maxAttempts = 30;

            while (pid == 0 && attempts < maxAttempts)
            {
                Thread.Sleep(3000);
                attempts++;

                try
                {
                    if (game.api.getGameTitleID() != gameNameId)
                    {
                        Console.WriteLine("Different game detected.");
                        return;
                    }

                    pid = game.api.getCurrentPID();
                    if (pid != 0)
                    {
                        Console.WriteLine($"Sly 3: Game detected after {attempts * 3} seconds (PID: {pid})");
                    }
                    else
                    {
                        Console.WriteLine($"Sly 3: Still waiting for game... ({attempts * 3}s elapsed)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sly 3: Error checking game status: {ex.Message}");
                }
            }

            if (pid == 0)
            {
                Console.WriteLine("Sly 3: Game did not start within 90 seconds");
                game.api.Notify("Sly 3: Reconnection timeout");
                return;
            }

            // Update PID for new game session
            AttachPS3Form.pid = pid;
            game.pid = pid;

            // Give game extra time to fully initialize
            Thread.Sleep(2000);

            // Re-establish memory subscriptions
            game.SetupInputDisplayMemorySubs();

            // Restart input timer if needed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                game.InputsTimer.Start();
            }

            game.api.Notify($"SluMAN v{Assembly.GetEntryAssembly().GetName().Version.ToString(3)} (Practice Mode)");
            Console.WriteLine("Sly 3: Reconnection complete");
        }

        private void SetCoinsFromTextBox()
        {
            var coinsText = coinsTextBox.Text;
            // try to parse the input as an integer
            if (int.TryParse(coinsText, out int coins))
            {
                game.SetCoinCount(coins);
            }
            else
            {
                statusLine.Error("Coins must be a whole number.");
            }
        }

        private void SetHealthFromTextBox()
        {
            var healthText = healthTextBox.Text;
            // try to parse the input as an integer
            if (int.TryParse(healthText, out int health))
            {
                game.SetHealth(health);
            }
            else
            {
                statusLine.Error("Health must be a whole number.");
            }
        }

        private void healthTextBox_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void setHealthButton_Click(object sender, EventArgs e)
        {
            SetHealthFromTextBox();
        }



        private void SLY3Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Program.AttachPS3Form.Visible == false)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
            }
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (game.api is Ratchetron r)
            {
                var dialogResult = MessageBox.Show("Do you want to turn off your PS3?", "Power Off PS3", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DisconnectGame();
                    WebMAN.TurnOffPS3(func.api.GetIP());
                    this.Close();
                    Program.AttachPS3Form.Show();
                }

            }
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (game.api is Ratchetron r)
            {
                var dialogResult = MessageBox.Show("Do you want to reboot your PS3?", "Reboot PS3", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DisconnectGame();
                    WebMAN.RebootPS3(func.api.GetIP());
                    this.Close();
                    Program.AttachPS3Form.Show();
                }
            }
        }

        private void invulnerabilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            game.SetInvulnerability(invulnerabilityCheckBox.Checked);
        }

        private void guardAICheckBox_CheckedChanged(object sender, EventArgs e)
        {
            game.SetGuardAI(guardAICheckBox.Checked);
        }

        private void deathBarriersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            game.SetDeathBarriers(deathBarriersCheckBox.Checked);
        }

        private void gameClockCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            game.SetGameClockFrozen(gameClockCheckBox.Checked);
        }

        

        private void infiniteHealthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void infiniteGadgetPowerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
