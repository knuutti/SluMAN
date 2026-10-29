using System;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Starts jobs, warps to goals and loads levels for Sly 4's and the Hackpack's Practice
    /// windows and their Jobs tab. Each goes through a call the game runs on its next frame (see
    /// sly4.IsGameCallPending), which this tracks, reporting on the status line once it has run;
    /// and some wait for the game first, so they're queued: a goal warp waits for its job to
    /// start and load, and a job restart or level load waits for the job being left to end.
    /// Create on the UI thread; Stop when the window closes.
    /// </summary>
    public class Sly4JobQueue
    {
        // How often the call and the queue are checked.
        private const int CheckIntervalMs = 100;

        // How long a call may wait for the game's next frame before it's dropped.
        private static readonly TimeSpan GameCallTimeout = TimeSpan.FromSeconds(3);

        // A goal warp waits this long for its job to start and load, and for it to have stayed
        // that way this long, so the warp doesn't land in the job's setup.
        private static readonly TimeSpan QueuedGoalWarpTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan QueuedGoalWarpSettleTime = TimeSpan.FromSeconds(1);

        private readonly sly4 game;
        private readonly StatusLine statusLine;
        private readonly Timer timer = new Timer();

        private bool gameCallPending;
        private DateTime gameCallDeadline;
        private string gameCallDoneMessage;

        // A goal to warp to once the job started for it runs (see CheckQueuedGoalWarp).
        private sly4.JobEntry queuedGoalJob;
        private int queuedGoal;
        private DateTime queuedGoalDeadline;
        private DateTime queuedGoalReadySince = DateTime.MaxValue;
        // A job started on another map settles first; a restart in place loads nothing, so it
        // warps at once, before the restarted job shows the player in an odd state for long.
        private TimeSpan queuedGoalSettleTime;

        // The running job, to start again at a goal once the game has abandoned it (see RestartJob).
        private sly4.JobEntry queuedJobStart;
        private int queuedJobStartGoal;
        private uint queuedJobStartGoalId;

        // A level to load once the job being abandoned for it has ended (see LoadLevel).
        private uint queuedLevelHash;
        private string queuedLevelName;
        private sly4.CharacterEntry queuedCharacter;
        private sly4.SpawnPointEntry queuedSpawnPoint;

        public Sly4JobQueue(sly4 game, StatusLine statusLine)
        {
            this.game = game;
            this.statusLine = statusLine;
            timer.Interval = CheckIntervalMs;
            timer.Tick += (sender, e) => CheckQueuedWork();
        }

        public void Stop()
        {
            timer.Stop();
            timer.Dispose();
        }

        /// <summary>
        /// Starts a job, or goes to its episode's hub or hideout; a running job starts again. The
        /// Hackpack has no hubs or hideouts per episode, so those are ignored there.
        /// </summary>
        public void StartJob(sly4.JobEntry job, bool toHub, bool toHideout)
        {
            ClearQueue();
            try
            {
                if (sly4.addr.IsHackpack)
                {
                    // The Hackpack's start can start the running job again (see sly4.StartJob).
                    StartJobAtGoal(job, 0, 0, false, game.IsJobRunning(job.index));
                    return;
                }
                if (!toHub && !toHideout && game.IsJobRunning(job.index))
                {
                    RestartJob(job, 0);
                    return;
                }
                game.StartJob(job.index, toHub, toHideout);
                string where = toHub ? $"{job.EpisodeTitle}'s hub" : (toHideout ? $"{job.EpisodeTitle}'s hideout" : job.Title);
                Track($"Going to {where}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't start the job: {ex.Message}");
            }
        }

        /// <summary>
        /// Warps to a goal (0-based) of a job (the running one if no job is given). A job that
        /// isn't running is started and warped once it has loaded. In Sly 4 a running job is
        /// started again, as if coming from another job, since a warp in place skips the goal's
        /// setup (such as a cutscene) and leaves the state of later goals behind; in the
        /// Hackpack, and when the running job can't be found, it warps in place.
        /// </summary>
        public void WarpToGoal(sly4.JobEntry job, int goal)
        {
            ClearQueue();
            try
            {
                if (job == null)
                {
                    job = RunningJob();
                }

                if (job == null || (sly4.addr.IsHackpack && game.IsJobRunning(job.index)))
                {
                    game.WarpToGoal(goal);
                    Track($"Warping to goal {goal + 1}.");
                }
                else if (game.IsJobRunning(job.index))
                {
                    RestartJob(job, goal);
                }
                else
                {
                    StartJobAtGoal(job, goal, 0, false, false);
                }
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't warp: {ex.Message}");
            }
        }

        /// <summary>Starts the running job again at its first goal.</summary>
        public void RestartRunningJob()
        {
            try
            {
                sly4.JobEntry job = RunningJob();
                if (job == null)
                {
                    statusLine.Error("No job is running.");
                    return;
                }
                StartJob(job, false, false);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't restart the job: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a level, as a character and at a spawn point (null for the level's own). In
        /// Sly 4 a running job would load its own level instead, so it's ended first and the
        /// level loaded once the game has run that call.
        /// </summary>
        public void LoadLevel(uint levelHash, string levelName, sly4.CharacterEntry character, sly4.SpawnPointEntry spawnPoint)
        {
            ClearQueue();
            try
            {
                if (!sly4.addr.IsHackpack && game.IsInJob())
                {
                    game.AbandonJob();
                    queuedLevelHash = levelHash;
                    queuedLevelName = levelName;
                    queuedCharacter = character;
                    queuedSpawnPoint = spawnPoint;
                    Track($"Left the job; loading {levelName}.");
                    return;
                }
                game.LoadLevel(levelHash, character, spawnPoint);
                Track(LoadingMessage(levelName, character, spawnPoint));
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the level: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a run file: goes to the hideout of its episode's first job with its gadgets, or
        /// with the gadgets as they are if gadgets is null. Throws if the game can't start it.
        /// </summary>
        public void LoadRunFile(string runFile, sly4.JobEntry job, byte[] gadgets)
        {
            ClearQueue();

            // Started first, so a job the game won't start leaves the gadgets alone. Starting a
            // job never resets gadgets; it can make more available, but a run file's gadgets
            // only hold ones available by its episode (GadgetAvailability).
            game.StartJob(job.index, false, true);
            Track($"Loading the {runFile} run file.");
            game.api.Notify($"SluMAN {func.VersionText}: Loading {runFile} run file.");

            if (gadgets != null)
            {
                game.SetGadgetStates(gadgets);
            }
        }

        /// <summary>
        /// Reloads so the gadgets written to the game take effect, as the game only reads them on
        /// a load: retries the checkpoint in a job (as Retry Checkpoint does), otherwise loads the
        /// current level again from its start. Used by the Gadgets tab's Apply.
        /// </summary>
        public void ReloadForGadgets()
        {
            int checkpoint;
            uint levelHash;
            try
            {
                game.GetCheckpoint(out checkpoint, out int goalCount, out string goalName);
                levelHash = game.GetLevelHash();
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't reload: {ex.Message}");
                return;
            }

            if (checkpoint > 0)
            {
                WarpToGoal(null, checkpoint - 1);
                return;
            }
            if (levelHash == 0)
            {
                statusLine.Error("No level is loaded, so the gadgets take effect on the next load.");
                return;
            }

            ClearQueue();
            try
            {
                game.LoadLevel(levelHash, null, null);
                Track($"Reloading {sly4.GetLevelName(levelHash)}.");
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't reload the level: {ex.Message}");
            }
        }

        /// <summary>The running job's entry, or null outside jobs.</summary>
        public sly4.JobEntry RunningJob()
        {
            int index = game.GetRunningJobIndex();
            if (index < 0)
            {
                return null;
            }
            foreach (sly4.JobEntry job in game.GetJobs())
            {
                if (job.index == index)
                {
                    return job;
                }
            }
            return null;
        }

        private void ClearQueue()
        {
            queuedGoalJob = null;
            queuedJobStart = null;
            queuedLevelName = null;
        }

        /// <summary>
        /// Starts a job that isn't running (or, in the Hackpack, again) and queues the warp to
        /// the goal (0-based): the game starts jobs at their first goal, so it warps once the job
        /// runs. <paramref name="goalId"/> resets the job's state to that goal (see RestartJob).
        /// </summary>
        private void StartJobAtGoal(sly4.JobEntry job, int goal, uint goalId, bool inPlace, bool restart)
        {
            game.StartJob(job.index, false, false, goalId, restart);
            if (goal > 0)
            {
                queuedGoalJob = job;
                queuedGoal = goal;
                queuedGoalDeadline = DateTime.Now + QueuedGoalWarpTimeout;
                queuedGoalReadySince = DateTime.MaxValue;
                queuedGoalSettleTime = inPlace ? TimeSpan.Zero : QueuedGoalWarpSettleTime;
            }
            string verb = restart ? "Restarting" : "Starting";
            Track(goal > 0 ? $"{verb} {job.Title}; it warps to goal {goal + 1} once loaded." : $"{verb} {job.Title}.");
        }

        /// <summary>
        /// Starts Sly 4's running job again at a goal (0-based), as if coming from another job:
        /// the game won't start the running job, so it's abandoned first and started once that
        /// has run. The goal's ID is read before abandoning, while the job's goals are loaded, and
        /// passed to the start, which resets the job's state to that goal but doesn't move the
        /// player or set the goal up; the warp to the goal once the job runs does that, like the
        /// game's own job start does (it teleports to the goal after StartJobByInst).
        /// </summary>
        private void RestartJob(sly4.JobEntry job, int goal)
        {
            uint goalId = goal > 0 ? game.GetGoalId(goal) : 0;
            game.AbandonJob();
            queuedJobStart = job;
            queuedJobStartGoal = goal;
            queuedJobStartGoalId = goalId;
            Track($"Left {job.Title}; starting it again.");
        }

        private static string LoadingMessage(string levelName, sly4.CharacterEntry character, sly4.SpawnPointEntry spawnPoint)
        {
            string message = $"Loading {levelName}";
            if (spawnPoint != null)
            {
                message += $" at {spawnPoint.title}";
            }
            if (character != null)
            {
                message += $" as {character.title}";
            }
            return message + ".";
        }

        /// <summary>Waits for a call queued through the code hook, reporting the message once it has run.</summary>
        private void Track(string doneMessage)
        {
            gameCallPending = true;
            gameCallDeadline = DateTime.Now + GameCallTimeout;
            gameCallDoneMessage = doneMessage;
            timer.Start();
        }

        private void CheckQueuedWork()
        {
            if (gameCallPending)
            {
                CheckGameCall();
            }
            else if (queuedLevelName != null)
            {
                LoadQueuedLevel();
            }
            else if (queuedJobStart != null)
            {
                StartQueuedJob();
            }
            else if (queuedGoalJob != null)
            {
                CheckQueuedGoalWarp();
            }
            else
            {
                timer.Stop();
            }
        }

        private void CheckGameCall()
        {
            try
            {
                if (!game.IsGameCallPending())
                {
                    gameCallPending = false;
                    statusLine.Info(gameCallDoneMessage);
                }
                else if (DateTime.Now > gameCallDeadline)
                {
                    gameCallPending = false;
                    ClearQueue();
                    game.CancelGameCall();
                    statusLine.Error("The game didn't run the call. On RPCS3, set the PPU decoder to Interpreter (static).");
                }
            }
            catch
            {
                // Checked again on the next tick.
            }
        }

        private void LoadQueuedLevel()
        {
            string levelName = queuedLevelName;
            queuedLevelName = null;
            try
            {
                game.LoadLevel(queuedLevelHash, queuedCharacter, queuedSpawnPoint);
                Track(LoadingMessage(levelName, queuedCharacter, queuedSpawnPoint));
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't load the level: {ex.Message}");
            }
        }

        private void StartQueuedJob()
        {
            sly4.JobEntry job = queuedJobStart;
            queuedJobStart = null;
            try
            {
                StartJobAtGoal(job, queuedJobStartGoal, queuedJobStartGoalId, true, false);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't restart the job: {ex.Message}");
            }
        }

        /// <summary>
        /// Warps to the queued goal once its job is running, done loading and has the goal loaded,
        /// and has stayed that way for a moment, so the warp doesn't land in the job's setup.
        /// </summary>
        private void CheckQueuedGoalWarp()
        {
            if (DateTime.Now > queuedGoalDeadline)
            {
                statusLine.Error($"{queuedGoalJob.Title} didn't start in time, so it wasn't warped to goal {queuedGoal + 1}.");
                queuedGoalJob = null;
                return;
            }

            try
            {
                bool ready = !game.IsLoading() && game.IsJobRunning(queuedGoalJob.index) && game.GetGoalCount() > queuedGoal;
                if (!ready)
                {
                    queuedGoalReadySince = DateTime.MaxValue;
                    return;
                }
                if (queuedGoalReadySince == DateTime.MaxValue)
                {
                    queuedGoalReadySince = DateTime.Now;
                }
                if (DateTime.Now - queuedGoalReadySince < queuedGoalSettleTime)
                {
                    return;
                }

                int goal = queuedGoal;
                queuedGoalJob = null;
                game.WarpToGoal(goal);
                Track($"Warping to goal {goal + 1}.");
            }
            catch (Exception ex)
            {
                queuedGoalJob = null;
                statusLine.Error($"Couldn't warp to the goal: {ex.Message}");
            }
        }
    }
}
