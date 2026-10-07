using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace racman
{
    public partial class InputDisplay : Form
    {
        public System.Windows.Forms.Timer timer;
        ControllerSkin controllerSkin;
        private readonly List<ToolStripMenuItem> skinMenuItems = new List<ToolStripMenuItem>();

        public InputDisplay()
        {
            InitializeComponent();
        }
        private void InputDisplay_Load(object sender, EventArgs e)
        {
            foreach (string skinName in ControllerSkin.SkinNames())
            {
                skinComboBox.Items.Add(skinName);
            }

            BuildSkinContextMenu();
            BuildObsMenu();

            // controllerSkin = ControllerSkin.Load(Directory.EnumerateDirectories("controllerskins").First());
            try
            {
                skinComboBox.SelectedIndex = Convert.ToInt32(func.GetConfigData("config.txt", "InputDisplaySkin"));
            }
            catch
            {
                skinComboBox.SelectedIndex = 0;
            }

            var savedBackColor = func.GetConfigData("config.txt", "InputDisplayBackColor");
            if (savedBackColor != "")
            {
                try
                {
                    this.BackColor = Color.FromArgb(Convert.ToInt32(savedBackColor));
                }
                catch
                {
                    // Ignore invalid color values in config.
                }
            }

            timer = new System.Windows.Forms.Timer();
            timer.Interval = (int)16.66667;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        public void timer_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private GraphicsUnit units = GraphicsUnit.Pixel;
        private void InputDisplay_Paint(object sender, PaintEventArgs e)
        {
            Image sprite = controllerSkin.image;

            InputPlot basePlot = controllerSkin.buttons["base"];
            InputPlot r3 = controllerSkin.buttons["r3"];
            InputPlot r3Press = controllerSkin.buttons["r3Press"];
            InputPlot l3 = controllerSkin.buttons["l3"];
            InputPlot l3Press = controllerSkin.buttons["l3Press"];

            InputPlot dpadLeft = controllerSkin.buttons["dpadLeft"];
            InputPlot dpadRight = controllerSkin.buttons["dpadRight"];
            InputPlot dpadDown = controllerSkin.buttons["dpadDown"];
            InputPlot dpadUp = controllerSkin.buttons["dpadUp"];

            InputPlot cross = controllerSkin.buttons["cross"];
            InputPlot circle = controllerSkin.buttons["circle"];
            InputPlot triangle = controllerSkin.buttons["triangle"];
            InputPlot square = controllerSkin.buttons["square"];

            InputPlot select = controllerSkin.buttons["select"];
            InputPlot start = controllerSkin.buttons["start"];

            InputPlot r1 = controllerSkin.buttons["r1"];
            InputPlot l1 = controllerSkin.buttons["l1"];
            InputPlot l2 = controllerSkin.buttons["l2"];
            InputPlot r2 = controllerSkin.buttons["r2"];

            e.Graphics.DrawImage(sprite, basePlot.drawX, basePlot.drawY, new Rectangle(basePlot.spriteX, basePlot.spriteY, basePlot.spriteWidth, basePlot.spriteHeight), units);

            if (Inputs.Mask.Contains(Inputs.Buttons.r3)) e.Graphics.DrawImage(sprite, r3.drawX + (Inputs.rx * controllerSkin.analogPitch), r3.drawY + (Inputs.ry * controllerSkin.analogPitch), new Rectangle(r3.spriteX, r3.spriteY, r3.spriteWidth, r3.spriteHeight), units); 
            else e.Graphics.DrawImage(sprite, r3Press.drawX + (Inputs.rx * controllerSkin.analogPitch), r3Press.drawY + (Inputs.ry * controllerSkin.analogPitch), new Rectangle(r3Press.spriteX, r3Press.spriteY, r3Press.spriteWidth, r3Press.spriteHeight), units); 
            if (Inputs.Mask.Contains(Inputs.Buttons.l3)) e.Graphics.DrawImage(sprite, l3.drawX + (Inputs.lx * controllerSkin.analogPitch), l3.drawY + (Inputs.ly * controllerSkin.analogPitch), new Rectangle(l3.spriteX, l3.spriteY, l3.spriteWidth, l3.spriteHeight), units); 
            else e.Graphics.DrawImage(sprite, l3Press.drawX + (Inputs.lx * controllerSkin.analogPitch), l3Press.drawY + (Inputs.ly * controllerSkin.analogPitch), new Rectangle(l3Press.spriteX, l3Press.spriteY, l3Press.spriteWidth, l3Press.spriteHeight), units); 

            if (Inputs.Mask.Contains(Inputs.Buttons.left)) e.Graphics.DrawImage(sprite, dpadLeft.drawX, dpadLeft.drawY, new Rectangle(dpadLeft.spriteX, dpadLeft.spriteY, dpadLeft.spriteWidth, dpadLeft.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.right)) e.Graphics.DrawImage(sprite, dpadRight.drawX, dpadRight.drawY, new Rectangle(dpadRight.spriteX, dpadRight.spriteY, dpadRight.spriteWidth, dpadRight.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.down)) e.Graphics.DrawImage(sprite, dpadDown.drawX, dpadDown.drawY, new Rectangle(dpadDown.spriteX, dpadDown.spriteY, dpadDown.spriteWidth, dpadDown.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.up)) e.Graphics.DrawImage(sprite, dpadUp.drawX, dpadUp.drawY, new Rectangle(dpadUp.spriteX, dpadUp.spriteY, dpadUp.spriteWidth, dpadUp.spriteHeight), units);

            if (Inputs.Mask.Contains(Inputs.Buttons.cross)) e.Graphics.DrawImage(sprite, cross.drawX, cross.drawY, new Rectangle(cross.spriteX, cross.spriteY, cross.spriteWidth, cross.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.circle)) e.Graphics.DrawImage(sprite, circle.drawX, circle.drawY, new Rectangle(circle.spriteX, circle.spriteY, circle.spriteWidth, circle.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.triangle)) e.Graphics.DrawImage(sprite, triangle.drawX, triangle.drawY, new Rectangle(triangle.spriteX, triangle.spriteY, triangle.spriteWidth, triangle.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.square)) e.Graphics.DrawImage(sprite, square.drawX, square.drawY, new Rectangle(square.spriteX, square.spriteY, triangle.spriteWidth, triangle.spriteHeight), units);

            if (Inputs.Mask.Contains(Inputs.Buttons.select)) e.Graphics.DrawImage(sprite, select.drawX, select.drawY, new Rectangle(select.spriteX, select.spriteY, select.spriteWidth, select.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.start)) e.Graphics.DrawImage(sprite, start.drawX, start.drawY, new Rectangle(start.spriteX, start.spriteY, start.spriteWidth, start.spriteHeight), units);

            if (Inputs.Mask.Contains(Inputs.Buttons.r1)) e.Graphics.DrawImage(sprite, r1.drawX, r1.drawY, new Rectangle(r1.spriteX, r1.spriteY, r1.spriteWidth, r1.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.l1)) e.Graphics.DrawImage(sprite, l1.drawX, l1.drawY, new Rectangle(l1.spriteX, l1.spriteY, l1.spriteWidth, l1.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.l2)) e.Graphics.DrawImage(sprite, l2.drawX, l2.drawY, new Rectangle(l2.spriteX, l2.spriteY, l2.spriteWidth, l2.spriteHeight), units);
            if (Inputs.Mask.Contains(Inputs.Buttons.r2)) e.Graphics.DrawImage(sprite, r2.drawX, r2.drawY, new Rectangle(r2.spriteX, r2.spriteY, r2.spriteWidth, r2.spriteHeight), units);
        }

        private void skinComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySkinByIndex(skinComboBox.SelectedIndex);
        }

        private void InputDisplay_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Enabled = false;
        }

        private void BuildSkinContextMenu()
        {
            skinToolStripMenuItem.DropDownItems.Clear();
            skinMenuItems.Clear();

            for (int i = 0; i < skinComboBox.Items.Count; i++)
            {
                var skinName = skinComboBox.Items[i].ToString();
                var menuItem = new ToolStripMenuItem(skinName)
                {
                    Tag = i,
                    CheckOnClick = true
                };

                menuItem.Click += SkinMenuItem_Click;
                skinToolStripMenuItem.DropDownItems.Add(menuItem);
                skinMenuItems.Add(menuItem);
            }
        }

        private void ApplySkinByIndex(int skinIndex)
        {
            if (skinIndex < 0 || skinIndex >= skinComboBox.Items.Count)
            {
                return;
            }

            var skinName = skinComboBox.Items[skinIndex].ToString();

            controllerSkin = ControllerSkin.Load(skinName);
            ObsPadServer.SelectedSkin = skinName;

            func.ChangeFileLines("config.txt", skinIndex.ToString(), "InputDisplaySkin");

            this.Width = Math.Max(controllerSkin.buttons["base"].spriteWidth + 50, this.Width);
            this.Height = Math.Max(controllerSkin.buttons["base"].spriteHeight + 50, this.Height);

            for (int i = 0; i < skinMenuItems.Count; i++)
            {
                skinMenuItems[i].Checked = i == skinIndex;
            }
        }

        private void SkinMenuItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem clickedItem) || clickedItem.Tag == null)
            {
                return;
            }

            int skinIndex = (int)clickedItem.Tag;
            skinComboBox.SelectedIndex = skinIndex;
        }

        private void InputDisplay_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(this, e.Location);
            }
        }

        private void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = this.BackColor;

            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                this.BackColor = colorDialog1.Color;
                func.ChangeFileLines("config.txt", this.BackColor.ToArgb().ToString(), "InputDisplayBackColor");
            }
        }

        // --- OBS Browser Source ---

        private ToolStripMenuItem serveForObsMenuItem;
        private ToolStripMenuItem copyObsUrlMenuItem;
        private ToolStripMenuItem changeObsPortMenuItem;

        private void BuildObsMenu()
        {
            serveForObsMenuItem = new ToolStripMenuItem("Serve for OBS");
            serveForObsMenuItem.Click += serveForObsMenuItem_Click;
            copyObsUrlMenuItem = new ToolStripMenuItem("Copy OBS URL");
            copyObsUrlMenuItem.Click += copyObsUrlMenuItem_Click;
            changeObsPortMenuItem = new ToolStripMenuItem("Change OBS Port...");
            changeObsPortMenuItem.Click += changeObsPortMenuItem_Click;

            contextMenuStrip1.Items.Add(new ToolStripSeparator());
            contextMenuStrip1.Items.Add(serveForObsMenuItem);
            contextMenuStrip1.Items.Add(copyObsUrlMenuItem);
            contextMenuStrip1.Items.Add(changeObsPortMenuItem);
            contextMenuStrip1.Opening += (sender, e) => UpdateObsMenu();
        }

        private void UpdateObsMenu()
        {
            serveForObsMenuItem.Checked = ObsPadServer.Enabled;
            copyObsUrlMenuItem.Enabled = ObsPadServer.IsRunning;
            copyObsUrlMenuItem.ToolTipText = ObsPadServer.IsRunning ? ObsPadServer.Url : ObsPadServer.LastError;
        }

        /// <summary>The size to give the Browser Source: the skin's base image.</summary>
        private string ObsSourceSize()
        {
            InputPlot basePlot;
            if (controllerSkin != null && controllerSkin.buttons.TryGetValue("base", out basePlot))
            {
                return $"{basePlot.spriteWidth} x {basePlot.spriteHeight}";
            }
            return "the skin's size";
        }

        private void serveForObsMenuItem_Click(object sender, EventArgs e)
        {
            bool enable = !ObsPadServer.Enabled;
            ObsPadServer.Enabled = enable;
            if (!enable)
            {
                func.Status("Stopped serving the input display for OBS.");
            }
            else if (ObsPadServer.IsRunning)
            {
                func.Status($"Serving the input display for OBS at {ObsPadServer.Url}.");
            }
            else
            {
                func.Status(ObsPadServer.LastError, true);
            }
        }

        private void copyObsUrlMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(ObsPadServer.Url);
                func.Status($"Copied {ObsPadServer.Url}. In OBS, add a Browser Source with that URL and size {ObsSourceSize()}.");
            }
            catch
            {
                func.Status($"Couldn't copy to the clipboard. The URL is {ObsPadServer.Url}", true);
            }
        }

        private void changeObsPortMenuItem_Click(object sender, EventArgs e)
        {
            SimpleInputDialogForm dialog = new SimpleInputDialogForm("OBS port", ObsPadServer.Port.ToString());
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            int port;
            if (!int.TryParse(dialog.inputTextBox.Text.Trim(), out port) || port < 1024 || port > 65535)
            {
                func.Status("Enter a port between 1024 and 65535.", true);
                return;
            }

            if (ObsPadServer.ChangePort(port))
            {
                func.Status($"The OBS input display is now at {ObsPadServer.Url}. Update the URL in OBS.");
            }
            else
            {
                func.Status(ObsPadServer.LastError, true);
            }
        }
    }
}
