using System;

namespace SluMAN
{
    /// <summary>
    /// Sly 3's Run Files tab: loads an episode's run file from data/s3_run_file_config.txt, with
    /// the gadgets saved for it in the Gadgets tab.
    /// </summary>
    public class Sly3RunFilesForm : RunFilesForm
    {
        private const string RunFileConfig = "data/s3_run_file_config.txt";

        private static readonly string[] RunFiles = new string[]
        {
            "Episode 1", "Episode 2", "Episode 3", "Episode 4", "Episode 5",
            "Episode 6 (No CE)", "Episode 6 (CE)",
        };

        private readonly sly3 game;

        public Sly3RunFilesForm(sly3 game)
            : base("Sly 3", RunFiles, "The gadgets a run file loads with are saved in the Gadgets tab, under Run file gadgets.")
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

            LoadRunFileGadgets(runFile, episodeKey);

            uint loadType = (uint)Sly3Addresses.LoadTypes.RunFile;

            if (episodeKey == "Episode6_CE" || episodeKey == "Episode6_NoCE")
            {
                game.SetJobState(4342, 1842, (int)-1);
                loadType = (uint)Sly3Addresses.LoadTypes.Job;
                if (episodeKey == "Episode6_CE")
                {
                    game.SetEpisode6NoobMode();
                }
            }
            else
            {
                SetTutorialComplete();
            }

            game.api.Notify($"SluMAN {func.VersionText}: Loading {runFile} run file.");
            game.TriggerGameLoad(loadType);
            statusLine.Info($"Loading the {runFile} run file.");
        }

        private RunFileData LoadRunFileDataFromConfig(string episodeKey)
        {
            string mapName = func.GetConfigData(RunFileConfig, episodeKey + "_MapName");
            string spawnLocationStr = func.GetConfigData(RunFileConfig, episodeKey + "_SpawnLocation");
            string memoryDataHex = func.GetConfigData(RunFileConfig, episodeKey + "_MemoryData");
            string memoryAddressStr = func.GetConfigData(RunFileConfig, episodeKey + "_MemoryAddress");

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

            runFileData.MemoryStartAddress = game.GetMemoryRegionForEpisode(episodeKey).startAddress;
            if (!string.IsNullOrEmpty(memoryAddressStr))
            {
                try
                {
                    runFileData.MemoryStartAddress = Convert.ToUInt32(memoryAddressStr, 16);
                }
                catch
                {
                    // Not hex: the episode's own region.
                }
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

        private void LoadRunFileGadgets(string runFile, string episodeKey)
        {
            string gadgetHex = func.GetConfigData("config.txt", episodeKey + "_GadgetUnlocks");
            string bindingHex = func.GetConfigData("config.txt", episodeKey + "_GadgetBindings");

            if (string.IsNullOrEmpty(gadgetHex))
            {
                statusLine.Info($"No saved gadget setup for {runFile}. Gadgets were left as they are.");
                return;
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
            switch (runFile)
            {
                case "Episode 6 (No CE)": return "Episode6_NoCE";
                case "Episode 6 (CE)": return "Episode6_CE";
                default: return runFile.Replace(" ", "");
            }
        }
    }
}
