using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Camera tab: the free camera and how fast it moves and turns. The camera keeps running
    /// when the tab is left; Sly4Practice stops it when the window closes. The controls are built
    /// in code.
    /// </summary>
    public class Sly4CameraForm : Form, IRefreshOnShow
    {
        private const int MaxLookDegrees = 360;

        private readonly Sly4FreeMove freeMove;

        private readonly CheckBox freeCameraCheckBox = new CheckBox();
        private readonly NumericUpDown moveSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown lookSpeedUpDown = new NumericUpDown();
        private StatusLine statusLine;

        public Sly4CameraForm(Sly4FreeMove freeMove)
        {
            this.freeMove = freeMove;
            Name = "Sly4CameraForm";
            Text = "SluMAN :: Camera";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            GroupBox freeCameraGroupBox = BuildFreeCameraGroup();
            ClientSize = new Size(freeCameraGroupBox.Right + 8, freeCameraGroupBox.Bottom + 8);
            statusLine = new StatusLine(this, false);

            freeMove.ModeChanged += ShowMode;
            FormClosing += (sender, e) => freeMove.ModeChanged -= ShowMode;
            ShowMode();
        }

        private GroupBox BuildFreeCameraGroup()
        {
            GroupBox group = new GroupBox();
            group.Text = "Free Camera";
            group.Location = new Point(8, 8);
            group.Size = new Size(284, 170);
            Controls.Add(group);

            freeCameraCheckBox.Text = "Free camera";
            freeCameraCheckBox.AutoSize = true;
            freeCameraCheckBox.Location = new Point(10, 20);
            freeCameraCheckBox.Click += freeCameraCheckBox_Click;
            group.Controls.Add(freeCameraCheckBox);

            Label moveLabel = new Label();
            moveLabel.Text = "Move speed (units / s):";
            moveLabel.AutoSize = true;
            moveLabel.Location = new Point(8, 49);
            group.Controls.Add(moveLabel);

            moveSpeedUpDown.Minimum = 1;
            moveSpeedUpDown.Maximum = 200;
            moveSpeedUpDown.Value = (decimal)Math.Max(1, Math.Min(200, freeMove.CameraSpeed));
            moveSpeedUpDown.Location = new Point(214, 46);
            moveSpeedUpDown.Size = new Size(60, 20);
            moveSpeedUpDown.ValueChanged += (sender, e) => freeMove.CameraSpeed = (float)moveSpeedUpDown.Value;
            group.Controls.Add(moveSpeedUpDown);

            Label lookLabel = new Label();
            lookLabel.Text = "Turn speed (degrees / s):";
            lookLabel.AutoSize = true;
            lookLabel.Location = new Point(8, 75);
            group.Controls.Add(lookLabel);

            lookSpeedUpDown.Minimum = 10;
            lookSpeedUpDown.Maximum = MaxLookDegrees;
            lookSpeedUpDown.Increment = 10;
            lookSpeedUpDown.Value = (decimal)Math.Max(10, Math.Min(MaxLookDegrees, Math.Round(freeMove.LookSpeed * 180 / Math.PI)));
            lookSpeedUpDown.Location = new Point(214, 72);
            lookSpeedUpDown.Size = new Size(60, 20);
            lookSpeedUpDown.ValueChanged += (sender, e) => freeMove.LookSpeed = (float)((double)lookSpeedUpDown.Value * Math.PI / 180);
            group.Controls.Add(lookSpeedUpDown);

            Label help = new Label();
            help.Text = "Left stick moves, right stick turns, R1 / L1 go up and down.\n" +
                "The player is held where they stood. The camera turns off\n" +
                "when a level loads. Turning on fly mode turns it off too.\n" +
                "On RPCS3, set the PPU decoder to Interpreter (static).";
            help.Location = new Point(8, 102);
            help.Size = new Size(270, 60);
            group.Controls.Add(help);

            return group;
        }

        private void freeCameraCheckBox_Click(object sender, EventArgs e)
        {
            try
            {
                freeMove.CameraSpeed = (float)moveSpeedUpDown.Value;
                freeMove.Start(freeCameraCheckBox.Checked ? Sly4FreeMove.Mode.FreeCamera : Sly4FreeMove.Mode.Off);
            }
            catch (Exception ex)
            {
                freeMove.Stop();
                statusLine.Error($"Couldn't start the free camera: {ex.Message}");
            }
            ShowMode();
        }

        private void ShowMode()
        {
            if (!IsDisposed)
            {
                freeCameraCheckBox.Checked = freeMove.Current == Sly4FreeMove.Mode.FreeCamera;
            }
        }

        public void RefreshFromGame()
        {
            ShowMode();
        }
    }
}
