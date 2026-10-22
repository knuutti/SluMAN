using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// Sly 3's Camera tab: the free camera (Sly3FreeCamera). It keeps running when the tab is
    /// left; Sly3Practice stops it when the window closes. The controls are built in code.
    /// </summary>
    public class Sly3CameraForm : Form, IRefreshOnShow
    {
        private const int MaxLookDegrees = 360;
        private const int MaxMoveSpeed = 20000;
        // How far, and for how long, Find camera raises each copy of the camera's position to see
        // whether the camera follows.
        private const float CameraTestRaise = 100f;
        private const int CameraTestMs = 2000;

        // Where the game's data can be: main memory (the EBOOT's data and the heap) and the user
        // memory above it.
        private static readonly uint[][] CameraSearchRanges =
        {
            new uint[] { 0x00010000, 0x10000000 },
            new uint[] { 0x30000000, 0x40000000 },
        };

        // The last Find camera result, to point out the copies that are found again.
        private List<uint> lastCameraFinds;

        private readonly sly3 game;
        private readonly Sly3FreeCamera freeCamera;

        private readonly CheckBox freeCameraCheckBox = new CheckBox();
        private readonly ComboBox cameraComboBox = new ComboBox();
        // The camera each item of cameraComboBox drives; 0 is the main one, whatever it is then.
        private readonly List<uint> cameraChoices = new List<uint>();
        private readonly NumericUpDown moveSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown lookSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown fovUpDown = new NumericUpDown();
        private readonly CheckBox characterControlCheckBox = new CheckBox();
        // X, Y, Z, yaw, pitch.
        private readonly TextBox[] cameraBoxes = new TextBox[5];
        private readonly Label drivenLabel = new Label();
        // Keeps the field of view box showing the camera's (R2 / L2 change it).
        private readonly Timer fovTimer = new Timer();
        // Set while the box is updated from the camera, so that isn't taken as an edit.
        private bool updatingFov;
        private StatusLine statusLine;

        public Sly3CameraForm(sly3 game, Sly3FreeCamera freeCamera)
        {
            this.game = game;
            this.freeCamera = freeCamera;
            Name = "Sly3CameraForm";
            Text = "SluMAN :: Camera";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            GroupBox freeCameraGroupBox = BuildFreeCameraGroup();
            GroupBox cameraGroupBox = BuildCameraGroup(new Point(freeCameraGroupBox.Right + 12, 8));
            ClientSize = new Size(cameraGroupBox.Right + 8, Math.Max(freeCameraGroupBox.Bottom, cameraGroupBox.Bottom) + 8);
            statusLine = new StatusLine(this, false);

            freeCamera.ModeChanged += freeCamera_ModeChanged;
            freeCamera.Notice += freeCamera_Notice;
            FormClosing += (sender, e) =>
            {
                freeCamera.ModeChanged -= freeCamera_ModeChanged;
                freeCamera.Notice -= freeCamera_Notice;
                fovTimer.Stop();
            };
            FormClosed += (sender, e) => fovTimer.Dispose();

            fovTimer.Interval = 100;
            fovTimer.Tick += (sender, e) => ShowFov();
            fovTimer.Start();

            ReadCameras();
            ShowMode();
        }

        // --- Layout ---

        private GroupBox BuildFreeCameraGroup()
        {
            GroupBox group = new GroupBox();
            group.Text = "Free Camera";
            group.Location = new Point(8, 8);
            group.Size = new Size(312, 238);
            Controls.Add(group);

            freeCameraCheckBox.Text = "Free camera";
            freeCameraCheckBox.AutoSize = true;
            freeCameraCheckBox.Location = new Point(10, 20);
            freeCameraCheckBox.Click += freeCameraCheckBox_Click;
            group.Controls.Add(freeCameraCheckBox);

            Label cameraLabel = new Label();
            cameraLabel.Text = "Camera:";
            cameraLabel.AutoSize = true;
            cameraLabel.Location = new Point(8, 49);
            group.Controls.Add(cameraLabel);

            cameraComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            cameraComboBox.Location = new Point(60, 46);
            cameraComboBox.Size = new Size(170, 21);
            group.Controls.Add(cameraComboBox);

            Button refreshButton = new Button();
            SetButton(refreshButton, "Refresh", 236, 45, 66, (sender, e) => ReadCameras());
            group.Controls.Add(refreshButton);

            Label moveLabel = new Label();
            moveLabel.Text = "Move speed (units / s):";
            moveLabel.AutoSize = true;
            moveLabel.Location = new Point(8, 79);
            group.Controls.Add(moveLabel);

            moveSpeedUpDown.Minimum = 10;
            moveSpeedUpDown.Maximum = MaxMoveSpeed;
            moveSpeedUpDown.Increment = 100;
            moveSpeedUpDown.Value = (decimal)Math.Max(10, Math.Min(MaxMoveSpeed, freeCamera.CameraSpeed));
            moveSpeedUpDown.Location = new Point(232, 76);
            moveSpeedUpDown.Size = new Size(70, 20);
            moveSpeedUpDown.ValueChanged += (sender, e) => freeCamera.CameraSpeed = (float)moveSpeedUpDown.Value;
            group.Controls.Add(moveSpeedUpDown);

            Label lookLabel = new Label();
            lookLabel.Text = "Turn speed (degrees / s):";
            lookLabel.AutoSize = true;
            lookLabel.Location = new Point(8, 105);
            group.Controls.Add(lookLabel);

            lookSpeedUpDown.Minimum = 10;
            lookSpeedUpDown.Maximum = MaxLookDegrees;
            lookSpeedUpDown.Increment = 10;
            lookSpeedUpDown.Value = (decimal)Math.Max(10, Math.Min(MaxLookDegrees, Math.Round(Degrees(freeCamera.LookSpeed))));
            lookSpeedUpDown.Location = new Point(232, 102);
            lookSpeedUpDown.Size = new Size(70, 20);
            lookSpeedUpDown.ValueChanged += (sender, e) => freeCamera.LookSpeed = Radians((float)lookSpeedUpDown.Value);
            group.Controls.Add(lookSpeedUpDown);

            Label fovLabel = new Label();
            fovLabel.Text = "Field of view (degrees):";
            fovLabel.AutoSize = true;
            fovLabel.Location = new Point(8, 131);
            group.Controls.Add(fovLabel);

            Button gameFovButton = new Button();
            SetButton(gameFovButton, "Game's", 168, 126, 58, (sender, e) => freeCamera.Fov = freeCamera.GameFov);
            group.Controls.Add(gameFovButton);

            fovUpDown.DecimalPlaces = 2;
            fovUpDown.Minimum = (decimal)Math.Ceiling(Degrees(Sly3FreeCamera.MinFov) * 100) / 100;
            fovUpDown.Maximum = (decimal)Math.Floor(Degrees(Sly3FreeCamera.MaxFov));
            fovUpDown.Value = (decimal)Math.Round(Degrees(freeCamera.GameFov), 2);
            fovUpDown.Location = new Point(232, 128);
            fovUpDown.Size = new Size(70, 20);
            fovUpDown.ValueChanged += (sender, e) =>
            {
                if (!updatingFov)
                {
                    freeCamera.Fov = Radians((float)fovUpDown.Value);
                }
            };
            group.Controls.Add(fovUpDown);

            characterControlCheckBox.Text = "Control character (camera stays put)";
            characterControlCheckBox.AutoSize = true;
            characterControlCheckBox.Location = new Point(10, 156);
            characterControlCheckBox.Checked = freeCamera.CharacterControl;
            characterControlCheckBox.CheckedChanged += (sender, e) => freeCamera.CharacterControl = characterControlCheckBox.Checked;
            group.Controls.Add(characterControlCheckBox);

            Label help = new Label();
            help.Text = "Left stick moves, right stick turns, R1 / L1 move camera-up,\n" +
                "R2 / L2 zoom. The player is held where they stood. The\n" +
                "camera turns off when a level loads. On RPCS3, set the PPU\n" +
                "decoder to Interpreter (static) so the game's camera stays out.";
            help.Location = new Point(8, 180);
            help.Size = new Size(298, 54);
            group.Controls.Add(help);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(cameraComboBox, "Which camera to drive: the main one, or one from the game's camera list.");
            tip.SetToolTip(refreshButton, "Reads the game's camera list again.");
            tip.SetToolTip(fovUpDown, "The free camera's field of view. R2 zooms in, L2 out.");
            tip.SetToolTip(gameFovButton, "Back to the field of view the game had when the free camera started.");
            tip.SetToolTip(characterControlCheckBox,
                "The free camera stays where it is and the controller moves the character as usual.\n" +
                "Untick to fly the camera again.");
            FormClosed += (sender, e) => tip.Dispose();
            return group;
        }

        /// <summary>The free camera's exact position and facing, to read and type in.</summary>
        private GroupBox BuildCameraGroup(Point location)
        {
            GroupBox group = new GroupBox();
            group.Text = "Camera";
            group.Location = location;
            group.Size = new Size(312, 130);
            Controls.Add(group);

            string[] labels = { "X", "Y", "Z", "Yaw", "Pitch" };
            for (int i = 0; i < cameraBoxes.Length; i++)
            {
                int x = 8 + (i % 3) * 100;
                int y = 20 + (i / 3) * 27;
                Label label = new Label();
                label.Text = labels[i] + ":";
                label.AutoSize = true;
                label.Location = new Point(x, y + 3);
                group.Controls.Add(label);

                cameraBoxes[i] = new TextBox();
                cameraBoxes[i].Location = new Point(x + 34, y);
                cameraBoxes[i].Size = new Size(60, 20);
                group.Controls.Add(cameraBoxes[i]);
            }

            AddButton(group, "Current", 8, 74, 100, (sender, e) => ReadCameraBoxes());
            Button setButton = new Button();
            SetButton(setButton, "Set Camera", 114, 74, 100, (sender, e) => WriteCameraBoxes());
            group.Controls.Add(setButton);
            Button findButton = new Button();
            SetButton(findButton, "Find camera", 220, 74, 84, (sender, e) => FindCamera());
            group.Controls.Add(findButton);
            foreach (TextBox box in cameraBoxes)
            {
                func.BindEnter(box, setButton);
            }

            drivenLabel.AutoSize = true;
            drivenLabel.Location = new Point(8, 106);
            group.Controls.Add(drivenLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(setButton, "Puts the free camera here (turning it on). Yaw and pitch in degrees; Z is up.");
            tip.SetToolTip(findButton,
                "Looks for every copy of the camera's position in memory (RPCS3 only). Move the camera\n" +
                "and search again to test which copy the camera is made from.");
            tip.SetToolTip(drivenLabel, "The camera being driven, and where its matrix is in it.");
            FormClosed += (sender, e) => tip.Dispose();
            return group;
        }

        private void AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            Button button = new Button();
            SetButton(button, text, x, y, width, click);
            parent.Controls.Add(button);
        }

        private static void SetButton(Button button, string text, int x, int y, int width, EventHandler click)
        {
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
        }

        // --- Free camera ---

        private void freeCameraCheckBox_Click(object sender, EventArgs e)
        {
            try
            {
                if (freeCameraCheckBox.Checked)
                {
                    freeCamera.CameraSpeed = (float)moveSpeedUpDown.Value;
                    freeCamera.Start(SelectedCamera());
                    if (!freeCamera.FovKnown)
                    {
                        statusLine.Info("The camera's field of view isn't one SluMAN knows, so zoom is off.");
                    }
                }
                else
                {
                    freeCamera.Stop();
                }
            }
            catch (Exception ex)
            {
                freeCamera.Stop();
                statusLine.Error($"Couldn't start the free camera: {ex.Message}");
            }
            ShowMode();
        }

        private uint SelectedCamera()
        {
            int index = cameraComboBox.SelectedIndex;
            return index >= 0 && index < cameraChoices.Count ? cameraChoices[index] : 0;
        }

        /// <summary>Fills the camera list: the main camera, then each in the game's camera array.</summary>
        private void ReadCameras()
        {
            uint selected = SelectedCamera();
            cameraChoices.Clear();
            cameraComboBox.Items.Clear();
            cameraChoices.Add(0);
            cameraComboBox.Items.Add("Main camera");
            try
            {
                uint main = game.GetMainCamera();
                uint[] cameras = game.GetCameras();
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] == 0)
                    {
                        continue;
                    }
                    cameraChoices.Add(cameras[i]);
                    cameraComboBox.Items.Add($"{i + 1}: 0x{cameras[i]:X}{(cameras[i] == main ? " (main)" : "")}");
                }
            }
            catch
            {
                // Only the main camera, which is looked up when the free camera starts.
            }
            int index = cameraChoices.IndexOf(selected);
            cameraComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private void freeCamera_ModeChanged()
        {
            ShowMode();
        }

        private void freeCamera_Notice(string message, bool isError)
        {
            if (IsDisposed)
            {
                return;
            }
            statusLine.Show(message, isError);
            ShowMode();
        }

        private void ShowMode()
        {
            if (IsDisposed)
            {
                return;
            }
            bool on = freeCamera.IsOn;
            freeCameraCheckBox.Checked = on;
            cameraComboBox.Enabled = !on;
            drivenLabel.Text = on
                ? $"Driving 0x{freeCamera.Camera:X}, matrix at +0x{freeCamera.MatrixOffset:X}, "
                    + (freeCamera.IsHooked ? "through the camera hook." : $"{freeCamera.MatrixCopyCount} drawn copies.")
                : "The free camera is off.";
            ShowFov();
        }

        private void ShowFov()
        {
            if (IsDisposed || fovUpDown.Focused || !freeCamera.IsOn)
            {
                return;
            }
            decimal value = (decimal)Math.Round(Degrees(freeCamera.Fov), 2);
            value = Math.Max(fovUpDown.Minimum, Math.Min(fovUpDown.Maximum, value));
            if (fovUpDown.Value != value)
            {
                updatingFov = true;
                fovUpDown.Value = value;
                updatingFov = false;
            }
        }

        private void ReadCameraBoxes()
        {
            float[] position;
            float yaw;
            float pitch;
            if (!freeCamera.GetCamera(out position, out yaw, out pitch))
            {
                statusLine.Error("Turn on the free camera first.");
                return;
            }
            float[] values = { position[0], position[1], position[2], Degrees(yaw), Degrees(pitch) };
            for (int i = 0; i < values.Length; i++)
            {
                cameraBoxes[i].Text = values[i].ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        private void WriteCameraBoxes()
        {
            float[] values = new float[cameraBoxes.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!float.TryParse(cameraBoxes[i].Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    statusLine.Error("X, Y, Z, yaw and pitch must all be numbers.");
                    return;
                }
            }
            try
            {
                if (!freeCamera.IsOn)
                {
                    freeCamera.CameraSpeed = (float)moveSpeedUpDown.Value;
                    freeCamera.Start(SelectedCamera());
                }
                freeCamera.SetCamera(new float[] { values[0], values[1], values[2] }, Radians(values[3]), Radians(values[4]));
                statusLine.Info("Moved the free camera.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't move the free camera: {ex.Message}");
            }
            ShowMode();
        }

        /// <summary>
        /// Looks through the game's memory for every other copy of the camera's position, to find
        /// where the game keeps the camera it writes into the camera struct each frame. On a second
        /// search, each copy found both times is raised for a moment while the camera is watched:
        /// a copy the camera is made from makes it rise too. RPCS3 only, which reads memory
        /// directly. The result goes on the clipboard.
        /// </summary>
        private void FindCamera()
        {
            if (!(game.api is RPCS3))
            {
                statusLine.Error("Finding the camera needs RPCS3: on a PS3 it would take thousands of reads.");
                return;
            }
            if (freeCamera.IsOn)
            {
                statusLine.Error("Turn off the free camera first: it writes the camera itself.");
                return;
            }
            try
            {
                uint camera = SelectedCamera();
                if (camera == 0)
                {
                    camera = game.GetMainCamera();
                }
                if (camera == 0)
                {
                    statusLine.Error("The game has no camera yet.");
                    return;
                }
                int matrixOffset = game.FindCameraMatrix(camera);
                if (matrixOffset < 0)
                {
                    statusLine.Error($"Found no camera matrix in the camera at 0x{camera:X}.");
                    return;
                }
                uint positionAddress = camera + (uint)matrixOffset + 0x30;

                Cursor = Cursors.WaitCursor;
                byte[] position = game.api.ReadMemory(game.pid, positionAddress, 12);
                List<uint> found = game.FindMemory(position, CameraSearchRanges);

                StringBuilder text = new StringBuilder();
                text.AppendLine($"Camera: {camera:X8}, matrix at +0x{matrixOffset:X}, position {BitConverter.ToString(position).Replace("-", " ")}");
                text.AppendLine($"(at {positionAddress:X8})");
                text.AppendLine();
                foreach (uint address in found)
                {
                    // Found by the last search too: a copy the game keeps up to date.
                    bool again = lastCameraFinds != null && lastCameraFinds.Contains(address);
                    string inCamera = address >= camera && address < camera + 0x1000 ? $"  (camera +0x{address - camera:X})" : "";
                    string matrix = game.IsCameraMatrixAt(address - 0x30) ? $"  (row 4 of a matrix at {address - 0x30:X8})" : "";
                    text.AppendLine($"{address:X8}{inCamera}{matrix}{(again ? "  (also last time)" : "")}");
                }
                if (found.Count == 0)
                {
                    text.AppendLine("No copies found.");
                }
                // The copies found both times, except the camera matrix's own.
                List<uint> live = new List<uint>();
                foreach (uint address in found)
                {
                    if (lastCameraFinds != null && lastCameraFinds.Contains(address) && address != positionAddress)
                    {
                        live.Add(address);
                    }
                }
                lastCameraFinds = found;

                if (live.Count > 0)
                {
                    text.AppendLine();
                    text.AppendLine($"Which copy moves the view (raising each by {CameraTestRaise} for {CameraTestMs / 1000} seconds):");
                    List<uint> moving = new List<uint>();
                    foreach (uint address in live)
                    {
                        Cursor = Cursors.Default;
                        if (MessageBox.Show(this, $"Raising the copy at {address:X8} for {CameraTestMs / 1000} seconds. Watch the game's view.",
                            "Find camera", MessageBoxButtons.OKCancel) != DialogResult.OK)
                        {
                            text.AppendLine("Stopped.");
                            break;
                        }
                        Cursor = Cursors.WaitCursor;
                        float moved = game.TestCameraCopy(address, camera, matrixOffset, CameraTestRaise, CameraTestMs);
                        Cursor = Cursors.Default;
                        bool viewMoved = MessageBox.Show(this, $"Did the view move up while {address:X8} was raised?",
                            "Find camera", MessageBoxButtons.YesNo) == DialogResult.Yes;
                        if (viewMoved)
                        {
                            moving.Add(address);
                        }
                        text.AppendLine($"{address:X8}: the view {(viewMoved ? "moved" : "didn't move")}, the camera struct rose by {moved.ToString("F2", CultureInfo.InvariantCulture)}");
                    }

                    // The free camera writes these along with the camera struct.
                    freeCamera.Copies = moving.ToArray();
                    text.AppendLine();
                    text.AppendLine(moving.Count > 0
                        ? $"The free camera also writes {string.Join(", ", moving.ConvertAll(a => a.ToString("X8")))}, on top of the copies it always writes (until SluMAN closes)."
                        : "None moved the view, so the free camera writes only the copies it always writes.");
                }
                else
                {
                    text.AppendLine();
                    text.AppendLine("Move the camera a little and search again to test which copy moves it.");
                }

                // Message boxes can't be selected from, so the result goes on the clipboard.
                bool copied = true;
                try { Clipboard.SetText(text.ToString()); } catch { copied = false; }
                text.AppendLine();
                text.AppendLine(copied ? "This text is on the clipboard: paste it anywhere." : "Couldn't copy this to the clipboard.");
                MessageBox.Show(this, text.ToString(), $"Camera position found {found.Count} times");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't search: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static float Degrees(float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }

        private static float Radians(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }

        public void RefreshFromGame()
        {
            if (!freeCamera.IsOn)
            {
                ReadCameras();
            }
            ShowMode();
        }
    }
}
