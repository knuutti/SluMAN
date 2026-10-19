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

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            api.SubMemory(pid, addr.inputButtons, 4, (value) =>
            {
                // The word is 00, Select..Left, 00, L2..Square, which is the standard layout's two
                // bytes with a gap between them.
                int buttons = BitConverter.ToInt32(value, 0);
                Inputs.RawInputs = (buttons & 0xFF) | ((buttons >> 8) & 0xFF00);
            });
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            // Each stick is X then Y as big-endian floats. The callback gets the 8 bytes reversed,
            // so Y comes first.
            api.SubMemory(pid, addr.analogLeft, 8, (value) =>
            {
                Inputs.ly = BitConverter.ToSingle(value, 0);
                Inputs.lx = BitConverter.ToSingle(value, 4);
            });
            api.SubMemory(pid, addr.analogRight, 8, (value) =>
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

        /// <summary>The main view's render copy, or 0 before the game has one.</summary>
        public uint GetMainRenderView()
        {
            uint view = GetMainView();
            return view == 0 ? 0 : ReadUInt(view + addr.viewRenderCopyOffset);
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
        /// Starts drawing the main view from the given matrices (see ViewMatrices) every frame:
        /// the code hook runs a routine while the view is drawn, after the game has made its
        /// render copy, that copies them in and rebuilds the render matrices. The game's camera
        /// can't overwrite them in between, so nothing flickers.
        /// </summary>
        public void StartViewOverride(uint renderView, byte[] matrices)
        {
            SetViewOverride(matrices);

            uint routine = addr.viewOverrideRoutine;
            uint[] words =
            {
                0x38C00020,             // li r6, 32 (words)
                0x7CC903A6,             // mtctr r6
                0x38A302EC,             // addi r5, r3, view matrix - 4
                0x3884FFFC,             // addi r4, r4, -4
                0x84E40004,             // lwzu r7, 4(r4)
                0x94E50004,             // stwu r7, 4(r5)
                0x4200FFF8,             // bdnz -8
                0x38800001,             // li r4, 1 (rebuild even if not marked)
                // b CalcRenderMatrices (r3 is still the view; it returns to the hook)
                0x48000000 | ((addr.calcRenderMatricesFunction - (routine + 8 * 4)) & 0x3FFFFFC),
            };
            byte[] code = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(words[i]).Reverse().ToArray(), 0, code, i * 4, 4);
            }
            api.WriteMemory(pid, routine, code);
            if (!api.ReadMemory(pid, routine, (uint)code.Length).SequenceEqual(code))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            codeHook.SetRepeatingCall(routine, renderView, addr.viewOverrideData);
        }

        public void SetViewOverride(byte[] matrices)
        {
            api.WriteMemory(pid, addr.viewOverrideData, matrices);
        }

        /// <summary>Lets the game draw its own camera again, from the next frame.</summary>
        public void StopViewOverride()
        {
            codeHook.ClearRepeatingCall();
        }

        /// <summary>Where a view keeps its camera's position: the camera matrix's last row.</summary>
        public uint CameraPositionOffset()
        {
            return addr.viewCameraMatrixOffset + 0x30;
        }

        /// <summary>
        /// Every 4-aligned address in the ranges ({start, end} each) holding the bytes. Reads 1 MB
        /// at a time; meant for RPCS3, where a read is a copy rather than a round trip.
        /// </summary>
        public List<uint> FindMemory(byte[] pattern, uint[][] ranges)
        {
            const uint chunk = 0x100000;
            List<uint> found = new List<uint>();
            foreach (uint[] range in ranges)
            {
                for (uint start = range[0]; start < range[1]; start += chunk)
                {
                    // Overlap the next chunk by the pattern's length, so a match across the edge
                    // is still found.
                    uint size = Math.Min(chunk + (uint)pattern.Length, range[1] - start);
                    byte[] data = api.ReadMemory(pid, start, size);
                    for (int i = 0; i + pattern.Length <= data.Length && i < chunk; i += 4)
                    {
                        if (data[i] != pattern[0])
                        {
                            continue;
                        }
                        int j = 1;
                        while (j < pattern.Length && data[i + j] == pattern[j])
                        {
                            j++;
                        }
                        if (j == pattern.Length)
                        {
                            found.Add(start + (uint)i);
                        }
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Raises a copy of the camera's position (3 floats) for a moment, writing it over and
        /// over, and returns the most the main view's camera rose meanwhile: about the raise for
        /// a copy the view is made from, 0 for one that isn't. The copy is put back afterwards.
        /// </summary>
        public float TestCameraCopy(uint address, uint view, float raise, int milliseconds)
        {
            byte[] original = api.ReadMemory(pid, address, 12);
            float viewY = ToFloat(api.ReadMemory(pid, view + CameraPositionOffset() + 4, 4), 0);
            byte[] raised = (byte[])original.Clone();
            Array.Copy(ConvertFloatToBytes(ToFloat(original, 4) + raise), 0, raised, 4, 4);

            float most = 0;
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < milliseconds)
            {
                api.WriteMemory(pid, address, raised);
                float y = ToFloat(api.ReadMemory(pid, view + CameraPositionOffset() + 4, 4), 0);
                most = Math.Max(most, y - viewY);
            }
            api.WriteMemory(pid, address, original);
            return most;
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

        /// <summary>The level's name if it's known, otherwise its hash in hex.</summary>
        public static string GetLevelName(uint levelHash)
        {
            string name;
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
        /// another level doesn't have yet. To start later, warp to the goal once the job runs.
        /// </remarks>
        public void StartJob(int jobIndex, bool toHub, bool toHideout)
        {
            uint episodeManager = ReadUInt(addr.episodeManagerPtr);
            if (episodeManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            ThrowIfLoading();

            uint job = ReadUInt(episodeManager + addr.jobArrayOffset) + (uint)jobIndex * Sly4Addresses.JobStride;
            uint currentJob = ReadUInt(episodeManager + addr.currentJobOffset);
            if (!toHub && !toHideout && currentJob == job)
            {
                throw new InvalidOperationException("That job is already running. Warp to one of its goals instead.");
            }

            codeHook.Call(addr.startJobByInstFunction, episodeManager, job, 0,
                toHub ? 1u : 0u, toHideout ? 1u : 0u, 0, 0, 0, 1);
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

        /// <summary>The player ref, or 0 while the player doesn't exist.</summary>
        private uint GetPlayerRef()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
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
