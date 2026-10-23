using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Direct render-matrix camera controls for the researched Sly 2 KOR build.
    /// </summary>
    public class Sly2CameraForm : Form
    {
        private readonly Sly2FreeCamera freeCamera;
        private readonly CheckBox enabledCheckBox = new CheckBox();
        private readonly CheckBox characterControlCheckBox = new CheckBox();
        private readonly NumericUpDown moveSpeed = new NumericUpDown();
        private readonly NumericUpDown lookSpeed = new NumericUpDown();
        private readonly TextBox[] cameraBoxes = new TextBox[5];
        private readonly Timer refreshTimer = new Timer();
        private bool updating;

        public Sly2CameraForm(sly2 game, Sly2FreeCamera freeCamera)
        {
            this.freeCamera = freeCamera;
            Text = "SluMAN :: Sly 2 Camera";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(330, 220);

            GroupBox controls = new GroupBox();
            controls.Text = "Free camera";
            controls.Location = new Point(8, 8);
            controls.Size = new Size(314, 204);
            Controls.Add(controls);

            enabledCheckBox.Text = "Free camera";
            enabledCheckBox.AutoSize = true;
            enabledCheckBox.Location = new Point(10, 22);
            enabledCheckBox.Click += enabledCheckBox_Click;
            controls.Controls.Add(enabledCheckBox);

            characterControlCheckBox.Text = "Control player";
            characterControlCheckBox.AutoSize = true;
            characterControlCheckBox.Location = new Point(112, 22);
            characterControlCheckBox.CheckedChanged += (sender, e) =>
            {
                freeCamera.CharacterControl = characterControlCheckBox.Checked;
            };
            controls.Controls.Add(characterControlCheckBox);

            AddSpeedControl(controls, "Move speed:", moveSpeed, 10, 54, 1, 20000, 1600);
            moveSpeed.ValueChanged += (sender, e) => freeCamera.CameraSpeed = (float)moveSpeed.Value;
            AddSpeedControl(controls, "Look speed:", lookSpeed, 160, 54, 1, 360, 115);
            lookSpeed.ValueChanged += (sender, e) => freeCamera.LookSpeed = (float)lookSpeed.Value * (float)Math.PI / 180;

            Label help = new Label();
            help.AutoSize = false;
            help.Location = new Point(10, 88);
            help.Size = new Size(290, 38);
            help.Text = "Left stick: move  Right stick: look\r\nR1/L1: up/down";
            controls.Controls.Add(help);

            string[] names = { "X", "Y", "Z", "Yaw", "Pitch" };
            for (int i = 0; i < cameraBoxes.Length; i++)
            {
                Label label = new Label();
                label.Text = names[i];
                label.AutoSize = true;
                label.Location = new Point(10 + (i % 3) * 98, 135 + (i / 3) * 28);
                controls.Controls.Add(label);
                cameraBoxes[i] = new TextBox();
                cameraBoxes[i].Location = new Point(label.Left + 25, label.Top - 3);
                cameraBoxes[i].Size = new Size(65, 20);
                controls.Controls.Add(cameraBoxes[i]);
            }

            Button setButton = new Button();
            setButton.Text = "Set camera";
            setButton.Location = new Point(215, 160);
            setButton.Size = new Size(82, 26);
            setButton.Click += (sender, e) => SetCamera();
            controls.Controls.Add(setButton);

            moveSpeed.Value = 1600;
            lookSpeed.Value = 115;
            refreshTimer.Interval = 100;
            refreshTimer.Tick += (sender, e) => RefreshCamera();
            refreshTimer.Start();
            FormClosing += (sender, e) =>
            {
                refreshTimer.Stop();
                freeCamera.Stop();
            };
        }

        private static void AddSpeedControl(Control parent, string text, NumericUpDown control,
            int x, int y, int minimum, int maximum, int value)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y + 3);
            parent.Controls.Add(label);
            control.Minimum = minimum;
            control.Maximum = maximum;
            control.Location = new Point(x + 70, y);
            control.Size = new Size(70, 20);
            control.Value = value;
            parent.Controls.Add(control);
        }

        private void enabledCheckBox_Click(object sender, EventArgs e)
        {
            try
            {
                if (enabledCheckBox.Checked)
                {
                    freeCamera.Start();
                }
                else
                {
                    freeCamera.Stop();
                }
            }
            catch (Exception ex)
            {
                enabledCheckBox.Checked = false;
                MessageBox.Show(this, $"Couldn't change the free camera: {ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshCamera()
        {
            if (IsDisposed)
            {
                return;
            }
            bool on = freeCamera.IsOn;
            if (enabledCheckBox.Checked != on)
            {
                enabledCheckBox.Checked = on;
            }
            if (!on || updating)
            {
                return;
            }
            float[] position;
            float yaw;
            float pitch;
            if (!freeCamera.GetCamera(out position, out yaw, out pitch))
            {
                return;
            }
            updating = true;
            cameraBoxes[0].Text = Format(position[0]);
            cameraBoxes[1].Text = Format(position[1]);
            cameraBoxes[2].Text = Format(position[2]);
            cameraBoxes[3].Text = Format(yaw * 180 / (float)Math.PI);
            cameraBoxes[4].Text = Format(pitch * 180 / (float)Math.PI);
            updating = false;
        }

        private void SetCamera()
        {
            try
            {
                float[] position =
                {
                    float.Parse(cameraBoxes[0].Text, CultureInfo.InvariantCulture),
                    float.Parse(cameraBoxes[1].Text, CultureInfo.InvariantCulture),
                    float.Parse(cameraBoxes[2].Text, CultureInfo.InvariantCulture),
                };
                float yaw = float.Parse(cameraBoxes[3].Text, CultureInfo.InvariantCulture) * (float)Math.PI / 180;
                float pitch = float.Parse(cameraBoxes[4].Text, CultureInfo.InvariantCulture) * (float)Math.PI / 180;
                if (!freeCamera.SetCamera(position, yaw, pitch))
                {
                    throw new InvalidOperationException("Turn on the free camera first.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't set the camera: {ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
