using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Camera tab: the free camera, and camera paths (keyframes the camera flies through,
    /// played back by Sly4FreeMove). The camera keeps running when the tab is left; Sly4Practice
    /// stops it when the window closes. The controls are built in code.
    /// </summary>
    public class Sly4CameraForm : Form, IRefreshOnShow
    {
        private const int MaxLookDegrees = 360;
        private const int ScrubSteps = 1000;
        private const int CountdownSeconds = 3;
        private const int PlaybackRefreshMs = 50;

        private readonly sly4 game;
        private readonly Sly4FreeMove freeMove;
        private CameraPath path = new CameraPath();

        private readonly CheckBox freeCameraCheckBox = new CheckBox();
        private readonly NumericUpDown moveSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown lookSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown fovUpDown = new NumericUpDown();
        private readonly CheckBox hideHudCheckBox = new CheckBox();
        private readonly CheckBox characterControlCheckBox = new CheckBox();
        // X, Y, Z, yaw, pitch, field of view (Camera group).
        private readonly TextBox[] cameraBoxes = new TextBox[6];
        private readonly NumericUpDown farUpDown = new NumericUpDown();
        private readonly Sly4CameraApi api;
        private readonly CheckBox apiCheckBox = new CheckBox();
        private readonly CheckBox noFogCheckBox = new CheckBox();
        // No fog is checked again every this many field-of-view refreshes (a load sets the fog).
        private const int NoFogRefreshTicks = 10;
        private int noFogTick;
        private bool updatingFar;
        // Keeps the field of view box showing the camera's (R2 / L2 and paths change it).
        private readonly Timer fovTimer = new Timer();
        // Set while the box is updated from the camera, so that isn't taken as an edit.
        private bool updatingFov;

        private readonly GroupBox savedGroupBox = new GroupBox();
        private readonly ComboBox pathNameComboBox = new ComboBox();

        private readonly Button playButton = new Button();
        private readonly Button stopButton = new Button();
        private readonly CheckBox loopCheckBox = new CheckBox();
        private readonly CheckBox smoothCheckBox = new CheckBox();
        private readonly CheckBox countdownCheckBox = new CheckBox();
        private readonly TrackBar scrubTrackBar = new TrackBar();
        private readonly Label timeLabel = new Label();

        private readonly ListView keyframeListView = new ListView();
        private readonly NumericUpDown durationUpDown = new NumericUpDown();
        private readonly ComboBox easingComboBox = new ComboBox();

        private readonly Timer playbackTimer = new Timer();
        private readonly Timer countdownTimer = new Timer();
        private int countdownLeft;
        // Set while the selected keyframe's values are put into the controls.
        private bool updating;
        private string levelName = "";
        // The level's name in the saved paths' file names (see sly4.CameraPathLevelKey).
        private string levelKey = "";
        private StatusLine statusLine;

        public Sly4CameraForm(sly4 game, Sly4FreeMove freeMove, Sly4CameraApi api)
        {
            this.game = game;
            this.freeMove = freeMove;
            this.api = api;
            Name = "Sly4CameraForm";
            Text = "SluMAN :: Camera";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            GroupBox freeCameraGroupBox = BuildFreeCameraGroup();
            BuildSavedGroup(freeCameraGroupBox.Bottom + 6);
            GroupBox playbackGroupBox = BuildPlaybackGroup(savedGroupBox.Bottom + 6);
            GroupBox keyframesGroupBox = BuildKeyframesGroup(new Point(freeCameraGroupBox.Right + 8, 8));
            GroupBox cameraGroupBox = BuildCameraGroup(new Point(keyframesGroupBox.Left, keyframesGroupBox.Bottom + 6));
            BuildApiCheckBox(playbackGroupBox.Bottom + 8);
            ClientSize = new Size(keyframesGroupBox.Right + 8, Math.Max(playbackGroupBox.Bottom, cameraGroupBox.Bottom) + 8);
            statusLine = new StatusLine(this, false);

            freeMove.ModeChanged += freeMove_ModeChanged;
            freeMove.KeyframeRequested += AddView;
            freeMove.PlaybackFinished += freeMove_PlaybackFinished;
            FormClosing += (sender, e) =>
            {
                freeMove.ModeChanged -= freeMove_ModeChanged;
                freeMove.KeyframeRequested -= AddView;
                freeMove.PlaybackFinished -= freeMove_PlaybackFinished;
                playbackTimer.Stop();
                countdownTimer.Stop();
                // Leave the game with its HUD.
                if (hideHudCheckBox.Checked)
                {
                    try { game.SetHudHidden(false); } catch { }
                }
                if (noFogCheckBox.Checked)
                {
                    try { game.SetFogDisabled(false); } catch { }
                }
            };
            FormClosed += (sender, e) =>
            {
                playbackTimer.Dispose();
                countdownTimer.Dispose();
            };

            fovTimer.Interval = 100;
            fovTimer.Tick += (sender, e) =>
            {
                ShowFov();
                if (freeMove.Current == Sly4FreeMove.Mode.FreeCamera)
                {
                    ShowFar();
                }
                // A level load sets the fog again.
                if (noFogCheckBox.Checked && ++noFogTick % NoFogRefreshTicks == 0)
                {
                    try { game.SetFogDisabled(true); } catch { }
                }
            };
            fovTimer.Start();
            FormClosing += (sender, e) => fovTimer.Stop();
            FormClosed += (sender, e) => fovTimer.Dispose();

            playbackTimer.Interval = PlaybackRefreshMs;
            playbackTimer.Tick += (sender, e) => ShowPlaybackTime();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += countdownTimer_Tick;

            ShowMode();
            ReadLevel();
            ShowKeyframes();
        }

        // --- Layout ---

        private GroupBox BuildFreeCameraGroup()
        {
            GroupBox group = new GroupBox();
            group.Text = "Free Camera";
            group.Location = new Point(8, 8);
            group.Size = new Size(284, 210);
            Controls.Add(group);

            freeCameraCheckBox.Text = "Free camera";
            freeCameraCheckBox.AutoSize = true;
            freeCameraCheckBox.Location = new Point(10, 20);
            freeCameraCheckBox.Click += freeCameraCheckBox_Click;
            group.Controls.Add(freeCameraCheckBox);

            hideHudCheckBox.Text = "Hide HUD";
            hideHudCheckBox.AutoSize = true;
            hideHudCheckBox.Location = new Point(190, 20);
            hideHudCheckBox.Click += (sender, e) => SetHudHidden(hideHudCheckBox.Checked);
            group.Controls.Add(hideHudCheckBox);
            ToolTip hudTip = new ToolTip();
            hudTip.SetToolTip(hideHudCheckBox, "Hides the HUD (sets the UI.3D.FOV tunable to 0). The pause menu is hidden too while it's on.");
            try
            {
                hideHudCheckBox.Checked = game.IsHudHidden();
            }
            catch
            {
                // Left unticked; ticking it writes the value anyway.
            }
            FormClosed += (sender, e) => hudTip.Dispose();

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
            lookSpeedUpDown.Value = (decimal)Math.Max(10, Math.Min(MaxLookDegrees, Math.Round(CameraPath.Degrees(freeMove.LookSpeed))));
            lookSpeedUpDown.Location = new Point(214, 72);
            lookSpeedUpDown.Size = new Size(60, 20);
            lookSpeedUpDown.ValueChanged += (sender, e) => freeMove.LookSpeed = CameraPath.Radians((float)lookSpeedUpDown.Value);
            group.Controls.Add(lookSpeedUpDown);

            Label fovLabel = new Label();
            fovLabel.Text = "Field of view (degrees):";
            fovLabel.AutoSize = true;
            fovLabel.Location = new Point(8, 101);
            group.Controls.Add(fovLabel);

            Button gameFovButton = new Button();
            SetButton(gameFovButton, "Game's", 150, 96, 58, (sender, e) => freeMove.Fov = freeMove.GameFov);
            group.Controls.Add(gameFovButton);

            fovUpDown.DecimalPlaces = 4;
            fovUpDown.Minimum = 0.0001m;
            fovUpDown.Maximum = (decimal)Math.Round(CameraPath.Degrees(Sly4FreeMove.MaxFov));
            fovUpDown.Value = 53.5m;
            fovUpDown.Location = new Point(214, 98);
            fovUpDown.Size = new Size(60, 20);
            fovUpDown.ValueChanged += (sender, e) =>
            {
                if (!updatingFov)
                {
                    freeMove.Fov = CameraPath.Radians((float)fovUpDown.Value);
                }
            };
            group.Controls.Add(fovUpDown);

            ToolTip fovTip = new ToolTip();
            fovTip.SetToolTip(fovUpDown, "The free camera's vertical field of view. R2 zooms in, L2 out.");
            fovTip.SetToolTip(gameFovButton, "Back to the field of view the game had when the free camera started.");
            FormClosed += (sender, e) => fovTip.Dispose();

            Label help = new Label();
            help.Text = "Left stick moves, right stick turns, R1 / L1 move camera-up,\n" +
                "R2 / L2 zoom, R3 adds a keyframe. The player is held where\n" +
                "they stood; in hacks, time stands still. Loads turn it off.\n" +
                "On RPCS3, set the PPU decoder to Interpreter (static).";
            help.Location = new Point(8, 124);
            help.Size = new Size(270, 58);
            group.Controls.Add(help);

            characterControlCheckBox.Text = "Control character (camera stays put)";
            characterControlCheckBox.AutoSize = true;
            characterControlCheckBox.Location = new Point(10, 186);
            characterControlCheckBox.Checked = freeMove.CharacterControl;
            characterControlCheckBox.CheckedChanged += (sender, e) => freeMove.CharacterControl = characterControlCheckBox.Checked;
            group.Controls.Add(characterControlCheckBox);
            ToolTip controlTip = new ToolTip();
            controlTip.SetToolTip(characterControlCheckBox,
                "The free camera stays where it is, or keeps playing its path, and the controller\n" +
                "moves the character as usual (in hacks, the minigame runs again). Untick to fly\n" +
                "the camera again.");
            FormClosed += (sender, e) => controlTip.Dispose();

            return group;
        }

        private void BuildSavedGroup(int top)
        {
            savedGroupBox.Text = "Saved Paths";
            savedGroupBox.Location = new Point(8, top);
            savedGroupBox.Size = new Size(284, 80);
            Controls.Add(savedGroupBox);

            pathNameComboBox.Location = new Point(10, 20);
            pathNameComboBox.Size = new Size(264, 21);
            pathNameComboBox.MaxDropDownItems = 20;
            savedGroupBox.Controls.Add(pathNameComboBox);

            AddButton(savedGroupBox, "Load", 10, 48, 84, (sender, e) => LoadPath());
            AddButton(savedGroupBox, "Save", 100, 48, 84, (sender, e) => SavePath());
            AddButton(savedGroupBox, "Delete", 190, 48, 84, (sender, e) => DeletePath());
        }

        private GroupBox BuildPlaybackGroup(int top)
        {
            GroupBox group = new GroupBox();
            group.Text = "Playback";
            group.Location = new Point(8, top);
            group.Size = new Size(284, 154);
            Controls.Add(group);

            SetButton(playButton, "Play", 10, 20, 84, (sender, e) => PlayClicked());
            group.Controls.Add(playButton);
            SetButton(stopButton, "Stop", 100, 20, 84, (sender, e) => StopPlayback());
            group.Controls.Add(stopButton);

            loopCheckBox.Text = "Loop";
            loopCheckBox.AutoSize = true;
            loopCheckBox.Location = new Point(192, 24);
            group.Controls.Add(loopCheckBox);

            smoothCheckBox.Text = "Smooth path (no stops at keyframes)";
            smoothCheckBox.AutoSize = true;
            smoothCheckBox.Location = new Point(10, 52);
            smoothCheckBox.CheckedChanged += (sender, e) =>
            {
                path.Smooth = smoothCheckBox.Checked;
                // The easing column and box don't apply to a smooth path.
                ShowKeyframes(SelectedIndex());
            };
            group.Controls.Add(smoothCheckBox);

            countdownCheckBox.Text = $"{CountdownSeconds} second countdown before playing";
            countdownCheckBox.AutoSize = true;
            countdownCheckBox.Location = new Point(10, 74);
            group.Controls.Add(countdownCheckBox);

            scrubTrackBar.Minimum = 0;
            scrubTrackBar.Maximum = ScrubSteps;
            scrubTrackBar.TickStyle = TickStyle.None;
            scrubTrackBar.Location = new Point(6, 96);
            scrubTrackBar.Size = new Size(272, 30);
            scrubTrackBar.Scroll += scrubTrackBar_Scroll;
            group.Controls.Add(scrubTrackBar);

            timeLabel.AutoSize = true;
            timeLabel.Location = new Point(10, 128);
            group.Controls.Add(timeLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(scrubTrackBar, "Drag to preview the path at any moment.");
            tip.SetToolTip(smoothCheckBox,
                "Plays the path as one continuous curve: the camera keeps its speed and turning\n" +
                "through every keyframe and eases only at the start and end. Easing is ignored.");
            FormClosed += (sender, e) => tip.Dispose();
            return group;
        }

        private GroupBox BuildKeyframesGroup(Point location)
        {
            GroupBox group = new GroupBox();
            group.Text = "Keyframes";
            group.Location = location;
            group.Size = new Size(352, 352);
            Controls.Add(group);

            keyframeListView.View = View.Details;
            keyframeListView.FullRowSelect = true;
            keyframeListView.GridLines = true;
            keyframeListView.HideSelection = false;
            keyframeListView.MultiSelect = false;
            keyframeListView.Location = new Point(10, 20);
            keyframeListView.Size = new Size(332, 176);
            keyframeListView.Columns.Add("#", 24);
            keyframeListView.Columns.Add("Position", 122);
            keyframeListView.Columns.Add("Yaw", 36);
            keyframeListView.Columns.Add("Pitch", 36);
            keyframeListView.Columns.Add("FOV", 34);
            keyframeListView.Columns.Add("Time", 36);
            keyframeListView.Columns.Add("Easing", 42);
            keyframeListView.SelectedIndexChanged += (sender, e) => ShowSelectedKeyframe();
            keyframeListView.DoubleClick += (sender, e) => GoToSelected();
            keyframeListView.KeyDown += keyframeListView_KeyDown;
            group.Controls.Add(keyframeListView);

            AddButton(group, "Add View", 10, 202, 100, (sender, e) => AddView());
            AddButton(group, "Update", 114, 202, 72, (sender, e) => UpdateSelected());
            AddButton(group, "Delete", 190, 202, 72, (sender, e) => DeleteSelected());
            AddButton(group, "Go To", 266, 202, 76, (sender, e) => GoToSelected());
            AddButton(group, "Move Up", 10, 231, 100, (sender, e) => MoveSelected(-1));
            AddButton(group, "Move Down", 114, 231, 100, (sender, e) => MoveSelected(1));

            Label durationLabel = new Label();
            durationLabel.Text = "Time to next keyframe (s):";
            durationLabel.AutoSize = true;
            durationLabel.Location = new Point(8, 269);
            group.Controls.Add(durationLabel);

            durationUpDown.DecimalPlaces = 2;
            durationUpDown.Increment = 0.5m;
            durationUpDown.Minimum = 0;
            durationUpDown.Maximum = 600;
            durationUpDown.Location = new Point(150, 266);
            durationUpDown.Size = new Size(64, 20);
            durationUpDown.ValueChanged += (sender, e) => EditSelected();
            group.Controls.Add(durationUpDown);

            Label easingLabel = new Label();
            easingLabel.Text = "Easing:";
            easingLabel.AutoSize = true;
            easingLabel.Location = new Point(222, 269);
            group.Controls.Add(easingLabel);

            easingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            easingComboBox.Items.AddRange(new object[] { "Linear", "Ease in/out", "Hold" });
            easingComboBox.Location = new Point(266, 266);
            easingComboBox.Size = new Size(76, 21);
            easingComboBox.SelectedIndexChanged += (sender, e) => EditSelected();
            group.Controls.Add(easingComboBox);

            Label help = new Label();
            help.Text = "Fly the free camera to a view and press Add View (or R3) to add\n" +
                "it after the selected keyframe. Double-click a keyframe to go\n" +
                "to it. Hold stays on a keyframe for its time, then cuts.";
            help.Location = new Point(8, 296);
            help.Size = new Size(336, 46);
            group.Controls.Add(help);

            return group;
        }

        /// <summary>The free camera's exact position, facing and field of view, to read and type in.</summary>
        private GroupBox BuildCameraGroup(Point location)
        {
            GroupBox group = new GroupBox();
            group.Text = "Camera";
            group.Location = location;
            group.Size = new Size(352, 130);
            Controls.Add(group);

            string[] labels = { "X", "Y", "Z", "Yaw", "Pitch", "FOV" };
            for (int i = 0; i < cameraBoxes.Length; i++)
            {
                int x = 8 + (i % 3) * 112;
                int y = 20 + (i / 3) * 27;
                Label label = new Label();
                label.Text = labels[i] + ":";
                label.AutoSize = true;
                label.Location = new Point(x, y + 3);
                group.Controls.Add(label);

                cameraBoxes[i] = new TextBox();
                cameraBoxes[i].Location = new Point(x + 36, y);
                cameraBoxes[i].Size = new Size(68, 20);
                group.Controls.Add(cameraBoxes[i]);
            }

            AddButton(group, "Current", 8, 74, 100, (sender, e) => ReadCameraBoxes());
            Button setButton = new Button();
            SetButton(setButton, "Set Camera", 114, 74, 100, (sender, e) => WriteCameraBoxes());
            group.Controls.Add(setButton);
            foreach (TextBox box in cameraBoxes)
            {
                func.BindEnter(box, setButton);
            }

            Label farLabel = new Label();
            farLabel.Text = "Far distance:";
            farLabel.AutoSize = true;
            farLabel.Location = new Point(8, 106);
            group.Controls.Add(farLabel);

            farUpDown.Minimum = 1;
            farUpDown.Maximum = 100000000;
            farUpDown.Increment = 1000;
            farUpDown.ThousandsSeparator = true;
            farUpDown.Location = new Point(84, 103);
            farUpDown.Size = new Size(100, 20);
            farUpDown.ValueChanged += (sender, e) =>
            {
                if (!updatingFar)
                {
                    freeMove.FarDistance = (float)farUpDown.Value;
                }
            };
            group.Controls.Add(farUpDown);

            Button gameFarButton = new Button();
            SetButton(gameFarButton, "Game's", 190, 101, 64, (sender, e) =>
            {
                freeMove.FarDistance = 0;
                ShowFar();
            });
            group.Controls.Add(gameFarButton);

            noFogCheckBox.Text = "No fog";
            noFogCheckBox.AutoSize = true;
            noFogCheckBox.Location = new Point(264, 105);
            noFogCheckBox.Click += (sender, e) => SetFogDisabled(noFogCheckBox.Checked);
            group.Controls.Add(noFogCheckBox);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(setButton, "Puts the free camera here (turning it on). Yaw, pitch and FOV in degrees.");
            tip.SetToolTip(farUpDown, "How far the free camera draws. Distance fog may follow it.");
            tip.SetToolTip(gameFarButton, "Back to the game's own far distance.");
            tip.SetToolTip(noFogCheckBox, "Moves the level's fog out of sight, and keeps it there after loads. Untick to put it back.");
            FormClosed += (sender, e) => tip.Dispose();
            return group;
        }

        private void ReadCameraBoxes()
        {
            float[] position;
            float yaw;
            float pitch;
            float fov;
            if (!freeMove.GetCamera(out position, out yaw, out pitch, out fov))
            {
                statusLine.Error("Turn on the free camera first.");
                return;
            }
            float[] values =
            {
                position[0], position[1], position[2],
                CameraPath.Degrees(yaw), CameraPath.Degrees(pitch), CameraPath.Degrees(fov),
            };
            for (int i = 0; i < values.Length; i++)
            {
                cameraBoxes[i].Text = values[i].ToString(i == 5 ? "0.#####" : "0.###", CultureInfo.InvariantCulture);
            }
        }

        private void WriteCameraBoxes()
        {
            float[] values = new float[cameraBoxes.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!float.TryParse(cameraBoxes[i].Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    statusLine.Error("X, Y, Z, yaw, pitch and FOV must all be numbers.");
                    return;
                }
            }
            if (values[5] <= 0)
            {
                statusLine.Error("The field of view must be above 0.");
                return;
            }
            if (!EnsureFreeCamera())
            {
                return;
            }
            freeMove.SetCamera(new float[] { values[0], values[1], values[2] },
                CameraPath.Radians(values[3]), CameraPath.Radians(values[4]), CameraPath.Radians(values[5]));
        }

        private static void AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
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

        private void freeMove_ModeChanged()
        {
            ShowMode();
        }

        private void ShowMode()
        {
            if (IsDisposed)
            {
                return;
            }
            freeCameraCheckBox.Checked = freeMove.Current == Sly4FreeMove.Mode.FreeCamera;
            ShowPlaying();
        }

        private void SetHudHidden(bool hidden)
        {
            try
            {
                game.SetHudHidden(hidden);
                statusLine.Info(hidden ? "Hid the HUD." : "Showed the HUD.");
            }
            catch (Exception ex)
            {
                hideHudCheckBox.Checked = !hidden;
                statusLine.Error($"Couldn't {(hidden ? "hide" : "show")} the HUD: {ex.Message}");
            }
        }

        private void BuildApiCheckBox(int top)
        {
            apiCheckBox.Text = $"Script API on 127.0.0.1:{api.Port}";
            apiCheckBox.AutoSize = true;
            apiCheckBox.Location = new Point(14, top);
            apiCheckBox.Checked = api.IsRunning;
            apiCheckBox.Click += (sender, e) =>
            {
                api.Enabled = apiCheckBox.Checked;
                apiCheckBox.Checked = api.IsRunning;
                apiCheckBox.Text = $"Script API on 127.0.0.1:{api.Port}";
                if (api.LastError != "")
                {
                    statusLine.Error(api.LastError);
                }
                else if (api.IsRunning)
                {
                    statusLine.Info($"Scripts can drive the free camera at {api.Url} (ask / for the list of requests).");
                }
            };
            Controls.Add(apiCheckBox);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(apiCheckBox,
                "Lets scripts on this PC read and move the free camera over HTTP, e.g.\n" +
                "http://127.0.0.1:" + api.Port + "/camera/set?x=0&y=500&z=0&pitch=-90&fov=5\n" +
                "Ask http://127.0.0.1:" + api.Port + "/ for the list of requests.");
            FormClosed += (sender, e) => tip.Dispose();
        }

        private void SetFogDisabled(bool disabled)
        {
            try
            {
                if (!game.SetFogDisabled(disabled))
                {
                    statusLine.Error("The game has no camera yet; No fog applies once a level is loaded.");
                    return;
                }
                statusLine.Info(disabled ? "Fog moved out of sight." : "Fog put back.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the fog: {ex.Message}");
            }
        }

        /// <summary>Shows the far distance in use: the one set, or the game's.</summary>
        private void ShowFar()
        {
            if (farUpDown.Focused)
            {
                return;
            }
            float far = freeMove.FarDistance > 0 ? freeMove.FarDistance : freeMove.GameFar;
            if (far <= 0)
            {
                return;
            }
            decimal value = Math.Max(farUpDown.Minimum, Math.Min(farUpDown.Maximum, (decimal)Math.Round(far)));
            if (farUpDown.Value != value)
            {
                updatingFar = true;
                farUpDown.Value = value;
                updatingFar = false;
            }
        }

        private void ShowFov()
        {
            if (freeMove.Current != Sly4FreeMove.Mode.FreeCamera || fovUpDown.Focused)
            {
                return;
            }
            decimal degrees = (decimal)Math.Round(CameraPath.Degrees(freeMove.Fov), 4);
            degrees = Math.Max(fovUpDown.Minimum, Math.Min(fovUpDown.Maximum, degrees));
            if (fovUpDown.Value != degrees)
            {
                updatingFov = true;
                fovUpDown.Value = degrees;
                updatingFov = false;
            }
        }

        /// <summary>Turns the free camera on if it isn't. False (with the reason shown) if it can't be.</summary>
        private bool EnsureFreeCamera()
        {
            if (freeMove.Current == Sly4FreeMove.Mode.FreeCamera)
            {
                return true;
            }
            try
            {
                freeMove.Start(Sly4FreeMove.Mode.FreeCamera);
                return true;
            }
            catch (Exception ex)
            {
                freeMove.Stop();
                statusLine.Error($"Couldn't start the free camera: {ex.Message}");
                return false;
            }
        }

        // --- Keyframes ---

        private void AddView()
        {
            if (IsDisposed)
            {
                return;
            }
            CameraPath.Keyframe key = new CameraPath.Keyframe();
            if (!ReadView(key))
            {
                return;
            }

            int selected = SelectedIndex();
            int index = selected < 0 ? path.Keyframes.Count : selected + 1;
            // A new keyframe takes the timing of the one before it.
            if (index > 0)
            {
                CameraPath.Keyframe previous = path.Keyframes[index - 1];
                key.duration = previous.duration;
                key.easing = previous.easing;
            }
            path.Keyframes.Insert(index, key);
            ShowKeyframes(index);
            statusLine.Info($"Added keyframe {index + 1}.");
        }

        /// <summary>Copies the free camera's view into a keyframe. False if the camera is off.</summary>
        private bool ReadView(CameraPath.Keyframe key)
        {
            float[] position;
            float yaw;
            float pitch;
            float fov;
            if (!freeMove.GetCamera(out position, out yaw, out pitch, out fov))
            {
                statusLine.Error("Turn on the free camera and fly to the view first.");
                return false;
            }
            key.x = position[0];
            key.y = position[1];
            key.z = position[2];
            key.yaw = yaw;
            key.pitch = pitch;
            key.fov = fov;
            return true;
        }

        private void UpdateSelected()
        {
            int index = SelectedIndex();
            if (index < 0)
            {
                statusLine.Error("Select a keyframe first.");
                return;
            }
            if (ReadView(path.Keyframes[index]))
            {
                ShowKeyframes(index);
                statusLine.Info($"Keyframe {index + 1} now has the current view.");
            }
        }

        private void DeleteSelected()
        {
            int index = SelectedIndex();
            if (index < 0)
            {
                return;
            }
            path.Keyframes.RemoveAt(index);
            ShowKeyframes(Math.Min(index, path.Keyframes.Count - 1));
        }

        private void MoveSelected(int offset)
        {
            int index = SelectedIndex();
            int target = index + offset;
            if (index < 0 || target < 0 || target >= path.Keyframes.Count)
            {
                return;
            }
            CameraPath.Keyframe key = path.Keyframes[index];
            path.Keyframes.RemoveAt(index);
            path.Keyframes.Insert(target, key);
            ShowKeyframes(target);
        }

        private void GoToSelected()
        {
            int index = SelectedIndex();
            if (index < 0 || !EnsureFreeCamera())
            {
                return;
            }
            CameraPath.Keyframe key = path.Keyframes[index];
            freeMove.SetCamera(new float[] { key.x, key.y, key.z }, key.yaw, key.pitch, key.fov);
            ShowScrubPosition(path.TimeOf(index));
        }

        private void keyframeListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelected();
                e.Handled = true;
            }
        }

        private int SelectedIndex()
        {
            return keyframeListView.SelectedIndices.Count == 0 ? -1 : keyframeListView.SelectedIndices[0];
        }

        /// <summary>Fills the list from the path and selects <paramref name="select"/>.</summary>
        private void ShowKeyframes(int select = -1)
        {
            keyframeListView.BeginUpdate();
            keyframeListView.Items.Clear();
            for (int i = 0; i < path.Keyframes.Count; i++)
            {
                keyframeListView.Items.Add(KeyframeItem(i));
            }
            keyframeListView.EndUpdate();

            if (select >= 0 && select < keyframeListView.Items.Count)
            {
                keyframeListView.Items[select].Selected = true;
                keyframeListView.Items[select].EnsureVisible();
            }
            ShowSelectedKeyframe();
            ShowScrubPosition(0);
        }

        private ListViewItem KeyframeItem(int index)
        {
            CameraPath.Keyframe key = path.Keyframes[index];
            bool last = index == path.Keyframes.Count - 1;
            ListViewItem item = new ListViewItem((index + 1).ToString());
            item.SubItems.Add($"{Format(key.x)}, {Format(key.y)}, {Format(key.z)}");
            item.SubItems.Add(Format(CameraPath.Degrees(key.yaw)));
            item.SubItems.Add(Format(CameraPath.Degrees(key.pitch)));
            item.SubItems.Add(key.fov > 0 ? FormatFov(CameraPath.Degrees(key.fov)) : "game");
            // The last keyframe ends the path, so its timing isn't used.
            item.SubItems.Add(last ? "-" : key.duration.ToString("0.##", CultureInfo.InvariantCulture));
            item.SubItems.Add(last || path.Smooth ? "-" : EasingText(key.easing));
            return item;
        }

        /// <summary>A field of view in degrees, with the digits a small one needs.</summary>
        private static string FormatFov(float degrees)
        {
            return degrees.ToString(degrees < 1 ? "0.####" : "0.0", CultureInfo.InvariantCulture);
        }

        private static string Format(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string EasingText(CameraPath.Easing easing)
        {
            return easing == CameraPath.Easing.EaseInOut ? "Ease" : easing.ToString();
        }

        private void ShowSelectedKeyframe()
        {
            int index = SelectedIndex();
            updating = true;
            if (index >= 0)
            {
                CameraPath.Keyframe key = path.Keyframes[index];
                durationUpDown.Value = (decimal)Math.Max(0, Math.Min(600, key.duration));
                easingComboBox.SelectedIndex = (int)key.easing;
            }
            durationUpDown.Enabled = index >= 0;
            // A smooth path ignores easing.
            easingComboBox.Enabled = index >= 0 && !path.Smooth;
            updating = false;
        }

        private void EditSelected()
        {
            int index = SelectedIndex();
            if (updating || index < 0)
            {
                return;
            }
            CameraPath.Keyframe key = path.Keyframes[index];
            key.duration = (float)durationUpDown.Value;
            if (easingComboBox.SelectedIndex >= 0)
            {
                key.easing = (CameraPath.Easing)easingComboBox.SelectedIndex;
            }
            ListViewItem item = KeyframeItem(index);
            for (int i = 1; i < item.SubItems.Count; i++)
            {
                keyframeListView.Items[index].SubItems[i].Text = item.SubItems[i].Text;
            }
            ShowScrubPosition(0);
        }

        // --- Playback ---

        private void PlayClicked()
        {
            if (path.Keyframes.Count < 2)
            {
                statusLine.Error("Add at least two keyframes to play a path.");
                return;
            }
            StopPlayback();
            if (countdownCheckBox.Checked)
            {
                // Show the start of the path while counting down.
                if (!EnsureFreeCamera())
                {
                    return;
                }
                CameraPath.Keyframe first = path.Keyframes[0];
                freeMove.SetCamera(new float[] { first.x, first.y, first.z }, first.yaw, first.pitch, first.fov);
                countdownLeft = CountdownSeconds;
                statusLine.Info($"Playing in {countdownLeft}...");
                countdownTimer.Start();
                return;
            }
            StartPlaying();
        }

        private void countdownTimer_Tick(object sender, EventArgs e)
        {
            countdownLeft--;
            if (countdownLeft > 0)
            {
                statusLine.Info($"Playing in {countdownLeft}...");
                return;
            }
            countdownTimer.Stop();
            StartPlaying();
        }

        private void StartPlaying()
        {
            try
            {
                freeMove.Play(path, loopCheckBox.Checked);
                statusLine.Info($"Playing {path.TotalDuration.ToString("0.##", CultureInfo.InvariantCulture)} seconds.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't play the path: {ex.Message}");
            }
            ShowPlaying();
        }

        private void StopPlayback()
        {
            countdownTimer.Stop();
            freeMove.StopPlayback();
            ShowPlaying();
        }

        private void freeMove_PlaybackFinished()
        {
            ShowPlaying();
            ShowScrubPosition(path.TotalDuration);
        }

        private void ShowPlaying()
        {
            bool playing = freeMove.IsPlaying;
            playButton.Enabled = !playing;
            stopButton.Enabled = playing || countdownTimer.Enabled;
            if (playing)
            {
                playbackTimer.Start();
            }
            else
            {
                playbackTimer.Stop();
            }
        }

        private void ShowPlaybackTime()
        {
            if (!freeMove.IsPlaying)
            {
                ShowPlaying();
                return;
            }
            ShowScrubPosition(freeMove.PlaybackTime);
        }

        private void ShowScrubPosition(float time)
        {
            float total = path.TotalDuration;
            int step = total <= 0 ? 0 : (int)Math.Round(Math.Max(0, Math.Min(1, time / total)) * ScrubSteps);
            scrubTrackBar.Value = step;
            timeLabel.Text = $"{time.ToString("0.0", CultureInfo.InvariantCulture)} / {total.ToString("0.0", CultureInfo.InvariantCulture)} s";
        }

        private void scrubTrackBar_Scroll(object sender, EventArgs e)
        {
            if (path.Keyframes.Count == 0)
            {
                return;
            }
            countdownTimer.Stop();
            freeMove.StopPlayback();
            if (!EnsureFreeCamera())
            {
                return;
            }

            float time = path.TotalDuration * scrubTrackBar.Value / ScrubSteps;
            float[] position = new float[3];
            float yaw;
            float pitch;
            float fov;
            path.Evaluate(time, freeMove.GameFov, position, out yaw, out pitch, out fov);
            freeMove.SetCamera(position, yaw, pitch, fov);
            timeLabel.Text = $"{time.ToString("0.0", CultureInfo.InvariantCulture)} / {path.TotalDuration.ToString("0.0", CultureInfo.InvariantCulture)} s";
            ShowPlaying();
        }

        // --- Saved paths ---

        /// <summary>Paths are saved per level; reads which level the game is in.</summary>
        private void ReadLevel()
        {
            string level = "";
            string key = "";
            try
            {
                uint levelHash = game.GetLevelHash();
                level = levelHash == 0 ? "" : sly4.GetLevelName(levelHash);
                key = levelHash == 0 ? "" : sly4.CameraPathLevelKey(levelHash);
            }
            catch
            {
                // Shown as no level; saving asks for one.
            }
            if (level == levelName && key == levelKey)
            {
                return;
            }

            levelName = level;
            levelKey = key;
            savedGroupBox.Text = level == "" ? "Saved Paths" : $"Saved Paths ({level})";
            string typed = pathNameComboBox.Text;
            pathNameComboBox.Items.Clear();
            if (key != "")
            {
                foreach (string name in CameraPath.SavedNames(sly4.CameraPathFolder, key))
                {
                    pathNameComboBox.Items.Add(name);
                }
            }
            pathNameComboBox.Text = typed;
        }

        private string TypedPathName()
        {
            string name = pathNameComboBox.Text.Trim();
            if (levelName == "")
            {
                statusLine.Error("Paths are saved per level; load into a level first.");
                return null;
            }
            if (!CameraPath.IsValidName(name))
            {
                statusLine.Error("Type a name for the path, without \\ / : * ? \" < > | characters.");
                return null;
            }
            return name;
        }

        private void SavePath()
        {
            ReadLevel();
            string name = TypedPathName();
            if (name == null)
            {
                return;
            }
            if (path.Keyframes.Count == 0)
            {
                statusLine.Error("Add a keyframe first.");
                return;
            }
            try
            {
                path.Save(sly4.CameraPathFolder, levelKey, name);
                if (!pathNameComboBox.Items.Contains(name))
                {
                    pathNameComboBox.Items.Add(name);
                }
                statusLine.Info($"Saved \"{name}\" for {levelName}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save the path: {ex.Message}");
            }
        }

        private void LoadPath()
        {
            ReadLevel();
            string name = TypedPathName();
            if (name == null)
            {
                return;
            }
            try
            {
                CameraPath loaded = CameraPath.Load(sly4.CameraPathFolder, levelKey, name);
                StopPlayback();
                path = loaded;
                smoothCheckBox.Checked = path.Smooth;
                ShowKeyframes(path.Keyframes.Count > 0 ? 0 : -1);
                statusLine.Info($"Loaded \"{name}\": {path.Keyframes.Count} keyframes.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the path: {ex.Message}");
            }
        }

        private void DeletePath()
        {
            ReadLevel();
            string name = TypedPathName();
            if (name == null)
            {
                return;
            }
            if (MessageBox.Show($"Delete the saved path \"{name}\" for {levelName}?", "Delete path",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                CameraPath.Delete(sly4.CameraPathFolder, levelKey, name);
                pathNameComboBox.Items.Remove(name);
                pathNameComboBox.Text = "";
                statusLine.Info($"Deleted \"{name}\". The keyframes stay in the list until you change them.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't delete the path: {ex.Message}");
            }
        }

        public void RefreshFromGame()
        {
            ShowMode();
            ReadLevel();
        }
    }
}
