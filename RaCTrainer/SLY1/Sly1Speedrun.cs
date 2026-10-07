using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace racman
{
    public partial class Sly1Speedrun : Form
    {
        private const string PrefersAutosplitterKey = "prefersAutosplitter";
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        public Form InputDisplay;
        public sly1 game;
        public string gameNameId = "NPUA80663";
        public AutosplitterHelper autosplitter;

        private StatusLine statusLine;

        public Sly1Speedrun(sly1 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            ApplySavedPreferences();

            game.SetupInputDisplayMemorySubs();

            GameReconnect.WatchRpcs3(this, game.api, gameNameId, true);

            if (func.api is Ratchetron r)
            {
                r.setDisconnectCallback(() =>
                {
                    DisconnectGame(false);
                });

                r.setReconnectCallback(() =>
                {
                    ReconnectGame();
                });
            }
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            OpenInputDisplay();
        }

        private void OpenInputDisplay()
        {
            if (InputDisplay == null || InputDisplay.IsDisposed)
            {
                InputDisplay = new InputDisplay();
                InputDisplay.Show();
            }
            else
            {
                InputDisplay.Focus();
            }
        }

        private void DisconnectGame(bool closeInputDisplay = true)
        {
            if (game.api is Ratchetron ratchetron)
            {
                if (autosplitter != null)
                {
                    autosplitter.Stop();
                    autosplitter = null;
                }
                ratchetron.ReleaseAllSubs();
            }
            if (closeInputDisplay)
            {
                try { game.api.Disconnect(); } catch { }
            }
            CloseAdditionalWindows(closeInputDisplay);
        }

        private void ApplySavedPreferences()
        {
            var prefersAutosplitter = bool.TryParse(func.GetConfigData("config.txt", PrefersAutosplitterKey), out bool autosplitterEnabled) && autosplitterEnabled;
            var prefersAlwaysOnTop = bool.TryParse(func.GetConfigData("config.txt", PrefersAlwaysOnTopKey), out bool alwaysOnTopEnabled) && alwaysOnTopEnabled;

            autosplitterCheckbox.Checked = prefersAutosplitter;
            alwaysOnTopCheckBox.Checked = prefersAlwaysOnTop;
        }

        private void CloseAdditionalWindows(bool closeInputDisplay = true)
        {
            if (closeInputDisplay && InputDisplay != null && !InputDisplay.IsDisposed)
            {
                InputDisplay.Close();
            }
        }

        private void ReconnectGame()
        {
            GameReconnect.Result result = GameReconnect.WaitForGame(game.api, gameNameId, "Sly 1", out int pid, out string runningTitleId);
            if (GameReconnect.HandleOtherResult(result, this, "Sly 1", runningTitleId, true))
            {
                return;
            }

            // Update PID for new game session
            AttachPS3Form.pid = pid;
            game.pid = pid;

            // Give game extra time to fully initialize
            Thread.Sleep(2000);

            // Re-establish memory subscriptions

            // Restart input timer if needed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                game.InputsTimer.Start();
            }

            // Restart autosplitter if it was running
            if (autosplitterCheckbox.Checked)
            {
                Console.WriteLine("Restarting autosplitter...");
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(this.game);
                Console.WriteLine("Autosplitter restarted successfully.");
            }

            game.SetupInputDisplayMemorySubs();

            game.api.Notify($"SluMAN {func.VersionText} (Speedrun Mode)");
            Console.WriteLine("Game reconnected.");
            func.Status("Reconnected to Sly 1.");
        }

        private void alwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void inputDisplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenInputDisplay();
        }

        private void Sly1Speedrun_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Program.AttachPS3Form.Visible == false && !AttachPS3Form.switchPending)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
            }
        }

        private void autosplitterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            func.ChangeFileLines("config.txt", autosplitterCheckbox.Checked ? "true" : "false", PrefersAutosplitterKey);

            if (!autosplitterCheckbox.Checked)
            {
                autosplitter?.Stop();
                autosplitter = null;
            }
            else
            {
                autosplitter?.Stop();
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(this.game);
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

        private void switchGameModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            this.Close();
            Program.AttachPS3Form.Show();
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryForm.ShowFor(this);
        }
    }
}
