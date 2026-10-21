using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The GUI tab: the game's GUI tree as it is now (every screen and the parts inside it), and
    /// hiding or showing any part with the game's own show method. Names come from the game's
    /// strings; a part whose name isn't among them shows its hash. After each change the tab
    /// lists the part's bytes that changed, to find where the game keeps what it shows. The
    /// controls are built in code.
    /// </summary>
    public class Sly4GuiForm : Form, IRefreshOnShow
    {
        // How much of a part is compared before and after a change.
        private const uint CompareLength = 0x200;

        private readonly sly4 game;
        private readonly TreeView tree = new TreeView();
        private readonly TextBox searchBox = new TextBox();
        private readonly TextBox changesBox = new TextBox();
        private readonly Label countLabel = new Label();
        private readonly HashSet<uint> hiddenNodes = new HashSet<uint>();
        private Dictionary<uint, string> names;
        private List<sly4.GuiNode> roots = new List<sly4.GuiNode>();
        private StatusLine statusLine;

        public Sly4GuiForm(sly4 game)
        {
            this.game = game;
            Name = "Sly4GuiForm";
            Text = "SluMAN :: GUI";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(tree.Right + 8, changesBox.Bottom + 8);
            // Grow with the tab in the fixed-size Practice window. Anchored once the size is set:
            // an anchor keeps the distance to the window's edge it has when set.
            tree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            changesBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLine = new StatusLine(this, false);
            // Leave the game's GUI as it was.
            FormClosing += (sender, e) =>
            {
                foreach (uint node in hiddenNodes)
                {
                    try { game.SetGuiNodeShown(node, true); } catch { }
                }
            };

            LoadTree();
        }

        private void BuildControls()
        {
            Label searchLabel = new Label();
            searchLabel.Text = "Search:";
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(8, 12);
            Controls.Add(searchLabel);

            searchBox.Location = new Point(56, 9);
            searchBox.Size = new Size(160, 20);
            searchBox.TextChanged += (sender, e) => ShowTree();
            Controls.Add(searchBox);

            AddButton("Hide", 224, 60, (sender, e) => SetSelectedShown(false));
            AddButton("Show", 288, 60, (sender, e) => SetSelectedShown(true));
            AddButton("Reload", 352, 60, (sender, e) => LoadTree());

            countLabel.AutoSize = true;
            countLabel.Location = new Point(420, 12);
            Controls.Add(countLabel);

            tree.Location = new Point(8, 38);
            tree.Size = new Size(620, 360);
            tree.HideSelection = false;
            tree.Font = new Font("Consolas", 9F);
            tree.NodeMouseDoubleClick += (sender, e) => SetSelectedShown(hiddenNodes.Contains(SelectedAddress()));
            Controls.Add(tree);

            changesBox.Location = new Point(8, 404);
            changesBox.Size = new Size(620, 64);
            changesBox.Multiline = true;
            changesBox.ReadOnly = true;
            changesBox.ScrollBars = ScrollBars.Vertical;
            changesBox.Font = new Font("Consolas", 8.25F);
            changesBox.Text = "Select a part and press Hide or Show (or double-click it). The bytes of the part that changed show here.";
            Controls.Add(changesBox);
        }

        private void AddButton(string text, int x, int width, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, 8);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            Controls.Add(button);
        }

        public void RefreshFromGame()
        {
            // The tree is read on demand (Reload): it can be thousands of parts.
        }

        private void LoadTree()
        {
            try
            {
                if (names == null)
                {
                    names = game.ReadGuiNames();
                }
                roots = game.ReadGuiTree(names);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the GUI: {ex.Message}");
                return;
            }
            ShowTree();
        }

        /// <summary>Fills the tree, keeping only parts whose name (or a child's) contains the search.</summary>
        private void ShowTree()
        {
            string search = searchBox.Text.Trim();
            int count = 0;
            tree.BeginUpdate();
            tree.Nodes.Clear();
            foreach (sly4.GuiNode root in roots)
            {
                TreeNode node = BuildNode(root, search, ref count);
                if (node != null)
                {
                    tree.Nodes.Add(node);
                }
            }
            if (search != "")
            {
                tree.ExpandAll();
            }
            tree.EndUpdate();
            countLabel.Text = $"{count} parts";
        }

        private TreeNode BuildNode(sly4.GuiNode part, string search, ref int count)
        {
            List<TreeNode> children = new List<TreeNode>();
            foreach (sly4.GuiNode child in part.children)
            {
                TreeNode childNode = BuildNode(child, search, ref count);
                if (childNode != null)
                {
                    children.Add(childNode);
                }
            }

            bool matches = search == "" || (part.name != null && part.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!matches && children.Count == 0)
            {
                return null;
            }
            count++;
            TreeNode node = new TreeNode(PartLabel(part));
            node.Tag = part;
            node.Nodes.AddRange(children.ToArray());
            if (hiddenNodes.Contains(part.address))
            {
                node.ForeColor = Color.Firebrick;
            }
            return node;
        }

        private string PartLabel(sly4.GuiNode part)
        {
            string name = part.name != null ? part.name : $"0x{part.hash:X8}";
            string label = $"{name}  @{part.address:X8}";
            if (hiddenNodes.Contains(part.address))
            {
                label += "  (hidden by SluMAN)";
            }
            return label;
        }

        private uint SelectedAddress()
        {
            sly4.GuiNode part = tree.SelectedNode == null ? null : tree.SelectedNode.Tag as sly4.GuiNode;
            return part == null ? 0 : part.address;
        }

        private void SetSelectedShown(bool shown)
        {
            sly4.GuiNode part = tree.SelectedNode == null ? null : tree.SelectedNode.Tag as sly4.GuiNode;
            if (part == null)
            {
                statusLine.Error("Select a part first.");
                return;
            }

            try
            {
                byte[] before = game.ReadGuiNodeBytes(part.address, CompareLength);
                game.SetGuiNodeShown(part.address, shown);
                byte[] after = game.ReadGuiNodeBytes(part.address, CompareLength);
                ShowChanges(part, shown, before, after);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change {PartLabel(part)}: {ex.Message}");
                return;
            }

            if (shown)
            {
                hiddenNodes.Remove(part.address);
            }
            else
            {
                hiddenNodes.Add(part.address);
            }
            tree.SelectedNode.Text = PartLabel(part);
            tree.SelectedNode.ForeColor = shown ? SystemColors.WindowText : Color.Firebrick;
            statusLine.Info($"{(shown ? "Showed" : "Hid")} {(part.name != null ? part.name : $"0x{part.hash:X8}")}.");
        }

        /// <summary>Lists the bytes the show method changed, to find where the game keeps it.</summary>
        private void ShowChanges(sly4.GuiNode part, bool shown, byte[] before, byte[] after)
        {
            StringBuilder text = new StringBuilder();
            text.Append($"{(shown ? "Show" : "Hide")} {(part.name != null ? part.name : $"0x{part.hash:X8}")} @{part.address:X8}: ");
            int changes = 0;
            for (int i = 0; i < before.Length && i < after.Length; i++)
            {
                if (before[i] != after[i])
                {
                    text.Append($"+0x{i:X}: {before[i]:X2} -> {after[i]:X2}   ");
                    changes++;
                }
            }
            if (changes == 0)
            {
                text.Append($"nothing changed in the first 0x{CompareLength:X} bytes.");
            }
            changesBox.Text = text.ToString();
        }
    }
}
