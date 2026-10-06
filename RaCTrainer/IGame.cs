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

        public float[] coords = new float[3];
        public int pid;

        public Timer InputsTimer = new Timer();

        public int selectedPositionIndex { get; set; }

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

        public virtual void SetupInputDisplayMemorySubs()
        {
            SetupInputDisplayMemorySubsButtons();
            SetupInputDisplayMemorySubsAnalogs();
        }

        protected virtual void SetupInputDisplayMemorySubsButtons() { }

        protected virtual void SetupInputDisplayMemorySubsAnalogs() { }

        public virtual void GetPlayerCoordinates() { }

        public abstract void CheckInputs(object sender, EventArgs e);
    }
}
