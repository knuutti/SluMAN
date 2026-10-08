
namespace SluMAN
{
    partial class AttachPS3Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AttachPS3Form));
            this.IPTextBox = new System.Windows.Forms.TextBox();
            this.attachButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.currentVerLabel = new System.Windows.Forms.Label();
            this.attachRestrictedButton = new System.Windows.Forms.Button();
            this.rpcs3CheckBox = new System.Windows.Forms.CheckBox();
            this.updateLinkLabel = new System.Windows.Forms.LinkLabel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // IPTextBox
            // 
            this.IPTextBox.Location = new System.Drawing.Point(19, 35);
            this.IPTextBox.Name = "IPTextBox";
            this.IPTextBox.Size = new System.Drawing.Size(150, 20);
            this.IPTextBox.TabIndex = 0;
            this.IPTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.IPTextBox_KeyDown);
            // 
            // attachButton
            // 
            this.attachButton.Location = new System.Drawing.Point(19, 110);
            this.attachButton.Name = "attachButton";
            this.attachButton.Size = new System.Drawing.Size(149, 30);
            this.attachButton.TabIndex = 1;
            this.attachButton.Text = "Attach (Practice Mode)";
            this.attachButton.UseVisualStyleBackColor = true;
            this.attachButton.Click += new System.EventHandler(this.attachButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "IP Address:";
            // 
            // currentVerLabel
            // 
            this.currentVerLabel.AutoSize = true;
            this.currentVerLabel.Location = new System.Drawing.Point(16, 199);
            this.currentVerLabel.Name = "currentVerLabel";
            this.currentVerLabel.Size = new System.Drawing.Size(79, 13);
            this.currentVerLabel.TabIndex = 3;
            this.currentVerLabel.Text = "SluMAN v0.0.0";
            this.currentVerLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.toolTip.SetToolTip(this.currentVerLabel, "Click to check for updates");
            this.currentVerLabel.Click += new System.EventHandler(this.currentVerLabel_Click);
            // 
            // updateLinkLabel
            // 
            this.updateLinkLabel.AutoSize = true;
            this.updateLinkLabel.Location = new System.Drawing.Point(98, 199);
            this.updateLinkLabel.Name = "updateLinkLabel";
            this.updateLinkLabel.Size = new System.Drawing.Size(88, 13);
            this.updateLinkLabel.TabIndex = 6;
            this.updateLinkLabel.TabStop = true;
            this.updateLinkLabel.Text = "Update available";
            this.updateLinkLabel.Visible = false;
            this.updateLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.updateLinkLabel_LinkClicked);
            // 
            // attachRestrictedButton
            // 
            this.attachRestrictedButton.Location = new System.Drawing.Point(19, 74);
            this.attachRestrictedButton.Name = "attachRestrictedButton";
            this.attachRestrictedButton.Size = new System.Drawing.Size(149, 30);
            this.attachRestrictedButton.TabIndex = 5;
            this.attachRestrictedButton.Text = "Attach (Speedrun Mode)";
            this.attachRestrictedButton.UseVisualStyleBackColor = true;
            this.attachRestrictedButton.Click += new System.EventHandler(this.attachPS3SpeedrunModeButton_Click);
            // 
            // rpcs3CheckBox
            // 
            this.rpcs3CheckBox.AutoSize = true;
            this.rpcs3CheckBox.Location = new System.Drawing.Point(19, 154);
            this.rpcs3CheckBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rpcs3CheckBox.Name = "rpcs3CheckBox";
            this.rpcs3CheckBox.Size = new System.Drawing.Size(61, 17);
            this.rpcs3CheckBox.TabIndex = 6;
            this.rpcs3CheckBox.Text = "RPCS3";
            this.rpcs3CheckBox.UseVisualStyleBackColor = true;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Location = new System.Drawing.Point(16, 176);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(162, 13);
            this.statusLabel.TabIndex = 7;
            // 
            // AttachPS3Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(194, 230);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.updateLinkLabel);
            this.Controls.Add(this.rpcs3CheckBox);
            this.Controls.Add(this.attachRestrictedButton);
            this.Controls.Add(this.currentVerLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.attachButton);
            this.Controls.Add(this.IPTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "AttachPS3Form";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SluMAN :: Attach to game";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AttachPS3Form_FormClosing);
            this.Load += new System.EventHandler(this.AttachPS3Form_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox IPTextBox;
        private System.Windows.Forms.Button attachButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label currentVerLabel;
        private System.Windows.Forms.LinkLabel updateLinkLabel;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button attachRestrictedButton;
        private System.Windows.Forms.CheckBox rpcs3CheckBox;
        private System.Windows.Forms.Label statusLabel;
    }
}