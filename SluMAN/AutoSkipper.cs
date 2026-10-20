using System;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Skips cutscenes by itself while it runs, doing what Skip Cinematic does as soon as an FMV
    /// or a dialogue line starts. A background thread reads the FMV state a few times a second,
    /// and watches the dialogue frame counter through a subscription whose callback only marks
    /// a skip as due. The thread subscribes, writes the skips and releases the subscription, so
    /// neither the UI thread nor the subscription thread ever waits on the console. One instance
    /// runs once: Start, then Stop.
    /// </summary>
    public class AutoSkipper
    {
        // Matches Skip Cinematic: an FMV plays while the state's low byte is 0, and writing 2
        // ends it. A dialogue line can be skipped once its frame counter's low byte passes 10.
        private const uint FmvSkipValue = 2;
        private const uint DialogueSkipValue = 0;
        private const byte DialogueSkipAfterFrames = 10;

        // How often the FMV state is read. A subscription to it missed the FMVs on PAL Sly 2,
        // although the state is 0 while one plays, so it's read like Skip Cinematic reads it.
        private const int FmvPollMs = 100;

        // At most one skip of each kind this often, in case a write doesn't take and the value
        // keeps bouncing back to the cutscene state.
        private const int MinSkipIntervalMs = 250;

        private readonly IPS3API api;
        private readonly int pid;
        private readonly uint fmvState;
        private readonly uint dialogueState;
        private readonly uint dialogueFrameCounter;
        private readonly bool freezeDialogue;

        private readonly object pendingLock = new object();
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private bool dialoguePending;
        private bool stopping;
        private bool releaseOnStop = true;

        // A dialogue skip fires once per line: the next one waits until the counter has gone
        // back below the threshold. Only the subscription thread uses this.
        private bool dialogueArmed = true;

        /// <param name="freezeDialogue">Freeze the dialogue state to 0 instead of skipping each
        /// line as its frame counter passes 10 (Sly 3, whose dialogue state skips every line
        /// while frozen).</param>
        public AutoSkipper(IPS3API api, int pid, uint fmvState, uint dialogueState, uint dialogueFrameCounter, bool freezeDialogue = false)
        {
            this.api = api;
            this.pid = pid;
            this.fmvState = fmvState;
            this.dialogueState = dialogueState;
            this.dialogueFrameCounter = dialogueFrameCounter;
            this.freezeDialogue = freezeDialogue;
        }

        public void Start()
        {
            Thread worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Auto-skipper";
            worker.Start();
        }

        /// <summary>
        /// Returns at once; the background thread releases the subscription and ends. After a game
        /// reset, pass false: the console has dropped it already and may have given its ID to a
        /// new subscription.
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
            int dialogueSubID = -1;
            try
            {
                if (freezeDialogue)
                {
                    // Released like the subscription when the skipper stops.
                    dialogueSubID = api.FreezeMemory(pid, dialogueState, MemoryCondition.Any, DialogueSkipValue);
                }
                else
                {
                    // Only the low byte matters, the same byte Skip Cinematic reads.
                    dialogueSubID = api.SubMemory(pid, dialogueFrameCounter + 0x3, 1, OnDialogueFrame);
                }
                Console.WriteLine("Auto-skip cinematics on.");

                DateTime lastFmvSkip = DateTime.MinValue;
                DateTime lastDialogueSkip = DateTime.MinValue;
                while (true)
                {
                    // Woken early by a dialogue line or Stop; otherwise time to read the FMV state.
                    wake.WaitOne(FmvPollMs);

                    bool skipDialogue;
                    lock (pendingLock)
                    {
                        if (stopping)
                        {
                            break;
                        }
                        skipDialogue = dialoguePending;
                        dialoguePending = false;
                    }

                    try
                    {
                        if ((DateTime.Now - lastFmvSkip).TotalMilliseconds >= MinSkipIntervalMs
                            && api.ReadMemory(pid, fmvState + 0x3, 1)[0] == 0)
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

        private void OnDialogueFrame(byte[] value)
        {
            if (value[0] <= DialogueSkipAfterFrames)
            {
                dialogueArmed = true;
            }
            else if (dialogueArmed)
            {
                dialogueArmed = false;
                MarkDialoguePending();
            }
        }

        private void MarkDialoguePending()
        {
            lock (pendingLock)
            {
                if (stopping)
                {
                    return;
                }
                dialoguePending = true;
            }
            wake.Set();
        }
    }
}
