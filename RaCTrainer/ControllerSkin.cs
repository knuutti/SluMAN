using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace racman
{
    /// <summary>
    /// Where one part of a skin is drawn, and which part of the sprite sheet it comes from.
    /// </summary>
    public struct InputPlot
    {
        public int drawX { get; set; }
        public int drawY { get; set; }
        public int spriteX { get; set; }
        public int spriteY { get; set; }
        public int spriteWidth { get; set; }
        public int spriteHeight { get; set; }
    }

    /// <summary>
    /// An input display skin: controllerskins/{name}/skin.txt and its sprite sheet. Used by the
    /// Input Display window and the OBS page.
    /// </summary>
    public class ControllerSkin
    {
        public const string SkinsFolder = "controllerskins";
        private const string SelectedSkinKey = "InputDisplaySkin";

        public string name;
        /// <summary>Full path of the sprite sheet.</summary>
        public string imagePath;
        /// <summary>The sprite sheet, when loaded with an image.</summary>
        public Image image;
        public Dictionary<string, InputPlot> buttons = new Dictionary<string, InputPlot>();
        public int analogPitch = 32;

        /// <summary>
        /// Reads a skin. <paramref name="loadImage"/> also decodes the sprite sheet for drawing;
        /// the OBS page only needs its path.
        /// </summary>
        public static ControllerSkin Load(string skinName, bool loadImage = true)
        {
            string skinPath = Path.Combine(SkinsFolder, skinName);
            ControllerSkin skin = new ControllerSkin();
            skin.name = skinName;

            string config = File.ReadAllText(Path.Combine(skinPath, "skin.txt"));

            foreach (string line in config.Split('\n'))
            {
                if (line.Length < 2 || line[0] == '#')
                {
                    continue;
                }

                string[] components = line.Split(':');
                if (components.Length < 2)
                {
                    continue;
                }

                string buttonName = components[0];

                if (buttonName == "imageName")
                {
                    skin.imagePath = Path.GetFullPath(Path.Combine(skinPath, components[1].Trim()));
                    if (loadImage)
                    {
                        using (Image rawImage = Image.FromFile(skin.imagePath))
                        {
                            // Convert to pre-multiplied alpha format. GDI+ DrawImage is up to 6x faster
                            // with Format32bppPArgb vs the default Format32bppArgb loaded from PNG.
                            // Without this, white/light skins cause high CPU because per-pixel alpha
                            // math can't be short-circuited for non-zero RGB values.
                            Bitmap bmp = new Bitmap(rawImage.Width, rawImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.DrawImage(rawImage, 0, 0, rawImage.Width, rawImage.Height);
                            skin.image = bmp;
                        }
                    }
                    continue;
                }

                if (buttonName == "analogPitch")
                {
                    skin.analogPitch = int.Parse(components[1].Trim());
                    continue;
                }

                int[] plot = components[1].Split(',').Select(thing => int.Parse(thing.Trim())).ToArray();

                if (plot.Length < 6)
                {
                    continue;
                }

                InputPlot inputPlot = new InputPlot();
                inputPlot.drawX = plot[0];
                inputPlot.drawY = plot[1];
                inputPlot.spriteX = plot[2];
                inputPlot.spriteY = plot[3];
                inputPlot.spriteWidth = plot[4];
                inputPlot.spriteHeight = plot[5];

                skin.buttons[buttonName] = inputPlot;
            }

            return skin;
        }

        /// <summary>The skin folders, in the order the Input Display lists them.</summary>
        public static List<string> SkinNames()
        {
            List<string> names = new List<string>();
            if (Directory.Exists(SkinsFolder))
            {
                foreach (string folder in Directory.EnumerateDirectories(SkinsFolder))
                {
                    names.Add(Path.GetFileName(folder));
                }
            }
            return names;
        }

        /// <summary>
        /// The skin chosen in the Input Display. config.txt stores its position in the list.
        /// </summary>
        public static string SelectedSkinName()
        {
            List<string> names = SkinNames();
            if (names.Count == 0)
            {
                return "";
            }

            int index = 0;
            try
            {
                int.TryParse(func.GetConfigData("config.txt", SelectedSkinKey), out index);
            }
            catch
            {
                // No readable config.txt.
            }
            return names[index >= 0 && index < names.Count ? index : 0];
        }

        /// <summary>True when <paramref name="skinName"/> is an existing skin folder, and nothing else.</summary>
        public static bool Exists(string skinName)
        {
            return !string.IsNullOrEmpty(skinName) && SkinNames().Contains(skinName);
        }
    }
}
