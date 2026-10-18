namespace SluMAN
{
    partial class Sly4Practice
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sly4Practice));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchGameModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryUtilitiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openUserDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.powerOffPS3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rebootPS3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playerGroupBox = new System.Windows.Forms.GroupBox();
            this.coinsLabel = new System.Windows.Forms.Label();
            this.coinsValueLabel = new System.Windows.Forms.Label();
            this.coinsTextBox = new System.Windows.Forms.TextBox();
            this.setCoinsButton = new System.Windows.Forms.Button();
            this.positionLabel = new System.Windows.Forms.Label();
            this.positionValueLabel = new System.Windows.Forms.Label();
            this.velocityLabel = new System.Windows.Forms.Label();
            this.velocityValueLabel = new System.Windows.Forms.Label();
            this.xTextBox = new System.Windows.Forms.TextBox();
            this.yTextBox = new System.Windows.Forms.TextBox();
            this.zTextBox = new System.Windows.Forms.TextBox();
            this.currentPositionButton = new System.Windows.Forms.Button();
            this.teleportButton = new System.Windows.Forms.Button();
            this.savedPositionsGroupBox = new System.Windows.Forms.GroupBox();
            this.slotComboBox = new System.Windows.Forms.ComboBox();
            this.savePosButton = new System.Windows.Forms.Button();
            this.loadPosButton = new System.Windows.Forms.Button();
            this.slowMotionCheckBox = new System.Windows.Forms.CheckBox();
            this.alwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.playerGroupBox.SuspendLayout();
            this.savedPositionsGroupBox.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip1
            //
            this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(300, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            //
            // toolsToolStripMenuItem
            //
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.switchGameModeToolStripMenuItem,
            this.memoryUtilitiesToolStripMenuItem,
            this.openUserDataToolStripMenuItem,
            this.toolStripSeparator1,
            this.powerOffPS3ToolStripMenuItem,
            this.rebootPS3ToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            //
            // switchGameModeToolStripMenuItem
            //
            this.switchGameModeToolStripMenuItem.Name = "switchGameModeToolStripMenuItem";
            this.switchGameModeToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.switchGameModeToolStripMenuItem.Text = "Switch Game/Mode";
            this.switchGameModeToolStripMenuItem.Click += new System.EventHandler(this.switchGameModeToolStripMenuItem_Click);
            //
            // memoryUtilitiesToolStripMenuItem
            //
            this.memoryUtilitiesToolStripMenuItem.Name = "memoryUtilitiesToolStripMenuItem";
            this.memoryUtilitiesToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.memoryUtilitiesToolStripMenuItem.Text = "Memory Utilities...";
            this.memoryUtilitiesToolStripMenuItem.Click += new System.EventHandler(this.memoryUtilitiesToolStripMenuItem_Click);
            //
            // openUserDataToolStripMenuItem
            //
            this.openUserDataToolStripMenuItem.Name = "openUserDataToolStripMenuItem";
            this.openUserDataToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.openUserDataToolStripMenuItem.Text = "Open User Data Folder";
            this.openUserDataToolStripMenuItem.Click += new System.EventHandler(this.openUserDataToolStripMenuItem_Click);
            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(189, 6);
            //
            // powerOffPS3ToolStripMenuItem
            //
            this.powerOffPS3ToolStripMenuItem.Name = "powerOffPS3ToolStripMenuItem";
            this.powerOffPS3ToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.powerOffPS3ToolStripMenuItem.Text = "Power Off (PS3)";
            this.powerOffPS3ToolStripMenuItem.Click += new System.EventHandler(this.powerOffPS3ToolStripMenuItem_Click);
            //
            // rebootPS3ToolStripMenuItem
            //
            this.rebootPS3ToolStripMenuItem.Name = "rebootPS3ToolStripMenuItem";
            this.rebootPS3ToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.rebootPS3ToolStripMenuItem.Text = "Reboot (PS3)";
            this.rebootPS3ToolStripMenuItem.Click += new System.EventHandler(this.rebootPS3ToolStripMenuItem_Click);
            //
            // playerGroupBox
            //
            this.playerGroupBox.Controls.Add(this.coinsLabel);
            this.playerGroupBox.Controls.Add(this.coinsValueLabel);
            this.playerGroupBox.Controls.Add(this.coinsTextBox);
            this.playerGroupBox.Controls.Add(this.setCoinsButton);
            this.playerGroupBox.Controls.Add(this.positionLabel);
            this.playerGroupBox.Controls.Add(this.positionValueLabel);
            this.playerGroupBox.Controls.Add(this.velocityLabel);
            this.playerGroupBox.Controls.Add(this.velocityValueLabel);
            this.playerGroupBox.Controls.Add(this.xTextBox);
            this.playerGroupBox.Controls.Add(this.yTextBox);
            this.playerGroupBox.Controls.Add(this.zTextBox);
            this.playerGroupBox.Controls.Add(this.currentPositionButton);
            this.playerGroupBox.Controls.Add(this.teleportButton);
            this.playerGroupBox.Location = new System.Drawing.Point(12, 32);
            this.playerGroupBox.Name = "playerGroupBox";
            this.playerGroupBox.Size = new System.Drawing.Size(276, 156);
            this.playerGroupBox.TabIndex = 1;
            this.playerGroupBox.TabStop = false;
            this.playerGroupBox.Text = "Player";
            //
            // coinsLabel
            //
            this.coinsLabel.AutoSize = true;
            this.coinsLabel.Location = new System.Drawing.Point(8, 23);
            this.coinsLabel.Name = "coinsLabel";
            this.coinsLabel.Size = new System.Drawing.Size(36, 13);
            this.coinsLabel.TabIndex = 0;
            this.coinsLabel.Text = "Coins:";
            //
            // coinsValueLabel
            //
            this.coinsValueLabel.AutoSize = true;
            this.coinsValueLabel.Location = new System.Drawing.Point(64, 23);
            this.coinsValueLabel.Name = "coinsValueLabel";
            this.coinsValueLabel.Size = new System.Drawing.Size(10, 13);
            this.coinsValueLabel.TabIndex = 1;
            this.coinsValueLabel.Text = "-";
            //
            // coinsTextBox
            //
            this.coinsTextBox.Location = new System.Drawing.Point(130, 20);
            this.coinsTextBox.Name = "coinsTextBox";
            this.coinsTextBox.Size = new System.Drawing.Size(70, 20);
            this.coinsTextBox.TabIndex = 2;
            //
            // setCoinsButton
            //
            this.setCoinsButton.Location = new System.Drawing.Point(206, 18);
            this.setCoinsButton.Name = "setCoinsButton";
            this.setCoinsButton.Size = new System.Drawing.Size(60, 23);
            this.setCoinsButton.TabIndex = 3;
            this.setCoinsButton.Text = "Set";
            this.setCoinsButton.UseVisualStyleBackColor = true;
            this.setCoinsButton.Click += new System.EventHandler(this.setCoinsButton_Click);
            //
            // positionLabel
            //
            this.positionLabel.AutoSize = true;
            this.positionLabel.Location = new System.Drawing.Point(8, 51);
            this.positionLabel.Name = "positionLabel";
            this.positionLabel.Size = new System.Drawing.Size(47, 13);
            this.positionLabel.TabIndex = 4;
            this.positionLabel.Text = "Position:";
            //
            // positionValueLabel
            //
            this.positionValueLabel.AutoSize = true;
            this.positionValueLabel.Location = new System.Drawing.Point(64, 51);
            this.positionValueLabel.Name = "positionValueLabel";
            this.positionValueLabel.Size = new System.Drawing.Size(10, 13);
            this.positionValueLabel.TabIndex = 5;
            this.positionValueLabel.Text = "-";
            //
            // velocityLabel
            //
            this.velocityLabel.AutoSize = true;
            this.velocityLabel.Location = new System.Drawing.Point(8, 73);
            this.velocityLabel.Name = "velocityLabel";
            this.velocityLabel.Size = new System.Drawing.Size(47, 13);
            this.velocityLabel.TabIndex = 6;
            this.velocityLabel.Text = "Velocity:";
            //
            // velocityValueLabel
            //
            this.velocityValueLabel.AutoSize = true;
            this.velocityValueLabel.Location = new System.Drawing.Point(64, 73);
            this.velocityValueLabel.Name = "velocityValueLabel";
            this.velocityValueLabel.Size = new System.Drawing.Size(10, 13);
            this.velocityValueLabel.TabIndex = 7;
            this.velocityValueLabel.Text = "-";
            //
            // xTextBox
            //
            this.xTextBox.Location = new System.Drawing.Point(10, 98);
            this.xTextBox.Name = "xTextBox";
            this.xTextBox.Size = new System.Drawing.Size(60, 20);
            this.xTextBox.TabIndex = 8;
            //
            // yTextBox
            //
            this.yTextBox.Location = new System.Drawing.Point(76, 98);
            this.yTextBox.Name = "yTextBox";
            this.yTextBox.Size = new System.Drawing.Size(60, 20);
            this.yTextBox.TabIndex = 9;
            //
            // zTextBox
            //
            this.zTextBox.Location = new System.Drawing.Point(142, 98);
            this.zTextBox.Name = "zTextBox";
            this.zTextBox.Size = new System.Drawing.Size(60, 20);
            this.zTextBox.TabIndex = 10;
            //
            // currentPositionButton
            //
            this.currentPositionButton.Location = new System.Drawing.Point(206, 96);
            this.currentPositionButton.Name = "currentPositionButton";
            this.currentPositionButton.Size = new System.Drawing.Size(60, 23);
            this.currentPositionButton.TabIndex = 11;
            this.currentPositionButton.Text = "Current";
            this.currentPositionButton.UseVisualStyleBackColor = true;
            this.currentPositionButton.Click += new System.EventHandler(this.currentPositionButton_Click);
            //
            // teleportButton
            //
            this.teleportButton.Location = new System.Drawing.Point(10, 124);
            this.teleportButton.Name = "teleportButton";
            this.teleportButton.Size = new System.Drawing.Size(256, 23);
            this.teleportButton.TabIndex = 12;
            this.teleportButton.Text = "Teleport to X, Y, Z";
            this.teleportButton.UseVisualStyleBackColor = true;
            this.teleportButton.Click += new System.EventHandler(this.teleportButton_Click);
            //
            // savedPositionsGroupBox
            //
            this.savedPositionsGroupBox.Controls.Add(this.slotComboBox);
            this.savedPositionsGroupBox.Controls.Add(this.savePosButton);
            this.savedPositionsGroupBox.Controls.Add(this.loadPosButton);
            this.savedPositionsGroupBox.Location = new System.Drawing.Point(12, 194);
            this.savedPositionsGroupBox.Name = "savedPositionsGroupBox";
            this.savedPositionsGroupBox.Size = new System.Drawing.Size(276, 54);
            this.savedPositionsGroupBox.TabIndex = 2;
            this.savedPositionsGroupBox.TabStop = false;
            this.savedPositionsGroupBox.Text = "Saved Positions";
            //
            // slotComboBox
            //
            this.slotComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.slotComboBox.FormattingEnabled = true;
            this.slotComboBox.Location = new System.Drawing.Point(10, 20);
            this.slotComboBox.Name = "slotComboBox";
            this.slotComboBox.Size = new System.Drawing.Size(80, 21);
            this.slotComboBox.TabIndex = 0;
            this.slotComboBox.SelectedIndexChanged += new System.EventHandler(this.slotComboBox_SelectedIndexChanged);
            //
            // savePosButton
            //
            this.savePosButton.Location = new System.Drawing.Point(96, 19);
            this.savePosButton.Name = "savePosButton";
            this.savePosButton.Size = new System.Drawing.Size(82, 23);
            this.savePosButton.TabIndex = 1;
            this.savePosButton.Text = "Save";
            this.savePosButton.UseVisualStyleBackColor = true;
            this.savePosButton.Click += new System.EventHandler(this.savePosButton_Click);
            //
            // loadPosButton
            //
            this.loadPosButton.Location = new System.Drawing.Point(184, 19);
            this.loadPosButton.Name = "loadPosButton";
            this.loadPosButton.Size = new System.Drawing.Size(82, 23);
            this.loadPosButton.TabIndex = 2;
            this.loadPosButton.Text = "Load";
            this.loadPosButton.UseVisualStyleBackColor = true;
            this.loadPosButton.Click += new System.EventHandler(this.loadPosButton_Click);
            //
            // slowMotionCheckBox
            //
            this.slowMotionCheckBox.AutoSize = true;
            this.slowMotionCheckBox.Location = new System.Drawing.Point(18, 256);
            this.slowMotionCheckBox.Name = "slowMotionCheckBox";
            this.slowMotionCheckBox.Size = new System.Drawing.Size(84, 17);
            this.slowMotionCheckBox.TabIndex = 3;
            this.slowMotionCheckBox.Text = "Slow Motion";
            this.slowMotionCheckBox.UseVisualStyleBackColor = true;
            this.slowMotionCheckBox.CheckedChanged += new System.EventHandler(this.slowMotionCheckBox_CheckedChanged);
            //
            // alwaysOnTopCheckBox
            //
            this.alwaysOnTopCheckBox.AutoSize = true;
            this.alwaysOnTopCheckBox.Location = new System.Drawing.Point(150, 256);
            this.alwaysOnTopCheckBox.Name = "alwaysOnTopCheckBox";
            this.alwaysOnTopCheckBox.Size = new System.Drawing.Size(98, 17);
            this.alwaysOnTopCheckBox.TabIndex = 4;
            this.alwaysOnTopCheckBox.Text = "Always On Top";
            this.alwaysOnTopCheckBox.UseVisualStyleBackColor = true;
            //
            // refreshTimer
            //
            this.refreshTimer.Interval = 200;
            this.refreshTimer.Tick += new System.EventHandler(this.refreshTimer_Tick);
            //
            // Sly4Practice
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 282);
            this.Controls.Add(this.playerGroupBox);
            this.Controls.Add(this.savedPositionsGroupBox);
            this.Controls.Add(this.slowMotionCheckBox);
            this.Controls.Add(this.alwaysOnTopCheckBox);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "Sly4Practice";
            this.Text = "SluMAN :: Sly Cooper: Thieves in Time (Practice Mode)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Sly4Practice_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.playerGroupBox.ResumeLayout(false);
            this.playerGroupBox.PerformLayout();
            this.savedPositionsGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchGameModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memoryUtilitiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openUserDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem powerOffPS3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rebootPS3ToolStripMenuItem;
        private System.Windows.Forms.GroupBox playerGroupBox;
        private System.Windows.Forms.Label coinsLabel;
        private System.Windows.Forms.Label coinsValueLabel;
        private System.Windows.Forms.TextBox coinsTextBox;
        private System.Windows.Forms.Button setCoinsButton;
        private System.Windows.Forms.Label positionLabel;
        private System.Windows.Forms.Label positionValueLabel;
        private System.Windows.Forms.Label velocityLabel;
        private System.Windows.Forms.Label velocityValueLabel;
        private System.Windows.Forms.TextBox xTextBox;
        private System.Windows.Forms.TextBox yTextBox;
        private System.Windows.Forms.TextBox zTextBox;
        private System.Windows.Forms.Button currentPositionButton;
        private System.Windows.Forms.Button teleportButton;
        private System.Windows.Forms.GroupBox savedPositionsGroupBox;
        private System.Windows.Forms.ComboBox slotComboBox;
        private System.Windows.Forms.Button savePosButton;
        private System.Windows.Forms.Button loadPosButton;
        private System.Windows.Forms.CheckBox slowMotionCheckBox;
        private System.Windows.Forms.CheckBox alwaysOnTopCheckBox;
        private System.Windows.Forms.Timer refreshTimer;
    }
}
