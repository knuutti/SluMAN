using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    public class Sly3Addresses
    {
        public uint inputOffset => 0x5EC5AA;
        public uint analogOffsetLeft => 0x5EC5F0;
        public uint analogOffsetRight => 0x5EC61C;
        public uint coinCount => 0x6CC808;
        public uint slyCharacterPtr => 0x5ED940;
        public uint activeCharacterPtr => 0x5EC654;

        public uint playerEntityPointer => 0x5EC654;
        public uint transformOffset => 0x44;
        public uint coordsOffsetX => 0x130;

        // Entity struct offsets (from activeCharacterPtr dereference)
        public uint healthEntityOffset => 0x168;
        public uint gadgetPowerEntityOffset => 0x170;
        public uint coordsOffsetY => 0x134; 
        public uint coordsOffsetZ => 0x138;

        // Cinematic skipping addresses
        public uint dialogueState => 0x39B13F70;
        public uint dialogueFrameCounter => 0x39B13F54;
        public uint fmvState => 0x83C8BC;

        public uint mapAOB => 0x78D2C8;
        public uint spawnLocation => 0x78D308;

        public uint loadType => 0x78D2C4;
        public uint loadTrigger => 0x78D2C0; // Set to 1 to trigger load

        public uint gadgetUnlocks => 0x6CC7F8;
        public uint gadgetBindsSly => 0x6CC7B0;
        public uint gadgetBindsBentley => 0x6CC7BC;
        public uint gadgetBindsMurray => 0x6CC7C8;

        // Run file specific addresses
        public uint suckValue => 0x589A3C;
        public uint currentCharacter => 0x5EA000;
        public uint cameraFov => 0x7F8680;

        // Autosplitter addresses
        public uint loadingState => 0x6CB600;
        public uint currentJob => 0x5EB488;
        public uint currentCheckpoint => 0x5EB48C;
        public uint currentMap => 0x78D398;
        public uint gameSpeed => 0x5898B8;
        public uint mtcTimerValue => 0x7DAB2C;
        public uint pauseLock => 0x5EC6C4;
        public uint guardAIAddress => 0x5EC6CC;
        public uint deathBarriersPointer => 0x78C6E4;
        public uint cameraPointer => 0x78CE2C;

        // Episode 1 specific addresses
        public uint veniceStarted => 0x6CE0B4;
        public uint outbackStarted => 0x6CEA80;
        public uint chinaStarted => 0x6D0288;
        public uint pirateStarted => 0x6D1110;

        public enum LoadTypes : uint
        {
            Fast = 0,
            Normal = 6,
            Reset = 15,
            RunFile = 18,
            Job = 134,
        }
    }

    public class sly3 : IGame, IAutosplitterAvailable
    {
        public static Sly3Addresses addr = new Sly3Addresses();

        public uint mapIndex;
        public bool speedrunMode;

        public struct MapData
        {
            public string naturalName;
            public string indicator;
            public uint defaultWarp;
            
            public MapData(string naturalName, string indicator, uint defaultWarp)
            {
                this.naturalName = naturalName;
                this.indicator = indicator;
                this.defaultWarp = defaultWarp;
            }
        }

        public MapData[] maps;
        private int infiniteJumpsFreezeSubID = -1;
        private int gameClockFreezeSubID = -1;

        public sly3(IPS3API api) : base(api)
        {
            this.maps = new MapData[]
            {
                new MapData("Main Menu", "Y$KFdvd_menu", 0),
                new MapData("Hazard Room", "Y$KFi_trainer", 424),
                new MapData("Venice Hub", "Y$KFv_ext", 44438),
                new MapData("Canal Chase", "Y$KFv_canal", 0),
                new MapData("Coffee House", "Y$KFv_apt", 0),
                new MapData("Opera House", "Y$KFv_gauntlet", 0),
                new MapData("Police Station", "Y$KFv_interpol", 0),
                new MapData("Outback Hub", "Y$KFo_ext", 44438),
                new MapData("Ayer's Rock", "Y$KFo_quarry", 0),
                new MapData("Oil Field", "Y$KFo_arena", 0),
                new MapData("Cave 1", "Y$KFo_cave_a", 0),
                new MapData("Cave 2", "Y$KFo_cave_b", 0),
                new MapData("Cave 3", "Y$KFo_cave_murray", 0),
                new MapData("Lemonade Bar", "Y$KFo_bar", 0),
                new MapData("Holland Hub", "Y$KFh_ext", 44438),
                new MapData("Hotel", "Y$KFh_hotel", 0),
                new MapData("Black Baron's Hangar", "Y$KFh_hangar_b", 0),
                new MapData("Cooper Gang Hangar", "Y$KFh_hangar_c", 0),
                new MapData("Team Belgium's Hangar", "Y$KFh_hangar_a", 0),
                new MapData("Sewer", "Y$KFh_sewer", 0),
                new MapData("Dogfight Arena", "Y$KFh_dogfight", 0),
                new MapData("China Hub", "Y$KFc_ext", 44438),
                new MapData("Intro", "Y$KFc_intro", 0),
                new MapData("Flashback", "Y$KFc_flashback", 0),
                new MapData("Tsao's Battleground", "Y$KFc_forest", 0),
                new MapData("Panda King's House", "Y$KFc_apt", 0),
                new MapData("Tsao's Business Center", "Y$KFc_hall_a", 0),
                new MapData("Palace", "Y$KFc_hall_b", 0),
                new MapData("Treasure Temple", "Y$KFc_tilt_hall", 0),
                new MapData("Pirate Hub", "Y$KFp_ext", 44438),
                new MapData("Sailing Map", "Y$KFp_at_sea", 0),
                new MapData("Underwater Shipwreck", "Y$KFp_dive", 0),
                new MapData("Dagger Island", "Y$KFp_island_map", 0),
                new MapData("Kaine Island", "Y$KFm_ext", 15022),
                new MapData("Underwater", "Y$KFm_underwater", 0),
                new MapData("Cooper Vault", "Y$KFm_vault", 0),
                new MapData("Gauntlet", "Y$KFm_gauntlet", 0),
                new MapData("Inner Sanctum", "Y$KFm_boss", 0)
            };
            
        }

        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[]
        {
            (addr.loadingState, 4),
            (addr.currentJob, 4),
            (addr.currentCheckpoint, 4),
            (addr.currentMap, 4), 
            (addr.gameSpeed, 4),
            (addr.slyCharacterPtr, 4),
            (addr.activeCharacterPtr, 4),
            (addr.cameraFov, 4),
            (addr.veniceStarted, 4),
            (addr.outbackStarted, 4), 
            (addr.chinaStarted, 4),
            (addr.pirateStarted, 4),
            (addr.mtcTimerValue, 4),
            (addr.currentCharacter, 4),
            (addr.pauseLock, 4),
        };

        public override void CheckInputs(object sender, EventArgs e)
        {
            // TODO: Implement controller combos for Sly 3
            /*
            if (Inputs.RawInputs == ConfigureCombos.saveCombo && inputCheck)
            {
                SavePosition();
                inputCheck = false;
            }
            if (Inputs.RawInputs == ConfigureCombos.loadCombo && inputCheck)
            {
                LoadPosition();
                inputCheck = false;
            }
            if (Inputs.RawInputs == ConfigureCombos.runScriptCombo && inputCheck)
            {
                AttachPS3Form.scripting?.RunCurrentCode();
                inputCheck = false;
            }
            if (Inputs.RawInputs == 0x00 && !inputCheck)
            {
                inputCheck = true;
            }
            */
        }

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            int buttonMaskSubID = api.SubMemory(pid, sly3.addr.inputOffset, 4, (value) =>
            {
                int slyButtonMask = BitConverter.ToInt32(value.Reverse().ToArray(), 0);
                
                int convertedMask = ConvertSlyButtonsToStandardFormat(slyButtonMask);
                
                Inputs.RawInputs = convertedMask;
                Inputs.Mask = Inputs.DecodeMask(convertedMask);
            });
        }

        private int ConvertSlyButtonsToStandardFormat(int slyMask)
        {
            int standardMask = 0;
            
            if ((slyMask & 0x0001) != 0) standardMask |= 0x100;   // Select
            if ((slyMask & 0x0008) != 0) standardMask |= 0x800;   // Start
            if ((slyMask & 0x0010) != 0) standardMask |= 0x1000;  // Up
            if ((slyMask & 0x0020) != 0) standardMask |= 0x2000;  // Right
            if ((slyMask & 0x0040) != 0) standardMask |= 0x4000;  // Down
            if ((slyMask & 0x0080) != 0) standardMask |= 0x8000;  // Left
            if ((slyMask & 0x0400) != 0) standardMask |= 0x4;     // L1
            if ((slyMask & 0x0100) != 0) standardMask |= 0x1;     // L2
            if ((slyMask & 0x0800) != 0) standardMask |= 0x8;     // R1
            if ((slyMask & 0x0200) != 0) standardMask |= 0x2;     // R2
            if ((slyMask & 0x1000) != 0) standardMask |= 0x10;    // Triangle
            if ((slyMask & 0x2000) != 0) standardMask |= 0x20;    // Circle
            if ((slyMask & 0x4000) != 0) standardMask |= 0x40;    // Cross
            if ((slyMask & 0x8000) != 0) standardMask |= 0x80;    // Square
            if ((slyMask & 0x0002) != 0) standardMask |= 0x200;  // L3
            if ((slyMask & 0x0004) != 0) standardMask |= 0x400;  // R3
            
            return standardMask;
        }

        public void Load()
        {
            api.WriteMemory(pid, sly3.addr.loadType, (uint)Sly3Addresses.LoadTypes.Normal);
            api.WriteMemory(pid, sly3.addr.loadTrigger, (uint)1);
        }

        public void LoadMap(int mapIndex)
        {
            if (mapIndex < 0 || mapIndex >= maps.Length)
            {
                func.Status("Select a map to load first.", true);
                return;
            }
            
            MapData selectedMap = maps[mapIndex];
            
            try
            {
                byte[] clearBytes = new byte[64];
                api.WriteMemory(pid, sly3.addr.mapAOB, clearBytes);

                byte[] indicatorBytes = System.Text.Encoding.ASCII.GetBytes(selectedMap.indicator);
                api.WriteMemory(pid, sly3.addr.mapAOB, indicatorBytes);
                
                api.WriteMemory(pid, sly3.addr.spawnLocation, selectedMap.defaultWarp);
                
                api.WriteMemory(pid, sly3.addr.loadTrigger, (uint)1);
            }
            catch (Exception ex)
            {
                func.Status($"Couldn't load the map: {ex.Message}", true);
            }
        }

        public string[] GetMapNames()
        {
            return maps.Select(m => m.naturalName).ToArray();
        }

        private uint GetPlayerCoordsAddress()
        {
            byte[] playerEntityPtrBytes = api.ReadMemory(pid, sly3.addr.playerEntityPointer, 4);
            uint playerEntity = BitConverter.ToUInt32(playerEntityPtrBytes.Reverse().ToArray(), 0);

            byte[] transformPtrBytes = api.ReadMemory(pid, playerEntity + sly3.addr.transformOffset, 4);
            uint transformPtr = BitConverter.ToUInt32(transformPtrBytes.Reverse().ToArray(), 0);

            return transformPtr + sly3.addr.coordsOffsetX;
        }

        public override void SavePosition()
        {
            try
            {
                uint coordsAddress = GetPlayerCoordsAddress();
                
                string position = api.ReadMemoryStr(pid, coordsAddress, 12);
                func.ChangeFileLines("config.txt", position, maps[mapIndex].indicator + "SavedPos" + selectedPositionIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save position: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public override void LoadPosition()
        {
            try
            {
                Console.WriteLine(maps[mapIndex].indicator + "SavedPos" + selectedPositionIndex);
                string position = func.GetConfigData("config.txt", maps[mapIndex].indicator + "SavedPos" + selectedPositionIndex);
                if (position != "")
                {
                    uint coordsAddress = GetPlayerCoordsAddress();
                    
                    api.WriteMemory(pid, coordsAddress, 12, position);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load position: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            int analogLSubID = api.SubMemory(pid, sly3.addr.analogOffsetLeft, 2, (value) =>
            {
                // When disconnected, memory reads as 0. Normal center is 127, so check for 0s
                if (value[0] == 0 && value[1] == 0)
                {
                    // Default to neutral position when disconnected
                    Inputs.ly = 0.0f;
                    Inputs.lx = 0.0f;
                }
                else
                {
                    Inputs.ly = (value[0] - 127) / 127.0f;
                    Inputs.lx = (value[1] - 127) / 127.0f;
                }
            });

            int analogRSubID = api.SubMemory(pid, sly3.addr.analogOffsetRight, 2, (value) =>
            {
                // When disconnected, memory reads as 0. Normal center is 127, so check for 0s
                if (value[0] == 0 && value[1] == 0)
                {
                    // Default to neutral position when disconnected
                    Inputs.ry = 0.0f;
                    Inputs.rx = 0.0f;
                }
                else
                {
                    Inputs.ry = (value[0] - 127) / 127.0f;
                    Inputs.rx = (value[1] - 127) / 127.0f;
                }
            });
        }

        public override void SetupLoadWatcher()
        {
            WatchLoads(addr.loadingState);
        }

        public PositionEditorLayout GetPositionEditorLayout()
        {
            return new PositionEditorLayout
            {
                gameName = "Sly 3",
                warpFilePrefix = "sly3",
                activeCharacterPtr = sly3.addr.activeCharacterPtr,
                mapNameAddress = sly3.addr.mapAOB,
                transformOffset = sly3.addr.transformOffset,
                positionOffset = sly3.addr.coordsOffsetX,
                velocityOffset = 0x1B0,
                entityIdOffset = 0x18,
                healthOffset = sly3.addr.healthEntityOffset,
                gadgetPowerOffset = 0x170,
                opacityOffset = 0x104,
                rotationOffset = 0x1AC,
                infiniteJumpOffset = 0x338,
            };
        }

        /// <summary>
        /// The readable name of a map such as "Y$KFv_ext", or null when it isn't in the list.
        /// </summary>
        public string GetMapDisplayName(string indicator)
        {
            foreach (MapData m in maps)
            {
                if (m.indicator == indicator)
                {
                    return m.naturalName;
                }
            }
            return null;
        }

        public void SetupWebManPopUp()
        {
            if (this.api is Ratchetron)
            {
                int webManPopUpSubID = api.SubMemory(pid, addr.loadingState, 4, (value) =>
                {
                    if (value[0] == 3)
                    {
                        WebMAN.DisplayVersionPopUp(func.api.GetIP());
                    }
                });
            }
        }
        
        public void SetCoinCount(int coins)
        {
            api.WriteMemory(pid, sly3.addr.coinCount, ConvertIntToBytes(coins));
        }

        public void SetHealth(int health)
        {
            byte[] entityPtrBytes = api.ReadMemory(pid, sly3.addr.activeCharacterPtr, 4);
            uint entityPtr = BitConverter.ToUInt32(entityPtrBytes.Reverse().ToArray(), 0);
            if (entityPtr == 0) return;
            api.WriteMemory(pid, entityPtr + sly3.addr.healthEntityOffset, ConvertIntToBytes(health));
        }

        public void SetGadgetPower(int value)
        {
            byte[] entityPtrBytes = api.ReadMemory(pid, sly3.addr.activeCharacterPtr, 4);
            uint entityPtr = BitConverter.ToUInt32(entityPtrBytes.Reverse().ToArray(), 0);
            if (entityPtr == 0) return;
            api.WriteMemory(pid, entityPtr + sly3.addr.gadgetPowerEntityOffset, ConvertIntToBytes(value));
        }

        public void SetGadgetUnlocks(byte[] gadgetBytes)
        {
            api.WriteMemory(pid, sly3.addr.gadgetUnlocks, gadgetBytes);
        }

        public byte[] GetGadgetUnlocks()
        {
            return api.ReadMemory(pid, sly3.addr.gadgetUnlocks, 8);
        }

        // Read gadget binding for a character (returns button binding index, -1 if unbound)
        public int GetGadgetBinding(uint baseAddress, int buttonOffset)
        {
            uint address = baseAddress + (uint)(buttonOffset * 4);
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }

        public void SetGadgetBindings(byte[] bindingBytes) {
            api.WriteMemory(pid, sly3.addr.gadgetBindsSly, bindingBytes);
        }

        // Run file loading helper methods
        public void SetSuckValue(float value)
        {
            byte[] suckBytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(suckBytes);
            }
            api.WriteMemory(pid, sly3.addr.suckValue, suckBytes);
        }

        public void WriteMemoryRegion(uint startAddress, byte[] data)
        {
            var spliceSize = 256; // Write in chunks to avoid overwhelming the API
            for (int i = 0; i < data.Length; i += spliceSize)
            {
                int chunkSize = Math.Min(spliceSize, data.Length - i);
                byte[] chunk = new byte[chunkSize];
                Array.Copy(data, i, chunk, 0, chunkSize);
                api.WriteMemory(pid, startAddress + (uint)i, chunk);
            }
        }

        public void SetMapName(string mapName)
        {
            if (!string.IsNullOrEmpty(mapName))
            {
                // Clear the map AOB region first
                byte[] clearBytes = new byte[64];
                api.WriteMemory(pid, sly3.addr.mapAOB, clearBytes);
                
                // Write the new map name
                byte[] mapBytes = System.Text.Encoding.ASCII.GetBytes(mapName);
                api.WriteMemory(pid, sly3.addr.mapAOB, mapBytes);
            }
        }

        public void SetSpawnLocation(uint location)
        {
            api.WriteMemory(pid, sly3.addr.spawnLocation, location);
        }

        public void SetJobState(int jobId, int cpId, int characterId = -1)
        {
            
            SetActiveCharacter(characterId);
            SetCurrentJob(jobId, cpId);
        }

        private void SetCurrentJob(int jobId, int checkpointId)
        {
            api.WriteMemory(pid, sly3.addr.currentJob, ConvertIntToBytes(jobId));
            api.WriteMemory(pid, sly3.addr.currentCheckpoint, ConvertIntToBytes(checkpointId));
            api.WriteMemory(pid, sly3.addr.currentCheckpoint + 0x4, ConvertIntToBytes(checkpointId));
        }

        public void SetActiveCharacter(int characterId)
        {
            api.WriteMemory(pid, sly3.addr.currentCharacter, ConvertIntToBytes(characterId));
        }

        private byte[] ConvertIntToBytes(int value)
        {
            byte[] byteArray = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            return byteArray;
        }

        private static byte[] ConvertFloatToBytes(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public void SetEpisode6NoobMode()
        {
            api.WriteMemory(pid, 0x6CCDA0, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCDC0, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCD90, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCD80, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCD60, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCDB0, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCDD0, ConvertFloatToBytes(1.0f));
            api.WriteMemory(pid, 0x6CCD70, ConvertFloatToBytes(1.0f));
        }

        /// <summary>
        /// True when the active character's entity exists, which it doesn't during and right after
        /// a load.
        /// </summary>
        public bool IsPlayerLoaded()
        {
            try
            {
                return GetEntityAddress() != 0;
            }
            catch
            {
                return false;
            }
        }

        private uint GetEntityAddress()
        {
            byte[] ptrBytes = api.ReadMemory(pid, sly3.addr.activeCharacterPtr, 4);
            return BitConverter.ToUInt32(ptrBytes.Reverse().ToArray(), 0);
        }

        public void SetInvulnerability(bool enabled)
        {
            uint entity = GetEntityAddress();
            api.WriteMemory(pid, entity + 0x17C, ConvertIntToBytes(enabled ? 1 : 0));
        }

        public void SetInfiniteDoubleJump(bool enabled)
        {
            if (infiniteJumpsFreezeSubID != -1)
            {
                api.ReleaseSubID(infiniteJumpsFreezeSubID);
                infiniteJumpsFreezeSubID = -1;
            }
            if (enabled)
            {
                uint entity = GetEntityAddress();
                infiniteJumpsFreezeSubID = api.FreezeMemory(pid, entity + 0x32C, 0u);
            }
        }

        public void SetSpeedMultiplier(float value)
        {
            uint entity = GetEntityAddress();
            api.WriteMemory(pid, entity + 0x344, ConvertFloatToBytes(value));
        }

        public void SetUndetectable(bool enabled)
        {
            uint entity = GetEntityAddress();
            api.WriteMemory(pid, entity + 0x1150, ConvertIntToBytes(enabled ? 1 : 0));
        }

        public void SetGuardAI(bool disabled)
        {
            api.WriteMemory(pid, sly3.addr.guardAIAddress, ConvertIntToBytes(disabled ? 1 : 0));
        }

        public void SetGameClockFrozen(bool frozen)
        {
            if (gameClockFreezeSubID != -1)
            {
                api.ReleaseSubID(gameClockFreezeSubID);
                gameClockFreezeSubID = -1;
            }
            if (frozen)
            {
                byte[] zero = ConvertFloatToBytes(0.0f);
                api.WriteMemory(pid, sly3.addr.gameSpeed, zero);
                gameClockFreezeSubID = api.FreezeMemory(pid, sly3.addr.gameSpeed, 4,
                    IPS3API.MemoryCondition.Any, zero);
            }
            else
            {
                api.WriteMemory(pid, sly3.addr.gameSpeed, ConvertFloatToBytes(1.0f));
            }
        }

        public void SetDeathBarriers(bool disabled)
        {
            byte[] ptrBytes = api.ReadMemory(pid, sly3.addr.deathBarriersPointer, 4);
            uint ptr = BitConverter.ToUInt32(ptrBytes.Reverse().ToArray(), 0);
            if (ptr == 0)
            {
                // Not set up yet (during a load); writing would land near address 0.
                return;
            }
            api.WriteMemory(pid, ptr + 0x2C, ConvertIntToBytes(disabled ? 1 : 0));
        }

        public void SetCameraFOV(float value)
        {
            byte[] ptrBytes = api.ReadMemory(pid, sly3.addr.cameraPointer, 4);
            uint ptr = BitConverter.ToUInt32(ptrBytes.Reverse().ToArray(), 0);
            api.WriteMemory(pid, ptr + 0x11C, ConvertFloatToBytes(value));
        }

        public void SetDrawDistance(float value)
        {
            byte[] ptrBytes = api.ReadMemory(pid, sly3.addr.cameraPointer, 4);
            uint ptr = BitConverter.ToUInt32(ptrBytes.Reverse().ToArray(), 0);
            api.WriteMemory(pid, ptr + 0x114, ConvertFloatToBytes(value));
        }

        public void TriggerGameLoad(uint loadType = (uint)Sly3Addresses.LoadTypes.Normal)
        {
            api.WriteMemory(pid, sly3.addr.loadType, loadType);
            api.WriteMemory(pid, sly3.addr.loadTrigger, (uint)1);
        }

        public byte[] ReadMemoryRegion(uint startAddress, uint size)
        {
            return api.ReadMemory(pid, startAddress, size);
        }

        public (uint startAddress, uint size) GetMemoryRegionForEpisode(string episode)
        {
            switch (episode)
            {
                case "Episode1":
                    return (0x6CD764, 4096); // Reasonable size for memory region
                case "Episode2":
                    return (0x6CE314, 4096);
                case "Episode3":
                    return (0x6CED80, 4096);
                case "Episode4":
                    return (0x6CF688, 4096);
                case "Episode5":
                    return (0x6D0680, 4096);
                case "Episode6_NoCE":
                    return (0x6D1258, 4096);
                case "Episode6_CE":
                    return (0x6D1258, 4096);
                default:
                    return (0x6CD764, 4096); // Default to Episode 1
            }
        }

        public void SkipCinematic()
        {
            // Skip FMV
            if (api.ReadMemory(pid, sly3.addr.fmvState + 0x3, 1)[0] == 0)
            {
                api.WriteMemory(pid, sly3.addr.fmvState, (uint)2);
            }

            // Skip dialogue
            if (api.ReadMemory(pid, sly3.addr.dialogueFrameCounter + 0x3, 1)[0] > 10)
            {
                api.WriteMemory(pid, sly3.addr.dialogueState, (uint)0);
            }
        }

        public void LoadJob(string jobName)
        {
            switch (jobName)
            {
                case "The Cooper Vault": LoadJobHelper(1798, 1799, 134, "Y$KFm_ext", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case " [TCV] Cave": LoadJobHelper(1798, 1800, 4, "Y$KFm_ext", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case " [TCV] Top": LoadJobHelper(1798, 1801, 4, "Y$KFm_ext", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case " [TCV] Chase": LoadJobHelper(1798, 1803, 4, "Y$KFm_ext", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case " [TCV] End": LoadJobHelper(1798, 1804, 4, "Y$KFm_ext", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Police HQ": LoadJobHelper(2117, 2118, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [PHQ] Exit the vent": LoadJobHelper(2117, 2123, 4, "Y$KFv_interpol", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [PHQ] Crawl to the key": LoadJobHelper(2117, 2131, 4, "Y$KFv_interpol", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [PHQ] Crawl back to Dimitri": LoadJobHelper(2117, 2138, 4, "Y$KFv_interpol", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [PHQ] Pick the lock": LoadJobHelper(2117, 2144, 4, "Y$KFv_interpol", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [PHQ] Carmelita chase": LoadJobHelper(2117, 2154, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Octavio Snap": LoadJobHelper(2261, 2268, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] After 1st picture": LoadJobHelper(2261, 2273, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] Taking 2nd picture": LoadJobHelper(2261, 2276, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] After 2nd picture": LoadJobHelper(2261, 2278, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] Taking 3rd picture": LoadJobHelper(2261, 2281, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] After 3rd picture": LoadJobHelper(2261, 2283, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] Taking 4th picture": LoadJobHelper(2261, 2287, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] After 4th picture": LoadJobHelper(2261, 2291, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [OS] Ferris Wheel": LoadJobHelper(2261, 2297, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Into the Depths": LoadJobHelper(2314, 2315, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [ItD] Enter the Opera House": LoadJobHelper(2314, 1811, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [ItD] Canal": LoadJobHelper(2314, 2327, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [ItD] First laser door": LoadJobHelper(2314, 2335, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [ItD] Computer Room": LoadJobHelper(2314, 2351, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Canal Chase": LoadJobHelper(2360, 2362, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [CC] Start of the chase": LoadJobHelper(2360, 1805, 4, "Y$KFv_canal", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Turf War!": LoadJobHelper(2170, 2172, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TW] Wave #1": LoadJobHelper(2170, 2182, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TW] Wave #2": LoadJobHelper(2170, 2186, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TW] Wave #3": LoadJobHelper(2170, 2190, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TW] Wave #4": LoadJobHelper(2170, 2194, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Tar Ball": LoadJobHelper(2199, 2202, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Run 'n Bomb": LoadJobHelper(2218, 2223, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [RnB] After bomb 1": LoadJobHelper(2218, 2229, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [RnB] Deliever bomb 2": LoadJobHelper(2218, 2232, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [RnB] Climb the tower": LoadJobHelper(2218, 2237, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [RnB] Run to the shop": LoadJobHelper(2218, 2241, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [RnB] Chase Octavio": LoadJobHelper(2218, 2245, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Guard Duty": LoadJobHelper(2382, 2383, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Enter Coffee House #1": LoadJobHelper(2382, 2391, 4, "Y$KFv_apt", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Run to Coffee House #2": LoadJobHelper(2382, 2031, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Enter Coffee House #2": LoadJobHelper(2382, 2404, 4, "Y$KFv_apt", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Run to Coffee House #3": LoadJobHelper(2382, 2411, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Enter Coffee House #3": LoadJobHelper(2382, 2422, 4, "Y$KFv_apt", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [GD] Escape the guards": LoadJobHelper(2382, 2429, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "OP: Tar Be Gone!": LoadJobHelper(2448, 2453, 134, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TBG] Enter the Opera House": LoadJobHelper(2448, 2459, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TBG] Tar Pump room": LoadJobHelper(2448, 2471, 4, "Y$KFv_gauntlet", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TBG] Opera Minigame": LoadJobHelper(2448, 2486, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TBG] Canal Chase": LoadJobHelper(2448, 2505, 4, "Y$KFv_canal", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case " [TBG] Boss fight": LoadJobHelper(2448, 2516, 4, "Y$KFv_ext", "Episode1_MemoryAddress", "Episode1_MemoryData"); break;
                case "Search for the Guru": LoadJobHelper(2605, 2606, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [SftG] Cave entrance": LoadJobHelper(2605, 2610, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [SftG] Guru's home": LoadJobHelper(2605, 2615, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Spelunking": LoadJobHelper(2623, 2624, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [S] Enter the cave": LoadJobHelper(2623, 2629, 4, "Y$KFo_cave_murray", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [S] First piston": LoadJobHelper(2623, 2632, 4, "Y$KFo_cave_murray", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [S] Second pistons": LoadJobHelper(2623, 2636, 4, "Y$KFo_cave_murray", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [S] Drills": LoadJobHelper(2623, 2640, 4, "Y$KFo_cave_murray", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [S] Find the Guru": LoadJobHelper(2623, 2648, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Dark Caves": LoadJobHelper(2829, 2833, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [DC] Enter cave #1": LoadJobHelper(2829, 2839, 4, "Y$KFo_cave_a", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [DC] Escape cave #1": LoadJobHelper(2829, 2846, 4, "Y$KFo_cave_a", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [DC] Find cave #2 entrance": LoadJobHelper(2829, 2850, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [DC] Enter cave #2": LoadJobHelper(2829, 2852, 4, "Y$KFo_cave_b", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [DC] Escape cave #2": LoadJobHelper(2829, 2859, 4, "Y$KFo_cave_b", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Big Truck": LoadJobHelper(2722, 2731, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [BT] Enter Ayer's Rock": LoadJobHelper(2722, 2734, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [BT] Phase 1": LoadJobHelper(2722, 2737, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [BT] Climb the tower": LoadJobHelper(2722, 2742, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [BT] Phase 2": LoadJobHelper(2722, 2742, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [BT] Release the scorpions": LoadJobHelper(2722, 2033, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Unleash the Guru": LoadJobHelper(2690, 1807, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [UtG] Find the drills": LoadJobHelper(2690, 2698, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [UtG] Drills": LoadJobHelper(2690, 2704, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [UtG] Generator": LoadJobHelper(2690, 2715, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "The Claw": LoadJobHelper(2650, 2654, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [TC] Phase 1": LoadJobHelper(2650, 2656, 4, "Y$KFo_arena", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [TC] Phase 2": LoadJobHelper(2650, 2668, 4, "Y$KFo_arena", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [TC] Phase 3": LoadJobHelper(2650, 2678, 4, "Y$KFo_arena", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Lemon Rage": LoadJobHelper(2756, 2757, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [LR] Enter the bar": LoadJobHelper(2756, 2759, 4, "Y$KFo_bar", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [LR] Drinking contest": LoadJobHelper(2756, 1806, 4, "Y$KFo_bar", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [LR] Bar fight": LoadJobHelper(2756, 2779, 4, "Y$KFo_bar", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [LR] Boss fight": LoadJobHelper(2756, 2788, 4, "Y$KFo_bar", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Hungry Croc": LoadJobHelper(2804, 2810, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [HC] Feed the Croc": LoadJobHelper(2804, 2814, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [HC] Flashlight guard": LoadJobHelper(2804, 2817, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "OP: Moon Crash": LoadJobHelper(2867, 2869, 134, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [MC] Sleep darts": LoadJobHelper(2867, 2891, 4, "Y$KFo_ext", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [MC] Truck": LoadJobHelper(2867, 2925, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case " [MC] Climb": LoadJobHelper(2867, 2934, 4, "Y$KFo_quarry", "Episode2_MemoryAddress", "Episode2_MemoryData"); break;
                case "Hidden Flight Roster": LoadJobHelper(3007, 3009, 134, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [HFR] Exit the hotel": LoadJobHelper(3007, 1818, 4, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [HFR] Find the castle": LoadJobHelper(3007, 1818, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [HFR] Castle Climb": LoadJobHelper(3007, 3022, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [HFR] Reach the hangar": LoadJobHelper(3007, 3031, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [HFR] Hangar": LoadJobHelper(3007, 3034, 4, "Y$KFh_hangar_b", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Frame Team Belgium": LoadJobHelper(3049, 3051, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTB] Find the pilot": LoadJobHelper(3049, 3053, 4, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTB] Pickpocket": LoadJobHelper(3049, 3058, 4, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTB] Guru section": LoadJobHelper(3049, 3068, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTB] Sly section": LoadJobHelper(3049, 3075, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Frame Team Iceland": LoadJobHelper(3085, 3086, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Rowing #1": LoadJobHelper(3085, 3088, 4, "Y$KFh_sewer", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Platforming #1": LoadJobHelper(3085, 3090, 4, "Y$KFh_sewer", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Hotel": LoadJobHelper(3085, 3094, 4, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Platforming #2": LoadJobHelper(3085, 3102, 4, "Y$KFh_sewer", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Rowing #2": LoadJobHelper(3085, 3104, 4, "Y$KFh_sewer", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Find the hangar": LoadJobHelper(3085, 3108, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [FTI] Hangar": LoadJobHelper(3085, 3112, 4, "Y$KFh_hangar_a", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Cooper Hangar Defence": LoadJobHelper(3125, 3126, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [CHD] Muggshot": LoadJobHelper(3125, 3126, 4, "Y$KFh_hangar_c", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [CHD] Sewers #1": LoadJobHelper(3125, 3140, 4, "Y$KFh_hangar_c", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [CHD] Sewers #2": LoadJobHelper(3125, 3146, 4, "Y$KFh_hangar_c", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [CHD] RC Chopper": LoadJobHelper(3125, 3153, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "ACES Semi-finals": LoadJobHelper(3167, 3164, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [AS] Dogfight": LoadJobHelper(3167, 1808, 4, "Y$KFh_dogfight", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Giant Wolf Massacre": LoadJobHelper(3225, 3227, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [GWM] Guru section": LoadJobHelper(3225, 3235, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Windmill Firewall": LoadJobHelper(3187, 3193, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Hack #1": LoadJobHelper(3187, 3196, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Find computer #2": LoadJobHelper(3187, 3201, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Hack #2": LoadJobHelper(3187, 3204, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Find computer #3": LoadJobHelper(3187, 3209, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Hack #3": LoadJobHelper(3187, 3212, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Find computer #4": LoadJobHelper(3187, 3216, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [WF] Hack #4": LoadJobHelper(3187, 3219, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "Beauty and the Beast": LoadJobHelper(3248, 3249, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [BatB] Find Muggshot": LoadJobHelper(3248, 3249, 4, "Y$KFh_hotel", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [BatB] Find Carmelita": LoadJobHelper(3248, 3256, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [BatB] Muggshot fight": LoadJobHelper(3248, 1809, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "OP: Turbo Dominant Eagle": LoadJobHelper(3281, 3283, 134, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [TDE] Murray section": LoadJobHelper(3281, 3303, 4, "Y$KFh_ext", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [TDE] Dogfight": LoadJobHelper(3281, 3317, 4, "Y$KFh_dogfight", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case " [TDE] Boss fight": LoadJobHelper(3281, 3325, 4, "Y$KFh_dogfight", "Episode3_MemoryAddress", "Episode3_MemoryData"); break;
                case "King of Fire": LoadJobHelper(3403, 3405, 134, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Murray": LoadJobHelper(3403, 3406, 4, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Penelope": LoadJobHelper(3403, 3411, 4, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Sly": LoadJobHelper(3403, 3425, 4, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Bentley": LoadJobHelper(3403, 3433, 4, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Guru": LoadJobHelper(3403, 3442, 4, "Y$KFc_intro", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [KoF] Flashback": LoadJobHelper(3403, 3454, 4, "Y$KFc_flashback", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Get a Job": LoadJobHelper(3471, 3473, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GaJ] Talk to Tsao": LoadJobHelper(3471, 3473, 4, "Y$KFc_hall_b", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GaJ] Picture #1": LoadJobHelper(3471, 3480, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GaJ] Picture #2": LoadJobHelper(3471, 3487, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GaJ] Picture #3": LoadJobHelper(3471, 3498, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GaJ] Pictures of Tsao": LoadJobHelper(3471, 3511, 4, "Y$KFc_hall_b", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Tearful Reunion": LoadJobHelper(3531, 3536, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [TR] Free the van": LoadJobHelper(3531, 3541, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [TR] Defend Murray #1": LoadJobHelper(3531, 3545, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [TR] Defend Murray #2": LoadJobHelper(3531, 3550, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Grapple-Cam Break-in": LoadJobHelper(3562, 3565, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Steal the keys": LoadJobHelper(3562, 3570, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Exchange the keys": LoadJobHelper(3562, 3576, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Break in the building": LoadJobHelper(3562, 3580, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Lure #1": LoadJobHelper(3562, 3584, 4, "Y$KFc_hall_a", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Lure #2": LoadJobHelper(3562, 3590, 4, "Y$KFc_hall_a", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Find the computer": LoadJobHelper(3562, 3595, 4, "Y$KFc_hall_a", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [GCB] Hack": LoadJobHelper(3562, 3600, 4, "Y$KFc_hall_a", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Laptop Retrieval": LoadJobHelper(3606, 3607, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [LR] Find the computer": LoadJobHelper(3606, 3609, 4, "Y$KFc_hall_b", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [LR] Hack": LoadJobHelper(3606, 3613, 4, "Y$KFc_hall_b", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [LR] Guru section": LoadJobHelper(3606, 3618, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [LR] Tsao #1": LoadJobHelper(3606, 1810, 4, "Y$KFc_forest", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [LR] Tsao #2": LoadJobHelper(3606, 3639, 4, "Y$KFc_forest", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Vampiric Demise": LoadJobHelper(3651, 3654, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Panda King's house": LoadJobHelper(3651, 3654, 4, "Y$KFc_apt", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Tutorial": LoadJobHelper(3651, 3662, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Go to safe #2": LoadJobHelper(3651, 3673, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Defend Sly #1": LoadJobHelper(3651, 3675, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Go to safe #3": LoadJobHelper(3651, 3679, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Defend Sly #2": LoadJobHelper(3651, 3681, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Find the gravestone": LoadJobHelper(3651, 3685, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [VD] Destroy the gravestone": LoadJobHelper(3651, 3689, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "Down the Line": LoadJobHelper(3693, 3694, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [DtL] RC section": LoadJobHelper(3693, 3701, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "A Battery of Peril": LoadJobHelper(3705, 3706, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [ABoP] Carmelita": LoadJobHelper(3705, 3709, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [ABoP] Stabilize the battery": LoadJobHelper(3705, 3718, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "OP: Wedding Crasher": LoadJobHelper(3733, 3734, 134, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Open the door": LoadJobHelper(3733, 3735, 4, "Y$KFc_tilt_hall", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Reach the computer": LoadJobHelper(3733, 3737, 4, "Y$KFc_tilt_hall", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Destroy alarms": LoadJobHelper(3733, 3742, 4, "Y$KFc_tilt_hall", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Open the trapdoor": LoadJobHelper(3733, 3779, 4, "Y$KFc_tilt_hall", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Go to the palace": LoadJobHelper(3733, 3782, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Protect vases": LoadJobHelper(3733, 3787, 4, "Y$KFc_hall_b", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Lure Carmelita": LoadJobHelper(3733, 3794, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case " [WC] Dragon fight": LoadJobHelper(3733, 3807, 4, "Y$KFc_ext", "Episode4_MemoryAddress", "Episode4_MemoryData"); break;
                case "The Talk of Pirates": LoadJobHelper(3867, 3871, 134, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Stone-Jake": LoadJobHelper(3867, 3875, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Find the loutenant": LoadJobHelper(3867, 3882, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Steal the bootleg": LoadJobHelper(3867, 3884, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Find Ned": LoadJobHelper(3867, 3891, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Chase Ned": LoadJobHelper(3867, 3895, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Return to Pete": LoadJobHelper(3867, 3907, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [TToP] Vinegar Talk": LoadJobHelper(3867, 3911, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Dynamic Duo": LoadJobHelper(3926, 3927, 134, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Go to Skull Keep": LoadJobHelper(3926, 3944, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Floating boxes": LoadJobHelper(3926, 3950, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Beat the guards": LoadJobHelper(3926, 3957, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Lure Penelope #1": LoadJobHelper(3926, 3977, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Lure Penelope #2": LoadJobHelper(3926, 3982, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DD] Lure Penelope #3": LoadJobHelper(3926, 3994, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Jollyboat of Destruction": LoadJobHelper(4055, 4056, 134, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [JoD] Harbor patrol": LoadJobHelper(4055, 4068, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [JoD] Cutter": LoadJobHelper(4055, 4079, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "X Marks the Spot": LoadJobHelper(4009, 4011, 134, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Row to the ship": LoadJobHelper(4009, 4015, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Beat the guards": LoadJobHelper(4009, 4019, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Escape the bay": LoadJobHelper(4009, 4023, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Sail to Dagger Isle": LoadJobHelper(4009, 4030, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Sink the ship": LoadJobHelper(4009, 4032, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Dagger Isle": LoadJobHelper(4009, 4040, 4, "Y$KFp_island_map", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Statue": LoadJobHelper(4009, 4042, 4, "Y$KFp_island_map", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [XMtS] Dig up the chest": LoadJobHelper(4009, 2040, 4, "Y$KFp_island_map", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Crusher from the Depths": LoadJobHelper(4088, 4091, 134, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [CftD] Shoot Crusher #1": LoadJobHelper(4088, 4097, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [CftD] Shoot the tentacles": LoadJobHelper(4088, 4101, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [CftD] Shoot Crusher #2": LoadJobHelper(4088, 4105, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [CftD] Cannons": LoadJobHelper(4088, 4109, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Deep Sea Danger": LoadJobHelper(4117, 4118, 134, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DSD] Underwater": LoadJobHelper(4117, 4123, 4, "Y$KFp_dive", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DSD] Collars": LoadJobHelper(4117, 4125, 4, "Y$KFp_dive", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DSD] Fish": LoadJobHelper(4117, 4128, 4, "Y$KFp_dive", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [DSD] Hammersharks": LoadJobHelper(4117, 4132, 4, "Y$KFp_dive", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Battle on the High Seas": LoadJobHelper(4136, 4137, 134, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [BotHS] Sail to fight #2": LoadJobHelper(4136, 4141, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [BotHS] Fight #2": LoadJobHelper(4136, 4145, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [BotHS] Sail to fight #3": LoadJobHelper(4136, 4149, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [BotHS] Fight #3": LoadJobHelper(4136, 4153, 4, "Y$KFp_at_sea", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "OP: Reverse Double-Cross": LoadJobHelper(4161, 4163, 134, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [RDC] Insult LefWee": LoadJobHelper(4161, 4166, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [RDC] Escape the ship": LoadJobHelper(4161, 4180, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [RDC] Skull Keep": LoadJobHelper(4161, 4185, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [RDC] Crusher": LoadJobHelper(4161, 4192, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case " [RDC] Boss fight": LoadJobHelper(4161, 4202, 4, "Y$KFp_ext", "Episode5_MemoryAddress", "Episode5_MemoryData"); break;
                case "Carmelita to the Rescue": LoadJobHelper(4342, 1842, 134, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [CttR] Talk with Dr. M": LoadJobHelper(4342, 4382, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "A Deadly Bite": LoadJobHelper(4384, 1843, 134, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [ADB] Sharks #2": LoadJobHelper(4384, 4396, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [ADB] Sharks #3": LoadJobHelper(4384, 4400, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [ADB] Sharks #4": LoadJobHelper(4384, 4404, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [ADB] Return to the boat": LoadJobHelper(4384, 4408, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "The Dark Current": LoadJobHelper(4411, 1844, 134, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TDC] Pinchers": LoadJobHelper(4411, 4413, 4, "Y$KFm_underwater", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TDC] Mutant fish": LoadJobHelper(4411, 4420, 4, "Y$KFm_underwater", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "Bump-Charge-Jump": LoadJobHelper(4427, 1845, 134, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [BCJ] Track #1": LoadJobHelper(4427, 4433, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [BCJ] Track #2": LoadJobHelper(4427, 4439, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [BCJ] Track #3": LoadJobHelper(4427, 4444, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "Danger in the Skies": LoadJobHelper(4451, 1846, 134, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [DitS] Turrets": LoadJobHelper(4451, 4453, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [DitS] Bats": LoadJobHelper(4451, 4472, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [DitS] Dogfight": LoadJobHelper(4451, 4478, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [DitS] Paraglide": LoadJobHelper(4451, 4486, 4, "Y$KFm_ext", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "The Ancestors' Gauntlet": LoadJobHelper(4494, 1847, 134, "Y$KFm_vault", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Enter the gauntlet": LoadJobHelper(4494, 4495, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Slytunkhamen Cooper II": LoadJobHelper(4494, 4498, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Sir Galleth Cooper": LoadJobHelper(4494, 4500, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Salim al-Kupar": LoadJobHelper(4494, 4502, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Slaigh MacCooper": LoadJobHelper(4494, 4504, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Rioichi Cooper": LoadJobHelper(4494, 4506, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Henriette Cooper": LoadJobHelper(4494, 4508, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Tennessee Cooper": LoadJobHelper(4494, 4510, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Thaddeus W. Cooper III": LoadJobHelper(4494, 4512, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Otto van Cooper": LoadJobHelper(4494, 4514, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [TAG] Connor Cooper": LoadJobHelper(4494, 4516, 4, "Y$KFm_gauntlet", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "Stand Your Ground": LoadJobHelper(4520, 1848, 134, "Y$KFm_vault", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [SYG] Checkpoint #1": LoadJobHelper(4520, 4531, 4, "Y$KFm_vault", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [SYG] Checkpoint #2": LoadJobHelper(4520, 4534, 134, "Y$KFm_vault", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [SYG] Place the treasures": LoadJobHelper(4520, 4538, 134, "Y$KFm_vault", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "Final Legacy": LoadJobHelper(4558, 1849, 134, "Y$KFm_boss", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case " [FL] Carmelita section": LoadJobHelper(4558, 2041, 134, "Y$KFm_boss", "Episode6_NoCE_MemoryAddress", "Episode6_NoCE_MemoryData"); break;
                case "Sly Tutorial #1": LoadJobHelper(4590, 4592, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Sly Tutorial #2": LoadJobHelper(4609, 4611, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Sly Tutorial #3": LoadJobHelper(4609, 4611, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Sly Tutorial #4": LoadJobHelper(4609, 4611, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Bentley Tutorial": LoadJobHelper(4609, 4611, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                case "Murray Tutorial": LoadJobHelper(4725, 4727, 134, "Y$KFi_trainer", "Episode0_MemoryAddress", "Episode0_MemoryData"); break;
                default: break;
            }
        }

        private void LoadJobHelper(int jobId, int checkpointId, int loadMode, string mapName, string stateStartAddressKey, string stateValuesKey)
        {
            SetMapName(mapName);
            SetActiveCharacter((int)-1);

            // TODO: Proper DAG logic, no we just replace the state with the beginning of the episode state
            string stateAddress = func.GetConfigData("data/s3_run_file_config.txt", stateStartAddressKey);
            string stateValues = func.GetConfigData("data/s3_run_file_config.txt", stateValuesKey);

            if (string.IsNullOrEmpty(stateAddress) || string.IsNullOrEmpty(stateValues))
            {
                Console.WriteLine($"Error: Missing state data for job {jobId} checkpoint {checkpointId}");
                return;
            }

            byte[] stateBytes = ConvertMemoryDataString(stateValues);
            WriteMemoryRegion((uint)Convert.ToInt32(stateAddress, 16), stateBytes);

            SetJobState(jobId, checkpointId);
            TriggerGameLoad((uint)134);
            return;
        }

        public void AbandonJob(string jobName)
        {
            SetJobState(-1, -1);
            
        }

        // Checking if user has gadgets configs in config.txt
        // If not, copy the defaults
        public void CheckRunFileConfig()
        {
            string[] keys = new string[] { "Episode1", "Episode2", "Episode3", "Episode4", "Episode5", "Episode6_NoCE", "Episode6_CE" };
            foreach (var key in keys)
            {
                string gadgetHex = func.GetConfigData("config.txt", key + "_GadgetUnlocks");
                string bindingHex = func.GetConfigData("config.txt", key + "_GadgetBindings");

                if (string.IsNullOrEmpty(gadgetHex))
                {
                    gadgetHex = func.GetConfigData("data/s3_run_file_config.txt", key + "_GadgetUnlocks");
                    func.ChangeFileLines("config.txt", gadgetHex, key + "_GadgetUnlocks");
                }

                if (string.IsNullOrEmpty(bindingHex))
                {
                    bindingHex = func.GetConfigData("data/s3_run_file_config.txt", key + "_GadgetBindings");
                    func.ChangeFileLines("config.txt", bindingHex, key + "_GadgetBindings");
                }
            }
            return;
        }

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
    }
}