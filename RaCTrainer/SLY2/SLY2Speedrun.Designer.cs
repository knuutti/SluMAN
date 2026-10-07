namespace racman
{
    partial class SLY2Speedrun
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SLY2Speedrun));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchGameModeItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inputDisplayItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryUtilitiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.powerOffPS3Item = new System.Windows.Forms.ToolStripMenuItem();
            this.rebootPS3Item = new System.Windows.Forms.ToolStripMenuItem();
            this.inputDisplayButton = new System.Windows.Forms.Button();
            this.autosplitterCheckbox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.runFileComboBox = new System.Windows.Forms.ComboBox();
            this.loadRunFileButton = new System.Windows.Forms.Button();
            this.alwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
            this.gadgetsButton = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
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
            this.menuStrip1.Size = new System.Drawing.Size(239, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.switchGameModeItem,
            this.inputDisplayItem,
            this.memoryUtilitiesToolStripMenuItem,
            this.toolStripSeparator1,
            this.powerOffPS3Item,
            this.rebootPS3Item});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            this.toolsToolStripMenuItem.Click += new System.EventHandler(this.toolsToolStripMenuItem_Click);
            // 
            // switchGameModeItem
            // 
            this.switchGameModeItem.Name = "switchGameModeItem";
            this.switchGameModeItem.Size = new System.Drawing.Size(185, 22);
            this.switchGameModeItem.Text = "Switch Game / Mode";
            this.switchGameModeItem.Click += new System.EventHandler(this.switchGameModeItem_Click);
            // 
            // inputDisplayItem
            // 
            this.inputDisplayItem.Name = "inputDisplayItem";
            this.inputDisplayItem.Size = new System.Drawing.Size(185, 22);
            this.inputDisplayItem.Text = "Input Display";
            this.inputDisplayItem.Click += new System.EventHandler(this.inputDisplayButton_Click);
            // 
            // memoryUtilitiesToolStripMenuItem
            // 
            this.memoryUtilitiesToolStripMenuItem.Name = "memoryUtilitiesToolStripMenuItem";
            this.memoryUtilitiesToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.memoryUtilitiesToolStripMenuItem.Text = "Memory Utilities...";
            this.memoryUtilitiesToolStripMenuItem.Click += new System.EventHandler(this.memoryUtilitiesToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(182, 6);
            // 
            // powerOffPS3Item
            // 
            this.powerOffPS3Item.Name = "powerOffPS3Item";
            this.powerOffPS3Item.Size = new System.Drawing.Size(185, 22);
            this.powerOffPS3Item.Text = "Power Off (PS3)";
            this.powerOffPS3Item.Click += new System.EventHandler(this.powerOffPS3Item_Click);
            // 
            // rebootPS3Item
            // 
            this.rebootPS3Item.Name = "rebootPS3Item";
            this.rebootPS3Item.Size = new System.Drawing.Size(185, 22);
            this.rebootPS3Item.Text = "Reboot (PS3)";
            this.rebootPS3Item.Click += new System.EventHandler(this.rebootPS3Item_Click);
            // 
            // inputDisplayButton
            // 
            this.inputDisplayButton.Location = new System.Drawing.Point(11, 36);
            this.inputDisplayButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.inputDisplayButton.Name = "inputDisplayButton";
            this.inputDisplayButton.Size = new System.Drawing.Size(91, 32);
            this.inputDisplayButton.TabIndex = 1;
            this.inputDisplayButton.Text = "Input Display";
            this.inputDisplayButton.UseVisualStyleBackColor = true;
            this.inputDisplayButton.Click += new System.EventHandler(this.inputDisplayButton_Click);
            // 
            // autosplitterCheckbox
            // 
            this.autosplitterCheckbox.AutoSize = true;
            this.autosplitterCheckbox.Location = new System.Drawing.Point(11, 80);
            this.autosplitterCheckbox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.autosplitterCheckbox.Name = "autosplitterCheckbox";
            this.autosplitterCheckbox.Size = new System.Drawing.Size(78, 17);
            this.autosplitterCheckbox.TabIndex = 2;
            this.autosplitterCheckbox.Text = "Autosplitter";
            this.autosplitterCheckbox.UseVisualStyleBackColor = true;
            this.autosplitterCheckbox.CheckedChanged += new System.EventHandler(this.autosplitterCheckbox_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.runFileComboBox);
            this.groupBox1.Controls.Add(this.loadRunFileButton);
            this.groupBox1.Location = new System.Drawing.Point(11, 134);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(219, 82);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Load run file";
            // 
            // runFileComboBox
            // 
            this.runFileComboBox.FormattingEnabled = true;
            this.runFileComboBox.Items.AddRange(new object[] {
            "Episode 1",
            "Episode 2",
            "Episode 3",
            "Episode 4",
            "Episode 5",
            "Episode 6",
            "Episode 7",
            "Episode 8"});
            this.runFileComboBox.Location = new System.Drawing.Point(13, 33);
            this.runFileComboBox.Name = "runFileComboBox";
            this.runFileComboBox.Size = new System.Drawing.Size(110, 21);
            this.runFileComboBox.TabIndex = 1;
            this.runFileComboBox.Text = "Select run file...";
            // 
            // loadRunFileButton
            // 
            this.loadRunFileButton.Location = new System.Drawing.Point(135, 25);
            this.loadRunFileButton.Name = "loadRunFileButton";
            this.loadRunFileButton.Size = new System.Drawing.Size(73, 35);
            this.loadRunFileButton.TabIndex = 0;
            this.loadRunFileButton.Text = "Load";
            this.loadRunFileButton.UseVisualStyleBackColor = true;
            this.loadRunFileButton.Click += new System.EventHandler(this.loadRunFileButton_Click);
            // 
            // alwaysOnTopCheckBox
            // 
            this.alwaysOnTopCheckBox.AutoSize = true;
            this.alwaysOnTopCheckBox.Location = new System.Drawing.Point(11, 102);
            this.alwaysOnTopCheckBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.alwaysOnTopCheckBox.Name = "alwaysOnTopCheckBox";
            this.alwaysOnTopCheckBox.Size = new System.Drawing.Size(98, 17);
            this.alwaysOnTopCheckBox.TabIndex = 4;
            this.alwaysOnTopCheckBox.Text = "Always On Top";
            this.alwaysOnTopCheckBox.UseVisualStyleBackColor = true;
            this.alwaysOnTopCheckBox.CheckedChanged += new System.EventHandler(this.alwaysOnTopCheckBox_CheckedChanged);
            // 
            // gadgetsButton
            // 
            this.gadgetsButton.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gadgetsButton.Location = new System.Drawing.Point(135, 36);
            this.gadgetsButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gadgetsButton.Name = "gadgetsButton";
            this.gadgetsButton.Size = new System.Drawing.Size(84, 32);
            this.gadgetsButton.TabIndex = 5;
            this.gadgetsButton.Text = "Gadgets";
            this.gadgetsButton.UseVisualStyleBackColor = false;
            this.gadgetsButton.Click += new System.EventHandler(this.gadgetsButton_Click);
            // 
            // SLY2Speedrun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(239, 226);
            this.Controls.Add(this.gadgetsButton);
            this.Controls.Add(this.alwaysOnTopCheckBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.autosplitterCheckbox);
            this.Controls.Add(this.inputDisplayButton);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "SLY2Speedrun";
            this.Text = "SluMAN :: Sly 2: Band of Thieves (Speedrun Mode)";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SLY2Speedrun_FormClosed);
            this.Load += new System.EventHandler(this.SLY2Speedrun_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Button inputDisplayButton;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.CheckBox autosplitterCheckbox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox alwaysOnTopCheckBox;
        private System.Windows.Forms.ComboBox runFileComboBox;
        private System.Windows.Forms.Button loadRunFileButton;
        private System.Windows.Forms.Button gadgetsButton;
        private System.Windows.Forms.ToolStripMenuItem switchGameModeItem;
        private System.Windows.Forms.ToolStripMenuItem inputDisplayItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem memoryUtilitiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem powerOffPS3Item;
        private System.Windows.Forms.ToolStripMenuItem rebootPS3Item;
    }
}