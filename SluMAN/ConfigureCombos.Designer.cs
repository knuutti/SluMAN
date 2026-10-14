
namespace SluMAN
{
    partial class ConfigureCombos
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureCombos));
            this.infoText = new System.Windows.Forms.Label();
            this.savePositionLabel = new System.Windows.Forms.Label();
            this.savePositionTextBox = new System.Windows.Forms.TextBox();
            this.savePositionClearButton = new System.Windows.Forms.Button();
            this.loadPositionLabel = new System.Windows.Forms.Label();
            this.loadPositionTextBox = new System.Windows.Forms.TextBox();
            this.loadPositionClearButton = new System.Windows.Forms.Button();
            this.loadGameLabel = new System.Windows.Forms.Label();
            this.loadGameTextBox = new System.Windows.Forms.TextBox();
            this.loadGameClearButton = new System.Windows.Forms.Button();
            this.runScriptLabel = new System.Windows.Forms.Label();
            this.runScriptTextBox = new System.Windows.Forms.TextBox();
            this.runScriptClearButton = new System.Windows.Forms.Button();
            this.combosEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // infoText
            //
            this.infoText.Location = new System.Drawing.Point(12, 9);
            this.infoText.Name = "infoText";
            this.infoText.Size = new System.Drawing.Size(300, 42);
            this.infoText.TabIndex = 0;
            this.infoText.Text = "Click a box, then press the buttons on your controller. The combo is saved when y" +
    "ou let go. Combos only work in Practice Mode.";
            //
            // savePositionLabel
            //
            this.savePositionLabel.AutoSize = true;
            this.savePositionLabel.Location = new System.Drawing.Point(12, 62);
            this.savePositionLabel.Name = "savePositionLabel";
            this.savePositionLabel.Size = new System.Drawing.Size(75, 13);
            this.savePositionLabel.TabIndex = 1;
            this.savePositionLabel.Text = "Save Position:";
            //
            // savePositionTextBox
            //
            this.savePositionTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.savePositionTextBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.savePositionTextBox.Location = new System.Drawing.Point(110, 59);
            this.savePositionTextBox.Name = "savePositionTextBox";
            this.savePositionTextBox.ReadOnly = true;
            this.savePositionTextBox.Size = new System.Drawing.Size(140, 20);
            this.savePositionTextBox.TabIndex = 2;
            this.savePositionTextBox.Click += new System.EventHandler(this.comboTextBox_Click);
            //
            // savePositionClearButton
            //
            this.savePositionClearButton.Location = new System.Drawing.Point(256, 57);
            this.savePositionClearButton.Name = "savePositionClearButton";
            this.savePositionClearButton.Size = new System.Drawing.Size(56, 23);
            this.savePositionClearButton.TabIndex = 3;
            this.savePositionClearButton.Text = "Clear";
            this.savePositionClearButton.UseVisualStyleBackColor = true;
            this.savePositionClearButton.Click += new System.EventHandler(this.clearButton_Click);
            //
            // loadPositionLabel
            //
            this.loadPositionLabel.AutoSize = true;
            this.loadPositionLabel.Location = new System.Drawing.Point(12, 92);
            this.loadPositionLabel.Name = "loadPositionLabel";
            this.loadPositionLabel.Size = new System.Drawing.Size(74, 13);
            this.loadPositionLabel.TabIndex = 4;
            this.loadPositionLabel.Text = "Load Position:";
            //
            // loadPositionTextBox
            //
            this.loadPositionTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.loadPositionTextBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.loadPositionTextBox.Location = new System.Drawing.Point(110, 89);
            this.loadPositionTextBox.Name = "loadPositionTextBox";
            this.loadPositionTextBox.ReadOnly = true;
            this.loadPositionTextBox.Size = new System.Drawing.Size(140, 20);
            this.loadPositionTextBox.TabIndex = 5;
            this.loadPositionTextBox.Click += new System.EventHandler(this.comboTextBox_Click);
            //
            // loadPositionClearButton
            //
            this.loadPositionClearButton.Location = new System.Drawing.Point(256, 87);
            this.loadPositionClearButton.Name = "loadPositionClearButton";
            this.loadPositionClearButton.Size = new System.Drawing.Size(56, 23);
            this.loadPositionClearButton.TabIndex = 6;
            this.loadPositionClearButton.Text = "Clear";
            this.loadPositionClearButton.UseVisualStyleBackColor = true;
            this.loadPositionClearButton.Click += new System.EventHandler(this.clearButton_Click);
            //
            // loadGameLabel
            //
            this.loadGameLabel.AutoSize = true;
            this.loadGameLabel.Location = new System.Drawing.Point(12, 122);
            this.loadGameLabel.Name = "loadGameLabel";
            this.loadGameLabel.Size = new System.Drawing.Size(63, 13);
            this.loadGameLabel.TabIndex = 7;
            this.loadGameLabel.Text = "Load Game:";
            //
            // loadGameTextBox
            //
            this.loadGameTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.loadGameTextBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.loadGameTextBox.Location = new System.Drawing.Point(110, 119);
            this.loadGameTextBox.Name = "loadGameTextBox";
            this.loadGameTextBox.ReadOnly = true;
            this.loadGameTextBox.Size = new System.Drawing.Size(140, 20);
            this.loadGameTextBox.TabIndex = 8;
            this.loadGameTextBox.Click += new System.EventHandler(this.comboTextBox_Click);
            //
            // loadGameClearButton
            //
            this.loadGameClearButton.Location = new System.Drawing.Point(256, 117);
            this.loadGameClearButton.Name = "loadGameClearButton";
            this.loadGameClearButton.Size = new System.Drawing.Size(56, 23);
            this.loadGameClearButton.TabIndex = 9;
            this.loadGameClearButton.Text = "Clear";
            this.loadGameClearButton.UseVisualStyleBackColor = true;
            this.loadGameClearButton.Click += new System.EventHandler(this.clearButton_Click);
            //
            // runScriptLabel
            //
            this.runScriptLabel.AutoSize = true;
            this.runScriptLabel.Location = new System.Drawing.Point(12, 152);
            this.runScriptLabel.Name = "runScriptLabel";
            this.runScriptLabel.Size = new System.Drawing.Size(61, 13);
            this.runScriptLabel.TabIndex = 10;
            this.runScriptLabel.Text = "Run Script:";
            //
            // runScriptTextBox
            //
            this.runScriptTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.runScriptTextBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.runScriptTextBox.Location = new System.Drawing.Point(110, 149);
            this.runScriptTextBox.Name = "runScriptTextBox";
            this.runScriptTextBox.ReadOnly = true;
            this.runScriptTextBox.Size = new System.Drawing.Size(140, 20);
            this.runScriptTextBox.TabIndex = 11;
            this.runScriptTextBox.Click += new System.EventHandler(this.comboTextBox_Click);
            //
            // runScriptClearButton
            //
            this.runScriptClearButton.Location = new System.Drawing.Point(256, 147);
            this.runScriptClearButton.Name = "runScriptClearButton";
            this.runScriptClearButton.Size = new System.Drawing.Size(56, 23);
            this.runScriptClearButton.TabIndex = 12;
            this.runScriptClearButton.Text = "Clear";
            this.runScriptClearButton.UseVisualStyleBackColor = true;
            this.runScriptClearButton.Click += new System.EventHandler(this.clearButton_Click);
            //
            // combosEnabledCheckBox
            //
            this.combosEnabledCheckBox.AutoSize = true;
            this.combosEnabledCheckBox.Location = new System.Drawing.Point(15, 184);
            this.combosEnabledCheckBox.Name = "combosEnabledCheckBox";
            this.combosEnabledCheckBox.Size = new System.Drawing.Size(103, 17);
            this.combosEnabledCheckBox.TabIndex = 13;
            this.combosEnabledCheckBox.Text = "Combos enabled";
            this.combosEnabledCheckBox.UseVisualStyleBackColor = true;
            this.combosEnabledCheckBox.CheckedChanged += new System.EventHandler(this.combosEnabledCheckBox_CheckedChanged);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 210);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(300, 30);
            this.statusLabel.TabIndex = 14;
            //
            // closeButton
            //
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(237, 245);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 15;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            //
            // ConfigureCombos
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(324, 280);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.combosEnabledCheckBox);
            this.Controls.Add(this.runScriptClearButton);
            this.Controls.Add(this.runScriptTextBox);
            this.Controls.Add(this.runScriptLabel);
            this.Controls.Add(this.loadGameClearButton);
            this.Controls.Add(this.loadGameTextBox);
            this.Controls.Add(this.loadGameLabel);
            this.Controls.Add(this.loadPositionClearButton);
            this.Controls.Add(this.loadPositionTextBox);
            this.Controls.Add(this.loadPositionLabel);
            this.Controls.Add(this.savePositionClearButton);
            this.Controls.Add(this.savePositionTextBox);
            this.Controls.Add(this.savePositionLabel);
            this.Controls.Add(this.infoText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigureCombos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Controller Combos";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigureCombos_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label infoText;
        private System.Windows.Forms.Label savePositionLabel;
        private System.Windows.Forms.TextBox savePositionTextBox;
        private System.Windows.Forms.Button savePositionClearButton;
        private System.Windows.Forms.Label loadPositionLabel;
        private System.Windows.Forms.TextBox loadPositionTextBox;
        private System.Windows.Forms.Button loadPositionClearButton;
        private System.Windows.Forms.Label loadGameLabel;
        private System.Windows.Forms.TextBox loadGameTextBox;
        private System.Windows.Forms.Button loadGameClearButton;
        private System.Windows.Forms.Label runScriptLabel;
        private System.Windows.Forms.TextBox runScriptTextBox;
        private System.Windows.Forms.Button runScriptClearButton;
        private System.Windows.Forms.CheckBox combosEnabledCheckBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button closeButton;
    }
}
