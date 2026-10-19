using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// Fly mode and the free camera, both steered with the controller as read for the input
    /// display (so its subscriptions must be running):
    /// <list type="bullet">
    /// <item>Fly: the left stick moves the player along the camera's facing, R1 and L1 move up and
    /// down. Each frame the player is put at the flown position and stopped, so they float.</item>
    /// <item>Free camera: the main view is drawn from SluMAN's camera (see sly4's view
    /// override). The left stick moves it, the right stick turns it, R1 and L1 move up and down.
    /// The player is held where they stood.</item>
    /// </list>
    /// Runs on its own thread, faster than the game's frame rate on RPCS3 so the camera moves
    /// smoothly, and keeps going whichever tab shows. Sly4Practice owns the one instance. The free camera turns itself off when a load
    /// starts; fly mode waits for the player to be back.
    /// </summary>
    public class Sly4FreeMove
    {
        public enum Mode { Off, Fly, FreeCamera }

        private const int Rpcs3IntervalMs = 5;
        private const int PS3IntervalMs = 50;
        private const float DeadZone = 0.2f;
        private const float MaxPitch = 1.5f;
        // A tick after a stall moves at most this much time's worth.
        private const float MaxStep = 0.1f;

        private readonly sly4 game;
        private readonly int intervalMs;
        private readonly SynchronizationContext uiContext;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly object stateLock = new object();

        private Mode mode = Mode.Off;
        private Thread worker;
        private volatile bool running;

        // Where the player is held (fly: where they've flown to), or null until it's read.
        private float[] playerPosition;

        // The free camera: position, facing, and how the game's matrix rows relate to the facing.
        private float[] cameraPosition;
        private float yaw;
        private float pitch;
        private float forwardSign;
        private float rightSign;
        private float upSign;
        // The main view, and the 4th float of each row of its view and camera matrices, which
        // hold something else and are written back as they were.
        private uint view;
        private float[] viewW;
        private float[] cameraW;

        /// <summary>How fast fly mode moves the player: units per second at full stick.</summary>
        public float FlySpeed = 10f;

        /// <summary>How fast the free camera moves: units per second at full stick.</summary>
        public float CameraSpeed = 10f;

        /// <summary>How fast the free camera turns: radians per second at full stick.</summary>
        public float LookSpeed = 2f;

        /// <summary>
        /// Raised on the UI thread when a mode stops by itself: an error, or a load turning the
        /// free camera off.
        /// </summary>
        public event Action<string> Stopped;

        /// <summary>
        /// Raised on the UI thread whenever the mode may have changed, so the Player and Camera
        /// tabs can show which one is on.
        /// </summary>
        public event Action ModeChanged;

        /// <summary>Create on the UI thread, so Stopped is raised there.</summary>
        public Sly4FreeMove(sly4 game)
        {
            this.game = game;
            intervalMs = game.api is RPCS3 ? Rpcs3IntervalMs : PS3IntervalMs;
            uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public Mode Current => mode;

        public void Start(Mode newMode)
        {
            Stop();
            if (newMode == Mode.Off)
            {
                return;
            }
            if (game.IsLoading())
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }

            lock (stateLock)
            {
                float[] position = game.GetPosition();
                playerPosition = position;
                if (newMode == Mode.FreeCamera)
                {
                    StartCamera(position);
                }
                mode = newMode;
            }

            clock.Restart();
            running = true;
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 4 fly / free camera";
            worker.Start();
            RaiseModeChanged();
        }

        public void Stop()
        {
            running = false;
            Thread thread = worker;
            worker = null;
            if (thread != null && thread != Thread.CurrentThread)
            {
                thread.Join(500);
            }

            lock (stateLock)
            {
                if (mode == Mode.FreeCamera)
                {
                    try { game.StopViewOverride(); } catch { }
                }
                mode = Mode.Off;
            }
            RaiseModeChanged();
        }

        private void RaiseModeChanged()
        {
            uiContext.Post((state) =>
            {
                Action handler = ModeChanged;
                if (handler != null)
                {
                    handler();
                }
            }, null);
        }

        private void Run()
        {
            while (running)
            {
                string stopReason = null;
                lock (stateLock)
                {
                    stopReason = Tick();
                }
                if (stopReason != null)
                {
                    running = false;
                    lock (stateLock)
                    {
                        if (mode == Mode.FreeCamera)
                        {
                            try { game.StopViewOverride(); } catch { }
                        }
                        mode = Mode.Off;
                    }
                    uiContext.Post((state) =>
                    {
                        Action<string> handler = Stopped;
                        if (handler != null)
                        {
                            handler(stopReason);
                        }
                    }, null);
                    RaiseModeChanged();
                    return;
                }
                Thread.Sleep(intervalMs);
            }
        }

        /// <summary>One step; returns why the mode has to stop, or null to carry on.</summary>
        private string Tick()
        {
            float step = Math.Min((float)clock.Elapsed.TotalSeconds, MaxStep);
            clock.Restart();
            try
            {
                if (game.IsLoading())
                {
                    if (mode == Mode.FreeCamera)
                    {
                        return "the free camera turns off while a level loads.";
                    }
                    playerPosition = null;
                    return null;
                }

                if (mode == Mode.Fly)
                {
                    TickFly(step);
                }
                else if (mode == Mode.FreeCamera)
                {
                    TickCamera(step);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void TickFly(float step)
        {
            if (playerPosition == null)
            {
                // Not loaded when flying started, or a load since: pick up where the player is.
                playerPosition = game.GetPosition();
                if (playerPosition == null)
                {
                    return;
                }
            }

            uint view = game.GetMainView();
            if (view != 0)
            {
                float[] m = game.ReadCameraMatrix(view);
                float[] forward = Flatten(Facing(m, playerPosition));
                float[] right = Flatten(Axis(m, 0));
                float move = -Stick(Inputs.ly);
                float strafe = Stick(Inputs.lx);
                float rise = Rise();
                for (int axis = 0; axis < 3; axis++)
                {
                    playerPosition[axis] += (forward[axis] * move + right[axis] * strafe) * FlySpeed * step;
                }
                playerPosition[1] += rise * FlySpeed * step;
            }

            if (!game.HoldPlayer(playerPosition[0], playerPosition[1], playerPosition[2]))
            {
                playerPosition = null;
            }
        }

        private void StartCamera(float[] player)
        {
            view = game.GetMainView();
            if (view == 0)
            {
                throw new InvalidOperationException("The game has no camera yet.");
            }
            float[] m = game.ReadCameraMatrix(view);
            cameraPosition = new float[] { m[12], m[13], m[14] };
            float[] viewMatrix = game.ReadMatrix(view + sly4.addr.viewMatrixOffset);
            viewW = new float[] { viewMatrix[3], viewMatrix[7], viewMatrix[11], viewMatrix[15] };
            cameraW = new float[] { m[3], m[7], m[11], m[15] };

            // The camera looks at the player, so the Z axis or its opposite points at them.
            float[] z = Normalize(Axis(m, 2));
            forwardSign = 1;
            if (player != null && Dot(z, Subtract(player, cameraPosition)) < 0)
            {
                forwardSign = -1;
            }
            float[] forward = Scale(z, forwardSign);
            yaw = (float)Math.Atan2(forward[0], forward[2]);
            pitch = (float)Math.Asin(Math.Max(-1, Math.Min(1, forward[1])));

            float[] right = RightOf(forward);
            float[] up = Cross(forward, right);
            rightSign = Dot(Axis(m, 0), right) < 0 ? -1 : 1;
            upSign = Dot(Axis(m, 1), up) < 0 ? -1 : 1;

            WriteCamera(true);
        }

        private void TickCamera(float step)
        {
            yaw -= Stick(Inputs.rx) * LookSpeed * step;
            pitch -= Stick(Inputs.ry) * LookSpeed * step;
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));

            float[] forward = Forward();
            float[] right = Scale(RightOf(forward), rightSign);
            float move = -Stick(Inputs.ly);
            float strafe = Stick(Inputs.lx);
            for (int axis = 0; axis < 3; axis++)
            {
                cameraPosition[axis] += (forward[axis] * move + right[axis] * strafe) * CameraSpeed * step;
            }
            cameraPosition[1] += Rise() * CameraSpeed * step;
            WriteCamera();

            // Keep the player where they stood, so steering the camera doesn't walk them off.
            if (playerPosition != null)
            {
                game.HoldPlayer(playerPosition[0], playerPosition[1], playerPosition[2]);
            }
        }

        private float[] Forward()
        {
            return new float[]
            {
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(pitch),
                (float)(Math.Cos(pitch) * Math.Cos(yaw)),
            };
        }

        private void WriteCamera(bool start = false)
        {
            float[] forward = Forward();
            float[] right = RightOf(forward);
            float[] up = Cross(forward, right);
            float[][] axes =
            {
                Scale(right, rightSign),
                Scale(up, upSign),
                Scale(forward, forwardSign),
            };
            byte[] matrices = sly4.ViewMatrices(axes, cameraPosition, viewW, cameraW);
            if (start)
            {
                game.StartViewOverride(view, matrices);
            }
            else
            {
                game.SetViewOverride(matrices);
            }
        }

        /// <summary>One of the camera's axes (0 X, 1 Y, 2 Z): a row of its matrix.</summary>
        private static float[] Axis(float[] m, int axis)
        {
            return new float[] { m[axis * 4], m[axis * 4 + 1], m[axis * 4 + 2] };
        }

        /// <summary>The game camera's facing: its Z axis, turned towards the player.</summary>
        private static float[] Facing(float[] m, float[] player)
        {
            float[] z = Axis(m, 2);
            float[] toPlayer = new float[] { player[0] - m[12], player[1] - m[13], player[2] - m[14] };
            return Dot(z, toPlayer) < 0 ? Scale(z, -1) : z;
        }

        /// <summary>R1 rises, L1 sinks.</summary>
        private static float Rise()
        {
            float rise = 0;
            if (Inputs.IsPressed(Inputs.Buttons.r1))
            {
                rise += 1;
            }
            if (Inputs.IsPressed(Inputs.Buttons.l1))
            {
                rise -= 1;
            }
            return rise;
        }

        private static float Stick(float value)
        {
            return Math.Abs(value) < DeadZone ? 0 : value;
        }

        /// <summary>Horizontal, perpendicular to the facing (Y is up).</summary>
        private static float[] RightOf(float[] forward)
        {
            return Normalize(new float[] { forward[2], 0, -forward[0] });
        }

        private static float[] Flatten(float[] v)
        {
            return Normalize(new float[] { v[0], 0, v[2] });
        }

        private static float[] Normalize(float[] v)
        {
            float length = (float)Math.Sqrt(Dot(v, v));
            return length < 1e-6f ? new float[] { 0, 0, 0 } : Scale(v, 1 / length);
        }

        private static float[] Scale(float[] v, float s)
        {
            return new float[] { v[0] * s, v[1] * s, v[2] * s };
        }

        private static float[] Subtract(float[] a, float[] b)
        {
            return new float[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }

        private static float Dot(float[] a, float[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        private static float[] Cross(float[] a, float[] b)
        {
            return new float[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0],
            };
        }
    }
}
