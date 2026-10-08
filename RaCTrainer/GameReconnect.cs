using System;
using System.Threading;
using System.Windows.Forms;
using racman.Memory;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    /// <summary>
    /// What the game forms share when the game reboots or another game starts: waiting for the
    /// game to come back, and switching to a different supported game in the same mode.
    /// </summary>
    public static class GameReconnect
    {
        private const int PollIntervalMs = 3000;
        private const int MaxWaitMs = 90000;

        /// <summary>
        /// Raised when a rebooted game is running again, before the game form sets itself up.
        /// Subscriptions don't survive a reboot, so windows that hold their own (the memory
        /// window) use this to set them up again. Raised on a background thread.
        /// </summary>
        public static event Action GameReconnected;

        public enum Result
        {
            /// <summary>The same game is running again.</summary>
            Reconnected,
            /// <summary>Nothing came back in time.</summary>
            TimedOut,
            /// <summary>A different supported game is running; the caller should switch to it.</summary>
            DifferentGame,
            /// <summary>A game SluMAN doesn't support is running.</summary>
            UnsupportedGame,
        }

        /// <summary>
        /// Waits for the game with <paramref name="titleId"/> to be running again. Blocks, so call it
        /// from a background thread (the Ratchetron reconnect callback already is one).
        /// </summary>
        /// <param name="gameName">For messages, such as "Sly 2".</param>
        /// <param name="pid">The new process ID when the result is Reconnected.</param>
        /// <param name="runningTitleId">The title that's running instead, for DifferentGame and
        /// UnsupportedGame.</param>
        public static Result WaitForGame(IPS3API api, string titleId, string gameName, out int pid, out string runningTitleId)
        {
            pid = 0;
            runningTitleId = "";

            for (int waited = 0; waited < MaxWaitMs; waited += PollIntervalMs)
            {
                Thread.Sleep(PollIntervalMs);

                try
                {
                    string title = api.getGameTitleID();
                    int currentPid = api.getCurrentPID();

                    if (title == titleId && currentPid != 0)
                    {
                        pid = currentPid;
                        // Before GameReconnected, so windows that subscribe again use the new PID.
                        AttachPS3Form.pid = currentPid;
                        Console.WriteLine($"{gameName}: game detected after {(waited + PollIntervalMs) / 1000} s (PID: {pid})");
                        Action handler = GameReconnected;
                        if (handler != null)
                        {
                            handler();
                        }
                        return Result.Reconnected;
                    }

                    if (IsRealTitle(title) && title != titleId && currentPid != 0)
                    {
                        runningTitleId = title;
                        return AttachPS3Form.IsSupportedTitle(title) ? Result.DifferentGame : Result.UnsupportedGame;
                    }

                    func.Status($"Waiting for {gameName} to start... {(waited + PollIntervalMs) / 1000} s");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{gameName}: error checking game status: {ex.Message}");
                }
            }

            return Result.TimedOut;
        }

        /// <summary>
        /// Reports a wait that didn't end with the same game. Returns true when the caller should
        /// stop; for DifferentGame it also starts the switch.
        /// </summary>
        public static bool HandleOtherResult(Result result, Form form, string gameName, string runningTitleId, bool speedrunMode)
        {
            switch (result)
            {
                case Result.TimedOut:
                    func.Status($"{gameName} didn't start within {MaxWaitMs / 1000} s. Start it and SluMAN reconnects.", true);
                    return true;
                case Result.UnsupportedGame:
                    func.Status($"{runningTitleId} isn't supported. SluMAN reconnects when you start a Sly game.", true);
                    return true;
                case Result.DifferentGame:
                    SwitchTo(form, runningTitleId, speedrunMode);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Closes <paramref name="form"/> and attaches to the game that's running now, in the same
        /// mode. Safe to call from any thread.
        /// </summary>
        public static void SwitchTo(Form form, string titleId, bool speedrunMode)
        {
            if (form.IsDisposed || !form.IsHandleCreated)
            {
                return;
            }

            try
            {
                form.BeginInvoke(new Action(() =>
                {
                    Console.WriteLine($"Switching to {titleId}.");
                    AttachPS3Form.RequestAttachAfterClose(speedrunMode);
                    form.Close();
                }));
            }
            catch
            {
                // The form closed in between.
            }
        }

        /// <summary>
        /// RPCS3 has no reconnect callback, so poll its window title for a different game. Restarting
        /// the same game needs nothing: RPCS3 keeps the process, and loads are handled elsewhere.
        /// The timer stops when the form closes.
        /// </summary>
        public static void WatchRpcs3(Form form, IPS3API api, string titleId, bool speedrunMode)
        {
            if (!(api is RPCS3))
            {
                return;
            }

            Timer timer = new Timer();
            timer.Interval = PollIntervalMs;
            timer.Tick += (sender, e) =>
            {
                string title;
                try
                {
                    title = api.getGameTitleID();
                }
                catch
                {
                    return;
                }

                if (IsRealTitle(title) && title != titleId && AttachPS3Form.IsSupportedTitle(title))
                {
                    timer.Stop();
                    SwitchTo(form, title, speedrunMode);
                }
            };
            form.FormClosed += (sender, e) =>
            {
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private static bool IsRealTitle(string title)
        {
            return !string.IsNullOrEmpty(title) && title != "NOGAME";
        }
    }
}
