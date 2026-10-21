using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Run Files tab of a Practice window: instructions, a run file to pick and Load. A run
    /// file is made once with this tab and saved over an anchor file in game; a run loads the
    /// anchor file, not SluMAN. Each game loads its run files itself. The controls are built in code.
    /// </summary>
    public abstract class RunFilesForm : Form
    {
        // Edit these to change the instructions on the tab.
        private const string Instructions =
            "A run file sets up the save you start a run from. You make it here once and keep it " +
            "in an anchor file slot; during a run, load the anchor file in game instead of using SluMAN.\n\n" +
            "1. Start a new game.\n" +
            "2. Pick the run file below and click Load. The game reloads at its start.\n" +
            "3. Save over your anchor file slot.";

        protected readonly ComboBox runFileComboBox = new ComboBox();
        protected StatusLine statusLine;
        private readonly Label instructionsLabel = new Label();
        private readonly Label noteLabel = new Label();
        private readonly Button loadButton = new Button();

        /// <param name="runFiles">The run files, as the combo box lists them.</param>
        /// <param name="note">A line below the Load button about this game's run files, or null.</param>
        protected RunFilesForm(string gameName, string[] runFiles, string note)
        {
            Name = "RunFilesForm";
            Text = $"SluMAN :: Run Files ({gameName})";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls(runFiles, note);
            ClientSize = new Size(640, noteLabel.Bottom + 8);
            statusLine = new StatusLine(this, false);
        }

        private void BuildControls(string[] runFiles, string note)
        {
            GroupBox instructionsGroupBox = new GroupBox();
            instructionsGroupBox.Text = "How to use run files";
            instructionsGroupBox.Location = new Point(8, 8);
            instructionsGroupBox.Size = new Size(624, 128);
            Controls.Add(instructionsGroupBox);

            instructionsLabel.Text = Instructions;
            instructionsLabel.Location = new Point(10, 20);
            instructionsLabel.Size = new Size(604, 100);
            instructionsGroupBox.Controls.Add(instructionsLabel);

            GroupBox loadGroupBox = new GroupBox();
            loadGroupBox.Text = "Load run file";
            loadGroupBox.Location = new Point(8, 144);
            loadGroupBox.Size = new Size(312, 54);
            Controls.Add(loadGroupBox);

            runFileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runFileComboBox.Location = new Point(10, 20);
            runFileComboBox.Size = new Size(193, 21);
            runFileComboBox.Items.AddRange(runFiles);
            loadGroupBox.Controls.Add(runFileComboBox);

            loadButton.Text = "Load";
            loadButton.Location = new Point(208, 19);
            loadButton.Size = new Size(94, 23);
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += loadButton_Click;
            loadGroupBox.Controls.Add(loadButton);

            noteLabel.Text = note ?? "";
            noteLabel.Location = new Point(8, 206);
            noteLabel.Size = new Size(624, 30);
            Controls.Add(noteLabel);
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to load first.");
                return;
            }

            try
            {
                LoadRunFile(runFileComboBox.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the run file: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        /// <summary>Writes the run file's save data, map, spawn and gadgets, and loads it.</summary>
        protected abstract void LoadRunFile(string runFile);

        /// <summary>The data a run file loads with, from the game's run file config.</summary>
        protected class RunFileData
        {
            public string MapName;
            public uint SpawnLocation;
            public byte[] MemoryData;
            public uint MemoryStartAddress;
            public float SuckValue = 0.0f;
        }
    }
}
