using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

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
    /// don't touch (controllerskins-user next to SluMAN.exe in a portable copy, whose user data
    /// folder is the app folder). A user skin with a shipped skin's name is used instead of the
    /// shipped one. The skin editor saves user skins only.
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

        /// <summary>
        /// The user's own skins, in the user data folder. A portable copy keeps everything in the
        /// app folder, so its user skins get a folder of their own there.
        /// </summary>
        public static string UserSkinsFolder => UserData.Path(UserData.IsPortable ? "controllerskins-user" : SkinsFolder);

        /// <summary>The sprite sheet a saved skin gets.</summary>
        public const string SavedImageName = "skin.png";

        /// <summary>
        /// A skin was saved (its name, and whether it was chosen with it: <see cref="Save"/> has
        /// already remembered that and told the OBS page). Raised on the thread that saved it.
        /// </summary>
        public static event Action<string, bool> SkinSaved;

        private static int revision;

        /// <summary>Goes up each time a skin is saved, so the OBS page knows to load it again.</summary>
        public static int Revision => revision;

        /// <summary>The sticks' parts: "r3" / "l3" while clicked in, "r3Press" / "l3Press" otherwise.</summary>
        public static readonly string[] StickParts = { "r3", "r3Press", "l3", "l3Press" };

        /// <summary>Every part name a skin can have.</summary>
        public static List<string> PartNames()
        {
            List<string> names = new List<string>();
            names.Add("base");
            names.AddRange(StickParts);
            foreach (KeyValuePair<Inputs.Buttons, string> part in ButtonParts)
            {
                names.Add(part.Value);
                names.Add(part.Value + ReleasedSuffix);
            }
            return names;
        }

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

        /// <summary>True when the skin is one of the user's own, not a shipped one.</summary>
        public static bool IsUserSkin(string skinName)
        {
            return Exists(skinName) && File.Exists(Path.Combine(UserSkinsFolder, skinName, "skin.txt"));
        }

        /// <summary>True when a shipped skin has this name, whatever its case.</summary>
        public static bool IsShippedName(string skinName)
        {
            return SkinNamesIn(ShippedSkinsFolder).Contains(skinName, StringComparer.OrdinalIgnoreCase);
        }

        private static readonly string[] reservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>
        /// Why <paramref name="skinName"/> can't be a skin folder's name, or null when it can.
        /// Doesn't check whether a skin already has it.
        /// </summary>
        public static string NameProblem(string skinName)
        {
            if (string.IsNullOrWhiteSpace(skinName))
            {
                return "Give the skin a name.";
            }
            if (skinName.Length > 64)
            {
                return "The name can be at most 64 characters.";
            }
            if (skinName != skinName.Trim() || skinName.EndsWith("."))
            {
                return "The name can't start or end with a space, or end with a dot.";
            }
            if (skinName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return "The name can't have any of these: \\ / : * ? \" < > |";
            }
            string stem = skinName.Split('.')[0].Trim();
            if (reservedNames.Contains(stem, StringComparer.OrdinalIgnoreCase))
            {
                return $"Windows doesn't allow \"{skinName}\" as a folder name.";
            }
            return null;
        }

        private static readonly object saveLock = new object();

        /// <summary>
        /// Saves a user skin: <see cref="SavedImageName"/> and a skin.txt for <paramref name="parts"/>,
        /// replacing the user skin of that name if there is one (its other files are kept). The
        /// folder is written beside the skins and moved into place, so a failed save leaves the
        /// old skin as it was. The caller checks the name and the parts.
        /// </summary>
        public static void Save(string skinName, byte[] png, int pitch, Dictionary<string, InputPlot> parts, bool select)
        {
            lock (saveLock)
            {
                Directory.CreateDirectory(UserSkinsFolder);
                string target = Path.Combine(UserSkinsFolder, skinName);
                string id = Guid.NewGuid().ToString("N");
                // In the user data folder, the skins folder's parent: on the same drive, so the
                // moves below are renames, and outside the skins folder, so it's never listed.
                string temp = UserData.Path("skin-save-" + id);
                string backup = UserData.Path("skin-old-" + id);

                try
                {
                    Directory.CreateDirectory(temp);
                    if (Directory.Exists(target))
                    {
                        foreach (string file in Directory.GetFiles(target))
                        {
                            string fileName = Path.GetFileName(file);
                            if (!fileName.Equals("skin.txt", StringComparison.OrdinalIgnoreCase)
                                && !fileName.Equals(SavedImageName, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Copy(file, Path.Combine(temp, fileName));
                            }
                        }
                    }
                    File.WriteAllBytes(Path.Combine(temp, SavedImageName), png);
                    File.WriteAllText(Path.Combine(temp, "skin.txt"), SkinText(pitch, parts), new UTF8Encoding(false));

                    bool replacing = Directory.Exists(target);
                    if (replacing)
                    {
                        Directory.Move(target, backup);
                    }
                    try
                    {
                        Directory.Move(temp, target);
                    }
                    catch
                    {
                        if (replacing)
                        {
                            Directory.Move(backup, target);
                        }
                        throw;
                    }
                }
                finally
                {
                    try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
                    try { if (Directory.Exists(backup)) Directory.Delete(backup, true); } catch { }
                }

                System.Threading.Interlocked.Increment(ref revision);
            }

            if (select)
            {
                SaveSelectedSkinName(skinName);
                ObsPadServer.SelectedSkin = skinName;
            }

            Action<string, bool> handler = SkinSaved;
            if (handler != null)
            {
                handler(skinName, select);
            }
        }

        private static void AppendPart(StringBuilder text, Dictionary<string, InputPlot> parts, string partName)
        {
            InputPlot plot;
            if (parts.TryGetValue(partName, out plot))
            {
                text.Append($"{partName}: {plot.drawX}, {plot.drawY}, {plot.spriteX}, {plot.spriteY}, {plot.spriteWidth}, {plot.spriteHeight}\r\n");
            }
        }

        private static void AppendButtons(StringBuilder text, Dictionary<string, InputPlot> parts, string heading, int first, int count)
        {
            bool any = false;
            for (int i = first; i < first + count; i++)
            {
                any |= parts.ContainsKey(ButtonParts[i].Value) || parts.ContainsKey(ButtonParts[i].Value + ReleasedSuffix);
            }
            if (!any)
            {
                return;
            }

            text.Append($"\r\n# {heading}\r\n");
            for (int i = first; i < first + count; i++)
            {
                AppendPart(text, parts, ButtonParts[i].Value);
                AppendPart(text, parts, ButtonParts[i].Value + ReleasedSuffix);
            }
        }

        /// <summary>A skin.txt for these parts, laid out like defaultSkin.txt.</summary>
        public static string SkinText(int pitch, Dictionary<string, InputPlot> parts)
        {
            StringBuilder text = new StringBuilder();
            text.Append("# Made with SluMAN's skin editor. The structure, as in defaultSkin.txt:\r\n");
            text.Append("# Name: drawX, drawY, spriteX, spriteY, spriteWidth, spriteHeight\r\n");
            text.Append($"# A button's \"{ReleasedSuffix}\" part is drawn while it isn't held.\r\n");

            text.Append("\r\n# Base controller image\r\n");
            AppendPart(text, parts, "base");

            text.Append("\r\n# Analog sticks: r3 / l3 while clicked in, r3Press / l3Press otherwise\r\n");
            foreach (string stick in StickParts)
            {
                AppendPart(text, parts, stick);
            }
            text.Append("\r\n# How far the sticks move\r\n");
            text.Append($"analogPitch: {pitch}\r\n");

            AppendButtons(text, parts, "D-Pad buttons", 0, 4);
            AppendButtons(text, parts, "Face buttons", 4, 4);
            AppendButtons(text, parts, "Pause buttons", 8, 2);
            AppendButtons(text, parts, "Shoulder buttons", 10, 4);

            text.Append("\r\n# The sprite sheet, in the same folder as this file\r\n");
            text.Append($"imageName: {SavedImageName}\r\n");
            return text.ToString();
        }

        /// <summary>True when <paramref name="skinName"/> is an existing skin folder, and nothing else.</summary>
        public static bool Exists(string skinName)
        {
            return !string.IsNullOrEmpty(skinName) && SkinNames().Contains(skinName);
        }
    }
}
