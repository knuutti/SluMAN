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

namespace SluMAN
{
    public partial class SLY3Speedrun : Form
    {
        public Form GadgetsWindow;
        public sly3 game;
        public string gameNameId;

        private StatusLine statusLine;
        private GameSession session;

        public SLY3Speedrun(sly3 game, string gameNameId = "NPEA00343")
        {
            this.game = game;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            this.gameNameId = gameNameId;

            game.SetupInputDisplayMemorySubs();

            game.CheckRunFileConfig();

            session = new GameSession(this, game, gameNameId, "Sly 3", true);
            session.BindAlwaysOnTop(alwaysTopButton);
            session.BindAutosplitter(autosplitterCheckbox);
        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            session.ShowInputDisplay();
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
                session.AddGameWindow(GadgetsWindow);
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

                game.api.Notify($"SluMAN {func.VersionText}: Loading {runFileComboBox.SelectedItem} run file.");

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
                    runFileData.MemoryData = IGame.ConvertMemoryDataString(memoryDataHex);
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
                    byte[] memoryData = IGame.ConvertMemoryDataString(memoryDataHex);
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
                    byte[] gadgetBytes = IPS3API.HexToBytes(gadgetHex);
                    byte[] bindingBytes = IPS3API.HexToBytes(bindingHex);
                    
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

        private void switchGameModeToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }

        private void powerOffPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.RebootPS3();
        }

        private void memoryUtilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryForm.ShowFor(this);
        }

        private void openUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserData.OpenInExplorer();
        }
    }
}
