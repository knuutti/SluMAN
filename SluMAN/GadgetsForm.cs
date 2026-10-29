using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Gadgets tab of a Practice window. The game's form lays out the gadget lists and
    /// bindings in three 152-wide columns (Sly, Bentley, Murray) and calls BuildButtonBar, which
    /// adds the same bar under the second and third columns in every game: run file gadgets
    /// (Load / Save) and Discard Changes, Save, Save and Reload. The space under the first column
    /// is the game's own (Sly 2's vault gadgets, Sly 3's special moves). Under the bar, a warning
    /// names the gadgets saving a run file left out, as they aren't available yet in its episode
    /// (GadgetAvailability).
    /// </summary>
    public abstract class GadgetsForm : Form, IRefreshOnShow
    {
        private const int ColumnWidth = 152;
        private const int SecondColumnX = 170;
        private const int ThirdColumnX = 328;

        private readonly IGame game;
        protected readonly ComboBox runFileComboBox = new ComboBox();
        private readonly Label runFileWarningLabel = new Label();
        protected StatusLine statusLine;

        protected GadgetsForm(IGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Adds the button bar with its top at <paramref name="top"/>, sizes the form to fit and
        /// adds the status line. Call at the end of the constructor.
        /// </summary>
        protected void BuildButtonBar(int top, string[] runFiles)
        {
            GroupBox runFileGroupBox = new GroupBox();
            runFileGroupBox.Text = "Run file gadgets";
            runFileGroupBox.Location = new Point(SecondColumnX, top);
            runFileGroupBox.Size = new Size(ColumnWidth, 98);
            Controls.Add(runFileGroupBox);

            runFileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runFileComboBox.Location = new Point(8, 19);
            runFileComboBox.Size = new Size(ColumnWidth - 16, 21);
            runFileComboBox.Items.AddRange(runFiles);
            runFileComboBox.SelectedIndexChanged += (sender, e) => runFileWarningLabel.Text = "";
            runFileGroupBox.Controls.Add(runFileComboBox);

            AddButton(runFileGroupBox, "Load", new Point(8, 45), ColumnWidth - 16, (sender, e) => LoadRunFileClicked());
            AddButton(runFileGroupBox, "Save", new Point(8, 69), ColumnWidth - 16, (sender, e) => SaveRunFileClicked());

            AddButton(this, "Discard Changes", new Point(ThirdColumnX, top + 6), ColumnWidth, (sender, e) => DiscardChanges());
            AddButton(this, "Save", new Point(ThirdColumnX, top + 35), ColumnWidth, (sender, e) => Save(false));
            AddButton(this, "Save and Reload", new Point(ThirdColumnX, top + 64), ColumnWidth, (sender, e) => Save(true));

            Label note = new Label();
            note.Text = "Changes apply when you Save; gadget bindings after a reload.";
            note.AutoSize = true;
            note.Location = new Point(SecondColumnX - 3, runFileGroupBox.Bottom + 6);
            Controls.Add(note);

            runFileWarningLabel.Location = new Point(SecondColumnX - 3, note.Bottom + 4);
            runFileWarningLabel.Size = new Size(ThirdColumnX + ColumnWidth - SecondColumnX + 3, 30);
            runFileWarningLabel.ForeColor = Color.Firebrick;
            Controls.Add(runFileWarningLabel);

            int bottom = 0;
            foreach (Control control in Controls)
            {
                bottom = Math.Max(bottom, control.Bottom);
            }
            ClientSize = new Size(ThirdColumnX + ColumnWidth + 12, bottom + 8);
            statusLine = new StatusLine(this, false);
        }

        private static void AddButton(Control parent, string text, Point location, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            parent.Controls.Add(button);
        }

        /// <summary>Reads the unlocks and bindings from the game into the form again.</summary>
        public abstract void RefreshFromGame();

        /// <summary>Writes the form's unlocks and bindings to the game.</summary>
        protected abstract void WriteToGame();

        /// <summary>Reloads the game, so the bindings take effect.</summary>
        protected abstract void ReloadGame();

        /// <summary>Fills the form in from a run file's saved gadgets (not the game).</summary>
        protected abstract void LoadRunFileGadgets(string runFile);

        /// <summary>
        /// Saves the form's gadgets to a run file, first unticking the ones not available yet at
        /// the start of its episode (GadgetAvailability), and returns their names.
        /// </summary>
        protected abstract List<string> SaveRunFileGadgets(string runFile);

        private void DiscardChanges()
        {
            RefreshFromGame();
            statusLine.Info("Showing the game's gadgets again.");
        }

        private void Save(bool reload)
        {
            game.api.Notify(reload ? "GadgetForm :: Save and Reload" : "GadgetForm :: Save");
            try
            {
                WriteToGame();
                if (reload)
                {
                    ReloadGame();
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save the gadgets: {ex.Message}");
                return;
            }
            // The tab hands back to Practice.
            Close();
        }

        private void LoadRunFileClicked()
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to load from first.");
                return;
            }
            runFileWarningLabel.Text = "";
            LoadRunFileGadgets(runFileComboBox.SelectedItem.ToString());
        }

        private void SaveRunFileClicked()
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to save to first.");
                return;
            }
            string runFile = runFileComboBox.SelectedItem.ToString();
            List<string> removed = SaveRunFileGadgets(runFile);
            runFileWarningLabel.Text = GadgetAvailability.RemovedWarning(runFile, removed);
            game.api.Notify($"SluMAN {func.VersionText}: Gadget settings saved to {runFile} run file.");
        }
    }
}
