using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace racman
{
    public partial class SLY3PositionEditor : Form
    {
        private sly3 game;
        private SLY3Form mainForm;
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

        private static readonly string UserWarpFile = "sly3_user_warps.txt";
        private static readonly string BuiltinWarpFile = "data/sly3_warp_locations.txt";

        private StatusLine statusLine;

        public SLY3PositionEditor(sly3 game, SLY3Form mainForm = null)
        {
            this.game = game;
            this.mainForm = mainForm;
            InitializeComponent();
            statusLine = new StatusLine(this, false);

            LoadBuiltinWarps();
            LoadUserWarps();

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 10;
            pollTimer.Tick += PollTimer_Tick;
            pollTimer.Start();
        }

        private bool TryResolvePointers(out uint entityPtr, out uint transformPtr)
        {
            entityPtr = 0;
            transformPtr = 0;
            try
            {
                byte[] epBytes = game.api.ReadMemory(game.pid, 0x5EC654, 4);
                entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                if (entityPtr == 0) return false;

                byte[] tpBytes = game.api.ReadMemory(game.pid, entityPtr + 0x44, 4);
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

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            string mapIndicator = ReadCurrentMapIndicator();
            if (mapIndicator != currentMapIndicator && mapIndicator.Length > 0)
            {
                currentMapIndicator = mapIndicator;
                string mapName = GetMapDisplayName(mapIndicator);
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
                bool flyModeEnabled = mainForm != null && mainForm.flyModeCheckBox.Checked;

                // Position freezes — also zero corresponding velocity axis
                if (frozenPosX.HasValue)
                {
                    WriteFloat(transformPtr + 0x130, frozenPosX.Value);
                    WriteFloat(transformPtr + 0x1B0, 0f);
                }
                if (frozenPosY.HasValue)
                {
                    WriteFloat(transformPtr + 0x134, frozenPosY.Value);
                    WriteFloat(transformPtr + 0x1B4, 0f);
                }
                if (!flyModeEnabled && frozenPosZ.HasValue)
                {
                    WriteFloat(transformPtr + 0x138, frozenPosZ.Value);
                    WriteFloat(transformPtr + 0x1B8, 0f);
                }

                // Fly mode: freeze Z pos + Z vel, adjust height with L2/R2, amplify horizontal movement
                if (flyModeEnabled && !prevFlyMode)
                {
                    flyFrozenZ = ReadFloat(transformPtr + 0x138);
                    prevFlyPosX = ReadFloat(transformPtr + 0x130);
                    prevFlyPosY = ReadFloat(transformPtr + 0x134);
                    zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);
                }
                prevFlyMode = flyModeEnabled;

                if (flyModeEnabled)
                {
                    if ((Inputs.RawInputs & 0x1) != 0) flyFrozenZ += FlyHeightStep;   // L2 = up
                    if ((Inputs.RawInputs & 0x2) != 0) flyFrozenZ -= FlyHeightStep;   // R2 = down

                    WriteFloat(transformPtr + 0x138, flyFrozenZ);
                    WriteFloat(transformPtr + 0x1B8, 0f);
                    zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);

                    float curX = ReadFloat(transformPtr + 0x130);
                    float curY = ReadFloat(transformPtr + 0x134);
                    if ((Inputs.RawInputs & 0x8) != 0)  // R1 = horizontal boost
                    {
                        float deltaX = curX - prevFlyPosX;
                        float deltaY = curY - prevFlyPosY;
                        if (Math.Abs(deltaX) > 0.001f || Math.Abs(deltaY) > 0.001f)
                        {
                            float newX = prevFlyPosX + deltaX * FlyBoostMultiplier;
                            float newY = prevFlyPosY + deltaY * FlyBoostMultiplier;
                            WriteFloat(transformPtr + 0x130, newX);
                            WriteFloat(transformPtr + 0x134, newY);
                            curX = newX;
                            curY = newY;
                        }
                    }
                    prevFlyPosX = curX;
                    prevFlyPosY = curY;
                }

                // Infinite jump — write full 4 bytes; LSB is what the game checks
                if (mainForm != null && mainForm.infiniteJumpCheckBox.Checked)
                    game.api.WriteMemory(game.pid, entityPtr + 0x338, (uint)0);

                // Read current state for display labels
                int entityId = ReadInt(entityPtr + 0x18);
                int health = ReadInt(entityPtr + 0x168);
                int gadgetPower = ReadInt(entityPtr + 0x170);
                float opacity = ReadFloat(entityPtr + 0x104);
                float rotation = ReadFloat(entityPtr + 0x1AC);

                float posX = ReadFloat(transformPtr + 0x130);
                float posY = ReadFloat(transformPtr + 0x134);
                float posZ = ReadFloat(transformPtr + 0x138);
                float velX = ReadFloat(transformPtr + 0x1B0);
                float velY = ReadFloat(transformPtr + 0x1B4);
                float velZ = ReadFloat(transformPtr + 0x1B8);
                float hSpeed = (float)Math.Sqrt(velX * velX + velY * velY);

                entityIdValueLabel.Text = entityId.ToString();
                healthValueLabel.Text = health.ToString();
                gadgetPowerValueLabel.Text = gadgetPower.ToString();
                opacityValueLabel.Text = opacity.ToString("F3", CultureInfo.InvariantCulture);
                rotationValueLabel.Text = rotation.ToString("F3", CultureInfo.InvariantCulture);
                xPosLiveLabel.Text = posX.ToString("F3", CultureInfo.InvariantCulture);
                yPosLiveLabel.Text = posY.ToString("F3", CultureInfo.InvariantCulture);
                zPosLiveLabel.Text = posZ.ToString("F3", CultureInfo.InvariantCulture);
                hSpeedLabel.Text = hSpeed.ToString("F3", CultureInfo.InvariantCulture);
                zVelLiveLabel.Text = velZ.ToString("F3", CultureInfo.InvariantCulture);
            }
            catch
            {
                SetLabelsUnavailable();
            }
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

        private void setXPosButton_Click(object sender, EventArgs e)
        {
            if (!float.TryParse(xPosTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WriteFloat(transformPtr + 0x130, val);
                WriteFloat(transformPtr + 0x1B0, 0f);
                if (freezePosXCheckBox.Checked) frozenPosX = val;
            }
            catch { }
        }

        private void setYPosButton_Click(object sender, EventArgs e)
        {
            if (!float.TryParse(yPosTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WriteFloat(transformPtr + 0x134, val);
                WriteFloat(transformPtr + 0x1B4, 0f);
                if (freezePosYCheckBox.Checked) frozenPosY = val;
            }
            catch { }
        }

        private void setZPosButton_Click(object sender, EventArgs e)
        {
            if (!float.TryParse(zPosTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float val)) return;
            try
            {
                if (!TryResolvePointers(out _, out uint transformPtr)) return;
                WriteFloat(transformPtr + 0x138, val);
                WriteFloat(transformPtr + 0x1B8, 0f);
                if (freezePosZCheckBox.Checked) frozenPosZ = val;
                if (mainForm != null && mainForm.flyModeCheckBox.Checked) flyFrozenZ = val;
            }
            catch { }
        }

        private void freezePosXCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (freezePosXCheckBox.Checked)
            {
                try
                {
                    if (TryResolvePointers(out _, out uint tp))
                    {
                        float live = ReadFloat(tp + 0x130);
                        frozenPosX = live;
                        xPosTextBox.Text = live.ToString("F3", CultureInfo.InvariantCulture);
                    }
                }
                catch { }
            }
            else
            {
                frozenPosX = null;
            }
        }

        private void freezePosYCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (freezePosYCheckBox.Checked)
            {
                try
                {
                    if (TryResolvePointers(out _, out uint tp))
                    {
                        float live = ReadFloat(tp + 0x134);
                        frozenPosY = live;
                        yPosTextBox.Text = live.ToString("F3", CultureInfo.InvariantCulture);
                    }
                }
                catch { }
            }
            else
            {
                frozenPosY = null;
            }
        }

        private void freezePosZCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (freezePosZCheckBox.Checked)
            {
                try
                {
                    if (TryResolvePointers(out _, out uint tp))
                    {
                        float live = ReadFloat(tp + 0x138);
                        frozenPosZ = live;
                        zPosTextBox.Text = live.ToString("F3", CultureInfo.InvariantCulture);
                    }
                }
                catch { }
            }
            else
            {
                frozenPosZ = null;
            }
        }

        private void SLY3PositionEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            pollTimer.Stop();
            pollTimer.Dispose();
        }

        // --- Warp Locations ---

        private string ReadCurrentMapIndicator()
        {
            try
            {
                byte[] b = game.api.ReadMemory(game.pid, 0x78D2C8, 32);
                int nullIdx = -1;
                for (int i = 0; i < b.Length; i++)
                {
                    if (b[i] == 0) { nullIdx = i; break; }
                }
                if (nullIdx < 0) nullIdx = b.Length;
                return Encoding.ASCII.GetString(b, 0, nullIdx);
            }
            catch
            {
                return "";
            }
        }

        private string GetMapDisplayName(string indicator)
        {
            foreach (sly3.MapData m in game.maps)
            {
                if (m.indicator == indicator)
                    return m.naturalName;
            }
            return null;
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
                WriteFloat(transformPtr + 0x130, loc.X);
                WriteFloat(transformPtr + 0x134, loc.Y);
                WriteFloat(transformPtr + 0x138, loc.Z);
                WriteFloat(transformPtr + 0x1B0, 0f);
                WriteFloat(transformPtr + 0x1B4, 0f);
                WriteFloat(transformPtr + 0x1B8, 0f);
                if (mainForm != null && mainForm.flyModeCheckBox.Checked) flyFrozenZ = loc.Z;
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
                float x = ReadFloat(transformPtr + 0x130);
                float y = ReadFloat(transformPtr + 0x134);
                float z = ReadFloat(transformPtr + 0x138);

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

            userWarps.RemoveAll(w => w.MapIndicator == loc.MapIndicator && w.Name == loc.Name);
            SaveUserWarps();
            RefreshWarpDropdown(currentMapIndicator);
        }

        private void LoadBuiltinWarps()
        {
            builtinWarps.Clear();
            if (!File.Exists(BuiltinWarpFile)) return;

            foreach (string line in File.ReadAllLines(BuiltinWarpFile))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) continue;

                string[] parts = trimmed.Split('|');
                if (parts.Length != 5) continue;

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                builtinWarps.Add(new WarpLocation { MapIndicator = parts[0], Name = parts[1], X = x, Y = y, Z = z, IsUserDefined = false });
            }
        }

        private void LoadUserWarps()
        {
            userWarps.Clear();
            if (!File.Exists(UserWarpFile)) return;

            foreach (string line in File.ReadAllLines(UserWarpFile))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string[] parts = trimmed.Split('|');
                if (parts.Length != 5) continue;

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                userWarps.Add(new WarpLocation { MapIndicator = parts[0], Name = parts[1], X = x, Y = y, Z = z, IsUserDefined = true });
            }
        }

        private void SaveUserWarps()
        {
            string[] lines = new string[userWarps.Count];
            for (int i = 0; i < userWarps.Count; i++)
            {
                WarpLocation w = userWarps[i];
                lines[i] = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}", w.MapIndicator, w.Name, w.X, w.Y, w.Z);
            }
            File.WriteAllLines(UserWarpFile, lines);
        }

        private void lblGadgetPower_Click(object sender, EventArgs e) { }
        private void lblHSpeed_Click(object sender, EventArgs e) { }
    }
}
