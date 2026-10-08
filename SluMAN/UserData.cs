using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Where SluMAN keeps the user's own files: settings (config.txt), watchlists, split routes and
    /// warp locations. Kept apart from the app folder so they don't depend on where SluMAN was
    /// extracted, and an update can't touch them.
    /// <para>
    /// The folder is %APPDATA%\SluMAN. A file named portable.txt next to SluMAN.exe keeps
    /// everything in the app folder instead, for a copy that lives on a USB stick.
    /// </para>
    /// </summary>
    public static class UserData
    {
        private const string PortableMarker = "portable.txt";
        private const string ConfigFile = "config.txt";

        // Files and folders that are the user's own, as they were laid out in the app folder.
        private static readonly string[] UserFolders = { "watchlists", "usr" };
        private static readonly string[] UserFilePatterns = { "*_user_warps.txt" };

        private static string folder;

        /// <summary>The folder SluMAN.exe is in, where the files it ships with are.</summary>
        public static string AppFolder => Application.StartupPath;

        public static bool IsPortable => File.Exists(System.IO.Path.Combine(AppFolder, PortableMarker));

        /// <summary>The user data folder. Created when first asked for.</summary>
        public static string Folder
        {
            get
            {
                if (folder == null)
                {
                    folder = IsPortable
                        ? AppFolder
                        : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SluMAN");
                    Directory.CreateDirectory(folder);
                }
                return folder;
            }
        }

        /// <summary>A path inside the user data folder.</summary>
        public static string Path(string relativePath)
        {
            return System.IO.Path.Combine(Folder, relativePath);
        }

        /// <summary>config.txt in the user data folder.</summary>
        public static string ConfigPath => Path(ConfigFile);

        /// <summary>
        /// Creates config.txt if it doesn't exist yet, after copying the user's files from an
        /// older SluMAN that kept them in the app folder.
        /// </summary>
        public static void Prepare()
        {
            if (!File.Exists(ConfigPath))
            {
                MigrateFromAppFolder();
            }
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, "");
            }
        }

        /// <summary>
        /// Copies config.txt, watchlists, split routes and user warps from the app folder, where
        /// SluMAN used to keep them. Only copies: the originals stay, and nothing already in the
        /// user data folder is overwritten. Runs once, before the first config.txt exists.
        /// </summary>
        private static void MigrateFromAppFolder()
        {
            if (string.Equals(System.IO.Path.GetFullPath(Folder).TrimEnd('\\'), System.IO.Path.GetFullPath(AppFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                // Portable: the files are already where they belong.
                return;
            }

            List<string> copied = new List<string>();
            try
            {
                CopyIfMissing(System.IO.Path.Combine(AppFolder, ConfigFile), ConfigPath, copied);

                foreach (string pattern in UserFilePatterns)
                {
                    foreach (string file in Directory.GetFiles(AppFolder, pattern))
                    {
                        CopyIfMissing(file, Path(System.IO.Path.GetFileName(file)), copied);
                    }
                }

                foreach (string userFolder in UserFolders)
                {
                    string source = System.IO.Path.Combine(AppFolder, userFolder);
                    if (!Directory.Exists(source))
                    {
                        continue;
                    }
                    Directory.CreateDirectory(Path(userFolder));
                    foreach (string file in Directory.GetFiles(source))
                    {
                        CopyIfMissing(file, Path(System.IO.Path.Combine(userFolder, System.IO.Path.GetFileName(file))), copied);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User data: couldn't copy files from {AppFolder}: {ex.Message}");
            }

            if (copied.Count > 0)
            {
                Console.WriteLine($"User data: copied {copied.Count} file(s) from {AppFolder} to {Folder}: {string.Join(", ", copied)}");
            }
        }

        private static void CopyIfMissing(string source, string destination, List<string> copied)
        {
            if (!File.Exists(source) || File.Exists(destination))
            {
                return;
            }
            File.Copy(source, destination);
            copied.Add(System.IO.Path.GetFileName(source));
        }

        /// <summary>Opens the user data folder in Explorer.</summary>
        public static void OpenInExplorer()
        {
            try
            {
                Process.Start("explorer.exe", "\"" + Folder + "\"");
            }
            catch (Exception ex)
            {
                func.Status($"Couldn't open {Folder}: {ex.Message}", true);
            }
        }
    }
}
