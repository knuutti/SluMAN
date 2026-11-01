using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SluMAN
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
    /// <para>
    /// Skins come from two folders: the ones SluMAN ships with, in controllerskins next to
    /// SluMAN.exe, and the user's own, in controllerskins in the user data folder, which updates
    /// don't touch. A user skin with a shipped skin's name is used instead of the shipped one.
    /// </para>
    /// </summary>
    public class ControllerSkin
    {
        public const string SkinsFolder = "controllerskins";
        private const string SelectedSkinKey = "InputDisplaySkin";

        /// <summary>
        /// Added to a button's part name for the part drawn while it is not held
        /// (<c>crossReleased</c>). Optional: skins without them have the released look in the base.
        /// </summary>
        public const string ReleasedSuffix = "Released";

        /// <summary>
        /// The skin part for each button that has one, in the order they are drawn. The sticks
        /// (l3 / r3) are drawn separately.
        /// </summary>
        public static readonly KeyValuePair<Inputs.Buttons, string>[] ButtonParts =
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

        public string name;
        /// <summary>Full path of the sprite sheet.</summary>
        public string imagePath;
        /// <summary>The sprite sheet, when loaded with an image.</summary>
        public Image image;
        public Dictionary<string, InputPlot> buttons = new Dictionary<string, InputPlot>();
        public int analogPitch = 32;

        /// <summary>The skins SluMAN ships with, next to SluMAN.exe.</summary>
        public static string ShippedSkinsFolder => Path.Combine(UserData.AppFolder, SkinsFolder);

        /// <summary>The user's own skins, in the user data folder.</summary>
        public static string UserSkinsFolder => UserData.Path(SkinsFolder);

        /// <summary>
        /// Reads a skin. <paramref name="loadImage"/> also decodes the sprite sheet for drawing;
        /// the OBS page only needs its path. Lines that can't be read are skipped; a skin without
        /// a base part or a sprite sheet throws.
        /// </summary>
        public static ControllerSkin Load(string skinName, bool loadImage = true)
        {
            string skinPath = SkinFolder(skinName);
            if (skinPath == null)
            {
                throw new DirectoryNotFoundException($"There is no skin named \"{skinName}\".");
            }

            ControllerSkin skin = new ControllerSkin();
            skin.name = skinName;

            string config = File.ReadAllText(Path.Combine(skinPath, "skin.txt"));
            string imageName = null;

            foreach (string rawLine in config.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length < 2 || line[0] == '#')
                {
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                string partName = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();

                if (partName == "imageName")
                {
                    imageName = value;
                    continue;
                }

                if (partName == "analogPitch")
                {
                    int pitch;
                    if (int.TryParse(value, out pitch))
                    {
                        skin.analogPitch = pitch;
                    }
                    continue;
                }

                InputPlot inputPlot;
                if (TryParsePlot(value, out inputPlot))
                {
                    skin.buttons[partName] = inputPlot;
                }
                else
                {
                    Console.WriteLine($"Skin \"{skinName}\": skipped the line \"{line}\".");
                }
            }

            if (string.IsNullOrEmpty(imageName))
            {
                throw new InvalidDataException($"The skin \"{skinName}\" has no imageName line.");
            }
            if (!skin.buttons.ContainsKey("base"))
            {
                throw new InvalidDataException($"The skin \"{skinName}\" has no base part.");
            }

            skin.imagePath = Path.GetFullPath(Path.Combine(skinPath, imageName));
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

            return skin;
        }

        /// <summary>Reads "drawX, drawY, spriteX, spriteY, spriteWidth, spriteHeight".</summary>
        private static bool TryParsePlot(string value, out InputPlot plot)
        {
            plot = new InputPlot();
            string[] fields = value.Split(',');
            if (fields.Length < 6)
            {
                return false;
            }

            int[] numbers = new int[6];
            for (int i = 0; i < 6; i++)
            {
                if (!int.TryParse(fields[i].Trim(), out numbers[i]))
                {
                    return false;
                }
            }

            plot.drawX = numbers[0];
            plot.drawY = numbers[1];
            plot.spriteX = numbers[2];
            plot.spriteY = numbers[3];
            plot.spriteWidth = numbers[4];
            plot.spriteHeight = numbers[5];
            return true;
        }

        /// <summary>
        /// The skin's folder: the user's if they have one by that name, otherwise the shipped one.
        /// Null when there is no such skin, or the name isn't a plain folder name.
        /// </summary>
        public static string SkinFolder(string skinName)
        {
            if (!Exists(skinName))
            {
                return null;
            }

            string userFolder = Path.Combine(UserSkinsFolder, skinName);
            if (File.Exists(Path.Combine(userFolder, "skin.txt")))
            {
                return userFolder;
            }
            return Path.Combine(ShippedSkinsFolder, skinName);
        }

        /// <summary>The skin folders with a skin.txt in them, in one folder, in name order.</summary>
        private static List<string> SkinNamesIn(string folder)
        {
            List<string> names = new List<string>();
            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (string skinFolder in Directory.EnumerateDirectories(folder))
                    {
                        if (File.Exists(Path.Combine(skinFolder, "skin.txt")))
                        {
                            names.Add(Path.GetFileName(skinFolder));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't list the skins in {folder}: {ex.Message}");
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>The shipped and user skins, in the order the Input Display lists them.</summary>
        public static List<string> SkinNames()
        {
            List<string> names = SkinNamesIn(ShippedSkinsFolder);
            foreach (string userSkin in SkinNamesIn(UserSkinsFolder))
            {
                if (!names.Contains(userSkin, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(userSkin);
                }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>
        /// The skin chosen in the Input Display. config.txt stores its name; versions before 1.1.7
        /// stored its position among the shipped skins, which is still read.
        /// </summary>
        public static string SelectedSkinName()
        {
            List<string> names = SkinNames();
            if (names.Count == 0)
            {
                return "";
            }

            string saved = "";
            try
            {
                saved = func.GetConfigData("config.txt", SelectedSkinKey);
            }
            catch
            {
                // No readable config.txt.
            }

            if (names.Contains(saved))
            {
                return saved;
            }

            int index;
            if (int.TryParse(saved, out index))
            {
                List<string> shipped = SkinNamesIn(ShippedSkinsFolder);
                if (index >= 0 && index < shipped.Count)
                {
                    return shipped[index];
                }
            }
            return names[0];
        }

        /// <summary>Remembers the skin chosen in the Input Display.</summary>
        public static void SaveSelectedSkinName(string skinName)
        {
            try
            {
                func.ChangeFileLines("config.txt", skinName, SelectedSkinKey);
            }
            catch
            {
                // Not remembered; the skin is still used.
            }
        }

        /// <summary>True when <paramref name="skinName"/> is an existing skin folder, and nothing else.</summary>
        public static bool Exists(string skinName)
        {
            return !string.IsNullOrEmpty(skinName) && SkinNames().Contains(skinName);
        }
    }
}
