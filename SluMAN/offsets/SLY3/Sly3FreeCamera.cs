using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// The Sly 3 free camera, steered with the controller as read for the input display (so its
    /// subscriptions must be running): the left stick moves it, the right stick turns it, R1 and
    /// L1 move up and down, R2 and L2 zoom. The player is held where they stood.
    /// <para>
    /// No game code is patched: the camera's matrix, and the copies of it the game draws from
    /// (cameraDrawMatrixOffset in the camera struct, cameraDrawMatrix in the game's data), are
    /// written from SluMAN, each in one write, faster than the game's frame rate. The game copies
    /// its own camera into them once a frame, so a write that lands after that is the one drawn. On RPCS3 that's nearly every frame; on a PS3, writes are slower
    /// and the game's camera shows through more often.
    /// </para>
    /// Runs on its own thread and keeps going whichever tab shows. Sly3Practice owns the one
    /// instance. It turns itself off when a load starts.
    /// </summary>
    public class Sly3FreeCamera
    {
        private const int Rpcs3IntervalMs = 2;
        private const int PS3IntervalMs = 30;
        private const float DeadZone = 0.2f;
        // Field of view limits and the game's usual one (the PS2 freecam's), in radians.
        public const float MinFov = 0.01f;
        public const float MaxFov = 3.0f;
        private const float DefaultFov = 1.1f;
        // Just short of straight up and down, where the yaw would stop meaning anything.
        private const float MaxPitch = 1.5586f;
        // A tick after a stall moves at most this much time's worth.
        private const float MaxStep = 0.1f;

        private readonly sly3 game;
        private readonly int intervalMs;
        private readonly SynchronizationContext uiContext;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly object stateLock = new object();

        private bool on;
        private Thread worker;
        private volatile bool running;

        // The camera driven, where its matrix is in it, and the game's field of view when the
        // free camera started.
        private uint camera;
        private int matrixOffset;
        private float gameFov = DefaultFov;
        private float[] cameraPosition;
        private float yaw;
        private float pitch;
        private float fov;
        // False when the camera's field of view isn't in radians as on the PS2 version, so it's
        // left to the game.
        private bool fovKnown;
        // Which way the game's Y (left) and Z (up) axes point, as read when the camera started,
        // and the W values of the matrix rows, written back as they were.
        private float leftSign = 1;
        private float upSign = 1;
        private float[] rowW;
        // Where the player is held, or null when there was none.
        private float[] playerPosition;
        private bool characterControl;

        // Other copies of the camera's position the game draws from (found with Find camera),
        // written with the camera each tick. A copy that's the last row of a camera matrix gets
        // the whole matrix, with its own W values; otherwise only the position.
        private uint[] copyPositions = new uint[0];
        private float[][] copyRowW = new float[0][];

        /// <summary>How fast the camera moves: units per second at full stick (the PS2 freecam's 1x).</summary>
        public float CameraSpeed = 1600f;

        /// <summary>How fast the camera turns: radians per second at full stick.</summary>
        public float LookSpeed = 2f;

        /// <summary>How fast R2 / L2 zoom: the field of view changes by this factor of e per second.</summary>
        public float ZoomSpeed = 0.7f;

        /// <summary>Create on the UI thread, so the events are raised there.</summary>
        public Sly3FreeCamera(sly3 game)
        {
            this.game = game;
            intervalMs = game.api is RPCS3 ? Rpcs3IntervalMs : PS3IntervalMs;
            uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        /// <summary>Raised on the UI thread when the camera stops by itself: an error or a load.</summary>
        public event Action<string> Stopped;

        /// <summary>Raised on the UI thread when the camera is turned on or off.</summary>
        public event Action ModeChanged;

        public bool IsOn => on;

        /// <summary>
        /// More copies of the camera's position to write with it (each the address of the
        /// position's 3 floats), on top of the ones the game draws from (cameraDrawMatrixOffset,
        /// cameraDrawMatrix). Takes effect the next time the camera starts.
        /// </summary>
        public uint[] Copies = new uint[0];

        /// <summary>How many copies are written as whole matrices, while the camera is on.</summary>
        public int MatrixCopyCount
        {
            get
            {
                lock (stateLock)
                {
                    int count = 0;
                    foreach (float[] w in copyRowW)
                    {
                        if (w != null)
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }
        }

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

        /// <summary>The camera being driven, or 0 when off.</summary>
        public uint Camera
        {
            get
            {
                lock (stateLock)
                {
                    return on ? camera : 0;
                }
            }
        }

        /// <summary>Where the driven camera's matrix is in its struct, or -1 when off.</summary>
        public int MatrixOffset
        {
            get
            {
                lock (stateLock)
                {
                    return on ? matrixOffset : -1;
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
                        game.WriteCameraFov(camera, fov);
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
                    if (!value && on)
                    {
                        try
                        {
                            playerPosition = game.GetPlayerPosition();
                        }
                        catch
                        {
                            playerPosition = null;
                        }
                    }
                }
            }
        }

        /// <summary>The camera's position, yaw and pitch (radians), or false when it's off.</summary>
        public bool GetCamera(out float[] position, out float cameraYaw, out float cameraPitch)
        {
            lock (stateLock)
            {
                if (!on)
                {
                    position = null;
                    cameraYaw = 0;
                    cameraPitch = 0;
                    return false;
                }
                position = (float[])cameraPosition.Clone();
                cameraYaw = yaw;
                cameraPitch = pitch;
                return true;
            }
        }

        /// <summary>Puts the camera somewhere, or false when it's off.</summary>
        public bool SetCamera(float[] position, float cameraYaw, float cameraPitch)
        {
            lock (stateLock)
            {
                if (!on)
                {
                    return false;
                }
                cameraPosition = (float[])position.Clone();
                yaw = cameraYaw;
                pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, cameraPitch));
                WriteCamera();
                return true;
            }
        }

        /// <summary>Starts driving a camera (the main one when 0), from where it is now.</summary>
        public void Start(uint cameraToDrive = 0)
        {
            Stop();
            if (game.IsLoading)
            {
                throw new InvalidOperationException("Wait for the level to finish loading.");
            }
            uint target = cameraToDrive != 0 ? cameraToDrive : game.GetMainCamera();
            if (target == 0)
            {
                throw new InvalidOperationException("The game has no camera yet.");
            }

            lock (stateLock)
            {
                StartCamera(target);
                try
                {
                    playerPosition = game.GetPlayerPosition();
                }
                catch
                {
                    playerPosition = null;
                }
                on = true;
            }

            clock.Restart();
            running = true;
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "Sly 3 free camera";
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
                }
                on = false;
            }
            RaiseModeChanged();
        }

        private void StartCamera(uint target)
        {
            int offset = game.FindCameraMatrix(target);
            if (offset < 0)
            {
                throw new InvalidOperationException($"Found no camera matrix in the camera at 0x{target:X}, so SluMAN can't move it.");
            }
            float[] m = game.ReadCameraMatrix(target, offset);
            float[] forward = Axis(m, 0);

            camera = target;
            matrixOffset = offset;
            cameraPosition = new float[] { m[12], m[13], m[14] };
            rowW = new float[] { m[3], m[7], m[11], m[15] };
            yaw = (float)Math.Atan2(forward[1], forward[0]);
            pitch = (float)Math.Asin(Math.Max(-1, Math.Min(1, forward[2])));
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));
            leftSign = Dot(Axis(m, 1), Left()) < 0 ? -1 : 1;
            upSign = Dot(Axis(m, 2), Cross(Forward(), Left())) < 0 ? -1 : 1;

            // The copies the game draws from, then any more found with Find camera.
            List<uint> copies = new List<uint>();
            copies.Add(target + sly3.addr.cameraDrawMatrixOffset + 0x30);
            copies.Add(sly3.addr.cameraDrawMatrix + 0x30);
            foreach (uint copy in Copies)
            {
                if (!copies.Contains(copy) && copy != target + (uint)offset + 0x30)
                {
                    copies.Add(copy);
                }
            }
            copyPositions = copies.ToArray();
            copyRowW = new float[copyPositions.Length][];
            for (int i = 0; i < copyPositions.Length; i++)
            {
                // A copy gets the whole matrix only when it's the same matrix as the camera's, so
                // nothing that just happens to share the position is turned.
                uint matrix = copyPositions[i] - 0x30;
                if (game.IsCameraMatrixAt(matrix))
                {
                    float[] copy = game.ReadMatrixAt(matrix);
                    if (SameRotation(copy, m))
                    {
                        copyRowW[i] = new float[] { copy[3], copy[7], copy[11], copy[15] };
                    }
                }
            }

            float cameraFov = game.ReadCameraFov(target);
            fovKnown = cameraFov >= MinFov && cameraFov <= MaxFov;
            gameFov = fovKnown ? cameraFov : DefaultFov;
            fov = gameFov;
            WriteCamera();
        }

        /// <summary>Lets the game's camera ease back to its own field of view.</summary>
        private void GiveBackFov()
        {
            if (fovKnown)
            {
                try { game.WriteCameraFov(camera, gameFov); } catch { }
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
                        on = false;
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

                if (!characterControl)
                {
                    TickSticks(step);
                    if (playerPosition != null)
                    {
                        game.SetPlayerPosition(playerPosition);
                    }
                }
                // The game writes its own camera every frame, so the free one is written every
                // tick, also while it stays put.
                WriteCamera();
                if (fovKnown)
                {
                    game.WriteCameraFov(camera, fov);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void TickSticks(float step)
        {
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

        private void WriteCamera()
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
            game.WriteCameraMatrix(camera, matrixOffset, m);

            for (int i = 0; i < copyPositions.Length; i++)
            {
                if (copyRowW[i] == null)
                {
                    game.WritePositionAt(copyPositions[i], cameraPosition);
                    continue;
                }
                float[] copy = (float[])m.Clone();
                for (int row = 0; row < 4; row++)
                {
                    copy[row * 4 + 3] = copyRowW[i][row];
                }
                game.WriteMatrixAt(copyPositions[i] - 0x30, copy);
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
