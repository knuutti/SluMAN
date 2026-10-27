using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SluMAN
{
    public class sly4 : IGame, IAutosplitterAvailable, IAutosplitterComputedValues
    {
        public static Sly4Addresses addr = Sly4Addresses.ForGame(Sly4Addresses.DefaultGameId);

        private const uint MainMenuLevelHash = 0xE4927E12;

        // The hacking minigames' level names start with one of these.
        private static readonly string[] HackLevelPrefixes = { "System Cracker", "Spark Runner", "Alter Ego" };

        // Display names for level hashes (gm + currentLevelHashOffset). Hashes not listed show in hex.
        private static readonly Dictionary<uint, string> levelNames = new Dictionary<uint, string>
        {
            // The names are from Scripts/Sly4Levels.xmb in Sly4.psarc; the hash is of the name as
            // written there (e.g. "IceAge HuntingGrounds").
            { 0xE4927E12, "Main Menu" },
            // "System Cracker01" in the level list, but it's where the Paris Prologue's sewers play.
            { 0x3B677F57, "Paris Sewers" },
            { 0x46C660F7, "Paris Hideout" },
            { 0xA0355EFB, "Paris Streets" },
            { 0x0C44F02D, "Paris Blimp" },
            { 0xBC676503, "Japan Bridge" },
            { 0xACAA7E76, "Japan Fishing Caves" },
            { 0x1D17219B, "Japan Geisha House" },
            { 0xFE492072, "Japan Hideout" },
            { 0xD44B7705, "Japan Prison" },
            { 0x29E1A78C, "Japan Rooftops" },
            { 0xA3907718, "Japan Sushi House" },
            { 0xE3541252, "Japan Village" },
            { 0xF69E553B, "West Hideout" },
            { 0x3FEEDCAF, "West Mines" },
            { 0xFAC4F502, "West Presidio" },
            { 0xC7B84D81, "West Saloon" },
            { 0x4192876D, "West Stagecoach" },
            { 0x1AD96E63, "West Town" },
            { 0x40556837, "West Train" },
            { 0xC9A34D20, "Ice Age Hideout" },
            { 0xF285665B, "Ice Age Arena" },
            { 0xC25ED889, "Ice Age Hunting Grounds" },
            { 0xF4596EEC, "Ice Age Ice Cave" },
            { 0xFCA16EEF, "Ice Age Rock Cliff" },
            { 0x4A2AD4EA, "Ice Age Mountain Base" },
            { 0xAEAE3F00, "Ice Age Village" },
            { 0x59CC1B15, "England Village" },
            { 0x74C12935, "England Hideout" },
            { 0xBF4B1637, "England Bakery" },
            { 0x350D543B, "England Blacksmith" },
            { 0x63910843, "England Circus Tent" },
            { 0x716016C8, "England Dragon Lair" },
            { 0x85EA7462, "England Shoemaker" },
            { 0xDE07516F, "England Tavern" },
            { 0x2986D493, "England Ward" },
            { 0xA70C9716, "Arabia Village" },
            { 0xE3DEF6B5, "Arabia Carpet Shop" },
            { 0x467B8C48, "Arabia Lamp Shop" },
            { 0x2E01E5EB, "Arabia Harem" },
            { 0x95841725, "Arabia Library" },
            { 0x2291A3C2, "Arabia Taxi Shop" },
            { 0xC201A536, "Arabia Hideout" },
            { 0xE3607734, "Spark Runner 01" },
            { 0xE3607735, "Spark Runner 02" },
            { 0xE3607736, "Spark Runner 03" },
            { 0xE3607737, "Spark Runner 04" },
            { 0xE3607738, "Spark Runner 05" },
            { 0xE3607739, "Spark Runner 06" },
            { 0xE360773A, "Spark Runner 07" },
            { 0x264C5604, "Alter Ego 01" },
            { 0x264C5605, "Alter Ego 02" },
            { 0x264C5606, "Alter Ego 03" },
            { 0x264C5607, "Alter Ego 04" },
            { 0x264C5608, "Alter Ego 05" },
            { 0x264C5609, "Alter Ego 06" },
            { 0xA33E6B59, "Alter Ego 06b" },
            { 0xA33E6B5A, "Alter Ego 06c" },
            { 0xA33E6B5B, "Alter Ego 06d" },
            { 0xA33E6B6F, "Alter Ego 06x" },
            { 0xBDB9E4B0, "Alter Ego 06 Multi" },
            { 0xE650A160, "Alter Ego 06b Multi" },
            { 0xE80579FF, "Alter Ego 06c Multi" },
            { 0xE9BA529E, "Alter Ego 06d Multi" },
            { 0x0BDB3F0A, "Alter Ego 06x Multi" },
            { 0x264C560A, "Alter Ego 07" },
            { 0xA33E6B78, "Alter Ego 07b" },
            { 0xA33E6B79, "Alter Ego 07c" },
            { 0xA33E6B7A, "Alter Ego 07d" },
            { 0xA33E6B8E, "Alter Ego 07x" },
            { 0xBF6EBD4F, "Alter Ego 07 Multi" },
            { 0x1B36DCA1, "Alter Ego 07b Multi" },
            { 0x1CEBB540, "Alter Ego 07c Multi" },
            { 0x1EA08DDF, "Alter Ego 07d Multi" },
            { 0x40C17A4B, "Alter Ego 07x Multi" },
            { 0x264C560B, "Alter Ego 08" },
            { 0x3B677F58, "System Cracker 02" },
            { 0x3B677F59, "System Cracker 03" },
            { 0x3B677F5A, "System Cracker 04" },
            { 0x3B677F5B, "System Cracker 05" },
            { 0x3B677F5D, "System Cracker 07" },
            { 0x3B677F5E, "System Cracker 08" },
            { 0x31886CA4, "System Cracker 08b" },
            { 0x31886CA5, "System Cracker 08c" },
            { 0x31886CA6, "System Cracker 08d" },
            { 0x3B677F5F, "System Cracker 09" },
            { 0x2AF1211A, "System Cracker 09 Multi" },
        };

        // Episode and job titles by their name key without "$" and "TitleName", from the English
        // text in GUI/Strings_English.str in Sly4.psarc. The minigames' keys (AlterEgo01 and so on)
        // read fine as they are, so Title spaces them out instead.
        private static readonly Dictionary<string, string> titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Episodes
            { "ParisPrologue", "Paris Prologue" },
            { "Japan", "Turning Japanese" },
            { "West", "Go West Young Raccoon" },
            { "IceAge", "Clan of the Cave Raccoon" },
            { "England", "Of Mice and Mechs" },
            { "Arabia", "Forty Thieves" },
            { "ParisEpilogue", "Paris Epilogue" },
            { "Minigame", "Minigames" },
            // Jobs
            { "Prologue", "I Smell A Rat" },
            { "PhotoOp", "Photo Op" },
            { "Breakout", "Breakout!" },
            { "SushiStartup", "Sushi House Startup" },
            { "Fishy", "Something's Fishy" },
            { "PrettyPink", "Pretty in Pinker" },
            { "MadameGeisha", "Tiger Tail" },
            { "SpikedSushi", "Spiked Sushi" },
            { "OpAltSick", "Altitude Sickness" },
            { "UnderArrest", "Under Arrest" },
            { "JailhouseBlues", "Jailhouse Blues" },
            { "ToothpicksVault", "Cooper for Hire" },
            { "SaloonBug", "Saloon Bug" },
            { "BlindDate", "Blind Date" },
            { "JailBreak", "Jail Break" },
            { "GrandKeyLarceny", "Grand Key Larceny" },
            { "OperationGoldDigger", "Operation: Gold Digger" },
            { "StoneAgeReconnaissance", "Stone Age Reconnaissance" },
            { "CavemanCoopersRescue", "Unexpected Ties" },
            { "GettingStronger", "Getting Stronger" },
            { "GoingUp", "Going Up" },
            { "HungryHungryHippo", "Starving Starving Hippo" },
            { "IceIceBentley", "Ice Ice Bentley" },
            { "DutyCalls", "Duty Calls" },
            { "OperationJurassicThievery", "Operation: Jurassic Thievery" },
            { "ShoppingSpree", "Shopping Spree" },
            { "JugglingAct", "Juggling Act" },
            { "CaneSwipe", "Cane Swipe" },
            { "RobbinHoods", "Eye in the Sky" },
            { "MechanicalMenace", "Mechanical Menace" },
            { "ShortSupply", "Short Supply" },
            { "ShellShocked", "Shell Shocked Heart" },
            { "HardTarget", "Hard Target" },
            { "OperationMousetrap", "Operation: Mousetrap" },
            { "LostAndFound", "Lost and Found" },
            { "SpecialDelivery", "Open Sesame" },
            { "CrazyClimber", "Rug Rats" },
            { "TaxiDriver", "Copy Cats" },
            { "UpInSmoke", "Up In Smoke" },
            { "RockAndRoll", "Heavy Metal Meltdown" },
            { "AllRolledUp", "All Rolled Up!" },
            { "Epilogue", "Deja Vu All Over Again!" },
        };

        private const int NormalClampFreqMax = 60;
        private const int NormalClampFreqMin = 1;
        private const int SlowMotionClampFreqMax = 386;
        private const int SlowMotionClampFreqMin = 291;

        private readonly Sly4CodeHook codeHook;

        // The debug menu's Tuning settings, read from the game when first needed.
        public readonly Sly4Tunables tunables;

        // Follows the current goal through memory subscriptions while it runs (see StartGoalWatcher).
        private Sly4GoalWatcher goalWatcher;

        // The autosplitter's values while it runs, and whether it started the goal watcher.
        private Sly4AutosplitterValues autosplitterValues;
        private bool autosplitterStartedGoalWatcher;

        /// <summary>
        /// Raised on a background thread when the current goal or the goal list changes, while the
        /// goal watcher runs.
        /// </summary>
        public event Action GoalsChanged;

        // Without the goal watcher (the old webMAN API has no subscriptions), each goal's state is
        // its own read, so the Practice panel and the Jobs tab share one reading of them, taken at
        // most this often. On a PS3 every read is a network round trip.
        private static readonly TimeSpan GoalSnapshotMaxAge = TimeSpan.FromSeconds(2);
        private List<uint> snapshotGoals = new List<uint>();
        private List<int> snapshotStates = new List<int>();
        private DateTime snapshotTakenAt = DateTime.MinValue;
        // The current goal's name, read again only when the current goal changes.
        private uint snapshotNameGoal;
        private string snapshotName;

        // The most read at once: Ratchetron's receive buffer is about 2 KB.
        private const uint MaxReadSize = 0x800;

        /// <summary>A job in the game's job array.</summary>
        public class JobEntry
        {
            public int index;
            // Name keys, e.g. "PhotoOp" and "Japan", as Sly4Goals knows them.
            public string name;
            public string episode;

            public string Title => sly4.Title(name);
            public string EpisodeTitle => sly4.Title(episode);

            public override string ToString()
            {
                return $"{EpisodeTitle}: {Title}";
            }
        }

        /// <summary>A level for the level warp list.</summary>
        public class LevelEntry
        {
            public uint hash;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        /// <summary>A named object in a level the player can be spawned at.</summary>
        public class SpawnPointEntry
        {
            public string name;
            public string title;

            public override string ToString()
            {
                return title;
            }
        }

        /// <summary>A playable character, by the name the game spawns it with.</summary>
        public class CharacterEntry
        {
            public string name;
            public string title;

            public override string ToString()
            {
                return title;
            }
        }

        // The characters a level can be loaded as. Each era's ancestor is only in their own
        // era's levels.
        public static readonly CharacterEntry[] Characters =
        {
            new CharacterEntry { name = "Sly", title = "Sly" },
            new CharacterEntry { name = "Bentley", title = "Bentley" },
            new CharacterEntry { name = "Murray", title = "Murray" },
            new CharacterEntry { name = "CarmelitaFox", title = "Carmelita" },
            new CharacterEntry { name = "Rioichi", title = "Rioichi" },
            new CharacterEntry { name = "KidCooper", title = "Tennessee Kid" },
            new CharacterEntry { name = "CavemanCooper", title = "Bob" },
            new CharacterEntry { name = "SirGalleth", title = "Sir Galleth" },
            new CharacterEntry { name = "Salim", title = "Salim al Kupar" },
        };

        private const int SpawnCharacterLength = 64;

        // Spawn point names start with one of these; the titles leave it out.
        private static readonly string[] SpawnPointPrefixes = { "Spawn_Player_", "Spawn_" };

        public sly4(IPS3API api, string gameNameId) : base(api)
        {
            addr = Sly4Addresses.ForGame(gameNameId);
            codeHook = new Sly4CodeHook(this);
            tunables = new Sly4Tunables(this);
        }

        public override void SavePosition()
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                throw new InvalidOperationException("The player isn't loaded.");
            }

            string key = SavedPositionKey(CurrentPositionMap(), selectedPositionIndex);
            string position = api.ReadMemoryStr(pid, body + addr.bodyPositionOffset, 12);
            func.ChangeFileLines("config.txt", position, key);
            OnPositionSaved();
        }

        public override void LoadPosition()
        {
            string position = func.GetConfigData("config.txt", SavedPositionKey(CurrentPositionMap(), selectedPositionIndex));
            if (position == "")
            {
                throw new InvalidOperationException($"Slot {selectedPositionIndex + 1} has no saved position.");
            }

            byte[] bytes = IPS3API.HexToBytes(position);
            Teleport(ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8));
        }

        // TODO: The pad address isn't known yet, so there is no input display and combos never fire.
        /// <summary>
        /// None: every Sly 4 value hangs off the game or episode manager, which only exist once the
        /// game has booted, so they're all computed values (see Sly4AutosplitterValues).
        /// </summary>
        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[0];

        public void StartComputedValues(Action<int, byte[]> write)
        {
            StopComputedValues();
            autosplitterValues = new Sly4AutosplitterValues(this, write);
            autosplitterValues.Start();
            GoalsChanged += WriteAutosplitterGoal;
            if (goalWatcher == null)
            {
                StartGoalWatcher();
                autosplitterStartedGoalWatcher = true;
            }
            WriteAutosplitterGoal();
        }

        public void StopComputedValues()
        {
            GoalsChanged -= WriteAutosplitterGoal;
            if (autosplitterValues != null)
            {
                autosplitterValues.Stop();
                autosplitterValues = null;
            }
            if (autosplitterStartedGoalWatcher)
            {
                StopGoalWatcher();
                autosplitterStartedGoalWatcher = false;
            }
        }

        /// <summary>Copies the goal watcher's current goal to the autosplitter. Runs on the watcher's threads.</summary>
        private void WriteAutosplitterGoal()
        {
            Sly4AutosplitterValues values = autosplitterValues;
            Sly4GoalWatcher watcher = goalWatcher;
            if (values == null || watcher == null)
            {
                return;
            }
            List<uint> goals;
            List<int> states;
            int current;
            watcher.GetState(out goals, out states, out current);
            values.WriteGoal(current, goals.Count);
        }

        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        // How often to look for the pad while the game is still booting.
        private const int PadRetryMs = 1000;

        // Bumped by each SetupInputDisplayMemorySubs, so a wait for the pad from before a
        // reconnect gives up instead of subscribing too.
        private int padGeneration;

        /// <summary>
        /// Subscribes to the pad through the game's pointer to it. The pointer is set early in
        /// boot, so right after a reset it can still be 0; then this waits for it on a background
        /// thread.
        /// </summary>
        public override void SetupInputDisplayMemorySubs()
        {
            int generation = System.Threading.Interlocked.Increment(ref padGeneration);
            uint pad = ReadPadPointer();
            if (pad != 0)
            {
                SubscribePad(pad);
                return;
            }

            System.Threading.Thread worker = new System.Threading.Thread(() =>
            {
                while (pad == 0)
                {
                    System.Threading.Thread.Sleep(PadRetryMs);
                    if (generation != padGeneration)
                    {
                        return;
                    }
                    pad = ReadPadPointer();
                }
                SubscribePad(pad);
            });
            worker.IsBackground = true;
            worker.Name = "Sly 4 pad lookup";
            worker.Start();
        }

        private uint ReadPadPointer()
        {
            try
            {
                return ReadUInt(addr.padPtr);
            }
            catch
            {
                // Not readable yet.
                return 0;
            }
        }

        private void SubscribePad(uint pad)
        {
            api.SubMemory(pid, pad + addr.padButtonsOffset, 4, (value) =>
            {
                // The word is 00, Select..Left, 00, L2..Square, which is the standard layout's two
                // bytes with a gap between them.
                int buttons = BitConverter.ToInt32(value, 0);
                Inputs.RawInputs = (buttons & 0xFF) | ((buttons >> 8) & 0xFF00);
            });

            // Each stick is X then Y as big-endian floats. The callback gets the 8 bytes reversed,
            // so Y comes first.
            api.SubMemory(pid, pad + addr.padAnalogLeftOffset, 8, (value) =>
            {
                Inputs.ly = BitConverter.ToSingle(value, 0);
                Inputs.lx = BitConverter.ToSingle(value, 4);
            });
            api.SubMemory(pid, pad + addr.padAnalogRightOffset, 8, (value) =>
            {
                Inputs.ry = BitConverter.ToSingle(value, 0);
                Inputs.rx = BitConverter.ToSingle(value, 4);
            });
        }

        public bool IsPlayerLoaded()
        {
            return GetRigidBody() != 0;
        }

        public int GetCoinCount()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            byte[] bytes = api.ReadMemory(pid, gameManager + addr.coinCountOffset, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }

        public void SetCoinCount(int coins)
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            api.WriteMemory(pid, gameManager + addr.coinCountOffset, ConvertIntToBytes(coins));
        }

        /// <summary>
        /// The player's X, Y and Z (Y is up), or null when the player isn't loaded. Read from the
        /// Havok body, which Teleport writes: the controller's copy sits lower, so teleporting the
        /// body to it would sink the player into the ground.
        /// </summary>
        public float[] GetPosition()
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                return null;
            }
            byte[] bytes = api.ReadMemory(pid, body + addr.bodyPositionOffset, 12);
            return new float[] { ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8) };
        }

        /// <summary>
        /// The player's position and velocity together, following the pointer chain once rather
        /// than once for each. False when the player isn't loaded.
        /// </summary>
        public bool ReadMotion(out float[] position, out float[] velocity)
        {
            position = null;
            velocity = null;

            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return false;
            }
            uint characterBody = ReadUInt(controller + addr.characterBodyOffset);
            uint body = characterBody == 0 ? 0 : ReadUInt(characterBody + addr.rigidBodyOffset);
            if (body == 0)
            {
                return false;
            }

            byte[] positionBytes = api.ReadMemory(pid, body + addr.bodyPositionOffset, 12);
            byte[] velocityBytes = api.ReadMemory(pid, controller + addr.controllerVelocityOffset, 12);
            position = new float[] { ToFloat(positionBytes, 0), ToFloat(positionBytes, 4), ToFloat(positionBytes, 8) };
            velocity = new float[] { ToFloat(velocityBytes, 0), ToFloat(velocityBytes, 4), ToFloat(velocityBytes, 8) };
            return true;
        }

        /// <summary>
        /// The Player tab's Position Editor. The position is the Havok body's (Y is up), moved
        /// with Teleport; the velocity is the character controller's. Sly 4's fly is
        /// Sly4FreeMove's, so the editor gets its speed to show and none of its own fly.
        /// </summary>
        public PositionEditorLayout GetPositionEditorLayout(Sly4FreeMove freeMove)
        {
            return new PositionEditorLayout
            {
                gameName = "Sly 4",
                warpFilePrefix = "sly4",
                readEntity = GetRigidBody,
                readMapIndicator = CurrentPositionMap,
                transformInEntity = true,
                positionOffset = addr.bodyPositionOffset,
                writePosition = Teleport,
                readVelocity = GetVelocity,
                heightAxis = 1,
                isLoading = IsLoading,
                getFlySpeed = () => freeMove.FlySpeed,
                setFlySpeed = (speed) => freeMove.FlySpeed = speed,
                hideEntityFields = true,
            };
        }

        /// <summary>
        /// The Camera tab, for Sly 4 and the Hackpack. Paths are saved under CameraPathLevelKey
        /// and shown with the level's name; the free camera has a far distance and a script API.
        /// </summary>
        public CameraTabLayout GetCameraTabLayout(Sly4FreeMove freeMove, Sly4CameraApi cameraApi)
        {
            return new CameraTabLayout
            {
                freeCamera = new Sly4FreeCamera(freeMove),
                pathFolder = CameraPathFolder,
                readPlace = (out string displayName) =>
                {
                    uint levelHash = GetLevelHash();
                    displayName = levelHash == 0 ? "" : GetLevelName(levelHash);
                    return levelHash == 0 ? "" : CameraPathLevelKey(levelHash);
                },
                isHudHidden = IsHudHidden,
                setHudHidden = SetHudHidden,
                hudToolTip = "Hides the HUD (sets the UI.3D.FOV tunable to 0). The pause menu is hidden too while it's on.",
                setFogDisabled = SetFogDisabled,
                fogToolTip = "Moves the level's fog out of sight, and keeps it there after loads. Untick to put it back.",
                playerHelp = "The player is held where\nthey stood; in hacks, time stands still. Loads turn it off.",
                characterControlToolTip =
                    "The free camera stays where it is, or keeps playing its path, and the controller\n" +
                    "moves the character as usual (in hacks, the minigame runs again). Untick to fly\n" +
                    "the camera again.",
                minMoveSpeed = 1,
                maxMoveSpeed = 200,
                moveSpeedIncrement = 1,
                minFov = Sly4FreeMove.MinFov,
                maxFov = Sly4FreeMove.MaxFov,
                fovDecimals = 4,
                startFovDegrees = 53.5f,
                fovToolTip = "The free camera's vertical field of view. R2 zooms in, L2 out.",
                upAxis = "Y",
                getFarDistance = () => freeMove.FarDistance,
                setFarDistance = (far) => freeMove.FarDistance = far,
                getGameFar = () => freeMove.GameFar,
                scriptApi = cameraApi,
            };
        }

        /// <summary>The character controller's velocity (Y is up), or null when the player isn't loaded.</summary>
        public float[] GetVelocity()
        {
            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return null;
            }
            byte[] bytes = api.ReadMemory(pid, controller + addr.controllerVelocityOffset, 12);
            return new float[] { ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8) };
        }

        /// <summary>
        /// Moves the player by writing the Havok body's motion state. The player's own position
        /// copies are refreshed from it every frame, so writing those does nothing.
        /// </summary>
        public void Teleport(float x, float y, float z)
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                throw new InvalidOperationException("The player isn't loaded.");
            }

            // The position, then the centre of mass at the start and end of the step. Havok moves
            // the body from the last two, so all three move by the same amount in one write. Their
            // W values hold the step time, so they stay as read.
            uint address = body + addr.bodyPositionOffset;
            byte[] state = api.ReadMemory(pid, address, 0x30);
            float[] delta = new float[]
            {
                x - ToFloat(state, 0),
                y - ToFloat(state, 4),
                z - ToFloat(state, 8),
            };

            for (int vector = 0; vector < 3; vector++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    int offset = vector * 16 + axis * 4;
                    byte[] value = ConvertFloatToBytes(ToFloat(state, offset) + delta[axis]);
                    Array.Copy(value, 0, state, offset, 4);
                }
            }

            api.WriteMemory(pid, address, state);
        }

        /// <summary>
        /// Puts the player at a position and stops them, for fly mode: the body is moved like
        /// Teleport does and the controller's velocity is zeroed, so gravity hasn't built up by the
        /// next frame. False when the player isn't loaded.
        /// </summary>
        public bool HoldPlayer(float x, float y, float z)
        {
            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return false;
            }
            uint characterBody = ReadUInt(controller + addr.characterBodyOffset);
            uint body = characterBody == 0 ? 0 : ReadUInt(characterBody + addr.rigidBodyOffset);
            if (body == 0)
            {
                return false;
            }

            uint address = body + addr.bodyPositionOffset;
            byte[] state = api.ReadMemory(pid, address, 0x30);
            float[] delta = new float[] { x - ToFloat(state, 0), y - ToFloat(state, 4), z - ToFloat(state, 8) };
            for (int vector = 0; vector < 3; vector++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    int offset = vector * 16 + axis * 4;
                    Array.Copy(ConvertFloatToBytes(ToFloat(state, offset) + delta[axis]), 0, state, offset, 4);
                }
            }
            api.WriteMemory(pid, address, state);
            api.WriteMemory(pid, controller + addr.controllerVelocityOffset, new byte[12]);
            return true;
        }

        /// <summary>The main gameplay view, or 0 before the game has one.</summary>
        public uint GetMainView()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            return gameManager == 0 ? 0 : ReadUInt(gameManager + addr.mainViewOffset);
        }

        /// <summary>
        /// A view's camera matrix as 16 floats: the camera's X, Y and Z axes, then its position,
        /// each with a W value.
        /// </summary>
        public float[] ReadCameraMatrix(uint view)
        {
            return ReadMatrix(view + addr.viewCameraMatrixOffset);
        }

        /// <summary>
        /// The view and camera matrices for a camera, as the view override takes them: the view
        /// matrix (the camera's inverse), then the camera matrix (its X, Y and Z axes, then its
        /// position), 4 floats a row. The 4th float of each row holds something else in the
        /// game's views, so it's taken from <paramref name="viewW"/> and
        /// <paramref name="cameraW"/>.
        /// </summary>
        public static byte[] ViewMatrices(float[][] axes, float[] position, float[] viewW, float[] cameraW)
        {
            byte[] bytes = new byte[0x80];
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    float view;
                    float camera;
                    if (row < 3)
                    {
                        // The rotation's inverse is its transpose.
                        view = axes[column][row];
                        camera = axes[row][column];
                    }
                    else
                    {
                        // The inverse moves the camera's position back to the origin.
                        view = -(position[0] * axes[column][0] + position[1] * axes[column][1] + position[2] * axes[column][2]);
                        camera = position[column];
                    }
                    Array.Copy(ConvertFloatToBytes(view), 0, bytes, row * 16 + column * 4, 4);
                    Array.Copy(ConvertFloatToBytes(camera), 0, bytes, 0x40 + row * 16 + column * 4, 4);
                }
                Array.Copy(ConvertFloatToBytes(viewW[row]), 0, bytes, row * 16 + 12, 4);
                Array.Copy(ConvertFloatToBytes(cameraW[row]), 0, bytes, 0x40 + row * 16 + 12, 4);
            }
            return bytes;
        }

        /// <summary>
        /// Starts drawing the main view from the given matrices (see ViewMatrices) every frame.
        /// The game rebuilds each view's render matrices and then copies the view for drawing;
        /// that copy call is pointed at a stub which, for the main view, first copies the matrices
        /// in and rebuilds the render matrices from them. That's after the game's camera has
        /// written the view, so it can't overwrite them, and before anything is drawn.
        /// </summary>
        public void StartViewOverride(uint view, byte[] matrices, float fov, float far)
        {
            uint call = addr.viewUpdateCopyCall;
            uint original = Branch(call, addr.viewCopyFunction, true);
            uint hooked = Branch(call, addr.viewOverrideStub, true);
            uint instruction = ReadUInt(call);
            if (instruction != original && instruction != hooked)
            {
                throw new InvalidOperationException("The game's camera code isn't what SluMAN expects, so the free camera isn't available on this version.");
            }

            // Off while the stub is written, then on once the matrices are in.
            byte[] header = new byte[0x10];
            Array.Copy(BitConverter.GetBytes(view).Reverse().ToArray(), 0, header, 4, 4);
            api.WriteMemory(pid, addr.viewOverrideData, header);
            SetViewOverride(matrices, fov, far);

            // To put back if the game doesn't draw its own camera again by itself (see
            // StopViewOverride).
            overrideView = view;
            viewBeforeOverride = api.ReadMemory(pid, view + addr.viewMatrixOffset, 0x80);
            fovBeforeOverride = api.ReadMemory(pid, view + addr.viewFovOffset, 4);
            farBeforeOverride = api.ReadMemory(pid, view + addr.viewFarOffset, 4);

            byte[] stub = ViewOverrideStub();
            api.WriteMemory(pid, addr.viewOverrideStub, stub);
            if (!api.ReadMemory(pid, addr.viewOverrideStub, (uint)stub.Length).SequenceEqual(stub))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            api.WriteMemory(pid, call, BitConverter.GetBytes(hooked).Reverse().ToArray());
            StartHackPreRenderHook();
            StartDrawListGuard();
            api.WriteMemory(pid, addr.viewOverrideData, BitConverter.GetBytes(1u).Reverse().ToArray());
        }

        /// <summary>
        /// Points the draw lists' push functions at stubs that drop an item when its list is full
        /// (see Sly4Addresses), so a free camera that sees more than the game's cameras ever do
        /// leaves some things undrawn rather than crashing the game. Each push whose code isn't
        /// what this version should have is left as it is.
        /// </summary>
        private void StartDrawListGuard()
        {
            for (int i = 0; i < addr.drawListPushDescriptors.Length; i++)
            {
                uint descriptor = addr.drawListPushDescriptors[i];
                uint push = addr.drawListPushFunctions[i];
                uint guard = addr.drawListGuardStubs + (uint)i * Sly4Addresses.DrawListGuardStubSize;
                uint code = ReadUInt(descriptor);
                if (code != push && code != guard)
                {
                    continue;
                }

                byte[] stub = DrawListGuardStub(guard, push);
                byte[] written = api.ReadMemory(pid, guard, (uint)stub.Length);
                if (!written.SequenceEqual(stub))
                {
                    // Only into unused memory, never rewritten in place (see StartHackPreRenderHook).
                    if (code == guard || written.Any(b => b != 0))
                    {
                        continue;
                    }
                    api.WriteMemory(pid, guard, stub);
                    if (!api.ReadMemory(pid, guard, (uint)stub.Length).SequenceEqual(stub))
                    {
                        continue;
                    }
                }
                api.WriteMemory(pid, descriptor, BitConverter.GetBytes(guard).Reverse().ToArray());
            }
        }

        private void StopDrawListGuard()
        {
            for (int i = 0; i < addr.drawListPushDescriptors.Length; i++)
            {
                uint descriptor = addr.drawListPushDescriptors[i];
                uint guard = addr.drawListGuardStubs + (uint)i * Sly4Addresses.DrawListGuardStubSize;
                if (ReadUInt(descriptor) == guard)
                {
                    api.WriteMemory(pid, descriptor, BitConverter.GetBytes(addr.drawListPushFunctions[i]).Reverse().ToArray());
                }
            }
        }

        /// <summary>
        /// A draw list push's guard, called with the list in r3 and the item in r4: returns without
        /// adding anything when the list holds capacity - 1 entries (capacity for item 0, the
        /// padding the game adds), otherwise continues into the push. r11 and r12 are free at a
        /// call, and the pushes set them before reading them.
        /// </summary>
        private byte[] DrawListGuardStub(uint stub, uint push)
        {
            uint[] words =
            {
                0x81630004,             //  0 lwz r11, 4(r3) (begin)
                0x81830008,             //  1 lwz r12, 8(r3) (end)
                0x7D8B6050,             //  2 subf r12, r11, r12
                0x558CE8FE,             //  3 srwi r12, r12, 3 (entries)
                0x81630000 | addr.drawListCapacityOffset, // 4 lwz r11, capacity(r3)
                0x2C040000,             //  5 cmpwi r4, 0
                0x41820008,             //  6 beq +8 (padding: may fill the last entry)
                0x396BFFFF,             //  7 addi r11, r11, -1
                0x7C0C5840,             //  8 cmplw r12, r11
                0x4C800020,             //  9 bgelr (full: drop it)
                Branch(stub + 10 * 4, push, false), // 10 b push
            };
            byte[] code = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(words[i]).Reverse().ToArray(), 0, code, i * 4, 4);
            }
            return code;
        }

        /// <summary>
        /// Points the hacking minigames' layer setup at its stub (see Sly4Addresses), so they draw
        /// from the free camera too. Leaves it be when the code isn't what this version should have
        /// (another version, or a mod): the free camera still works outside the minigames.
        /// </summary>
        private void StartHackPreRenderHook()
        {
            uint call = addr.hackPreRenderCall;
            uint original = Branch(call, addr.hackPreRenderFunction, true);
            uint hooked = Branch(call, addr.hackPreRenderStub, true);
            uint instruction = ReadUInt(call);
            if (instruction != original && instruction != hooked)
            {
                return;
            }

            byte[] stub = HackPreRenderStub();
            byte[] written = api.ReadMemory(pid, addr.hackPreRenderStub, (uint)stub.Length);
            if (!written.SequenceEqual(stub))
            {
                // Only into unused memory, and never rewritten in place: RPCS3 keeps running code
                // it has run as it was decoded then.
                if (instruction == hooked || written.Any(b => b != 0))
                {
                    return;
                }
                api.WriteMemory(pid, addr.hackPreRenderStub, stub);
                if (!api.ReadMemory(pid, addr.hackPreRenderStub, (uint)stub.Length).SequenceEqual(stub))
                {
                    return;
                }
            }
            api.WriteMemory(pid, call, BitConverter.GetBytes(hooked).Reverse().ToArray());
        }

        /// <summary>
        /// The matrices (see ViewMatrices) and vertical field of view (radians; 0 keeps the
        /// game's) the main view is drawn with, from the next frame. One write, so a frame never
        /// mixes old and new.
        /// </summary>
        public void SetViewOverride(byte[] matrices, float fov, float far)
        {
            byte[] data = new byte[0x88];
            Array.Copy(matrices, 0, data, 0, 0x80);
            Array.Copy(ConvertFloatToBytes(fov), 0, data, 0x80, 4);
            Array.Copy(ConvertFloatToBytes(far), 0, data, 0x84, 4);
            api.WriteMemory(pid, addr.viewOverrideData + 0x10, data);
            lastOverride = data;
        }

        // The main view the free camera draws, its matrices, field of view and far distance before
        // it started, and what the free camera last drew with (as SetViewOverride writes it).
        private uint overrideView;
        private byte[] viewBeforeOverride;
        private byte[] fovBeforeOverride;
        private byte[] farBeforeOverride;
        private byte[] lastOverride;

        // How long StopViewOverride gives the game to draw its own camera again.
        private const int ViewRestoreWaitMs = 150;

        // No fog pushes the fog's start and end this far away.
        private const float NoFogDistance = 2147483648f;
        // The fog distances No fog replaced, by view and block, to put back.
        private readonly Dictionary<uint, float[]> fogBeforeNoFog = new Dictionary<uint, float[]>();

        /// <summary>
        /// Turns the main view's fog off by moving both fog blocks' start and end distances far
        /// away, or puts back what they were. A level load sets them again, so call it again
        /// after one; it only writes when a block isn't already moved. Returns whether the view
        /// was there.
        /// </summary>
        public bool SetFogDisabled(bool disabled)
        {
            uint view = GetMainView();
            if (view == 0)
            {
                return false;
            }
            foreach (uint offset in addr.viewFogDistanceOffsets)
            {
                uint address = view + offset;
                byte[] bytes = api.ReadMemory(pid, address, 8);
                float start = ToFloat(bytes, 0);
                float end = ToFloat(bytes, 4);
                bool moved = end == NoFogDistance;
                if (disabled && !moved)
                {
                    fogBeforeNoFog[address] = new float[] { start, end };
                    byte[] far = new byte[8];
                    Array.Copy(ConvertFloatToBytes(NoFogDistance), 0, far, 0, 4);
                    Array.Copy(ConvertFloatToBytes(NoFogDistance), 0, far, 4, 4);
                    api.WriteMemory(pid, address, far);
                }
                else if (!disabled && moved)
                {
                    float[] before;
                    if (fogBeforeNoFog.TryGetValue(address, out before))
                    {
                        byte[] original = new byte[8];
                        Array.Copy(ConvertFloatToBytes(before[0]), 0, original, 0, 4);
                        Array.Copy(ConvertFloatToBytes(before[1]), 0, original, 4, 4);
                        api.WriteMemory(pid, address, original);
                    }
                }
            }
            if (!disabled)
            {
                fogBeforeNoFog.Clear();
            }
            return true;
        }

        /// <summary>A view's far clipping distance.</summary>
        public float ReadViewFar(uint view)
        {
            return ToFloat(api.ReadMemory(pid, view + addr.viewFarOffset, 4), 0);
        }

        /// <summary>A view's vertical field of view, in radians.</summary>
        public float ReadViewFov(uint view)
        {
            return ToFloat(api.ReadMemory(pid, view + addr.viewFovOffset, 4), 0);
        }

        /// <summary>
        /// Lets the game draw its own camera again, from the next frame. Most cameras write the
        /// view every frame; when the game hasn't a moment later (a camera that only writes it as
        /// it moves, or the game paused), the view is put back as it was before the free camera,
        /// so it doesn't stay where the free camera left it.
        /// </summary>
        public void StopViewOverride()
        {
            api.WriteMemory(pid, addr.viewOverrideData, new byte[4]);
            uint call = addr.viewUpdateCopyCall;
            if (ReadUInt(call) == Branch(call, addr.viewOverrideStub, true))
            {
                api.WriteMemory(pid, call, BitConverter.GetBytes(Branch(call, addr.viewCopyFunction, true)).Reverse().ToArray());
            }
            uint hackCall = addr.hackPreRenderCall;
            if (ReadUInt(hackCall) == Branch(hackCall, addr.hackPreRenderStub, true))
            {
                api.WriteMemory(pid, hackCall, BitConverter.GetBytes(Branch(hackCall, addr.hackPreRenderFunction, true)).Reverse().ToArray());
            }
            StopDrawListGuard();
            RestoreViewAfterOverride();
        }

        private void RestoreViewAfterOverride()
        {
            uint view = overrideView;
            byte[] matrices = viewBeforeOverride;
            byte[] fov = fovBeforeOverride;
            byte[] far = farBeforeOverride;
            byte[] last = lastOverride;
            overrideView = 0;
            viewBeforeOverride = null;
            fovBeforeOverride = null;
            farBeforeOverride = null;
            lastOverride = null;
            if (view == 0 || last == null)
            {
                return;
            }

            // Not while a load replaces the views.
            if (IsLoading() || GetMainView() != view)
            {
                return;
            }
            System.Threading.Thread.Sleep(ViewRestoreWaitMs);
            if (IsLoading() || GetMainView() != view)
            {
                return;
            }

            bool restored = false;
            if (api.ReadMemory(pid, view + addr.viewMatrixOffset, 0x80).SequenceEqual(last.Take(0x80)))
            {
                api.WriteMemory(pid, view + addr.viewMatrixOffset, matrices);
                restored = true;
            }
            // The stub writes the field of view and far distance only when they aren't 0.
            byte[] lastFov = last.Skip(0x80).Take(4).ToArray();
            if (lastFov.Any(b => b != 0) && api.ReadMemory(pid, view + addr.viewFovOffset, 4).SequenceEqual(lastFov))
            {
                api.WriteMemory(pid, view + addr.viewFovOffset, fov);
                restored = true;
            }
            byte[] lastFar = last.Skip(0x84).Take(4).ToArray();
            if (lastFar.Any(b => b != 0) && api.ReadMemory(pid, view + addr.viewFarOffset, 4).SequenceEqual(lastFar))
            {
                api.WriteMemory(pid, view + addr.viewFarOffset, far);
                restored = true;
            }
            if (restored)
            {
                // Has the game rebuild the view's render matrices from them.
                uint flags = view + addr.viewFlagsOffset;
                api.WriteMemory(pid, flags, new byte[] { (byte)(ReadByte(flags) | 0x80) });
            }
        }

        /// <summary>
        /// The view override's stub, called instead of the view copy with the view in r3. Data:
        /// +0x0 on, +0x4 the main view, +0x10 its view matrix then its camera matrix (0x80 bytes,
        /// the view's +0x2F0 to +0x36F), +0x90 its field of view and +0x94 its far distance (0
        /// keeps the game's).
        /// </summary>
        private byte[] ViewOverrideStub()
        {
            uint stub = addr.viewOverrideStub;
            uint data = addr.viewOverrideData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;

            uint[] words =
            {
                0xF821FF81,             //  0 stdu r1, -0x80(r1)
                0x7C0802A6,             //  1 mflr r0
                0xF8010090,             //  2 std r0, 0x90(r1)
                0xF8610070,             //  3 std r3, 0x70(r1)
                0x3D600000 | high,      //  4 lis r11, data@ha
                0x396B0000 | low,       //  5 addi r11, r11, data@l
                0x818B0000,             //  6 lwz r12, 0(r11)
                0x2C0C0000,             //  7 cmpwi r12, 0
                0x41820054,             //  8 beq +0x54 (to 29: off)
                0x818B0004,             //  9 lwz r12, 4(r11)
                0x7C036000,             // 10 cmpw r3, r12
                0x40820048,             // 11 bne +0x48 (to 29: another view)
                0x38C00020,             // 12 li r6, 32 (words)
                0x7CC903A6,             // 13 mtctr r6
                0x38A302EC,             // 14 addi r5, r3, view matrix - 4
                0x388B000C,             // 15 addi r4, r11, matrices - 4
                0x84E40004,             // 16 lwzu r7, 4(r4)
                0x94E50004,             // 17 stwu r7, 4(r5)
                0x4200FFF8,             // 18 bdnz -8
                0x80EB0090,             // 19 lwz r7, 0x90(r11) (field of view)
                0x2C070000,             // 20 cmpwi r7, 0
                0x41820008,             // 21 beq +8 (0: keep the game's)
                0x90E30000 | addr.viewFovOffset, // 22 stw r7, fov(r3)
                0x80EB0094,             // 23 lwz r7, 0x94(r11) (far distance)
                0x2C070000,             // 24 cmpwi r7, 0
                0x41820008,             // 25 beq +8 (0: keep the game's)
                0x90E30000 | addr.viewFarOffset, // 26 stw r7, far(r3)
                0x38800001,             // 27 li r4, 1 (rebuild even if not marked)
                Branch(stub + 28 * 4, addr.calcRenderMatricesFunction, true),
                0xE8610070,             // 29 ld r3, 0x70(r1)
                0xE8010090,             // 30 ld r0, 0x90(r1)
                0x7C0803A6,             // 31 mtlr r0
                0x38210080,             // 32 addi r1, r1, 0x80
                Branch(stub + 33 * 4, addr.viewCopyFunction, false),
            };
            byte[] code = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(words[i]).Reverse().ToArray(), 0, code, i * 4, 4);
            }
            return code;
        }

        /// <summary>
        /// The hacking minigames' stub, called instead of their layer setup with the mechanic in r3
        /// and the main view in r4 (see Sly4Addresses). While the view override is on, it first
        /// puts the override's matrices, field of view and far distance into the main view (the
        /// override stub does it again later, for the main view's own copy) and turns the main
        /// view's depth of field off; after the setup, it turns the bottom layer's off. The game
        /// turns both on again each frame, so nothing stays changed once the hook is gone.
        /// </summary>
        private byte[] HackPreRenderStub()
        {
            uint stub = addr.hackPreRenderStub;
            uint data = addr.viewOverrideData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint effects = addr.viewEffectsOffset;
            uint dof = addr.effectDofOffset;
            uint enabled = addr.effectEnabledOffset;

            uint[] words =
            {
                0xF821FF81,             //  0 stdu r1, -0x80(r1)
                0x7C0802A6,             //  1 mflr r0
                0xF8010090,             //  2 std r0, 0x90(r1)
                0xF8610070,             //  3 std r3, 0x70(r1)
                0x3D600000 | high,      //  4 lis r11, data@ha
                0x396B0000 | low,       //  5 addi r11, r11, data@l
                0x818B0000,             //  6 lwz r12, 0(r11)
                0x2C0C0000,             //  7 cmpwi r12, 0
                0x41820070,             //  8 beq +0x70 (to 36: off)
                0x818B0004,             //  9 lwz r12, 4(r11)
                0x7C046000,             // 10 cmpw r4, r12
                0x40820064,             // 11 bne +0x64 (to 36: another view)
                0x38C00020,             // 12 li r6, 32 (words)
                0x7CC903A6,             // 13 mtctr r6
                0x38A40000 | (addr.viewMatrixOffset - 4), // 14 addi r5, r4, view matrix - 4
                0x38EB000C,             // 15 addi r7, r11, matrices - 4
                0x85070004,             // 16 lwzu r8, 4(r7)
                0x95050004,             // 17 stwu r8, 4(r5)
                0x4200FFF8,             // 18 bdnz -8
                0x810B0090,             // 19 lwz r8, 0x90(r11) (field of view)
                0x2C080000,             // 20 cmpwi r8, 0
                0x41820008,             // 21 beq +8 (0: keep the game's)
                0x91040000 | addr.viewFovOffset, // 22 stw r8, fov(r4)
                0x810B0094,             // 23 lwz r8, 0x94(r11) (far distance)
                0x2C080000,             // 24 cmpwi r8, 0
                0x41820008,             // 25 beq +8 (0: keep the game's)
                0x91040000 | addr.viewFarOffset, // 26 stw r8, far(r4)
                0x80A40000 | effects,   // 27 lwz r5, effects(r4)
                0x2C050000,             // 28 cmpwi r5, 0
                0x4182001C,             // 29 beq +0x1C (to 36)
                0x80A50000 | dof,       // 30 lwz r5, dof(r5)
                0x2C050000,             // 31 cmpwi r5, 0
                0x41820010,             // 32 beq +0x10 (to 36)
                0x38C00000,             // 33 li r6, 0
                0x98C50000 | enabled,   // 34 stb r6, enabled(r5)
                0xE8610070,             // 35 ld r3, 0x70(r1)
                Branch(stub + 36 * 4, addr.hackPreRenderFunction, true), // 36 bl layer setup
                0x3D600000 | high,      // 37 lis r11, data@ha
                0x396B0000 | low,       // 38 addi r11, r11, data@l
                0x818B0000,             // 39 lwz r12, 0(r11)
                0x2C0C0000,             // 40 cmpwi r12, 0
                0x41820034,             // 41 beq +0x34 (to 54: off)
                0xE8610070,             // 42 ld r3, 0x70(r1)
                0x80A30000 | addr.hackBottomLayerOffset, // 43 lwz r5, bottom layer(r3)
                0x2C050000,             // 44 cmpwi r5, 0
                0x41820024,             // 45 beq +0x24 (to 54)
                0x80A50000 | effects,   // 46 lwz r5, effects(r5)
                0x2C050000,             // 47 cmpwi r5, 0
                0x41820018,             // 48 beq +0x18 (to 54)
                0x80A50000 | dof,       // 49 lwz r5, dof(r5)
                0x2C050000,             // 50 cmpwi r5, 0
                0x4182000C,             // 51 beq +0xC (to 54)
                0x38C00000,             // 52 li r6, 0
                0x98C50000 | enabled,   // 53 stb r6, enabled(r5)
                0xE8010090,             // 54 ld r0, 0x90(r1)
                0x7C0803A6,             // 55 mtlr r0
                0x38210080,             // 56 addi r1, r1, 0x80
                0x4E800020,             // 57 blr
            };
            byte[] code = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(words[i]).Reverse().ToArray(), 0, code, i * 4, 4);
            }
            return code;
        }

        private static uint Branch(uint from, uint to, bool link)
        {
            return 0x48000000 | ((to - from) & 0x3FFFFFC) | (link ? 1u : 0u);
        }

        /// <summary>A camera matrix at an address, laid out as ReadCameraMatrix's.</summary>
        public float[] ReadMatrix(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 0x40);
            float[] matrix = new float[16];
            for (int i = 0; i < 16; i++)
            {
                matrix[i] = ToFloat(bytes, i * 4);
            }
            return matrix;
        }

        public void SetSlowMotion(bool enabled)
        {
            api.WriteMemory(pid, addr.clockClampFreqMax, ConvertIntToBytes(enabled ? SlowMotionClampFreqMax : NormalClampFreqMax));
            api.WriteMemory(pid, addr.clockClampFreqMin, ConvertIntToBytes(enabled ? SlowMotionClampFreqMin : NormalClampFreqMin));
        }

        public bool IsSlowMotionOn()
        {
            byte[] bytes = api.ReadMemory(pid, addr.clockClampFreqMax, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0) == SlowMotionClampFreqMax;
        }

        // Frozen time: every frame's time step is clamped to 1 / this many seconds, so the game
        // runs about 1,700 times slower than at 60 frames a second and still draws every frame.
        private const int FrozenClampFreq = 100000;
        // The clock's ClampFreqMax and ClampFreqMin before SetTimeFrozen froze it, or null.
        private byte[] clockBeforeFreeze;

        /// <summary>
        /// Holds the game's time nearly still (or lets it run again) through the clock's frame
        /// time clamps, like Slow Motion. For the free camera in the hacking minigames, where the
        /// sticks also drive the minigame's ship, ball or avatar and there's no character to hold.
        /// Unfreezing puts back the clamps it found, unless something (Slow Motion) has set them
        /// since.
        /// </summary>
        public void SetTimeFrozen(bool frozen)
        {
            if (frozen)
            {
                if (clockBeforeFreeze != null)
                {
                    return;
                }
                byte[] max = api.ReadMemory(pid, addr.clockClampFreqMax, 4);
                byte[] min = api.ReadMemory(pid, addr.clockClampFreqMin, 4);
                // Left frozen by a SluMAN that went away: run normally afterwards.
                if (ToInt(max) == FrozenClampFreq)
                {
                    max = ConvertIntToBytes(NormalClampFreqMax);
                    min = ConvertIntToBytes(NormalClampFreqMin);
                }
                clockBeforeFreeze = max.Concat(min).ToArray();
                api.WriteMemory(pid, addr.clockClampFreqMax, ConvertIntToBytes(FrozenClampFreq));
                api.WriteMemory(pid, addr.clockClampFreqMin, ConvertIntToBytes(FrozenClampFreq));
            }
            else
            {
                byte[] before = clockBeforeFreeze;
                clockBeforeFreeze = null;
                if (before == null)
                {
                    return;
                }
                if (ToInt(api.ReadMemory(pid, addr.clockClampFreqMax, 4)) == FrozenClampFreq
                    && ToInt(api.ReadMemory(pid, addr.clockClampFreqMin, 4)) == FrozenClampFreq)
                {
                    api.WriteMemory(pid, addr.clockClampFreqMax, before.Take(4).ToArray());
                    api.WriteMemory(pid, addr.clockClampFreqMin, before.Skip(4).ToArray());
                }
            }
        }

        private static int ToInt(byte[] bigEndian)
        {
            return BitConverter.ToInt32(bigEndian.Reverse().ToArray(), 0);
        }

        public void SetInvulnerable(bool enabled)
        {
            api.WriteMemory(pid, addr.cheatInvulnerable, new byte[] { (byte)(enabled ? 1 : 0) });
        }

        public bool IsInvulnerable()
        {
            return ReadByte(addr.cheatInvulnerable) != 0;
        }

        public void SetUndetectable(bool enabled)
        {
            api.WriteMemory(pid, addr.cheatUndetectable, new byte[] { (byte)(enabled ? 1 : 0) });
        }

        public bool IsUndetectable()
        {
            return ReadByte(addr.cheatUndetectable) != 0;
        }

        /// <summary>
        /// Turns cutscenes and dialogue off by clearing Game.CutSceneEnable. A few spots softlock
        /// without them (e.g. looting, some minigames and boss fights), so it's for practice only.
        /// </summary>
        public void SetSkipCutscenes(bool enabled)
        {
            api.WriteMemory(pid, addr.cutSceneEnable, new byte[] { (byte)(enabled ? 0 : 1) });
        }

        public bool IsSkippingCutscenes()
        {
            return ReadByte(addr.cutSceneEnable) == 0;
        }

        /// <summary>
        /// The game's name hash: each character uppercased, then h = h * 31 + c. Level, episode
        /// and job hashes are all this, e.g. "Paris Hideout" or "Japan_PhotoOp".
        /// </summary>
        public static uint Hash(string name)
        {
            uint hash = 0;
            foreach (char c in name)
            {
                hash = hash * 31 + char.ToUpperInvariant(c);
            }
            return hash;
        }

        /// <summary>The current level's hash, or 0 before a level has loaded.</summary>
        public uint GetLevelHash()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                return 0;
            }
            return ReadUInt(gameManager + addr.currentLevelHashOffset);
        }

        /// <summary>
        /// Whether a level is loading. The loading level hash is set as soon as a load is asked
        /// for, before the load phase changes, so both are checked.
        /// </summary>
        public bool IsLoading()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                return false;
            }
            return ReadByte(gameManager + addr.loadPhaseOffset) != 0
                || ReadUInt(gameManager + addr.loadingLevelHashOffset) != 0;
        }

        /// <summary>
        /// Whether a level is one of the hacking minigames (System Cracker, Spark Runner, Alter
        /// Ego). Each is a level of its own; "System Cracker01" is the Paris Sewers. Every
        /// Hackpack level is one but the hub (FrontEnd).
        /// </summary>
        public static bool IsHackLevel(uint levelHash)
        {
            if (addr.IsHackpack)
            {
                return levelHash != MainMenuLevelHash;
            }
            string name;
            if (!levelNames.TryGetValue(levelHash, out name))
            {
                return false;
            }
            foreach (string prefix in HackLevelPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInHack()
        {
            return IsHackLevel(GetLevelHash());
        }

        /// <summary>The level's name if it's known, otherwise its hash in hex.</summary>
        public static string GetLevelName(uint levelHash)
        {
            string name;
            if (addr.IsHackpack)
            {
                name = HackpackLevels.GetName(levelHash);
                return name == null ? levelHash.ToString("X8") : name;
            }
            if (levelNames.TryGetValue(levelHash, out name))
            {
                return name;
            }
            return levelHash.ToString("X8");
        }

        /// <summary>Every named level but the main menu, in the order of the level table.</summary>
        public static List<LevelEntry> GetWarpLevels()
        {
            List<LevelEntry> levels = new List<LevelEntry>();
            foreach (KeyValuePair<uint, string> level in levelNames)
            {
                if (level.Key != MainMenuLevelHash)
                {
                    levels.Add(new LevelEntry { hash = level.Key, name = level.Value });
                }
            }
            return levels;
        }

        /// <summary>The folder this game's camera paths are saved in.</summary>
        public static string CameraPathFolder => addr.IsHackpack ? CameraPath.HackpackFolder : CameraPath.Sly4Folder;

        /// <summary>
        /// The level's part of its saved camera paths' file names: its name, or for the Hackpack,
        /// whose level names aren't all known yet, its hash, so paths stay put once it's named.
        /// </summary>
        public static string CameraPathLevelKey(uint levelHash)
        {
            return addr.IsHackpack ? levelHash.ToString("X8") : GetLevelName(levelHash);
        }

        /// <summary>
        /// The spawn points known for a level, with readable titles. Empty for levels without any.
        /// </summary>
        public static List<SpawnPointEntry> GetSpawnPoints(uint levelHash)
        {
            List<SpawnPointEntry> spawnPoints = new List<SpawnPointEntry>();
            string[] names;
            if (Sly4SpawnPoints.ByLevel.TryGetValue(levelHash, out names))
            {
                foreach (string name in names)
                {
                    spawnPoints.Add(new SpawnPointEntry { name = name, title = SpawnPointTitle(name) });
                }
            }
            return spawnPoints;
        }

        /// <summary>
        /// A spawn point's name without its prefix, as words: "Spawn_Player_5LairDoor1" is
        /// "5 Lair Door 1" and "Spawn_GeishaTower01" is "Geisha Tower 1". The game has no
        /// display names for them.
        /// </summary>
        private static string SpawnPointTitle(string name)
        {
            string title = name;
            foreach (string prefix in SpawnPointPrefixes)
            {
                if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    title = title.Substring(prefix.Length);
                    break;
                }
            }
            title = Regex.Replace(title.Replace('_', ' '), @"(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Za-z])", " ");
            // "01" reads as "1".
            title = Regex.Replace(title, @"\b0+(?=[0-9])", "");
            return title;
        }

        /// <summary>The current episode's title, or null when there is none.</summary>
        public string GetEpisodeName()
        {
            string key = ReadEntryName(addr.currentEpisodeOffset);
            return key == null ? null : Title(key);
        }

        /// <summary>The current job's title, or null outside jobs.</summary>
        public string GetJobName()
        {
            string key = ReadEntryName(addr.currentJobOffset);
            return key == null ? null : Title(key);
        }

        /// <summary>
        /// The English title for an episode or job name key ("Japan" is "Turning Japanese"), or
        /// the key with spaces ("AlterEgo01" as "Alter Ego 01") when there isn't one.
        /// </summary>
        public static string Title(string key)
        {
            string title;
            if (titles.TryGetValue(key, out title))
            {
                return title;
            }
            StringBuilder spaced = new StringBuilder(key.Length + 8);
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (i > 0)
                {
                    char previous = key[i - 1];
                    bool wordStart = char.IsUpper(c) && (char.IsLower(previous) || char.IsDigit(previous));
                    bool numberStart = char.IsDigit(c) && char.IsLetter(previous);
                    if (wordStart || numberStart)
                    {
                        spaced.Append(' ');
                    }
                }
                spaced.Append(c);
            }
            return spaced.ToString();
        }

        /// <summary>
        /// The current checkpoint (1-based, the first goal not done yet), its name and the job's
        /// goal count. The numbers are 0 and the name null outside jobs.
        /// </summary>
        public void GetCheckpoint(out int checkpoint, out int goalCount, out string goalName)
        {
            checkpoint = 0;
            goalName = null;

            TakeGoalSnapshot();
            goalCount = snapshotGoals.Count;
            uint current = 0;
            for (int i = 0; i < snapshotGoals.Count; i++)
            {
                if (snapshotGoals[i] == 0)
                {
                    continue;
                }
                checkpoint = i + 1;
                current = snapshotGoals[i];
                if (snapshotStates[i] != Sly4Addresses.GoalStateDone)
                {
                    break;
                }
            }

            if (current != 0)
            {
                if (current != snapshotNameGoal)
                {
                    snapshotName = ReadString(current + addr.goalNameOffset);
                    snapshotNameGoal = current;
                }
                goalName = snapshotName;
            }
        }

        /// <summary>
        /// The running job's goals and their states: from the goal watcher while it runs, otherwise
        /// read at most every GoalSnapshotMaxAge. For display only: warps read the goals fresh.
        /// </summary>
        private void TakeGoalSnapshot()
        {
            Sly4GoalWatcher watcher = goalWatcher;
            if (watcher != null)
            {
                List<uint> watchedGoals;
                List<int> watchedStates;
                int current;
                watcher.GetState(out watchedGoals, out watchedStates, out current);
                snapshotGoals = watchedGoals;
                snapshotStates = watchedStates;
                return;
            }

            if (DateTime.Now - snapshotTakenAt < GoalSnapshotMaxAge)
            {
                return;
            }

            List<uint> goals = GetGoals();
            List<int> states = new List<int>();
            foreach (uint goal in goals)
            {
                states.Add(goal == 0 ? -1 : ReadByte(goal + addr.goalStateOffset));
            }
            snapshotGoals = goals;
            snapshotStates = states;
            snapshotTakenAt = DateTime.Now;
        }

        /// <summary>
        /// Every job in the game, in the job array's order. The episode and job arrays are read in
        /// a few large reads rather than a couple of reads per entry.
        /// </summary>
        public List<JobEntry> GetJobs()
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }

            uint episodeArray = ReadUInt(episodeManager + addr.episodeArrayOffset);
            int episodeCount = (int)Math.Min(ReadUInt(episodeManager + addr.episodeCountOffset), 32u);
            byte[] episodeBytes = ReadLarge(episodeArray, (uint)episodeCount * Sly4Addresses.EpisodeStride);
            List<string> episodes = new List<string>();
            for (int i = 0; i < episodeCount; i++)
            {
                episodes.Add(TrimKey(ReadString(episodeBytes, i * (int)Sly4Addresses.EpisodeStride)));
            }

            uint jobArray = ReadUInt(episodeManager + addr.jobArrayOffset);
            int jobCount = (int)Math.Min(ReadUInt(episodeManager + addr.jobCountOffset), 256u);
            byte[] jobBytes = ReadLarge(jobArray, (uint)jobCount * Sly4Addresses.JobStride);
            List<JobEntry> jobs = new List<JobEntry>();
            for (int i = 0; i < jobCount; i++)
            {
                int job = i * (int)Sly4Addresses.JobStride;
                // Unused slots have no name.
                string name = TrimKey(ReadString(jobBytes, job));
                if (name == "")
                {
                    continue;
                }

                int episode = jobBytes[job + (int)addr.jobEpisodeIndexOffset];
                jobs.Add(new JobEntry
                {
                    index = i,
                    name = name,
                    episode = episode < episodes.Count ? episodes[episode] : $"Episode {episode}",
                });
            }
            return jobs;
        }

        /// <summary>The names of the current job's goals, or an empty list outside jobs.</summary>
        public List<string> GetGoalNames()
        {
            List<string> names = new List<string>();
            foreach (uint goal in GetGoals())
            {
                names.Add(goal == 0 ? "-" : ReadString(goal + addr.goalNameOffset));
            }
            return names;
        }

        /// <summary>
        /// The goal names of a job: read from the game while it runs, otherwise from the known
        /// lists in Sly4Goals. Empty when they aren't known.
        /// </summary>
        public List<string> GetGoalNames(JobEntry job)
        {
            if (IsJobRunning(job.index))
            {
                return GetGoalNames();
            }

            string[] names = Sly4Goals.ForJob(job.name);
            return names == null ? new List<string>() : new List<string>(names);
        }

        /// <summary>The running job's index in the job array, or -1 outside jobs.</summary>
        public int GetRunningJobIndex()
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                return -1;
            }
            uint currentJob = ReadUInt(episodeManager + addr.currentJobOffset);
            uint jobArray = ReadUInt(episodeManager + addr.jobArrayOffset);
            if (currentJob == 0 || currentJob < jobArray || (currentJob - jobArray) % Sly4Addresses.JobStride != 0)
            {
                return -1;
            }
            return (int)((currentJob - jobArray) / Sly4Addresses.JobStride);
        }

        /// <summary>
        /// The state of each of the running job's goals (GoalStateDone once done), -1 for an empty
        /// slot. Empty outside jobs. Up to GoalSnapshotMaxAge old.
        /// </summary>
        public List<int> GetGoalStates()
        {
            TakeGoalSnapshot();
            return new List<int>(snapshotStates);
        }

        public bool IsJobRunning(int jobIndex)
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                return false;
            }
            uint job = ReadUInt(episodeManager + addr.jobArrayOffset) + (uint)jobIndex * Sly4Addresses.JobStride;
            return ReadUInt(episodeManager + addr.currentJobOffset) == job;
        }

        /// <summary>
        /// Starts a job at its first goal, or sends the player to its episode's hub or hideout,
        /// like the debug menu's job list. The game ends a running job itself, so jobs can be
        /// switched directly. The call runs on the game's next frame (see IsGameCallPending).
        /// </summary>
        /// <remarks>
        /// StartJobByInst's third argument isn't a goal index: the game looks for the goal whose
        /// ID (goal + 0xA00) matches it, and only among goals already loaded, which a job in
        /// another level doesn't have yet. To start later, warp to the goal once the job runs,
        /// or pass the ID of one of the running job's goals (GetGoalId) to restart it there.
        /// With <paramref name="restart"/>, the running job may be started again: the 6th argument
        /// is 1, as the Hackpack's own restart (0x357684) passes it. Where the game's RequestJob is
        /// known (the Hackpack), jobs are queued through it instead, as the game's own job triggers
        /// do, so the job's goals are set up.
        /// </remarks>
        public void StartJob(int jobIndex, bool toHub, bool toHideout, uint goalId = 0, bool restart = false)
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            ThrowIfLoading();

            uint job = ReadUInt(episodeManager + addr.jobArrayOffset) + (uint)jobIndex * Sly4Addresses.JobStride;
            uint currentJob = ReadUInt(episodeManager + addr.currentJobOffset);
            if (!toHub && !toHideout && !restart && currentJob == job)
            {
                throw new InvalidOperationException("That job is already running. Warp to one of its goals instead.");
            }

            if (addr.requestJobFunction != 0 && !toHub && !toHideout)
            {
                codeHook.Call(addr.requestJobFunction, episodeManager, 0, job, goalId, 0, 0);
                return;
            }
            codeHook.Call(addr.startJobByInstFunction, episodeManager, job, goalId,
                toHub ? 1u : 0u, toHideout ? 1u : 0u, restart ? 1u : 0u, 0, 0, 1);
        }

        /// <summary>The ID of one of the running job's goals (0-based), for StartJob.</summary>
        public uint GetGoalId(int goalIndex)
        {
            List<uint> goals = GetGoals();
            if (goalIndex < 0 || goalIndex >= goals.Count || goals[goalIndex] == 0)
            {
                throw new InvalidOperationException("That goal isn't in the current job.");
            }
            return ReadUInt(goals[goalIndex] + addr.goalIdOffset);
        }

        /// <summary>The number of goals the running job has loaded, 0 outside jobs.</summary>
        public int GetGoalCount()
        {
            return GetGoals().Count;
        }

        /// <summary>Moves the player to one of the current job's goals (0-based).</summary>
        public void WarpToGoal(int goalIndex)
        {
            ThrowIfLoading();
            List<uint> goals = GetGoals();
            if (goalIndex < 0 || goalIndex >= goals.Count || goals[goalIndex] == 0)
            {
                throw new InvalidOperationException("That goal isn't in the current job.");
            }

            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            codeHook.Call(addr.teleportToGoalFunction, episodeManager, goals[goalIndex], 1, 0);
        }

        /// <summary>
        /// Loads a level by its hash, outside any job flow: the player lands at the level's
        /// default start. The game's own calls pass (game manager, hash, 0); LoadLevel only sets
        /// the level to load and the load flags, and the game loads it on a later frame. A running
        /// job sends the load back to its own level, so end it first with AbandonJob.
        /// </summary>
        /// <param name="character">
        /// The character to spawn as, or null for the level's own. Set the way door transitions
        /// keep the current character; the load reads it once and clears it.
        /// </param>
        /// <param name="spawnPoint">
        /// Where to spawn, or null for the level's default start. Set the way doors set their
        /// arrival point; a name the level doesn't have falls back to the default start.
        /// </param>
        public void LoadLevel(uint levelHash, CharacterEntry character, SpawnPointEntry spawnPoint)
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            ThrowIfLoading();

            // Always written, so an earlier choice that never got loaded can't carry over.
            byte[] name = new byte[SpawnCharacterLength];
            if (character != null)
            {
                Encoding.ASCII.GetBytes(character.name, 0, character.name.Length, name, 0);
            }
            api.WriteMemory(pid, gameManager + addr.spawnCharacterOffset, name);
            uint spawnHash = spawnPoint == null ? 0 : Hash(spawnPoint.name);
            api.WriteMemory(pid, gameManager + addr.spawnPointOffset, ConvertIntToBytes((int)spawnHash));
            codeHook.Call(addr.loadLevelFunction, gameManager, levelHash, 0);
        }

        /// <summary>True while a job runs (the episode manager's current job is set).</summary>
        public bool IsInJob()
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            return episodeManager != 0 && ReadUInt(episodeManager + addr.currentJobOffset) != 0;
        }

        /// <summary>
        /// Ends the running job with the game's AbandonCurrentJob, which loads nothing: the game
        /// clears the current job and resets its goals. Does nothing outside jobs.
        /// </summary>
        public void AbandonJob()
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            ThrowIfLoading();
            codeHook.Call(addr.abandonCurrentJobFunction, episodeManager);
        }

        // UI.3D.FOV's default, put back if the value before hiding isn't known.
        private const float DefaultUiFov = 65f;
        private static readonly TimeSpan GuiCallTimeout = TimeSpan.FromSeconds(3);
        private float uiFovBeforeHiding = DefaultUiFov;

        /// <summary>
        /// Hides or shows the HUD with the UI.3D.FOV tunable: the HUD is drawn in 3D with that
        /// field of view, so at 0 it's off screen. Menus are drawn the same way, so they're hidden
        /// too while it's on. Showing puts back the value it had.
        /// </summary>
        public void SetHudHidden(bool hidden)
        {
            float current = ToFloat(api.ReadMemory(pid, addr.uiFov, 4), 0);
            if (hidden)
            {
                if (current != 0)
                {
                    uiFovBeforeHiding = current;
                }
                api.WriteMemory(pid, addr.uiFov, ConvertFloatToBytes(0f));
            }
            else if (current == 0)
            {
                api.WriteMemory(pid, addr.uiFov, ConvertFloatToBytes(uiFovBeforeHiding));
            }
        }

        public bool IsHudHidden()
        {
            return ToFloat(api.ReadMemory(pid, addr.uiFov, 4), 0) == 0;
        }

        /// <summary>Runs a game function through the code hook and waits for what it returns.</summary>
        private uint CallAndWait(uint function, params uint[] arguments)
        {
            codeHook.Call(function, arguments);
            DateTime deadline = DateTime.Now + GuiCallTimeout;
            while (codeHook.IsCallPending())
            {
                if (DateTime.Now > deadline)
                {
                    codeHook.CancelCall();
                    throw new InvalidOperationException("The game didn't run the call. On RPCS3, set the PPU decoder to Interpreter (static).");
                }
                System.Threading.Thread.Sleep(5);
            }
            return codeHook.ReturnValue();
        }

        /// <summary>
        /// The code address of a GUI node's show method (vtable +hudShowVtableOffset, a pointer to
        /// its function descriptor), checked to be game code run with the game's TOC.
        /// </summary>
        private uint GuiShowFunction(uint node)
        {
            uint vtable = ReadUInt(node);
            uint descriptor = ReadUInt(vtable + addr.hudShowVtableOffset);
            uint code = ReadUInt(descriptor);
            if (ReadUInt(descriptor + 4) != addr.gameToc || code < 0x10000 || code >= addr.legacyHookCodeCave)
            {
                throw new InvalidOperationException("That GUI part isn't what SluMAN expects, so it wasn't touched.");
            }
            return code;
        }

        /// <summary>Shows or hides one GUI node with its show method, through the code hook.</summary>
        public void SetGuiNodeShown(uint node, bool shown)
        {
            ThrowIfLoading();
            CallAndWait(GuiShowFunction(node), node, shown ? 1u : 0u);
        }

        /// <summary>A part of the game's GUI, as read by ReadGuiTree.</summary>
        public class GuiNode
        {
            public uint address;
            public uint hash;
            /// <summary>Its name, or null if no string in the game hashes to it.</summary>
            public string name;
            public List<GuiNode> children = new List<GuiNode>();
        }

        private const int MaxGuiNodes = 20000;

        /// <summary>
        /// Reads the GUI tree: every top-level node and its descendants, named from
        /// <paramref name="names"/> (see ReadGuiNames).
        /// </summary>
        public List<GuiNode> ReadGuiTree(Dictionary<uint, string> names)
        {
            List<GuiNode> roots = new List<GuiNode>();
            HashSet<uint> seen = new HashSet<uint>();
            ReadGuiSiblings(ReadUInt(addr.guiRootList), roots, names, seen);
            return roots;
        }

        private void ReadGuiSiblings(uint node, List<GuiNode> into, Dictionary<uint, string> names, HashSet<uint> seen)
        {
            while (IsGuiNodeAddress(node) && seen.Count < MaxGuiNodes && seen.Add(node))
            {
                byte[] bytes = api.ReadMemory(pid, node, addr.guiNodeNextSiblingOffset + 4);
                GuiNode entry = new GuiNode();
                entry.address = node;
                entry.hash = BigEndianUInt(bytes, addr.guiNodeHashOffset);
                string name;
                entry.name = names.TryGetValue(entry.hash, out name) ? name : null;
                into.Add(entry);

                ReadGuiSiblings(BigEndianUInt(bytes, addr.guiNodeFirstChildOffset), entry.children, names, seen);
                node = BigEndianUInt(bytes, addr.guiNodeNextSiblingOffset);
            }
        }

        private static bool IsGuiNodeAddress(uint address)
        {
            return address >= 0x10000 && address < 0x40000000 && address % 4 == 0;
        }

        private static uint BigEndianUInt(byte[] bytes, uint offset)
        {
            return (uint)(bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]);
        }

        /// <summary>
        /// Names for GUI node hashes: every word in the game's read-only strings (dotted names
        /// split into their parts), hashed as the GUI hashes names. Read from the game's memory.
        /// </summary>
        public Dictionary<uint, string> ReadGuiNames()
        {
            Dictionary<uint, string> names = new Dictionary<uint, string>();
            const uint chunk = 0x10000;
            StringBuilder word = new StringBuilder();
            for (uint address = addr.guiNamesStart; address < addr.guiNamesEnd; address += chunk)
            {
                byte[] bytes = api.ReadMemory(pid, address, chunk);
                foreach (byte b in bytes)
                {
                    char c = (char)b;
                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')
                    {
                        word.Append(c);
                        continue;
                    }
                    AddGuiName(names, word);
                    word.Clear();
                }
            }
            AddGuiName(names, word);
            return names;
        }

        private static void AddGuiName(Dictionary<uint, string> names, StringBuilder word)
        {
            if (word.Length < 2 || word.Length > 48)
            {
                return;
            }
            string name = word.ToString();
            uint hash = Hash(name);
            if (!names.ContainsKey(hash))
            {
                names[hash] = name;
            }
        }

        /// <summary>The first bytes of a GUI node, to see what its show method changes.</summary>
        public byte[] ReadGuiNodeBytes(uint node, uint length)
        {
            return api.ReadMemory(pid, node, length);
        }

        /// <summary>A warp during a load would start a second load inside it, which the game can't take.</summary>
        private void ThrowIfLoading()
        {
            if (IsLoading())
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }
        }

        public bool IsGameCallPending()
        {
            return codeHook.IsCallPending();
        }

        public void CancelGameCall()
        {
            codeHook.CancelCall();
        }

        /// <summary>Puts the game's code back as it was. Call before disconnecting.</summary>
        public void RemoveCodeHook()
        {
            codeHook.Uninstall();
            StopViewOverride();
        }

        public bool IsJobFailed()
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                return false;
            }
            return ReadUInt(episodeManager + addr.failedGoalOffset) != 0;
        }

        /// <summary>
        /// The config key part for saved positions, so each level keeps its own slots. Throws
        /// while loading, since the level hash still names the previous level then.
        /// </summary>
        private string CurrentPositionMap()
        {
            uint levelHash = GetLevelHash();
            if (levelHash == 0 || IsLoading())
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }
            return "sly4_" + levelHash.ToString("X8");
        }

        /// <summary>
        /// The name of the episode or job entry the episode manager points to at the offset. The
        /// entry starts with a localization key such as "$JapanTitleName", shown as "Japan".
        /// </summary>
        private string ReadEntryName(uint pointerOffset)
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                return null;
            }

            uint entry = ReadUInt(episodeManager + pointerOffset);
            if (entry == 0)
            {
                return null;
            }

            return TrimKey(ReadString(entry));
        }

        /// <summary>
        /// Starts following the current goal through memory subscriptions, so the checkpoint and
        /// the autosplitter see goal changes as they happen. Call again after a reconnect: the
        /// console drops subscriptions when the game resets. Does nothing on the old webMAN API,
        /// which has no subscriptions; the goals are polled there instead.
        /// </summary>
        public void StartGoalWatcher(bool afterReset = false)
        {
            StopGoalWatcher(!afterReset);
            if (api is WebMAN)
            {
                return;
            }
            Sly4GoalWatcher watcher = new Sly4GoalWatcher(this);
            watcher.Changed += () =>
            {
                Action handler = GoalsChanged;
                if (handler != null)
                {
                    handler();
                }
            };
            goalWatcher = watcher;
            watcher.Start();
        }

        public void StopGoalWatcher(bool releaseSubscriptions = true)
        {
            Sly4GoalWatcher watcher = goalWatcher;
            goalWatcher = null;
            if (watcher != null)
            {
                watcher.Stop(releaseSubscriptions);
            }
        }

        /// <summary>The running job's goal pointers (0 for empty slots), read now. For the goal watcher.</summary>
        public List<uint> ReadGoals()
        {
            return GetGoals();
        }

        public int ReadGoalState(uint goal)
        {
            return ReadByte(goal + addr.goalStateOffset);
        }

        /// <summary>The current job's goal pointers (0 for empty slots), or none outside jobs.</summary>
        private List<uint> GetGoals()
        {
            List<uint> goals = new List<uint>();
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                return goals;
            }

            byte[] countBytes = api.ReadMemory(pid, gameManager + addr.goalCountOffset, 2);
            int count = BitConverter.ToUInt16(countBytes.Reverse().ToArray(), 0);
            uint goalArray = ReadUInt(gameManager + addr.goalArrayOffset);
            // Jobs have a few dozen goals; anything far above is a read from a half-loaded job.
            if (count == 0 || count > 256 || goalArray == 0)
            {
                return goals;
            }

            byte[] bytes = api.ReadMemory(pid, goalArray, (uint)(count * 4));
            for (int i = 0; i < count; i++)
            {
                goals.Add(BitConverter.ToUInt32(bytes.Skip(i * 4).Take(4).Reverse().ToArray(), 0));
            }
            return goals;
        }

        /// <summary>A localization key such as "$JapanTitleName" as a name, "Japan".</summary>
        private static string TrimKey(string key)
        {
            if (key.StartsWith("$"))
            {
                key = key.Substring(1);
            }
            if (key.EndsWith("TitleName"))
            {
                key = key.Substring(0, key.Length - "TitleName".Length);
            }
            return key;
        }

        /// <summary>A null-terminated ASCII string of up to 64 characters.</summary>
        private string ReadString(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 64);
            int length = Array.IndexOf(bytes, (byte)0);
            return Encoding.ASCII.GetString(bytes, 0, length < 0 ? bytes.Length : length);
        }

        /// <summary>A string of up to 64 characters at <paramref name="offset"/> in bytes already read.</summary>
        private static string ReadString(byte[] bytes, int offset)
        {
            int maxLength = Math.Min(64, bytes.Length - offset);
            int length = 0;
            while (length < maxLength && bytes[offset + length] != 0)
            {
                length++;
            }
            return Encoding.ASCII.GetString(bytes, offset, length);
        }

        /// <summary>Reads more than one request can carry, in MaxReadSize pieces.</summary>
        private byte[] ReadLarge(uint address, uint size)
        {
            byte[] result = new byte[size];
            uint done = 0;
            while (done < size)
            {
                uint chunk = Math.Min(MaxReadSize, size - done);
                byte[] part = api.ReadMemory(pid, address + done, chunk);
                Array.Copy(part, 0, result, done, chunk);
                done += chunk;
            }
            return result;
        }

        /// <summary>
        /// The player ref, or 0 while the player doesn't exist. Also 0 in the hacking minigames:
        /// the player there is the minigame's ship, ball or avatar, not a character, so the
        /// character controller's pointers followed from it lead to memory that isn't a
        /// character's, and holding or teleporting the player would write over it.
        /// </summary>
        private uint GetPlayerRef()
        {
            // The Hackpack has no character anywhere: its hub is only looked at.
            if (addr.IsHackpack)
            {
                return 0;
            }
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                return 0;
            }
            if (IsHackLevel(ReadUInt(gameManager + addr.currentLevelHashOffset)))
            {
                return 0;
            }

            uint playerRef = ReadUInt(gameManager + addr.playerRefOffset);
            if (playerRef == 0)
            {
                return 0;
            }

            // Bit 1 is set while the player is live.
            if ((ReadUInt(playerRef + addr.playerFlagsOffset) & 2) == 0)
            {
                return 0;
            }
            return playerRef;
        }

        private uint GetCharacterController()
        {
            uint playerRef = GetPlayerRef();
            if (playerRef == 0)
            {
                return 0;
            }
            return ReadUInt(playerRef + addr.characterControllerOffset);
        }

        private uint GetRigidBody()
        {
            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return 0;
            }

            uint characterBody = ReadUInt(controller + addr.characterBodyOffset);
            if (characterBody == 0)
            {
                return 0;
            }
            return ReadUInt(characterBody + addr.rigidBodyOffset);
        }

        private uint ReadUInt(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }

        private byte ReadByte(uint address)
        {
            return api.ReadMemory(pid, address, 1)[0];
        }

        private static float ToFloat(byte[] bytes, int offset)
        {
            byte[] value = new byte[4];
            Array.Copy(bytes, offset, value, 0, 4);
            return BitConverter.ToSingle(value.Reverse().ToArray(), 0);
        }
    }
}
