using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace racman
{


    class func
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        /// <summary>
        /// Raised by <see cref="Status"/>. Game forms show these on their <see cref="StatusLine"/>.
        /// </summary>
        public static event Action<string, bool> StatusMessage;

        /// <summary>
        /// Shows a message on the open game form's status line instead of a dialog. Safe to call
        /// from any thread. Also written to the console window.
        /// </summary>
        public static void Status(string message, bool isError = false)
        {
            Console.WriteLine(isError ? $"Error: {message}" : message);

            Action<string, bool> handler = StatusMessage;
            if (handler != null)
            {
                handler(message, isError);
            }
        }

        /// <summary>
        /// True for local builds. The repo keeps the version at 0.0.0.0 and the release workflow
        /// stamps the real one from the release branch name.
        /// </summary>
        public static bool IsDevBuild
        {
            get { return typeof(func).Assembly.GetName().Version.Major == 0; }
        }

        /// <summary>
        /// The version as shown to users, e.g. "v1.0.10", or "dev build" for local builds.
        /// </summary>
        public static string VersionText
        {
            get { return IsDevBuild ? "dev build" : "v" + typeof(func).Assembly.GetName().Version.ToString(3); }
        }

        /// <summary>
        /// Pressing Enter in the box clicks the button, as if the user had clicked it. The key
        /// press is swallowed so Windows doesn't beep.
        /// </summary>
        public static void BindEnter(Control box, Button button)
        {
            box.KeyDown += (sender, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                {
                    return;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (button.Enabled)
                {
                    button.PerformClick();
                }
            };
        }

        public static WebClient client = new WebClient();
        public static IPS3API api;
        public static string sprxPath = Environment.CurrentDirectory + @"\";
        public static string get_data(string url)
        {
            string x = null;
            try
            {
                x = client.DownloadString(url);
            }
            catch
            {
            }
            return x;
        }

        public static bool PrepareSPRX(string ip, string sprx, int slot)
        {
            // Check if Ratchetron is already loaded
            string slot6sprx = get_data($"http://{ip}/home.ps3mapi");
            if (slot6sprx == null)
            {
                MessageBox.Show($"Couldn't reach webMAN MOD on the PS3 at {ip}. Check the IP address and that webMAN MOD is running.", "Couldn't connect");
                return false;
            }

            bool sprxLoaded = slot6sprx.Contains(sprx);

            if (sprxLoaded)
            {
                return true;
            }

            client.UploadFile($"ftp://{ip}:21/dev_hdd0/tmp/{sprx}", $@"{sprxPath}\{sprx}");
            get_data($"http://{ip}/vshplugin.ps3mapi?prx=%2Fdev_hdd0%2Ftmp%2F{sprx}&load_slot={slot}");

            return true;
        }

        public static bool PrepareRatchetron(string ip)
        {
            return PrepareSPRX(ip, "ratchetron_server.sprx", 6);
        }

        private static readonly Regex ConfigKeyRegex = new Regex(@"^([\w\-]+)", RegexOptions.Compiled);
        private static readonly object configLock = new object();

        private class ConfigFile
        {
            public DateTime lastWriteUtc;
            public string[] lines;
            // The first line of each key, as GetConfigData has always read it.
            public Dictionary<string, int> firstLineOfKey;
        }

        // Config files read so far. Re-read when the file changes on disk, so editing
        // config.txt by hand while SluMAN runs still works.
        private static readonly Dictionary<string, ConfigFile> configCache = new Dictionary<string, ConfigFile>(StringComparer.OrdinalIgnoreCase);

        private static string ConfigKeyOf(string line)
        {
            return ConfigKeyRegex.Match(line).Value;
        }

        private static ConfigFile CacheConfigLines(string path, string[] lines)
        {
            ConfigFile file = new ConfigFile();
            file.lines = lines;
            file.lastWriteUtc = File.GetLastWriteTimeUtc(path);
            file.firstLineOfKey = new Dictionary<string, int>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                string key = ConfigKeyOf(lines[i]);
                if (!file.firstLineOfKey.ContainsKey(key))
                {
                    file.firstLineOfKey[key] = i;
                }
            }
            configCache[path] = file;
            return file;
        }

        /// <summary>
        /// The file's lines, from the cache unless the file changed. Throws like File.ReadAllLines
        /// when the file is missing. Call with configLock held.
        /// </summary>
        private static ConfigFile ReadConfigFile(string path)
        {
            ConfigFile cached;
            if (configCache.TryGetValue(path, out cached) && File.GetLastWriteTimeUtc(path) == cached.lastWriteUtc)
            {
                return cached;
            }
            return CacheConfigLines(path, File.ReadAllLines(path));
        }

        public static void ChangeFileLines(string filename, string contents, string keyword)
        {
            // Only config.txt is ever written; it lives in the user data folder.
            string configPath = UserData.ConfigPath;
            string newLine = keyword + " = " + contents;

            lock (configLock)
            {
                string[] data = ReadConfigFile(configPath).lines;
                List<string> newData = new List<string>(data.Length + 1);
                bool found = false;

                foreach (string line in data)
                {
                    if (ConfigKeyOf(line) == keyword)
                    {
                        newData.Add(newLine);
                        found = true;
                    }
                    else
                    {
                        newData.Add(line);
                    }
                }

                if (!found)
                {
                    newData.Add(newLine);
                }

                string[] lines = newData.ToArray();
                File.WriteAllLines(configPath, lines);
                CacheConfigLines(configPath, lines);
            }
        }

        public static string GetConfigData(string filename, string keyword)
        {
            // config.txt is the user's; other files (data/*.txt) ship next to SluMAN.exe.
            string path = filename == "config.txt" ? UserData.ConfigPath : filename;

            lock (configLock)
            {
                ConfigFile file = ReadConfigFile(path);
                int index;
                if (!file.firstLineOfKey.TryGetValue(keyword, out index))
                {
                    return "";
                }

                string line = file.lines[index];
                int startPos = line.IndexOf("=") + 2;
                return line.Substring(startPos, line.Length - startPos);
            }
        }

        public static List<string> GetWindowTitles(string processName)
        {
            List<string> titles = new List<string>();
            uint processId = (uint)Process.GetProcessesByName(processName)[0].Id;

            EnumWindows((hWnd, lParam) =>
            {
                uint windowProcessId;
                GetWindowThreadProcessId(hWnd, out windowProcessId);

                if (windowProcessId == processId)
                {
                    int length = GetWindowTextLength(hWnd);
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    titles.Add(sb.ToString());
                }

                return true;  // Continue enumeration
            }, IntPtr.Zero);

            return titles;
        }
    }

    public static class ControlExtensions
    {
        public static void DoubleBuffering(this Control control, bool enable)
        {
            var method = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(control, new object[] { ControlStyles.OptimizedDoubleBuffer, enable });
        }
    }
}
