using System;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Skips cutscenes by itself while it runs, doing what Skip Cinematic does as soon as an FMV
    /// or a dialogue line starts. It watches the FMV state and the dialogue frame counter through
    /// subscriptions. Their callbacks only mark a skip as due; a background thread subscribes,
    /// writes the skips and releases the subscriptions, so neither the UI thread nor the
    /// subscription thread ever waits on the console. One instance runs once: Start, then Stop.
    /// </summary>
    public class AutoSkipper
    {
        // Matches Skip Cinematic: an FMV plays while the state's low byte is 0, and writing 2
        // ends it. A dialogue line can be skipped once its frame counter's low byte passes 10.
        private const uint FmvSkipValue = 2;
        private const uint DialogueSkipValue = 0;
        private const byte DialogueSkipAfterFrames = 10;

        // At most one skip of each kind this often, in case a write doesn't take and the value
        // keeps bouncing back to the cutscene state.
        private const int MinSkipIntervalMs = 250;

        private readonly IPS3API api;
        private readonly int pid;
        private readonly uint fmvState;
        private readonly uint dialogueState;
        private readonly uint dialogueFrameCounter;

        private readonly object pendingLock = new object();
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private bool fmvPending;
        private bool dialoguePending;
        private bool stopping;
        private bool releaseOnStop = true;

        // A skip fires once per cutscene: the next one waits until the value has left the
        // cutscene state. Only the subscription thread uses these.
        private bool fmvArmed = true;
        private bool dialogueArmed = true;

        public AutoSkipper(IPS3API api, int pid, uint fmvState, uint dialogueState, uint dialogueFrameCounter)
        {
            this.api = api;
            this.pid = pid;
            this.fmvState = fmvState;
            this.dialogueState = dialogueState;
            this.dialogueFrameCounter = dialogueFrameCounter;
        }

        public void Start()
        {
            Thread worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Auto-skipper";
            worker.Start();
        }

        /// <summary>
        /// Returns at once; the background thread releases the subscriptions and ends. After a game
        /// reset, pass false: the console has dropped them already and may have given their IDs to
        /// new subscriptions.
        /// </summary>
        public void Stop(bool releaseSubscriptions = true)
        {
            lock (pendingLock)
            {
                stopping = true;
                releaseOnStop = releaseSubscriptions;
            }
            wake.Set();
        }

        private void Run()
        {
            int fmvSubID = -1;
            int dialogueSubID = -1;
            try
            {
                // Only the low byte matters, the same byte Skip Cinematic reads.
                fmvSubID = api.SubMemory(pid, fmvState + 0x3, 1, OnFmvState);
                dialogueSubID = api.SubMemory(pid, dialogueFrameCounter + 0x3, 1, OnDialogueFrame);
                Console.WriteLine("Auto-skip cinematics on.");

                DateTime lastFmvSkip = DateTime.MinValue;
                DateTime lastDialogueSkip = DateTime.MinValue;
                while (true)
                {
                    wake.WaitOne();

                    bool skipFmv;
                    bool skipDialogue;
                    lock (pendingLock)
                    {
                        if (stopping)
                        {
                            break;
                        }
                        skipFmv = fmvPending;
                        skipDialogue = dialoguePending;
                        fmvPending = false;
                        dialoguePending = false;
                    }

                    try
                    {
                        if (skipFmv && WaitForInterval(lastFmvSkip))
                        {
                            api.WriteMemory(pid, fmvState, FmvSkipValue);
                            lastFmvSkip = DateTime.Now;
                        }
                        if (skipDialogue && WaitForInterval(lastDialogueSkip))
                        {
                            api.WriteMemory(pid, dialogueState, DialogueSkipValue);
                            lastDialogueSkip = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Keep running: the console may only be busy or reconnecting.
                        Console.WriteLine($"Auto-skip couldn't skip a cutscene: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-skip cinematics couldn't start: {ex.Message}");
            }
            finally
            {
                // The event stays undisposed: a callback or Stop may still set it after this.
                bool release;
                lock (pendingLock)
                {
                    release = releaseOnStop;
                }
                if (release)
                {
                    Release(fmvSubID);
                    Release(dialogueSubID);
                }
            }
        }

        /// <summary>
        /// Sleeps on this thread until the last skip of the kind is MinSkipIntervalMs old. False
        /// if Stop was called meanwhile.
        /// </summary>
        private bool WaitForInterval(DateTime lastSkip)
        {
            double waitMs = MinSkipIntervalMs - (DateTime.Now - lastSkip).TotalMilliseconds;
            if (waitMs > 0)
            {
                Thread.Sleep((int)waitMs);
            }
            lock (pendingLock)
            {
                return !stopping;
            }
        }

        private void Release(int subID)
        {
            if (subID < 0)
            {
                return;
            }
            try
            {
                api.ReleaseSubID(subID);
            }
            catch
            {
                // The connection may be closing with the form.
            }
        }

        private void OnFmvState(byte[] value)
        {
            if (value[0] != 0)
            {
                fmvArmed = true;
            }
            else if (fmvArmed)
            {
                fmvArmed = false;
                MarkPending(true);
            }
        }

        private void OnDialogueFrame(byte[] value)
        {
            if (value[0] <= DialogueSkipAfterFrames)
            {
                dialogueArmed = true;
            }
            else if (dialogueArmed)
            {
                dialogueArmed = false;
                MarkPending(false);
            }
        }

        private void MarkPending(bool fmv)
        {
            lock (pendingLock)
            {
                if (stopping)
                {
                    return;
                }
                if (fmv)
                {
                    fmvPending = true;
                }
                else
                {
                    dialoguePending = true;
                }
            }
            wake.Set();
        }
    }
}
