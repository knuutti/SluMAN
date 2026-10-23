using System;
using System.Diagnostics;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Direct-render-matrix free camera for the researched Sly 2 KOR build. The renderer matrix
    /// is rewritten continuously because the game can replace it during a frame.
    /// </summary>
    public class Sly2FreeCamera
    {
        private const int PS3IntervalMs = 30;
        private const float DeadZone = 0.2f;
        private const float MaxPitch = 1.5586f;
        private const float MaxStep = 0.1f;

        private readonly sly2 game;
        private readonly int intervalMs;
        private readonly bool rewriteNonstop;
        private readonly object stateLock = new object();
        private Thread worker;
        private volatile bool running;
        private bool on;
        private float[] cameraPosition;
        private float yaw;
        private float pitch;
        private float[] rowW;
        private float[] playerPosition;
        private bool characterControl;

        public float CameraSpeed = 1600f;
        public float LookSpeed = 2f;

        public Sly2FreeCamera(sly2 game)
        {
            this.game = game;
            rewriteNonstop = game.api is Memory.RPCS3;
            intervalMs = PS3IntervalMs;
        }

        public event Action<string> Stopped;
        public event Action ModeChanged;

        public bool IsOn
        {
            get { lock (stateLock) { return on; } }
        }

        public bool CharacterControl
        {
            get { lock (stateLock) { return characterControl; } }
            set
            {
                lock (stateLock)
                {
                    characterControl = value;
                    if (!value && on)
                    {
                        playerPosition = game.GetPlayerPosition();
                    }
                }
            }
        }

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

        public void Start()
        {
            Stop();
            if (game.IsLoading)
            {
                throw new InvalidOperationException("The camera cannot start during a load.");
            }
            float[] matrix = game.ReadRenderCameraMatrix();
            float[] position = new float[] { matrix[12], matrix[13], matrix[14] };
            float[] forward = Axis(matrix, 0);
            float[] left = Axis(matrix, 1);
            float[] up = Axis(matrix, 2);
            lock (stateLock)
            {
                cameraPosition = position;
                yaw = (float)Math.Atan2(forward[1], forward[0]);
                pitch = (float)Math.Asin(Math.Max(-1, Math.Min(1, forward[2])));
                rowW = new float[] { matrix[3], matrix[7], matrix[11], matrix[15] };
                playerPosition = characterControl ? null : game.GetPlayerPosition();
                on = true;
                running = true;
            }
            if (rewriteNonstop)
            {
                try
                {
                    game.StartCameraHook();
                }
                catch
                {
                    lock (stateLock)
                    {
                        on = false;
                        running = false;
                    }
                    throw;
                }
            }
            worker = new Thread(Work);
            worker.IsBackground = true;
            worker.Start();
            Raise(ModeChanged);
        }

        public void Stop()
        {
            running = false;
            Thread thread = worker;
            if (thread != null && thread != Thread.CurrentThread)
            {
                thread.Join(500);
            }
            lock (stateLock)
            {
                if (!on)
                {
                    return;
                }
                on = false;
                if (playerPosition != null)
                {
                    try { game.SetPlayerPosition(playerPosition); } catch { }
                    playerPosition = null;
                }
            }
            if (rewriteNonstop)
            {
                game.StopCameraHook();
            }
            Raise(ModeChanged);
        }

        private void Work()
        {
            Stopwatch clock = Stopwatch.StartNew();
            long previous = clock.ElapsedMilliseconds;
            try
            {
                while (running)
                {
                    long now = clock.ElapsedMilliseconds;
                    float step = Math.Min(MaxStep, Math.Max(0, (now - previous) / 1000f));
                    previous = now;
                    lock (stateLock)
                    {
                        if (!on)
                        {
                            return;
                        }
                        if (game.IsLoading)
                        {
                            running = false;
                            on = false;
                            playerPosition = null;
                            if (rewriteNonstop)
                            {
                                game.StopCameraHook();
                            }
                            Raise(Stopped, "The camera stopped because the game started loading.");
                            Raise(ModeChanged);
                            return;
                        }
                        Move(step);
                        WriteCamera();
                        if (!characterControl && playerPosition != null)
                        {
                            game.SetPlayerPosition(playerPosition);
                        }
                    }
                    if (!rewriteNonstop)
                    {
                        Thread.Sleep(intervalMs);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (stateLock)
                {
                    on = false;
                    running = false;
                    playerPosition = null;
                }
                if (rewriteNonstop)
                {
                    try { game.StopCameraHook(); } catch { }
                }
                Raise(Stopped, ex.Message);
                Raise(ModeChanged);
            }
        }

        private void Move(float step)
        {
            yaw -= Stick(Inputs.rx) * LookSpeed * step;
            pitch -= Stick(Inputs.ry) * LookSpeed * step;
            pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, pitch));
            float[] forward = Forward();
            float[] left = Left();
            float[] up = Cross(forward, left);
            float move = -Stick(Inputs.ly);
            float strafe = Stick(Inputs.lx);
            float rise = 0;
            if (Inputs.IsPressed(Inputs.Buttons.r1)) rise += 1;
            if (Inputs.IsPressed(Inputs.Buttons.l1)) rise -= 1;
            for (int axis = 0; axis < 3; axis++)
            {
                cameraPosition[axis] += (forward[axis] * move - left[axis] * strafe + up[axis] * rise) * CameraSpeed * step;
            }
        }

        private void WriteCamera()
        {
            float[] forward = Forward();
            float[] left = Left();
            float[] up = Cross(forward, left);
            float[][] axes = { forward, left, up, cameraPosition };
            float[] matrix = new float[16];
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    matrix[row * 4 + column] = axes[row][column];
                }
                matrix[row * 4 + 3] = rowW[row];
            }
            // The renderer matrix is rebuilt from the global camera matrix. Rewrite both layers
            // continuously; writing only the renderer copy produces occasional flashes.
            game.WriteCameraMatrix(matrix);
            game.WriteRenderCameraMatrix(matrix);
        }

        private float[] Forward()
        {
            return new float[]
            {
                (float)(Math.Cos(pitch) * Math.Cos(yaw)),
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(pitch),
            };
        }

        private float[] Left()
        {
            return new float[] { (float)-Math.Sin(yaw), (float)Math.Cos(yaw), 0 };
        }

        private static float Stick(float value)
        {
            return Math.Abs(value) < DeadZone ? 0 : value;
        }

        private static float[] Axis(float[] matrix, int axis)
        {
            return new float[] { matrix[axis * 4], matrix[axis * 4 + 1], matrix[axis * 4 + 2] };
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

        private static void Raise(Action action)
        {
            if (action != null) action();
        }

        private static void Raise(Action<string> action, string message)
        {
            if (action != null) action(message);
        }
    }
}
