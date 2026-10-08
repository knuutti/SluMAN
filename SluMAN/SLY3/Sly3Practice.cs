using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Net.Http;
using System.Threading;
using System.Reflection;

namespace SluMAN
{
    public partial class Sly3Practice : Form, IPositionEditorHost
    {
        public Form GadgetsWindow;
        private PositionEditor positionEditorWindow;

        // One window: the Practice controls on the first tab, the tools on the others.
        private PracticeTabs practiceTabs;
        private const string GadgetsTab = "Gadgets";
        private const string PositionTab = "Position Editor";
        private const string MemoryTab = "Memory";
        private const string ViewerTab = "Memory Viewer";

        public bool FlyModeEnabled => flyModeCheckBox.Checked;
        public bool InfiniteJumpEnabled => infiniteJumpCheckBox.Checked;
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
        private GameSession session;

        public Sly3Practice(sly3 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);
            func.BindEnter(healthTextBox, setHealthButton);

            mapComboBox.Items.AddRange(game.GetMapNames());
            mapComboBox.SelectedIndex = 0;

            gameNameId = sly3.addr.GameId;

            game.SetupInputDisplayMemorySubs();
            game.SetupWebManPopUp();

            game.CheckRunFileConfig();

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Sly 3", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            freezeTimer = new System.Windows.Forms.Timer();
            freezeTimer.Interval = 16;
            freezeTimer.Tick += FreezeTimer_Tick;
            freezeTimer.Start();

            reapplyTimer.Interval = ReapplyDelayMs;
            reapplyTimer.Tick += reapplyTimer_Tick;
            game.LoadFinished += game_LoadFinished;
            game.LoadStarted += game_LoadStarted;
            game.SetupLoadWatcher();

            practiceTabs = new PracticeTabs(this, "Practice");
            practiceTabs.AddTool(GadgetsTab, () => new Sly3Gadgets(game));
            practiceTabs.AddTool(PositionTab, () => new PositionEditor(game, game.GetPositionEditorLayout(), game.GetMapDisplayName, this));
            // The watch list and the hex viewer open each other's tabs.
            practiceTabs.AddTool(MemoryTab, () => new MemoryForm(() => (MemoryViewerForm)practiceTabs.Show(ViewerTab)));
            practiceTabs.AddTool(ViewerTab, () => new MemoryViewerForm(() => (MemoryForm)practiceTabs.Show(MemoryTab)));
            practiceTabs.ToolCreated += practiceTabs_ToolCreated;
            practiceTabs.ToolClosed += practiceTabs_ToolClosed;
        }

        private void game_LoadStarted()
        {
            // Raised on the subscription thread. Position freezes hold coordinates from the old
            // map, so drop them as soon as the load begins rather than writing them all through it.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    PositionEditor positionEditor = positionEditorWindow;
                    if (positionEditor != null && !positionEditor.IsDisposed)
                    {
                        positionEditor.ClearPositionFreezes();
                    }
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        // How long to wait after a load finishes before re-applying, so the game has set up the
        // player entity.
        private const int ReapplyDelayMs = 500;
        private readonly Timer reapplyTimer = new Timer();

        private void game_LoadFinished()
        {
            // Raised on the subscription thread.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() =>
                {
                    // The load may have been another save, so an open tool such as the gadgets
                    // reads it again. Save data doesn't wait for the player like the toggles do.
                    practiceTabs.RefreshShownTool();

                    reapplyAttempts = 0;
                    reapplyTimer.Stop();
                    reapplyTimer.Start();
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        // Retries while the player entity doesn't exist yet: 10 x 500 ms.
        private const int MaxReapplyAttempts = 10;
        private int reapplyAttempts = 0;

        private void reapplyTimer_Tick(object sender, EventArgs e)
        {
            reapplyTimer.Stop();

            if (!game.IsPlayerLoaded())
            {
                reapplyAttempts++;
                if (reapplyAttempts < MaxReapplyAttempts)
                {
                    reapplyTimer.Start();
                }
                else
                {
                    reapplyAttempts = 0;
                    Console.WriteLine("Load finished, but the player didn't appear; toggles not re-applied.");
                }
                return;
            }

            reapplyAttempts = 0;
            ReapplyToggles();
        }

        /// <summary>
        /// Applies the checked game toggles again after a load. A load can move the player entity
        /// and other dynamic addresses, so a value written or frozen before it may now be in the
        /// wrong place. Toggles that run every tick (infinite health, gadget power, jump) already
        /// look the address up each time and need nothing here.
        /// </summary>
        private void ReapplyToggles()
        {
            // Fly mode holds the height it had when switched on; take the new map's height instead.
            prevFlyModeState = false;
            PositionEditor positionEditor = positionEditorWindow;
            if (positionEditor != null && !positionEditor.IsDisposed)
            {
                positionEditor.ResetFlyHeight();
            }

            List<string> applied = new List<string>();
            Reapply(invulnerabilityCheckBox, () => game.SetInvulnerability(true), applied);
            Reapply(guardAICheckBox, () => game.SetGuardAI(true), applied);
            Reapply(deathBarriersCheckBox, () => game.SetDeathBarriers(true), applied);
            Reapply(gameClockCheckBox, () => game.SetGameClockFrozen(true), applied);

            if (applied.Count > 0)
            {
                Console.WriteLine($"Load finished, re-applied: {string.Join(", ", applied)}");
            }
        }

        private void Reapply(CheckBox toggle, Action apply, List<string> applied)
        {
            if (!toggle.Checked)
            {
                return;
            }
            try
            {
                apply();
                applied.Add(toggle.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't re-apply {toggle.Text} after load: {ex.Message}");
            }
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
            bool posEditorOpen = positionEditorWindow != null && !positionEditorWindow.IsDisposed;
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

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            session.ShowInputDisplay();
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

        private void Sly3Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The tools' own cleanup (subscriptions, timers) runs when they close.
            practiceTabs.CloseAll();
            game.LoadFinished -= game_LoadFinished;
            game.LoadStarted -= game_LoadStarted;
            reapplyTimer.Stop();
            freezeTimer.Stop();
            // The session closes the other windows and disconnects after this.
        }

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void switchGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }

        private void configureButtonCombosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureCombos configureCombos = new ConfigureCombos();
            configureCombos.ShowDialog(this);
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            practiceTabs.Show(MemoryTab);
        }

        private void practiceTabs_ToolCreated(string title, Form form)
        {
            if (title == GadgetsTab)
            {
                GadgetsWindow = form;
                session.AddGameWindow(form);
            }
            else if (title == PositionTab)
            {
                positionEditorWindow = (PositionEditor)form;
                session.AddGameWindow(form);
            }
        }

        private void practiceTabs_ToolClosed(string title)
        {
            if (title == GadgetsTab)
            {
                GadgetsWindow = null;
            }
            else if (title == PositionTab)
            {
                positionEditorWindow = null;
            }
        }

        private void openUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserData.OpenInExplorer();
        }

        private void loadMapButton_Click(object sender, EventArgs e)
        {
            game.LoadMap(mapComboBox.SelectedIndex);
        }

        private void gadgetsButton_Click(object sender, EventArgs e)
        {
            practiceTabs.Show(GadgetsTab);
        }

        private void positionEditorButton_Click(object sender, EventArgs e)
        {
            practiceTabs.Show(PositionTab);
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

        private void session_Reconnected()
        {
            game.SetupWebManPopUp();
            game.SetupLoadWatcher();
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



        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.RebootPS3();
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
