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

        public abstract void SavePosition();
        public abstract void LoadPosition();

        public virtual void SetupInputDisplayMemorySubs()
        {
            SetupInputDisplayMemorySubsButtons();
            SetupInputDisplayMemorySubsAnalogs();
        }

        protected virtual void SetupInputDisplayMemorySubsButtons() { }

        protected virtual void SetupInputDisplayMemorySubsAnalogs() { }

        public virtual void GetPlayerCoordinates() { }

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
