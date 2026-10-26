using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Sly 1's powerups (thief moves), like Sly 2-4's Gadgets tab: which are unlocked (the vault
    /// powerups in the order vaults give them, Mz. Ruby's Invisibility and the worlds' blueprints)
    /// and which one is equipped. Ticking only changes the lists; Save writes them to the game,
    /// Discard reads them again. Bits SluMAN doesn't list are kept. The controls are built in code.
    /// </summary>
    public class Sly1PowerupsForm : Form, IRefreshOnShow
    {
        private readonly sly1 game;
        private readonly StatusLine statusLine;

        private readonly CheckedListBox vaultCheckedList = new CheckedListBox();
        private readonly CheckedListBox otherCheckedList = new CheckedListBox();
        private readonly CheckBox toggleAllCheckBox = new CheckBox();
        private readonly ComboBox equippedComboBox = new ComboBox();
        private Label noteLabel;

        // Set while the lists are filled from the game, so that isn't taken as an edit.
        private bool loading;

        public Sly1PowerupsForm(sly1 game, StatusLine statusLine)
        {
            this.game = game;
            this.statusLine = statusLine;
            Name = "Sly1PowerupsForm";
            Text = "SluMAN :: Powerups";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(652, noteLabel.Bottom + 8);
            LoadPowerups();
        }

        private void BuildControls()
        {
            GroupBox vaultGroupBox = AddGroup("Vault Powerups", 8, 8, 312, 286);
            for (int i = 0; i < sly1.VaultPowerups.Length; i++)
            {
                vaultCheckedList.Items.Add($"{i + 1}. {sly1.VaultPowerups[i].name}");
            }
            SetUpList(vaultCheckedList, vaultGroupBox, sly1.VaultPowerups.Length);

            toggleAllCheckBox.Text = "Check / Uncheck All";
            toggleAllCheckBox.AutoSize = true;
            toggleAllCheckBox.Location = new Point(12, vaultCheckedList.Bottom + 8);
            toggleAllCheckBox.Click += (sender, e) => ToggleAll(toggleAllCheckBox.Checked);
            vaultGroupBox.Controls.Add(toggleAllCheckBox);

            GroupBox otherGroupBox = AddGroup("Other Unlocks", 332, 8, 312, 108);
            foreach (sly1.Powerup powerup in sly1.OtherPowerups)
            {
                otherCheckedList.Items.Add(powerup.name);
            }
            SetUpList(otherCheckedList, otherGroupBox, sly1.OtherPowerups.Length);

            GroupBox equippedGroupBox = AddGroup("Equipped", 332, otherGroupBox.Bottom + 6, 312, 54);
            AddLabel(equippedGroupBox, "Triangle:", 10, 23);
            equippedComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            equippedComboBox.Location = new Point(70, 20);
            equippedComboBox.Size = new Size(232, 21);
            equippedGroupBox.Controls.Add(equippedComboBox);

            GroupBox saveGroupBox = AddGroup("Game", 332, equippedGroupBox.Bottom + 6, 312, 54);
            AddButton(saveGroupBox, "Save", 10, 19, 143, (sender, e) => SavePowerups());
            AddButton(saveGroupBox, "Discard Changes", 159, 19, 143, (sender, e) =>
            {
                LoadPowerups();
                statusLine.Info("Read the powerups from the game again.");
            });

            noteLabel = new Label();
            noteLabel.Text = "The Nth vault opened gives the Nth vault powerup, whichever level it's in.\n" +
                "A powerup only works where the level allows it, even when unlocked.";
            noteLabel.Location = new Point(10, vaultGroupBox.Bottom + 6);
            noteLabel.Size = new Size(632, 28);
            Controls.Add(noteLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(equippedComboBox, "The powerup Triangle uses. Only unlocked ones are listed; L2 / R2 change it in the game.");
            FormClosed += (sender, e) => tip.Dispose();
        }

        private void SetUpList(CheckedListBox list, GroupBox group, int rows)
        {
            list.CheckOnClick = true;
            list.IntegralHeight = false;
            list.Location = new Point(10, 20);
            list.Size = new Size(group.Width - 20, rows * list.ItemHeight + 6);
            // The equipped list offers only what's ticked, so it follows the lists.
            list.ItemCheck += (sender, e) =>
            {
                if (!loading)
                {
                    BeginInvoke((MethodInvoker)(() => FillEquipped(SelectedEquipped())));
                }
            };
            group.Controls.Add(list);
        }

        private GroupBox AddGroup(string text, int x, int y, int width, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            Controls.Add(group);
            return group;
        }

        private static void AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            parent.Controls.Add(button);
        }

        private static void AddLabel(Control parent, string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            parent.Controls.Add(label);
        }

        private void ToggleAll(bool on)
        {
            loading = true;
            for (int i = 0; i < vaultCheckedList.Items.Count; i++)
            {
                vaultCheckedList.SetItemChecked(i, on);
            }
            loading = false;
            FillEquipped(SelectedEquipped());
        }

        /// <summary>The bits the lists have ticked.</summary>
        private int CheckedBits()
        {
            int bits = 0;
            for (int i = 0; i < sly1.VaultPowerups.Length; i++)
            {
                if (vaultCheckedList.GetItemChecked(i))
                {
                    bits |= sly1.VaultPowerups[i].bit;
                }
            }
            for (int i = 0; i < sly1.OtherPowerups.Length; i++)
            {
                if (otherCheckedList.GetItemChecked(i))
                {
                    bits |= sly1.OtherPowerups[i].bit;
                }
            }
            return bits;
        }

        /// <summary>Every bit the lists show.</summary>
        private static int ListedBits()
        {
            int bits = 0;
            foreach (sly1.Powerup powerup in sly1.VaultPowerups)
            {
                bits |= powerup.bit;
            }
            foreach (sly1.Powerup powerup in sly1.OtherPowerups)
            {
                bits |= powerup.bit;
            }
            return bits;
        }

        private class EquippedEntry
        {
            public int powerup;

            public override string ToString()
            {
                return powerup == sly1.NoPowerup ? "None" : sly1.EquippablePowerups[powerup];
            }
        }

        private int SelectedEquipped()
        {
            EquippedEntry entry = equippedComboBox.SelectedItem as EquippedEntry;
            return entry == null ? sly1.NoPowerup : entry.powerup;
        }

        /// <summary>Lists None and the equippable powerups the lists have unlocked, selecting <paramref name="select"/> if it's there.</summary>
        private void FillEquipped(int select)
        {
            int bits = CheckedBits();
            equippedComboBox.Items.Clear();
            equippedComboBox.Items.Add(new EquippedEntry { powerup = sly1.NoPowerup });
            equippedComboBox.SelectedIndex = 0;
            for (int i = 0; i < sly1.EquippablePowerups.Length; i++)
            {
                if ((bits & sly1.EquippablePowerupBits[i]) == 0)
                {
                    continue;
                }
                int index = equippedComboBox.Items.Add(new EquippedEntry { powerup = i });
                if (i == select)
                {
                    equippedComboBox.SelectedIndex = index;
                }
            }
        }

        private void LoadPowerups()
        {
            int bits;
            int equipped;
            try
            {
                bits = game.GetPowerups();
                equipped = game.GetEquippedPowerup();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the powerups: {ex.Message}");
                return;
            }

            loading = true;
            for (int i = 0; i < sly1.VaultPowerups.Length; i++)
            {
                vaultCheckedList.SetItemChecked(i, (bits & sly1.VaultPowerups[i].bit) != 0);
            }
            for (int i = 0; i < sly1.OtherPowerups.Length; i++)
            {
                otherCheckedList.SetItemChecked(i, (bits & sly1.OtherPowerups[i].bit) != 0);
            }
            toggleAllCheckBox.Checked = vaultCheckedList.CheckedItems.Count == vaultCheckedList.Items.Count;
            loading = false;
            FillEquipped(equipped);
        }

        private void SavePowerups()
        {
            try
            {
                int bits = (game.GetPowerups() & ~ListedBits()) | CheckedBits();
                game.SetPowerups(bits);
                int equipped = SelectedEquipped();
                if (equipped != game.GetEquippedPowerup())
                {
                    game.SetEquippedPowerup(equipped);
                }
                statusLine.Info("Saved the powerups.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't save the powerups: {ex.Message}");
            }
        }

        public void RefreshFromGame()
        {
            LoadPowerups();
        }
    }
}
