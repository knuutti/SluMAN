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
        private Sly4CameraPath path = new Sly4CameraPath();

        private readonly CheckBox freeCameraCheckBox = new CheckBox();
        private readonly NumericUpDown moveSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown lookSpeedUpDown = new NumericUpDown();
        private readonly NumericUpDown fovUpDown = new NumericUpDown();
        private readonly CheckBox hideHudCheckBox = new CheckBox();
        private readonly CheckBox characterControlCheckBox = new CheckBox();
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
        private StatusLine statusLine;

        public Sly4CameraForm(sly4 game, Sly4FreeMove freeMove)
        {
            this.game = game;
            this.freeMove = freeMove;
            Name = "Sly4CameraForm";
            Text = "SluMAN :: Camera";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            GroupBox freeCameraGroupBox = BuildFreeCameraGroup();
            BuildSavedGroup(freeCameraGroupBox.Bottom + 6);
            GroupBox playbackGroupBox = BuildPlaybackGroup(savedGroupBox.Bottom + 6);
            GroupBox keyframesGroupBox = BuildKeyframesGroup(new Point(freeCameraGroupBox.Right + 8, 8));
            ClientSize = new Size(keyframesGroupBox.Right + 8, Math.Max(playbackGroupBox.Bottom, keyframesGroupBox.Bottom) + 8);
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
            };
            FormClosed += (sender, e) =>
            {
                playbackTimer.Dispose();
                countdownTimer.Dispose();
            };

            fovTimer.Interval = 100;
            fovTimer.Tick += (sender, e) => ShowFov();
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
            lookSpeedUpDown.Value = (decimal)Math.Max(10, Math.Min(MaxLookDegrees, Math.Round(Sly4CameraPath.Degrees(freeMove.LookSpeed))));
            lookSpeedUpDown.Location = new Point(214, 72);
            lookSpeedUpDown.Size = new Size(60, 20);
            lookSpeedUpDown.ValueChanged += (sender, e) => freeMove.LookSpeed = Sly4CameraPath.Radians((float)lookSpeedUpDown.Value);
            group.Controls.Add(lookSpeedUpDown);

            Label fovLabel = new Label();
            fovLabel.Text = "Field of view (degrees):";
            fovLabel.AutoSize = true;
            fovLabel.Location = new Point(8, 101);
            group.Controls.Add(fovLabel);

            Button gameFovButton = new Button();
            SetButton(gameFovButton, "Game's", 150, 96, 58, (sender, e) => freeMove.Fov = freeMove.GameFov);
            group.Controls.Add(gameFovButton);

            fovUpDown.DecimalPlaces = 1;
            fovUpDown.Minimum = (decimal)Math.Round(Sly4CameraPath.Degrees(Sly4FreeMove.MinFov));
            fovUpDown.Maximum = (decimal)Math.Round(Sly4CameraPath.Degrees(Sly4FreeMove.MaxFov));
            fovUpDown.Value = 53.5m;
            fovUpDown.Location = new Point(214, 98);
            fovUpDown.Size = new Size(60, 20);
            fovUpDown.ValueChanged += (sender, e) =>
            {
                if (!updatingFov)
                {
                    freeMove.Fov = Sly4CameraPath.Radians((float)fovUpDown.Value);
                }
            };
            group.Controls.Add(fovUpDown);

            ToolTip fovTip = new ToolTip();
            fovTip.SetToolTip(fovUpDown, "The free camera's vertical field of view. R2 zooms in, L2 out.");
            fovTip.SetToolTip(gameFovButton, "Back to the field of view the game had when the free camera started.");
            FormClosed += (sender, e) => fovTip.Dispose();

            Label help = new Label();
            help.Text = "Left stick moves, right stick turns, R1 / L1 go up and down,\n" +
                "R2 / L2 zoom, R3 adds a keyframe. The player is held where\n" +
                "they stood. The camera turns off when a level loads.\n" +
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
                "moves the character as usual. Untick to fly the camera again.");
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
            group.Size = new Size(352, 476);
            Controls.Add(group);

            keyframeListView.View = View.Details;
            keyframeListView.FullRowSelect = true;
            keyframeListView.GridLines = true;
            keyframeListView.HideSelection = false;
            keyframeListView.MultiSelect = false;
            keyframeListView.Location = new Point(10, 20);
            keyframeListView.Size = new Size(332, 300);
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

            AddButton(group, "Add View", 10, 326, 100, (sender, e) => AddView());
            AddButton(group, "Update", 114, 326, 72, (sender, e) => UpdateSelected());
            AddButton(group, "Delete", 190, 326, 72, (sender, e) => DeleteSelected());
            AddButton(group, "Go To", 266, 326, 76, (sender, e) => GoToSelected());
            AddButton(group, "Move Up", 10, 355, 100, (sender, e) => MoveSelected(-1));
            AddButton(group, "Move Down", 114, 355, 100, (sender, e) => MoveSelected(1));

            Label durationLabel = new Label();
            durationLabel.Text = "Time to next keyframe (s):";
            durationLabel.AutoSize = true;
            durationLabel.Location = new Point(8, 393);
            group.Controls.Add(durationLabel);

            durationUpDown.DecimalPlaces = 2;
            durationUpDown.Increment = 0.5m;
            durationUpDown.Minimum = 0;
            durationUpDown.Maximum = 600;
            durationUpDown.Location = new Point(150, 390);
            durationUpDown.Size = new Size(64, 20);
            durationUpDown.ValueChanged += (sender, e) => EditSelected();
            group.Controls.Add(durationUpDown);

            Label easingLabel = new Label();
            easingLabel.Text = "Easing:";
            easingLabel.AutoSize = true;
            easingLabel.Location = new Point(222, 393);
            group.Controls.Add(easingLabel);

            easingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            easingComboBox.Items.AddRange(new object[] { "Linear", "Ease in/out", "Hold" });
            easingComboBox.Location = new Point(266, 390);
            easingComboBox.Size = new Size(76, 21);
            easingComboBox.SelectedIndexChanged += (sender, e) => EditSelected();
            group.Controls.Add(easingComboBox);

            Label help = new Label();
            help.Text = "Fly the free camera to a view and press Add View (or R3) to add\n" +
                "it after the selected keyframe. Double-click a keyframe to go\n" +
                "to it. Hold stays on a keyframe for its time, then cuts.";
            help.Location = new Point(8, 420);
            help.Size = new Size(336, 46);
            group.Controls.Add(help);

            return group;
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

        private void ShowFov()
        {
            if (freeMove.Current != Sly4FreeMove.Mode.FreeCamera || fovUpDown.Focused)
            {
                return;
            }
            decimal degrees = (decimal)Math.Round(Sly4CameraPath.Degrees(freeMove.Fov), 1);
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
            Sly4CameraPath.Keyframe key = new Sly4CameraPath.Keyframe();
            if (!ReadView(key))
            {
                return;
            }

            int selected = SelectedIndex();
            int index = selected < 0 ? path.Keyframes.Count : selected + 1;
            // A new keyframe takes the timing of the one before it.
            if (index > 0)
            {
                Sly4CameraPath.Keyframe previous = path.Keyframes[index - 1];
                key.duration = previous.duration;
                key.easing = previous.easing;
            }
            path.Keyframes.Insert(index, key);
            ShowKeyframes(index);
            statusLine.Info($"Added keyframe {index + 1}.");
        }

        /// <summary>Copies the free camera's view into a keyframe. False if the camera is off.</summary>
        private bool ReadView(Sly4CameraPath.Keyframe key)
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
            Sly4CameraPath.Keyframe key = path.Keyframes[index];
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
            Sly4CameraPath.Keyframe key = path.Keyframes[index];
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
            Sly4CameraPath.Keyframe key = path.Keyframes[index];
            bool last = index == path.Keyframes.Count - 1;
            ListViewItem item = new ListViewItem((index + 1).ToString());
            item.SubItems.Add($"{Format(key.x)}, {Format(key.y)}, {Format(key.z)}");
            item.SubItems.Add(Format(Sly4CameraPath.Degrees(key.yaw)));
            item.SubItems.Add(Format(Sly4CameraPath.Degrees(key.pitch)));
            item.SubItems.Add(key.fov > 0 ? Format(Sly4CameraPath.Degrees(key.fov)) : "game");
            // The last keyframe ends the path, so its timing isn't used.
            item.SubItems.Add(last ? "-" : key.duration.ToString("0.##", CultureInfo.InvariantCulture));
            item.SubItems.Add(last || path.Smooth ? "-" : EasingText(key.easing));
            return item;
        }

        private static string Format(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string EasingText(Sly4CameraPath.Easing easing)
        {
            return easing == Sly4CameraPath.Easing.EaseInOut ? "Ease" : easing.ToString();
        }

        private void ShowSelectedKeyframe()
        {
            int index = SelectedIndex();
            updating = true;
            if (index >= 0)
            {
                Sly4CameraPath.Keyframe key = path.Keyframes[index];
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
            Sly4CameraPath.Keyframe key = path.Keyframes[index];
            key.duration = (float)durationUpDown.Value;
            if (easingComboBox.SelectedIndex >= 0)
            {
                key.easing = (Sly4CameraPath.Easing)easingComboBox.SelectedIndex;
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
                Sly4CameraPath.Keyframe first = path.Keyframes[0];
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
            try
            {
                uint levelHash = game.GetLevelHash();
                level = levelHash == 0 ? "" : sly4.GetLevelName(levelHash);
            }
            catch
            {
                // Shown as no level; saving asks for one.
            }
            if (level == levelName)
            {
                return;
            }

            levelName = level;
            savedGroupBox.Text = level == "" ? "Saved Paths" : $"Saved Paths ({level})";
            string typed = pathNameComboBox.Text;
            pathNameComboBox.Items.Clear();
            if (level != "")
            {
                foreach (string name in Sly4CameraPath.SavedNames(level))
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
            if (!Sly4CameraPath.IsValidName(name))
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
                path.Save(levelName, name);
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
                Sly4CameraPath loaded = Sly4CameraPath.Load(levelName, name);
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
                Sly4CameraPath.Delete(levelName, name);
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
