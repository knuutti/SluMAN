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
    /// override). The left stick moves it, the right stick turns it, R1 and L1 move up and down,
    /// R2 and L2 zoom (the field of view), R3 asks for a keyframe (KeyframeRequested). The player is held where they stood. It can
    /// also play a camera path instead of following the sticks.</item>
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
        // Field of view limits, in radians (10 and 150 degrees), and the game's usual one
        // (Camera.Default.FOV, 53.5 degrees) for when the view's can't be read.
        public const float MinFov = 1.745e-6f;
        public const float MaxFov = 2.618f;
        private const float DefaultFov = 0.9338f;
        // Straight up and down: the camera's right comes from the yaw alone, so it's defined there.
        private const float MaxPitch = (float)(Math.PI / 2);
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
        // Vertical field of view in radians, and the game's own when the free camera started.
        private float fov;
        private float gameFov = DefaultFov;
        // Far clipping distance; 0 keeps the game's. And the game's when the camera started.
        private float farDistance;
        private float gameFar;
        private float forwardSign;
        // The forward direction last worked out from the player, for when there's none (0: not yet).
        private static float learnedForwardSign;
        // Further than this, the player isn't what the game camera is following.
        private const float MaxPlayerDistance = 60f;
        private float rightSign;
        private float upSign;
        // The main view, and the 4th float of each row of its view and camera matrices, which
        // hold something else and are written back as they were.
        private uint view;
        private float[] viewW;
        private float[] cameraW;

        // The path being played (a copy, so editing the list doesn't touch it), or null.
        private CameraPath playingPath;
        private bool loopPlayback;
        private float playTime;
        private bool r3WasPressed;

        /// <summary>How fast fly mode moves the player: units per second at full stick.</summary>
        public float FlySpeed = 10f;

        /// <summary>How fast the free camera moves: units per second at full stick.</summary>
        public float CameraSpeed = 10f;

        /// <summary>How fast the free camera turns: radians per second at full stick.</summary>
        public float LookSpeed = 2f;

        /// <summary>
        /// How fast R2 / L2 zoom: the field of view changes by this factor of e per second at full
        /// press, so zooming is as smooth at 1 degree as at 60.
        /// </summary>
        public float ZoomSpeed = 0.7f;

        /// <summary>
        /// The free camera's vertical field of view in radians. Setting it while the camera is
        /// on applies it at once.
        /// </summary>
        public float Fov
        {
            get
            {
                lock (stateLock)
                {
                    return fov;
                }
            }
            set
            {
                lock (stateLock)
                {
                    fov = ClampFov(value);
                    if (mode == Mode.FreeCamera && playingPath == null)
                    {
                        WriteCamera();
                    }
                }
            }
        }

        private bool characterControl;

        /// <summary>
        /// While on, the free camera stays where it is (or keeps playing its path) and the player
        /// isn't held: the controller moves the character as usual. Turning it off holds the
        /// player where they are then.
        /// </summary>
        public bool CharacterControl
        {
            get
            {
                lock (stateLock)
                {
                    return characterControl;
                }
            }
            set
            {
                lock (stateLock)
                {
                    characterControl = value;
                    if (!value && mode == Mode.FreeCamera)
                    {
                        try
                        {
                            playerPosition = game.GetPosition();
                        }
                        catch
                        {
                            playerPosition = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The free camera's far clipping distance, or 0 for the game's own. Applies at once
        /// while the camera is on, and stays set when it's turned off and on.
        /// </summary>
        public float FarDistance
        {
            get
            {
                lock (stateLock)
                {
                    return farDistance;
                }
            }
            set
            {
                lock (stateLock)
                {
                    farDistance = Math.Max(0, value);
                    if (mode == Mode.FreeCamera && playingPath == null)
                    {
                        WriteCamera();
                    }
                }
            }
        }

        /// <summary>The game's far clipping distance when the free camera started.</summary>
        public float GameFar
        {
            get
            {
                lock (stateLock)
                {
                    return gameFar;
                }
            }
        }

        /// <summary>The game's field of view when the free camera started, in radians.</summary>
        public float GameFov
        {
            get
            {
                lock (stateLock)
                {
                    return gameFov;
                }
            }
        }

        private static float ClampFov(float value)
        {
            return Math.Max(MinFov, Math.Min(MaxFov, value));
        }

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

        /// <summary>Raised on the UI thread when R3 is pressed with the free camera on.</summary>
        public event Action KeyframeRequested;

        /// <summary>Raised on the UI thread when a path finishes playing (not when stopped).</summary>
        public event Action PlaybackFinished;

        public bool IsPlaying
        {
            get
            {
                lock (stateLock)
                {
                    return playingPath != null;
                }
            }
        }

        /// <summary>Seconds into the path being played.</summary>
        public float PlaybackTime
        {
            get
            {
                lock (stateLock)
                {
                    return playTime;
                }
            }
        }

        /// <summary>
        /// Plays a camera path, turning the free camera on first if needed. Needs a keyframe.
        /// </summary>
        public void Play(CameraPath path, bool loop)
        {
            if (path.Keyframes.Count == 0)
            {
                throw new InvalidOperationException("Add a keyframe first.");
            }
            if (mode != Mode.FreeCamera)
            {
                Start(Mode.FreeCamera);
            }

            CameraPath copy = new CameraPath();
            copy.Smooth = path.Smooth;
            foreach (CameraPath.Keyframe key in path.Keyframes)
            {
                copy.Keyframes.Add(new CameraPath.Keyframe
                {
                    x = key.x, y = key.y, z = key.z, yaw = key.yaw, pitch = key.pitch, fov = key.fov,
                    duration = key.duration, easing = key.easing
                });
            }
            lock (stateLock)
            {
                playingPath = copy;
                loopPlayback = loop;
                playTime = 0;
                ShowPathAt(0);
            }
        }

        public void StopPlayback()
        {
            lock (stateLock)
            {
                playingPath = null;
            }
        }

        /// <summary>The free camera's position, facing and field of view, or false when it's off.</summary>
        public bool GetCamera(out float[] position, out float cameraYaw, out float cameraPitch, out float cameraFov)
        {
            lock (stateLock)
            {
                if (mode != Mode.FreeCamera)
                {
                    position = null;
                    cameraYaw = 0;
                    cameraPitch = 0;
                    cameraFov = 0;
                    return false;
                }
                position = (float[])cameraPosition.Clone();
                cameraYaw = yaw;
                cameraPitch = pitch;
                cameraFov = fov;
                return true;
            }
        }

        /// <summary>
        /// Puts the free camera somewhere (stopping any playback), or false when it's off. A field
        /// of view of 0 is the game's.
        /// </summary>
        public bool SetCamera(float[] position, float cameraYaw, float cameraPitch, float cameraFov)
        {
            lock (stateLock)
            {
                if (mode != Mode.FreeCamera)
                {
                    return false;
                }
                playingPath = null;
                cameraPosition = (float[])position.Clone();
                yaw = cameraYaw;
                pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, cameraPitch));
                fov = ClampFov(cameraFov > 0 ? cameraFov : gameFov);
                WriteCamera();
                return true;
            }
        }

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
                playingPath = null;
            }
            RaiseModeChanged();
        }

        private void RaiseModeChanged()
        {
            Post(() => ModeChanged);
        }

        /// <summary>Raises an event on the UI thread. Reads the handler there, as it is then.</summary>
        private void Post(Func<Action> handlerOf)
        {
            uiContext.Post((state) =>
            {
                Action handler = handlerOf();
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
                        playingPath = null;
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

            // Starts at the game's own field of view.
            float viewFov = game.ReadViewFov(view);
            gameFov = viewFov >= MinFov && viewFov <= MaxFov ? viewFov : DefaultFov;
            fov = gameFov;
            gameFar = game.ReadViewFar(view);

            // Which way along its Z axis the camera looks. A game camera following the player looks
            // at them, which tells it; with no player near (hideout, title screen, a character the
            // level doesn't have), the direction learned last time is used, or -Z: the game's
            // projection matrix is OpenGL-style (m[11] = -1), so its cameras look down -Z.
            float[] z = Normalize(Axis(m, 2));
            forwardSign = learnedForwardSign != 0 ? learnedForwardSign : -1;
            if (player != null)
            {
                float[] toPlayer = Subtract(player, cameraPosition);
                float distance = (float)Math.Sqrt(Dot(toPlayer, toPlayer));
                float along = Dot(z, toPlayer);
                // Only when the player is close and clearly in front of or behind the camera.
                if (distance > 0.5f && distance < MaxPlayerDistance && Math.Abs(along) > distance * 0.5f)
                {
                    forwardSign = along < 0 ? -1 : 1;
                    learnedForwardSign = forwardSign;
                }
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
            if (playingPath != null)
            {
                TickPlayback(step);
            }
            else if (!characterControl)
            {
                TickCameraSticks(step);
            }
            // With character control the camera stays put: the view override keeps drawing from
            // the matrices last written.

            // Keep the player where they stood, so steering the camera doesn't walk them off.
            if (!characterControl && playerPosition != null)
            {
                game.HoldPlayer(playerPosition[0], playerPosition[1], playerPosition[2]);
            }
        }

        private void TickPlayback(float step)
        {
            float total = playingPath.TotalDuration;
            playTime += step;
            if (playTime >= total)
            {
                if (loopPlayback && total > 0)
                {
                    playTime %= total;
                }
                else
                {
                    playTime = total;
                    ShowPathAt(total);
                    playingPath = null;
                    Post(() => PlaybackFinished);
                    return;
                }
            }
            ShowPathAt(playTime);
        }

        private void ShowPathAt(float time)
        {
            float[] position = new float[3];
            float pathYaw;
            float pathPitch;
            float pathFov;
            playingPath.Evaluate(time, gameFov, position, out pathYaw, out pathPitch, out pathFov);
            cameraPosition = position;
            yaw = pathYaw;
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pathPitch));
            fov = ClampFov(pathFov);
            WriteCamera();
        }

        private void TickCameraSticks(float step)
        {
            // R3 asks the Camera tab for a keyframe here, once per press.
            bool r3 = Inputs.IsPressed(Inputs.Buttons.r3);
            if (r3 && !r3WasPressed)
            {
                Post(() => KeyframeRequested);
            }
            r3WasPressed = r3;

            yaw -= Stick(Inputs.rx) * LookSpeed * step;
            pitch -= Stick(Inputs.ry) * LookSpeed * step;
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));

            // Everything moves along the camera's own axes, as in most free cameras: the stick
            // along its facing and right, R1 / L1 along its up (towards the top of the screen
            // when looking straight down).
            float[] forward = Forward();
            float[] right = Scale(RightOfYaw(), rightSign);
            float[] up = Cross(forward, RightOfYaw());
            float move = -Stick(Inputs.ly);
            float strafe = Stick(Inputs.lx);
            float rise = Rise();
            for (int axis = 0; axis < 3; axis++)
            {
                cameraPosition[axis] += (forward[axis] * move + right[axis] * strafe + up[axis] * rise) * CameraSpeed * step;
            }

            // R2 zooms in, L2 out.
            float zoom = 0;
            if (Inputs.IsPressed(Inputs.Buttons.l2))
            {
                zoom += 1;
            }
            if (Inputs.IsPressed(Inputs.Buttons.r2))
            {
                zoom -= 1;
            }
            fov = ClampFov(fov * (float)Math.Exp(zoom * ZoomSpeed * step));
            WriteCamera();
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
            float[] right = RightOfYaw();
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
                game.StartViewOverride(view, matrices, fov, farDistance);
            }
            else
            {
                game.SetViewOverride(matrices, fov, farDistance);
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
        /// <summary>
        /// Horizontal and perpendicular to the facing, from the yaw alone: the same as RightOf the
        /// facing, but still defined when the camera looks straight up or down.
        /// </summary>
        private float[] RightOfYaw()
        {
            return new float[] { (float)Math.Cos(yaw), 0, (float)-Math.Sin(yaw) };
        }

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
