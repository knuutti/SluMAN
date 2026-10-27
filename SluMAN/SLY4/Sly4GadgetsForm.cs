using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The Gadgets tab: a list of each character's gadgets, ticked if bought, and the gadgets each
    /// run file loads with. Ticking one buys it; unticking a bought one puts it back in ThiefNet.
    /// The game only reads its gadgets on a load (a checkpoint or a level), so changes stay in
    /// the lists until Apply writes them and reloads. The controls are built in code.
    /// </summary>
    public class Sly4GadgetsForm : Form, IRefreshOnShow
    {
        // One column per character, in Sly4Gadgets.CharacterNames' order. Carmelita's short list
        // leaves room below it for Apply and Discard.
        private const int ColumnWidth = 151;
        private const int ColumnGap = 4;
        private const int ListTop = 30;

        private readonly sly4 game;
        private readonly Sly4JobQueue jobQueue;
        private readonly Dictionary<string, CheckedListBox> lists = new Dictionary<string, CheckedListBox>();
        // Above each list: ticks or unticks the character's every gadget.
        private readonly Dictionary<string, CheckBox> allCheckBoxes = new Dictionary<string, CheckBox>();
        private readonly ComboBox runFileComboBox = new ComboBox();
        private readonly GroupBox runFileGroupBox = new GroupBox();
        // Says which gadgets saving a run file left out, as they aren't available yet.
        private readonly Label runFileWarningLabel = new Label();
        // How many changes the lists have that aren't in the game yet.
        private readonly Label changesLabel = new Label();
        private StatusLine statusLine;
        // Set while states are put into the lists, so that doesn't count as an edit.
        private bool updating;

        // The gadgets as last read from the game, and as the lists have them; null until read.
        private byte[] gameStates;
        private byte[] states;

        public Sly4GadgetsForm(sly4 game, Sly4JobQueue jobQueue)
        {
            this.game = game;
            this.jobQueue = jobQueue;
            Name = "Sly4GadgetsForm";
            Text = "SluMAN :: Gadgets";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(runFileWarningLabel.Right + 8, runFileGroupBox.Bottom + 8);
            statusLine = new StatusLine(this, false);

            RefreshFromGame();
        }

        private void BuildControls()
        {
            ToolTip tip = new ToolTip();
            FormClosed += (sender, e) => tip.Dispose();

            int listsBottom = 0;
            for (int column = 0; column < Sly4Gadgets.CharacterNames.Length; column++)
            {
                string character = Sly4Gadgets.CharacterNames[column];
                int x = 8 + column * (ColumnWidth + ColumnGap);

                CheckBox allCheckBox = new CheckBox();
                allCheckBox.Text = character;
                allCheckBox.Font = new Font(Font, FontStyle.Bold);
                allCheckBox.AutoSize = true;
                allCheckBox.Location = new Point(x + 2, 8);
                allCheckBox.CheckedChanged += (sender, e) => SetCharacter(character, allCheckBox.Checked);
                Controls.Add(allCheckBox);
                allCheckBoxes[character] = allCheckBox;
                tip.SetToolTip(allCheckBox, $"Ticks or unticks every one of {character}'s gadgets.");

                CheckedListBox list = new CheckedListBox();
                list.Location = new Point(x, ListTop);
                list.CheckOnClick = true;
                list.IntegralHeight = false;
                foreach (Sly4Gadgets.Gadget gadget in Sly4Gadgets.All)
                {
                    if (gadget.character == character)
                    {
                        list.Items.Add(gadget);
                    }
                }
                list.ItemCheck += list_ItemCheck;
                // Sized once on the form: the item height follows the font it takes from it.
                Controls.Add(list);
                list.Size = new Size(ColumnWidth, list.ItemHeight * list.Items.Count + 4);
                lists[character] = list;
                listsBottom = Math.Max(listsBottom, list.Bottom);
            }

            // Below Carmelita's list.
            CheckedListBox lastList = lists[Sly4Gadgets.CharacterNames[Sly4Gadgets.CharacterNames.Length - 1]];
            Button applyButton = AddButton("Apply", lastList.Left, lastList.Bottom + 10, (sender, e) => Apply());
            tip.SetToolTip(applyButton, "Writes the changes to the game and reloads so they take effect: the checkpoint in a job, otherwise the level from its start.");
            Button discardButton = AddButton("Discard", lastList.Left, applyButton.Bottom + 4, (sender, e) => ReadGame(false));
            tip.SetToolTip(discardButton, "Drops the changes not applied and reads the gadgets from the game again.");

            changesLabel.Location = new Point(lastList.Left, discardButton.Bottom + 6);
            changesLabel.Size = new Size(ColumnWidth, 44);
            changesLabel.ForeColor = Color.DarkOrange;
            Controls.Add(changesLabel);

            runFileGroupBox.Text = "Run file gadgets";
            runFileGroupBox.Location = new Point(8, Math.Max(listsBottom, changesLabel.Bottom) + 8);
            runFileGroupBox.Size = new Size(312, 54);
            Controls.Add(runFileGroupBox);

            runFileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runFileComboBox.Location = new Point(10, 20);
            runFileComboBox.Size = new Size(120, 21);
            runFileComboBox.Items.AddRange(Sly4Gadgets.RunFiles);
            runFileComboBox.SelectedIndex = 0;
            runFileComboBox.SelectedIndexChanged += (sender, e) => runFileWarningLabel.Text = "";
            runFileGroupBox.Controls.Add(runFileComboBox);

            Button loadButton = new Button();
            loadButton.Text = "Load";
            loadButton.Location = new Point(136, 19);
            loadButton.Size = new Size(80, 23);
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += loadRunFileButton_Click;
            runFileGroupBox.Controls.Add(loadButton);
            tip.SetToolTip(loadButton, "Puts the gadgets saved for this run file in the lists. Apply to use them in game.");

            Button saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(222, 19);
            saveButton.Size = new Size(80, 23);
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += saveRunFileButton_Click;
            runFileGroupBox.Controls.Add(saveButton);
            tip.SetToolTip(saveButton, "Saves the lists' gadgets as this run file's. Gadgets that aren't in ThiefNet yet at the start of its episode are left out.");

            runFileWarningLabel.Location = new Point(328, runFileGroupBox.Top + 6);
            runFileWarningLabel.Size = new Size(lastList.Right - 328, runFileGroupBox.Height - 6);
            runFileWarningLabel.ForeColor = Color.Firebrick;
            Controls.Add(runFileWarningLabel);
        }

        private Button AddButton(string text, int x, int y, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(ColumnWidth, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            Controls.Add(button);
            return button;
        }

        /// <summary>Reads the gadgets from the game, keeping the changes not applied yet.</summary>
        public void RefreshFromGame()
        {
            ReadGame(true);
        }

        private void ReadGame(bool keepChanges)
        {
            byte[] read;
            try
            {
                read = game.GetGadgetStates();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the gadgets: {ex.Message}");
                return;
            }

            if (keepChanges && states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i] == gameStates[i])
                    {
                        states[i] = read[i];
                    }
                }
            }
            else
            {
                states = (byte[])read.Clone();
            }
            gameStates = read;
            ShowStates();
        }

        /// <summary>Ticks the bought gadgets and counts the changes.</summary>
        private void ShowStates()
        {
            updating = true;
            foreach (CheckedListBox list in lists.Values)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)list.Items[i];
                    list.SetItemChecked(i, Sly4Gadgets.IsPurchased(states[gadget.index]));
                }
            }
            ShowAllCheckBoxes();
            updating = false;
            ShowChanges();
        }

        /// <summary>Ticks each character's box when all their gadgets are ticked.</summary>
        private void ShowAllCheckBoxes()
        {
            bool wasUpdating = updating;
            updating = true;
            foreach (KeyValuePair<string, CheckedListBox> entry in lists)
            {
                allCheckBoxes[entry.Key].Checked = entry.Value.CheckedItems.Count == entry.Value.Items.Count;
            }
            updating = wasUpdating;
        }

        private void ShowChanges()
        {
            int changes = 0;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] != gameStates[i])
                {
                    changes++;
                }
            }
            changesLabel.Text = changes == 0 ? ""
                : $"{changes} {(changes == 1 ? "change" : "changes")} not in game yet. Apply reloads to use them.";
        }

        /// <summary>Whether the gadgets have been read, so the lists can be changed.</summary>
        private bool HasStates()
        {
            if (states == null)
            {
                statusLine.Error("The gadgets haven't been read from the game. Click Discard to read them.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// A gadget's state when ticked (bought) or not: as the game has it if that matches, so
        /// ticking it back is no change; otherwise bought, or back in ThiefNet.
        /// </summary>
        private byte StateFor(Sly4Gadgets.Gadget gadget, bool purchased)
        {
            byte inGame = gameStates[gadget.index];
            if (Sly4Gadgets.IsPurchased(inGame) == purchased)
            {
                return inGame;
            }
            return purchased ? Sly4Gadgets.Owned : Sly4Gadgets.InThiefNet;
        }

        private void list_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (updating || states == null)
            {
                return;
            }
            CheckedListBox list = (CheckedListBox)sender;
            Sly4Gadgets.Gadget gadget = (Sly4Gadgets.Gadget)list.Items[e.Index];
            states[gadget.index] = StateFor(gadget, e.NewValue == CheckState.Checked);
            // The item is only ticked once this returns.
            BeginInvoke((MethodInvoker)(() =>
            {
                ShowAllCheckBoxes();
                ShowChanges();
            }));
        }

        private void SetCharacter(string character, bool purchased)
        {
            if (updating || !HasStates())
            {
                return;
            }
            foreach (Sly4Gadgets.Gadget gadget in lists[character].Items)
            {
                states[gadget.index] = StateFor(gadget, purchased);
            }
            ShowStates();
        }

        /// <summary>Writes the lists' changes to the game and reloads so they take effect.</summary>
        private void Apply()
        {
            if (!HasStates())
            {
                return;
            }
            try
            {
                // Only the changed gadgets, over what the game has now: it may have changed others
                // since they were read.
                byte[] current = game.GetGadgetStates();
                int changes = 0;
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i] != gameStates[i])
                    {
                        current[i] = states[i];
                        changes++;
                    }
                }
                if (changes == 0)
                {
                    statusLine.Info("There are no changes to apply.");
                    return;
                }

                game.SetGadgetStates(current);
                gameStates = current;
                states = (byte[])current.Clone();
                ShowStates();
                statusLine.Info($"Applied {changes} {(changes == 1 ? "change" : "changes")}; reloading.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't apply the gadgets: {ex.Message}");
                return;
            }
            jobQueue.ReloadForGadgets();
        }

        private void loadRunFileButton_Click(object sender, EventArgs e)
        {
            if (!HasStates())
            {
                return;
            }
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                byte[] saved = Sly4Gadgets.LoadRunFile(runFile);
                if (saved == null)
                {
                    statusLine.Error($"No gadgets are saved for {runFile}. Set them here and click Save.");
                    return;
                }
                states = saved;
                ShowStates();
                runFileWarningLabel.Text = "";
                statusLine.Info($"Put {runFile}'s gadgets in the lists. Apply to use them in game.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load {runFile}'s gadgets: {ex.Message}");
            }
        }

        private void saveRunFileButton_Click(object sender, EventArgs e)
        {
            if (!HasStates())
            {
                return;
            }
            string runFile = runFileComboBox.SelectedItem.ToString();
            try
            {
                // Locked in the lists too, so they show what was saved.
                List<string> removed = Sly4Gadgets.RemoveUnavailable(runFile, states);
                Sly4Gadgets.SaveRunFile(runFile, states);
                ShowStates();
                runFileWarningLabel.Text = GadgetAvailability.RemovedWarning(runFile, removed);
                statusLine.Info($"Saved the lists' gadgets as {runFile}'s.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save {runFile}'s gadgets: {ex.Message}");
            }
        }
    }
}
