using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    public class sly1 : IGame, IAutosplitterAvailable
    {
        public static Sly1Addresses addr = Sly1Addresses.ForGame(Sly1Addresses.DefaultGameId);

        public sly1(IPS3API api, string gameNameId) : base(api)
        {
            addr = Sly1Addresses.ForGame(gameNameId);
        }

        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[]
        {
            // World and level as one block, so the autosplitter never sees one updated without
            // the other. Blocks arrive byte-reversed, which puts the level ID first.
            (addr.worldId, 8),
            (addr.loadingState, 4),
            (addr.w3Keys, 4),
            (addr.charmsCount, 4),
            (addr.livesCount, 4),
            (addr.coinCount, 4),
            (addr.transitionState, 4),
            (addr.clockwerkHealth, 4),
        };

        /// <summary>
        /// The entity being played: Sly's, or the vehicle's when he's in one. 0 if none, or while
        /// a load is pending, when the entity is about to be freed.
        /// </summary>
        public uint GetActiveEntity()
        {
            if (ReadInt(sly1.addr.reloadBlock) != 0)
            {
                return 0;
            }
            uint sly = (uint)ReadInt(sly1.addr.slyEntityPtr);
            if (sly != 0 && ReadInt(sly + sly1.addr.slyActiveOffset) != 0)
            {
                return sly;
            }
            return (uint)ReadInt(sly1.addr.vehicleEntityPtr);
        }

        /// <summary>
        /// The name the current level's warps and position slots are kept under: its title, as
        /// the Level dropdown shows it. Empty when the level isn't known.
        /// </summary>
        public string GetCurrentMapIndicator()
        {
            int index = GetCurrentLevelIndex();
            return index >= 0 ? LevelTitle(index) : "";
        }

        public PositionEditorLayout GetPositionEditorLayout()
        {
            return new PositionEditorLayout
            {
                gameName = "Sly 1",
                warpFilePrefix = "sly1",
                readEntity = GetActiveEntity,
                readMapIndicator = GetCurrentMapIndicator,
                transformOffset = sly1.addr.transformOffset,
                transformInEntity = true,
                positionOffset = sly1.addr.positionOffset,
                velocityOffset = sly1.addr.velocityOffset,
                applyInfiniteJump = ApplyInfiniteJump,
                hideEntityFields = true,
            };
        }

        /// <summary>Gives Sly another double jump. Does nothing to a vehicle.</summary>
        public void ApplyInfiniteJump(uint entity)
        {
            if (entity == 0 || entity != (uint)ReadInt(sly1.addr.slyEntityPtr))
            {
                return;
            }
            api.WriteMemory(pid, entity + sly1.addr.infiniteJumpOffset1, ConvertIntToBytes(-1));
            api.WriteMemory(pid, entity + sly1.addr.infiniteJumpOffset2, ConvertIntToBytes(1));
        }

        private uint GetPlayerCoordsAddress()
        {
            uint entity = GetActiveEntity();
            if (entity == 0)
            {
                throw new InvalidOperationException("No player to move.");
            }
            return entity + sly1.addr.transformOffset + sly1.addr.positionOffset;
        }

        /// <summary>The config key a position slot is stored under, per level.</summary>
        private string SavedPositionKey()
        {
            string mapIndicator = GetCurrentMapIndicator();
            if (mapIndicator == "")
            {
                throw new InvalidOperationException("Couldn't read the current level.");
            }
            return SavedPositionKey(mapIndicator, selectedPositionIndex);
        }

        public override void SavePosition()
        {
            string key = SavedPositionKey();
            string position = api.ReadMemoryStr(pid, GetPlayerCoordsAddress(), 12);
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

            uint coords = GetPlayerCoordsAddress();
            api.WriteMemory(pid, coords, 12, position);
            // Arrive standing still rather than carrying the old speed.
            uint velocity = coords - sly1.addr.positionOffset + sly1.addr.velocityOffset;
            api.WriteMemory(pid, velocity, new byte[12]);
        }

        /// <summary>The Load Game combo: reloads the current level.</summary>
        public override void LoadGame()
        {
            ReloadLevel();
        }

        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            SubscribeSlyButtons(sly1.addr.inputOffset);
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            int analogLSubID = api.SubMemory(pid, sly1.addr.analogOffsetLeft, 8, (value) =>
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

            int analogRSubID = api.SubMemory(pid, sly1.addr.analogOffsetRight, 8, (value) =>
            {
                Inputs.ry = -1 * BitConverter.ToSingle(value, 0);
                Inputs.rx = BitConverter.ToSingle(value, 4);
            });
        }

        public void SetCoinCount(int coins)
        {
            api.WriteMemory(pid, sly1.addr.coinCount, ConvertIntToBytes(coins));
        }

        /// <summary>A level the Level dropdown lists: its index in the game's level list and its name.</summary>
        public class LevelEntry
        {
            public int index;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        // The game's level list: Splash, Paris and the Hideout, then 9 levels per world. The
        // world/level IDs map to it as (world - 1) * 9 + level + 3. Null entries are unused slots,
        // which have no entry in the reload table.
        private static readonly string[] LevelNames = new string[]
        {
            "Splash", "Paris", "Hideout",
            // Tide of Terror
            "A Stealthy Approach", "Prowling the Grounds", "High Class Heist", "Into the Machine",
            "A Cunning Disguise", "The Fire Down Below", "Treasure in the Depths", "The Gunboat Graveyard",
            "The Eye of the Storm",
            // Sunset Snake Eyes
            "A Rocky Start", "Muggshot's Turf", "Boneyard Casino", "Murray's Big Gamble", "At the Dog Track",
            "Two to Tango", "Straight to the Top", "Back Alley Heist", "Last Call",
            // Vicious Voodoo
            "The Dread Swamp Path", "The Swamp's Dark Centre", "The Lair of the Beast", "A Grave Undertaking",
            "Piranha Lake", "Descent into Danger", "A Ghastly Voyage", "Down Home Cooking", "A Deadly Dance",
            // Fire in the Sky
            "A Perilous Ascent", "Inside the Stronghold", "Flaming Temple of Flame", "The Unseen Foe",
            "The King of the Hill", "Rapid Fire Assault", "Duel by the Dragon", "A Desperate Race", "Flame Fu!",
            // A Cold Heart of Hate
            "A Hazardous Path", null, "Burning Rubber", "A Daring Rescue", "Bentley Comes Through",
            "A Temporary Truce", "Sinking Peril", null, "A Strange Reunion",
        };

        private static readonly string[] WorldNames = new string[]
        {
            null, "Tide of Terror", "Sunset Snake Eyes", "Vicious Voodoo", "Fire in the Sky", "A Cold Heart of Hate",
        };

        public const int LevelsPerWorld = 9;
        private const int FirstWorldLevelIndex = 3;

        /// <summary>Every level, in the game's order, named "World: Level".</summary>
        public static List<LevelEntry> GetLevels()
        {
            List<LevelEntry> levels = new List<LevelEntry>();
            for (int i = 0; i < LevelNames.Length; i++)
            {
                if (LevelNames[i] == null)
                {
                    continue;
                }
                levels.Add(new LevelEntry { index = i, name = LevelTitle(i) });
            }
            return levels;
        }

        public static string LevelTitle(int index)
        {
            if (index < 0 || index >= LevelNames.Length || LevelNames[index] == null)
            {
                return "-";
            }
            if (index < FirstWorldLevelIndex)
            {
                return LevelNames[index];
            }
            int world = (index - FirstWorldLevelIndex) / LevelsPerWorld + 1;
            return $"{WorldNames[world]}: {LevelNames[index]}";
        }

        /// <summary>The current level's index in the level list, or -1 if it isn't known.</summary>
        public int GetCurrentLevelIndex()
        {
            int world = ReadInt(sly1.addr.worldId);
            int level = ReadInt(sly1.addr.levelId);
            int index;
            if (world == 0)
            {
                // Level IDs 2 to 4: Splash, Paris, Hideout
                index = level - 2;
            }
            else
            {
                index = (world - 1) * LevelsPerWorld + level + FirstWorldLevelIndex;
            }
            return index >= 0 && index < LevelNames.Length && LevelNames[index] != null ? index : -1;
        }

        // The reload block's entrance for the level's own start. Any other entrance ID left there
        // from an earlier load belongs to another level, which can spawn Sly out of bounds or not
        // at all (a black screen).
        private const int LevelStartEntrance = -1;

        /// <summary>Loads a level, at one of its entrances or (entrance null) at its start.</summary>
        public void LoadLevel(int index, int? entrance)
        {
            if (index < 0 || index >= LevelNames.Length || LevelNames[index] == null)
            {
                throw new ArgumentException("Not a level.");
            }

            // The reload table has no entries for the two unused level slots.
            int tableIndex = index;
            for (int i = 0; i < index; i++)
            {
                if (LevelNames[i] == null)
                {
                    tableIndex--;
                }
            }

            uint language = (uint)Math.Max(0, ReadInt(sly1.addr.language));
            uint entry = sly1.addr.reloadTable + (uint)tableIndex * sly1.addr.reloadEntrySize
                + language * sly1.addr.reloadLanguageSize;

            api.WriteMemory(pid, sly1.addr.reloadBlock + 0x14,
                ConvertIntToBytes(entrance.HasValue ? entrance.Value : LevelStartEntrance));
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0x10, ConvertIntToBytes((int)entry));
            ReloadLevel();
        }

        /// <summary>Reloads the level the reload block points at: the current one, or the last loaded.</summary>
        public void ReloadLevel()
        {
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0xC, ConvertIntToBytes(1));
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0x20, ConvertIntToBytes(1));
            api.WriteMemory(pid, sly1.addr.reloadBlock, ConvertIntToBytes(1));
        }

        /// <summary>The current level's entrance IDs, in the game's order. Empty while loading.</summary>
        public List<int> GetEntrances()
        {
            List<int> entrances = new List<int>();
            uint list = (uint)ReadInt(sly1.addr.entranceListPtr);
            if (list == 0)
            {
                return entrances;
            }
            uint entrance = (uint)ReadInt(list + sly1.addr.entranceListOffset);
            // A level has a few dozen entrances; the cap stops a bad read from looping.
            for (int i = 0; entrance != 0 && i < 256; i++)
            {
                entrances.Add(ReadInt(entrance + 0x8));
                entrance = (uint)ReadInt(entrance + 0x24);
            }
            return entrances;
        }

        public int GetLives()
        {
            return ReadInt(sly1.addr.livesCount);
        }

        public void SetLives(int lives)
        {
            api.WriteMemory(pid, sly1.addr.livesCount, ConvertIntToBytes(lives));
        }

        public int GetCharms()
        {
            return ReadInt(sly1.addr.charmsCount);
        }

        /// <summary>0 = None, 1 = Blue, 2 = Gold.</summary>
        public void SetCharms(int charms)
        {
            api.WriteMemory(pid, sly1.addr.charmsCount, ConvertIntToBytes(charms));
        }

        public int GetCoins()
        {
            return ReadInt(sly1.addr.coinCount);
        }

        private int livesFreezeSubID = -1;
        private int charmsFreezeSubID = -1;

        /// <summary>Holds the lives at their current count, or releases them.</summary>
        public void SetLivesFrozen(bool frozen)
        {
            SetFrozen(ref livesFreezeSubID, frozen, sly1.addr.livesCount, Math.Max(GetLives(), 1));
        }

        /// <summary>Holds a gold charm, so the next two hits don't cost a life, or releases it.</summary>
        public void SetCharmsFrozen(bool frozen)
        {
            SetFrozen(ref charmsFreezeSubID, frozen, sly1.addr.charmsCount, 2);
        }

        /// <summary>
        /// Forgets freezes the console dropped when the game reset, so turning them on again
        /// makes new ones.
        /// </summary>
        public void ForgetFreezes()
        {
            livesFreezeSubID = -1;
            charmsFreezeSubID = -1;
        }

        private void SetFrozen(ref int subID, bool frozen, uint address, int value)
        {
            if (subID >= 0)
            {
                try
                {
                    api.ReleaseSubID(subID);
                }
                catch
                {
                    // The connection may already have dropped it.
                }
                subID = -1;
            }
            if (frozen)
            {
                subID = api.FreezeMemory(pid, address, IPS3API.MemoryCondition.Any, (uint)value);
            }
        }

        /// <summary>Ends the dialogue line that's playing, or the FMV when there's no dialogue.</summary>
        public void SkipCinematic()
        {
            uint dialogue = (uint)ReadInt(sly1.addr.dialoguePtr);
            if (dialogue == 0)
            {
                api.WriteMemory(pid, sly1.addr.fmvSkip, ConvertIntToBytes(1));
            }
            else
            {
                api.WriteMemory(pid, dialogue + sly1.addr.dialogueSkipOffset, ConvertIntToBytes(0));
            }
        }

        // World states. Worlds 1 to 5 are the five gangs' worlds, in LevelNames' order; each has
        // LevelsPerWorld level saves, in the order of its levels there.
        public const int WorldCount = 5;

        // A level save's flags
        public const int LevelUnlocked = 0x1;
        public const int LevelKey = 0x2;
        public const int LevelVault = 0x4;
        public const int LevelSprint = 0x8;

        // A world save's flags
        public const int WorldStarted = 0x1;
        public const int WorldKey1 = 0x2;
        public const int WorldKey3 = 0x4;
        public const int WorldKey7 = 0x8;
        public const int WorldBossDefeated = 0x20;

        // Tide of Terror stores its key flags in another order: Key 1 at 0x8, Key 3 at 0x2 and
        // Key 7 at 0x4. GetWorldFlags and SetWorldFlags use the other worlds' order for it too.
        private const int TideOfTerror = 1;
        private const int WorldKeyFlags = WorldKey1 | WorldKey3 | WorldKey7;

        /// <summary>A world's counts of keys collected, vaults opened and sprints completed.</summary>
        public enum WorldCounter
        {
            Keys,
            Vaults,
            Sprints,
        }

        public static string WorldName(int world)
        {
            return world >= 1 && world <= WorldCount ? WorldNames[world] : "-";
        }

        /// <summary>The level's name, or null for the two unused level slots.</summary>
        public static string WorldLevelName(int world, int level)
        {
            return LevelNames[(world - 1) * LevelsPerWorld + level + FirstWorldLevelIndex];
        }

        private static uint WorldSave(int world)
        {
            if (world < 1 || world > WorldCount)
            {
                throw new ArgumentException("Not a world.");
            }
            return sly1.addr.gameState + sly1.addr.worldSaveStart + (uint)world * sly1.addr.worldSaveSize;
        }

        private static uint LevelSave(int world, int level)
        {
            if (level < 0 || level >= LevelsPerWorld)
            {
                throw new ArgumentException("Not a level.");
            }
            return WorldSave(world) + (uint)level * sly1.addr.levelSaveSize;
        }

        private static uint WorldCounterAddress(int world, WorldCounter counter)
        {
            switch (counter)
            {
                case WorldCounter.Keys:
                    return WorldSave(world) + sly1.addr.worldKeysOffset;
                case WorldCounter.Vaults:
                    return WorldSave(world) + sly1.addr.worldVaultsOffset;
                default:
                    return WorldSave(world) + sly1.addr.worldSprintsOffset;
            }
        }

        /// <summary>The level's LevelUnlocked, LevelKey, LevelVault and LevelSprint flags.</summary>
        public int GetLevelFlags(int world, int level)
        {
            return ReadInt(LevelSave(world, level));
        }

        public void SetLevelFlags(int world, int level, int flags)
        {
            api.WriteMemory(pid, LevelSave(world, level), ConvertIntToBytes(flags));
        }

        public int GetWorldCounter(int world, WorldCounter counter)
        {
            return ReadInt(WorldCounterAddress(world, counter));
        }

        public void SetWorldCounter(int world, WorldCounter counter, int value)
        {
            api.WriteMemory(pid, WorldCounterAddress(world, counter), ConvertIntToBytes(value));
        }

        /// <summary>The world's WorldStarted, WorldKey1/3/7 and WorldBossDefeated flags.</summary>
        public int GetWorldFlags(int world)
        {
            int flags = ReadInt(WorldSave(world) + sly1.addr.worldFlagsOffset);
            if (world == TideOfTerror)
            {
                int keys = ((flags & 0x8) != 0 ? WorldKey1 : 0)
                    | ((flags & 0x2) != 0 ? WorldKey3 : 0)
                    | ((flags & 0x4) != 0 ? WorldKey7 : 0);
                flags = (flags & ~WorldKeyFlags) | keys;
            }
            return flags;
        }

        public void SetWorldFlags(int world, int flags)
        {
            if (world == TideOfTerror)
            {
                int keys = ((flags & WorldKey1) != 0 ? 0x8 : 0)
                    | ((flags & WorldKey3) != 0 ? 0x2 : 0)
                    | ((flags & WorldKey7) != 0 ? 0x4 : 0);
                flags = (flags & ~WorldKeyFlags) | keys;
            }
            api.WriteMemory(pid, WorldSave(world) + sly1.addr.worldFlagsOffset, ConvertIntToBytes(flags));
        }

        // The last loading state the Practice Mode popup saw: 1 while loading, 0 once done.
        private volatile int popUpLoadingState = -1;

        /// <summary>
        /// Shows webMAN's "SluMAN (Practice Mode)" popup on the PS3 after every load, like Sly 2
        /// and Sly 3, so a run's video shows Practice Mode was on. Ratchetron only. Call again
        /// after a reconnect.
        /// </summary>
        public void SetupWebManPopUp()
        {
            if (!(api is Ratchetron))
            {
                return;
            }
            popUpLoadingState = -1;
            api.SubMemory(pid, sly1.addr.loadingState, 4, (value) =>
            {
                int state = BitConverter.ToInt32(value, 0);
                bool finished = state == 0 && popUpLoadingState == 1;
                popUpLoadingState = state;
                if (finished)
                {
                    // A web request: not on the subscription thread.
                    string ip = api.GetIP();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => WebMAN.DisplayVersionPopUp(ip));
                }
            });
        }

        /// <summary>Whether a level is loading, or a load is about to start.</summary>
        public bool IsLevelLoading()
        {
            return ReadInt(sly1.addr.loadingState) != 0 || ReadInt(sly1.addr.reloadBlock) != 0;
        }

        /// <summary>Where the player's position is (3 floats), or 0 when there's no player.</summary>
        public uint GetPlayerPositionAddress()
        {
            uint entity = GetActiveEntity();
            return entity == 0 ? 0 : entity + sly1.addr.transformOffset + sly1.addr.positionOffset;
        }

        public float[] ReadPositionAt(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 12);
            return new float[] { ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8) };
        }

        /// <summary>Puts the player at a position (from GetPlayerPositionAddress) and stops them there.</summary>
        public void HoldPlayerAt(uint positionAddress, float[] position)
        {
            byte[] bytes = new byte[12];
            for (int axis = 0; axis < 3; axis++)
            {
                Array.Copy(ConvertFloatToBytes(position[axis]), 0, bytes, axis * 4, 4);
            }
            api.WriteMemory(pid, positionAddress, bytes);
            api.WriteMemory(pid, positionAddress - sly1.addr.positionOffset + sly1.addr.velocityOffset, new byte[12]);
        }

        // g_grfjoyt's bits and g_grfusr's, as UpdateGrfjoytFromGrfusr uses them.
        private const int InputTargetPlayer = 0x2;
        private const int UserStateMenu = 0x1;
        private const int UserStateHandsOff = 0x2;
        private const int UserStateOff = 0x84;

        /// <summary>
        /// Stops the controller from moving the player (Sly, and his binocucom), or gives it back
        /// as the game would have it. The game works the targets out again when its state
        /// changes (a cutscene, the pause menu), so keep blocking while it should hold.
        /// </summary>
        public void SetPlayerControlBlocked(bool blocked)
        {
            int targets = ReadInt(sly1.addr.inputTargets);
            if (blocked)
            {
                if ((targets & InputTargetPlayer) != 0)
                {
                    api.WriteMemory(pid, sly1.addr.inputTargets, ConvertIntToBytes(targets & ~InputTargetPlayer));
                }
                return;
            }

            int state = ReadInt(sly1.addr.userState);
            int gameTargets;
            if ((state & UserStateOff) != 0)
            {
                gameTargets = 0;
            }
            else if ((state & UserStateMenu) != 0)
            {
                gameTargets = 5;
            }
            else if ((state & UserStateHandsOff) != 0)
            {
                gameTargets = 4;
            }
            else
            {
                gameTargets = 7;
            }
            if (targets != gameTargets)
            {
                api.WriteMemory(pid, sly1.addr.inputTargets, ConvertIntToBytes(gameTargets));
            }
        }

        // --- Camera ---

        /// <summary>
        /// The part of a camera the camera hook puts in, and what it's worked out from. Vectors
        /// keep their W values.
        /// </summary>
        public class CameraState
        {
            // X, Y, Z, W
            public float[] position = new float[4];
            // The forward, left and up rows, each X, Y, Z, W. Z is up.
            public float[] rotation = new float[12];
            public float lodScale;
            // 4x4, row by row
            public float[] projection = new float[16];
            public float lodFov;
            public float fov;
            public float fovTarget;
            public float aspect;
            // tan(fov / 2) * aspect, tan(fov / 2)
            public float[] frustum = new float[2];

            public CameraState Clone()
            {
                CameraState copy = (CameraState)MemberwiseClone();
                copy.position = (float[])position.Clone();
                copy.rotation = (float[])rotation.Clone();
                copy.projection = (float[])projection.Clone();
                copy.frustum = (float[])frustum.Clone();
                return copy;
            }

            /// <summary>
            /// The projection, frustum and LOD scale for another field of view (radians), worked
            /// out as the game does (RecalcCmFrustrum) from these ones. False when these aren't a
            /// camera's.
            /// </summary>
            public bool SetFov(CameraState from, float newFov)
            {
                if (from.frustum[0] <= 0 || from.frustum[1] <= 0 || newFov <= 0)
                {
                    return false;
                }
                float tanY = (float)Math.Tan(newFov / 2);
                float tanX = tanY * from.aspect;
                projection = (float[])from.projection.Clone();
                projection[0] = from.projection[0] * from.frustum[0] / tanX;
                projection[5] = from.projection[5] * from.frustum[1] / tanY;
                frustum = new float[] { tanX, tanY };
                lodScale = Math.Max(1, from.lodFov / newFov);
                fov = newFov;
                fovTarget = newFov;
                return true;
            }
        }

        /// <summary>The main camera (the CM struct), or 0 before the game has one.</summary>
        public uint GetMainCamera()
        {
            return (uint)ReadInt(sly1.addr.cameraPointer);
        }

        // The camera is read from its position to the end of the frustum in one read.
        private const uint CameraBlockSize = 0x1B8;

        public CameraState ReadCamera(uint camera)
        {
            uint start = camera + sly1.addr.cameraPositionOffset;
            byte[] bytes = api.ReadMemory(pid, start, CameraBlockSize);
            Func<uint, int> at = (offset) => (int)(offset - sly1.addr.cameraPositionOffset);

            CameraState state = new CameraState();
            ReadFloats(bytes, at(sly1.addr.cameraPositionOffset), state.position);
            ReadFloats(bytes, at(sly1.addr.cameraRotationOffset), state.rotation);
            ReadFloats(bytes, at(sly1.addr.cameraProjectionOffset), state.projection);
            ReadFloats(bytes, at(sly1.addr.cameraFrustumOffset), state.frustum);
            state.lodScale = ToFloat(bytes, at(sly1.addr.cameraLodScaleOffset));
            state.lodFov = ToFloat(bytes, at(sly1.addr.cameraLodFovOffset));
            state.fov = ToFloat(bytes, at(sly1.addr.cameraFovOffset));
            state.fovTarget = ToFloat(bytes, at(sly1.addr.cameraFovTargetOffset));
            state.aspect = ToFloat(bytes, at(sly1.addr.cameraAspectOffset));
            return state;
        }

        /// <summary>
        /// Writes a camera's field of view, its target, projection, frustum and LOD scale (to give
        /// the game back its own).
        /// </summary>
        public void WriteCameraFov(uint camera, CameraState state)
        {
            api.WriteMemory(pid, camera + sly1.addr.cameraFovOffset, FloatsToBytes(new float[] { state.fov, state.fovTarget }));
            api.WriteMemory(pid, camera + sly1.addr.cameraProjectionOffset, FloatsToBytes(state.projection));
            api.WriteMemory(pid, camera + sly1.addr.cameraFrustumOffset, FloatsToBytes(state.frustum));
            api.WriteMemory(pid, camera + sly1.addr.cameraLodScaleOffset, ConvertFloatToBytes(state.lodScale));
        }

        // The camera hook's data: +0x0 on, +0x4 the camera, +0x8 how many times the stub has put
        // the camera in (so SluMAN can tell it runs), +0xC SluMAN's mark. The values put in are
        // at their offsets in the camera (+0x40 to +0x1F8), and the list of word offsets the stub
        // copies (ending with 0) at +0x200.
        private const uint CameraHookMark = 0x534C4D31; // "SLM1"
        private const uint CameraHookListOffset = 0x200;

        /// <summary>
        /// The camera's word offsets the stub copies: the position, rotation, LOD scale,
        /// projection, field of view and its target, and frustum.
        /// </summary>
        private static List<uint> CameraHookOffsets()
        {
            List<uint> offsets = new List<uint>();
            Action<uint, int> add = (offset, words) =>
            {
                for (int i = 0; i < words; i++)
                {
                    offsets.Add(offset + (uint)i * 4);
                }
            };
            add(sly1.addr.cameraPositionOffset, 4);
            add(sly1.addr.cameraRotationOffset, 12);
            add(sly1.addr.cameraLodScaleOffset, 1);
            add(sly1.addr.cameraProjectionOffset, 16);
            add(sly1.addr.cameraFovOffset, 1);
            add(sly1.addr.cameraFovTargetOffset, 1);
            add(sly1.addr.cameraFrustumOffset, 2);
            return offsets;
        }

        private static uint CameraHookDataSize()
        {
            return CameraHookListOffset + (uint)(CameraHookOffsets().Count + 1) * 4;
        }

        /// <summary>
        /// Starts the camera hook for a camera: from the next frame, the game's camera matrix
        /// update puts the state given to SetCameraHookState into the camera before working out
        /// the matrices it's drawn with.
        /// </summary>
        public void StartCameraHook(uint camera, CameraState state)
        {
            uint function = sly1.addr.cameraMatrixUpdateFunction;
            uint stub = sly1.addr.cameraHookStub;
            uint data = sly1.addr.cameraHookData;
            foreach (uint call in sly1.addr.cameraMatrixUpdateCalls)
            {
                CheckCall(call, function, stub, true);
            }
            foreach (uint call in sly1.addr.cameraMatrixUpdateTailCalls)
            {
                CheckCall(call, function, stub, false);
            }

            // Both go in unused memory: zeros, or what SluMAN put there before.
            byte[] code = CameraHookStub();
            byte[] codeThere = api.ReadMemory(pid, stub, (uint)code.Length);
            if (!codeThere.SequenceEqual(code) && codeThere.Any(b => b != 0))
            {
                throw new InvalidOperationException($"The memory at 0x{stub:X} SluMAN puts its camera code in isn't empty. Restart the game, or the camera hook isn't available on this version.");
            }
            uint dataSize = CameraHookDataSize();
            byte[] dataThere = api.ReadMemory(pid, data, dataSize);
            if ((uint)ReadInt(data + 0xC) != CameraHookMark && dataThere.Any(b => b != 0))
            {
                throw new InvalidOperationException($"The memory at 0x{data:X} SluMAN keeps its camera in is in use.");
            }

            // Off while the stub and the values are written, then on.
            byte[] header = new byte[0x10];
            Array.Copy(BitConverter.GetBytes(camera).Reverse().ToArray(), 0, header, 4, 4);
            Array.Copy(BitConverter.GetBytes(CameraHookMark).Reverse().ToArray(), 0, header, 0xC, 4);
            api.WriteMemory(pid, data, header);
            SetCameraHookState(state);
            List<uint> offsets = CameraHookOffsets();
            offsets.Add(0);
            api.WriteMemory(pid, data + CameraHookListOffset, WordsToBytes(offsets.ToArray()));
            if (!codeThere.SequenceEqual(code))
            {
                api.WriteMemory(pid, stub, code);
                if (!api.ReadMemory(pid, stub, (uint)code.Length).SequenceEqual(code))
                {
                    throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
                }
            }
            api.WriteMemory(pid, data, BitConverter.GetBytes(1u).Reverse().ToArray());
            foreach (uint call in sly1.addr.cameraMatrixUpdateCalls)
            {
                api.WriteMemory(pid, call, BitConverter.GetBytes(Branch(call, stub, true)).Reverse().ToArray());
            }
            foreach (uint call in sly1.addr.cameraMatrixUpdateTailCalls)
            {
                api.WriteMemory(pid, call, BitConverter.GetBytes(Branch(call, stub, false)).Reverse().ToArray());
            }
        }

        /// <summary>Throws unless a call goes to the function, or to the stub (SluMAN's hook from before).</summary>
        private void CheckCall(uint call, uint function, uint stub, bool link)
        {
            uint instruction = (uint)ReadInt(call);
            if (instruction != Branch(call, function, link) && instruction != Branch(call, stub, link))
            {
                throw new InvalidOperationException("The game's code isn't what SluMAN expects, so the camera hook isn't available on this version.");
            }
        }

        /// <summary>The state the camera hook puts in, from the next frame. One write.</summary>
        public void SetCameraHookState(CameraState state)
        {
            uint start = sly1.addr.cameraPositionOffset;
            byte[] bytes = new byte[CameraBlockSize];
            Action<uint, float[]> put = (offset, values) =>
            {
                byte[] valueBytes = FloatsToBytes(values);
                Array.Copy(valueBytes, 0, bytes, offset - start, valueBytes.Length);
            };
            put(sly1.addr.cameraPositionOffset, state.position);
            put(sly1.addr.cameraRotationOffset, state.rotation);
            put(sly1.addr.cameraLodScaleOffset, new float[] { state.lodScale });
            put(sly1.addr.cameraProjectionOffset, state.projection);
            put(sly1.addr.cameraFovOffset, new float[] { state.fov });
            put(sly1.addr.cameraFovTargetOffset, new float[] { state.fovTarget });
            put(sly1.addr.cameraFrustumOffset, state.frustum);
            api.WriteMemory(pid, sly1.addr.cameraHookData + start, bytes);
        }

        /// <summary>How many times the camera hook has put the camera in since it started.</summary>
        public uint CameraHookCount()
        {
            return (uint)ReadInt(sly1.addr.cameraHookData + 8);
        }

        /// <summary>Lets the game's own camera through again, and puts the game's calls back.</summary>
        public void StopCameraHook()
        {
            api.WriteMemory(pid, sly1.addr.cameraHookData, new byte[4]);
            uint function = sly1.addr.cameraMatrixUpdateFunction;
            uint stub = sly1.addr.cameraHookStub;
            foreach (uint call in sly1.addr.cameraMatrixUpdateCalls)
            {
                if ((uint)ReadInt(call) == Branch(call, stub, true))
                {
                    api.WriteMemory(pid, call, BitConverter.GetBytes(Branch(call, function, true)).Reverse().ToArray());
                }
            }
            foreach (uint call in sly1.addr.cameraMatrixUpdateTailCalls)
            {
                if ((uint)ReadInt(call) == Branch(call, stub, false))
                {
                    api.WriteMemory(pid, call, BitConverter.GetBytes(Branch(call, function, false)).Reverse().ToArray());
                }
            }
        }

        /// <summary>
        /// The camera hook's stub, called instead of the camera matrix update with the camera in
        /// r3. It only uses registers a call may change (r5 to r7, r11, r12 and cr0), then
        /// branches on into the function, which returns to the caller.
        /// </summary>
        private byte[] CameraHookStub()
        {
            uint stub = sly1.addr.cameraHookStub;
            uint data = sly1.addr.cameraHookData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;

            uint[] words =
            {
                0x3D600000 | high,                      //  0 lis r11, data@ha
                0x396B0000 | low,                       //  1 addi r11, r11, data@l
                0x818B0000,                             //  2 lwz r12, 0(r11) (on)
                0x2C0C0000,                             //  3 cmpwi r12, 0
                0x41820038,                             //  4 beq +0x38 (to 18: off)
                0x818B0004,                             //  5 lwz r12, 4(r11) (the camera)
                0x7C036000,                             //  6 cmpw r3, r12
                0x4082002C,                             //  7 bne +0x2C (to 18: another camera)
                0x38AB0000 | (CameraHookListOffset - 4), //  8 addi r5, r11, list - 4
                0x84C50004,                             //  9 lwzu r6, 4(r5) (next offset)
                0x2C060000,                             // 10 cmpwi r6, 0
                0x41820010,                             // 11 beq +0x10 (to 15: done)
                0x7CEB302E,                             // 12 lwzx r7, r11, r6
                0x7CE3312E,                             // 13 stwx r7, r3, r6
                0x4BFFFFEC,                             // 14 b -0x14 (to 9)
                0x80EB0008,                             // 15 lwz r7, 8(r11)
                0x38E70001,                             // 16 addi r7, r7, 1
                0x90EB0008,                             // 17 stw r7, 8(r11) (count)
                Branch(stub + 18 * 4, sly1.addr.cameraMatrixUpdateFunction, false), // 18 b to the function
            };
            return WordsToBytes(words);
        }

        private static byte[] WordsToBytes(uint[] words)
        {
            byte[] bytes = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(words[i]).Reverse().ToArray(), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        private static uint Branch(uint from, uint to, bool link)
        {
            return 0x48000000 | ((to - from) & 0x3FFFFFC) | (link ? 1u : 0u);
        }

        // --- HUD and fog ---

        /// <summary>Hides the HUD (every blot's Draw does nothing) or puts it back.</summary>
        public void SetHudHidden(bool hidden)
        {
            uint[,] draws = sly1.addr.blotVtableDraws;
            uint empty = sly1.addr.emptyFunctionDescriptor;
            // All checked first, so the HUD is never left half hidden.
            for (int i = 0; i < draws.GetLength(0); i++)
            {
                uint slot = (uint)ReadInt(draws[i, 0] + sly1.addr.blotDrawSlotOffset);
                if (slot != draws[i, 1] && slot != empty)
                {
                    throw new InvalidOperationException("The game's HUD isn't what SluMAN expects, so Hide HUD isn't available on this version.");
                }
            }
            for (int i = 0; i < draws.GetLength(0); i++)
            {
                uint value = hidden ? empty : draws[i, 1];
                api.WriteMemory(pid, draws[i, 0] + sly1.addr.blotDrawSlotOffset, BitConverter.GetBytes(value).Reverse().ToArray());
            }
        }

        public bool IsHudHidden()
        {
            uint[,] draws = sly1.addr.blotVtableDraws;
            return (uint)ReadInt(draws[0, 0] + sly1.addr.blotDrawSlotOffset) == sly1.addr.emptyFunctionDescriptor;
        }

        private const float NoFogAmount = 0f;
        private readonly Dictionary<string, float> levelFogBeforeNoFog = new Dictionary<string, float>();

        /// <summary>
        /// Turns the current level's fog off, or puts back the amount saved for that level. False
        /// when there's no camera (a load). A load sets the amount again, so call it again after one.
        /// </summary>
        public bool SetFogDisabled(bool disabled)
        {
            uint camera = GetMainCamera();
            string level = GetCurrentMapIndicator();
            if (camera == 0 || level == "")
            {
                return false;
            }

            uint fog = camera + sly1.addr.cameraFogOffset;
            if (disabled)
            {
                if (!levelFogBeforeNoFog.ContainsKey(level))
                {
                    float current = ToFloat(api.ReadMemory(pid, fog, 4), 0);
                    if (current != NoFogAmount)
                    {
                        levelFogBeforeNoFog[level] = current;
                    }
                }
                api.WriteMemory(pid, fog, ConvertFloatToBytes(NoFogAmount));
            }
            else
            {
                float original;
                if (levelFogBeforeNoFog.TryGetValue(level, out original))
                {
                    api.WriteMemory(pid, fog, ConvertFloatToBytes(original));
                    levelFogBeforeNoFog.Remove(level);
                }
            }
            return true;
        }

        public bool IsFogDisabled()
        {
            uint camera = GetMainCamera();
            return camera != 0 && ToFloat(api.ReadMemory(pid, camera + sly1.addr.cameraFogOffset, 4), 0) == NoFogAmount;
        }

        private static void ReadFloats(byte[] bytes, int offset, float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ToFloat(bytes, offset + i * 4);
            }
        }

        private static byte[] FloatsToBytes(float[] values)
        {
            byte[] bytes = new byte[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
            {
                Array.Copy(ConvertFloatToBytes(values[i]), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        private static float ToFloat(byte[] bytes, int offset)
        {
            byte[] value = new byte[4];
            Array.Copy(bytes, offset, value, 0, 4);
            return BitConverter.ToSingle(value.Reverse().ToArray(), 0);
        }

        private int ReadInt(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }
    }
}
