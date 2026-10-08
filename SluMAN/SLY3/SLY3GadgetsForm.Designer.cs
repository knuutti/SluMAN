namespace SluMAN
{
    partial class SLY3GadgetsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SLY3GadgetsForm));
            this.slyGadgetsCheckedList = new System.Windows.Forms.CheckedListBox();
            this.murrayGadgetsCheckedList = new System.Windows.Forms.CheckedListBox();
            this.bentleyGadgetsCheckedList = new System.Windows.Forms.CheckedListBox();
            this.spinAttackLevelSelector = new System.Windows.Forms.NumericUpDown();
            this.slyGadgetsLabel = new System.Windows.Forms.Label();
            this.spinAttackLevelLabel = new System.Windows.Forms.Label();
            this.pushAttackLevelLabel = new System.Windows.Forms.Label();
            this.pushAttackLevelSelector = new System.Windows.Forms.NumericUpDown();
            this.jumpAttackLevelLabel = new System.Windows.Forms.Label();
            this.jumpAttackLevelSelector = new System.Windows.Forms.NumericUpDown();
            this.bentleyGadgetsLabel = new System.Windows.Forms.Label();
            this.murrayGadgetsLabel = new System.Windows.Forms.Label();
            this.slyGadgetsToggleCheckBox = new System.Windows.Forms.CheckBox();
            this.bentleyGadgetsToggleCheckBox = new System.Windows.Forms.CheckBox();
            this.murrayGadgetsToggleCheckBox = new System.Windows.Forms.CheckBox();
            this.slyGadgetsL1Label = new System.Windows.Forms.Label();
            this.slyGadgetsL1ComboBox = new System.Windows.Forms.ComboBox();
            this.slyGadgetsL2ComboBox = new System.Windows.Forms.ComboBox();
            this.slyGadgetsL2Label = new System.Windows.Forms.Label();
            this.slyGadgetsR2ComboBox = new System.Windows.Forms.ComboBox();
            this.slyGadgetsR2Label = new System.Windows.Forms.Label();
            this.bentleyGadgetsR2ComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bentleyGadgetsL2ComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bentleyGadgetsL1ComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.murrayGadgetsR2ComboBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.murrayGadgetsL2ComboBox = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.murrayGadgetsL1ComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.saveAndReloadButton = new System.Windows.Forms.Button();
            this.discardChangesButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.loadRunFileGadgetsButton = new System.Windows.Forms.Button();
            this.saveRunFileGadgetsButton = new System.Windows.Forms.Button();
            this.runFileComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.spinAttackLevelSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pushAttackLevelSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.jumpAttackLevelSelector)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // slyGadgetsCheckedList
            // 
            this.slyGadgetsCheckedList.BackColor = System.Drawing.Color.White;
            this.slyGadgetsCheckedList.FormattingEnabled = true;
            this.slyGadgetsCheckedList.Items.AddRange(new object[] {
            "Knockout Dive",
            "Feral Pounce",
            "Silent Obliteration",
            "Paraglider",
            "Combat Dodge",
            "Smoke Bomb",
            "Venice Disguise",
            "Photographer Disguise",
            "Pirate Disguise",
            "Treasure Map",
            "Rocket Boots",
            "Shadow Power",
            "Shadow Power 2",
            "Thief Reflexes"});
            this.slyGadgetsCheckedList.Location = new System.Drawing.Point(12, 22);
            this.slyGadgetsCheckedList.Name = "slyGadgetsCheckedList";
            this.slyGadgetsCheckedList.Size = new System.Drawing.Size(152, 214);
            this.slyGadgetsCheckedList.TabIndex = 0;
            this.slyGadgetsCheckedList.SelectedIndexChanged += new System.EventHandler(this.slyGadgetsCheckedList_SelectedIndexChanged);
            // 
            // murrayGadgetsCheckedList
            // 
            this.murrayGadgetsCheckedList.BackColor = System.Drawing.Color.White;
            this.murrayGadgetsCheckedList.FormattingEnabled = true;
            this.murrayGadgetsCheckedList.Items.AddRange(new object[] {
            "Be The Ball",
            "Raging Inferno Flop",
            "Temporal Lock",
            "Fists of Flame",
            "Guttural Roar",
            "Juggernaut Throw",
            "Berserker Charge",
            "Diablo Fire Slam"});
            this.murrayGadgetsCheckedList.Location = new System.Drawing.Point(328, 22);
            this.murrayGadgetsCheckedList.Name = "murrayGadgetsCheckedList";
            this.murrayGadgetsCheckedList.Size = new System.Drawing.Size(152, 214);
            this.murrayGadgetsCheckedList.TabIndex = 1;
            // 
            // bentleyGadgetsCheckedList
            // 
            this.bentleyGadgetsCheckedList.BackColor = System.Drawing.Color.White;
            this.bentleyGadgetsCheckedList.FormattingEnabled = true;
            this.bentleyGadgetsCheckedList.Items.AddRange(new object[] {
            "Fishing Pole",
            "Trigger Bomb",
            "Rage Bomb",
            "Size Destabilizer",
            "Reduction Bomb",
            "Grapple-Cam",
            "Insanity Strike",
            "Hover Pack",
            "Health Extractor",
            "Adrenaline Burst",
            "Alarm Clock"});
            this.bentleyGadgetsCheckedList.Location = new System.Drawing.Point(170, 22);
            this.bentleyGadgetsCheckedList.Name = "bentleyGadgetsCheckedList";
            this.bentleyGadgetsCheckedList.Size = new System.Drawing.Size(152, 214);
            this.bentleyGadgetsCheckedList.TabIndex = 2;
            // 
            // spinAttackLevelSelector
            // 
            this.spinAttackLevelSelector.Location = new System.Drawing.Point(112, 388);
            this.spinAttackLevelSelector.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.spinAttackLevelSelector.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.spinAttackLevelSelector.Name = "spinAttackLevelSelector";
            this.spinAttackLevelSelector.Size = new System.Drawing.Size(52, 20);
            this.spinAttackLevelSelector.TabIndex = 3;
            this.spinAttackLevelSelector.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.spinAttackLevelSelector.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // slyGadgetsLabel
            // 
            this.slyGadgetsLabel.AutoSize = true;
            this.slyGadgetsLabel.Location = new System.Drawing.Point(12, 6);
            this.slyGadgetsLabel.Name = "slyGadgetsLabel";
            this.slyGadgetsLabel.Size = new System.Drawing.Size(24, 13);
            this.slyGadgetsLabel.TabIndex = 4;
            this.slyGadgetsLabel.Text = "Sly:";
            // 
            // spinAttackLevelLabel
            // 
            this.spinAttackLevelLabel.AutoSize = true;
            this.spinAttackLevelLabel.Location = new System.Drawing.Point(12, 390);
            this.spinAttackLevelLabel.Name = "spinAttackLevelLabel";
            this.spinAttackLevelLabel.Size = new System.Drawing.Size(94, 13);
            this.spinAttackLevelLabel.TabIndex = 5;
            this.spinAttackLevelLabel.Text = "Spin Attack Level:";
            // 
            // pushAttackLevelLabel
            // 
            this.pushAttackLevelLabel.AutoSize = true;
            this.pushAttackLevelLabel.Location = new System.Drawing.Point(12, 416);
            this.pushAttackLevelLabel.Name = "pushAttackLevelLabel";
            this.pushAttackLevelLabel.Size = new System.Drawing.Size(97, 13);
            this.pushAttackLevelLabel.TabIndex = 6;
            this.pushAttackLevelLabel.Text = "Push Attack Level:";
            // 
            // pushAttackLevelSelector
            // 
            this.pushAttackLevelSelector.Location = new System.Drawing.Point(112, 414);
            this.pushAttackLevelSelector.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.pushAttackLevelSelector.Name = "pushAttackLevelSelector";
            this.pushAttackLevelSelector.Size = new System.Drawing.Size(52, 20);
            this.pushAttackLevelSelector.TabIndex = 7;
            this.pushAttackLevelSelector.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // jumpAttackLevelLabel
            // 
            this.jumpAttackLevelLabel.AutoSize = true;
            this.jumpAttackLevelLabel.Location = new System.Drawing.Point(12, 442);
            this.jumpAttackLevelLabel.Name = "jumpAttackLevelLabel";
            this.jumpAttackLevelLabel.Size = new System.Drawing.Size(98, 13);
            this.jumpAttackLevelLabel.TabIndex = 8;
            this.jumpAttackLevelLabel.Text = "Jump Attack Level:";
            // 
            // jumpAttackLevelSelector
            // 
            this.jumpAttackLevelSelector.Location = new System.Drawing.Point(112, 440);
            this.jumpAttackLevelSelector.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.jumpAttackLevelSelector.Name = "jumpAttackLevelSelector";
            this.jumpAttackLevelSelector.Size = new System.Drawing.Size(52, 20);
            this.jumpAttackLevelSelector.TabIndex = 9;
            this.jumpAttackLevelSelector.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // bentleyGadgetsLabel
            // 
            this.bentleyGadgetsLabel.AutoSize = true;
            this.bentleyGadgetsLabel.Location = new System.Drawing.Point(167, 6);
            this.bentleyGadgetsLabel.Name = "bentleyGadgetsLabel";
            this.bentleyGadgetsLabel.Size = new System.Drawing.Size(45, 13);
            this.bentleyGadgetsLabel.TabIndex = 10;
            this.bentleyGadgetsLabel.Text = "Bentley:";
            // 
            // murrayGadgetsLabel
            // 
            this.murrayGadgetsLabel.AutoSize = true;
            this.murrayGadgetsLabel.Location = new System.Drawing.Point(325, 6);
            this.murrayGadgetsLabel.Name = "murrayGadgetsLabel";
            this.murrayGadgetsLabel.Size = new System.Drawing.Size(42, 13);
            this.murrayGadgetsLabel.TabIndex = 11;
            this.murrayGadgetsLabel.Text = "Murray:";
            // 
            // slyGadgetsToggleCheckBox
            // 
            this.slyGadgetsToggleCheckBox.AutoSize = true;
            this.slyGadgetsToggleCheckBox.Location = new System.Drawing.Point(15, 258);
            this.slyGadgetsToggleCheckBox.Name = "slyGadgetsToggleCheckBox";
            this.slyGadgetsToggleCheckBox.Size = new System.Drawing.Size(126, 17);
            this.slyGadgetsToggleCheckBox.TabIndex = 12;
            this.slyGadgetsToggleCheckBox.Text = "Check / Uncheck All";
            this.slyGadgetsToggleCheckBox.UseVisualStyleBackColor = true;
            this.slyGadgetsToggleCheckBox.CheckedChanged += new System.EventHandler(this.slyGadgetsToggleCheckBox_CheckedChanged);
            // 
            // bentleyGadgetsToggleCheckBox
            // 
            this.bentleyGadgetsToggleCheckBox.AutoSize = true;
            this.bentleyGadgetsToggleCheckBox.Location = new System.Drawing.Point(170, 258);
            this.bentleyGadgetsToggleCheckBox.Name = "bentleyGadgetsToggleCheckBox";
            this.bentleyGadgetsToggleCheckBox.Size = new System.Drawing.Size(126, 17);
            this.bentleyGadgetsToggleCheckBox.TabIndex = 13;
            this.bentleyGadgetsToggleCheckBox.Text = "Check / Uncheck All";
            this.bentleyGadgetsToggleCheckBox.UseVisualStyleBackColor = true;
            this.bentleyGadgetsToggleCheckBox.CheckedChanged += new System.EventHandler(this.bentleyGadgetsToggleCheckBox_CheckedChanged);
            // 
            // murrayGadgetsToggleCheckBox
            // 
            this.murrayGadgetsToggleCheckBox.AutoSize = true;
            this.murrayGadgetsToggleCheckBox.Location = new System.Drawing.Point(328, 258);
            this.murrayGadgetsToggleCheckBox.Name = "murrayGadgetsToggleCheckBox";
            this.murrayGadgetsToggleCheckBox.Size = new System.Drawing.Size(126, 17);
            this.murrayGadgetsToggleCheckBox.TabIndex = 14;
            this.murrayGadgetsToggleCheckBox.Text = "Check / Uncheck All";
            this.murrayGadgetsToggleCheckBox.UseVisualStyleBackColor = true;
            this.murrayGadgetsToggleCheckBox.CheckedChanged += new System.EventHandler(this.murrayGadgetsToggleCheckBox_CheckedChanged);
            // 
            // slyGadgetsL1Label
            // 
            this.slyGadgetsL1Label.AutoSize = true;
            this.slyGadgetsL1Label.Location = new System.Drawing.Point(15, 290);
            this.slyGadgetsL1Label.Name = "slyGadgetsL1Label";
            this.slyGadgetsL1Label.Size = new System.Drawing.Size(22, 13);
            this.slyGadgetsL1Label.TabIndex = 15;
            this.slyGadgetsL1Label.Text = "L1:";
            // 
            // slyGadgetsL1ComboBox
            // 
            this.slyGadgetsL1ComboBox.FormattingEnabled = true;
            this.slyGadgetsL1ComboBox.Location = new System.Drawing.Point(43, 287);
            this.slyGadgetsL1ComboBox.Name = "slyGadgetsL1ComboBox";
            this.slyGadgetsL1ComboBox.Size = new System.Drawing.Size(121, 21);
            this.slyGadgetsL1ComboBox.TabIndex = 16;
            // 
            // slyGadgetsL2ComboBox
            // 
            this.slyGadgetsL2ComboBox.FormattingEnabled = true;
            this.slyGadgetsL2ComboBox.Location = new System.Drawing.Point(43, 314);
            this.slyGadgetsL2ComboBox.Name = "slyGadgetsL2ComboBox";
            this.slyGadgetsL2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.slyGadgetsL2ComboBox.TabIndex = 18;
            // 
            // slyGadgetsL2Label
            // 
            this.slyGadgetsL2Label.AutoSize = true;
            this.slyGadgetsL2Label.Location = new System.Drawing.Point(15, 317);
            this.slyGadgetsL2Label.Name = "slyGadgetsL2Label";
            this.slyGadgetsL2Label.Size = new System.Drawing.Size(22, 13);
            this.slyGadgetsL2Label.TabIndex = 17;
            this.slyGadgetsL2Label.Text = "L2:";
            // 
            // slyGadgetsR2ComboBox
            // 
            this.slyGadgetsR2ComboBox.FormattingEnabled = true;
            this.slyGadgetsR2ComboBox.Location = new System.Drawing.Point(43, 341);
            this.slyGadgetsR2ComboBox.Name = "slyGadgetsR2ComboBox";
            this.slyGadgetsR2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.slyGadgetsR2ComboBox.TabIndex = 20;
            // 
            // slyGadgetsR2Label
            // 
            this.slyGadgetsR2Label.AutoSize = true;
            this.slyGadgetsR2Label.Location = new System.Drawing.Point(15, 344);
            this.slyGadgetsR2Label.Name = "slyGadgetsR2Label";
            this.slyGadgetsR2Label.Size = new System.Drawing.Size(24, 13);
            this.slyGadgetsR2Label.TabIndex = 19;
            this.slyGadgetsR2Label.Text = "R2:";
            // 
            // bentleyGadgetsR2ComboBox
            // 
            this.bentleyGadgetsR2ComboBox.FormattingEnabled = true;
            this.bentleyGadgetsR2ComboBox.Location = new System.Drawing.Point(201, 341);
            this.bentleyGadgetsR2ComboBox.Name = "bentleyGadgetsR2ComboBox";
            this.bentleyGadgetsR2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.bentleyGadgetsR2ComboBox.TabIndex = 26;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(173, 344);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "R2:";
            // 
            // bentleyGadgetsL2ComboBox
            // 
            this.bentleyGadgetsL2ComboBox.FormattingEnabled = true;
            this.bentleyGadgetsL2ComboBox.Location = new System.Drawing.Point(201, 314);
            this.bentleyGadgetsL2ComboBox.Name = "bentleyGadgetsL2ComboBox";
            this.bentleyGadgetsL2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.bentleyGadgetsL2ComboBox.TabIndex = 24;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(173, 317);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(22, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "L2:";
            // 
            // bentleyGadgetsL1ComboBox
            // 
            this.bentleyGadgetsL1ComboBox.FormattingEnabled = true;
            this.bentleyGadgetsL1ComboBox.Location = new System.Drawing.Point(201, 287);
            this.bentleyGadgetsL1ComboBox.Name = "bentleyGadgetsL1ComboBox";
            this.bentleyGadgetsL1ComboBox.Size = new System.Drawing.Size(121, 21);
            this.bentleyGadgetsL1ComboBox.TabIndex = 22;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(173, 290);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(22, 13);
            this.label3.TabIndex = 21;
            this.label3.Text = "L1:";
            // 
            // murrayGadgetsR2ComboBox
            // 
            this.murrayGadgetsR2ComboBox.FormattingEnabled = true;
            this.murrayGadgetsR2ComboBox.Location = new System.Drawing.Point(359, 341);
            this.murrayGadgetsR2ComboBox.Name = "murrayGadgetsR2ComboBox";
            this.murrayGadgetsR2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.murrayGadgetsR2ComboBox.TabIndex = 32;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(331, 344);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(24, 13);
            this.label4.TabIndex = 31;
            this.label4.Text = "R2:";
            // 
            // murrayGadgetsL2ComboBox
            // 
            this.murrayGadgetsL2ComboBox.FormattingEnabled = true;
            this.murrayGadgetsL2ComboBox.Location = new System.Drawing.Point(359, 314);
            this.murrayGadgetsL2ComboBox.Name = "murrayGadgetsL2ComboBox";
            this.murrayGadgetsL2ComboBox.Size = new System.Drawing.Size(121, 21);
            this.murrayGadgetsL2ComboBox.TabIndex = 30;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(331, 317);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(22, 13);
            this.label5.TabIndex = 29;
            this.label5.Text = "L2:";
            // 
            // murrayGadgetsL1ComboBox
            // 
            this.murrayGadgetsL1ComboBox.FormattingEnabled = true;
            this.murrayGadgetsL1ComboBox.Location = new System.Drawing.Point(359, 287);
            this.murrayGadgetsL1ComboBox.Name = "murrayGadgetsL1ComboBox";
            this.murrayGadgetsL1ComboBox.Size = new System.Drawing.Size(121, 21);
            this.murrayGadgetsL1ComboBox.TabIndex = 28;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(331, 290);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(22, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "L1:";
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(359, 414);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(121, 23);
            this.saveButton.TabIndex = 33;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // saveAndReloadButton
            // 
            this.saveAndReloadButton.Location = new System.Drawing.Point(359, 442);
            this.saveAndReloadButton.Name = "saveAndReloadButton";
            this.saveAndReloadButton.Size = new System.Drawing.Size(121, 23);
            this.saveAndReloadButton.TabIndex = 34;
            this.saveAndReloadButton.Text = "Save and Reload";
            this.saveAndReloadButton.UseVisualStyleBackColor = true;
            this.saveAndReloadButton.Click += new System.EventHandler(this.saveAndReloadButton_Click);
            // 
            // discardChangesButton
            // 
            this.discardChangesButton.BackColor = System.Drawing.Color.White;
            this.discardChangesButton.Location = new System.Drawing.Point(359, 387);
            this.discardChangesButton.Name = "discardChangesButton";
            this.discardChangesButton.Size = new System.Drawing.Size(121, 23);
            this.discardChangesButton.TabIndex = 35;
            this.discardChangesButton.Text = "Discard Changes";
            this.discardChangesButton.UseVisualStyleBackColor = false;
            this.discardChangesButton.Click += new System.EventHandler(this.discardChangesButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.loadRunFileGadgetsButton);
            this.groupBox1.Controls.Add(this.saveRunFileGadgetsButton);
            this.groupBox1.Controls.Add(this.runFileComboBox);
            this.groupBox1.Location = new System.Drawing.Point(201, 371);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Size = new System.Drawing.Size(146, 94);
            this.groupBox1.TabIndex = 36;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Run file gadgets";
            // 
            // loadRunFileGadgetsButton
            // 
            this.loadRunFileGadgetsButton.Location = new System.Drawing.Point(6, 43);
            this.loadRunFileGadgetsButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.loadRunFileGadgetsButton.Name = "loadRunFileGadgetsButton";
            this.loadRunFileGadgetsButton.Size = new System.Drawing.Size(134, 21);
            this.loadRunFileGadgetsButton.TabIndex = 1;
            this.loadRunFileGadgetsButton.Text = "Load";
            this.loadRunFileGadgetsButton.UseVisualStyleBackColor = true;
            this.loadRunFileGadgetsButton.Click += new System.EventHandler(this.loadRunFileGadgetsButton_Click);
            // 
            // saveRunFileGadgetsButton
            // 
            this.saveRunFileGadgetsButton.Location = new System.Drawing.Point(6, 67);
            this.saveRunFileGadgetsButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.saveRunFileGadgetsButton.Name = "saveRunFileGadgetsButton";
            this.saveRunFileGadgetsButton.Size = new System.Drawing.Size(134, 21);
            this.saveRunFileGadgetsButton.TabIndex = 2;
            this.saveRunFileGadgetsButton.Text = "Save";
            this.saveRunFileGadgetsButton.UseVisualStyleBackColor = true;
            this.saveRunFileGadgetsButton.Click += new System.EventHandler(this.saveRunFileGadgetsButton_Click);
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
            "Episode 6 (No CE)",
            "Episode 6 (CE)"});
            this.runFileComboBox.Location = new System.Drawing.Point(6, 19);
            this.runFileComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.runFileComboBox.Name = "runFileComboBox";
            this.runFileComboBox.Size = new System.Drawing.Size(131, 21);
            this.runFileComboBox.TabIndex = 0;
            this.runFileComboBox.Text = "Select run file...";
            // 
            // SLY3GadgetsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 475);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.discardChangesButton);
            this.Controls.Add(this.saveAndReloadButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.murrayGadgetsR2ComboBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.murrayGadgetsL2ComboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.murrayGadgetsL1ComboBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.bentleyGadgetsR2ComboBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bentleyGadgetsL2ComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.bentleyGadgetsL1ComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.slyGadgetsR2ComboBox);
            this.Controls.Add(this.slyGadgetsR2Label);
            this.Controls.Add(this.slyGadgetsL2ComboBox);
            this.Controls.Add(this.slyGadgetsL2Label);
            this.Controls.Add(this.slyGadgetsL1ComboBox);
            this.Controls.Add(this.slyGadgetsL1Label);
            this.Controls.Add(this.murrayGadgetsToggleCheckBox);
            this.Controls.Add(this.bentleyGadgetsToggleCheckBox);
            this.Controls.Add(this.slyGadgetsToggleCheckBox);
            this.Controls.Add(this.murrayGadgetsLabel);
            this.Controls.Add(this.bentleyGadgetsLabel);
            this.Controls.Add(this.jumpAttackLevelSelector);
            this.Controls.Add(this.jumpAttackLevelLabel);
            this.Controls.Add(this.pushAttackLevelSelector);
            this.Controls.Add(this.pushAttackLevelLabel);
            this.Controls.Add(this.spinAttackLevelLabel);
            this.Controls.Add(this.slyGadgetsLabel);
            this.Controls.Add(this.spinAttackLevelSelector);
            this.Controls.Add(this.bentleyGadgetsCheckedList);
            this.Controls.Add(this.murrayGadgetsCheckedList);
            this.Controls.Add(this.slyGadgetsCheckedList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SLY3GadgetsForm";
            this.Text = "SluMAN :: Gadget Editor (Sly 3)";
            ((System.ComponentModel.ISupportInitialize)(this.spinAttackLevelSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pushAttackLevelSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.jumpAttackLevelSelector)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox slyGadgetsCheckedList;
        private System.Windows.Forms.CheckedListBox murrayGadgetsCheckedList;
        private System.Windows.Forms.CheckedListBox bentleyGadgetsCheckedList;
        private System.Windows.Forms.NumericUpDown spinAttackLevelSelector;
        private System.Windows.Forms.Label slyGadgetsLabel;
        private System.Windows.Forms.Label spinAttackLevelLabel;
        private System.Windows.Forms.Label pushAttackLevelLabel;
        private System.Windows.Forms.NumericUpDown pushAttackLevelSelector;
        private System.Windows.Forms.Label jumpAttackLevelLabel;
        private System.Windows.Forms.NumericUpDown jumpAttackLevelSelector;
        private System.Windows.Forms.Label bentleyGadgetsLabel;
        private System.Windows.Forms.Label murrayGadgetsLabel;
        private System.Windows.Forms.CheckBox slyGadgetsToggleCheckBox;
        private System.Windows.Forms.CheckBox bentleyGadgetsToggleCheckBox;
        private System.Windows.Forms.CheckBox murrayGadgetsToggleCheckBox;
        private System.Windows.Forms.Label slyGadgetsL1Label;
        private System.Windows.Forms.ComboBox slyGadgetsL1ComboBox;
        private System.Windows.Forms.ComboBox slyGadgetsL2ComboBox;
        private System.Windows.Forms.Label slyGadgetsL2Label;
        private System.Windows.Forms.ComboBox slyGadgetsR2ComboBox;
        private System.Windows.Forms.Label slyGadgetsR2Label;
        private System.Windows.Forms.ComboBox bentleyGadgetsR2ComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox bentleyGadgetsL2ComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox bentleyGadgetsL1ComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox murrayGadgetsR2ComboBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox murrayGadgetsL2ComboBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox murrayGadgetsL1ComboBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button saveAndReloadButton;
        private System.Windows.Forms.Button discardChangesButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button loadRunFileGadgetsButton;
        private System.Windows.Forms.Button saveRunFileGadgetsButton;
        private System.Windows.Forms.ComboBox runFileComboBox;
    }
}