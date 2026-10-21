using System;
using System.Collections.Generic;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Keeps the Sly 4 autosplitter's values (autosplitters/sly4-autosplitter.asl) in the shared
    /// memory. They all hang off the game and episode managers, which only exist once the game has
    /// booted, so a background thread waits for those, then subscribes to each value; the current
    /// goal comes from the goal watcher. One instance runs once: Start, then Stop.
    ///
    /// Layout, every value a little-endian 32-bit int:
    /// <list type="table">
    /// <item><term>0</term><description>load phase, 0 when not loading</description></item>
    /// <item><term>4</term><description>in game, 0 on the title screen</description></item>
    /// <item><term>8</term><description>current level hash (unsigned)</description></item>
    /// <item><term>12</term><description>current job's index in the job array, -1 outside jobs</description></item>
    /// <item><term>16</term><description>1 while the job has failed</description></item>
    /// <item><term>20</term><description>current goal's index, -1 outside jobs</description></item>
    /// <item><term>24</term><description>the job's goal count</description></item>
    /// </list>
    /// </summary>
    public class Sly4AutosplitterValues
    {
        public const int LoadPhase = 0;
        public const int InGame = 4;
        public const int LevelHash = 8;
        public const int JobIndex = 12;
        public const int JobFailed = 16;
        public const int GoalIndex = 20;
        public const int GoalCount = 24;

        private const int RetryMs = 1000;

        private readonly sly4 game;
        private readonly Action<int, byte[]> write;
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly object stopLock = new object();
        private bool stopping;

        public Sly4AutosplitterValues(sly4 game, Action<int, byte[]> write)
        {
            this.game = game;
            this.write = write;
        }

        public void Start()
        {
            WriteInt(JobIndex, -1);
            WriteInt(GoalIndex, -1);

            Thread worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 4 autosplitter";
            worker.Start();
        }

        /// <summary>Returns at once; the background thread releases the subscriptions and ends.</summary>
        public void Stop()
        {
            lock (stopLock)
            {
                stopping = true;
            }
            wake.Set();
        }

        /// <summary>Writes the goal watcher's current goal. Called from any thread.</summary>
        public void WriteGoal(int goalIndex, int goalCount)
        {
            WriteInt(GoalIndex, goalIndex);
            WriteInt(GoalCount, goalCount);
        }

        private void Run()
        {
            List<int> subIDs = new List<int>();
            try
            {
                // Wait for the managers, which the game allocates while it boots.
                uint gameManager = 0;
                uint episodeManager = 0;
                uint jobArray = 0;
                while (!IsStopping())
                {
                    try
                    {
                        gameManager = ReadUInt(sly4.addr.gameManagerPtr);
                        episodeManager = ReadUInt(sly4.addr.episodeManagerPtr);
                        jobArray = episodeManager == 0 ? 0 : ReadUInt(episodeManager + sly4.addr.jobArrayOffset);
                    }
                    catch
                    {
                        // Not readable yet.
                    }
                    if (gameManager != 0 && jobArray != 0)
                    {
                        break;
                    }
                    wake.WaitOne(RetryMs);
                }
                if (IsStopping())
                {
                    return;
                }

                Watch(subIDs, gameManager + sly4.addr.loadPhaseOffset, 1, LoadPhase, (value) => value[0]);
                Watch(subIDs, gameManager + sly4.addr.inGameOffset, 1, InGame, (value) => value[0]);
                Watch(subIDs, gameManager + sly4.addr.currentLevelHashOffset, 4, LevelHash, (value) => BitConverter.ToInt32(value, 0));
                Watch(subIDs, episodeManager + sly4.addr.currentJobOffset, 4, JobIndex, (value) => JobIndexOf(BitConverter.ToUInt32(value, 0), jobArray));
                Watch(subIDs, episodeManager + sly4.addr.failedGoalOffset, 4, JobFailed, (value) => BitConverter.ToUInt32(value, 0) != 0 ? 1 : 0);

                while (!IsStopping())
                {
                    wake.WaitOne();
                }
            }
            catch (Exception ex)
            {
                func.Status($"The Sly 4 autosplitter stopped: {ex.Message}", true);
            }
            finally
            {
                foreach (int subID in subIDs)
                {
                    try { game.api.ReleaseSubID(subID); } catch { }
                }
            }
        }

        /// <summary>
        /// Writes a value now and whenever it changes. <paramref name="convert"/> gets the value's
        /// bytes little-endian, as subscription callbacks do.
        /// </summary>
        private void Watch(List<int> subIDs, uint address, uint size, int offset, Func<byte[], int> convert)
        {
            byte[] initial = game.api.ReadMemory(game.pid, address, size);
            Array.Reverse(initial);
            WriteInt(offset, convert(initial));
            subIDs.Add(game.api.SubMemory(game.pid, address, size, (value) => WriteInt(offset, convert(value))));
        }

        private static int JobIndexOf(uint job, uint jobArray)
        {
            if (job == 0 || job < jobArray || (job - jobArray) % Sly4Addresses.JobStride != 0)
            {
                return -1;
            }
            return (int)((job - jobArray) / Sly4Addresses.JobStride);
        }

        private bool IsStopping()
        {
            lock (stopLock)
            {
                return stopping;
            }
        }

        private void WriteInt(int offset, int value)
        {
            write(offset, BitConverter.GetBytes(value));
        }

        private uint ReadUInt(uint address)
        {
            byte[] bytes = game.api.ReadMemory(game.pid, address, 4);
            return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        }
    }
}
