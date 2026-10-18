using System;
using System.Globalization;
using System.Windows.Forms;

namespace SluMAN
{
    public partial class Sly4Practice : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        private StatusLine statusLine;
        private GameSession session;

        public Sly4Practice(sly4 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);
            func.BindEnter(coinsTextBox, setCoinsButton);
            func.BindEnter(zTextBox, teleportButton);

            for (int slot = 1; slot <= IGame.PositionSlotCount; slot++)
            {
                slotComboBox.Items.Add($"Slot {slot}");
            }
            slotComboBox.SelectedIndex = game.selectedPositionIndex;

            try
            {
                slowMotionCheckBox.Checked = game.IsSlowMotionOn();
            }
            catch
            {
                // Left unchecked; ticking it writes both values anyway.
            }

            // Controller combos run on the inputs timer, so it runs for as long as the form is open.
            game.combosActive = true;
            game.InputsTimer.Start();

            session = new GameSession(this, game, gameNameId, "Sly 4", false);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.Reconnected += session_Reconnected;

            refreshTimer.Start();
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                coinsValueLabel.Text = game.GetCoinCount().ToString();
            }
            catch
            {
                coinsValueLabel.Text = "-";
            }

            try
            {
                positionValueLabel.Text = FormatVector(game.GetPosition());
                velocityValueLabel.Text = FormatVector(game.GetVelocity());
            }
            catch
            {
                positionValueLabel.Text = "-";
                velocityValueLabel.Text = "-";
            }
        }

        private static string FormatVector(float[] vector)
        {
            if (vector == null)
            {
                return "-";
            }
            return string.Join(", ", Array.ConvertAll(vector, v => v.ToString("F2", CultureInfo.InvariantCulture)));
        }

        private void setCoinsButton_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(coinsTextBox.Text, out int coins))
            {
                statusLine.Error("Coins must be a whole number.");
                return;
            }

            try
            {
                game.SetCoinCount(coins);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't set coins: {ex.Message}");
            }
        }

        private void currentPositionButton_Click(object sender, EventArgs e)
        {
            float[] position;
            try
            {
                position = game.GetPosition();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the position: {ex.Message}");
                return;
            }

            if (position == null)
            {
                statusLine.Error("The player isn't loaded.");
                return;
            }

            xTextBox.Text = position[0].ToString("F3", CultureInfo.InvariantCulture);
            yTextBox.Text = position[1].ToString("F3", CultureInfo.InvariantCulture);
            zTextBox.Text = position[2].ToString("F3", CultureInfo.InvariantCulture);
        }

        private void teleportButton_Click(object sender, EventArgs e)
        {
            if (!TryParseCoordinate(xTextBox.Text, out float x)
                || !TryParseCoordinate(yTextBox.Text, out float y)
                || !TryParseCoordinate(zTextBox.Text, out float z))
            {
                statusLine.Error("X, Y and Z must be numbers.");
                return;
            }

            try
            {
                game.Teleport(x, y, z);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't teleport: {ex.Message}");
            }
        }

        /// <summary>Accepts both "1.5" and "1,5", since Windows may use either.</summary>
        private static bool TryParseCoordinate(string text, out float value)
        {
            return float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void slotComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            game.selectedPositionIndex = slotComboBox.SelectedIndex;
        }

        private void savePosButton_Click(object sender, EventArgs e)
        {
            try
            {
                game.SavePosition();
                statusLine.Info($"Saved position to slot {game.selectedPositionIndex + 1}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save the position: {ex.Message}");
            }
        }

        private void loadPosButton_Click(object sender, EventArgs e)
        {
            try
            {
                game.LoadPosition();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the position: {ex.Message}");
            }
        }

        private void slowMotionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                game.SetSlowMotion(slowMotionCheckBox.Checked);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the game speed: {ex.Message}");
            }
        }

        private void session_Reconnected()
        {
            // A game reset puts the tunables back to normal speed.
            if (slowMotionCheckBox.Checked)
            {
                try
                {
                    game.SetSlowMotion(true);
                }
                catch (Exception ex)
                {
                    statusLine.Error($"Couldn't turn slow motion back on: {ex.Message}");
                }
            }
        }

        private void Sly4Practice_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop();

            // Leave the game at normal speed. This runs before the session disconnects.
            if (slowMotionCheckBox.Checked)
            {
                try { game.SetSlowMotion(false); } catch { }
            }
        }

        private void switchGameModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryForm.ShowFor(this);
        }

        private void openUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserData.OpenInExplorer();
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.RebootPS3();
        }
    }
}
