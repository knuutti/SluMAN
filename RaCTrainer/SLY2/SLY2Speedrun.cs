using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static racman.SLY3Speedrun;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    public partial class SLY2Speedrun : Form
    {
        private const string PrefersAutosplitterKey = "prefersAutosplitter";
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        public Form InputDisplay;
        public Form GadgetsWindow;
        public sly2 game;
        public string gameNameId;
        public AutosplitterHelper autosplitter;

        private StatusLine statusLine;

        public SLY2Speedrun(sly2 game, string gameNameId = "NPHA80175")
        {
            this.game = game;
            this.gameNameId = gameNameId;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            ApplySavedPreferences();

            game.SetupInputDisplayMemorySubs();
            game.CheckRunFileConfig();

            GameReconnect.WatchRpcs3(this, game.api, gameNameId, true);

            if (func.api is Ratchetron r)
            {
                r.setDisconnectCallback(() =>
                {
                    DisconnectGame(false);
                });

                r.setReconnectCallback(() =>
                {
                    ReconnectGame();
                });
            }
        }

        private void SLY2Speedrun_Load(object sender, EventArgs e)
        {

        }

        private void ApplySavedPreferences()
        {
            var prefersAutosplitter = bool.TryParse(func.GetConfigData("config.txt", PrefersAutosplitterKey), out bool autosplitterEnabled) && autosplitterEnabled;
            var prefersAlwaysOnTop = bool.TryParse(func.GetConfigData("config.txt", PrefersAlwaysOnTopKey), out bool alwaysOnTopEnabled) && alwaysOnTopEnabled;

            autosplitterCheckbox.Checked = prefersAutosplitter;
            alwaysOnTopCheckBox.Checked = prefersAlwaysOnTop;
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            if (InputDisplay == null || InputDisplay.IsDisposed)
            {
                InputDisplay = new InputDisplay();
                InputDisplay.Show();
                game.InputsTimer.Start();
            }
            else
            {
                InputDisplay.Focus();
            }
        }

        private void alwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            func.ChangeFileLines("config.txt", alwaysOnTopCheckBox.Checked ? "true" : "false", PrefersAlwaysOnTopKey);

            if (!alwaysOnTopCheckBox.Checked)
            {
                this.TopMost = false;

            }
            else
            {
                this.TopMost = true;
            }
        }

        private void autosplitterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            func.ChangeFileLines("config.txt", autosplitterCheckbox.Checked ? "true" : "false", PrefersAutosplitterKey);

            if (!autosplitterCheckbox.Checked)
            {
                autosplitter?.Stop();
                autosplitter = null;
            }
            else
            {
                autosplitter?.Stop();
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(this.game);
            }
        }

        private void gadgetsButton_Click(object sender, EventArgs e)
        {
            if (GadgetsWindow == null || GadgetsWindow.IsDisposed)
            {
                GadgetsWindow = new Sly2Gadgets(game);
                GadgetsWindow.FormClosed += GadgetsWindow_FormClosed;
                GadgetsWindow.Show();
            }
            else
            {
                GadgetsWindow.Focus();
            }
        }

        private void GadgetsWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            GadgetsWindow = null;
        }

        private void loadRunFileButton_Click(object sender, EventArgs e)
        {
            if (runFileComboBox.SelectedItem == null)
            {
                statusLine.Error("Select a run file to load first.");
                return;
            }

            LoadCompleteRunFile();
        }

        private void LoadCompleteRunFile()
        {
            string episodeKey = GetEpisodeKey(runFileComboBox.SelectedItem.ToString());

            try
            {
                var runFileData = LoadRunFileDataFromConfig(episodeKey);

                if (runFileData == null)
                {
                    game.api.Notify("Error loading a run file: Config file not found.");
                    return;
                }

                if (runFileData.MemoryData != null && runFileData.MemoryData.Length > 0)
                {
                    game.WriteMemoryRegion(runFileData.MemoryStartAddress, runFileData.MemoryData);
                }

                if (!string.IsNullOrEmpty(runFileData.MapName))
                {
                    game.SetMapName(runFileData.MapName);
                }

                game.SetSpawnLocation((int)runFileData.SpawnLocation);

                LoadRunFileGadgets();

                var loadType = (uint)Sly2Addresses.LoadTypes.RunFile;

                game.api.Notify($"SluMAN {func.VersionText}: Loading {runFileComboBox.SelectedItem} run file.");

                game.TriggerGameLoad(loadType);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the run file: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private RunFileData LoadRunFileDataFromConfig(string episodeKey)
        {
            string mapName = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_MapName");
            string spawnLocationStr = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_SpawnLocation");
            string memoryDataHex = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_MemoryData");
            string memoryAddressStr = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_MemoryAddress");

            if (string.IsNullOrEmpty(mapName) && string.IsNullOrEmpty(memoryDataHex))
            {
                return null;
            }

            var runFileData = new RunFileData();

            runFileData.MapName = mapName;

            if (uint.TryParse(spawnLocationStr, out uint spawnLocation))
            {
                runFileData.SpawnLocation = spawnLocation;
            }

            if (!string.IsNullOrEmpty(memoryDataHex))
            {
                try
                {
                    runFileData.MemoryData = ConvertMemoryDataString(memoryDataHex);
                }
                catch
                {
                    runFileData.MemoryData = null;
                }
            }

            if (!string.IsNullOrEmpty(memoryAddressStr))
            {
                try
                {
                    runFileData.MemoryStartAddress = Convert.ToUInt32(memoryAddressStr, 16);
                }
                catch
                {
                    var (startAddress, size) = game.GetMemoryRegionForEpisode(episodeKey);
                    runFileData.MemoryStartAddress = startAddress;
                }
            }
            else
            {
                var (startAddress, size) = game.GetMemoryRegionForEpisode(episodeKey);
                runFileData.MemoryStartAddress = startAddress;
            }

            return runFileData;
        }

        private void LoadRunFileGadgets()
        {
            string episodeKey = GetEpisodeKey(runFileComboBox.SelectedItem.ToString());

            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetBindings");
            string lastGadgetUpdate = func.GetConfigData("config.txt", "GadgetConfigUpdate");

            if (string.IsNullOrEmpty(gadgetHex) || string.IsNullOrEmpty(lastGadgetUpdate))
            {
                gadgetHex = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_Sly2GadgetUnlocks");
                bindingHex = func.GetConfigData("data/s2_run_file_config.txt", episodeKey + "_Sly2GadgetBindings");
                func.ChangeFileLines("config.txt", "29032026", "GadgetConfigUpdate");
            }

            try
            {
                byte[] gadgetBytes = StringToByteArray(gadgetHex);
                byte[] bindingBytes = StringToByteArray(bindingHex);

                game.SetGadgetUnlocks(gadgetBytes);

                if (!string.IsNullOrEmpty(bindingHex))
                {
                    game.SetGadgetBindings(bindingBytes);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the gadget setup: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        public class RunFileData
        {
            public string MapName { get; set; }
            public uint SpawnLocation { get; set; }
            public byte[] MemoryData { get; set; }
            public uint MemoryStartAddress { get; set; }
            public float SuckValue { get; set; } = 0.0f;
        }

        private string GetEpisodeKey(string episodeName)
        {
            switch (episodeName)
            {
                case "Episode 1": return "Episode1";
                case "Episode 2": return "Episode2";
                case "Episode 3": return "Episode3";
                case "Episode 4": return "Episode4";
                case "Episode 5": return "Episode5";
                case "Episode 6": return "Episode6";
                case "Episode 7": return "Episode7";
                case "Episode 8": return "Episode8";
                default: return "Episode1";
            }
        }

        // Helper function for converting strings like "FFFF" to [0xFF, 0xFF] byte array
        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // Helper function for converting (comma-separated) memory data to byte array
        private static byte[] ConvertMemoryDataString(string data)
        {
            var parts = data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            byte[] bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                bytes[i] = (byte)int.Parse(parts[i].ToString());
            }
            return bytes;
        }

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void switchGameModeItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            this.Close();
            Program.AttachPS3Form.Show();
        }

        private void powerOffPS3Item_Click(object sender, EventArgs e)
        {
            if (game.api is Ratchetron r)
            {
                var dialogResult = MessageBox.Show("Do you want to turn off your PS3?", "Power Off PS3", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DisconnectGame();
                    WebMAN.TurnOffPS3(func.api.GetIP());
                    this.Close();
                    Program.AttachPS3Form.Show();
                }

            }
        }

        private void rebootPS3Item_Click(object sender, EventArgs e)
        {
            if (game.api is Ratchetron r)
            {
                var dialogResult = MessageBox.Show("Do you want to reboot your PS3?", "Reboot PS3", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DisconnectGame();
                    WebMAN.RebootPS3(func.api.GetIP());
                    this.Close();
                    Program.AttachPS3Form.Show();
                }
            }
        }

        private void DisconnectGame(bool closeInputDisplay = true)
        {
            if (game.api is Ratchetron ratchetron)
            {
                if (autosplitter != null)
                {
                    autosplitter.Stop();
                    autosplitter = null;
                }
                ratchetron.ReleaseAllSubs();
            }
            if (closeInputDisplay)
            {
                try { game.api.Disconnect(); } catch { }
            }
            CloseAdditionalWindows(closeInputDisplay);
        }

        private void CloseAdditionalWindows(bool closeInputDisplay = true)
        {
            if (closeInputDisplay && InputDisplay != null && !InputDisplay.IsDisposed)
            {
                InputDisplay.Close();
            }
            if (GadgetsWindow != null && !GadgetsWindow.IsDisposed)
            {
                GadgetsWindow.Close();
            }
        }

        private void ReconnectGame()
        {
            GameReconnect.Result result = GameReconnect.WaitForGame(game.api, gameNameId, "Sly 2", out int pid, out string runningTitleId);
            if (GameReconnect.HandleOtherResult(result, this, "Sly 2", runningTitleId, true))
            {
                return;
            }

            // Update PID for new game session
            AttachPS3Form.pid = pid;
            game.pid = pid;

            // Give game extra time to fully initialize
            Thread.Sleep(2000);

            // Re-establish memory subscriptions

            // Restart input timer if needed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                game.InputsTimer.Start();
            }

            // Restart autosplitter if it was running
            if (autosplitterCheckbox.Checked)
            {
                Console.WriteLine("Restarting autosplitter...");
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(this.game);
                Console.WriteLine("Autosplitter restarted successfully.");
            }

            game.SetupInputDisplayMemorySubs();

            game.api.Notify($"SluMAN {func.VersionText} (Speedrun Mode)");
            Console.WriteLine("Game reconnected.");
            func.Status("Reconnected to Sly 2.");
        }

        private void refreshMemorySubsPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            ReconnectGame();
        }

        private void SLY2Speedrun_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Program.AttachPS3Form.Visible == false && !AttachPS3Form.switchPending)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
            }
        }
    }
}
