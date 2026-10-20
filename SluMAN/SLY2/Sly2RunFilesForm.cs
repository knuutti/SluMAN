using System;

namespace SluMAN
{
    /// <summary>
    /// Sly 2's Run Files tab: loads an episode's run file from data/s2_run_file_config.txt, with
    /// the gadgets saved for it in the Gadgets tab.
    /// </summary>
    public class Sly2RunFilesForm : RunFilesForm
    {
        private const string RunFileConfig = "data/s2_run_file_config.txt";

        private static readonly string[] RunFiles = new string[]
        {
            "Episode 1", "Episode 2", "Episode 3", "Episode 4",
            "Episode 5", "Episode 6", "Episode 7", "Episode 8",
        };

        private readonly sly2 game;

        public Sly2RunFilesForm(sly2 game)
            : base("Sly 2", RunFiles, "The gadgets a run file loads with are saved in the Gadgets tab, under Run file gadgets.")
        {
            this.game = game;
        }

        protected override void LoadRunFile(string runFile)
        {
            string episodeKey = GetEpisodeKey(runFile);
            RunFileData runFileData = LoadRunFileDataFromConfig(episodeKey);

            if (runFileData == null)
            {
                statusLine.Error($"No run file data for {runFile} in {RunFileConfig}.");
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

            LoadRunFileGadgets(episodeKey);

            game.api.Notify($"SluMAN {func.VersionText}: Loading {runFile} run file.");
            game.TriggerGameLoad((uint)Sly2Addresses.LoadTypes.RunFile);
            statusLine.Info($"Loading the {runFile} run file.");
        }

        private RunFileData LoadRunFileDataFromConfig(string episodeKey)
        {
            string mapName = func.GetConfigData(RunFileConfig, episodeKey + "_MapName");
            string spawnLocationStr = func.GetConfigData(RunFileConfig, episodeKey + "_SpawnLocation");
            string memoryDataHex = func.GetConfigData(RunFileConfig, episodeKey + "_MemoryData");

            if (string.IsNullOrEmpty(mapName) && string.IsNullOrEmpty(memoryDataHex))
            {
                return null;
            }

            RunFileData runFileData = new RunFileData();
            runFileData.MapName = mapName;

            uint spawnLocation;
            if (uint.TryParse(spawnLocationStr, out spawnLocation))
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

            // The episode's save data moves between game versions, so it comes from the addresses.
            runFileData.MemoryStartAddress = game.GetMemoryRegionForEpisode(episodeKey).startAddress;

            return runFileData;
        }

        private void LoadRunFileGadgets(string episodeKey)
        {
            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_Sly2GadgetBindings");
            string lastGadgetUpdate = func.GetConfigData("config.txt", "GadgetConfigUpdate");

            if (string.IsNullOrEmpty(gadgetHex) || string.IsNullOrEmpty(lastGadgetUpdate))
            {
                gadgetHex = func.GetConfigData(RunFileConfig, episodeKey + "_Sly2GadgetUnlocks");
                bindingHex = func.GetConfigData(RunFileConfig, episodeKey + "_Sly2GadgetBindings");
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

        private static string GetEpisodeKey(string runFile)
        {
            // "Episode 3" -> "Episode3"
            return runFile.Replace(" ", "");
        }
    }
}
