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
        public Form GadgetsWindow;
        public sly2 game;
        public string gameNameId;

        private StatusLine statusLine;
        private GameSession session;

        public SLY2Speedrun(sly2 game, string gameNameId = "NPHA80175")
        {
            this.game = game;
            this.gameNameId = gameNameId;
            InitializeComponent();
            statusLine = new StatusLine(this, true);

            game.SetupInputDisplayMemorySubs();
            game.CheckRunFileConfig();

            session = new GameSession(this, game, gameNameId, "Sly 2", true);
            session.BindAlwaysOnTop(alwaysOnTopCheckBox);
            session.BindAutosplitter(autosplitterCheckbox);
        }

        private void SLY2Speedrun_Load(object sender, EventArgs e)
        {

        }

        private void inputDisplayButton_Click(object sender, EventArgs e)
        {
            session.ShowInputDisplay();
        }

        private void gadgetsButton_Click(object sender, EventArgs e)
        {
            if (GadgetsWindow == null || GadgetsWindow.IsDisposed)
            {
                GadgetsWindow = new Sly2Gadgets(game);
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

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void switchGameModeItem_Click(object sender, EventArgs e)
        {
            session.SwitchGameOrMode();
        }


        private void powerOffPS3Item_Click(object sender, EventArgs e)
        {
            session.PowerOffPS3();
        }

        private void rebootPS3Item_Click(object sender, EventArgs e)
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
