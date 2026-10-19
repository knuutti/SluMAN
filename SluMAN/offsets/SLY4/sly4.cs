using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SluMAN
{
    public class sly4 : IGame, IAutosplitterAvailable, IAutosplitterComputedValues
    {
        public static Sly4Addresses addr = Sly4Addresses.ForGame(Sly4Addresses.DefaultGameId);

        // Display names for level hashes (gm + currentLevelHashOffset). Hashes not listed show in hex.
        private static readonly Dictionary<uint, string> levelNames = new Dictionary<uint, string>
        {
            { 0xE4927E12, "Main Menu" },
            { 0x46C660F7, "Paris Hideout" },
            { 0xA0355EFB, "Paris Streets" },
            { 0x3B677F57, "Paris Sewers" },
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
            public string name;
            public string episode;

            public override string ToString()
            {
                return $"{episode}: {name}";
            }
        }

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

        /// <summary>The current episode's name, or null when there is none.</summary>
        public string GetEpisodeName()
        {
            return ReadEntryName(addr.currentEpisodeOffset);
        }

        /// <summary>The current job's name, or null outside jobs.</summary>
        public string GetJobName()
        {
            return ReadEntryName(addr.currentJobOffset);
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
                List<uint> goals;
                List<int> states;
                int current;
                watcher.GetState(out goals, out states, out current);
                snapshotGoals = goals;
                snapshotStates = states;
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
