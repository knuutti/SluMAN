using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SluMAN
{
    public partial class Sly1Speedrun : Form
    {
        public sly1 game;
        public string gameNameId = "NPUA80663";

        private StatusLine statusLine;
        private GameSession session;

        public Sly1Speedrun(sly1 game)
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            game.SetupInputDisplayMemorySubs();

            session = new GameSession(this, game, gameNameId, "Sly 1", true);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.BindAutosplitter(autosplitterCheckbox);
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            session.ShowInputDisplay();
        }

        private void inputDisplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.ShowInputDisplay();
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.RebootPS3();
        }

        private void switchGameModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryForm.ShowFor(this);
        }

        private void openUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserData.OpenInExplorer();
        }
    }
}
