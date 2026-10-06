using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    public partial class SLY3Speedrun : Form
    {
        private const string PrefersAutosplitterKey = "prefersAutosplitter";
        private const string PrefersAlwaysOnTopKey = "prefersAlwaysOnTop";

        public Form InputDisplay;
        public Form GadgetsWindow;
        public sly3 game;
        public string gameNameId;
        public AutosplitterHelper autosplitter;

        private StatusLine statusLine;

        public SLY3Speedrun(sly3 game, string gameNameId = "NPEA00343")
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            ApplySavedPreferences();

            game.SetupInputDisplayMemorySubs();

            game.CheckRunFileConfig();

            if (func.api is Ratchetron r)
            {
                r.setDisconnectCallback(() => { DisconnectGame(false); });

                r.setReconnectCallback(() => { ReconnectGame(); });
            }

            this.gameNameId = gameNameId;
        }

        private void ApplySavedPreferences()
        {
            var prefersAutosplitter = bool.TryParse(func.GetConfigData("config.txt", PrefersAutosplitterKey), out bool autosplitterEnabled) && autosplitterEnabled;
            var prefersAlwaysOnTop = bool.TryParse(func.GetConfigData("config.txt", PrefersAlwaysOnTopKey), out bool alwaysOnTopEnabled) && alwaysOnTopEnabled;

            autosplitterCheckbox.Checked = prefersAutosplitter;
            alwaysTopButton.Checked = prefersAlwaysOnTop;
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

        private void AutosplitterCheckbox_CheckedChanged(object sender, EventArgs e)
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

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void gadgetsButton_Click(object sender, EventArgs e)
        {
            if (GadgetsWindow == null || GadgetsWindow.IsDisposed)
            {
                GadgetsWindow = new SLY3GadgetsForm(game);
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

        private void alwaysTopButton_CheckedChanged(object sender, EventArgs e)
        {
            func.ChangeFileLines("config.txt", alwaysTopButton.Checked ? "true" : "false", PrefersAlwaysOnTopKey);

            if (!alwaysTopButton.Checked)
            {
                this.TopMost = false;

            }
            else
            {
                this.TopMost = true;
            }
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
                
                game.SetSuckValue(runFileData.SuckValue);
                
                if (runFileData.MemoryData != null && runFileData.MemoryData.Length > 0)
                {
                    game.WriteMemoryRegion(runFileData.MemoryStartAddress, runFileData.MemoryData);
                }
                
                if (!string.IsNullOrEmpty(runFileData.MapName))
                {
                    game.SetMapName(runFileData.MapName);
                }
                
                game.SetSpawnLocation(runFileData.SpawnLocation);
                
                LoadRunFileGadgets();

                var loadType = (uint)Sly3Addresses.LoadTypes.RunFile;

                if (episodeKey == "Episode6_CE" || episodeKey == "Episode6_NoCE")
                {
                    game.SetJobState(4342, 1842, (int)-1);
                    loadType = (uint)Sly3Addresses.LoadTypes.Job;
                    if (episodeKey == "Episode6_CE")
                    {
                        game.SetEpisode6NoobMode();
                    }   
                } else
                {
                    SetTutorialComplete();
                }

                game.api.Notify($"SluMAN v{Assembly.GetEntryAssembly().GetName().Version.ToString(3)}: Loading {runFileComboBox.SelectedItem} run file.");

                game.TriggerGameLoad(loadType);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the run file: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        // Function for loading saved run file data from config
        private RunFileData LoadRunFileDataFromConfig(string episodeKey)
        {
            string mapName = func.GetConfigData("data/s3_run_file_config.txt", episodeKey + "_MapName");
            string spawnLocationStr = func.GetConfigData("data/s3_run_file_config.txt", episodeKey + "_SpawnLocation");
            string memoryDataHex = func.GetConfigData("data/s3_run_file_config.txt", episodeKey + "_MemoryData");
            string memoryAddressStr = func.GetConfigData("data/s3_run_file_config.txt", episodeKey + "_MemoryAddress");

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

        private void SetTutorialComplete()
        {
            string memoryDataHex = func.GetConfigData("config.txt", "Episode0_MemoryData");
            string memoryAddressStr = func.GetConfigData("config.txt", "Episode0_MemoryAddress");   

            if (!string.IsNullOrEmpty(memoryDataHex) && !string.IsNullOrEmpty(memoryAddressStr))
            {
                try
                {
                    byte[] memoryData = ConvertMemoryDataString(memoryDataHex);
                    uint memoryAddress = Convert.ToUInt32(memoryAddressStr, 16);
                    game.WriteMemoryRegion(memoryAddress, memoryData);
                }
                catch (Exception ex)
                {
                    statusLine.Error($"Couldn't mark the tutorial complete: {ex.Message}");
                    Console.WriteLine(ex);
                }
            }
        }

        private void LoadRunFileGadgets()
        {
            string episodeKey = GetEpisodeKey(runFileComboBox.SelectedItem.ToString());
            
            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_GadgetBindings");
            
            if (!string.IsNullOrEmpty(gadgetHex))
            {
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
            else
            {
                statusLine.Info($"No saved gadget setup for {runFileComboBox.SelectedItem}. Gadgets were left as they are.");
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
                case "Episode 6 (No CE)": return "Episode6_NoCE";
                case "Episode 6 (CE)": return "Episode6_CE";
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

        private void switchGameModeToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            DisconnectGame();
            this.Close();
            Program.AttachPS3Form.Show();
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void SLY3Speedrun_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Program.AttachPS3Form.Visible == false)
            {
                Program.AttachPS3Form.Close();
                Environment.Exit(0);
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
            int pid = 0;
            int attempts = 0;
            int maxAttempts = 30;

            while (pid == 0 && attempts < maxAttempts)
            {
                Thread.Sleep(3000);
                attempts++;

                try
                {
                    if (game.api.getGameTitleID() != this.gameNameId)
                    {
                        Console.WriteLine("Different game detected.");
                        return;
                    }
                    pid = game.api.getCurrentPID();
                    if (pid != 0)
                    {
                        Console.WriteLine($"Sly 3: Game detected after {attempts * 3} seconds (PID: {pid})");
                    }
                    else
                    {
                        Console.WriteLine($"Sly 3: Still waiting for game... ({attempts * 3}s elapsed)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sly 3: Error checking game status: {ex.Message}");
                }
            }

            if (pid == 0)
            {
                Console.WriteLine("Sly 3: Game did not start within 90 seconds");
                game.api.Notify("Sly 3: Reconnection timeout");
                return;
            }

            // Update PID for new game session
            AttachPS3Form.pid = pid;
            game.pid = pid;

            // Give game extra time to fully initialize
            Thread.Sleep(2000);

            // Re-establish memory subscriptions
            game.SetupInputDisplayMemorySubs();

            // Restart input timer if needed
            if (InputDisplay != null && !InputDisplay.IsDisposed)
            {
                game.InputsTimer.Start();
            }

            // Restart autosplitter if it was running
            if (autosplitterCheckbox.Checked)
            {
                Console.WriteLine("Sly 3: Restarting autosplitter...");
                autosplitter = new AutosplitterHelper();
                autosplitter.StartAutosplitterForGame(this.game);
            }

            game.api.Notify($"SluMAN v{Assembly.GetEntryAssembly().GetName().Version.ToString(3)} (Speedrun Mode)");
            Console.WriteLine("Sly 3: Reconnection complete");
        }

        private void refreshMemorySubsPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectGame();
            ReconnectGame();
        }
    }
}
