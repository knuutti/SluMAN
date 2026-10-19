using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// The player's live position, velocity and horizontal speed (with the top speed reached), and
    /// teleporting to typed coordinates. Read only while its tab shows. The controls are built in
    /// code.
    /// </summary>
    public class Sly4PositionForm : Form, IRefreshOnShow
    {
        // RPCS3 reads its memory directly, so it can afford a smooth speedometer. On a PS3 every
        // read is a network round trip, and one refresh takes about eight.
        private const int Rpcs3RefreshIntervalMs = 50;
        private const int PS3RefreshIntervalMs = 200;

        private readonly sly4 game;

        private readonly Label positionValueLabel = new Label();
        private readonly Label velocityValueLabel = new Label();
        private readonly Label speedValueLabel = new Label();
        private readonly Label topSpeedValueLabel = new Label();
        private readonly Button resetTopSpeedButton = new Button();
        private readonly TextBox xTextBox = new TextBox();
        private readonly TextBox yTextBox = new TextBox();
        private readonly TextBox zTextBox = new TextBox();
        private readonly Button currentPositionButton = new Button();
        private readonly Button teleportButton = new Button();
        private readonly CheckBox flyCheckBox = new CheckBox();
        private readonly CheckBox freeCameraCheckBox = new CheckBox();
        private readonly NumericUpDown speedUpDown = new NumericUpDown();
        private readonly Timer refreshTimer = new Timer();
        private readonly Sly4FreeMove freeMove;
        private StatusLine statusLine;

        private float topSpeed;

        public Sly4PositionForm(sly4 game)
        {
            this.game = game;
            Name = "Sly4PositionForm";
            Text = "SluMAN :: Position";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            GroupBox liveGroupBox = BuildLiveGroup();
            GroupBox teleportGroupBox = BuildTeleportGroup(liveGroupBox.Bottom + 6);
            GroupBox freeMoveGroupBox = BuildFreeMoveGroup(teleportGroupBox.Bottom + 6);
            ClientSize = new Size(liveGroupBox.Right + 8, freeMoveGroupBox.Bottom + 8);
            statusLine = new StatusLine(this, false);

            freeMove = new Sly4FreeMove(game);
            freeMove.Stopped += (message) =>
            {
                ShowMode();
                statusLine.Error($"Fly / free camera stopped: {message}");
            };
            FormClosing += (sender, e) => freeMove.Stop();

            refreshTimer.Interval = game.api is RPCS3 ? Rpcs3RefreshIntervalMs : PS3RefreshIntervalMs;
            refreshTimer.Tick += refreshTimer_Tick;
            FormClosing += (sender, e) => refreshTimer.Stop();
            FormClosed += (sender, e) => refreshTimer.Dispose();
            refreshTimer.Start();
        }

        private GroupBox BuildLiveGroup()
        {
            GroupBox group = new GroupBox();
            group.Text = "Player";
            group.Location = new Point(8, 8);
            group.Size = new Size(284, 116);
            Controls.Add(group);

            Font mono = new Font("Consolas", 9F);
            AddRow(group, "Position:", positionValueLabel, mono, 22);
            AddRow(group, "Velocity:", velocityValueLabel, mono, 42);

            Label speedLabel = new Label();
            speedLabel.Text = "Speed:";
            speedLabel.AutoSize = true;
            speedLabel.Location = new Point(8, 68);
            group.Controls.Add(speedLabel);

            speedValueLabel.Font = new Font("Consolas", 14F, FontStyle.Bold);
            speedValueLabel.AutoSize = true;
            speedValueLabel.Location = new Point(70, 62);
            speedValueLabel.Text = "-";
            group.Controls.Add(speedValueLabel);

            AddRow(group, "Top speed:", topSpeedValueLabel, mono, 90);

            resetTopSpeedButton.Text = "Reset";
            resetTopSpeedButton.Location = new Point(210, 85);
            resetTopSpeedButton.Size = new Size(64, 23);
            resetTopSpeedButton.UseVisualStyleBackColor = true;
            resetTopSpeedButton.Click += (sender, e) =>
            {
                topSpeed = 0;
                topSpeedValueLabel.Text = FormatSpeed(topSpeed);
            };
            group.Controls.Add(resetTopSpeedButton);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(speedValueLabel, "Horizontal speed: the length of the X and Z velocity. Y (height) isn't counted.");
            tip.SetToolTip(resetTopSpeedButton, "Starts tracking the top speed again.");
            FormClosed += (sender, e) => tip.Dispose();
            return group;
        }

        private static void AddRow(GroupBox group, string name, Label valueLabel, Font font, int y)
        {
            Label nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.AutoSize = true;
            nameLabel.Location = new Point(8, y);
            group.Controls.Add(nameLabel);

            valueLabel.Font = font;
            valueLabel.AutoSize = true;
            valueLabel.Location = new Point(70, y - 1);
            valueLabel.Text = "-";
            group.Controls.Add(valueLabel);
        }

        private GroupBox BuildTeleportGroup(int top)
        {
            GroupBox group = new GroupBox();
            group.Text = "Teleport";
            group.Location = new Point(8, top);
            group.Size = new Size(284, 78);
            Controls.Add(group);

            xTextBox.Location = new Point(10, 20);
            yTextBox.Location = new Point(76, 20);
            zTextBox.Location = new Point(142, 20);
            foreach (TextBox box in new[] { xTextBox, yTextBox, zTextBox })
            {
                box.Size = new Size(60, 20);
                group.Controls.Add(box);
            }

            currentPositionButton.Text = "Current";
            currentPositionButton.Location = new Point(208, 18);
            currentPositionButton.Size = new Size(66, 23);
            currentPositionButton.UseVisualStyleBackColor = true;
            currentPositionButton.Click += currentPositionButton_Click;
            group.Controls.Add(currentPositionButton);

            teleportButton.Text = "Teleport to X, Y, Z";
            teleportButton.Location = new Point(10, 46);
            teleportButton.Size = new Size(264, 23);
            teleportButton.UseVisualStyleBackColor = true;
            teleportButton.Click += teleportButton_Click;
            group.Controls.Add(teleportButton);

            func.BindEnter(zTextBox, teleportButton);
            return group;
        }

        private GroupBox BuildFreeMoveGroup(int top)
        {
            GroupBox group = new GroupBox();
            group.Text = "Fly / Free camera";
            group.Location = new Point(8, top);
            group.Size = new Size(284, 92);
            Controls.Add(group);

            flyCheckBox.Text = "Fly";
            flyCheckBox.AutoSize = true;
            flyCheckBox.Location = new Point(10, 20);
            flyCheckBox.Click += (sender, e) => SetMode(flyCheckBox.Checked ? Sly4FreeMove.Mode.Fly : Sly4FreeMove.Mode.Off);
            group.Controls.Add(flyCheckBox);

            freeCameraCheckBox.Text = "Free camera";
            freeCameraCheckBox.AutoSize = true;
            freeCameraCheckBox.Location = new Point(70, 20);
            freeCameraCheckBox.Click += (sender, e) => SetMode(freeCameraCheckBox.Checked ? Sly4FreeMove.Mode.FreeCamera : Sly4FreeMove.Mode.Off);
            group.Controls.Add(freeCameraCheckBox);

            Label speedLabel = new Label();
            speedLabel.Text = "Speed:";
            speedLabel.AutoSize = true;
            speedLabel.Location = new Point(170, 21);
            group.Controls.Add(speedLabel);

            speedUpDown.Minimum = 1;
            speedUpDown.Maximum = 200;
            speedUpDown.Value = 10;
            speedUpDown.Location = new Point(214, 18);
            speedUpDown.Size = new Size(60, 20);
            speedUpDown.ValueChanged += (sender, e) => freeMove.Speed = (float)speedUpDown.Value;
            group.Controls.Add(speedUpDown);

            Label help = new Label();
            help.Text = "Left stick moves, R1 / L1 go up and down. The free camera\n" +
                "turns with the right stick and holds the player in place.";
            help.Location = new Point(8, 46);
            help.Size = new Size(270, 40);
            group.Controls.Add(help);
            return group;
        }

        private void SetMode(Sly4FreeMove.Mode mode)
        {
            try
            {
                freeMove.Speed = (float)speedUpDown.Value;
                freeMove.Start(mode);
            }
            catch (Exception ex)
            {
                freeMove.Stop();
                statusLine.Error($"Couldn't start: {ex.Message}");
            }
            ShowMode();
        }

        private void ShowMode()
        {
            flyCheckBox.Checked = freeMove.Current == Sly4FreeMove.Mode.Fly;
            freeCameraCheckBox.Checked = freeMove.Current == Sly4FreeMove.Mode.FreeCamera;
        }

        public void RefreshFromGame()
        {
            ReadMotion();
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            // Only while this tab is the one showing.
            if (!Visible || (Parent != null && !Parent.Visible))
            {
                return;
            }
            ReadMotion();
        }

        private void ReadMotion()
        {
            float[] position;
            float[] velocity;
            try
            {
                if (!game.ReadMotion(out position, out velocity))
                {
                    ShowNoPlayer();
                    return;
                }
            }
            catch
            {
                ShowNoPlayer();
                return;
            }

            positionValueLabel.Text = FormatVector(position);
            velocityValueLabel.Text = FormatVector(velocity);

            // Y is up, so the horizontal speed leaves it out.
            float speed = (float)Math.Sqrt(velocity[0] * velocity[0] + velocity[2] * velocity[2]);
            speedValueLabel.Text = FormatSpeed(speed);
            if (speed > topSpeed)
            {
                topSpeed = speed;
            }
            topSpeedValueLabel.Text = FormatSpeed(topSpeed);
        }

        private void ShowNoPlayer()
        {
            positionValueLabel.Text = "-";
            velocityValueLabel.Text = "-";
            speedValueLabel.Text = "-";
        }

        private static string FormatVector(float[] vector)
        {
            return string.Join(", ", Array.ConvertAll(vector, v => v.ToString("F2", CultureInfo.InvariantCulture)));
        }

        private static string FormatSpeed(float speed)
        {
            return speed.ToString("F2", CultureInfo.InvariantCulture);
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
    }
}
