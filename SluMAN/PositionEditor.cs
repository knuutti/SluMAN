using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SluMAN
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
        private uint flyEntityPtr;

        private const float FlyHeightStep = 20.0f;
        private const float FlyBoostMultiplier = 8.0f;

        private struct WarpLocation
        {
            public string Name;
            public string MapIndicator;
            public float X, Y, Z;
            // Which way the player faced (IGame.ReadFacing), null if not saved.
            public byte[] Facing;
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
            if (layout.hideEntityFields)
            {
                // Hide the entity fields' column and move the position and velocity readouts into
                // its place.
                int shift = lblPosX.Left - lblEntityId.Left;
                foreach (Control control in charInfoGroupBox.Controls)
                {
                    if (control.Left < lblPosX.Left)
                    {
                        control.Visible = false;
                    }
                    else
                    {
                        control.Left -= shift;
                    }
                }
            }
            AddTopSpeedRow();
            if (layout.getFlySpeed != null)
            {
                AddFlySpeedRow();
            }
            Text = $"SluMAN :: Position Editor ({layout.gameName})";
            statusLine = new StatusLine(this, false);
            func.BindEnter(xPosTextBox, setXPosButton);
            func.BindEnter(yPosTextBox, setYPosButton);
            func.BindEnter(zPosTextBox, setZPosButton);
            func.BindEnter(warpNameTextBox, saveWarpButton);

            LoadBuiltinWarps();
            LoadUserWarps();

            fillingSlots = true;
            for (int i = 0; i < IGame.PositionSlotCount; i++)
            {
                slotGrid.Rows.Add((i + 1).ToString(), "", "", "", "");
            }
            slotGrid.Rows[game.selectedPositionIndex].Selected = true;
            slotGrid.CurrentCell = slotGrid.Rows[game.selectedPositionIndex].Cells[0];
            fillingSlots = false;
            RefreshSlots();
            game.PositionSaved += game_PositionSaved;

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 10;
            pollTimer.Tick += PollTimer_Tick;
            pollTimer.Start();
        }

        // A row added below a group's last one.
        private const int ExtraRowHeight = 23;

        // The fastest horizontal speed seen since the editor opened or Reset.
        private float topSpeed;
        private readonly Label topSpeedValueLabel = new Label();

        /// <summary>
        /// Makes a group taller by <paramref name="extra"/>, moving the groups below it in its
        /// column down and growing the saved positions' table to match the window.
        /// </summary>
        private void GrowGroup(GroupBox group, int extra)
        {
            int bottom = group.Bottom;
            group.Height += extra;
            foreach (Control control in Controls)
            {
                if (control != group && control.Left == group.Left && control.Top >= bottom)
                {
                    control.Top += extra;
                }
            }
            savedPositionsGroupBox.Height += extra;
            slotGrid.Height += extra;
            saveSlotButton.Top += extra;
            loadSlotButton.Top += extra;
            clearSlotButton.Top += extra;
            slotInfoLabel.Top += extra;
            ClientSize = new System.Drawing.Size(ClientSize.Width, ClientSize.Height + extra);
        }

        /// <summary>Character Info's Top speed row, under the velocity, with a Reset button.</summary>
        private void AddTopSpeedRow()
        {
            int y = zVelLiveLabel.Top + ExtraRowHeight;
            GrowGroup(charInfoGroupBox, ExtraRowHeight);

            Label nameLabel = new Label();
            nameLabel.Text = "Top Speed:";
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(lblZVel.Left, y);
            charInfoGroupBox.Controls.Add(nameLabel);

            topSpeedValueLabel.Text = "N/A";
            topSpeedValueLabel.AutoSize = true;
            topSpeedValueLabel.Location = new System.Drawing.Point(zVelLiveLabel.Left, y);
            charInfoGroupBox.Controls.Add(topSpeedValueLabel);

            Button resetButton = new Button();
            resetButton.Text = "Reset Top Speed";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Size = new System.Drawing.Size(110, 23);
            // Under the entity fields, or right of the value when they're hidden (the position
            // and velocity rows have moved into their place).
            int buttonX = layout.hideEntityFields ? zVelLiveLabel.Left + 70 : lblEntityId.Left;
            resetButton.Location = new System.Drawing.Point(buttonX, y - 5);
            resetButton.Click += (sender, e) =>
            {
                topSpeed = 0;
                topSpeedValueLabel.Text = FormatFloat(topSpeed);
            };
            charInfoGroupBox.Controls.Add(resetButton);
        }

        /// <summary>Set Position's Fly speed row, for a game whose fly runs outside the editor (Sly 4).</summary>
        private void AddFlySpeedRow()
        {
            int y = setZPosButton.Bottom + 8;
            GrowGroup(positionGroupBox, ExtraRowHeight + 4);

            Label speedLabel = new Label();
            speedLabel.Text = "Fly speed:";
            speedLabel.AutoSize = true;
            speedLabel.Location = new System.Drawing.Point(lblSetZ.Left, y + 3);
            positionGroupBox.Controls.Add(speedLabel);

            NumericUpDown speedUpDown = new NumericUpDown();
            speedUpDown.Minimum = 1;
            speedUpDown.Maximum = 200;
            speedUpDown.Value = (decimal)Math.Max(1, Math.Min(200, layout.getFlySpeed()));
            speedUpDown.Location = new System.Drawing.Point(zPosTextBox.Left, y);
            speedUpDown.Size = new System.Drawing.Size(zPosTextBox.Width, 20);
            speedUpDown.ValueChanged += (sender, e) => layout.setFlySpeed((float)speedUpDown.Value);
            positionGroupBox.Controls.Add(speedUpDown);

            Label help = new Label();
            help.Text = "Fly Mode is on the Practice tab.";
            help.AutoSize = true;
            help.Location = new System.Drawing.Point(setZPosButton.Left, y + 3);
            positionGroupBox.Controls.Add(help);
        }

        private bool FlyModeEnabled => host != null && host.FlyModeEnabled;
        private bool InfiniteJumpEnabled => host != null && host.InfiniteJumpEnabled;

        /// <summary>A load is in progress, so nothing may be written to the player.</summary>
        private bool GameLoading
        {
            get
            {
                if (game.IsLoading)
                {
                    return true;
                }
                try
                {
                    return layout.isLoading != null && layout.isLoading();
                }
                catch
                {
                    return true;
                }
            }
        }

        // The horizontal axes: the two that aren't up.
        private uint HorizontalAxis1 => layout.heightAxis == 0 ? 1u : 0u;
        private uint HorizontalAxis2 => layout.heightAxis == 2 ? 1u : 2u;

        // Transform addresses for one axis (0 = X, 1 = Y, 2 = Z).
        private uint PositionAddress(uint transformPtr, uint axis) => transformPtr + layout.positionOffset + axis * 4;
        private uint VelocityAddress(uint transformPtr, uint axis) => transformPtr + layout.velocityOffset + axis * 4;

        /// <summary>X, Y and Z velocity, or null where the game's isn't known.</summary>
        private float[] ReadVelocity(uint transformPtr)
        {
            if (layout.readVelocity != null)
            {
                return layout.readVelocity();
            }
            if (layout.velocityOffset == 0)
            {
                return null;
            }
            return new float[]
            {
                ReadFloat(VelocityAddress(transformPtr, 0)),
                ReadFloat(VelocityAddress(transformPtr, 1)),
                ReadFloat(VelocityAddress(transformPtr, 2)),
            };
        }

        /// <summary>Moves the player to X, Y and Z, stopping them where the game's velocity is known.</summary>
        private void WritePositionVector(uint transformPtr, float x, float y, float z)
        {
            if (layout.writePosition != null)
            {
                layout.writePosition(x, y, z);
                return;
            }
            WritePosition(transformPtr, 0, x);
            WritePosition(transformPtr, 1, y);
            WritePosition(transformPtr, 2, z);
        }

        private bool TryResolvePointers(out uint entityPtr, out uint transformPtr)
        {
            entityPtr = 0;
            transformPtr = 0;
            try
            {
                if (layout.readEntity != null)
                {
                    entityPtr = layout.readEntity();
                }
                else
                {
                    byte[] epBytes = game.api.ReadMemory(game.pid, layout.activeCharacterPtr, 4);
                    entityPtr = BitConverter.ToUInt32(epBytes.Reverse().ToArray(), 0);
                }
                if (entityPtr == 0) return false;

                if (layout.transformInEntity)
                {
                    transformPtr = entityPtr + layout.transformOffset;
                    return true;
                }

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
            if (layout.writePosition != null)
            {
                float[] position = new float[3];
                for (uint i = 0; i < 3; i++)
                {
                    position[i] = ReadFloat(PositionAddress(transformPtr, i));
                }
                position[axis] = value;
                layout.writePosition(position[0], position[1], position[2]);
                return;
            }
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
                RefreshSlots();
            }

            // During a load (the fade to black included) the player entity is being freed or set
            // up, and writing to it crashes the game. With no player, e.g. in a hideout, fly mode
            // takes the height again once there is one.
            if (GameLoading || !TryResolvePointers(out uint entityPtr, out uint transformPtr))
            {
                prevFlyMode = false;
                SetLabelsUnavailable();
                return;
            }

            try
            {
                bool flyModeEnabled = FlyModeEnabled;
                uint up = layout.heightAxis;
                uint across1 = HorizontalAxis1;
                uint across2 = HorizontalAxis2;

                // Position freezes; each also stops movement on its axis.
                if (frozenPosX.HasValue && !(flyModeEnabled && up == 0))
                {
                    WritePosition(transformPtr, 0, frozenPosX.Value);
                }
                if (frozenPosY.HasValue && !(flyModeEnabled && up == 1))
                {
                    WritePosition(transformPtr, 1, frozenPosY.Value);
                }
                if (frozenPosZ.HasValue && !(flyModeEnabled && up == 2))
                {
                    WritePosition(transformPtr, 2, frozenPosZ.Value);
                }

                // Fly mode: hold the height, adjust height with R1 / L1 (as every game's fly and free
                // camera), amplify horizontal movement with R2.
                if (flyModeEnabled && (!prevFlyMode || entityPtr != flyEntityPtr))
                {
                    flyFrozenZ = ReadFloat(PositionAddress(transformPtr, up));
                    prevFlyPosX = ReadFloat(PositionAddress(transformPtr, across1));
                    prevFlyPosY = ReadFloat(PositionAddress(transformPtr, across2));
                    flyEntityPtr = entityPtr;
                    // Not a position yet: the entity is still being set up.
                    if (!IsFinite(flyFrozenZ) || !IsFinite(prevFlyPosX) || !IsFinite(prevFlyPosY))
                    {
                        flyModeEnabled = false;
                    }
                    else
                    {
                        zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);
                    }
                }
                prevFlyMode = flyModeEnabled;

                if (flyModeEnabled)
                {
                    if (Inputs.IsPressed(Inputs.Buttons.r1)) flyFrozenZ += FlyHeightStep;
                    if (Inputs.IsPressed(Inputs.Buttons.l1)) flyFrozenZ -= FlyHeightStep;

                    WritePosition(transformPtr, up, flyFrozenZ);
                    // Don't overwrite a value the user is typing.
                    if (!zPosTextBox.Focused)
                    {
                        zPosTextBox.Text = flyFrozenZ.ToString("F3", CultureInfo.InvariantCulture);
                    }

                    float curX = ReadFloat(PositionAddress(transformPtr, across1));
                    float curY = ReadFloat(PositionAddress(transformPtr, across2));
                    if (Inputs.IsPressed(Inputs.Buttons.r2))  // horizontal boost
                    {
                        float deltaX = curX - prevFlyPosX;
                        float deltaY = curY - prevFlyPosY;
                        if (Math.Abs(deltaX) > 0.001f || Math.Abs(deltaY) > 0.001f)
                        {
                            float newX = prevFlyPosX + deltaX * FlyBoostMultiplier;
                            float newY = prevFlyPosY + deltaY * FlyBoostMultiplier;
                            WriteFloat(PositionAddress(transformPtr, across1), newX);
                            WriteFloat(PositionAddress(transformPtr, across2), newY);
                            curX = newX;
                            curY = newY;
                        }
                    }
                    prevFlyPosX = curX;
                    prevFlyPosY = curY;
                }

                // Infinite jump: write the full 4 bytes; the LSB is what the game checks.
                if (InfiniteJumpEnabled && layout.applyInfiniteJump != null)
                {
                    layout.applyInfiniteJump(entityPtr);
                }
                else if (InfiniteJumpEnabled && layout.infiniteJumpOffset != 0)
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

                float[] velocity = ReadVelocity(transformPtr);
                if (velocity != null)
                {
                    float speed = (float)Math.Sqrt(velocity[across1] * velocity[across1] + velocity[across2] * velocity[across2]);
                    hSpeedLabel.Text = FormatFloat(speed);
                    zVelLiveLabel.Text = FormatFloat(velocity[up]);
                    if (IsFinite(speed) && speed > topSpeed)
                    {
                        topSpeed = speed;
                    }
                    topSpeedValueLabel.Text = FormatFloat(topSpeed);
                }
                else
                {
                    hSpeedLabel.Text = "N/A";
                    zVelLiveLabel.Text = "N/A";
                    topSpeedValueLabel.Text = "N/A";
                }
            }
            catch
            {
                SetLabelsUnavailable();
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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
                game.Warp(() => WritePosition(transformPtr, 0, val));
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
                game.Warp(() => WritePosition(transformPtr, 1, val));
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
                game.Warp(() => WritePosition(transformPtr, 2, val));
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
            game.PositionSaved -= game_PositionSaved;
            pollTimer.Stop();
            pollTimer.Dispose();
        }

        // --- Saved Positions ---

        // True while the table is being filled in code, so its events don't act on it.
        private bool fillingSlots;

        private int SelectedSlot => slotGrid.CurrentRow != null ? slotGrid.CurrentRow.Index : game.selectedPositionIndex;

        /// <summary>
        /// Reads a saved position: 24 hex digits holding X, Y and Z as big-endian floats.
        /// </summary>
        private static bool TryParseSavedPosition(string hex, out float x, out float y, out float z)
        {
            x = y = z = 0;
            if (hex == null || hex.Length != 24)
            {
                return false;
            }
            try
            {
                byte[] bytes = new byte[12];
                for (int i = 0; i < 12; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                x = BitConverter.ToSingle(bytes.Skip(0).Take(4).Reverse().ToArray(), 0);
                y = BitConverter.ToSingle(bytes.Skip(4).Take(4).Reverse().ToArray(), 0);
                z = BitConverter.ToSingle(bytes.Skip(8).Take(4).Reverse().ToArray(), 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SlotHasPosition(int slot)
        {
            float x, y, z;
            return currentMapIndicator != ""
                && TryParseSavedPosition(func.GetConfigData("config.txt", IGame.SavedPositionKey(currentMapIndicator, slot)), out x, out y, out z);
        }

        private string SlotName(int slot)
        {
            return currentMapIndicator == "" ? "" : func.GetConfigData("config.txt", IGame.SavedPositionNameKey(currentMapIndicator, slot));
        }

        /// <summary>
        /// Fills the table with the current map's slots. Called when the map changes and after a
        /// save, including one from a combo.
        /// </summary>
        private void RefreshSlots()
        {
            if (slotGrid.IsCurrentCellInEditMode)
            {
                // Don't throw away a name being typed; the edit's end refreshes it.
                return;
            }

            fillingSlots = true;
            try
            {
                for (int i = 0; i < IGame.PositionSlotCount; i++)
                {
                    DataGridViewRow row = slotGrid.Rows[i];
                    row.Cells[slotNameColumn.Index].Value = SlotName(i);

                    float x, y, z;
                    string hex = currentMapIndicator == "" ? "" : func.GetConfigData("config.txt", IGame.SavedPositionKey(currentMapIndicator, i));
                    bool saved = TryParseSavedPosition(hex, out x, out y, out z);
                    row.Cells[slotXColumn.Index].Value = saved ? x.ToString("F1", CultureInfo.InvariantCulture) : "";
                    row.Cells[slotYColumn.Index].Value = saved ? y.ToString("F1", CultureInfo.InvariantCulture) : "";
                    row.Cells[slotZColumn.Index].Value = saved ? z.ToString("F1", CultureInfo.InvariantCulture) : "";
                }
            }
            finally
            {
                fillingSlots = false;
            }

            bool mapKnown = currentMapIndicator != "";
            saveSlotButton.Enabled = mapKnown;
            loadSlotButton.Enabled = mapKnown;
            clearSlotButton.Enabled = mapKnown;
            UpdateSlotInfo();
        }

        private void UpdateSlotInfo()
        {
            int slot = game.selectedPositionIndex;
            string name = SlotName(slot);
            string slotText = name == "" ? $"slot {slot + 1}" : $"slot {slot + 1} ({name})";
            slotInfoLabel.Text = $"The Save Position and Load Position combos use {slotText}.\n\n"
                + "Each map has its own slots. Select a slot and type to name it; double-click a slot to load it.";
        }

        private void game_PositionSaved()
        {
            if (IsDisposed)
            {
                return;
            }
            RefreshSlots();
        }

        private void slotGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (fillingSlots || slotGrid.CurrentRow == null)
            {
                return;
            }
            game.selectedPositionIndex = slotGrid.CurrentRow.Index;
            UpdateSlotInfo();
        }

        private void slotGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (fillingSlots || e.ColumnIndex != slotNameColumn.Index)
            {
                return;
            }

            if (currentMapIndicator == "")
            {
                RefreshSlots();
                return;
            }

            object value = slotGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            // config.txt is one entry per line, so a name can't span lines.
            string name = (value == null ? "" : value.ToString()).Replace("\r", " ").Replace("\n", " ").Trim();
            func.ChangeFileLines("config.txt", name, IGame.SavedPositionNameKey(currentMapIndicator, e.RowIndex));
            RefreshSlots();
        }

        private void slotGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Double-clicking the name edits it instead.
            if (e.RowIndex >= 0 && e.ColumnIndex != slotNameColumn.Index)
            {
                LoadSlot(e.RowIndex);
            }
        }

        private void saveSlotButton_Click(object sender, EventArgs e)
        {
            int slot = SelectedSlot;
            game.selectedPositionIndex = slot;
            try
            {
                game.SavePosition();
                statusLine.Info($"Saved slot {slot + 1}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save slot {slot + 1}: {ex.Message}");
            }
        }

        private void loadSlotButton_Click(object sender, EventArgs e)
        {
            LoadSlot(SelectedSlot);
        }

        private void LoadSlot(int slot)
        {
            if (!SlotHasPosition(slot))
            {
                statusLine.Error($"Slot {slot + 1} is empty on this map.");
                return;
            }

            game.selectedPositionIndex = slot;
            try
            {
                game.LoadPosition();
                // Fly mode would otherwise pull the player back to the height it was holding.
                ResetFlyHeight();
                statusLine.Info($"Loaded slot {slot + 1}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load slot {slot + 1}: {ex.Message}");
            }
        }

        private void clearSlotButton_Click(object sender, EventArgs e)
        {
            int slot = SelectedSlot;
            if (!SlotHasPosition(slot) && SlotName(slot) == "")
            {
                return;
            }

            if (!ConfirmButton.Confirm(clearSlotButton, "Confirm", statusLine, $"Click Confirm to clear slot {slot + 1} on this map."))
            {
                return;
            }

            func.ChangeFileLines("config.txt", "", IGame.SavedPositionKey(currentMapIndicator, slot));
            func.ChangeFileLines("config.txt", "", IGame.SavedPositionNameKey(currentMapIndicator, slot));
            func.ChangeFileLines("config.txt", "", IGame.SavedFacingKey(currentMapIndicator, slot));
            RefreshSlots();
            statusLine.Info($"Cleared slot {slot + 1}.");
        }

        // --- Warp Locations ---

        private string ReadCurrentMapIndicator()
        {
            try
            {
                if (layout.readMapIndicator != null)
                {
                    return layout.readMapIndicator() ?? "";
                }
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
                game.Warp(() =>
                {
                    WritePositionVector(transformPtr, loc.X, loc.Y, loc.Z);
                    game.WriteFacing(loc.Facing);
                });
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
                byte[] facing = game.ReadFacing();

                userWarps.RemoveAll(w => w.MapIndicator == currentMapIndicator && w.Name == name);
                userWarps.Add(new WarpLocation { Name = name, MapIndicator = currentMapIndicator, X = x, Y = y, Z = z, Facing = facing, IsUserDefined = true });
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

                // Map|Name|X|Y|Z, then optionally the facing in hex.
                string[] parts = trimmed.Split('|');
                if (parts.Length != 5 && parts.Length != 6) continue;

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                byte[] facing = parts.Length == 6 ? IGame.FacingFromHex(parts[5]) : null;

                warps.Add(new WarpLocation { MapIndicator = parts[0], Name = parts[1], X = x, Y = y, Z = z, Facing = facing, IsUserDefined = isUserDefined });
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
                if (w.Facing != null)
                {
                    lines[i] += "|" + IGame.FacingToHex(w.Facing);
                }
            }
            File.WriteAllLines(userWarpFile, lines);
        }

        private void lblGadgetPower_Click(object sender, EventArgs e) { }
        private void lblHSpeed_Click(object sender, EventArgs e) { }
    }
}
