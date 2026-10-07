namespace racman
{
    partial class Sly1Speedrun
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sly1Speedrun));
            this.alwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
            this.autosplitterCheckbox = new System.Windows.Forms.CheckBox();
            this.inputDisplayButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchGameModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inputDisplayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryUtilitiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.powerOffPS3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rebootPS3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // alwaysOnTopCheckBox
            // 
            this.alwaysOnTopCheckBox.AutoSize = true;
            this.alwaysOnTopCheckBox.Location = new System.Drawing.Point(115, 60);
            this.alwaysOnTopCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.alwaysOnTopCheckBox.Name = "alwaysOnTopCheckBox";
            this.alwaysOnTopCheckBox.Size = new System.Drawing.Size(98, 17);
            this.alwaysOnTopCheckBox.TabIndex = 11;
            this.alwaysOnTopCheckBox.Text = "Always On Top";
            this.alwaysOnTopCheckBox.UseVisualStyleBackColor = true;
            this.alwaysOnTopCheckBox.CheckedChanged += new System.EventHandler(this.alwaysOnTopCheckBox_CheckedChanged);
            // 
            // autosplitterCheckbox
            // 
            this.autosplitterCheckbox.AutoSize = true;
            this.autosplitterCheckbox.Location = new System.Drawing.Point(115, 38);
            this.autosplitterCheckbox.Margin = new System.Windows.Forms.Padding(2);
            this.autosplitterCheckbox.Name = "autosplitterCheckbox";
            this.autosplitterCheckbox.Size = new System.Drawing.Size(78, 17);
            this.autosplitterCheckbox.TabIndex = 10;
            this.autosplitterCheckbox.Text = "Autosplitter";
            this.autosplitterCheckbox.UseVisualStyleBackColor = true;
            this.autosplitterCheckbox.CheckedChanged += new System.EventHandler(this.autosplitterCheckbox_CheckedChanged);
            // 
            // inputDisplayButton
            // 
            this.inputDisplayButton.Location = new System.Drawing.Point(11, 38);
            this.inputDisplayButton.Margin = new System.Windows.Forms.Padding(2);
            this.inputDisplayButton.Name = "inputDisplayButton";
            this.inputDisplayButton.Size = new System.Drawing.Size(91, 39);
            this.inputDisplayButton.TabIndex = 9;
            this.inputDisplayButton.Text = "Input Display";
            this.inputDisplayButton.UseVisualStyleBackColor = true;
            this.inputDisplayButton.Click += new System.EventHandler(this.inputDisplayButton_Click);
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
            this.menuStrip1.Size = new System.Drawing.Size(228, 24);
            this.menuStrip1.TabIndex = 12;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.switchGameModeToolStripMenuItem,
            this.inputDisplayToolStripMenuItem,
            this.memoryUtilitiesToolStripMenuItem,
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
            this.switchGameModeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.switchGameModeToolStripMenuItem.Text = "Switch Game/Mode";
            this.switchGameModeToolStripMenuItem.Click += new System.EventHandler(this.switchGameModeToolStripMenuItem_Click);
            // 
            // inputDisplayToolStripMenuItem
            // 
            this.inputDisplayToolStripMenuItem.Name = "inputDisplayToolStripMenuItem";
            this.inputDisplayToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.inputDisplayToolStripMenuItem.Text = "Input Display";
            this.inputDisplayToolStripMenuItem.Click += new System.EventHandler(this.inputDisplayToolStripMenuItem_Click);
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
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // powerOffPS3ToolStripMenuItem
            // 
            this.powerOffPS3ToolStripMenuItem.Name = "powerOffPS3ToolStripMenuItem";
            this.powerOffPS3ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.powerOffPS3ToolStripMenuItem.Text = "Power Off (PS3)";
            this.powerOffPS3ToolStripMenuItem.Click += new System.EventHandler(this.powerOffPS3ToolStripMenuItem_Click);
            // 
            // rebootPS3ToolStripMenuItem
            // 
            this.rebootPS3ToolStripMenuItem.Name = "rebootPS3ToolStripMenuItem";
            this.rebootPS3ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.rebootPS3ToolStripMenuItem.Text = "Reboot (PS3)";
            this.rebootPS3ToolStripMenuItem.Click += new System.EventHandler(this.rebootPS3ToolStripMenuItem_Click);
            // 
            // Sly1Speedrun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(228, 97);
            this.Controls.Add(this.alwaysOnTopCheckBox);
            this.Controls.Add(this.autosplitterCheckbox);
            this.Controls.Add(this.inputDisplayButton);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Sly1Speedrun";
            this.Text = "SluMAN :: Sly Cooper and the Thievius Raccoonus (Speedrun Mode)";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Sly1Speedrun_FormClosed);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox alwaysOnTopCheckBox;
        private System.Windows.Forms.CheckBox autosplitterCheckbox;
        private System.Windows.Forms.Button inputDisplayButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchGameModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inputDisplayToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem memoryUtilitiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem powerOffPS3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rebootPS3ToolStripMenuItem;
    }
}