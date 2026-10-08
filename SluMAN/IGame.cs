using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    public abstract class IGame
    {
        public IPS3API api { get; }

        public bool inputCheck = true;

        public int pid;

        public Timer InputsTimer = new Timer();

        /// <summary>How many saved position slots each map has.</summary>
        public const int PositionSlotCount = 8;

        private const string PositionSlotKey = "positionSlot";
        private int selectedPosition = ReadSelectedPositionSlot();

        /// <summary>
        /// The slot that Save Position and Load Position use, from the controller combos as well as
        /// the Position Editor. Remembered across launches.
        /// </summary>
        public int selectedPositionIndex
        {
            get { return selectedPosition; }
            set
            {
                int slot = Math.Max(0, Math.Min(PositionSlotCount - 1, value));
                if (slot == selectedPosition)
                {
                    return;
                }
                selectedPosition = slot;
                func.ChangeFileLines("config.txt", slot.ToString(), PositionSlotKey);
            }
        }

        private static int ReadSelectedPositionSlot()
        {
            try
            {
                int slot;
                if (int.TryParse(func.GetConfigData("config.txt", PositionSlotKey), out slot) && slot >= 0 && slot < PositionSlotCount)
                {
                    return slot;
                }
            }
            catch
            {
                // No readable config.txt yet.
            }
            return 0;
        }

        /// <summary>
        /// A map name made safe for config.txt, whose keys may only hold letters, digits, '_' and
        /// '-'. Map names such as "Y$KFv_ext" contain '$', which made their keys unreadable.
        /// </summary>
        public static string ConfigKeyForMap(string mapIndicator)
        {
            StringBuilder key = new StringBuilder(mapIndicator.Length);
            foreach (char c in mapIndicator)
            {
                key.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }
            return key.ToString();
        }

        /// <summary>The config key holding a slot's coordinates on a map.</summary>
        public static string SavedPositionKey(string mapIndicator, int slot)
        {
            return ConfigKeyForMap(mapIndicator) + "SavedPos" + slot;
        }

        /// <summary>The config key holding a slot's name on a map.</summary>
        public static string SavedPositionNameKey(string mapIndicator, int slot)
        {
            return ConfigKeyForMap(mapIndicator) + "SavedPosName" + slot;
        }

        /// <summary>
        /// Raised after a position is saved, from a combo or the Position Editor, so open views can
        /// refresh. Raised on the thread that saved, which is the UI thread for both.
        /// </summary>
        public event Action PositionSaved;

        protected void OnPositionSaved()
        {
            Action handler = PositionSaved;
            if (handler != null)
            {
                handler();
            }
        }

        protected IGame(IPS3API api)
        {
            this.api = api;
            this.pid = api.getCurrentPID();

            if (api is Ratchetron)
            {
                ((Ratchetron)api).OpenDataChannel();
            }

            InputsTimer.Interval = (int)16.66667;
            InputsTimer.Tick += new EventHandler(CheckInputs);
        }

        /// <summary>
        /// Set by the Practice Mode forms. Controller combos only run while this is true, so
        /// Speedrun Mode never acts on a combo.
        /// </summary>
        public bool combosActive = false;

        public abstract void SavePosition();
        public abstract void LoadPosition();

        /// <summary>
        /// Reloads the game from the last checkpoint. Games without a reload leave this empty.
        /// </summary>
        public virtual void LoadGame() { }

        /// <summary>
        /// Runs the action whose combo matches the pad right now. A combo fires once per press:
        /// after one fires, nothing else fires until every button is released.
        /// </summary>
        protected void RunCombos()
        {
            int input = Inputs.RawInputs;

            if (input == 0)
            {
                inputCheck = true;
                return;
            }

            if (!combosActive || !ConfigureCombos.combosEnabled || ConfigureCombos.capturing || !inputCheck)
            {
                return;
            }

            Action action = null;
            string actionName = "";

            if (input == ConfigureCombos.saveCombo)
            {
                action = SavePosition;
                actionName = "Save position";
            }
            else if (input == ConfigureCombos.loadCombo)
            {
                action = LoadPosition;
                actionName = "Load position";
            }
            else if (input == ConfigureCombos.loadGameCombo)
            {
                action = LoadGame;
                actionName = "Load game";
            }
            else if (input == ConfigureCombos.runScriptCombo)
            {
                action = RunCurrentScript;
                actionName = "Run script";
            }

            if (action == null)
            {
                return;
            }

            // Cleared before the action runs: an action that shows a dialog keeps the message loop,
            // and this timer, running while the pad is still held.
            inputCheck = false;

            try
            {
                action();
                Console.WriteLine($"Combo: {actionName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Combo {actionName} failed: {ex.Message}");
            }
        }

        private static void RunCurrentScript()
        {
            if (AttachPS3Form.scripting != null)
            {
                AttachPS3Form.scripting.RunCurrentCode();
            }
        }

        /// <summary>
        /// Reads a null-terminated map name such as "Y$KFv_ext" from game memory. Returns an empty
        /// string when it can't be read.
        /// </summary>
        protected string ReadMapIndicator(uint address)
        {
            try
            {
                byte[] bytes = api.ReadMemory(pid, address, 64);
                int length = Array.IndexOf(bytes, (byte)0);
                if (length < 0)
                {
                    length = bytes.Length;
                }
                return Encoding.ASCII.GetString(bytes, 0, length);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Subscribes to a Sly game's pad and keeps <see cref="Inputs.RawInputs"/> in the standard
        /// layout. Sly 1, 2 and 3 store their buttons the same way.
        /// </summary>
        protected void SubscribeSlyButtons(uint inputAddress)
        {
            api.SubMemory(pid, inputAddress, 4, (value) =>
            {
                int slyButtonMask = BitConverter.ToInt32(value.Reverse().ToArray(), 0);
                Inputs.RawInputs = ConvertSlyButtonsToStandardFormat(slyButtonMask);
            });
        }

        private static int ConvertSlyButtonsToStandardFormat(int slyMask)
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
            if ((slyMask & 0x0002) != 0) standardMask |= 0x200;   // L3
            if ((slyMask & 0x0004) != 0) standardMask |= 0x400;   // R3

            return standardMask;
        }

        /// <summary>An int as the big-endian bytes the game stores.</summary>
        protected static byte[] ConvertIntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>A float as the big-endian bytes the game stores.</summary>
        protected static byte[] ConvertFloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Writes a large block in 256-byte chunks, so a single write doesn't overwhelm the API.
        /// </summary>
        public void WriteMemoryRegion(uint startAddress, byte[] data)
        {
            const int chunkSize = 256;
            for (int i = 0; i < data.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, data.Length - i);
                byte[] chunk = new byte[size];
                Array.Copy(data, i, chunk, 0, size);
                api.WriteMemory(pid, startAddress + (uint)i, chunk);
            }
        }

        /// <summary>
        /// Converts the comma-separated decimal bytes in run file and job configs, such as
        /// "0,1,255", to bytes.
        /// </summary>
        public static byte[] ConvertMemoryDataString(string data)
        {
            string[] parts = data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            byte[] bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                bytes[i] = (byte)int.Parse(parts[i]);
            }
            return bytes;
        }

        public virtual void SetupInputDisplayMemorySubs()
        {
            SetupInputDisplayMemorySubsButtons();
            SetupInputDisplayMemorySubsAnalogs();
        }

        protected virtual void SetupInputDisplayMemorySubsButtons() { }

        protected virtual void SetupInputDisplayMemorySubsAnalogs() { }

        public abstract void CheckInputs(object sender, EventArgs e);

        /// <summary>
        /// Raised when a load finishes: entering a map, reloading, or resetting. Dynamic addresses
        /// such as the player entity can change during a load, so anything written to them must
        /// be applied again. Raised on a background thread.
        /// </summary>
        public event Action LoadFinished;

        /// <summary>
        /// Raised when a load starts. Raised on a background thread.
        /// </summary>
        public event Action LoadStarted;

        private int loadWatcherSubID = -1;
        private int lastLoadingState = -1;

        /// <summary>
        /// Starts watching for finished loads. Call again after a reconnect, since subscriptions
        /// don't survive one. Games without a known loading address do nothing.
        /// </summary>
        public virtual void SetupLoadWatcher() { }

        /// <summary>
        /// Subscribes to the game's loading state. The game is done loading when it reads 3, the
        /// same value the autosplitters and the Practice Mode pop-up use.
        /// </summary>
        protected void WatchLoads(uint loadingStateAddress)
        {
            if (api is WebMAN)
            {
                // The old API has no memory subscriptions.
                return;
            }

            if (loadWatcherSubID != -1)
            {
                try { api.ReleaseSubID(loadWatcherSubID); } catch { }
                loadWatcherSubID = -1;
            }

            // The first value seen is only a starting point, so subscribing while in game doesn't
            // count as a load.
            lastLoadingState = -1;

            loadWatcherSubID = api.SubMemory(pid, loadingStateAddress, 4, (value) =>
            {
                int state = value[0];
                if (state != lastLoadingState)
                {
                    Console.WriteLine($"Loading state {lastLoadingState} -> {state}");
                }
                bool finished = state == 3 && lastLoadingState != -1 && lastLoadingState != 3;
                bool started = state != 3 && lastLoadingState == 3;
                lastLoadingState = state;

                Action handler = started ? LoadStarted : (finished ? LoadFinished : null);
                if (handler != null)
                {
                    handler();
                }
            });
        }
    }
}
