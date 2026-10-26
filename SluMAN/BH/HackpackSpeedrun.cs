using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Bentley's Hackpack Speedrun Mode: the autosplitter (autosplitters/hackpack-autosplitter.asl),
    /// the input display and nothing that changes the game. The game runs on Sly 4's engine, so
    /// this drives the sly4 class. The controls are built in code.
    /// </summary>
    public class HackpackSpeedrun : Form
    {
        public sly4 game;
        public string gameNameId = sly4.addr.GameId;

        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly CheckBox autosplitterCheckBox = new CheckBox();
        private readonly CheckBox alwaysOnTopCheckBox = new CheckBox();
        private readonly Button inputDisplayButton = new Button();
        private StatusLine statusLine;
        private GameSession session;

        public HackpackSpeedrun(sly4 game)
        {
            this.game = game;
            Name = "HackpackSpeedrun";
            Text = "SluMAN :: Bentley's Hackpack (Speedrun Mode)";
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
            ResetTunables();

            session = new GameSession(this, game, gameNameId, "Hackpack", true);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.BindAutosplitter(autosplitterCheckBox);
        }

        /// <summary>
        /// Puts back tunables a Practice Mode that didn't get to close properly left changed, so
        /// runs aren't done with them.
        /// </summary>
        private void ResetTunables()
        {
            if (game.tunables.ChangedCount == 0)
            {
                return;
            }
            try
            {
                int reset = game.tunables.ResetToDefaults(false);
                statusLine.Info($"Put {reset} tunables changed in Practice Mode back to their defaults.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't reset the tunables changed in Practice Mode: {ex.Message}");
            }
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
                "Shares the game state with LiveSplit. Load autosplitters/hackpack-autosplitter.asl\n" +
                "in LiveSplit's Scriptable Auto Splitter.");
            FormClosed += (sender, e) => tip.Dispose();
        }
    }
}
