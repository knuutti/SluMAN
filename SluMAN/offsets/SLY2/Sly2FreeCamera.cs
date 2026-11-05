using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// The Sly 2 free camera, steered with the controller as read for the input display (so its
    /// subscriptions must be running): the left stick moves it, the right stick turns it, R1 and
    /// L1 move up and down, R2 and L2 zoom (the field of view, or the height shown while
    /// orthographic), R3 asks for a keyframe (KeyframeRequested). The player
    /// is made uncontrollable. It can also play a camera path instead of following the sticks.
    /// <para>
    /// The camera is driven through the camera hook (sly2.StartCameraHook): the game's world
    /// matrix update puts SluMAN's matrix into the camera before working out the matrix that's
    /// drawn, so the game's own camera never shows. SluMAN only writes the matrix to the hook's
    /// data each tick.
    /// </para>
    /// <para>
    /// When the hook can't run on a PS3, the camera's matrix and the copies the game draws from
    /// (cameraDrawMatrixOffset, cameraDrawMatrix) are written directly instead. The game copies
    /// its own camera into them just before drawing, so it shows through now and then. On RPCS3
    /// (where the hook needs the Interpreter PPU decoder) the free camera turns off instead: there
    /// the game's camera shows through too often to be of use.
    /// </para>
    /// Runs on its own thread and keeps going whichever tab shows. Sly2Practice owns the one
    /// instance. It turns itself off when a load starts.
    /// </summary>
    public class Sly2FreeCamera : IFreeCamera
    {
        // How often the sticks move the camera.
        private const int Rpcs3IntervalMs = 5;
        private const int PS3IntervalMs = 30;
        private const float DeadZone = 0.2f;
        // Field of view limits, in radians: just above 0 and just below 180 degrees (0.0001 and
        // 179.9999), where the projection breaks down.
        public const float MinFov = 1.745e-6f;
        public const float MaxFov = 3.1415909f;
        // The game's usual field of view (the PS2 freecam's), in radians.
        private const float DefaultFov = 1f;
        // Straight up and down: the camera's left comes from the yaw alone, so it's defined there.
        private const float MaxPitch = (float)(Math.PI / 2);
        // A tick after a stall moves at most this much time's worth.
        private const float MaxStep = 0.1f;
        // For when the camera's aspect ratio can't be read.
        private const float DefaultAspect = 16f / 9f;
        // Zooming in orthographically stops at this height, as the Camera tab's box does.
        private const float MinOrthoHeight = 0.01f;
        // The near distance when the game's can't be read.
        private const float DefaultNear = 10f;

        private readonly sly2 game;
        private readonly int intervalMs;
        // How long the camera hook gets to show it runs (its count goes up) before SluMAN falls
        // back to writing the camera directly (or, on RPCS3, turns it off).
        private const int HookCheckMs = 500;
        private readonly SynchronizationContext uiContext;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly object stateLock = new object();

        private bool on;
        private Thread worker;
        private volatile bool running;

        // The camera driven, where its matrix is in it, and the game's field of view when the
        // free camera started.
        private float gameFov = DefaultFov;
        private float[] cameraPosition;
        private float yaw;
        private float pitch;
        private float fov;
        // The free camera's render distance (0 for the game's), the game's when the camera
        // started, and the one last written (0 for none), so stopping only puts the game's back
        // over SluMAN's own.
        private float farDistance;
        private float gameFar;
        private float farWritten;
        // Orthographic: how many world units the view shows top to bottom (0 for perspective), the
        // near distance (0 for the game's), and the camera's aspect ratio and near distance when
        // it started.
        private float orthoHeight;
        private float orthoNear;
        private float aspect = DefaultAspect;
        private float gameNear = DefaultNear;
        // False when the camera's field of view isn't in radians as on the PS2 version, so it's
        // left to the game.
        private bool fovKnown;
        // Which way the game's Y (left) and Z (up) axes point, as read when the camera started,
        // and the W values of the matrix rows, written back as they were.
        private float leftSign = 1;
        private float upSign = 1;
        private float[] rowW;
        // The active character's controllable value before the camera changed it.
        private int playerControllable;
        private bool playerControllableKnown;
        private bool characterControl;

        // The path being played (a copy, so editing the list doesn't touch it), or null.
        private CameraPath playingPath;
        private bool loopPlayback;
        private float playTime;
        private bool r3WasPressed;

        // Whether the camera hook drives the camera, and whether it has been seen running.
        private bool hooked;
        private bool hookConfirmed;
        // The orthographic view last given to the hooked camera (width, height, near, far), or
        // null when it's perspective.
        private float[] orthoWritten;
        private readonly Stopwatch sinceHook = new Stopwatch();

        /// <summary>How fast the camera moves: units per second at full stick (the PS2 freecam's 1x).</summary>
        public float CameraSpeed { get; set; } = 1600f;

        /// <summary>How fast the camera turns: radians per second at full stick.</summary>
        public float LookSpeed { get; set; } = 2f;

        /// <summary>How fast R2 / L2 zoom: the field of view changes by this factor of e per second.</summary>
        public float ZoomSpeed = 0.7f;

        /// <summary>Create on the UI thread, so the events are raised there.</summary>
        public Sly2FreeCamera(sly2 game)
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

        /// <summary>
        /// The free camera's render distance, or 0 for the game's own. Applies at once while the
        /// camera is on, and stays set when it's turned off and on.
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
                    if (on)
                    {
                        WriteFar();
                    }
                }
            }
        }

        /// <summary>Whether this game version's free camera can be orthographic.</summary>
        public bool OrthoSupported => game.OrthoSupported;

        /// <summary>
        /// How many world units the free camera shows from the top of the screen to the bottom
        /// when it's orthographic, or 0 for perspective. Applies at once while the camera is on,
        /// and stays set when it's turned off and on. Needs the camera hook.
        /// </summary>
        public float OrthoHeight
        {
            get
            {
                lock (stateLock)
                {
                    return orthoHeight;
                }
            }
            set
            {
                if (value > 0 && !game.OrthoSupported)
                {
                    throw new InvalidOperationException("This game version has no orthographic camera.");
                }
                lock (stateLock)
                {
                    orthoHeight = Math.Max(0, value);
                    if (on)
                    {
                        try
                        {
                            WriteCamera();
                        }
                        catch
                        {
                            // Perspective, rather than failing again every tick.
                            orthoHeight = 0;
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The orthographic camera's near clipping distance, or 0 for the game's. Applies at once
        /// while the camera is on and orthographic.
        /// </summary>
        public float OrthoNear
        {
            get
            {
                lock (stateLock)
                {
                    return orthoNear;
                }
            }
            set
            {
                lock (stateLock)
                {
                    orthoNear = Math.Max(0, value);
                    if (on)
                    {
                        WriteCamera();
                    }
                }
            }
        }

        /// <summary>The game's render distance when the free camera started (0 if unknown).</summary>
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
                        game.WriteCameraFov(fov);
                    }
                }
            }
        }

        /// <summary>
        /// While on, the camera stays where it is and the controller moves the character as usual.
        /// Turning it off makes the character uncontrollable.
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
                    if (!value && on)
                    {
                        try
                        {
                            game.SetPlayerControllable(false);
                        }
                        catch
                        {
                        }
                    }
                    else if (value && on)
                    {
                        try
                        {
                            game.SetPlayerControllable(true);
                        }
                        catch
                        {
                        }
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
            if (game.IsLoading)
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }
            lock (stateLock)
            {
                StartCamera();
                try
                {
                    playerControllable = game.GetPlayerControllable();
                    playerControllableKnown = true;
                    game.SetPlayerControllable(characterControl);
                }
                catch
                {
                    playerControllableKnown = false;
                }
                on = true;
            }

            clock.Restart();
            running = true;
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 2 free camera";
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
                    GiveBackFov();
                    GiveBackFar();
                    Unhook();
                    RestorePlayerControllable();
                }
                on = false;
                playingPath = null;
            }
            RaiseModeChanged();
        }

        private void Unhook()
        {
            if (hooked)
            {
                try { game.StopCameraHook(); } catch { }
                hooked = false;
            }
        }

        /// <summary>
        /// Starts the camera hook, or leaves it off (writing the camera directly) and says why. On
        /// RPCS3 it throws instead, so the free camera doesn't start.
        /// </summary>
        private void Hook()
        {
            hooked = false;
            hookConfirmed = false;
            orthoWritten = null;
            try
            {
                game.StartCameraHook(BuildMatrix());
                hooked = true;
                sinceHook.Restart();
            }
            catch (Exception ex)
            {
                HookFailed(ex.Message);
            }
        }

        /// <summary>
        /// Leaves the camera written directly, and says why. On RPCS3 the game's camera shows
        /// through too often for that, so it throws instead.
        /// </summary>
        private void HookFailed(string reason)
        {
            if (game.api is RPCS3)
            {
                throw new InvalidOperationException(reason);
            }
            PostNotice($"{reason} The game's camera shows through now and then.", true);
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

        private void StartCamera()
        {
            float[] m = game.ReadCameraMatrix();
            float[] forward = Axis(m, 0);

            cameraPosition = new float[] { m[12], m[13], m[14] };
            rowW = new float[] { m[3], m[7], m[11], m[15] };
            yaw = (float)Math.Atan2(forward[1], forward[0]);
            pitch = (float)Math.Asin(Math.Max(-1, Math.Min(1, forward[2])));
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));
            leftSign = Dot(Axis(m, 1), Left()) < 0 ? -1 : 1;
            upSign = Dot(Axis(m, 2), Cross(Forward(), Left())) < 0 ? -1 : 1;

            float cameraFov = game.ReadCameraFov();
            fovKnown = cameraFov >= MinFov && cameraFov <= MaxFov;
            gameFov = fovKnown ? cameraFov : DefaultFov;
            fov = gameFov;
            gameFar = game.ReadRenderDistance();
            farWritten = 0;
            float cameraAspect = game.ReadCameraAspect();
            aspect = cameraAspect > 0.1f && cameraAspect < 10f ? cameraAspect : DefaultAspect;
            float cameraNear = game.ReadCameraNear();
            gameNear = cameraNear > 0 && cameraNear < 10000f ? cameraNear : DefaultNear;
            WriteFar();
            Hook();
            WriteCamera();
        }

        /// <summary>Writes the render distance set, or the game's when it's 0.</summary>
        private void WriteFar()
        {
            if (gameFar <= 0)
            {
                return;
            }
            float far = farDistance > 0 ? farDistance : gameFar;
            if (far != farWritten)
            {
                game.WriteRenderDistance(far);
                farWritten = far;
            }
        }

        /// <summary>
        /// Puts the game's render distance back, unless the game has set its own since (a load).
        /// </summary>
        private void GiveBackFar()
        {
            if (farWritten <= 0 || farWritten == gameFar)
            {
                return;
            }
            try
            {
                if (game.ReadRenderDistance() == farWritten)
                {
                    game.WriteRenderDistance(gameFar);
                }
            }
            catch { }
            farWritten = 0;
        }

        /// <summary>Lets the game's camera ease back to its own field of view.</summary>
        private void GiveBackFov()
        {
            if (fovKnown)
            {
                try { game.WriteCameraFov(gameFov); } catch { }
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
                        GiveBackFov();
                        Unhook();
                        RestorePlayerControllable();
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
                if (game.IsLoading)
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
                game.SetPlayerControllable(characterControl);
                CheckHook();
                // The game writes its own camera every frame, so the free one is written every
                // tick, also while it stays put.
                WriteCamera();
                if (fovKnown)
                {
                    game.WriteCameraFov(fov);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void RestorePlayerControllable()
        {
            if (playerControllableKnown)
            {
                try
                {
                    game.SetPlayerControllable(playerControllable != 0);
                }
                catch
                {
                }
                playerControllableKnown = false;
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
            float zoomFactor = (float)Math.Exp(zoom * ZoomSpeed * step);
            if (orthoHeight > 0 && hooked)
            {
                // The field of view does nothing while orthographic, so zoom changes what it shows.
                orthoHeight = Math.Max(MinOrthoHeight, orthoHeight * zoomFactor);
            }
            else
            {
                fov = ClampFov(fov * zoomFactor);
            }
        }

        /// <summary>The free camera's matrix, laid out as sly2.ReadCameraMatrix's.</summary>
        private float[] BuildMatrix()
        {
            float[] forward = Forward();
            float[] left = Left();
            float[][] axes =
            {
                forward,
                Scale(left, leftSign),
                Scale(Cross(forward, left), upSign),
                cameraPosition,
            };
            float[] m = new float[16];
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    m[row * 4 + column] = axes[row][column];
                }
                m[row * 4 + 3] = rowW[row];
            }
            return m;
        }

        private void WriteCamera()
        {
            float[] m = BuildMatrix();
            if (hooked)
            {
                game.SetCameraHookMatrix(m);
                WriteOrtho();
                return;
            }

            game.WriteCameraMatrix(m);
            game.WriteRenderCameraMatrix(m);
        }

        /// <summary>
        /// Makes the hooked camera orthographic while there's a height, or perspective again
        /// once it was orthographic.
        /// </summary>
        private void WriteOrtho()
        {
            if (orthoHeight > 0 && game.OrthoSupported)
            {
                float near = orthoNear > 0 ? orthoNear : gameNear;
                float far = farWritten > 0 ? farWritten : gameFar;
                if (far <= near)
                {
                    far = near + 1000000f;
                }
                float[] ortho = { orthoHeight * aspect, orthoHeight, near, far };
                if (orthoWritten == null || !ortho.SequenceEqual(orthoWritten))
                {
                    game.SetCameraHookOrtho(ortho[0], ortho[1], ortho[2], ortho[3]);
                    orthoWritten = ortho;
                }
            }
            else if (orthoWritten != null)
            {
                game.SetCameraHookOrtho(0, 0, 0, 0);
                orthoWritten = null;
            }
        }

        /// <summary>
        /// Once the camera hook has had time to run, checks that it did; if it didn't, falls back
        /// to writing the camera directly, or on RPCS3 throws, which turns the free camera off.
        /// </summary>
        private void CheckHook()
        {
            if (!hooked || hookConfirmed || sinceHook.ElapsedMilliseconds < HookCheckMs)
            {
                return;
            }
            if (game.CameraHookCount() > 0)
            {
                hookConfirmed = true;
                PostNotice("Free camera on, through the camera hook: the game's camera stays out.", false);
                return;
            }
            Unhook();
            if (game.api is RPCS3)
            {
                throw new InvalidOperationException("The camera hook isn't running: set RPCS3's PPU decoder to Interpreter (static) and start the free camera again.");
            }
            PostNotice("The camera hook isn't running, so the game's camera shows through now and then.", true);
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

        // How far two matrices' axes may differ and still be the same rotation.
        private const float SameRotationTolerance = 0.01f;

        private static bool SameRotation(float[] a, float[] b)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (Math.Abs(a[row * 4 + column] - b[row * 4 + column]) > SameRotationTolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>One of the matrix's axes (0 X, 1 Y, 2 Z): a row.</summary>
        private static float[] Axis(float[] m, int axis)
        {
            return new float[] { m[axis * 4], m[axis * 4 + 1], m[axis * 4 + 2] };
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
