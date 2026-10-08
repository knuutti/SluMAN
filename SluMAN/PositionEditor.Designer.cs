namespace SluMAN
{
    partial class PositionEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PositionEditor));
            this.charInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.lblEntityId = new System.Windows.Forms.Label();
            this.entityIdValueLabel = new System.Windows.Forms.Label();
            this.lblHealth = new System.Windows.Forms.Label();
            this.healthValueLabel = new System.Windows.Forms.Label();
            this.lblGadgetPower = new System.Windows.Forms.Label();
            this.gadgetPowerValueLabel = new System.Windows.Forms.Label();
            this.lblOpacity = new System.Windows.Forms.Label();
            this.opacityValueLabel = new System.Windows.Forms.Label();
            this.lblRotation = new System.Windows.Forms.Label();
            this.rotationValueLabel = new System.Windows.Forms.Label();
            this.lblPosX = new System.Windows.Forms.Label();
            this.xPosLiveLabel = new System.Windows.Forms.Label();
            this.lblPosY = new System.Windows.Forms.Label();
            this.yPosLiveLabel = new System.Windows.Forms.Label();
            this.lblPosZ = new System.Windows.Forms.Label();
            this.zPosLiveLabel = new System.Windows.Forms.Label();
            this.lblHSpeed = new System.Windows.Forms.Label();
            this.hSpeedLabel = new System.Windows.Forms.Label();
            this.lblZVel = new System.Windows.Forms.Label();
            this.zVelLiveLabel = new System.Windows.Forms.Label();
            this.positionGroupBox = new System.Windows.Forms.GroupBox();
            this.lblSetX = new System.Windows.Forms.Label();
            this.xPosTextBox = new System.Windows.Forms.TextBox();
            this.setXPosButton = new System.Windows.Forms.Button();
            this.freezePosXCheckBox = new System.Windows.Forms.CheckBox();
            this.lblSetY = new System.Windows.Forms.Label();
            this.yPosTextBox = new System.Windows.Forms.TextBox();
            this.setYPosButton = new System.Windows.Forms.Button();
            this.freezePosYCheckBox = new System.Windows.Forms.CheckBox();
            this.lblSetZ = new System.Windows.Forms.Label();
            this.zPosTextBox = new System.Windows.Forms.TextBox();
            this.setZPosButton = new System.Windows.Forms.Button();
            this.freezePosZCheckBox = new System.Windows.Forms.CheckBox();
            this.warpGroupBox = new System.Windows.Forms.GroupBox();
            this.lblWarpLocation = new System.Windows.Forms.Label();
            this.warpLocationComboBox = new System.Windows.Forms.ComboBox();
            this.warpButton = new System.Windows.Forms.Button();
            this.lblWarpName = new System.Windows.Forms.Label();
            this.warpNameTextBox = new System.Windows.Forms.TextBox();
            this.saveWarpButton = new System.Windows.Forms.Button();
            this.deleteWarpButton = new System.Windows.Forms.Button();
            this.currentMapLabel = new System.Windows.Forms.Label();
            this.savedPositionsGroupBox = new System.Windows.Forms.GroupBox();
            this.slotGrid = new System.Windows.Forms.DataGridView();
            this.slotNumberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.slotNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.slotXColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.slotYColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.slotZColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.saveSlotButton = new System.Windows.Forms.Button();
            this.loadSlotButton = new System.Windows.Forms.Button();
            this.clearSlotButton = new System.Windows.Forms.Button();
            this.slotInfoLabel = new System.Windows.Forms.Label();
            this.savedPositionsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.slotGrid)).BeginInit();
            this.charInfoGroupBox.SuspendLayout();
            this.positionGroupBox.SuspendLayout();
            this.warpGroupBox.SuspendLayout();
            this.SuspendLayout();
            //
            // charInfoGroupBox
            //
            this.charInfoGroupBox.Controls.Add(this.lblEntityId);
            this.charInfoGroupBox.Controls.Add(this.entityIdValueLabel);
            this.charInfoGroupBox.Controls.Add(this.lblHealth);
            this.charInfoGroupBox.Controls.Add(this.healthValueLabel);
            this.charInfoGroupBox.Controls.Add(this.lblGadgetPower);
            this.charInfoGroupBox.Controls.Add(this.gadgetPowerValueLabel);
            this.charInfoGroupBox.Controls.Add(this.lblOpacity);
            this.charInfoGroupBox.Controls.Add(this.opacityValueLabel);
            this.charInfoGroupBox.Controls.Add(this.lblRotation);
            this.charInfoGroupBox.Controls.Add(this.rotationValueLabel);
            this.charInfoGroupBox.Controls.Add(this.lblPosX);
            this.charInfoGroupBox.Controls.Add(this.xPosLiveLabel);
            this.charInfoGroupBox.Controls.Add(this.lblPosY);
            this.charInfoGroupBox.Controls.Add(this.yPosLiveLabel);
            this.charInfoGroupBox.Controls.Add(this.lblPosZ);
            this.charInfoGroupBox.Controls.Add(this.zPosLiveLabel);
            this.charInfoGroupBox.Controls.Add(this.lblHSpeed);
            this.charInfoGroupBox.Controls.Add(this.hSpeedLabel);
            this.charInfoGroupBox.Controls.Add(this.lblZVel);
            this.charInfoGroupBox.Controls.Add(this.zVelLiveLabel);
            this.charInfoGroupBox.Location = new System.Drawing.Point(10, 10);
            this.charInfoGroupBox.Name = "charInfoGroupBox";
            this.charInfoGroupBox.Size = new System.Drawing.Size(462, 130);
            this.charInfoGroupBox.TabIndex = 0;
            this.charInfoGroupBox.TabStop = false;
            this.charInfoGroupBox.Text = "Character Info";
            //
            // lblEntityId
            //
            this.lblEntityId.AutoSize = true;
            this.lblEntityId.Location = new System.Drawing.Point(8, 22);
            this.lblEntityId.Name = "lblEntityId";
            this.lblEntityId.Size = new System.Drawing.Size(21, 13);
            this.lblEntityId.TabIndex = 0;
            this.lblEntityId.Text = "ID:";
            //
            // entityIdValueLabel
            //
            this.entityIdValueLabel.AutoSize = true;
            this.entityIdValueLabel.Location = new System.Drawing.Point(56, 22);
            this.entityIdValueLabel.Name = "entityIdValueLabel";
            this.entityIdValueLabel.Size = new System.Drawing.Size(27, 13);
            this.entityIdValueLabel.TabIndex = 1;
            this.entityIdValueLabel.Text = "N/A";
            //
            // lblHealth
            //
            this.lblHealth.AutoSize = true;
            this.lblHealth.Location = new System.Drawing.Point(8, 45);
            this.lblHealth.Name = "lblHealth";
            this.lblHealth.Size = new System.Drawing.Size(41, 13);
            this.lblHealth.TabIndex = 2;
            this.lblHealth.Text = "Health:";
            //
            // healthValueLabel
            //
            this.healthValueLabel.AutoSize = true;
            this.healthValueLabel.Location = new System.Drawing.Point(56, 45);
            this.healthValueLabel.Name = "healthValueLabel";
            this.healthValueLabel.Size = new System.Drawing.Size(27, 13);
            this.healthValueLabel.TabIndex = 3;
            this.healthValueLabel.Text = "N/A";
            //
            // lblGadgetPower
            //
            this.lblGadgetPower.AutoSize = true;
            this.lblGadgetPower.Location = new System.Drawing.Point(8, 68);
            this.lblGadgetPower.Name = "lblGadgetPower";
            this.lblGadgetPower.Size = new System.Drawing.Size(35, 13);
            this.lblGadgetPower.TabIndex = 4;
            this.lblGadgetPower.Text = "Juice:";
            this.lblGadgetPower.Click += new System.EventHandler(this.lblGadgetPower_Click);
            //
            // gadgetPowerValueLabel
            //
            this.gadgetPowerValueLabel.AutoSize = true;
            this.gadgetPowerValueLabel.Location = new System.Drawing.Point(56, 68);
            this.gadgetPowerValueLabel.Name = "gadgetPowerValueLabel";
            this.gadgetPowerValueLabel.Size = new System.Drawing.Size(27, 13);
            this.gadgetPowerValueLabel.TabIndex = 5;
            this.gadgetPowerValueLabel.Text = "N/A";
            //
            // lblOpacity
            //
            this.lblOpacity.AutoSize = true;
            this.lblOpacity.Location = new System.Drawing.Point(8, 91);
            this.lblOpacity.Name = "lblOpacity";
            this.lblOpacity.Size = new System.Drawing.Size(46, 13);
            this.lblOpacity.TabIndex = 6;
            this.lblOpacity.Text = "Opacity:";
            //
            // opacityValueLabel
            //
            this.opacityValueLabel.AutoSize = true;
            this.opacityValueLabel.Location = new System.Drawing.Point(56, 91);
            this.opacityValueLabel.Name = "opacityValueLabel";
            this.opacityValueLabel.Size = new System.Drawing.Size(27, 13);
            this.opacityValueLabel.TabIndex = 7;
            this.opacityValueLabel.Text = "N/A";
            //
            // lblRotation
            //
            this.lblRotation.AutoSize = true;
            this.lblRotation.Location = new System.Drawing.Point(132, 91);
            this.lblRotation.Name = "lblRotation";
            this.lblRotation.Size = new System.Drawing.Size(37, 13);
            this.lblRotation.TabIndex = 8;
            this.lblRotation.Text = "Angle:";
            //
            // rotationValueLabel
            //
            this.rotationValueLabel.AutoSize = true;
            this.rotationValueLabel.Location = new System.Drawing.Point(175, 91);
            this.rotationValueLabel.Name = "rotationValueLabel";
            this.rotationValueLabel.Size = new System.Drawing.Size(27, 13);
            this.rotationValueLabel.TabIndex = 9;
            this.rotationValueLabel.Text = "N/A";
            //
            // lblPosX
            //
            this.lblPosX.AutoSize = true;
            this.lblPosX.Location = new System.Drawing.Point(132, 22);
            this.lblPosX.Name = "lblPosX";
            this.lblPosX.Size = new System.Drawing.Size(17, 13);
            this.lblPosX.TabIndex = 10;
            this.lblPosX.Text = "X:";
            //
            // xPosLiveLabel
            //
            this.xPosLiveLabel.Location = new System.Drawing.Point(175, 22);
            this.xPosLiveLabel.Name = "xPosLiveLabel";
            this.xPosLiveLabel.Size = new System.Drawing.Size(88, 13);
            this.xPosLiveLabel.TabIndex = 11;
            this.xPosLiveLabel.Text = "N/A";
            //
            // lblPosY
            //
            this.lblPosY.AutoSize = true;
            this.lblPosY.Location = new System.Drawing.Point(132, 45);
            this.lblPosY.Name = "lblPosY";
            this.lblPosY.Size = new System.Drawing.Size(17, 13);
            this.lblPosY.TabIndex = 12;
            this.lblPosY.Text = "Y:";
            //
            // yPosLiveLabel
            //
            this.yPosLiveLabel.Location = new System.Drawing.Point(175, 45);
            this.yPosLiveLabel.Name = "yPosLiveLabel";
            this.yPosLiveLabel.Size = new System.Drawing.Size(88, 13);
            this.yPosLiveLabel.TabIndex = 13;
            this.yPosLiveLabel.Text = "N/A";
            //
            // lblPosZ
            //
            this.lblPosZ.AutoSize = true;
            this.lblPosZ.Location = new System.Drawing.Point(132, 68);
            this.lblPosZ.Name = "lblPosZ";
            this.lblPosZ.Size = new System.Drawing.Size(17, 13);
            this.lblPosZ.TabIndex = 14;
            this.lblPosZ.Text = "Z:";
            //
            // zPosLiveLabel
            //
            this.zPosLiveLabel.Location = new System.Drawing.Point(175, 68);
            this.zPosLiveLabel.Name = "zPosLiveLabel";
            this.zPosLiveLabel.Size = new System.Drawing.Size(88, 13);
            this.zPosLiveLabel.TabIndex = 15;
            this.zPosLiveLabel.Text = "N/A";
            //
            // lblHSpeed
            //
            this.lblHSpeed.AutoSize = true;
            this.lblHSpeed.Location = new System.Drawing.Point(269, 22);
            this.lblHSpeed.Name = "lblHSpeed";
            this.lblHSpeed.Size = new System.Drawing.Size(103, 13);
            this.lblHSpeed.TabIndex = 16;
            this.lblHSpeed.Text = "Velocity (Horizontal):";
            this.lblHSpeed.Click += new System.EventHandler(this.lblHSpeed_Click);
            //
            // hSpeedLabel
            //
            this.hSpeedLabel.Location = new System.Drawing.Point(378, 22);
            this.hSpeedLabel.Name = "hSpeedLabel";
            this.hSpeedLabel.Size = new System.Drawing.Size(88, 13);
            this.hSpeedLabel.TabIndex = 17;
            this.hSpeedLabel.Text = "N/A";
            //
            // lblZVel
            //
            this.lblZVel.AutoSize = true;
            this.lblZVel.Location = new System.Drawing.Point(269, 45);
            this.lblZVel.Name = "lblZVel";
            this.lblZVel.Size = new System.Drawing.Size(91, 13);
            this.lblZVel.TabIndex = 18;
            this.lblZVel.Text = "Velocity (Vertical):";
            //
            // zVelLiveLabel
            //
            this.zVelLiveLabel.Location = new System.Drawing.Point(378, 45);
            this.zVelLiveLabel.Name = "zVelLiveLabel";
            this.zVelLiveLabel.Size = new System.Drawing.Size(88, 13);
            this.zVelLiveLabel.TabIndex = 19;
            this.zVelLiveLabel.Text = "N/A";
            //
            // positionGroupBox
            //
            this.positionGroupBox.Controls.Add(this.lblSetX);
            this.positionGroupBox.Controls.Add(this.xPosTextBox);
            this.positionGroupBox.Controls.Add(this.setXPosButton);
            this.positionGroupBox.Controls.Add(this.freezePosXCheckBox);
            this.positionGroupBox.Controls.Add(this.lblSetY);
            this.positionGroupBox.Controls.Add(this.yPosTextBox);
            this.positionGroupBox.Controls.Add(this.setYPosButton);
            this.positionGroupBox.Controls.Add(this.freezePosYCheckBox);
            this.positionGroupBox.Controls.Add(this.lblSetZ);
            this.positionGroupBox.Controls.Add(this.zPosTextBox);
            this.positionGroupBox.Controls.Add(this.setZPosButton);
            this.positionGroupBox.Controls.Add(this.freezePosZCheckBox);
            this.positionGroupBox.Location = new System.Drawing.Point(10, 148);
            this.positionGroupBox.Name = "positionGroupBox";
            this.positionGroupBox.Size = new System.Drawing.Size(462, 115);
            this.positionGroupBox.TabIndex = 1;
            this.positionGroupBox.TabStop = false;
            this.positionGroupBox.Text = "Set Position";
            //
            // lblSetX
            //
            this.lblSetX.AutoSize = true;
            this.lblSetX.Location = new System.Drawing.Point(5, 25);
            this.lblSetX.Name = "lblSetX";
            this.lblSetX.Size = new System.Drawing.Size(17, 13);
            this.lblSetX.TabIndex = 0;
            this.lblSetX.Text = "X:";
            //
            // xPosTextBox
            //
            this.xPosTextBox.Location = new System.Drawing.Point(24, 22);
            this.xPosTextBox.Name = "xPosTextBox";
            this.xPosTextBox.Size = new System.Drawing.Size(150, 20);
            this.xPosTextBox.TabIndex = 0;
            //
            // setXPosButton
            //
            this.setXPosButton.Location = new System.Drawing.Point(179, 21);
            this.setXPosButton.Name = "setXPosButton";
            this.setXPosButton.Size = new System.Drawing.Size(50, 22);
            this.setXPosButton.TabIndex = 1;
            this.setXPosButton.Text = "Set";
            this.setXPosButton.UseVisualStyleBackColor = true;
            this.setXPosButton.Click += new System.EventHandler(this.setXPosButton_Click);
            //
            // freezePosXCheckBox
            //
            this.freezePosXCheckBox.AutoSize = true;
            this.freezePosXCheckBox.Location = new System.Drawing.Point(234, 23);
            this.freezePosXCheckBox.Name = "freezePosXCheckBox";
            this.freezePosXCheckBox.Size = new System.Drawing.Size(58, 17);
            this.freezePosXCheckBox.TabIndex = 2;
            this.freezePosXCheckBox.Text = "Freeze";
            this.freezePosXCheckBox.UseVisualStyleBackColor = true;
            this.freezePosXCheckBox.CheckedChanged += new System.EventHandler(this.freezePosXCheckBox_CheckedChanged);
            //
            // lblSetY
            //
            this.lblSetY.AutoSize = true;
            this.lblSetY.Location = new System.Drawing.Point(5, 53);
            this.lblSetY.Name = "lblSetY";
            this.lblSetY.Size = new System.Drawing.Size(17, 13);
            this.lblSetY.TabIndex = 3;
            this.lblSetY.Text = "Y:";
            //
            // yPosTextBox
            //
            this.yPosTextBox.Location = new System.Drawing.Point(24, 50);
            this.yPosTextBox.Name = "yPosTextBox";
            this.yPosTextBox.Size = new System.Drawing.Size(150, 20);
            this.yPosTextBox.TabIndex = 3;
            //
            // setYPosButton
            //
            this.setYPosButton.Location = new System.Drawing.Point(179, 49);
            this.setYPosButton.Name = "setYPosButton";
            this.setYPosButton.Size = new System.Drawing.Size(50, 22);
            this.setYPosButton.TabIndex = 4;
            this.setYPosButton.Text = "Set";
            this.setYPosButton.UseVisualStyleBackColor = true;
            this.setYPosButton.Click += new System.EventHandler(this.setYPosButton_Click);
            //
            // freezePosYCheckBox
            //
            this.freezePosYCheckBox.AutoSize = true;
            this.freezePosYCheckBox.Location = new System.Drawing.Point(234, 51);
            this.freezePosYCheckBox.Name = "freezePosYCheckBox";
            this.freezePosYCheckBox.Size = new System.Drawing.Size(58, 17);
            this.freezePosYCheckBox.TabIndex = 5;
            this.freezePosYCheckBox.Text = "Freeze";
            this.freezePosYCheckBox.UseVisualStyleBackColor = true;
            this.freezePosYCheckBox.CheckedChanged += new System.EventHandler(this.freezePosYCheckBox_CheckedChanged);
            //
            // lblSetZ
            //
            this.lblSetZ.AutoSize = true;
            this.lblSetZ.Location = new System.Drawing.Point(5, 81);
            this.lblSetZ.Name = "lblSetZ";
            this.lblSetZ.Size = new System.Drawing.Size(17, 13);
            this.lblSetZ.TabIndex = 6;
            this.lblSetZ.Text = "Z:";
            //
            // zPosTextBox
            //
            this.zPosTextBox.Location = new System.Drawing.Point(24, 78);
            this.zPosTextBox.Name = "zPosTextBox";
            this.zPosTextBox.Size = new System.Drawing.Size(150, 20);
            this.zPosTextBox.TabIndex = 6;
            //
            // setZPosButton
            //
            this.setZPosButton.Location = new System.Drawing.Point(179, 77);
            this.setZPosButton.Name = "setZPosButton";
            this.setZPosButton.Size = new System.Drawing.Size(50, 22);
            this.setZPosButton.TabIndex = 7;
            this.setZPosButton.Text = "Set";
            this.setZPosButton.UseVisualStyleBackColor = true;
            this.setZPosButton.Click += new System.EventHandler(this.setZPosButton_Click);
            //
            // freezePosZCheckBox
            //
            this.freezePosZCheckBox.AutoSize = true;
            this.freezePosZCheckBox.Location = new System.Drawing.Point(234, 79);
            this.freezePosZCheckBox.Name = "freezePosZCheckBox";
            this.freezePosZCheckBox.Size = new System.Drawing.Size(58, 17);
            this.freezePosZCheckBox.TabIndex = 8;
            this.freezePosZCheckBox.Text = "Freeze";
            this.freezePosZCheckBox.UseVisualStyleBackColor = true;
            this.freezePosZCheckBox.CheckedChanged += new System.EventHandler(this.freezePosZCheckBox_CheckedChanged);
            //
            // warpGroupBox
            //
            this.warpGroupBox.Controls.Add(this.lblWarpLocation);
            this.warpGroupBox.Controls.Add(this.warpLocationComboBox);
            this.warpGroupBox.Controls.Add(this.warpButton);
            this.warpGroupBox.Controls.Add(this.lblWarpName);
            this.warpGroupBox.Controls.Add(this.warpNameTextBox);
            this.warpGroupBox.Controls.Add(this.saveWarpButton);
            this.warpGroupBox.Controls.Add(this.deleteWarpButton);
            this.warpGroupBox.Controls.Add(this.currentMapLabel);
            this.warpGroupBox.Location = new System.Drawing.Point(10, 271);
            this.warpGroupBox.Name = "warpGroupBox";
            this.warpGroupBox.Size = new System.Drawing.Size(462, 133);
            this.warpGroupBox.TabIndex = 2;
            this.warpGroupBox.TabStop = false;
            this.warpGroupBox.Text = "Warp Locations";
            //
            // lblWarpLocation
            //
            this.lblWarpLocation.AutoSize = true;
            this.lblWarpLocation.Location = new System.Drawing.Point(8, 25);
            this.lblWarpLocation.Name = "lblWarpLocation";
            this.lblWarpLocation.Size = new System.Drawing.Size(51, 13);
            this.lblWarpLocation.TabIndex = 0;
            this.lblWarpLocation.Text = "Location:";
            //
            // warpLocationComboBox
            //
            this.warpLocationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.warpLocationComboBox.FormattingEnabled = true;
            this.warpLocationComboBox.Location = new System.Drawing.Point(65, 22);
            this.warpLocationComboBox.Name = "warpLocationComboBox";
            this.warpLocationComboBox.Size = new System.Drawing.Size(285, 21);
            this.warpLocationComboBox.TabIndex = 0;
            this.warpLocationComboBox.SelectedIndexChanged += new System.EventHandler(this.warpLocationComboBox_SelectedIndexChanged);
            //
            // warpButton
            //
            this.warpButton.Location = new System.Drawing.Point(355, 21);
            this.warpButton.Name = "warpButton";
            this.warpButton.Size = new System.Drawing.Size(97, 23);
            this.warpButton.TabIndex = 1;
            this.warpButton.Text = "Warp";
            this.warpButton.UseVisualStyleBackColor = true;
            this.warpButton.Click += new System.EventHandler(this.warpButton_Click);
            //
            // lblWarpName
            //
            this.lblWarpName.AutoSize = true;
            this.lblWarpName.Location = new System.Drawing.Point(8, 56);
            this.lblWarpName.Name = "lblWarpName";
            this.lblWarpName.Size = new System.Drawing.Size(38, 13);
            this.lblWarpName.TabIndex = 2;
            this.lblWarpName.Text = "Name:";
            //
            // warpNameTextBox
            //
            this.warpNameTextBox.Location = new System.Drawing.Point(65, 52);
            this.warpNameTextBox.Name = "warpNameTextBox";
            this.warpNameTextBox.Size = new System.Drawing.Size(285, 20);
            this.warpNameTextBox.TabIndex = 2;
            //
            // saveWarpButton
            //
            this.saveWarpButton.Location = new System.Drawing.Point(65, 80);
            this.saveWarpButton.Name = "saveWarpButton";
            this.saveWarpButton.Size = new System.Drawing.Size(120, 23);
            this.saveWarpButton.TabIndex = 3;
            this.saveWarpButton.Text = "Save Position";
            this.saveWarpButton.UseVisualStyleBackColor = true;
            this.saveWarpButton.Click += new System.EventHandler(this.saveWarpButton_Click);
            //
            // deleteWarpButton
            //
            this.deleteWarpButton.Enabled = false;
            this.deleteWarpButton.Location = new System.Drawing.Point(190, 80);
            this.deleteWarpButton.Name = "deleteWarpButton";
            this.deleteWarpButton.Size = new System.Drawing.Size(75, 23);
            this.deleteWarpButton.TabIndex = 4;
            this.deleteWarpButton.Text = "Delete";
            this.deleteWarpButton.UseVisualStyleBackColor = true;
            this.deleteWarpButton.Click += new System.EventHandler(this.deleteWarpButton_Click);
            //
            // currentMapLabel
            //
            this.currentMapLabel.Location = new System.Drawing.Point(8, 111);
            this.currentMapLabel.Name = "currentMapLabel";
            this.currentMapLabel.Size = new System.Drawing.Size(444, 13);
            this.currentMapLabel.TabIndex = 5;
            this.currentMapLabel.Text = "Current map: -";
            //
            // savedPositionsGroupBox
            //
            this.savedPositionsGroupBox.Controls.Add(this.slotGrid);
            this.savedPositionsGroupBox.Controls.Add(this.saveSlotButton);
            this.savedPositionsGroupBox.Controls.Add(this.loadSlotButton);
            this.savedPositionsGroupBox.Controls.Add(this.clearSlotButton);
            this.savedPositionsGroupBox.Controls.Add(this.slotInfoLabel);
            this.savedPositionsGroupBox.Location = new System.Drawing.Point(482, 10);
            this.savedPositionsGroupBox.Name = "savedPositionsGroupBox";
            this.savedPositionsGroupBox.Size = new System.Drawing.Size(330, 394);
            this.savedPositionsGroupBox.TabIndex = 3;
            this.savedPositionsGroupBox.TabStop = false;
            this.savedPositionsGroupBox.Text = "Saved Positions (this map)";
            //
            // slotGrid
            //
            this.slotGrid.AllowUserToAddRows = false;
            this.slotGrid.AllowUserToDeleteRows = false;
            this.slotGrid.AllowUserToResizeRows = false;
            this.slotGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.slotGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.slotGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.slotNumberColumn,
            this.slotNameColumn,
            this.slotXColumn,
            this.slotYColumn,
            this.slotZColumn});
            this.slotGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystrokeOrF2;
            this.slotGrid.Location = new System.Drawing.Point(10, 20);
            this.slotGrid.MultiSelect = false;
            this.slotGrid.Name = "slotGrid";
            this.slotGrid.RowHeadersVisible = false;
            this.slotGrid.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.slotGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.slotGrid.Size = new System.Drawing.Size(310, 222);
            this.slotGrid.TabIndex = 0;
            this.slotGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.slotGrid_CellEndEdit);
            this.slotGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.slotGrid_CellDoubleClick);
            this.slotGrid.SelectionChanged += new System.EventHandler(this.slotGrid_SelectionChanged);
            //
            // slotNumberColumn
            //
            this.slotNumberColumn.FillWeight = 12F;
            this.slotNumberColumn.HeaderText = "#";
            this.slotNumberColumn.Name = "slotNumberColumn";
            this.slotNumberColumn.ReadOnly = true;
            this.slotNumberColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // slotNameColumn
            //
            this.slotNameColumn.FillWeight = 46F;
            this.slotNameColumn.HeaderText = "Name";
            this.slotNameColumn.MaxInputLength = 48;
            this.slotNameColumn.Name = "slotNameColumn";
            this.slotNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // slotXColumn
            //
            this.slotXColumn.FillWeight = 26F;
            this.slotXColumn.HeaderText = "X";
            this.slotXColumn.Name = "slotXColumn";
            this.slotXColumn.ReadOnly = true;
            this.slotXColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // slotYColumn
            //
            this.slotYColumn.FillWeight = 26F;
            this.slotYColumn.HeaderText = "Y";
            this.slotYColumn.Name = "slotYColumn";
            this.slotYColumn.ReadOnly = true;
            this.slotYColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // slotZColumn
            //
            this.slotZColumn.FillWeight = 26F;
            this.slotZColumn.HeaderText = "Z";
            this.slotZColumn.Name = "slotZColumn";
            this.slotZColumn.ReadOnly = true;
            this.slotZColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // saveSlotButton
            //
            this.saveSlotButton.Location = new System.Drawing.Point(10, 250);
            this.saveSlotButton.Name = "saveSlotButton";
            this.saveSlotButton.Size = new System.Drawing.Size(98, 30);
            this.saveSlotButton.TabIndex = 1;
            this.saveSlotButton.Text = "Save Here";
            this.saveSlotButton.UseVisualStyleBackColor = true;
            this.saveSlotButton.Click += new System.EventHandler(this.saveSlotButton_Click);
            //
            // loadSlotButton
            //
            this.loadSlotButton.Location = new System.Drawing.Point(116, 250);
            this.loadSlotButton.Name = "loadSlotButton";
            this.loadSlotButton.Size = new System.Drawing.Size(98, 30);
            this.loadSlotButton.TabIndex = 2;
            this.loadSlotButton.Text = "Load";
            this.loadSlotButton.UseVisualStyleBackColor = true;
            this.loadSlotButton.Click += new System.EventHandler(this.loadSlotButton_Click);
            //
            // clearSlotButton
            //
            this.clearSlotButton.Location = new System.Drawing.Point(222, 250);
            this.clearSlotButton.Name = "clearSlotButton";
            this.clearSlotButton.Size = new System.Drawing.Size(98, 30);
            this.clearSlotButton.TabIndex = 3;
            this.clearSlotButton.Text = "Clear";
            this.clearSlotButton.UseVisualStyleBackColor = true;
            this.clearSlotButton.Click += new System.EventHandler(this.clearSlotButton_Click);
            //
            // slotInfoLabel
            //
            this.slotInfoLabel.Location = new System.Drawing.Point(8, 292);
            this.slotInfoLabel.Name = "slotInfoLabel";
            this.slotInfoLabel.Size = new System.Drawing.Size(312, 92);
            this.slotInfoLabel.TabIndex = 4;
            this.slotInfoLabel.Text = "The Save Position and Load Position combos use the selected slot.";
            //
            // PositionEditor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 414);
            this.Controls.Add(this.savedPositionsGroupBox);
            this.Controls.Add(this.charInfoGroupBox);
            this.Controls.Add(this.positionGroupBox);
            this.Controls.Add(this.warpGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "PositionEditor";
            this.Text = "SluMAN :: Position Editor (Sly 3)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PositionEditor_FormClosing);
            this.charInfoGroupBox.ResumeLayout(false);
            this.charInfoGroupBox.PerformLayout();
            this.savedPositionsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.slotGrid)).EndInit();
            this.positionGroupBox.ResumeLayout(false);
            this.positionGroupBox.PerformLayout();
            this.warpGroupBox.ResumeLayout(false);
            this.warpGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.GroupBox charInfoGroupBox;
        private System.Windows.Forms.Label lblEntityId;
        private System.Windows.Forms.Label entityIdValueLabel;
        private System.Windows.Forms.Label lblHealth;
        private System.Windows.Forms.Label healthValueLabel;
        private System.Windows.Forms.Label lblGadgetPower;
        private System.Windows.Forms.Label gadgetPowerValueLabel;
        private System.Windows.Forms.Label lblOpacity;
        private System.Windows.Forms.Label opacityValueLabel;
        private System.Windows.Forms.Label lblRotation;
        private System.Windows.Forms.Label rotationValueLabel;
        private System.Windows.Forms.Label lblPosX;
        private System.Windows.Forms.Label xPosLiveLabel;
        private System.Windows.Forms.Label lblPosY;
        private System.Windows.Forms.Label yPosLiveLabel;
        private System.Windows.Forms.Label lblPosZ;
        private System.Windows.Forms.Label zPosLiveLabel;
        private System.Windows.Forms.Label lblHSpeed;
        private System.Windows.Forms.Label hSpeedLabel;
        private System.Windows.Forms.Label lblZVel;
        private System.Windows.Forms.Label zVelLiveLabel;

        private System.Windows.Forms.GroupBox positionGroupBox;
        private System.Windows.Forms.Label lblSetX;
        private System.Windows.Forms.TextBox xPosTextBox;
        private System.Windows.Forms.Button setXPosButton;
        private System.Windows.Forms.CheckBox freezePosXCheckBox;
        private System.Windows.Forms.Label lblSetY;
        private System.Windows.Forms.TextBox yPosTextBox;
        private System.Windows.Forms.Button setYPosButton;
        private System.Windows.Forms.CheckBox freezePosYCheckBox;
        private System.Windows.Forms.Label lblSetZ;
        private System.Windows.Forms.TextBox zPosTextBox;
        private System.Windows.Forms.Button setZPosButton;
        private System.Windows.Forms.CheckBox freezePosZCheckBox;

        private System.Windows.Forms.GroupBox warpGroupBox;
        private System.Windows.Forms.Label lblWarpLocation;
        private System.Windows.Forms.ComboBox warpLocationComboBox;
        private System.Windows.Forms.Button warpButton;
        private System.Windows.Forms.Label lblWarpName;
        private System.Windows.Forms.TextBox warpNameTextBox;
        private System.Windows.Forms.Button saveWarpButton;
        private System.Windows.Forms.Button deleteWarpButton;
        private System.Windows.Forms.Label currentMapLabel;
        private System.Windows.Forms.GroupBox savedPositionsGroupBox;
        private System.Windows.Forms.DataGridView slotGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn slotNumberColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn slotNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn slotXColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn slotYColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn slotZColumn;
        private System.Windows.Forms.Button saveSlotButton;
        private System.Windows.Forms.Button loadSlotButton;
        private System.Windows.Forms.Button clearSlotButton;
        private System.Windows.Forms.Label slotInfoLabel;
    }
}
