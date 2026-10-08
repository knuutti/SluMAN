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

        // What was last drawn, so the window only repaints when the pad changes.
        private int drawnInputs = -1;
        private float drawnLx, drawnLy, drawnRx, drawnRy;

        public void timer_Tick(object sender, EventArgs e)
        {
            int inputs = Inputs.RawInputs;
            if (inputs == drawnInputs && Inputs.lx == drawnLx && Inputs.ly == drawnLy && Inputs.rx == drawnRx && Inputs.ry == drawnRy)
            {
                return;
            }
            // Invalidate instead of Refresh: Windows paints when it's ready instead of right away.
            this.Invalidate();
        }

        // Skin parts drawn while their button is held. The sticks are drawn separately.
        private static readonly KeyValuePair<Inputs.Buttons, string>[] buttonParts =
        {
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.left, "dpadLeft"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.right, "dpadRight"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.down, "dpadDown"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.up, "dpadUp"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.cross, "cross"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.circle, "circle"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.triangle, "triangle"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.square, "square"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.select, "select"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.start, "start"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.r1, "r1"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.l1, "l1"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.l2, "l2"),
            new KeyValuePair<Inputs.Buttons, string>(Inputs.Buttons.r2, "r2"),
        };

        private const GraphicsUnit units = GraphicsUnit.Pixel;

        private void DrawPart(Graphics graphics, Image sprite, string part, float offsetX = 0, float offsetY = 0)
        {
            InputPlot plot = controllerSkin.buttons[part];
            graphics.DrawImage(sprite, plot.drawX + offsetX, plot.drawY + offsetY, new Rectangle(plot.spriteX, plot.spriteY, plot.spriteWidth, plot.spriteHeight), units);
        }

        private void InputDisplay_Paint(object sender, PaintEventArgs e)
        {
            int inputs = Inputs.RawInputs;
            float lx = Inputs.lx, ly = Inputs.ly, rx = Inputs.rx, ry = Inputs.ry;
            drawnInputs = inputs;
            drawnLx = lx;
            drawnLy = ly;
            drawnRx = rx;
            drawnRy = ry;

            Image sprite = controllerSkin.image;
            Graphics graphics = e.Graphics;
            float pitch = controllerSkin.analogPitch;

            DrawPart(graphics, sprite, "base");

            // The skins name the sticks the other way round: "r3" is drawn while R3 is held.
            DrawPart(graphics, sprite, IsHeld(inputs, Inputs.Buttons.r3) ? "r3" : "r3Press", rx * pitch, ry * pitch);
            DrawPart(graphics, sprite, IsHeld(inputs, Inputs.Buttons.l3) ? "l3" : "l3Press", lx * pitch, ly * pitch);

            foreach (KeyValuePair<Inputs.Buttons, string> part in buttonParts)
            {
                if (IsHeld(inputs, part.Key))
                {
                    DrawPart(graphics, sprite, part.Value);
                }
            }
        }

        private static bool IsHeld(int inputs, Inputs.Buttons button)
        {
            return (inputs & (1 << (int)button)) != 0;
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

            ControllerSkin previous = controllerSkin;
            controllerSkin = ControllerSkin.Load(skinName);
            ObsPadServer.SelectedSkin = skinName;
            if (previous != null && previous.image != null)
            {
                previous.image.Dispose();
            }
            drawnInputs = -1;
            Invalidate();

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
