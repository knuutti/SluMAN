namespace racman
{
    partial class Sly2Practice
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sly2Practice));
            this.jobGroupBox = new System.Windows.Forms.GroupBox();
            this.jobComboBox = new System.Windows.Forms.ComboBox();
            this.loadJobButton = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.mapComboBox = new System.Windows.Forms.ComboBox();
            this.loadMapButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.setHealthButton = new System.Windows.Forms.Button();
            this.coinsTextBox = new System.Windows.Forms.TextBox();
            this.skipCinematicsButton = new System.Windows.Forms.Button();
            this.setCoinsButton = new System.Windows.Forms.Button();
            this.healthTextBox = new System.Windows.Forms.TextBox();
            this.gadgetButton = new System.Windows.Forms.Button();
            this.reloadGroupBox = new System.Windows.Forms.GroupBox();
            this.reloadButton = new System.Windows.Forms.Button();
            this.killButton = new System.Windows.Forms.Button();
            this.toggleGroupBox = new System.Windows.Forms.GroupBox();
            this.invulnerabilityCheckBox = new System.Windows.Forms.CheckBox();
            this.infiniteHealthCheckBox = new System.Windows.Forms.CheckBox();
            this.infiniteGadgetPowerCheckBox = new System.Windows.Forms.CheckBox();
            this.gameClockCheckBox = new System.Windows.Forms.CheckBox();
            this.infiniteJumpCheckBox = new System.Windows.Forms.CheckBox();
            this.positionEditorButton = new System.Windows.Forms.Button();
            this.alwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
            this.inputDisplayButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inputDisplayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureButtonCombosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryUtilitiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openUserDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.powerOffPs3Button = new System.Windows.Forms.ToolStripMenuItem();
            this.rebootPS3Button = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadAsCharacterComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.jobGroupBox.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.reloadGroupBox.SuspendLayout();
            this.toggleGroupBox.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // jobGroupBox
            // 
            this.jobGroupBox.Controls.Add(this.jobComboBox);
            this.jobGroupBox.Controls.Add(this.loadJobButton);
            this.jobGroupBox.Location = new System.Drawing.Point(7, 125);
            this.jobGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.jobGroupBox.Name = "jobGroupBox";
            this.jobGroupBox.Padding = new System.Windows.Forms.Padding(2);
            this.jobGroupBox.Size = new System.Drawing.Size(205, 89);
            this.jobGroupBox.TabIndex = 33;
            this.jobGroupBox.TabStop = false;
            this.jobGroupBox.Text = "Jobs";
            // 
            // jobComboBox
            // 
            this.jobComboBox.FormattingEnabled = true;
            this.jobComboBox.Items.AddRange(new object[] {
            "Cairo Museum Break-in",
            "Satellite Sabotage",
            "Breaking and Entering",
            " [BnE] Sneak",
            " [BnE] Guards",
            " [BnE] Photos",
            "Bug Dimitri\'s Office",
            " [BDO] Nightclub",
            "Follow Dimitri",
            " [FD] Follow",
            "Waterpump Destruction",
            "Silence the Alarms",
            "Moonlight Rendezvous",
            "Theater Pickpocketing",
            "Disco Demolitions",
            "OP: Thunder Beak",
            " [OTB] Steal the key",
            " [OTB] Climb the peacock",
            " [OTB] Shoot the hook",
            " [OTB] Printing room",
            " [OTB] Dimitri fight",
            "Recon the Ballroom",
            " [RtB] Photos",
            "Lower the Drawbridge",
            " [LtD] Lever",
            "Steal a Tuxedo",
            "Battle the Chopper",
            " [BtC] Fight",
            "Dominate the Dance Floor",
            "RC Bombing Run",
            " [RBR] Destroy the Jeep",
            "Elephant Rampage",
            " [ER] Elephants",
            "Boardroom Brawl",
            " [BB] Find the code",
            " [BB] Protect Bentley",
            "OP: Hippo Drop",
            " [OHD] First floor",
            " [OHD] Second floor",
            " [OHD] Dance",
            " [OHD] Protect Murray",
            "Spice Room Recon",
            " [SRR] Interior",
            "Freeing the Elephant",
            "Water Bug Run",
            "Leading Rajan",
            " [LR] Blueprints",
            "Blow the Dam",
            "Spice Grinder Destruction",
            "Neyla\'s Secret",
            " [NS] Keys",
            "Rip-Off the Ruby",
            " [ROtR] Reach the ruby",
            " [ROtR] Carry 1",
            " [ROtR] Carry 2",
            "OP: Wet Tiger",
            " [OWT] Protect Murray",
            " [OWT] Deliver TNT",
            " [OWT] Follow Neyla",
            " [OWT] Rajan fight",
            "Eavesdrop on Contessa",
            "Train Hack",
            " [TH] Hack 1",
            " [TH] Go to hack 2",
            " [TH] Hack 2",
            " [TH] Go to hack 3",
            " [TH] Hack 3",
            " [TH] Go to hack 4",
            " [TH] Hack 4",
            " [TH] Go to hack 5",
            " [TH] Hack 5",
            " [TH] Go to hack 6",
            " [TH] Hack 6",
            "Wall Bombing",
            " [WP] RC Chopper",
            " [WP] Return to hideout",
            "Code Capture",
            " [CC] Picture 2",
            " [CC] Picture 3",
            " [CC] Picture 4",
            "Lightning Action",
            " [LA] Rod 1",
            " [LA] Rod 2",
            " [LA] Rod 3",
            " [LA] Rod 4",
            "Close to Contessa",
            " [CtC] Key 2",
            " [CtC] Tank schedule",
            "Big House Brawl",
            " [BHB] Prison fight",
            "Disguise Bridge",
            " [DB] Protect Bentley",
            "OP: Trojan Tank",
            " [OTT] Crawl",
            " [OTT] Enter prison",
            " [OTT] Reach the control panel",
            " [OTT] Return to Bentley",
            " [OTT] Activate hypno-boxes",
            " [OTT] Destroy hypno-boxes",
            " [OTT] Lift the gate",
            " [OTT] Chase Contessa",
            "Know Your Enemy",
            " [KYE] Photos",
            " [KYE] Education tower",
            "Kidnap the General",
            " [KtG] General",
            "Ghost Capture",
            " [GC] Enter the tomb",
            " [GC] Tomb",
            " [GC] Ghosts",
            " [GC] Ghosts (No CS)",
            "Mojo Trap Action",
            " [MTA] Crypt 1",
            " [MTA] Go to crypt 2",
            " [MTA] Crypt 2",
            " [MTA] Go to crypt 3",
            " [MTA] Crypt 3",
            " [MTA] Go to crypt 4",
            " [MTA] Crypt 4",
            "Tank Showdown",
            " [TS] Tanks",
            "Stealing Voices",
            " [SV] Keys 1",
            " [SV] Go to crypt 1",
            " [SV] Crypt 1",
            " [SV] Keys 2",
            " [SV] Go to crypt 2",
            " [SV] Crypt 2",
            " [SV] Keys 3",
            " [SV] Go to crypt 3",
            " [SV] Crypt 3",
            "Crypt Hack",
            " [CH] Find the computer",
            " [CH] Hack 1",
            " [CH] Go to hack 2",
            " [CH] Hack 2",
            " [CH] Go to hack 3",
            " [CH] Hack 3",
            "OP: High Road",
            " [OHR] Paraglide",
            " [OHR] Enter tower",
            " [OHR] Hack",
            " [OHR] Chase Neyla",
            " [OHR] Turret",
            " [OHR] Contessa 1",
            " [OHR] Tank",
            " [OHR] Contessa 2",
            "Cabin Crimes",
            " [CC] Enter cabin 1",
            " [CC] Cabin 1",
            " [CC] Enter cabin 2",
            " [CC] Cabin 2",
            " [CC] Enter cabin 3",
            " [CC] Cabin 3",
            " [CC] Climb the mountain",
            "Spice in the Sky",
            " [SitS] Train 1",
            " [SitS] Train 2",
            " [SitS] Train 3",
            "A Friend in Need",
            " [AFiN] Follow Carmelita",
            " [AFiN] Key 1",
            " [AFiN] Key 2",
            " [AFiN] Key 3",
            "Ride the Iron Horse",
            " [RtIH] Enter the train",
            " [RtIH] Train",
            "Aerial Assault",
            " [AA] Enter the train",
            " [AA] RC Chopper",
            "Bear Cub Kidnapping",
            " [BCK] Cub 1",
            " [BCK] Cub 2",
            "Theft on the Rails",
            " [TotR] Enter the train",
            " [TotR] Train",
            "OP: Choo-Choo",
            " [OCC] Catch the train",
            " [OCC] Sly section 1",
            " [OCC] Neyla fight 1",
            " [OCC] Sly section 2",
            " [OCC] Neyla fight 2",
            "Recon the Sawmill",
            " [RtS] Photos",
            "Laser Redirection",
            " [LR] Crystal 1",
            " [LR] Crystal 2",
            " [LR] Crystal 3",
            " [LR] Crystal 4",
            " [LR] Crystal 5",
            " [LR] Crystal 6",
            "Bearcave Bugging",
            " [BB] Cave",
            " [BB] Transmitters",
            " [BB] Transmitters (No CS)",
            "RC Combat Club",
            " [RCC] Enter the club",
            " [RCC] Barrel",
            " [RCC] Moose head",
            " [RCC] RC fight",
            "Old Grizzle Face",
            " [OGF] Destroy the generators",
            "Lighthouse Break In",
            " [LBI] Enter the lighthouse",
            " [LBI] Reach the door",
            " [LBI] Climb the lighthouse",
            "Thermal Ride",
            " [TR] Paraglide",
            " [TR] Egg",
            "Boat Hack",
            " [BH] Go to hack 1",
            " [BH] Hack 1",
            " [BH] Go to hack 2",
            " [BH] Hack 2",
            " [BH] Go to hack 3",
            " [BH] Hack 3",
            "OP: Canada Games",
            " [OCG] Cut the log",
            " [OCG] Carry the egg",
            " [OCG] Climb the wall",
            " [OCG] Shoot the hooks",
            " [OCG] Balance on the logs",
            " [OCG] Lure the ducks",
            " [OCG] Enter the saw",
            " [OCG] Bison fight",
            " [OCG] Reach the battery",
            "Blimp HQ Recon",
            " [BHR] Enter the blimp",
            " [BHR] Photos",
            " [BHR] Photos (No CS)",
            " [BHR] Keys",
            " [BHR] Magnets",
            "Bentley/Murray Team Up",
            " [BMTU] Go to hack 1",
            " [BMTU] Hack 1",
            " [BMTU] Go to hack 2",
            " [BMTU] Hack 2",
            " [BMTU] Go to hack 3",
            " [BMTU] Hack 3",
            " [BMTU] First floor",
            " [BMTU] Second floor",
            "Murray/Sly Tag Team",
            " [MSTT] Alarm 1",
            " [MSTT] Alarm 2",
            " [MSTT] Alarm 3",
            " [MSTT] Alarm 4",
            " [MSTT] Alarm 5",
            " [MSTT] Reach the engine",
            " [MSTT] Lift the door",
            " [MSTT] First floor",
            " [MSTT] Second floor",
            "Sly/Bentley Conspire",
            " [SBC] Keys",
            " [SBC] Enter the engine",
            " [SBC] First floor",
            " [SBC] Second floor",
            "Charged TNT Run",
            " [CTR] Enter TNT",
            " [CTR] Checkpoint 1",
            " [CTR] Checkpoint 2",
            " [CTR] Checkpoint 3",
            "Mega-Jump Job",
            " [MJJ] Climb",
            "Showdown with Clock-La",
            " [SWC] Clock-La fight",
            " [SWC] Paraglide",
            " [SWC] Hit the head",
            " [SWC] Get the hate chip"});
            this.jobComboBox.Location = new System.Drawing.Point(16, 25);
            this.jobComboBox.Name = "jobComboBox";
            this.jobComboBox.Size = new System.Drawing.Size(177, 21);
            this.jobComboBox.TabIndex = 16;
            this.jobComboBox.Text = "Select a job...";
            // 
            // loadJobButton
            // 
            this.loadJobButton.Location = new System.Drawing.Point(16, 51);
            this.loadJobButton.Name = "loadJobButton";
            this.loadJobButton.Size = new System.Drawing.Size(88, 24);
            this.loadJobButton.TabIndex = 17;
            this.loadJobButton.Text = "Load";
            this.loadJobButton.UseVisualStyleBackColor = true;
            this.loadJobButton.Click += new System.EventHandler(this.loadJobButton_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.mapComboBox);
            this.groupBox2.Controls.Add(this.loadMapButton);
            this.groupBox2.Location = new System.Drawing.Point(11, 37);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size = new System.Drawing.Size(205, 84);
            this.groupBox2.TabIndex = 32;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Locations";
            // 
            // mapComboBox
            // 
            this.mapComboBox.FormattingEnabled = true;
            this.mapComboBox.Location = new System.Drawing.Point(15, 25);
            this.mapComboBox.Name = "mapComboBox";
            this.mapComboBox.Size = new System.Drawing.Size(179, 21);
            this.mapComboBox.TabIndex = 8;
            this.mapComboBox.Text = "Select a map...";
            // 
            // loadMapButton
            // 
            this.loadMapButton.Location = new System.Drawing.Point(15, 51);
            this.loadMapButton.Name = "loadMapButton";
            this.loadMapButton.Size = new System.Drawing.Size(85, 24);
            this.loadMapButton.TabIndex = 9;
            this.loadMapButton.Text = "Load Map";
            this.loadMapButton.UseVisualStyleBackColor = true;
            this.loadMapButton.Click += new System.EventHandler(this.loadMapButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.setHealthButton);
            this.groupBox1.Controls.Add(this.coinsTextBox);
            this.groupBox1.Controls.Add(this.skipCinematicsButton);
            this.groupBox1.Controls.Add(this.setCoinsButton);
            this.groupBox1.Controls.Add(this.healthTextBox);
            this.groupBox1.Controls.Add(this.gadgetButton);
            this.groupBox1.Location = new System.Drawing.Point(227, 37);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(234, 155);
            this.groupBox1.TabIndex = 31;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Utility";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 58);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 25;
            this.label2.Text = "Health:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 17);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Coins:";
            // 
            // setHealthButton
            // 
            this.setHealthButton.Location = new System.Drawing.Point(134, 72);
            this.setHealthButton.Name = "setHealthButton";
            this.setHealthButton.Size = new System.Drawing.Size(86, 24);
            this.setHealthButton.TabIndex = 23;
            this.setHealthButton.Text = "Set Health";
            this.setHealthButton.UseVisualStyleBackColor = true;
            this.setHealthButton.Click += new System.EventHandler(this.setHealthButton_Click);
            // 
            // coinsTextBox
            // 
            this.coinsTextBox.Location = new System.Drawing.Point(16, 33);
            this.coinsTextBox.Name = "coinsTextBox";
            this.coinsTextBox.Size = new System.Drawing.Size(112, 20);
            this.coinsTextBox.TabIndex = 0;
            this.coinsTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.coinsTextBox_KeyDown);
            // 
            // skipCinematicsButton
            // 
            this.skipCinematicsButton.Location = new System.Drawing.Point(15, 99);
            this.skipCinematicsButton.Name = "skipCinematicsButton";
            this.skipCinematicsButton.Size = new System.Drawing.Size(98, 40);
            this.skipCinematicsButton.TabIndex = 18;
            this.skipCinematicsButton.Text = "Skip Cinematic";
            this.skipCinematicsButton.UseVisualStyleBackColor = true;
            this.skipCinematicsButton.Click += new System.EventHandler(this.skipCinematicsButton_Click);
            // 
            // setCoinsButton
            // 
            this.setCoinsButton.Location = new System.Drawing.Point(134, 30);
            this.setCoinsButton.Name = "setCoinsButton";
            this.setCoinsButton.Size = new System.Drawing.Size(86, 24);
            this.setCoinsButton.TabIndex = 22;
            this.setCoinsButton.Text = "Set Coins";
            this.setCoinsButton.UseVisualStyleBackColor = true;
            this.setCoinsButton.Click += new System.EventHandler(this.setCoinsButton_Click);
            // 
            // healthTextBox
            // 
            this.healthTextBox.Location = new System.Drawing.Point(16, 75);
            this.healthTextBox.Name = "healthTextBox";
            this.healthTextBox.Size = new System.Drawing.Size(112, 20);
            this.healthTextBox.TabIndex = 21;
            // 
            // gadgetButton
            // 
            this.gadgetButton.Location = new System.Drawing.Point(119, 99);
            this.gadgetButton.Name = "gadgetButton";
            this.gadgetButton.Size = new System.Drawing.Size(101, 40);
            this.gadgetButton.TabIndex = 11;
            this.gadgetButton.Text = "Gadgets";
            this.gadgetButton.UseVisualStyleBackColor = true;
            this.gadgetButton.Click += new System.EventHandler(this.gadgetButton_Click);
            // 
            // reloadGroupBox
            // 
            this.reloadGroupBox.Controls.Add(this.label3);
            this.reloadGroupBox.Controls.Add(this.reloadAsCharacterComboBox);
            this.reloadGroupBox.Controls.Add(this.reloadButton);
            this.reloadGroupBox.Controls.Add(this.killButton);
            this.reloadGroupBox.Location = new System.Drawing.Point(11, 218);
            this.reloadGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.reloadGroupBox.Name = "reloadGroupBox";
            this.reloadGroupBox.Padding = new System.Windows.Forms.Padding(2);
            this.reloadGroupBox.Size = new System.Drawing.Size(201, 98);
            this.reloadGroupBox.TabIndex = 30;
            this.reloadGroupBox.TabStop = false;
            this.reloadGroupBox.Text = "Reload";
            // 
            // reloadButton
            // 
            this.reloadButton.Location = new System.Drawing.Point(13, 25);
            this.reloadButton.Name = "reloadButton";
            this.reloadButton.Size = new System.Drawing.Size(87, 31);
            this.reloadButton.TabIndex = 21;
            this.reloadButton.Text = "Reload";
            this.reloadButton.UseVisualStyleBackColor = true;
            this.reloadButton.Click += new System.EventHandler(this.reloadButton_Click);
            // 
            // killButton
            // 
            this.killButton.Location = new System.Drawing.Point(106, 25);
            this.killButton.Name = "killButton";
            this.killButton.Size = new System.Drawing.Size(82, 31);
            this.killButton.TabIndex = 19;
            this.killButton.Text = "Kill";
            this.killButton.UseVisualStyleBackColor = true;
            this.killButton.Click += new System.EventHandler(this.killButton_Click);
            // 
            // toggleGroupBox
            // 
            this.toggleGroupBox.Controls.Add(this.invulnerabilityCheckBox);
            this.toggleGroupBox.Controls.Add(this.infiniteHealthCheckBox);
            this.toggleGroupBox.Controls.Add(this.infiniteGadgetPowerCheckBox);
            this.toggleGroupBox.Controls.Add(this.gameClockCheckBox);
            this.toggleGroupBox.Controls.Add(this.infiniteJumpCheckBox);
            this.toggleGroupBox.Controls.Add(this.alwaysOnTopCheckBox);
            this.toggleGroupBox.Location = new System.Drawing.Point(227, 196);
            this.toggleGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.toggleGroupBox.Name = "toggleGroupBox";
            this.toggleGroupBox.Padding = new System.Windows.Forms.Padding(2);
            this.toggleGroupBox.Size = new System.Drawing.Size(234, 144);
            this.toggleGroupBox.TabIndex = 29;
            this.toggleGroupBox.TabStop = false;
            this.toggleGroupBox.Text = "Toggles";
            // 
            // invulnerabilityCheckBox
            // 
            this.invulnerabilityCheckBox.AutoSize = true;
            this.invulnerabilityCheckBox.Location = new System.Drawing.Point(16, 20);
            this.invulnerabilityCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.invulnerabilityCheckBox.Name = "invulnerabilityCheckBox";
            this.invulnerabilityCheckBox.Size = new System.Drawing.Size(94, 17);
            this.invulnerabilityCheckBox.TabIndex = 30;
            this.invulnerabilityCheckBox.Text = "Invulnerability";
            this.invulnerabilityCheckBox.UseVisualStyleBackColor = true;
            this.invulnerabilityCheckBox.CheckedChanged += new System.EventHandler(this.invulnerabilityCheckBox_CheckedChanged);
            // 
            // infiniteHealthCheckBox
            // 
            this.infiniteHealthCheckBox.AutoSize = true;
            this.infiniteHealthCheckBox.Location = new System.Drawing.Point(16, 40);
            this.infiniteHealthCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.infiniteHealthCheckBox.Name = "infiniteHealthCheckBox";
            this.infiniteHealthCheckBox.Size = new System.Drawing.Size(95, 17);
            this.infiniteHealthCheckBox.TabIndex = 31;
            this.infiniteHealthCheckBox.Text = "Infinite Health";
            this.infiniteHealthCheckBox.UseVisualStyleBackColor = true;
            this.infiniteHealthCheckBox.CheckedChanged += new System.EventHandler(this.infiniteHealthCheckBox_CheckedChanged);
            // 
            // infiniteGadgetPowerCheckBox
            // 
            this.infiniteGadgetPowerCheckBox.AutoSize = true;
            this.infiniteGadgetPowerCheckBox.Location = new System.Drawing.Point(16, 60);
            this.infiniteGadgetPowerCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.infiniteGadgetPowerCheckBox.Name = "infiniteGadgetPowerCheckBox";
            this.infiniteGadgetPowerCheckBox.Size = new System.Drawing.Size(131, 17);
            this.infiniteGadgetPowerCheckBox.TabIndex = 32;
            this.infiniteGadgetPowerCheckBox.Text = "Infinite Gadget Power";
            this.infiniteGadgetPowerCheckBox.UseVisualStyleBackColor = true;
            this.infiniteGadgetPowerCheckBox.CheckedChanged += new System.EventHandler(this.infiniteGadgetPowerCheckBox_CheckedChanged);
            // 
            // gameClockCheckBox
            // 
            this.gameClockCheckBox.AutoSize = true;
            this.gameClockCheckBox.Location = new System.Drawing.Point(16, 80);
            this.gameClockCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.gameClockCheckBox.Name = "gameClockCheckBox";
            this.gameClockCheckBox.Size = new System.Drawing.Size(114, 17);
            this.gameClockCheckBox.TabIndex = 33;
            this.gameClockCheckBox.Text = "Freeze Game Clock";
            this.gameClockCheckBox.UseVisualStyleBackColor = true;
            this.gameClockCheckBox.CheckedChanged += new System.EventHandler(this.gameClockCheckBox_CheckedChanged);
            // 
            // infiniteJumpCheckBox
            // 
            this.infiniteJumpCheckBox.AutoSize = true;
            this.infiniteJumpCheckBox.Location = new System.Drawing.Point(16, 100);
            this.infiniteJumpCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.infiniteJumpCheckBox.Name = "infiniteJumpCheckBox";
            this.infiniteJumpCheckBox.Size = new System.Drawing.Size(87, 17);
            this.infiniteJumpCheckBox.TabIndex = 34;
            this.infiniteJumpCheckBox.Text = "Infinite Jump";
            this.infiniteJumpCheckBox.UseVisualStyleBackColor = true;
            this.infiniteJumpCheckBox.CheckedChanged += new System.EventHandler(this.infiniteJumpCheckBox_CheckedChanged);
            // 
            // alwaysOnTopCheckBox
            // 
            this.alwaysOnTopCheckBox.AutoSize = true;
            this.alwaysOnTopCheckBox.Location = new System.Drawing.Point(16, 120);
            this.alwaysOnTopCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.alwaysOnTopCheckBox.Name = "alwaysOnTopCheckBox";
            this.alwaysOnTopCheckBox.Size = new System.Drawing.Size(98, 17);
            this.alwaysOnTopCheckBox.TabIndex = 5;
            this.alwaysOnTopCheckBox.Text = "Always On Top";
            this.alwaysOnTopCheckBox.UseVisualStyleBackColor = true;
            this.alwaysOnTopCheckBox.CheckedChanged += new System.EventHandler(this.alwaysOnTopCheckBox_CheckedChanged);
            // 
            // inputDisplayButton
            // 
            this.inputDisplayButton.Location = new System.Drawing.Point(227, 345);
            this.inputDisplayButton.Name = "inputDisplayButton";
            this.inputDisplayButton.Size = new System.Drawing.Size(115, 30);
            this.inputDisplayButton.TabIndex = 27;
            this.inputDisplayButton.Text = "Input Display";
            this.inputDisplayButton.UseVisualStyleBackColor = true;
            this.inputDisplayButton.Click += new System.EventHandler(this.inputDisplayButton_Click);
            // 
            // positionEditorButton
            // 
            this.positionEditorButton.Location = new System.Drawing.Point(346, 345);
            this.positionEditorButton.Name = "positionEditorButton";
            this.positionEditorButton.Size = new System.Drawing.Size(115, 30);
            this.positionEditorButton.TabIndex = 35;
            this.positionEditorButton.Text = "Position Editor";
            this.positionEditorButton.UseVisualStyleBackColor = true;
            this.positionEditorButton.Click += new System.EventHandler(this.positionEditorButton_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(479, 24);
            this.menuStrip1.TabIndex = 28;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.switchGameToolStripMenuItem,
            this.inputDisplayToolStripMenuItem,
            this.configureButtonCombosToolStripMenuItem,
            this.memoryUtilitiesToolStripMenuItem,
            this.openUserDataToolStripMenuItem,
            this.toolStripSeparator1,
            this.powerOffPs3Button,
            this.rebootPS3Button});
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.menuToolStripMenuItem.Text = "Tools";
            // 
            // switchGameToolStripMenuItem
            // 
            this.switchGameToolStripMenuItem.Name = "switchGameToolStripMenuItem";
            this.switchGameToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.switchGameToolStripMenuItem.Text = "Switch Game/Mode";
            this.switchGameToolStripMenuItem.Click += new System.EventHandler(this.switchGameToolStripMenuItem_Click);
            // 
            // inputDisplayToolStripMenuItem
            // 
            this.inputDisplayToolStripMenuItem.Name = "inputDisplayToolStripMenuItem";
            this.inputDisplayToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.inputDisplayToolStripMenuItem.Text = "Input Display";
            this.inputDisplayToolStripMenuItem.Click += new System.EventHandler(this.inputDisplayToolStripMenuItem_Click);
            // 
            // configureButtonCombosToolStripMenuItem
            // 
            this.configureButtonCombosToolStripMenuItem.Name = "configureButtonCombosToolStripMenuItem";
            this.configureButtonCombosToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.configureButtonCombosToolStripMenuItem.Text = "Controller Combos...";
            this.configureButtonCombosToolStripMenuItem.Click += new System.EventHandler(this.configureButtonCombosToolStripMenuItem_Click);
            // 
            // memoryUtilitiesToolStripMenuItem
            // 
            this.memoryUtilitiesToolStripMenuItem.Name = "memoryUtilitiesToolStripMenuItem";
            this.memoryUtilitiesToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.memoryUtilitiesToolStripMenuItem.Text = "Memory Utilities...";
            this.memoryUtilitiesToolStripMenuItem.Click += new System.EventHandler(this.memoryUtilitiesToolStripMenuItem_Click);
            // 
            // openUserDataToolStripMenuItem
            // 
            this.openUserDataToolStripMenuItem.Name = "openUserDataToolStripMenuItem";
            this.openUserDataToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.openUserDataToolStripMenuItem.Text = "Open User Data Folder";
            this.openUserDataToolStripMenuItem.Click += new System.EventHandler(this.openUserDataToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(176, 6);
            // 
            // powerOffPs3Button
            // 
            this.powerOffPs3Button.Name = "powerOffPs3Button";
            this.powerOffPs3Button.Size = new System.Drawing.Size(179, 22);
            this.powerOffPs3Button.Text = "Power Off (PS3)";
            this.powerOffPs3Button.Click += new System.EventHandler(this.powerOffPs3Button_Click);
            // 
            // rebootPS3Button
            // 
            this.rebootPS3Button.Name = "rebootPS3Button";
            this.rebootPS3Button.Size = new System.Drawing.Size(179, 22);
            this.rebootPS3Button.Text = "Reboot (PS3)";
            this.rebootPS3Button.Click += new System.EventHandler(this.rebootPS3Button_Click);
            // 
            // reloadAsCharacterComboBox
            // 
            this.reloadAsCharacterComboBox.FormattingEnabled = true;
            this.reloadAsCharacterComboBox.Items.AddRange(new object[] {
            "Default",
            "Sly",
            "Bentley",
            "Murray"});
            this.reloadAsCharacterComboBox.Location = new System.Drawing.Point(74, 62);
            this.reloadAsCharacterComboBox.Name = "reloadAsCharacterComboBox";
            this.reloadAsCharacterComboBox.Size = new System.Drawing.Size(114, 21);
            this.reloadAsCharacterComboBox.TabIndex = 22;
            this.reloadAsCharacterComboBox.Text = "Default";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 23;
            this.label3.Text = "Character:";
            // 
            // Sly2Practice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(479, 383);
            this.Controls.Add(this.jobGroupBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.reloadGroupBox);
            this.Controls.Add(this.toggleGroupBox);
            this.Controls.Add(this.inputDisplayButton);
            this.Controls.Add(this.positionEditorButton);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Sly2Practice";
            this.Text = "SluMAN :: Sly 2: Band of Thieves (Practice Mode)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Sly2Practice_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Sly2Practice_FormClosed);
            this.jobGroupBox.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.reloadGroupBox.ResumeLayout(false);
            this.reloadGroupBox.PerformLayout();
            this.toggleGroupBox.ResumeLayout(false);
            this.toggleGroupBox.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox jobGroupBox;
        private System.Windows.Forms.ComboBox jobComboBox;
        private System.Windows.Forms.Button loadJobButton;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox mapComboBox;
        private System.Windows.Forms.Button loadMapButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button setHealthButton;
        private System.Windows.Forms.TextBox coinsTextBox;
        private System.Windows.Forms.Button skipCinematicsButton;
        private System.Windows.Forms.Button setCoinsButton;
        private System.Windows.Forms.TextBox healthTextBox;
        private System.Windows.Forms.Button gadgetButton;
        private System.Windows.Forms.GroupBox reloadGroupBox;
        private System.Windows.Forms.Button reloadButton;
        private System.Windows.Forms.Button killButton;
        private System.Windows.Forms.GroupBox toggleGroupBox;
        private System.Windows.Forms.CheckBox alwaysOnTopCheckBox;
        private System.Windows.Forms.CheckBox invulnerabilityCheckBox;
        private System.Windows.Forms.CheckBox infiniteHealthCheckBox;
        private System.Windows.Forms.CheckBox infiniteGadgetPowerCheckBox;
        private System.Windows.Forms.CheckBox gameClockCheckBox;
        private System.Windows.Forms.CheckBox infiniteJumpCheckBox;
        private System.Windows.Forms.Button positionEditorButton;
        private System.Windows.Forms.Button inputDisplayButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inputDisplayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureButtonCombosToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem memoryUtilitiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openUserDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem powerOffPs3Button;
        private System.Windows.Forms.ToolStripMenuItem rebootPS3Button;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox reloadAsCharacterComboBox;
    }
}