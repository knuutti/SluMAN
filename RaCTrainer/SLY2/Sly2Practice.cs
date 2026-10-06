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
    public partial class Sly2Practice : Form
    {
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        public Form InputDisplay;
        public Form GadgetsWindow;
        private PositionEditor positionEditorWindow;
        public sly2 game;
        public string gameNameId;

        private StatusLine statusLine;

        public Sly2Practice(sly2 game, string gameNameId = "NPHA80175")
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

            GameReconnect.WatchRpcs3(this, game.api, gameNameId, false);
            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            if (func.api is Ratchetron r)
            {
                r.setDisconnectCallback(() => { DisconnectGame(false); });

                r.setReconnectCallback(() => { ReconnectGame(); });
            }

            this.gameNameId = gameNameId;

            if (!sly2.addr.HasCharacterStats)
            {
                // Health and gadget power addresses aren't known for this version yet.
                infiniteHealthCheckBox.Enabled = false;
                infiniteGadgetPowerCheckBox.Enabled = false;
            }

            reapplyTimer.Interval = ReapplyDelayMs;
            reapplyTimer.Tick += reapplyTimer_Tick;
            game.LoadFinished += game_LoadFinished;
            game.LoadStarted += game_LoadStarted;
            game.SetupLoadWatcher();
        }

        private void infiniteJumpCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteJumpCheckBox, () => game.SetInfiniteJump(infiniteJumpCheckBox.Checked));
        }

        private void positionEditorButton_Click(object sender, EventArgs e)
        {
            if (positionEditorWindow == null || positionEditorWindow.IsDisposed)
            {
                // Sly 2 has no Fly Mode, and its Infinite Jump is a freeze, so there's no host.
                positionEditorWindow = new PositionEditor(game, game.GetPositionEditorLayout(), game.GetMapDisplayName);
                positionEditorWindow.FormClosed += (s, args) => { positionEditorWindow = null; };
                positionEditorWindow.Show();
            }
            else
            {
                positionEditorWindow.Focus();
            }
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
                    if (positionEditorWindow != null && !positionEditorWindow.IsDisposed)
                    {
                        positionEditorWindow.ClearPositionFreezes();
                    }
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        private void invulnerabilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(invulnerabilityCheckBox, () => game.SetInvulnerability(invulnerabilityCheckBox.Checked));
        }

        private void infiniteHealthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteHealthCheckBox, () => game.SetInfiniteHealth(infiniteHealthCheckBox.Checked));
        }

        private void infiniteGadgetPowerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(infiniteGadgetPowerCheckBox, () => game.SetInfiniteGadgetPower(infiniteGadgetPowerCheckBox.Checked));
        }

        private void gameClockCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToggle(gameClockCheckBox, () => game.SetGameClockFrozen(gameClockCheckBox.Checked));
        }

        private void ApplyToggle(CheckBox toggle, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't change {toggle.Text}: {ex.Message}");
            }
        }

        // How long to wait after a load finishes before re-applying, so the game has set up the
        // player entity.
        private const int ReapplyDelayMs = 500;
        // Retries while the player entity doesn't exist yet: 10 x 500 ms.
        private const int MaxReapplyAttempts = 10;
        private readonly Timer reapplyTimer = new Timer();
        private int reapplyAttempts = 0;

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
        /// Applies the checked game toggles again after a load. A load can move the player entity,
        /// so a freeze set up before it may now point at the wrong address.
        /// </summary>
        private void ReapplyToggles()
        {
            List<string> applied = new List<string>();
            Reapply(invulnerabilityCheckBox, () => game.SetInvulnerability(true), applied);
            Reapply(infiniteHealthCheckBox, () => game.SetInfiniteHealth(true), applied);
            Reapply(infiniteGadgetPowerCheckBox, () => game.SetInfiniteGadgetPower(true), applied);
            Reapply(gameClockCheckBox, () => game.SetGameClockFrozen(true), applied);
            Reapply(infiniteJumpCheckBox, () => game.SetInfiniteJump(true), applied);

            if (applied.Count > 0)
            {
                Console.WriteLine($"Load finished, re-applied: {string.Join(", ", applied)}");
            }
        }

        private void Reapply(CheckBox toggle, Action apply, List<string> applied)
        {
            if (!toggle.Checked || !toggle.Enabled)
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

        private void powerOffPs3Button_Click(object sender, EventArgs e)
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

        private void rebootPS3Button_Click(object sender, EventArgs e)
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

        private void switchGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            this.Close();
            Program.AttachPS3Form.Show();
        }

        private void configureButtonCombosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureCombos configureCombos = new ConfigureCombos();
            configureCombos.ShowDialog(this);
        }

        private void inputDisplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenInputDisplay();
        }

        private void OpenInputDisplay()
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

        private void coinsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SetCoinsFromTextBox();
            }
        }

        private void Sly2Practice_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Program.AttachPS3Form.Visible == false && !AttachPS3Form.switchPending)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
            }
        }

        private void Sly2Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            game.LoadFinished -= game_LoadFinished;
            game.LoadStarted -= game_LoadStarted;
            reapplyTimer.Stop();

            // Make sure all child forms are closed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                InputDisplay.Close();
            }
            if (GadgetsWindow != null && !GadgetsWindow.IsDisposed)
            {
                GadgetsWindow.Close();
            }
            if (positionEditorWindow != null && !positionEditorWindow.IsDisposed)
            {
                positionEditorWindow.Close();
            }

            // Stop timers
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

        private void skipCinematicsButton_Click(object sender, EventArgs e)
        {
            game.SkipCinematic();
        }

        private void gadgetButton_Click(object sender, EventArgs e)
        {
            if (GadgetsWindow == null || GadgetsWindow.IsDisposed)
            {
                GadgetsWindow = new Sly2Gadgets(game);
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

        private void reloadButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 1:
                    game.SetActiveCharacter(7);
                    break;
                case 2:
                    game.SetActiveCharacter(8);
                    break;
                case 3:
                    game.SetActiveCharacter(9);
                    break;
            }
            game.TriggerGameLoad((uint)0);
        }

        private void killButton_Click(object sender, EventArgs e)
        {
            switch (reloadAsCharacterComboBox.SelectedIndex)
            {
                case 0:
                    game.SetActiveCharacter(7);
                    break;
                case 1:
                    game.SetActiveCharacter(8);
                    break;
                case 2:
                    game.SetActiveCharacter(9);
                    break;
            }
            game.KillActiveCharacter();
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

        private void setCoinsButton_Click(object sender, EventArgs e)
        {
            SetCoinsFromTextBox();
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
            if (positionEditorWindow != null && !positionEditorWindow.IsDisposed)
            {
                positionEditorWindow.Close();
            }
        }

        private void ReconnectGame()
        {
            GameReconnect.Result result = GameReconnect.WaitForGame(game.api, gameNameId, "Sly 2", out int pid, out string runningTitleId);
            if (GameReconnect.HandleOtherResult(result, this, "Sly 2", runningTitleId, false))
            {
                return;
            }

            // Update PID for new game session
            AttachPS3Form.pid = pid;
            game.pid = pid;

            // Give game extra time to fully initialize
            Thread.Sleep(2000);

            // Re-establish memory subscriptions
            game.SetupInputDisplayMemorySubs();
            game.SetupWebManPopUp();
            game.SetupLoadWatcher();

            // Restart input timer if needed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                game.InputsTimer.Start();
            }

            game.api.Notify($"SluMAN v{Assembly.GetEntryAssembly().GetName().Version.ToString(3)} (Practice Mode)");
            Console.WriteLine("Sly 2: Reconnection complete");
            func.Status("Reconnected to Sly 2.");
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

        private void ApplySavedPreferences()
        {
            var prefersAlwaysOnTop = bool.TryParse(func.GetConfigData("config.txt", PrefersAlwaysOnTopKey), out bool alwaysOnTopEnabled) && alwaysOnTopEnabled;
            alwaysOnTopCheckBox.Checked = prefersAlwaysOnTop;
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

        private void setHealthButton_Click(object sender, EventArgs e)
        {
            SetHealthFromTextBox();
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            OpenInputDisplay();
        }

        private void loadMapButton_Click(object sender, EventArgs e)
        {
            game.LoadMap(mapComboBox.SelectedIndex);
        }

        private void loadJobButton_Click(object sender, EventArgs e)
        {
            game.LoadJob(jobComboBox.Text);
        }
    }
}
