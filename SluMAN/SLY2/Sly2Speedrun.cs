using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Sly 2 Speedrun Mode: the autosplitter (autosplitters/sly2-autosplitter.asl), the input
    /// display and nothing that changes the game. Run files are made in Practice Mode's Run Files
    /// tab and kept in an anchor file. The controls are built in code.
    /// </summary>
    public class Sly2Speedrun : Form
    {
        public sly2 game;
        public string gameNameId = sly2.addr.GameId;

        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly CheckBox autosplitterCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Button inputDisplayButton = new Button();
        private StatusLine statusLine;
        private GameSession session;

        public Sly2Speedrun(sly2 game)
        {
            this.game = game;
            Name = "Sly2Speedrun";
            Text = "SluMAN :: Sly 2: Band of Thieves (Speedrun Mode)";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft Sans Serif", 8.25F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            BuildMenu();
            BuildControls();
            ClientSize = new Size(260, inputDisplayButton.Bottom + 12);
            statusLine = new StatusLine(this, true);

            game.SetupInputDisplayMemorySubs();

            session = new GameSession(this, game, gameNameId, "Sly 2", true);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.BindAutosplitter(autosplitterCheckBox);
            session.AddAutosplitterLogMenu(menuStrip);
        }

        private void BuildMenu()
        {
            ToolStripMenuItem tools = new ToolStripMenuItem("Tools");
            tools.DropDownItems.Add("Switch Game/Mode", null, (sender, e) => session.SwitchGameOrMode());
            tools.DropDownItems.Add("Open User Data Folder", null, (sender, e) => UserData.OpenInExplorer());
            tools.DropDownItems.Add(new ToolStripSeparator());
            tools.DropDownItems.Add("Power Off (PS3)", null, (sender, e) => session.PowerOffPS3());
            tools.DropDownItems.Add("Reboot (PS3)", null, (sender, e) => session.RebootPS3());
            menuStrip.Items.Add(tools);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void BuildControls()
        {
            int top = menuStrip.PreferredSize.Height + 12;

            autosplitterCheckBox.Text = "Autosplitter";
            autosplitterCheckBox.AutoSize = true;
            autosplitterCheckBox.Location = new Point(14, top);
            Controls.Add(autosplitterCheckBox);

            alwaysOnTopCheckBox.Text = "Always On Top";
            alwaysOnTopCheckBox.AutoSize = true;
            alwaysOnTopCheckBox.Location = new Point(134, top);
            Controls.Add(alwaysOnTopCheckBox);

            inputDisplayButton.Text = "Input Display";
            inputDisplayButton.Location = new Point(12, top + 28);
            inputDisplayButton.Size = new Size(236, 28);
            inputDisplayButton.UseVisualStyleBackColor = true;
            inputDisplayButton.Click += (sender, e) => session.ShowInputDisplay();
            Controls.Add(inputDisplayButton);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(autosplitterCheckBox,
                "Shares the game state with LiveSplit. Load autosplitters/sly2-autosplitter.asl\n" +
                "in LiveSplit's Scriptable Auto Splitter.");
            FormClosed += (sender, e) => tip.Dispose();
        }
    }
}
