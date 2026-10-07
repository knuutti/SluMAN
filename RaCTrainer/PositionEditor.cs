using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace racman
{
    /// <summary>
    /// Live position, character info, per-axis freezes and warp locations for any game that
    /// describes its memory with a <see cref="PositionEditorLayout"/>.
    /// </summary>
    public partial class PositionEditor : Form
    {
        private IGame game;
        private PositionEditorLayout layout;
        private IPositionEditorHost host;
        private Func<string, string> getMapDisplayName;
        private System.Windows.Forms.Timer pollTimer;

        private float? frozenPosX;
        private float? frozenPosY;
        private float? frozenPosZ;
        private float flyFrozenZ;
        private float prevFlyPosX;
        private float prevFlyPosY;
        private bool prevFlyMode;

        private const float FlyHeightStep = 20.0f;
        private const float FlyBoostMultiplier = 8.0f;

        private struct WarpLocation
        {
            public string Name;
            public string MapIndicator;
            public float X, Y, Z;
            public bool IsUserDefined;
        }

        private List<WarpLocation> builtinWarps = new List<WarpLocation>();
        private List<WarpLocation> userWarps = new List<WarpLocation>();
        private List<WarpLocation> displayedWarps = new List<WarpLocation>();
        private string currentMapIndicator = "";

        private readonly string userWarpFile;
        private readonly string builtinWarpFile;

        private StatusLine statusLine;

        /// <param name="getMapDisplayName">Turns a map name such as "Y$KFv_ext" into a readable
        /// one, or returns null when it doesn't know it.</param>
        /// <param name="host">The Practice window, for Fly Mode and Infinite Jump. Optional.</param>
        public PositionEditor(IGame game, PositionEditorLayout layout, Func<string, string> getMapDisplayName, IPositionEditorHost host = null)
        {
            this.game = game;
            this.layout = layout;
            this.getMapDisplayName = getMapDisplayName;
            this.host = host;
            userWarpFile = UserData.Path($"{layout.warpFilePrefix}_user_warps.txt");
            builtinWarpFile = $"data/{layout.warpFilePrefix}_warp_locations.txt";

            InitializeComponent();
            Text = $"SluMAN :: Position Editor ({layout.gameName})";
            statusLine = new StatusLine(this, false);
            func.BindEnter(xPosTextBox, setXPosButton);
            func.BindEnter(yPosTextBox, setYPosButton);
            func.BindEnter(zPosTextBox, setZPosButton);
            func.BindEnter(warpNameTextBox, saveWarpButton);

            LoadBuiltinWarps();
            LoadUserWarps();

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 10;
            pollTimer.Tick += PollTimer_Tick;
            pollTimer.Start();
        }

        private bool FlyModeEnabled => host != null && host.FlyModeEnabled;
        private bool InfiniteJumpEnabled => host != null && host.InfiniteJumpEnabled;

        // Transform addresses for one axis (0 = X, 1 = Y, 2 = Z).
        private uint PositionAddress(uint transformPtr, uint axis) => transformPtr + layout.positionOffset + axis * 4;
        private uint VelocityAddress(uint transformPtr, uint axis) => transformPtr + layout.velocityOffset + axis * 4;

        private bool TryResolvePointers(out uint entityPtr, out uint transformPtr)
        {
            entityPtr = 0;
            transformPtr = 0;
            try
            {
                byte[] epBytes = game.api.ReadMemory(game.pid, layout.activeCharacterPtr, 4);
                entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                if (entityPtr == 0) return false;

                byte[] tpBytes = game.api.ReadMemory(game.pid, entityPtr + layout.transformOffset, 4);
                transformPtr = BitConverter.ToUInt32(tpBytes.Reverse().ToArray(), 0);
                return transformPtr != 0;
            }
            catch
            {
                return false;
            }
        }

        private float ReadFloat(uint address)
        {
            byte[] b = game.api.ReadMemory(game.pid, address, 4);
            return BitConverter.ToSingle(b.Reverse().ToArray(), 0);
        }

        private int ReadInt(uint address)
        {
            byte[] b = game.api.ReadMemory(game.pid, address, 4);
            return BitConverter.ToInt32(b.Reverse().ToArray(), 0);
        }

        private void WriteFloat(uint address, float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            game.api.WriteMemory(game.pid, address, b);
        }

        /// <summary>
        /// Sets one axis of the position and stops movement along it, where the game's velocity
        /// offset is known.
        /// </summary>
        private void WritePosition(uint transformPtr, uint axis, float value)
        {
            WriteFloat(PositionAddress(transformPtr, axis), value);
            if (layout.velocityOffset != 0)
            {
                WriteFloat(VelocityAddress(transformPtr, axis), 0f);
            }
        }

        /// <summary>
        /// Makes fly mode take the current height on its next tick, after a load moved the player.
        /// </summary>
        public void ResetFlyHeight()
        {
            prevFlyMode = false;
        }

        /// <summary>
        /// Turns off the X, Y and Z freezes. Called when a load starts, since the frozen
        /// coordinates belong to the map being left.
        /// </summary>
        public void ClearPositionFreezes()
        {
            bool anyCleared = freezePosXCheckBox.Checked || freezePosYCheckBox.Checked || freezePosZCheckBox.Checked;

            // Unchecking runs each box's handler, which clears its frozen value.
            freezePosXCheckBox.Checked = false;
            freezePosYCheckBox.Checked = false;
            freezePosZCheckBox.Checked = false;

            if (anyCleared)
            {
                Console.WriteLine("Load started, position freezes cleared.");
            }
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            string mapIndicator = ReadCurrentMapIndicator();
            if (mapIndicator != currentMapIndicator && mapIndicator.Length > 0)
            {
                currentMapIndicator = mapIndicator;
                string mapName = getMapDisplayName != null ? getMapDisplayName(mapIndicator) : null;
                currentMapLabel.Text = "Current map: " + (mapName ?? mapIndicator);
                RefreshWarpDropdown(mapIndicator);
            }

            if (!TryResolvePointers(out uint entityPtr, out uint transformPtr))
            {
                SetLabelsUnavailable();
                return;
            }

            try
            {
                bool flyModeEnabled = FlyModeEnabled;

                // Position freezes; each also stops movement on its axis.
                if (frozenPosX.HasValue)
                {
                    WritePosition(transformPtr, 0, frozenPosX.Value);
                }
                if (frozenPosY.HasValue)
                {
                    WritePosition(transformPtr, 1, frozenPosY.Value);
                }
                if (!flyModeEnabled && frozenPosZ.HasValue)
                {
                    WritePosition(transformPtr, 2, frozenPosZ.Value);
                }

                // Fly mode: hold Z, adjust height with L2/R2, amplify horizontal movement with R1.
                if (flyModeEnabled && !prevFlyMode)
                {
                    flyFrozenZ = ReadFloat(PositionAddress(transformPtr, 2));
                    prevFlyPosX = ReadFloat(PositionAddress(transformPtr, 0));
                    prevFlyPosY = ReadFloat(PositionAddress(transformPtr, 1));
                    zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);
                }
                prevFlyMode = flyModeEnabled;

                if (flyModeEnabled)
                {
                    if ((Inputs.RawInputs & 0x1) != 0) flyFrozenZ += FlyHeightStep;   // L2 = up
                    if ((Inputs.RawInputs & 0x2) != 0) flyFrozenZ -= FlyHeightStep;   // R2 = down

                    WritePosition(transformPtr, 2, flyFrozenZ);
                    // Don't overwrite a value the user is typing.
                    if (!zPosTextBox.Focused)
                    {
                        zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);
                    }

                    float curX = ReadFloat(PositionAddress(transformPtr, 0));
                    float curY = ReadFloat(PositionAddress(transformPtr, 1));
                    if ((Inputs.RawInputs & 0x8) != 0)  // R1 = horizontal boost
                    {
                        float deltaX = curX - prevFlyPosX;
                        float deltaY = curY - prevFlyPosY;
                        if (Math.Abs(deltaX) > 0.001f || Math.Abs(deltaY) > 0.001f)
                        {
                            float newX = prevFlyPosX + deltaX * FlyBoostMultiplier;
                            float newY = prevFlyPosY + deltaY * FlyBoostMultiplier;
                            WriteFloat(PositionAddress(transformPtr, 0), newX);
                            WriteFloat(PositionAddress(transformPtr, 1), newY);
                            curX = newX;
                            curY = newY;
                        }
                    }
                    prevFlyPosX = curX;
                    prevFlyPosY = curY;
                }

                // Infinite jump: write the full 4 bytes; the LSB is what the game checks.
                if (InfiniteJumpEnabled && layout.infiniteJumpOffset != 0)
                {
                    game.api.WriteMemory(game.pid, entityPtr + layout.infiniteJumpOffset, (uint)0);
                }

                // Current state for the labels.
                entityIdValueLabel.Text = layout.entityIdOffset != 0 ? ReadInt(entityPtr + layout.entityIdOffset).ToString() : "N/A";
                healthValueLabel.Text = layout.healthOffset != 0 ? ReadInt(entityPtr + layout.healthOffset).ToString() : "N/A";
                gadgetPowerValueLabel.Text = layout.gadgetPowerOffset != 0 ? ReadInt(entityPtr + layout.gadgetPowerOffset).ToString() : "N/A";
                opacityValueLabel.Text = layout.opacityOffset != 0 ? FormatFloat(ReadFloat(entityPtr + layout.opacityOffset)) : "N/A";
                rotationValueLabel.Text = layout.rotationOffset != 0 ? FormatFloat(ReadFloat(entityPtr + layout.rotationOffset)) : "N/A";

                xPosLiveLabel.Text = FormatFloat(ReadFloat(PositionAddress(transformPtr, 0)));
                yPosLiveLabel.Text = FormatFloat(ReadFloat(PositionAddress(transformPtr, 1)));
                zPosLiveLabel.Text = FormatFloat(ReadFloat(PositionAddress(transformPtr, 2)));

                if (layout.velocityOffset != 0)
                {
                    float velX = ReadFloat(VelocityAddress(transformPtr, 0));
                    float velY = ReadFloat(VelocityAddress(transformPtr, 1));
                    float velZ = ReadFloat(VelocityAddress(transformPtr, 2));
                    hSpeedLabel.Text = FormatFloat((float)Math.Sqrt(velX * velX + velY * velY));
                    zVelLiveLabel.Text = FormatFloat(velZ);
                }
                else
                {
                    hSpeedLabel.Text = "N/A";
                    zVelLiveLabel.Text = "N/A";
                }
            }
            catch
            {
                SetLabelsUnavailable();
            }
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }

        private void SetLabelsUnavailable()
        {
            entityIdValueLabel.Text = "N/A";
            healthValueLabel.Text = "N/A";
            gadgetPowerValueLabel.Text = "N/A";
            opacityValueLabel.Text = "N/A";
            rotationValueLabel.Text = "N/A";
            xPosLiveLabel.Text = "N/A";
            yPosLiveLabel.Text = "N/A";
            zPosLiveLabel.Text = "N/A";
            hSpeedLabel.Text = "N/A";
            zVelLiveLabel.Text = "N/A";
        }

        /// <summary>
        /// Reads a typed coordinate. Shows a message and returns false when it isn't a number.
        /// </summary>
        private bool TryParseCoordinate(string text, out float value)
        {
            if (float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                return true;
            }
            statusLine.Error("Enter a number, for example -125.5.");
            return false;
        }

        private void setXPosButton_Click(object sender, EventArgs e)
        {
            if (!TryParseCoordinate(xPosTextBox.Text, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WritePosition(transformPtr, 0, val);
                if (freezePosXCheckBox.Checked) frozenPosX = val;
            }
            catch { }
        }

        private void setYPosButton_Click(object sender, EventArgs e)
        {
            if (!TryParseCoordinate(yPosTextBox.Text, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WritePosition(transformPtr, 1, val);
                if (freezePosYCheckBox.Checked) frozenPosY = val;
            }
            catch { }
        }

        private void setZPosButton_Click(object sender, EventArgs e)
        {
            if (!TryParseCoordinate(zPosTextBox.Text, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WritePosition(transformPtr, 2, val);
                if (freezePosZCheckBox.Checked) frozenPosZ = val;
                if (FlyModeEnabled) flyFrozenZ = val;
            }
            catch { }
        }

        /// <summary>
        /// Starts or stops freezing one axis. Starting takes the current position and shows it in
        /// the axis's box.
        /// </summary>
        private void ToggleAxisFreeze(CheckBox checkBox, TextBox textBox, uint axis, ref float? frozenValue)
        {
            if (!checkBox.Checked)
            {
                frozenValue = null;
                return;
            }
            try
            {
                if (TryResolvePointers(out _, out uint tp))
                {
                    float live = ReadFloat(PositionAddress(tp, axis));
                    frozenValue = live;
                    textBox.Text = FormatFloat(live);
                }
            }
            catch { }
        }

        private void freezePosXCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAxisFreeze(freezePosXCheckBox, xPosTextBox, 0, ref frozenPosX);
        }

        private void freezePosYCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAxisFreeze(freezePosYCheckBox, yPosTextBox, 1, ref frozenPosY);
        }

        private void freezePosZCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAxisFreeze(freezePosZCheckBox, zPosTextBox, 2, ref frozenPosZ);
        }

        private void PositionEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            pollTimer.Stop();
            pollTimer.Dispose();
        }

        // --- Warp Locations ---

        private string ReadCurrentMapIndicator()
        {
            try
            {
                byte[] b = game.api.ReadMemory(game.pid, layout.mapNameAddress, 32);
                int nullIdx = Array.IndexOf(b, (byte)0);
                if (nullIdx < 0) nullIdx = b.Length;
                return Encoding.ASCII.GetString(b, 0, nullIdx);
            }
            catch
            {
                return "";
            }
        }

        private void RefreshWarpDropdown(string mapIndicator)
        {
            displayedWarps.Clear();
            warpLocationComboBox.Items.Clear();

            foreach (WarpLocation w in builtinWarps)
            {
                if (w.MapIndicator == mapIndicator)
                {
                    displayedWarps.Add(w);
                    warpLocationComboBox.Items.Add("[Default] " + w.Name);
                }
            }

            foreach (WarpLocation w in userWarps)
            {
                if (w.MapIndicator == mapIndicator)
                {
                    displayedWarps.Add(w);
                    warpLocationComboBox.Items.Add(w.Name);
                }
            }

            deleteWarpButton.Enabled = false;
            warpNameTextBox.Clear();

            if (warpLocationComboBox.Items.Count > 0)
                warpLocationComboBox.SelectedIndex = 0;
        }

        private void warpLocationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = warpLocationComboBox.SelectedIndex;
            if (idx < 0 || idx >= displayedWarps.Count)
            {
                deleteWarpButton.Enabled = false;
                return;
            }

            WarpLocation loc = displayedWarps[idx];
            if (loc.IsUserDefined)
            {
                warpNameTextBox.Text = loc.Name;
                deleteWarpButton.Enabled = true;
            }
            else
            {
                warpNameTextBox.Clear();
                deleteWarpButton.Enabled = false;
            }
        }

        private void warpButton_Click(object sender, EventArgs e)
        {
            int idx = warpLocationComboBox.SelectedIndex;
            if (idx < 0 || idx >= displayedWarps.Count) return;

            WarpLocation loc = displayedWarps[idx];
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WritePosition(transformPtr, 0, loc.X);
                WritePosition(transformPtr, 1, loc.Y);
                WritePosition(transformPtr, 2, loc.Z);
                if (FlyModeEnabled) flyFrozenZ = loc.Z;
            }
            catch { }
        }

        private void saveWarpButton_Click(object sender, EventArgs e)
        {
            string name = warpNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                statusLine.Error("Enter a name for the warp location.");
                return;
            }
            if (currentMapIndicator == "") return;
            if (!TryResolvePointers(out _, out uint transformPtr)) return;

            try
            {
                float x = ReadFloat(PositionAddress(transformPtr, 0));
                float y = ReadFloat(PositionAddress(transformPtr, 1));
                float z = ReadFloat(PositionAddress(transformPtr, 2));

                userWarps.RemoveAll(w => w.MapIndicator == currentMapIndicator && w.Name == name);
                userWarps.Add(new WarpLocation { Name = name, MapIndicator = currentMapIndicator, X = x, Y = y, Z = z, IsUserDefined = true });
                SaveUserWarps();
                RefreshWarpDropdown(currentMapIndicator);

                for (int i = 0; i < displayedWarps.Count; i++)
                {
                    if (displayedWarps[i].IsUserDefined && displayedWarps[i].Name == name)
                    {
                        warpLocationComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch { }
        }

        private void deleteWarpButton_Click(object sender, EventArgs e)
        {
            int idx = warpLocationComboBox.SelectedIndex;
            if (idx < 0 || idx >= displayedWarps.Count) return;

            WarpLocation loc = displayedWarps[idx];
            if (!loc.IsUserDefined) return;

            if (!ConfirmButton.Confirm(deleteWarpButton, "Confirm", statusLine, $"Click Confirm to delete the warp \"{loc.Name}\"."))
            {
                return;
            }

            userWarps.RemoveAll(w => w.MapIndicator == loc.MapIndicator && w.Name == loc.Name);
            SaveUserWarps();
            RefreshWarpDropdown(currentMapIndicator);
            statusLine.Info($"Deleted the warp \"{loc.Name}\".");
        }

        private static List<WarpLocation> ReadWarpFile(string path, bool isUserDefined)
        {
            List<WarpLocation> warps = new List<WarpLocation>();
            if (!File.Exists(path)) return warps;

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) continue;

                string[] parts = trimmed.Split('|');
                if (parts.Length != 5) continue;

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                warps.Add(new WarpLocation { MapIndicator = parts[0], Name = parts[1], X = x, Y = y, Z = z, IsUserDefined = isUserDefined });
            }
            return warps;
        }

        private void LoadBuiltinWarps()
        {
            builtinWarps = ReadWarpFile(builtinWarpFile, false);
        }

        private void LoadUserWarps()
        {
            userWarps = ReadWarpFile(userWarpFile, true);
        }

        private void SaveUserWarps()
        {
            string[] lines = new string[userWarps.Count];
            for (int i = 0; i < userWarps.Count; i++)
            {
                WarpLocation w = userWarps[i];
                lines[i] = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}", w.MapIndicator, w.Name, w.X, w.Y, w.Z);
            }
            File.WriteAllLines(userWarpFile, lines);
        }

        private void lblGadgetPower_Click(object sender, EventArgs e) { }
        private void lblHSpeed_Click(object sender, EventArgs e) { }
    }
}
