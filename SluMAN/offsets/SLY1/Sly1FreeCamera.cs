using System;
using System.Diagnostics;
using System.Threading;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// The Sly 1 free camera, steered with the controller as read for the input display (so its
    /// subscriptions must be running): the left stick moves it, the right stick turns it, R1 and
    /// L1 move up and down, R2 and L2 zoom, R3 asks for a keyframe (KeyframeRequested). The player
    /// is made uncontrollable (so R1 / L1 don't open the binocucom either) and held where they
    /// stood. It can also play a camera path instead of following the sticks.
    /// <para>
    /// The camera is driven through the camera hook (sly1.StartCameraHook): the game's camera
    /// matrix update puts SluMAN's position, rotation, field of view and projection into the
    /// camera before
    /// working out the matrices it's drawn with, so the game's own camera never shows. SluMAN only
    /// writes them to the hook's data each tick. Every place the game sets the camera calls that
    /// update just after, so without the hook (on RPCS3 it needs the Interpreter PPU decoder)
    /// nothing SluMAN writes would show, and the camera stays the game's.
    /// </para>
    /// Runs on its own thread and keeps going whichever tab shows. Sly1Practice owns the one
    /// instance. It turns itself off when a load starts.
    /// </summary>
    public class Sly1FreeCamera : IFreeCamera
    {
        // How often the sticks move the camera (and the player is held).
        private const int Rpcs3IntervalMs = 5;
        private const int PS3IntervalMs = 30;
        private const float DeadZone = 0.2f;
        // Field of view limits, in radians: just above 0 and just below 180 degrees (0.0001 and
        // 179.9999), where the projection breaks down.
        public const float MinFov = 1.745e-6f;
        public const float MaxFov = 3.1415909f;
        // The field of view used when the game's can't be read, in radians.
        private const float DefaultFov = 1f;
        // Straight up and down: the camera's left comes from the yaw alone, so it's defined there.
        private const float MaxPitch = (float)(Math.PI / 2);
        // A tick after a stall moves at most this much time's worth.
        private const float MaxStep = 0.1f;
        // How long the camera hook gets to show it runs (its count goes up) before SluMAN says
        // it doesn't.
        private const int HookCheckMs = 500;

        private readonly sly1 game;
        private readonly int intervalMs;
        private readonly SynchronizationContext uiContext;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly object stateLock = new object();

        private bool on;
        private Thread worker;
        private volatile bool running;

        // The camera driven, and how it was when the free camera started: the W values and
        // projection are kept from it, and the field of view is worked out from it.
        private uint camera;
        private sly1.CameraState gameState;
        private float gameFov = DefaultFov;
        private float[] cameraPosition;
        private float yaw;
        private float pitch;
        private float fov;
        // False when the camera's field of view or frustum isn't one SluMAN can work from, so the
        // game's projection is kept.
        private bool fovKnown;
        // Which way the game's left and up rows point, as read when the camera started.
        private float leftSign = 1;
        private float upSign = 1;
        // Where the player is held, and where their position is, or 0 when there was no player.
        private float[] playerPosition;
        private uint playerPositionAddress;
        private bool characterControl;

        // The path being played (a copy, so editing the list doesn't touch it), or null.
        private CameraPath playingPath;
        private bool loopPlayback;
        private float playTime;
        private bool r3WasPressed;

        // Whether the camera hook has been seen running, and whether SluMAN said it hasn't.
        private bool hookConfirmed;
        private bool hookWarned;
        private readonly Stopwatch sinceHook = new Stopwatch();

        /// <summary>How fast the camera moves: units per second at full stick (the PS2 freecam's 1x).</summary>
        public float CameraSpeed { get; set; } = 1600f;

        /// <summary>How fast the camera turns: radians per second at full stick.</summary>
        public float LookSpeed { get; set; } = 2f;

        /// <summary>How fast R2 / L2 zoom: the field of view changes by this factor of e per second.</summary>
        public float ZoomSpeed = 0.7f;

        /// <summary>Create on the UI thread, so the events are raised there.</summary>
        public Sly1FreeCamera(sly1 game)
        {
            this.game = game;
            intervalMs = game.api is RPCS3 ? Rpcs3IntervalMs : PS3IntervalMs;
            uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        /// <summary>Raised on the UI thread when the camera stops by itself: an error or a load.</summary>
        public event Action<string> Stopped;

        /// <summary>Raised on the UI thread with news about the camera hook: whether it runs.</summary>
        public event Action<string, bool> Notice;

        /// <summary>Raised on the UI thread when the camera is turned on or off.</summary>
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
            if (!on)
            {
                Start();
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

        public bool IsOn => on;

        /// <summary>Whether the free camera sets the field of view (see fovKnown).</summary>
        public bool FovKnown
        {
            get
            {
                lock (stateLock)
                {
                    return fovKnown;
                }
            }
        }

        /// <summary>The game's field of view when the camera started, in radians.</summary>
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

        /// <summary>The free camera's field of view in radians. Applies at once while it's on.</summary>
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
                    if (on && fovKnown)
                    {
                        WriteCamera();
                    }
                }
            }
        }

        /// <summary>
        /// While on, the camera stays where it is and the player isn't held: the controller moves
        /// the character as usual. Turning it off holds the player where they are then.
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
                    if (on)
                    {
                        if (!value)
                        {
                            ReadPlayer();
                        }
                        try { game.SetPlayerControlBlocked(!value); } catch { }
                    }
                }
            }
        }

        /// <summary>
        /// The camera's position, yaw, pitch and field of view (radians; 0 when the game's is
        /// left alone, see FovKnown), or false when it's off.
        /// </summary>
        public bool GetCamera(out float[] position, out float cameraYaw, out float cameraPitch, out float cameraFov)
        {
            lock (stateLock)
            {
                if (!on)
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
                cameraFov = fovKnown ? fov : 0;
                return true;
            }
        }

        /// <summary>
        /// Puts the camera somewhere (stopping any playback), or false when it's off. A field of
        /// view of 0 is the game's.
        /// </summary>
        public bool SetCamera(float[] position, float cameraYaw, float cameraPitch, float cameraFov)
        {
            lock (stateLock)
            {
                if (!on)
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

        /// <summary>Starts driving the main camera, from where it is now.</summary>
        public void Start()
        {
            Stop();
            if (game.IsLevelLoading())
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }
            uint target = game.GetMainCamera();
            if (target == 0)
            {
                throw new InvalidOperationException("The game has no camera yet.");
            }

            lock (stateLock)
            {
                StartCamera(target);
                ReadPlayer();
                on = true;
            }

            clock.Restart();
            running = true;
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 1 free camera";
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
                if (on)
                {
                    Unhook();
                }
                on = false;
                playingPath = null;
            }
            RaiseModeChanged();
        }

        /// <summary>
        /// Takes the camera hook out and gives the game back its field of view as it was when the
        /// free camera started: the hook had put SluMAN's in, and the game only works out its
        /// projection again when its field of view changes.
        /// </summary>
        private void Unhook()
        {
            try { game.StopCameraHook(); } catch { }
            try { game.SetPlayerControlBlocked(false); } catch { }
            try
            {
                // After a reset the camera may be somewhere else.
                if (game.GetMainCamera() == camera)
                {
                    game.WriteCameraFov(camera, gameState);
                }
            }
            catch
            {
                // Not connected; the game sets its field of view again when it next changes it.
            }
        }

        private void PostNotice(string message, bool isError)
        {
            uiContext.Post((state) =>
            {
                Action<string, bool> handler = Notice;
                if (handler != null)
                {
                    handler(message, isError);
                }
            }, null);
        }

        private void StartCamera(uint target)
        {
            sly1.CameraState state = game.ReadCamera(target);
            float[] forward = Row(state.rotation, 0);

            camera = target;
            gameState = state;
            cameraPosition = new float[] { state.position[0], state.position[1], state.position[2] };
            yaw = (float)Math.Atan2(forward[1], forward[0]);
            pitch = (float)Math.Asin(Math.Max(-1, Math.Min(1, forward[2])));
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));
            leftSign = Dot(Row(state.rotation, 1), Left()) < 0 ? -1 : 1;
            upSign = Dot(Row(state.rotation, 2), Cross(Forward(), Left())) < 0 ? -1 : 1;

            fovKnown = state.fov >= MinFov && state.fov <= MaxFov && state.Clone().SetFov(state, state.fov);
            gameFov = fovKnown ? state.fov : DefaultFov;
            fov = gameFov;

            game.StartCameraHook(camera, BuildState());
            hookConfirmed = false;
            hookWarned = false;
            sinceHook.Restart();
        }

        /// <summary>Reads where the player stands, to hold them there.</summary>
        private void ReadPlayer()
        {
            try
            {
                playerPositionAddress = game.GetPlayerPositionAddress();
                playerPosition = playerPositionAddress == 0 ? null : game.ReadPositionAt(playerPositionAddress);
            }
            catch
            {
                playerPositionAddress = 0;
                playerPosition = null;
            }
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
                string stopReason;
                lock (stateLock)
                {
                    stopReason = Tick();
                }
                if (stopReason != null)
                {
                    running = false;
                    lock (stateLock)
                    {
                        Unhook();
                        on = false;
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

        /// <summary>One step; returns why the camera has to stop, or null to carry on.</summary>
        private string Tick()
        {
            float step = Math.Min((float)clock.Elapsed.TotalSeconds, MaxStep);
            clock.Restart();
            try
            {
                if (game.IsLevelLoading())
                {
                    return "the free camera turns off while a level loads.";
                }

                if (playingPath != null)
                {
                    TickPlayback(step);
                }
                else if (!characterControl)
                {
                    TickSticks(step);
                }
                // With character control the camera stays put (or keeps playing its path).
                // Otherwise the controller doesn't reach the player, and they're kept where they
                // stood. The game gives the controller back to the player whenever its state
                // changes, so that's taken away again every tick.
                if (!characterControl)
                {
                    game.SetPlayerControlBlocked(true);
                    if (playerPosition != null)
                    {
                        game.HoldPlayerAt(playerPositionAddress, playerPosition);
                    }
                }
                CheckHook();
                WriteCamera();
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
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

        private void TickSticks(float step)
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

            // Along the camera's own axes: the stick along its facing and right, R1 / L1 along
            // its up.
            float[] forward = Forward();
            float[] left = Left();
            float[] up = Cross(forward, left);
            float move = -Stick(Inputs.ly);
            float strafe = Stick(Inputs.lx);
            float rise = 0;
            if (Inputs.IsPressed(Inputs.Buttons.r1))
            {
                rise += 1;
            }
            if (Inputs.IsPressed(Inputs.Buttons.l1))
            {
                rise -= 1;
            }
            for (int axis = 0; axis < 3; axis++)
            {
                cameraPosition[axis] += (forward[axis] * move - left[axis] * strafe + up[axis] * rise) * CameraSpeed * step;
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
        }

        /// <summary>The free camera as the camera hook puts it in: the game's, with SluMAN's position, rotation and field of view.</summary>
        private sly1.CameraState BuildState()
        {
            sly1.CameraState state = gameState.Clone();
            float[] forward = Forward();
            float[] left = Left();
            float[][] rows =
            {
                forward,
                Scale(left, leftSign),
                Scale(Cross(forward, left), upSign),
            };
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    state.rotation[row * 4 + column] = rows[row][column];
                }
            }
            for (int axis = 0; axis < 3; axis++)
            {
                state.position[axis] = cameraPosition[axis];
            }
            if (fovKnown)
            {
                state.SetFov(gameState, fov);
            }
            return state;
        }

        private void WriteCamera()
        {
            game.SetCameraHookState(BuildState());
        }

        /// <summary>
        /// Once the camera hook has had time to run, checks that it did, and says so either way.
        /// The game doesn't update its camera while paused, so it's checked until it has run.
        /// </summary>
        private void CheckHook()
        {
            if (hookConfirmed || sinceHook.ElapsedMilliseconds < HookCheckMs)
            {
                return;
            }
            if (game.CameraHookCount() > 0)
            {
                hookConfirmed = true;
                PostNotice("Free camera on, through the camera hook: the game's camera stays out.", false);
                return;
            }
            if (!hookWarned)
            {
                hookWarned = true;
                PostNotice(game.api is RPCS3
                    ? "The camera hook isn't running, so the camera stays the game's: set RPCS3's PPU decoder to Interpreter (static), and unpause the game."
                    : "The camera hook isn't running yet, so the camera stays the game's. Unpause the game.", true);
            }
        }

        /// <summary>Where the camera looks. Z is up.</summary>
        private float[] Forward()
        {
            return new float[]
            {
                (float)(Math.Cos(pitch) * Math.Cos(yaw)),
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(pitch),
            };
        }

        /// <summary>Horizontal, to the left of the facing, from the yaw alone.</summary>
        private float[] Left()
        {
            return new float[] { (float)-Math.Sin(yaw), (float)Math.Cos(yaw), 0 };
        }

        private static float ClampFov(float value)
        {
            return Math.Max(MinFov, Math.Min(MaxFov, value));
        }

        private static float Stick(float value)
        {
            return Math.Abs(value) < DeadZone ? 0 : value;
        }

        /// <summary>One of the rotation's rows (0 forward, 1 left, 2 up).</summary>
        private static float[] Row(float[] rotation, int row)
        {
            return new float[] { rotation[row * 4], rotation[row * 4 + 1], rotation[row * 4 + 2] };
        }

        private static float[] Scale(float[] v, float s)
        {
            return new float[] { v[0] * s, v[1] * s, v[2] * s };
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
