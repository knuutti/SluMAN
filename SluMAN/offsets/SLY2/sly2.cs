using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    public class Sly2Addresses
    {
        private class AddressValues
        {
            public uint inputOffset;
            public uint analogOffsetLeft;
            public uint analogOffsetRight;

            public uint slyCharacterPtr;
            public uint activeCharacterPtr;

            public uint transformOffset;
            public uint coordsOffsetX;
            public uint coordsOffsetY;
            public uint coordsOffsetZ;

            public uint coinCount;
            public uint currentCharacter;
            public uint cameraFov;
            public uint cameraParameter;
            public uint loadingState;
            public uint currentJobId;
            public uint currentCheckpointId;
            public uint currentMapId;
            public uint gameSpeed;
            public uint pauseMenuState;

            public uint dialogueState;
            public uint dialogueFrameCounter;
            public uint fmvState;

            public uint mapAOB;
            public uint spawnLocation;
            public uint loadType;
            public uint loadTrigger;

            public uint gadgetUnlocks;
            public uint gadgetBindsSly;
            public uint gadgetBindsBentley;
            public uint gadgetBindsMurray;

            public uint parisStarted;
            public uint templeStarted;
            public uint prisonStarted;
            public uint castleStarted;
            public uint trainStarted;
            public uint sawmillStarted;
            public uint blimpStarted;
            public uint parisCinemaState;
            public uint rajanHealth;

            // Per-character health and gadget power (juice). 0 means not known for this version.
            public uint healthSly;
            public uint healthBentley;
            public uint healthMurray;
            public uint juiceSly;
            public uint juiceBentley;
            public uint juiceMurray;

            public AddressValues Clone()
            {
                return (AddressValues)MemberwiseClone();
            }
        }

        public const string GameIdKOR = "NPHA80175";
        public const string GameIdPAL = "NPEA00342";
        public const string DefaultGameId = GameIdKOR;

        private static readonly Dictionary<string, AddressValues> gameVersions = CreateGameVersions();

        private readonly string gameId;
        private readonly AddressValues values;

        public string GameId => gameId;

        public Sly2Addresses(string gameId = DefaultGameId)
        {
            string normalizedGameId = NormalizeGameId(gameId);

            if (!gameVersions.TryGetValue(normalizedGameId, out values))
            {
                normalizedGameId = DefaultGameId;
                values = gameVersions[DefaultGameId];
            }

            this.gameId = normalizedGameId;
        }

        public static bool IsSupportedGameId(string gameId)
        {
            return gameVersions.ContainsKey(NormalizeGameId(gameId));
        }

        public static string GetDisplayName(string gameId)
        {
            switch (NormalizeGameId(gameId))
            {
                case GameIdKOR:
                    return "SLY 2 (KOR, PSN)";
                case GameIdPAL:
                    return "SLY 2 (EU, PSN)";
                default:
                    return "SLY 2";
            }
        }

        private static string NormalizeGameId(string gameId)
        {
            return string.IsNullOrWhiteSpace(gameId) ? DefaultGameId : gameId.Trim().ToUpperInvariant();
        }

        private static Dictionary<string, AddressValues> CreateGameVersions()
        {
            var versions = new Dictionary<string, AddressValues>(StringComparer.OrdinalIgnoreCase);

            var kor = new AddressValues
            {
                inputOffset = 0x500F76,
                analogOffsetLeft = 0x500EFC,
                analogOffsetRight = 0x500F30,

                slyCharacterPtr = 0x502100,
                activeCharacterPtr = 0x49E290,

                transformOffset = 0x44,
                coordsOffsetX = 0x130,
                coordsOffsetY = 0x134,
                coordsOffsetZ = 0x138,

                coinCount = 0x7A83B0,
                currentCharacter = 0x7A830C,
                cameraFov = 0x49E054,
                cameraParameter = 0x49E07C,
                loadingState = 0x7A7200,
                currentJobId = 0x4FEBF4,
                currentCheckpointId = 0x4FEBF8,
                currentMapId = 0x7B4CE0,
                gameSpeed = 0x49DF80,
                pauseMenuState = 0x4FFE84,

                dialogueState = 0x39E4BF70,
                dialogueFrameCounter = 0x39E4BF54,
                fmvState = 0x9066FC,

                mapAOB = 0x7B4C58,
                spawnLocation = 0x7B4C98,
                loadType = 0x7B4C54,
                loadTrigger = 0x7B4C50,

                gadgetUnlocks = 0x7A83A8,
                gadgetBindsSly = 0x7A836C,
                gadgetBindsBentley = 0x7A8384,
                gadgetBindsMurray = 0x7A839C,

                parisStarted = 0x7A9D84,
                templeStarted = 0x7AA9F0,
                prisonStarted = 0x7AB38C,
                castleStarted = 0x7ABBE8,
                trainStarted = 0x7ABF28,
                sawmillStarted = 0x7AC818,
                blimpStarted = 0x7AD150,
                parisCinemaState = 0x7A9AC0,
                rajanHealth = 0x35C2AE2C,

                // From SluMAN-EXT (utility.lua)
                healthSly = 0x7A8360,
                healthBentley = 0x7A8378,
                healthMurray = 0x7A8390,
                juiceSly = 0x7A8364,
                juiceBentley = 0x7A837C,
                juiceMurray = 0x7A8394,
            };

            versions[GameIdKOR] = kor;

            // PAL addresses
            var pal = kor.Clone();
            pal.coinCount = 0x7A8330;
            pal.loadTrigger = 0x7B4BD0;
            pal.mapAOB = 0x7B4BD8;
            pal.spawnLocation = 0x7B4C68;
            pal.inputOffset = 0x500EF6;
            pal.analogOffsetLeft = 0x500E7C;
            pal.analogOffsetRight = 0x500EB0;
            // Not found for PAL yet.
            pal.healthSly = 0;
            pal.healthBentley = 0;
            pal.healthMurray = 0;
            pal.juiceSly = 0;
            pal.juiceBentley = 0;
            pal.juiceMurray = 0;

            versions[GameIdPAL] = pal;

            return versions;
        }

        // Inputs
        public uint inputOffset => values.inputOffset;
        public uint analogOffsetLeft => values.analogOffsetLeft;
        public uint analogOffsetRight => values.analogOffsetRight;

        // Pointers
        public uint slyCharacterPtr => values.slyCharacterPtr;
        public uint activeCharacterPtr => values.activeCharacterPtr;

        // Offsets
        public uint transformOffset => values.transformOffset;
        public uint coordsOffsetX => values.coordsOffsetX;
        public uint coordsOffsetY => values.coordsOffsetY;
        public uint coordsOffsetZ => values.coordsOffsetZ;


        // Utility
        public uint coinCount => values.coinCount;
        public uint currentCharacter => values.currentCharacter;
        public uint cameraFov => values.cameraFov;
        public uint cameraParameter => values.cameraParameter;
        public uint loadingState => values.loadingState;
        public uint currentJobId => values.currentJobId;
        public uint currentCheckpointId => values.currentCheckpointId;
        public uint currentMapId => values.currentMapId;
        public uint gameSpeed => values.gameSpeed;
        public uint pauseMenuState => values.pauseMenuState;

        // Cutscene skipping
        public uint dialogueState => values.dialogueState;
        public uint dialogueFrameCounter => values.dialogueFrameCounter;
        public uint fmvState => values.fmvState;

        public uint mapAOB => values.mapAOB;
        public uint spawnLocation => values.spawnLocation;
        public uint loadType => values.loadType;
        public uint loadTrigger => values.loadTrigger;

        // Gadgets
        public uint gadgetUnlocks => values.gadgetUnlocks;
        public uint gadgetBindsSly => values.gadgetBindsSly;
        public uint gadgetBindsBentley => values.gadgetBindsBentley;
        public uint gadgetBindsMurray => values.gadgetBindsMurray;

        // Values for autosplitter
        public uint parisStarted => values.parisStarted;
        public uint templeStarted => values.templeStarted;
        public uint prisonStarted => values.prisonStarted;
        public uint castleStarted => values.castleStarted;
        public uint trainStarted => values.trainStarted; // 2 = CC not started, 3 = CC started
        public uint sawmillStarted => values.sawmillStarted;
        public uint blimpStarted => values.blimpStarted;
        public uint parisCinemaState => values.parisCinemaState;
        public uint rajanHealth => values.rajanHealth;
        public uint healthSly => values.healthSly;
        public uint healthBentley => values.healthBentley;
        public uint healthMurray => values.healthMurray;
        public uint juiceSly => values.juiceSly;
        public uint juiceBentley => values.juiceBentley;
        public uint juiceMurray => values.juiceMurray;

        /// <summary>
        /// True when this version's per-character health and gadget power addresses are known.
        /// </summary>
        public bool HasCharacterStats => values.healthSly != 0 && values.juiceSly != 0;

        public enum LoadTypes : uint
        {
            Fast = 0,
            Normal = 6,
            Reset = 15,
            RunFile = 18,
            Job = 134,
        }
    }

    public class sly2 : IGame, IAutosplitterAvailable
    {
        public static Sly2Addresses addr = new Sly2Addresses();

        public static bool SupportsGameId(string gameId)
        {
            return Sly2Addresses.IsSupportedGameId(gameId);
        }

        public static string GetDisplayName(string gameId)
        {
            return Sly2Addresses.GetDisplayName(gameId);
        }

        public uint mapIndex;

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

        public sly2(IPS3API api, string gameNameId = null) : base(api)
        {
            addr = new Sly2Addresses(string.IsNullOrWhiteSpace(gameNameId) ? api.getGameTitleID() : gameNameId);

            this.maps = new MapData[]
            {
                new MapData("Museum", "Y$KFjb_intro", 148457),
                new MapData("DVD Menu", "Y$KFdvd_menu", 0),
                new MapData("Paris Hub", "Y$KFf_nightclub_exterior", 27280),
                new MapData("Wine Cellar", "Y$KFf_nightclub_cellar", 397),
                new MapData("Disco", "Y$KFf_nightclub_disco", 401),
                new MapData("Print Room", "Y$KFf_nightclub_heist", 121308),
                new MapData("Theater", "Y$KFf_nightclub_moulinrouge", 400),
                new MapData("Pump Room", "Y$KFf_nightclub_waterpump", 399),
                new MapData("Palace Hub", "Y$KFi_palace_ext", 105705),
                new MapData("Hotel", "Y$KFi_palace_int", 105779),
                new MapData("Basement", "Y$KFi_palace_basement", 105764),
                new MapData("Ballroom", "Y$KFi_palace_ballroom", 105063),
                new MapData("Temple Hub", "Y$KFi_temple_ext", 96699),
                new MapData("Temple Interior", "Y$KFi_temple_int", 398),
                new MapData("Spice Grinder", "Y$KFi_temple_int", 400),
                new MapData("Prison Hub", "Y$KFp_prison_ext", 76660),
                new MapData("Jail", "Y$KFp_prison_int", 76806),
                new MapData("Prison Vault", "Y$KFp_prison_vault", 0),
                new MapData("Castle Hub", "Y$KFp_castle_ext", 59827),
                new MapData("Waterrails", "Y$KFp_castle_waterrails", 33438),
                new MapData("Guardbreak", "Y$KFp_castle_guardbreak", 59836),
                new MapData("Wolftomb", "Y$KFp_castle_wolftomb", 0),
                new MapData("Heist", "Y$KFp_castle_heist", 59889),
                new MapData("Ewoktrainer", "Y$KFp_castle_ewoktrainer", 393),
                new MapData("Ewoktomb", "Y$KFp_castle_ewoktomb", 393),
                new MapData("Ewokhall", "Y$KFp_castle_ewokhall", 393),
                new MapData("Ewokwater", "Y$KFp_castle_ewokwater", 393),
                new MapData("Canada Hub", "Y$KFc_train_ext", 49327),
                new MapData("Cabins", "Y$KFc_train_cabins", 49224),
                new MapData("Train A", "Y$KFc_train_a", 393),
                new MapData("Train B", "Y$KFc_train_b", 393),
                new MapData("Train C", "Y$KFc_train_c", 393),
                new MapData("Sawmill Hub", "Y$KFc_sawmill_ext", 33583),
                new MapData("Mulch", "Y$KFc_sawmill_mulch", 392),
                new MapData("Burn", "Y$KFc_sawmill_burn", 393),
                new MapData("Lighthouse", "Y$KFc_sawmill_lighthouse", 33551),
                new MapData("Bearcave", "Y$KFc_sawmill_bearcave", 33438),
                new MapData("Bison's Saw", "Y$KFc_sawmill_boss", 393),
                new MapData("Blimp Hub", "Y$KFa_blimp_ext", 17451),
                new MapData("Arpeggio's Blimp", "Y$KFa_blimp_int", 17372),
                new MapData("Engine 1", "Y$KFa_blimp_engine_room_murray", 393),
                new MapData("Engine 2", "Y$KFa_blimp_engine_room_bentley", 393),
                new MapData("Engine 3", "Y$KFa_blimp_engine_room_jt", 393),
                new MapData("Clock-La", "Y$KFa_blimp_boss_final", 11233),
            };

        }

        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[]
        {
            (addr.loadingState, 4),
            (addr.currentJobId, 4),
            (addr.currentCheckpointId, 4),
            (addr.currentMapId, 4),
            (addr.gameSpeed, 4),
            (addr.slyCharacterPtr, 4),
            (addr.activeCharacterPtr, 4),
            (addr.cameraFov, 4),
            (addr.cameraParameter, 4),
            (addr.pauseMenuState, 4),
            (addr.parisStarted, 4),
            (addr.rajanHealth, 4),
            (addr.templeStarted, 4),
            (addr.prisonStarted, 4),
            (addr.castleStarted, 4),
            (addr.trainStarted, 4),
            (addr.sawmillStarted, 4),
            (addr.blimpStarted, 4),
            (addr.parisCinemaState, 4),
        };

        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            SubscribeSlyButtons(sly2.addr.inputOffset);
        }

        public void Load()
        {
            api.WriteMemory(pid, sly2.addr.loadType, (uint)Sly2Addresses.LoadTypes.Normal);
            api.WriteMemory(pid, sly2.addr.loadTrigger, (uint)1);
        }

        public void CheckRunFileConfig()
        {
            string[] keys = new string[] { "Episode1", "Episode2", "Episode3", "Episode4", "Episode5", "Episode6", "Episode7", "Episode8" };
            foreach (var key in keys)
            {
                string gadgetHex = func.GetConfigData("config.txt", key + "_Sly2GadgetUnlocks");
                string bindingHex = func.GetConfigData("config.txt", key + "_Sly2GadgetBindings");

                if (string.IsNullOrEmpty(gadgetHex))
                {
                    gadgetHex = func.GetConfigData("data/s2_run_file_config.txt", key + "_Sly2GadgetUnlocks");
                    func.ChangeFileLines("config.txt", gadgetHex, key + "_Sly2GadgetUnlocks");
                }

                if (string.IsNullOrEmpty(bindingHex))
                {
                    bindingHex = func.GetConfigData("data/s2_run_file_config.txt", key + "_Sly2GadgetBindings");
                    func.ChangeFileLines("config.txt", bindingHex, key + "_Sly2GadgetBindings");
                }
            }
            return;
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
                api.WriteMemory(pid, sly2.addr.mapAOB, clearBytes);

                byte[] indicatorBytes = System.Text.Encoding.ASCII.GetBytes(selectedMap.indicator);
                api.WriteMemory(pid, sly2.addr.mapAOB, indicatorBytes);

                api.WriteMemory(pid, sly2.addr.spawnLocation, selectedMap.defaultWarp);

                api.WriteMemory(pid, sly2.addr.loadTrigger, (uint)Sly2Addresses.LoadTypes.Normal);
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
            byte[] playerEntityPtrBytes = api.ReadMemory(pid, sly2.addr.activeCharacterPtr, 4);
            uint playerEntity = BitConverter.ToUInt32(playerEntityPtrBytes.Reverse().ToArray(), 0);

            byte[] transformPtrBytes = api.ReadMemory(pid, playerEntity + sly2.addr.transformOffset, 4);
            uint transformPtr = BitConverter.ToUInt32(transformPtrBytes.Reverse().ToArray(), 0);

            return transformPtr + sly2.addr.coordsOffsetX;
        }

        /// <summary>
        /// The config key a position slot is stored under, per map so each map keeps its own slots.
        /// </summary>
        private string SavedPositionKey()
        {
            string mapIndicator = ReadMapIndicator(sly2.addr.mapAOB);
            if (mapIndicator == "")
            {
                throw new InvalidOperationException("Couldn't read the current map.");
            }
            return SavedPositionKey(mapIndicator, selectedPositionIndex);
        }

        public override void SavePosition()
        {
            string key = SavedPositionKey();
            uint coordsAddress = GetPlayerCoordsAddress();

            string position = api.ReadMemoryStr(pid, coordsAddress, 12);
            func.ChangeFileLines("config.txt", position, key);
            OnPositionSaved();
        }

        public override void LoadPosition()
        {
            string key = SavedPositionKey();
            string position = func.GetConfigData("config.txt", key);
            if (position == "")
            {
                Console.WriteLine($"No saved position for {key}");
                return;
            }

            uint coordsAddress = GetPlayerCoordsAddress();
            api.WriteMemory(pid, coordsAddress, 12, position);
        }

        public override void LoadGame()
        {
            TriggerGameLoad((uint)Sly2Addresses.LoadTypes.Fast);
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            int analogLSubID = api.SubMemory(pid, sly2.addr.analogOffsetLeft, 8, (value) =>
            {
                // Pressing D-pad also moves the stick in memory
                if ((Inputs.RawInputs & 0xF000) != 0)
                {
                    Inputs.ly = 0;
                    Inputs.lx = 0;
                    return;
                }

                Inputs.ly = -1 * BitConverter.ToSingle(value, 0);
                Inputs.lx = BitConverter.ToSingle(value, 4);
            });

            int analogRSubID = api.SubMemory(pid, sly2.addr.analogOffsetRight, 8, (value) =>
            {
                Inputs.ry = -1 * BitConverter.ToSingle(value, 0);
                Inputs.rx = BitConverter.ToSingle(value, 4);
            });
        }

        public void SetCoinCount(int coins)
        {
            api.WriteMemory(pid, sly2.addr.coinCount, ConvertIntToBytes(coins));
        }

        public void SetHealth(int health)
        {
            var currentCharacterId = api.ReadMemory(pid, sly2.addr.currentCharacter, 4);
            // The hard-coded fallbacks keep the old behaviour on versions without known addresses.
            uint currentCharacterHealth = sly2.addr.HasCharacterStats ? sly2.addr.healthSly : 0x7A8360;
            if (currentCharacterId[3] == 8)
            {
                currentCharacterHealth = sly2.addr.HasCharacterStats ? sly2.addr.healthBentley : 0x7A8378;
            }
            else if (currentCharacterId[3] == 9)
            {
                currentCharacterHealth = sly2.addr.HasCharacterStats ? sly2.addr.healthMurray : 0x7A8390;
            }
            byte[] healthBytes = ConvertIntToBytes(health);
            api.WriteMemory(pid, currentCharacterHealth, 4, healthBytes);
        }

        // Values the toggles hold, from SluMAN-EXT: full health per character and full gadget power.
        private const uint FullHealthSlyBentley = 40;
        private const uint FullHealthMurray = 60;
        private const uint FullGadgetPower = 100;

        private int invulnerabilityFreezeSubID = -1;
        private readonly List<int> infiniteHealthSubIDs = new List<int>();
        private readonly List<int> infiniteGadgetPowerSubIDs = new List<int>();
        private int gameClockFreezeSubID = -1;

        private void ReleaseFreeze(ref int subID)
        {
            if (subID != -1)
            {
                try { api.ReleaseSubID(subID); } catch { }
                subID = -1;
            }
        }

        private void ReleaseFreezes(List<int> subIDs)
        {
            foreach (int subID in subIDs)
            {
                try { api.ReleaseSubID(subID); } catch { }
            }
            subIDs.Clear();
        }

        private uint GetEntityAddress()
        {
            byte[] ptrBytes = api.ReadMemory(pid, sly2.addr.activeCharacterPtr, 4);
            return BitConverter.ToUInt32(ptrBytes.Reverse().ToArray(), 0);
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

        /// <summary>
        /// Freezes the active character's invulnerability flag. The flag is on the character
        /// entity, so it has to be applied again after a load or a character switch.
        /// </summary>
        public void SetInvulnerability(bool enabled)
        {
            ReleaseFreeze(ref invulnerabilityFreezeSubID);

            uint entity = GetEntityAddress();
            if (entity == 0)
            {
                return;
            }

            if (enabled)
            {
                invulnerabilityFreezeSubID = api.FreezeMemory(pid, entity + 0x27C, 1u);
            }
            else
            {
                api.WriteMemory(pid, entity + 0x27C, ConvertIntToBytes(0));
            }
        }

        /// <summary>
        /// Holds every character at full health, so switching characters keeps it on.
        /// </summary>
        public void SetInfiniteHealth(bool enabled)
        {
            ReleaseFreezes(infiniteHealthSubIDs);
            if (!enabled || !sly2.addr.HasCharacterStats)
            {
                return;
            }
            infiniteHealthSubIDs.Add(api.FreezeMemory(pid, sly2.addr.healthSly, FullHealthSlyBentley));
            infiniteHealthSubIDs.Add(api.FreezeMemory(pid, sly2.addr.healthBentley, FullHealthSlyBentley));
            infiniteHealthSubIDs.Add(api.FreezeMemory(pid, sly2.addr.healthMurray, FullHealthMurray));
        }

        /// <summary>
        /// Holds every character's gadget power full.
        /// </summary>
        public void SetInfiniteGadgetPower(bool enabled)
        {
            ReleaseFreezes(infiniteGadgetPowerSubIDs);
            if (!enabled || !sly2.addr.HasCharacterStats)
            {
                return;
            }
            infiniteGadgetPowerSubIDs.Add(api.FreezeMemory(pid, sly2.addr.juiceSly, FullGadgetPower));
            infiniteGadgetPowerSubIDs.Add(api.FreezeMemory(pid, sly2.addr.juiceBentley, FullGadgetPower));
            infiniteGadgetPowerSubIDs.Add(api.FreezeMemory(pid, sly2.addr.juiceMurray, FullGadgetPower));
        }

        /// <summary>
        /// Freezes the game speed at 0, the same way Sly 3's Freeze Game Clock does.
        /// </summary>
        public void SetGameClockFrozen(bool frozen)
        {
            ReleaseFreeze(ref gameClockFreezeSubID);

            if (frozen)
            {
                byte[] zero = ConvertFloatToBytes(0.0f);
                api.WriteMemory(pid, sly2.addr.gameSpeed, zero);
                gameClockFreezeSubID = api.FreezeMemory(pid, sly2.addr.gameSpeed, 4, IPS3API.MemoryCondition.Any, zero);
            }
            else
            {
                api.WriteMemory(pid, sly2.addr.gameSpeed, ConvertFloatToBytes(1.0f));
            }
        }

        public override void SetupLoadWatcher()
        {
            WatchLoads(sly2.addr.loadingState);
        }

        private int infiniteJumpFreezeSubID = -1;

        /// <summary>
        /// Holds the active character's jump counter at 0. It's on the character entity, so it
        /// has to be applied again after a load.
        /// </summary>
        public void SetInfiniteJump(bool enabled)
        {
            ReleaseFreeze(ref infiniteJumpFreezeSubID);
            if (!enabled)
            {
                return;
            }

            uint entity = GetEntityAddress();
            if (entity == 0)
            {
                return;
            }
            infiniteJumpFreezeSubID = api.FreezeMemory(pid, entity + 0x2C8, 0u);
        }

        public PositionEditorLayout GetPositionEditorLayout()
        {
            // Velocity and the Character Info fields aren't known for Sly 2 yet, so the editor
            // shows N/A for them. Health lives per character in the save data, not on the entity.
            return new PositionEditorLayout
            {
                gameName = "Sly 2",
                warpFilePrefix = "sly2",
                activeCharacterPtr = sly2.addr.activeCharacterPtr,
                mapNameAddress = sly2.addr.mapAOB,
                transformOffset = sly2.addr.transformOffset,
                positionOffset = sly2.addr.coordsOffsetX,
            };
        }

        /// <summary>
        /// The readable name of a map such as "Y$KFf_nightclub_exterior", or null when it isn't in
        /// the list.
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

        public void SetGadgetUnlocks(byte[] gadgetBytes)
        {
            api.WriteMemory(pid, sly2.addr.gadgetUnlocks, gadgetBytes);
        }

        public byte[] GetGadgetUnlocks()
        {
            return api.ReadMemory(pid, sly2.addr.gadgetUnlocks, 8);
        }

        // Read gadget binding for a character (returns button binding index, -1 if unbound)
        public int GetGadgetBinding(uint baseAddress, int buttonOffset)
        {
            uint address = baseAddress + (uint)(buttonOffset * 4);
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }

        public void SetGadgetBindings(byte[] bindingBytes)
        {
            // bindingBytes contains: 12 bytes Sly + 12 bytes Bentley + 12 bytes Murray
            byte[] slyBindings = new byte[12];
            byte[] bentleyBindings = new byte[12];
            byte[] murrayBindings = new byte[12];

            Array.Copy(bindingBytes, 0, slyBindings, 0, 12);
            Array.Copy(bindingBytes, 12, bentleyBindings, 0, 12);
            Array.Copy(bindingBytes, 24, murrayBindings, 0, 12);

            api.WriteMemory(pid, sly2.addr.gadgetBindsSly, slyBindings);
            api.WriteMemory(pid, sly2.addr.gadgetBindsBentley, bentleyBindings);
            api.WriteMemory(pid, sly2.addr.gadgetBindsMurray, murrayBindings);
        }

        public void SetMapName(string mapName)
        {
            if (!string.IsNullOrEmpty(mapName))
            {
                // Clear the map AOB region first
                byte[] clearBytes = new byte[64];
                api.WriteMemory(pid, sly2.addr.mapAOB, clearBytes);

                // Write the new map name
                byte[] mapBytes = System.Text.Encoding.ASCII.GetBytes(mapName);
                api.WriteMemory(pid, sly2.addr.mapAOB, mapBytes);
            }
        }

        public void SetSpawnLocation(int location)
        {
            api.WriteMemory(pid, sly2.addr.spawnLocation, ConvertIntToBytes(location));
        }

        public void SetJobState(int jobId, int cpId)
        {
            SetCurrentJob(jobId, cpId);
        }

        private void SetCurrentJob(int jobId, int checkpointId)
        {
            api.WriteMemory(pid, sly2.addr.currentJobId, ConvertIntToBytes(jobId));
            api.WriteMemory(pid, sly2.addr.currentCheckpointId, ConvertIntToBytes(checkpointId));
            api.WriteMemory(pid, sly2.addr.currentCheckpointId + 0x4, ConvertIntToBytes(checkpointId));
        }

        public void SetActiveCharacter(int characterId)
        {
            api.WriteMemory(pid, sly2.addr.currentCharacter, ConvertIntToBytes(characterId));
        }

        public void TriggerGameLoad(uint loadType = (uint)Sly2Addresses.LoadTypes.Normal)
        {
            api.WriteMemory(pid, sly2.addr.loadType, loadType);
            api.WriteMemory(pid, sly2.addr.loadTrigger, (uint)1);
        }

        public (uint startAddress, uint size) GetMemoryRegionForEpisode(string episode)
        {
            // Same addresses as the *_MemoryAddress keys in data/s2_run_file_config.txt
            switch (episode)
            {
                case "Episode1": return (0x7A96D8, 4096);
                case "Episode2": return (0x7A9E8C, 4096);
                case "Episode3": return (0x7AA5CC, 4096);
                case "Episode4": return (0x7AAC34, 4096);
                case "Episode5": return (0x7AB4CC, 4096);
                case "Episode6": return (0x7ABCC4, 4096);
                case "Episode7": return (0x7AC264, 4096);
                case "Episode8": return (0x7ACBEC, 4096);
                default: return (0x7A96D8, 4096); // Default to Episode 1
            }
        }

        public void SkipCinematic()
        {
            // Skip FMV
            if (api.ReadMemory(pid, sly2.addr.fmvState + 0x3, 1)[0] == 0)
            {
                api.WriteMemory(pid, sly2.addr.fmvState, (uint)2);
            }

            // Skip dialogue
            if (api.ReadMemory(pid, sly2.addr.dialogueFrameCounter + 0x3, 1)[0] > 10)
            {
                api.WriteMemory(pid, sly2.addr.dialogueState, (uint)0);
            }
        }

        public void LoadJob(string jobName)
        {
            switch (jobName)
            {
                case "Cairo Museum Break-in": LoadJobHelper(7, 1696, 1584, 148457, "jb_intro", "EP0_ADDR", "EP0_STAR"); break;
                case "Satellite Sabotage": LoadJobHelper(7, 1784, 1785, 122409, "f_nightclub_exterior", "EP1_ADDR", "EP1_SATELLITE_START"); break;
                case "Breaking and Entering": LoadJobHelper(7, 1795, 1797, 397, "f_nightclub_cellar", "EP1_ADDR", "EP1_BREAKING_START"); break;
                case " [BnE] Sneak": LoadJobHelper(7, 1795, 1804, 122447, "f_nightclub_cellar", "EP1_ADDR", "EP1_BREAKING_SNEAK"); break;
                case " [BnE] Guards": LoadJobHelper(7, 1795, 1808, 122448, "f_nightclub_cellar", "EP1_ADDR", "EP1_BREAKING_GUARDS"); break;
                case " [BnE] Photos": LoadJobHelper(7, 1795, 1813, 402, "f_nightclub_heist", "EP1_ADDR", "EP1_BREAKING_PHOTOS"); break;
                case "Bug Dimitri's Office": LoadJobHelper(7, 1833, 1834, 122459, "f_nightclub_exterior", "EP1_ADDR", "EP1_BUG_START"); break;
                case " [BDO] Nightclub": LoadJobHelper(7, 1833, 1834, 398, "f_nightclub_disco", "EP1_ADDR", "EP1_BUG_DISCO"); break;
                case "Follow Dimitri": LoadJobHelper(7, 1841, 1585, 122473, "f_nightclub_exterior", "EP1_ADDR", "EP1_FOLLOW_START"); break;
                case " [FD] Follow": LoadJobHelper(7, 1841, 1842, 122475, "f_nightclub_exterior", "EP1_ADDR", "EP1_FOLLOW_FOLLOW"); break;
                case "Waterpump Destruction": LoadJobHelper(9, 1900, -1, 399, "f_nightclub_waterpump", "EP1_ADDR", "EP1_WATERPUMP_START"); break;
                case "Silence the Alarms": LoadJobHelper(9, 1826, 1586, 122451, "f_nightclub_exterior", "EP1_ADDR", "EP1_SILENCE_START"); break;
                case "Moonlight Rendezvous": LoadJobHelper(7, 1874, 1875, 122519, "f_nightclub_exterior", "EP1_ADDR", "EP1_MOONLIGHT_START"); break;
                case "Theater Pickpocketing": LoadJobHelper(7, 1859, -1, 400, "f_nightclub_moulinrouge", "EP1_ADDR", "EP1_THEATER_START"); break;
                case "Disco Demolitions": LoadJobHelper(8, 1882, -1, 401, "f_nightclub_disco", "EP1_ADDR", "EP1_DISCO_START"); break;
                case "OP: Thunder Beak": LoadJobHelper(8, 1912, 1913, 27280, "f_nightclub_exterior", "EP1_ADDR", "EP1_OP_START"); break;
                case " [OTB] Steal the key": LoadJobHelper(7, 1912, 1923, 27280, "f_nightclub_exterior", "EP1_ADDR", "EP1_OP_KEY"); break;
                case " [OTB] Climb the peacock": LoadJobHelper(7, 1912, 1929, 27280, "f_nightclub_exterior", "EP1_ADDR", "EP1_OP_CLIMB"); break;
                case " [OTB] Shoot the hook": LoadJobHelper(7, 1912, 1932, 122600, "f_nightclub_exterior", "EP1_ADDR", "EP1_OP_HOOK"); break;
                case " [OTB] Printing room": LoadJobHelper(7, 1938, 1943, 121308, "f_nightclub_heist", "EP1_ADDR", "EP1_OP_DIMITRI"); break;
                case " [OTB] Dimitri fight": LoadJobHelper(7, 1938, 1943, 121308, "f_nightclub_heist", "EP1_ADDR", "EP1_OP_FIGHT"); break;
                case "Recon the Ballroom": LoadJobHelper(7, 2003, 2006, 49223, "i_palace_ext", "EP2_ADDR", "EP2_RECON_START"); break;
                case " [RtB] Photos": LoadJobHelper(7, 2003, -1, 104738, "i_palace_ballroom", "EP2_ADDR", "EP2_RECON_PHOTO"); break;
                case "Lower the Drawbridge": LoadJobHelper(7, 2022, 2024, 105705, "i_palace_ext", "EP2_ADDR", "EP2_LOWER_START"); break;
                case " [LtD] Lever": LoadJobHelper(7, 2022, 2031, 105705, "i_palace_ext", "EP2_ADDR", "EP2_LOWER_LEVER"); break;
                case "Steal a Tuxedo": LoadJobHelper(7, 2059, 2060, 105779, "i_palace_int", "EP2_ADDR", "EP2_TUX_START"); break;
                case "Battle the Chopper": LoadJobHelper(9, 2034, 2035, 105722, "i_palace_ext", "EP2_ADDR", "EP2_CHOPPER_START"); break;
                case " [BtC] Fight": LoadJobHelper(9, 2034, 2038, 105722, "i_palace_ext", "EP2_ADDR", "EP2_CHOPPER_FIGHT"); break;
                case "Dominate the Dance Floor": LoadJobHelper(7, 2077, 2080, 105063, "i_palace_ballroom", "EP2_ADDR", "EP2_DANCE_START"); break;
                case "RC Bombing Run": LoadJobHelper(8, 2085, 2087, 49304, "i_palace_ext", "EP2_ADDR", "EP2_RC_START"); break;
                case " [RBR] Destroy the Jeep": LoadJobHelper(8, 2085, 2089, 49304, "i_palace_ext", "EP2_ADDR", "EP2_RC_JEEP"); break;
                case "Elephant Rampage": LoadJobHelper(7, 2092, 2094, 105619, "i_palace_ext", "EP2_ADDR", "EP2_ELEPHANT_START"); break;
                case " [ER] Elephants": LoadJobHelper(7, 2092, 2097, 105619, "i_palace_ext", "EP2_ADDR", "EP2_ELEPHANT_RUBIES"); break;
                case "Boardroom Brawl": LoadJobHelper(7, 2042, 1589, 105734, "i_palace_ext", "EP2_ADDR", "EP2_BOARDROOM_START"); break;
                case " [BB] Find the code": LoadJobHelper(7, -1, -1, 105734, "i_palace_basement", "EP2_ADDR", "EP2_BOARDROOM_CODE"); break;
                case " [BB] Protect Bentley": LoadJobHelper(7, 2042, 1592, 105764, "i_palace_basement", "EP2_ADDR", "EP2_BOARDROOM_PROTECT"); break;
                case "OP: Hippo Drop": LoadJobHelper(8, 2111, 1593, 76733, "i_palace_ext", "EP2_ADDR", "EP2_OP_START"); break;
                case " [OHD] First floor": LoadJobHelper(8, 2111, 1593, 76733, "i_palace_ext", "EP2_ADDR", "EP2_OP_BOMB1"); break;
                case " [OHD] Second floor": LoadJobHelper(8, 2111, 2126, 105827, "i_palace_ext", "EP2_ADDR", "EP2_OP_BOMB2"); break;
                case " [OHD] Dance": LoadJobHelper(8, 2138, 2139, 105063, "i_palace_ballroom", "EP2_ADDR", "EP2_OP_DANCE"); break;
                case " [OHD] Protect Murray": LoadJobHelper(8, 2141, 2143, 105685, "i_palace_ext", "EP2_ADDR", "EP2_OP_PROTECT"); break;
                case "Spice Room Recon": LoadJobHelper(7, 2192, 2193, 403, "i_temple_ext", "EP3_ADDR", "EP3_RECON_START"); break;
                case " [SRR] Interior": LoadJobHelper(7, 2192, 2194, 398, "i_temple_int", "EP3_ADDR", "EP3_RECON_INTERIOR"); break;
                case "Freeing the Elephant": LoadJobHelper(7, 2214, -1, 96699, "i_temple_ext", "EP3_ADDR", "EP3_ELEPHANT_START"); break;
                case "Water Bug Run": LoadJobHelper(7, 2209, 2210, 96698, "i_temple_ext", "EP3_ADDR", "EP3_WATER_START"); break;
                case "Leading Rajan": LoadJobHelper(8, 2254, 2261, 96737, "i_temple_ext", "EP3_ADDR", "EP3_LEADING_START"); break;
                case " [LR] Blueprints": LoadJobHelper(8, 2254, 2261, 96737, "i_temple_ext", "EP3_ADDR", "EP3_LEADING_BLUEPRINTS"); break;
                case "Blow the Dam": LoadJobHelper(8, 2302, 2305, 17505, "i_temple_ext", "EP3_ADDR", "EP3_SPICE_START"); break;
                case "Spice Grinder Destruction": LoadJobHelper(7, 2286, 2287, 400, "i_temple_int", "EP3_ADDR", "EP3_SPICE_START"); break;
                case "Neyla's Secret": LoadJobHelper(7, 2235, 2236, 401, "i_temple_ext", "EP3_ADDR", "EP3_NEYLA_START"); break;
                case " [NS] Keys": LoadJobHelper(7, 2235, 2242, 399, "i_temple_int", "EP3_ADDR", "EP3_NEYLA_KEYS"); break;
                case "Rip-Off the Ruby": LoadJobHelper(7, 2308, 2309, 96658, "i_temple_ext", "EP3_ADDR", "EP3_RUBY_START"); break;
                case " [ROtR] Reach the ruby": LoadJobHelper(9, 2308, 2310, 96660, "i_temple_ext", "EP3_ADDR", "EP3_RUBY_MURRAY"); break;
                case " [ROtR] Carry 1": LoadJobHelper(9, 2308, 2311, 96779, "i_temple_ext", "EP3_ADDR", "EP3_RUBY_PART1"); break;
                case " [ROtR] Carry 2": LoadJobHelper(9, 2308, 2313, 96665, "i_temple_ext", "EP3_ADDR", "EP3_RUBY_PART2"); break;
                case "OP: Wet Tiger": LoadJobHelper(9, 2318, 2320, 33676, "i_temple_ext", "EP3_ADDR", "EP3_OP_START"); break;
                case " [OWT] Protect Murray": LoadJobHelper(8, 2318, 2330, 33676, "i_temple_ext", "EP3_ADDR", "EP3_OP_PROTECT"); break;
                case " [OWT] Deliver TNT": LoadJobHelper(7, 2318, 2341, 33676, "i_temple_ext", "EP3_ADDR", "EP3_OP_TNT"); break;
                case " [OWT] Follow Neyla": LoadJobHelper(7, 2346, 2349, 96822, "i_temple_ext", "EP3_ADDR", "EP3_OP_NEYLA"); break;
                case " [OWT] Rajan fight": LoadJobHelper(9, 2346, 2354, 96835, "i_temple_ext", "EP3_ADDR", "EP3_OP_RAJAN"); break;
                case "Eavesdrop on Contessa": LoadJobHelper(8, 2411, 2420, 17366, "p_prison_ext", "EP4_ADDR", "EP4_EAVESDROP_START"); break;
                case "Train Hack": LoadJobHelper(8, 2427, 2433, 76660, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_START"); break;
                case " [TH] Hack 1": LoadJobHelper(8, 2427, 2435, 59889, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK1"); break;
                case " [TH] Go to hack 2": LoadJobHelper(8, 2427, 2436, 59889, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_GO2"); break;
                case " [TH] Hack 2": LoadJobHelper(8, 2427, 2437, 59896, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK2"); break;
                case " [TH] Go to hack 3": LoadJobHelper(8, 2427, 2438, 59896, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_GO3"); break;
                case " [TH] Hack 3": LoadJobHelper(8, 2427, 2439, 76668, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK3"); break;
                case " [TH] Go to hack 4": LoadJobHelper(8, 2427, 2440, 76668, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_GO4"); break;
                case " [TH] Hack 4": LoadJobHelper(8, 2427, 2441, 76672, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK4"); break;
                case " [TH] Go to hack 5": LoadJobHelper(8, 2427, 2442, 76672, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_GO5"); break;
                case " [TH] Hack 5": LoadJobHelper(8, 2427, 2443, 76676, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK5"); break;
                case " [TH] Go to hack 6": LoadJobHelper(8, 2427, 2444, 76676, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_GO6"); break;
                case " [TH] Hack 6": LoadJobHelper(8, 2427, 1636, 76676, "p_prison_ext", "EP4_ADDR", "EP4_TRAIN_HACK6"); break;
                case "Wall Bombing": LoadJobHelper(8, 2447, 2450, 76682, "p_prison_ext", "EP4_ADDR", "EP4_WALL_START"); break;
                case " [WP] RC Chopper": LoadJobHelper(8, 2447, 2450, 76682, "p_prison_ext", "EP4_ADDR", "EP4_WALL_RC"); break;
                case " [WP] Return to hideout": LoadJobHelper(7, 2447, 2460, 76691, "p_prison_ext", "EP4_ADDR", "EP4_WALL_SLY"); break;
                case "Code Capture": LoadJobHelper(7, 2505, 2507, 76753, "p_prison_ext", "EP4_ADDR", "EP4_CODE_START"); break;
                case " [CC] Picture 2": LoadJobHelper(7, 2505, 2512, 76753, "p_prison_ext", "EP4_ADDR", "EP4_CODE_PIC2"); break;
                case " [CC] Picture 3": LoadJobHelper(7, 2505, 2527, 76775, "p_prison_ext", "EP4_ADDR", "EP4_CODE_PIC3"); break;
                case " [CC] Picture 4": LoadJobHelper(7, 2505, 2517, 76764, "p_prison_ext", "EP4_ADDR", "EP4_CODE_PICK4"); break;
                case "Lightning Action": LoadJobHelper(7, 2473, 2475, 76716, "p_prison_ext", "EP4_ADDR", "EP4_LIGHTNING_START"); break;
                case " [LA] Rod 1": LoadJobHelper(7, 2473, 2478, 76720, "p_prison_ext", "EP4_ADDR", "EP4_LIGHTNING_ROD1"); break;
                case " [LA] Rod 2": LoadJobHelper(7, 2473, 2490, 76728, "p_prison_ext", "EP4_ADDR", "EP4_LIGHTNING_ROD2"); break;
                case " [LA] Rod 3": LoadJobHelper(7, 2473, 2487, 76726, "p_prison_ext", "EP4_ADDR", "EP4_LIGHTNING_ROD3"); break;
                case " [LA] Rod 4": LoadJobHelper(7, 2473, 2484, 76724, "p_prison_ext", "EP4_ADDR", "EP4_LIGHTNING_ROD4"); break;
                case "Close to Contessa": LoadJobHelper(7, 2530, 2537, 76776, "p_prison_ext", "EP4_ADDR", "EP4_CONTESSA_START"); break;
                case " [CtC] Key 2": LoadJobHelper(7, 2530, 2546, 76783, "p_prison_ext", "EP4_ADDR", "EP4_CONTESSA_KEY2"); break;
                case " [CtC] Tank schedule": LoadJobHelper(7, 2530, 2548, 76784, "p_prison_ext", "EP4_ADDR", "EP4_CONTESSA_SCHEDULE"); break;
                case "Big House Brawl": LoadJobHelper(7, 2464, 2465, 76697, "p_prison_ext", "EP4_ADDR", "EP4_BRAWL_START"); break;
                case " [BHB] Prison fight": LoadJobHelper(9, 2464, 2466, 76698, "p_prison_int", "EP4_ADDR", "EP4_BRAWL_FIGHT"); break;
                case "Disguise Bridge": LoadJobHelper(7, 2494, 2495, 76733, "p_prison_ext", "EP4_ADDR", "EP4_BRIDGE_START"); break;
                case " [DB] Protect Bentley": LoadJobHelper(7, 2494, 2500, 76744, "p_prison_ext", "EP4_ADDR", "EP4_BRIDGE_PROTECT"); break;
                case "OP: Trojan Tank": LoadJobHelper(8, -1, -1, 76786, "p_prison_ext", "EP4_ADDR", "EP4_OP_START"); break;
                case " [OTT] Crawl": LoadJobHelper(7, 2553, 2559, 76794, "p_prison_ext", "EP4_ADDR", "EP4_OP_CRAWL"); break;
                case " [OTT] Enter prison": LoadJobHelper(7, 2553, 2562, 76622, "p_prison_int", "EP4_ADDR", "EP4_OP_ENTER"); break;
                case " [OTT] Reach the control panel": LoadJobHelper(7, 2553, -1, 76806, "p_prison_int", "EP4_ADDR", "EP4_OP_SKIP"); break;
                case " [OTT] Return to Bentley": LoadJobHelper(7, 2553, 2570, 76818, "p_prison_int", "EP4_ADDR", "EP4_OP_RETURN"); break;
                case " [OTT] Activate hypno-boxes": LoadJobHelper(7, 2553, 2572, 76820, "p_prison_int", "EP4_ADDR", "EP4_OP_HACKS"); break;
                case " [OTT] Destroy hypno-boxes": LoadJobHelper(7, 2553, 2596, 76844, "p_prison_int", "EP4_ADDR", "EP4_OP_DESTROY"); break;
                case " [OTT] Lift the gate": LoadJobHelper(9, 2553, 2609, 76853, "p_prison_int", "EP4_ADDR", "EP4_OP_LIFT"); break;
                case " [OTT] Chase Contessa": LoadJobHelper(9, 2553, 2616, 76859, "p_prison_ext", "EP4_ADDR", "EP4_OP_CHASE"); break;
                case "Know Your Enemy": LoadJobHelper(7, -1, -1, 49223, "p_castle_ext", "EP5_ADDR", "EP5_ENEMY_START"); break;
                case " [KYE] Photos": LoadJobHelper(7, 2645, 2646, 49223, "p_castle_ext", "EP5_ADDR", "EP5_ENEMY_START_NOCS"); break;
                case " [KYE] Education tower": LoadJobHelper(7, 2645, -1, 33438, "p_castle_heist", "EP5_ADDR", "EP5_ENEMY_PHOTOS"); break;
                case "Kidnap the General": LoadJobHelper(9, -1, -1, 59820, "p_castle_ext", "EP5_ADDR", "EP5_KIDNAP_START"); break;
                case " [KtG] General": LoadJobHelper(9, 2701, 2702, 59820, "p_castle_ext", "EP5_ADDR", "EP5_KIDNAP_START_NOCS"); break;
                case "Ghost Capture": LoadJobHelper(7, -1, -1, 59748, "p_castle_ext", "EP5_ADDR", "EP5_GHOST_START"); break;
                case " [GC] Enter the tomb": LoadJobHelper(7, 2662, 2663, 59748, "p_castle_ext", "EP5_ADDR", "EP5_GHOST_START_NOCS"); break;
                case " [GC] Tomb": LoadJobHelper(7, 2662, 2664, 105734, "p_castle_wolftomb", "EP5_ADDR", "EP5_GHOST_TOMB"); break;
                case " [GC] Ghosts": LoadJobHelper(7, 2662, 2668, 59782, "p_castle_ext", "EP5_ADDR", "EP5_GHOST_GHOSTS"); break;
                case " [GC] Ghosts (No CS)": LoadJobHelper(7, 2662, 2668, 59782, "p_castle_ext", "EP5_ADDR", "EP5_GHOST_GHOSTS_NOCS"); break;
                case "Mojo Trap Action": LoadJobHelper(8, 2683, 2689, 393, "p_castle_ewoktrainer", "EP5_ADDR", "EP5_MOJO_START"); break;
                case " [MTA] Crypt 1": LoadJobHelper(8, 2683, 2689, 393, "p_castle_ewoktrainer", "EP5_ADDR", "EP5_MOJO_C1"); break;
                case " [MTA] Go to crypt 2": LoadJobHelper(8, 2683, 2691, 59810, "p_castle_ext", "EP5_ADDR", "EP5_MOJO_GOC2"); break;
                case " [MTA] Crypt 2": LoadJobHelper(8, 2683, 2692, 33438, "p_castle_ewokhall", "EP5_ADDR", "EP5_MOJO_C2"); break;
                case " [MTA] Go to crypt 3": LoadJobHelper(8, 2683, 2694, 59814, "p_castle_ext", "EP5_ADDR", "EP5_MOJO_GOC3"); break;
                case " [MTA] Crypt 3": LoadJobHelper(8, 2683, 2695, 33438, "p_castle_ewoktomb", "EP5_ADDR", "EP5_MOJO_C3"); break;
                case " [MTA] Go to crypt 4": LoadJobHelper(8, 2683, 2697, 59818, "p_castle_ext", "EP5_ADDR", "EP5_MOJO_GOC4"); break;
                case " [MTA] Crypt 4": LoadJobHelper(8, 2683, 2698, 33438, "p_castle_ewokwater", "EP5_ADDR", "EP5_MOJO_C4"); break;
                case "Tank Showdown": LoadJobHelper(9, 2734, 2735, 59868, "p_castle_ext", "EP5_ADDR", "EP5_TANK_START"); break;
                case " [TS] Tanks": LoadJobHelper(9, 2734, 2735, 59868, "p_castle_ext", "EP5_ADDR", "EP5_TANK_TANK"); break;
                case "Stealing Voices": LoadJobHelper(7, -1, -1, 59827, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_START"); break;
                case " [SV] Keys 1": LoadJobHelper(7, 2711, 2712, 59827, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_KEYS1"); break;
                case " [SV] Go to crypt 1": LoadJobHelper(7, 2711, 2716, 59827, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_GOC1"); break;
                case " [SV] Crypt 1": LoadJobHelper(7, 2711, 2717, 59836, "p_castle_guardbreak", "EP5_ADDR", "EP5_VOICES_C1"); break;
                case " [SV] Keys 2": LoadJobHelper(7, 2711, 2719, 59838, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_KEYS2"); break;
                case " [SV] Go to crypt 2": LoadJobHelper(7, 2711, 2723, 59838, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_GOC2"); break;
                case " [SV] Crypt 2": LoadJobHelper(7, 2711, 2724, 59847, "p_castle_guardbreak", "EP5_ADDR", "EP5_VOICES_C2"); break;
                case " [SV] Keys 3": LoadJobHelper(7, 2711, 2726, 59854, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_KEYS3"); break;
                case " [SV] Go to crypt 3": LoadJobHelper(7, 2711, 2730, 59854, "p_castle_ext", "EP5_ADDR", "EP5_VOICES_GOC3"); break;
                case " [SV] Crypt 3": LoadJobHelper(7, 2711, 2731, 33438, "p_castle_waterrails", "EP5_ADDR", "EP5_VOICES_C3"); break;
                case "Crypt Hack": LoadJobHelper(8, 2746, 2748, 59882, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_START"); break;
                case " [CH] Find the computer": LoadJobHelper(8, 2746, 2748, 59882, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_FIND"); break;
                case " [CH] Hack 1": LoadJobHelper(8, 2746, 2751, 59889, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_H1"); break;
                case " [CH] Go to hack 2": LoadJobHelper(8, 2746, 2753, 59889, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_GOH2"); break;
                case " [CH] Hack 2": LoadJobHelper(8, 2746, 2755, 59896, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_H2"); break;
                case " [CH] Go to hack 3": LoadJobHelper(8, 2746, 2757, 59896, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_GOH3"); break;
                case " [CH] Hack 3": LoadJobHelper(8, 2746, 2759, 59896, "p_castle_heist", "EP5_ADDR", "EP5_CRYPT_H3"); break;
                case "OP: High Road": LoadJobHelper(9, 2765, 2766, 27280, "p_castle_ext", "EP5_ADDR", "EP5_OP_START"); break;
                case " [OHR] Paraglide": LoadJobHelper(7, 2765, 2767, 59915, "p_castle_ext", "EP5_ADDR", "EP5_OP_PARAGLIDE"); break;
                case " [OHR] Enter tower": LoadJobHelper(7, 2765, 2774, 33438, "p_castle_heist", "EP5_ADDR", "EP5_OP_TOWER"); break;
                case " [OHR] Hack": LoadJobHelper(7, 2765, 1638, 33438, "p_castle_heist", "EP5_ADDR", "EP5_OP_HACK"); break;
                case " [OHR] Chase Neyla": LoadJobHelper(7, 2765, 2791, 59983, "p_castle_ext", "EP5_ADDR", "EP5_OP_NEYLA"); break;
                case " [OHR] Turret": LoadJobHelper(7, 2765, 2798, 60005, "p_castle_ext", "EP5_ADDR", "EP5_OP_TURRET"); break;
                case " [OHR] Contessa 1": LoadJobHelper(7, 2765, 2808, 49343, "p_castle_ext", "EP5_ADDR", "EP5_OP_C1"); break;
                case " [OHR] Tank": LoadJobHelper(9, 2765, 2825, 59868, "p_castle_ext", "EP5_ADDR", "EP5_OP_TANK"); break;
                case " [OHR] Contessa 2": LoadJobHelper(7, 2765, 2832, 49343, "p_castle_ext", "EP5_ADDR", "EP5_OP_C2"); break;
                case "Cabin Crimes": LoadJobHelper(7, -1, -1, 49223, "c_train_ext", "EP6_ADDR", "EP6_CABIN_START"); break;
                case " [CC] Enter cabin 1": LoadJobHelper(7, 2860, 2861, 49223, "c_train_ext", "EP6_ADDR", "EP6_CABIN_GOC1"); break;
                case " [CC] Cabin 1": LoadJobHelper(7, 2860, 2862, 49224, "c_train_cabins", "EP6_ADDR", "EP6_CABIN_C1"); break;
                case " [CC] Enter cabin 2": LoadJobHelper(7, 2860, 2869, 49231, "c_train_ext", "EP6_ADDR", "EP6_CABIN_GOC2"); break;
                case " [CC] Cabin 2": LoadJobHelper(7, 2860, 2870, 49237, "c_train_cabins", "EP6_ADDR", "EP6_CABIN_C2"); break;
                case " [CC] Enter cabin 3": LoadJobHelper(7, 2860, 2870, 49238, "c_train_ext", "EP6_ADDR", "EP6_CABIN_GOC3"); break;
                case " [CC] Cabin 3": LoadJobHelper(7, 2860, 2872, 49244, "c_train_cabins", "EP6_ADDR", "EP6_CABIN_C3"); break;
                case " [CC] Climb the mountain": LoadJobHelper(7, 2860, 2875, 49245, "c_train_ext", "EP6_ADDR", "EP6_CABIN_CLIMB"); break;
                case "Spice in the Sky": LoadJobHelper(7, -1, -1, 49254, "c_train_ext", "EP6_ADDR", "EP6_SPICE_START"); break;
                case " [SitS] Train 1": LoadJobHelper(7, -1, -1, 49254, "c_train_ext", "EP6_ADDR", "EP6_SPICE_T1"); break;
                case " [SitS] Train 2": LoadJobHelper(7, 2883, 2889, 49254, "c_train_ext", "EP6_ADDR", "EP6_SPICE_T2"); break;
                case " [SitS] Train 3": LoadJobHelper(7, 2883, 2890, 49254, "c_train_ext", "EP6_ADDR", "EP6_SPICE_T3"); break;
                case "A Friend in Need": LoadJobHelper(7, -1, -1, 49280, "c_train_ext", "EP6_ADDR", "EP6_FRIEND_START"); break;
                case " [AFiN] Follow Carmelita": LoadJobHelper(7, 2909, 2912, 49280, "c_train_ext", "EP6_ADDR", "EP6_FRIEND_FOLLOW"); break;
                case " [AFiN] Key 1": LoadJobHelper(7, 2909, 2918, 49285, "c_train_ext", "EP6_ADDR", "EP6_FRIEND_K1"); break;
                case " [AFiN] Key 2": LoadJobHelper(7, 2909, 2923, 49285, "c_train_ext", "EP6_ADDR", "EP6_FRIEND_K2"); break;
                case " [AFiN] Key 3": LoadJobHelper(7, 2909, 2926, 49285, "c_train_ext", "EP6_ADDR", "EP6_FRIEND_K3"); break;
                case "Ride the Iron Horse": LoadJobHelper(8, -1, -1, 49276, "c_train_ext", "EP6_ADDR", "EP6_RIDE_START"); break;
                case " [RtIH] Enter the train": LoadJobHelper(8, 2904, 2905, 49276, "c_train_ext", "EP6_ADDR", "EP6_RIDE_ENTER"); break;
                case " [RtIH] Train": LoadJobHelper(8, 2904, -1, 393, "c_train_c", "EP6_ADDR", "EP6_RIDE_TRAIN"); break;
                case "Aerial Assault": LoadJobHelper(8, 2933, 2934, 49304, "c_train_ext", "EP6_ADDR", "EP6_AERIAL_START"); break;
                case " [AA] Enter the train": LoadJobHelper(8, 2933, 2934, 49304, "c_train_ext", "EP6_ADDR", "EP6_AERIAL_ENTER"); break;
                case " [AA] RC Chopper": LoadJobHelper(8, 2933, 2935, 49304, "c_train_a", "EP6_ADDR", "EP6_AERIAL_TRAIN"); break;
                case "Bear Cub Kidnapping": LoadJobHelper(9, -1, -1, 49324, "c_train_ext", "EP6_ADDR", "EP6_BEAR_START"); break;
                case " [BCK] Cub 1": LoadJobHelper(9, 2945, 2946, 49324, "c_train_ext", "EP6_ADDR", "EP6_BEAR_C1"); break;
                case " [BCK] Cub 2": LoadJobHelper(9, 2945, 2951, 49327, "c_train_ext", "EP6_ADDR", "EP6_BEAR_C2"); break;
                case "Theft on the Rails": LoadJobHelper(7, -1, -1, 49310, "c_train_ext", "EP6_ADDR", "EP6_THEFT_START"); break;
                case " [TotR] Enter the train": LoadJobHelper(7, 2939, 2940, 49310, "c_train_ext", "EP6_ADDR", "EP6_THEFT_ENTER"); break;
                case " [TotR] Train": LoadJobHelper(7, 2939, -1, 393, "c_train_a", "EP6_ADDR", "EP6_THEFT_TRAIN"); break;
                case "OP: Choo-Choo": LoadJobHelper(9, 2962, 2965, 27280, "c_train_ext", "EP6_ADDR", "EP6_OP_START"); break;
                case " [OCC] Catch the train": LoadJobHelper(7, 2962, 2967, 393, "c_train_b", "EP6_ADDR", "EP6_OP_CATCH"); break;
                case " [OCC] Sly section 1": LoadJobHelper(7, 2962, 2967, 393, "c_train_b", "EP6_ADDR", "EP6_OP_S1"); break;
                case " [OCC] Neyla fight 1": LoadJobHelper(7, 2962, 2971, 393, "c_train_b", "EP6_ADDR", "EP6_OP_N1"); break;
                case " [OCC] Sly section 2": LoadJobHelper(7, 2962, 2977, 49370, "c_train_b", "EP6_ADDR", "EP6_OP_S2"); break;
                case " [OCC] Neyla fight 2": LoadJobHelper(7, 2962, 2979, 393, "c_train_b", "EP6_ADDR", "EP6_OP_N2"); break;
                case "Recon the Sawmill": LoadJobHelper(7, -1, -1, 33420, "c_sawmill_ext", "EP7_ADDR", "EP7_RECON_START"); break;
                case " [RtS] Photos": LoadJobHelper(7, 3037, 3043, 33430, "c_sawmill_lighthouse", "EP7_ADDR", "EP7_RECON_PHOTOS"); break;
                case "Laser Redirection": LoadJobHelper(7, 3101, 3102, 393, "c_sawmill_burn", "EP7_ADDR", "EP7_LASER_START"); break;
                case " [LR] Crystal 1": LoadJobHelper(7, 3101, 3105, 33507, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C1"); break;
                case " [LR] Crystal 2": LoadJobHelper(7, 3101, 3110, 33510, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C2"); break;
                case " [LR] Crystal 3": LoadJobHelper(7, 3101, 3114, 33510, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C3"); break;
                case " [LR] Crystal 4": LoadJobHelper(7, 3101, 3116, 33510, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C4"); break;
                case " [LR] Crystal 5": LoadJobHelper(7, 3101, 3118, 33510, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C5"); break;
                case " [LR] Crystal 6": LoadJobHelper(7, 3101, 3120, 33510, "c_sawmill_ext", "EP7_ADDR", "EP7_LASER_C6"); break;
                case "Bearcave Bugging": LoadJobHelper(7, 3056, 3057, 33437, "c_sawmill_ext", "EP7_ADDR", "EP7_BEAR_START"); break;
                case " [BB] Cave": LoadJobHelper(7, 3056, 3057, 33438, "c_sawmill_bearcave", "EP7_ADDR", "EP7_BEAR_CAVE"); break;
                case " [BB] Transmitters": LoadJobHelper(7, 3056, 3066, 33439, "c_sawmill_ext", "EP7_ADDR", "EP7_BEAR_HUB"); break;
                case " [BB] Transmitters (No CS)": LoadJobHelper(7, 3056, 3067, 33439, "c_sawmill_ext", "EP7_ADDR", "EP7_BEAR_HUB_NOCS"); break;
                case "RC Combat Club": LoadJobHelper(9, -1, -1, 33479, "c_sawmill_ext", "EP7_ADDR", "EP7_RC_START"); break;
                case " [RCC] Enter the club": LoadJobHelper(9, 3083, 3087, 33479, "c_sawmill_ext", "EP7_ADDR", "EP7_RC_ENTER"); break;
                case " [RCC] Barrel": LoadJobHelper(9, 3083, 3088, 392, "c_sawmill_mulch", "EP7_ADDR", "EP7_RC_BARREL"); break;
                case " [RCC] Moose head": LoadJobHelper(7, 3083, 1645, 392, "c_sawmill_mulch", "EP7_ADDR", "EP7_RC_MOOSE"); break;
                case " [RCC] RC fight": LoadJobHelper(9, 3083, 3092, 33489, "c_sawmill_mulch", "EP7_ADDR", "EP7_RC_FIGHT"); break;
                case "Old Grizzle Face": LoadJobHelper(9, -1, -1, 33583, "c_sawmill_ext", "EP7_ADDR", "EP7_GRIZZLE_START"); break;
                case " [OGF] Destroy the generators": LoadJobHelper(9, 3146, 3149, 33583, "c_sawmill_ext", "EP7_ADDR", "EP7_GRIZZLE_DESTROY"); break;
                case "Lighthouse Break In": LoadJobHelper(7, -1, -1, 33551, "c_sawmill_ext", "EP7_ADDR", "EP7_LIGHTHOUSE_START"); break;
                case " [LBI] Enter the lighthouse": LoadJobHelper(7, 3130, 3132, 33551, "c_sawmill_ext", "EP7_ADDR", "EP7_LIGHTHOUSE_ENTER"); break;
                case " [LBI] Reach the door": LoadJobHelper(7, 3130, 3133, 33235, "c_sawmill_lighthouse", "EP7_ADDR", "EP7_LIGHTHOUSE_DOOR"); break;
                case " [LBI] Climb the lighthouse": LoadJobHelper(7, 3130, 3138, 393, "c_sawmill_lighthouse", "EP7_ADDR", "EP7_LIGHTHOUSE_CLIMB"); break;
                case "Thermal Ride": LoadJobHelper(7, -1, -1, 33645, "c_sawmill_ext", "EP7_ADDR", "EP7_THERMAL_START"); break;
                case " [TR] Paraglide": LoadJobHelper(7, 3188, 3190, 33645, "c_sawmill_ext", "EP7_ADDR", "EP7_THERMAL_PARAGLIDE"); break;
                case " [TR] Egg": LoadJobHelper(7, 3188, 3196, 33662, "c_sawmill_ext", "EP7_ADDR", "EP7_THERMAL_EGG"); break;
                case "Boat Hack": LoadJobHelper(8, -1, -1, 33596, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_START"); break;
                case " [BH] Go to hack 1": LoadJobHelper(8, 3156, 3158, 33596, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_GOH1"); break;
                case " [BH] Hack 1": LoadJobHelper(8, 3156, 3163, 33596, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_H1"); break;
                case " [BH] Go to hack 2": LoadJobHelper(8, 3156, 3168, 33610, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_GOH2"); break;
                case " [BH] Hack 2": LoadJobHelper(8, 3156, 3172, 33610, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_H2"); break;
                case " [BH] Go to hack 3": LoadJobHelper(8, 3156, 3176, 33624, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_GOH3"); break;
                case " [BH] Hack 3": LoadJobHelper(8, 3156, 3180, 33624, "c_sawmill_ext", "EP7_ADDR", "EP7_BOAT_H3"); break;
                case "OP: Canada Games": LoadJobHelper(8, -1, -1, 33664, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_START"); break;
                case " [OCG] Cut the log": LoadJobHelper(9, 3202, 3215, 33676, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_LOG"); break;
                case " [OCG] Carry the egg": LoadJobHelper(8, 3202, 3224, 33679, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_EGG"); break;
                case " [OCG] Climb the wall": LoadJobHelper(7, 3202, 3240, 33692, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_CLIMB"); break;
                case " [OCG] Shoot the hooks": LoadJobHelper(9, 3202, 3245, 33694, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_HOOKS"); break;
                case " [OCG] Balance on the logs": LoadJobHelper(8, 3202, 3261, 33733, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_BALANCE"); break;
                case " [OCG] Lure the ducks": LoadJobHelper(7, 3202, 3266, 33740, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_LURE"); break;
                case " [OCG] Enter the saw": LoadJobHelper(7, 3285, 3289, 393, "c_sawmill_boss", "EP7_ADDR", "EP7_OP_SAW"); break;
                case " [OCG] Bison fight": LoadJobHelper(8, 3285, 3291, 393, "c_sawmill_boss", "EP7_ADDR", "EP7_OP_BOSS"); break;
                case " [OCG] Reach the battery": LoadJobHelper(8, 3285, 3297, 33407, "c_sawmill_ext", "EP7_ADDR", "EP7_OP_BATTERY"); break;
                case "Blimp HQ Recon": LoadJobHelper(7, -1, -1, 17366, "a_blimp_ext", "EP8_ADDR", "EP8_RECON_START"); break;
                case " [BHR] Enter the blimp": LoadJobHelper(7, 3352, 3354, 17366, "a_blimp_ext", "EP8_ADDR", "EP8_RECON_ENTER"); break;
                case " [BHR] Photos": LoadJobHelper(7, 3352, 3354, 17372, "a_blimp_int", "EP8_ADDR", "EP8_RECON_PHOTOS"); break;
                case " [BHR] Photos (No CS)": LoadJobHelper(7, 3352, 3356, 17372, "a_blimp_int", "EP8_ADDR", "EP8_RECON_PHOTOS_NOCS"); break;
                case " [BHR] Keys": LoadJobHelper(7, 3352, 3368, 17384, "a_blimp_int", "EP8_ADDR", "EP8_RECON_KEYS"); break;
                case " [BHR] Magnets": LoadJobHelper(7, 3352, 3374, 17384, "a_blimp_int", "EP8_ADDR", "EP8_RECON_MAGNETS"); break;
                case "Bentley/Murray Team Up": LoadJobHelper(8, -1, -1, 17466, "a_blimp_ext", "EP8_ADDR", "EP8_BM_START"); break;
                case " [BMTU] Go to hack 1": LoadJobHelper(8, 3431, 3433, 17466, "a_blimp_ext", "EP8_ADDR", "EP8_BM_GOH1"); break;
                case " [BMTU] Hack 1": LoadJobHelper(8, 3431, 3434, 17470, "a_blimp_ext", "EP8_ADDR", "EP8_BM_H1"); break;
                case " [BMTU] Go to hack 2": LoadJobHelper(8, 3431, 3435, 17470, "a_blimp_ext", "EP8_ADDR", "EP8_BM_GOH2"); break;
                case " [BMTU] Hack 2": LoadJobHelper(8, 3431, 3436, 17473, "a_blimp_ext", "EP8_ADDR", "EP8_BM_H2"); break;
                case " [BMTU] Go to hack 3": LoadJobHelper(8, 3431, 3437, 17473, "a_blimp_ext", "EP8_ADDR", "EP8_BM_GOH3"); break;
                case " [BMTU] Hack 3": LoadJobHelper(8, 3431, 3438, 19472, "a_blimp_ext", "EP8_ADDR", "EP8_BM_H3"); break;
                case " [BMTU] First floor": LoadJobHelper(9, 3431, -1, 393, "a_blimp_engine_room_murray", "EP8_ADDR", "EP8_BM_F1"); break;
                case " [BMTU] Second floor": LoadJobHelper(9, 3431, 3450, 17446, "a_blimp_engine_room_murray", "EP8_ADDR", "EP8_BM_F2"); break;
                case "Murray/Sly Tag Team": LoadJobHelper(9, -1, -1, 17420, "a_blimp_ext", "EP8_ADDR", "EP8_MS_START"); break;
                case " [MSTT] Alarm 1": LoadJobHelper(9, 3401, 3403, 17420, "a_blimp_ext", "EP8_ADDR", "EP8_MS_A1"); break;
                case " [MSTT] Alarm 2": LoadJobHelper(9, 3401, 3406, 17429, "a_blimp_ext", "EP8_ADDR", "EP8_MS_A2"); break;
                case " [MSTT] Alarm 3": LoadJobHelper(9, 3401, 3407, 17432, "a_blimp_ext", "EP8_ADDR", "EP8_MS_A3"); break;
                case " [MSTT] Alarm 4": LoadJobHelper(9, 3401, 3404, 17423, "a_blimp_ext", "EP8_ADDR", "EP8_MS_A4"); break;
                case " [MSTT] Alarm 5": LoadJobHelper(9, 3401, 3405, 17426, "a_blimp_ext", "EP8_ADDR", "EP8_MS_A5"); break;
                case " [MSTT] Reach the engine": LoadJobHelper(9, 3401, 3408, 17435, "a_blimp_ext", "EP8_ADDR", "EP8_MS_REACH"); break;
                case " [MSTT] Lift the door": LoadJobHelper(9, 3401, 3409, 17436, "a_blimp_ext", "EP8_ADDR", "EP8_MS_LIFT"); break;
                case " [MSTT] First floor": LoadJobHelper(7, 3401, 3412, 393, "a_blimp_engine_room_jt", "EP8_ADDR", "EP8_MS_F1"); break;
                case " [MSTT] Second floor": LoadJobHelper(7, 3401, 3413, 17446, "a_blimp_engine_room_jt", "EP8_ADDR", "EP8_MS_F2"); break;
                case "Sly/Bentley Conspire": LoadJobHelper(7, -1, -1, 17451, "a_blimp_ext", "EP8_ADDR", "EP8_SB_START"); break;
                case " [SBC] Keys": LoadJobHelper(7, 3416, 3419, 17451, "a_blimp_ext", "EP8_ADDR", "EP8_SB_KEYS"); break;
                case " [SBC] Enter the engine": LoadJobHelper(7, 3416, 3420, 17451, "a_blimp_ext", "EP8_ADDR", "EP8_SB_ENTER"); break;
                case " [SBC] First floor": LoadJobHelper(8, 3416, 3427, 393, "a_blimp_engine_room_bentley", "EP8_ADDR", "EP8_SB_F1"); break;
                case " [SBC] Second floor": LoadJobHelper(8, 3416, 3428, 17466, "a_blimp_engine_room_bentley", "EP8_ADDR", "EP8_SB_F2"); break;
                case "Charged TNT Run": LoadJobHelper(7, -1, -1, 17401, "a_blimp_ext", "EP8_ADDR", "EP8_TNT_START"); break;
                case " [CTR] Enter TNT": LoadJobHelper(7, 3390, 3391, 17401, "a_blimp_ext", "EP8_ADDR", "EP8_TNT_ENTER"); break;
                case " [CTR] Checkpoint 1": LoadJobHelper(7, 3390, 3393, 17402, "a_blimp_ext", "EP8_ADDR", "EP8_TNT_CP1"); break;
                case " [CTR] Checkpoint 2": LoadJobHelper(7, 3390, 3394, 17404, "a_blimp_ext", "EP8_ADDR", "EP8_TNT_CP2"); break;
                case " [CTR] Checkpoint 3": LoadJobHelper(7, 3390, 3396, 17407, "a_blimp_ext", "EP8_ADDR", "EP8_TNT_CP3"); break;
                case "Mega-Jump Job": LoadJobHelper(7, -1, -1, 17485, "a_blimp_ext", "EP8_ADDR", "EP8_MEGA_START"); break;
                case " [MJJ] Climb": LoadJobHelper(7, 3464, 3466, 17485, "a_blimp_ext", "EP8_ADDR", "EP8_MEGA_CLIMB"); break;
                case "Showdown with Clock-La": LoadJobHelper(7, -1, -1, 17485, "a_blimp_ext", "EP8_ADDR", "EP8_CLOCKLA_START"); break;
                case " [SWC] Clock-La fight": LoadJobHelper(7, 3477, 3479, 17485, "a_blimp_ext", "EP8_ADDR", "EP8_CLOCKLA_FIGHT"); break;
                case " [SWC] Paraglide": LoadJobHelper(7, 3482, 3483, 393, "a_blimp_boss_final", "EP8_ADDR", "EP8_CLOCKLA_PARAGLIDE"); break;
                case " [SWC] Hit the head": LoadJobHelper(7, 3482, 3486, 13307, "a_blimp_boss_final", "EP8_ADDR", "EP8_CLOCKLA_HEAD"); break;
                case " [SWC] Get the hate chip": LoadJobHelper(9, 3482, 3491, 11233, "a_blimp_boss_final", "EP8_ADDR", "EP8_CLOCKLA_MURRAY"); break;
                default: break;
            }
        }

        private void LoadJobHelper(int characterId, int jobId, int checkpointId, int spawnLocationId, string mapName, string stateStartAddressKey, string stateValuesKey)
        {
            SetMapName("Y$KF" + mapName);
            SetSpawnLocation(spawnLocationId);
            SetActiveCharacter(characterId);

            string stateAddress = func.GetConfigData("data/s2_job_config.txt", stateStartAddressKey);
            string stateValues = func.GetConfigData("data/s2_job_config.txt", stateValuesKey);

            if (string.IsNullOrEmpty(stateAddress) || string.IsNullOrEmpty(stateValues))
            {
                Console.WriteLine($"Error: Missing state data for job {jobId} checkpoint {checkpointId}");
                return;
            }

            byte[] stateBytes = ConvertMemoryDataString(stateValues);
            WriteMemoryRegion((uint)Convert.ToInt32(stateAddress, 16), stateBytes);

            SetJobState(jobId, checkpointId);
            TriggerGameLoad(0);
            return;
        }

        public void KillActiveCharacter()
        {
            byte[] entityAddress = api.ReadMemory(pid, addr.activeCharacterPtr, 4);
            api.WriteMemory(pid, BitConverter.ToUInt32(entityAddress.Reverse().ToArray(), 0) + 0xDD4, 8);
        }

        public void SetupWebManPopUp()
        {
            if (this.api is Ratchetron)
            {
                int webManPopUpSubID = api.SubMemory(pid, addr.loadingState, 8, (value) =>
                {
                    if (value[4] == 3)
                    {
                        WebMAN.DisplayVersionPopUp(func.api.GetIP());
                    }
                });
            }
        }
    }
}
