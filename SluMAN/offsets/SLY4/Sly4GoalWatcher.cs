using System;
using System.Collections.Generic;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Follows the running job's current goal (the first one not done) as it changes, for the
    /// autosplitter and the checkpoint display. The game keeps no "current goal" of its own; it
    /// loops over the goals and checks each state. So instead of reading every goal's state over
    /// and over, this subscribes to:
    /// <list type="bullet">
    /// <item>the current goal's state, so finishing it is seen within a server tick (~8 ms);</item>
    /// <item>the goal before it, so a warp back (which resets the later goals) is seen too;</item>
    /// <item>the goal array and count, which change when a job starts or ends.</item>
    /// </list>
    /// Callbacks only note what changed; a background thread reads the goals again and moves the
    /// subscriptions, so neither the UI thread nor the subscription thread waits on the console.
    /// One instance runs once: Start, then Stop.
    /// </summary>
    public class Sly4GoalWatcher
    {
        // How long to wait before trying again when the game manager or goals aren't readable.
        private const int RetryMs = 1000;

        private readonly sly4 game;
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly object stateLock = new object();

        private bool stopping;
        private bool releaseOnStop = true;

        // Read by the UI; written by the worker and, for a finished goal, by the callback.
        private List<uint> goals = new List<uint>();
        private List<int> states = new List<int>();
        private int current = -1;

        /// <summary>
        /// Raised when the current goal or the goal list changes, on a background thread. A form
        /// should BeginInvoke its refresh.
        /// </summary>
        public event Action Changed;

        public Sly4GoalWatcher(sly4 game)
        {
            this.game = game;
        }

        public void Start()
        {
            Thread worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 4 goal watcher";
            worker.Start();
        }

        /// <summary>
        /// Returns at once; the background thread releases the subscriptions and ends. After a game
        /// reset, pass false: the console has dropped them already and may have given their IDs to
        /// new subscriptions.
        /// </summary>
        public void Stop(bool releaseSubscriptions = true)
        {
            lock (stateLock)
            {
                stopping = true;
                releaseOnStop = releaseSubscriptions;
            }
            wake.Set();
        }

        /// <summary>
        /// The running job's goal pointers (0 for empty slots), their states as last read, and the
        /// current goal's index (-1 outside jobs; the goal count once all are done, so finishing the
        /// last goal shows as a change too).
        /// </summary>
        public void GetState(out List<uint> goalList, out List<int> stateList, out int currentIndex)
        {
            lock (stateLock)
            {
                goalList = new List<uint>(goals);
                stateList = new List<int>(states);
                currentIndex = current;
            }
        }

        private void Run()
        {
            uint gameManager = 0;
            int arraySubID = -1;
            int countSubID = -1;
            int currentSubID = -1;
            int previousSubID = -1;

            try
            {
                bool retry = false;
                // The subscribed goals changed while subscribing: read the goals again at once.
                bool recheck = false;
                while (true)
                {
                    lock (stateLock)
                    {
                        if (stopping)
                        {
                            break;
                        }
                    }
                    recheck = false;

                    try
                    {
                        uint manager = ReadUInt(sly4.addr.gameManagerPtr);
                        if (manager == 0)
                        {
                            Publish(new List<uint>(), new List<int>(), -1);
                            retry = true;
                        }
                        else
                        {
                            if (manager != gameManager)
                            {
                                Release(ref arraySubID);
                                Release(ref countSubID);
                                gameManager = manager;
                                arraySubID = game.api.SubMemory(game.pid, manager + sly4.addr.goalArrayOffset, 4, (value) => wake.Set());
                                countSubID = game.api.SubMemory(game.pid, manager + sly4.addr.goalCountOffset, 2, (value) => wake.Set());
                            }

                            List<uint> goalList = game.ReadGoals();
                            List<int> stateList = new List<int>();
                            foreach (uint goal in goalList)
                            {
                                stateList.Add(goal == 0 ? -1 : game.ReadGoalState(goal));
                            }
                            int currentIndex = CurrentIndex(goalList, stateList);
                            Publish(goalList, stateList, currentIndex);

                            Release(ref currentSubID);
                            Release(ref previousSubID);
                            if (currentIndex >= 0)
                            {
                                if (currentIndex < goalList.Count)
                                {
                                    currentSubID = SubscribeState(goalList, currentIndex, true);
                                    // A subscription only reports changes after it's made, and a
                                    // warp marks goals done in quick succession, so a goal finished
                                    // since it was read would never be reported: read it again.
                                    recheck = game.ReadGoalState(goalList[currentIndex]) == Sly4Addresses.GoalStateDone;
                                }
                                int previous = PreviousGoal(goalList, currentIndex);
                                if (previous >= 0)
                                {
                                    previousSubID = SubscribeState(goalList, previous, false);
                                    recheck = recheck || game.ReadGoalState(goalList[previous]) != Sly4Addresses.GoalStateDone;
                                }
                            }
                            retry = false;
                        }
                    }
                    catch
                    {
                        // The game is loading or reconnecting.
                        retry = true;
                    }

                    if (!recheck)
                    {
                        wake.WaitOne(retry ? RetryMs : Timeout.Infinite);
                    }
                }
            }
            finally
            {
                bool release;
                lock (stateLock)
                {
                    release = releaseOnStop;
                }
                if (release)
                {
                    Release(ref arraySubID);
                    Release(ref countSubID);
                    Release(ref currentSubID);
                    Release(ref previousSubID);
                }
            }
        }

        /// <summary>
        /// Subscribes to a goal's state. For the current goal, finishing it moves "current" on at
        /// once, before the goals are read again, so the change is reported without a round trip.
        /// </summary>
        private int SubscribeState(List<uint> goalList, int index, bool isCurrent)
        {
            uint goal = goalList[index];
            return game.api.SubMemory(game.pid, goal + sly4.addr.goalStateOffset, 1, (value) =>
            {
                bool done = value[0] == Sly4Addresses.GoalStateDone;
                if (isCurrent && done)
                {
                    AdvanceFrom(goal);
                }
                // The previous goal no longer done means a warp back; either way, read them again.
                if (isCurrent == done)
                {
                    wake.Set();
                }
            });
        }

        private void AdvanceFrom(uint finishedGoal)
        {
            bool changed = false;
            lock (stateLock)
            {
                if (current >= 0 && current < goals.Count && goals[current] == finishedGoal)
                {
                    states[current] = Sly4Addresses.GoalStateDone;
                    int next = CurrentIndex(goals, states);
                    changed = next != current;
                    current = next;
                }
            }
            if (changed)
            {
                RaiseChanged();
            }
        }

        private void Publish(List<uint> goalList, List<int> stateList, int currentIndex)
        {
            bool changed;
            lock (stateLock)
            {
                changed = currentIndex != current || !SameGoals(goalList, goals);
                goals = goalList;
                states = stateList;
                current = currentIndex;
            }
            if (changed)
            {
                RaiseChanged();
            }
        }

        private void RaiseChanged()
        {
            Action handler = Changed;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>The first goal not done, or the goal count once all are done; -1 without goals.</summary>
        private static int CurrentIndex(List<uint> goalList, List<int> stateList)
        {
            bool any = false;
            for (int i = 0; i < goalList.Count; i++)
            {
                if (goalList[i] == 0)
                {
                    continue;
                }
                any = true;
                if (stateList[i] != Sly4Addresses.GoalStateDone)
                {
                    return i;
                }
            }
            return any ? goalList.Count : -1;
        }

        private static int PreviousGoal(List<uint> goalList, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (goalList[i] != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool SameGoals(List<uint> a, List<uint> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void Release(ref int subID)
        {
            if (subID != -1)
            {
                try { game.api.ReleaseSubID(subID); } catch { }
                subID = -1;
            }
        }

        private uint ReadUInt(uint address)
        {
            byte[] bytes = game.api.ReadMemory(game.pid, address, 4);
            return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        }
    }
}
