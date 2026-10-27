using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SluMAN
{
    /// <summary>
    /// A small local HTTP API for scripts (e.g. Python) to drive a game's free camera, on
    /// 127.0.0.1 only, so nothing else on the network can reach it. Every request is a GET and
    /// every answer is JSON; angles are in degrees. It works from the game's CameraTabLayout, so
    /// every game with a Camera tab has it; the Practice window owns it and keeps it running
    /// whichever tab shows.
    /// <list type="bullet">
    /// <item><c>/</c>: the list of requests, and which axis is up.</item>
    /// <item><c>/camera</c>: the free camera: on, x, y, z, yaw, pitch, fov, the game's own fov,
    /// and far and gameFar for games with a far distance.</item>
    /// <item><c>/camera/set?x=&amp;y=&amp;z=&amp;yaw=&amp;pitch=&amp;fov=&amp;far=</c>: moves the free
    /// camera, turning it on first. Values left out stay as they are; fov=0 and far=0 are the game's.</item>
    /// <item><c>/camera/off</c>: turns the free camera off.</item>
    /// <item><c>/hud?hidden=1</c> or <c>0</c>, <c>/fog?off=1</c> or <c>0</c>: Hide HUD and No fog.</item>
    /// <item><c>/clock</c>, <c>/clock?frozen=1</c> or <c>0</c>: the Practice tab's Freeze Game Clock.</item>
    /// </list>
    /// </summary>
    public class CameraScriptApi
    {
        public const int DefaultPort = 9675;
        private const string EnabledKey = "cameraApi";
        private const string PortKey = "cameraApiPort";
        // Where Sly 4, the first game with the API, kept them.
        private const string OldEnabledKey = "sly4CameraApi";
        private const string OldPortKey = "sly4CameraApiPort";
        private const int MaxRequestLineLength = 8192;

        private readonly CameraTabLayout layout;
        private readonly IFreeCamera freeCamera;
        private readonly object sync = new object();
        // One request at a time: they all drive the same camera.
        private readonly object requestLock = new object();
        private TcpListener listener;
        private volatile bool running;

        /// <summary>Drives the layout's free camera, and puts itself on the layout's Camera tab.</summary>
        public CameraScriptApi(CameraTabLayout layout)
        {
            this.layout = layout;
            freeCamera = layout.freeCamera;
            layout.scriptApi = this;
        }

        /// <summary>The layout the API drives, for the Camera tab.</summary>
        public CameraTabLayout Layout => layout;

        public int Port { get; private set; } = DefaultPort;

        /// <summary>Why the API isn't running, or "" when it is (or is switched off).</summary>
        public string LastError { get; private set; } = "";

        public bool IsRunning => running;

        public string Url => $"http://127.0.0.1:{Port}/";

        /// <summary>Whether the API is on, remembered in config.txt. Turning it on starts it.</summary>
        public bool Enabled
        {
            get { return ReadConfig(EnabledKey, OldEnabledKey) == "true"; }
            set
            {
                WriteConfig(value ? "true" : "false", EnabledKey);
                if (value)
                {
                    Start();
                }
                else
                {
                    Stop();
                    LastError = "";
                }
            }
        }

        /// <summary>Starts the API if it was left on. Call when the Practice window opens.</summary>
        public void StartFromConfig()
        {
            if (Enabled)
            {
                Start();
            }
        }

        /// <summary>The setting, or the one it was kept under before if it isn't set.</summary>
        private static string ReadConfig(string key, string oldKey)
        {
            try
            {
                string value = func.GetConfigData("config.txt", key);
                if (string.IsNullOrEmpty(value))
                {
                    value = func.GetConfigData("config.txt", oldKey);
                }
                return value == null ? "" : value;
            }
            catch
            {
                return "";
            }
        }

        private static void WriteConfig(string value, string key)
        {
            try
            {
                func.ChangeFileLines("config.txt", value, key);
            }
            catch
            {
                // Not remembered; the API still does what was asked.
            }
        }

        public bool Start()
        {
            int port;
            if (!int.TryParse(ReadConfig(PortKey, OldPortKey), out port) || port <= 0 || port >= 65536)
            {
                port = DefaultPort;
            }

            lock (sync)
            {
                StopLocked();
                Port = port;
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                }
                catch (SocketException ex)
                {
                    listener = null;
                    LastError = $"Port {port} is in use by another program. Set {PortKey} in config.txt to another port.";
                    Console.WriteLine($"Camera API: couldn't listen on {port}: {ex.Message}");
                    return false;
                }

                LastError = "";
                running = true;
                TcpListener current = listener;
                Thread thread = new Thread(() => AcceptLoop(current));
                thread.IsBackground = true;
                thread.Name = "Camera API";
                thread.Start();
                return true;
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                StopLocked();
            }
        }

        private void StopLocked()
        {
            running = false;
            if (listener != null)
            {
                try { listener.Stop(); } catch { }
                listener = null;
            }
        }

        private void AcceptLoop(TcpListener current)
        {
            while (running && current == listener)
            {
                TcpClient client;
                try
                {
                    client = current.AcceptTcpClient();
                }
                catch
                {
                    // Stopped.
                    return;
                }

                Thread thread = new Thread(() => HandleClient(client));
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000;

                    string requestLine = ReadLine(stream);
                    string header;
                    do
                    {
                        header = ReadLine(stream);
                    }
                    while (header != null && header.Length > 0);

                    if (requestLine == null)
                    {
                        return;
                    }

                    string[] parts = requestLine.Split(' ');
                    if (parts.Length < 2 || parts[0] != "GET")
                    {
                        WriteJson(stream, "405 Method Not Allowed", Error("Only GET is supported."));
                        return;
                    }

                    string path = parts[1];
                    string query = "";
                    int queryStart = path.IndexOf('?');
                    if (queryStart >= 0)
                    {
                        query = path.Substring(queryStart + 1);
                        path = path.Substring(0, queryStart);
                    }

                    Dictionary<string, string> values = ParseQuery(query);
                    object answer;
                    string status = "200 OK";
                    try
                    {
                        lock (requestLock)
                        {
                            answer = Handle(path.TrimEnd('/'), values);
                        }
                        if (answer == null)
                        {
                            status = "404 Not Found";
                            answer = Error("Not found. Ask / for the list of requests.");
                        }
                    }
                    catch (FormatException ex)
                    {
                        status = "400 Bad Request";
                        answer = Error(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        status = "409 Conflict";
                        answer = Error(ex.Message);
                    }
                    WriteJson(stream, status, answer);
                }
                catch (IOException)
                {
                    // The script went away.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Camera API: {ex.Message}");
                }
            }
        }

        /// <summary>Answers one request, or null when the path isn't one.</summary>
        private object Handle(string path, Dictionary<string, string> values)
        {
            switch (path)
            {
                case "":
                    List<string> requests = new List<string>
                    {
                        "/camera",
                        layout.setFarDistance != null
                            ? "/camera/set?x=&y=&z=&yaw=&pitch=&fov=&far= (any of them; degrees; fov=0 and far=0 are the game's)"
                            : "/camera/set?x=&y=&z=&yaw=&pitch=&fov= (any of them; degrees; fov=0 is the game's)",
                        "/camera/off",
                    };
                    if (layout.setHudHidden != null)
                    {
                        requests.Add("/hud?hidden=1|0");
                    }
                    if (layout.setFogDisabled != null)
                    {
                        requests.Add("/fog?off=1|0");
                    }
                    if (layout.setClockFrozen != null)
                    {
                        requests.Add("/clock?frozen=1|0 (or none to read it)");
                    }
                    return new Dictionary<string, object>
                    {
                        { "requests", requests },
                        { "upAxis", layout.upAxis },
                    };
                case "/camera":
                    return Camera();
                case "/camera/set":
                    SetCamera(values);
                    return Camera();
                case "/camera/off":
                    freeCamera.Stop();
                    return Camera();
                case "/hud":
                    if (layout.setHudHidden == null)
                    {
                        throw new InvalidOperationException("This game can't hide the HUD.");
                    }
                    bool hidden = Flag(values, "hidden");
                    layout.setHudHidden(hidden);
                    return new Dictionary<string, object> { { "hidden", layout.isHudHidden != null ? layout.isHudHidden() : hidden } };
                case "/fog":
                    if (layout.setFogDisabled == null)
                    {
                        throw new InvalidOperationException("This game can't turn the fog off.");
                    }
                    bool off = Flag(values, "off");
                    if (!layout.setFogDisabled(off))
                    {
                        throw new InvalidOperationException("The game has no camera yet.");
                    }
                    return new Dictionary<string, object> { { "off", off } };
                case "/clock":
                    if (layout.setClockFrozen == null)
                    {
                        throw new InvalidOperationException("This game can't freeze the clock.");
                    }
                    if (values.ContainsKey("frozen"))
                    {
                        layout.setClockFrozen(Flag(values, "frozen"));
                    }
                    return new Dictionary<string, object> { { "frozen", layout.isClockFrozen() } };
                default:
                    return null;
            }
        }

        private Dictionary<string, object> Camera()
        {
            Dictionary<string, object> camera = new Dictionary<string, object>();
            float[] position;
            float yaw;
            float pitch;
            float fov;
            bool on = freeCamera.GetCamera(out position, out yaw, out pitch, out fov);
            camera["on"] = on;
            if (on)
            {
                camera["x"] = position[0];
                camera["y"] = position[1];
                camera["z"] = position[2];
                camera["yaw"] = CameraPath.Degrees(yaw);
                camera["pitch"] = CameraPath.Degrees(pitch);
                camera["fov"] = CameraPath.Degrees(fov);
            }
            if (freeCamera.FovKnown)
            {
                camera["gameFov"] = CameraPath.Degrees(freeCamera.GameFov);
            }
            if (layout.getFarDistance != null)
            {
                camera["far"] = layout.getFarDistance();
                camera["gameFar"] = layout.getGameFar();
            }
            return camera;
        }

        private void SetCamera(Dictionary<string, string> values)
        {
            if (!freeCamera.IsOn)
            {
                freeCamera.Start();
            }

            float[] position;
            float yaw;
            float pitch;
            float fov;
            if (!freeCamera.GetCamera(out position, out yaw, out pitch, out fov))
            {
                throw new InvalidOperationException("The free camera didn't start.");
            }
            position[0] = Value(values, "x", position[0]);
            position[1] = Value(values, "y", position[1]);
            position[2] = Value(values, "z", position[2]);
            yaw = CameraPath.Radians(Value(values, "yaw", CameraPath.Degrees(yaw)));
            pitch = CameraPath.Radians(Value(values, "pitch", CameraPath.Degrees(pitch)));
            // 0 is the game's; it's what a camera that couldn't read the game's gives back.
            float fovDegrees = Value(values, "fov", CameraPath.Degrees(fov));
            if (fovDegrees < 0)
            {
                throw new FormatException("fov can't be below 0.");
            }

            if (values.ContainsKey("far"))
            {
                if (layout.setFarDistance == null)
                {
                    throw new FormatException("This game has no far distance to set.");
                }
                layout.setFarDistance(Value(values, "far", 0));
            }
            freeCamera.SetCamera(position, yaw, pitch, CameraPath.Radians(fovDegrees));
        }

        private static float Value(Dictionary<string, string> values, string key, float current)
        {
            string text;
            if (!values.TryGetValue(key, out text) || text == "")
            {
                return current;
            }
            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new FormatException($"{key} must be a number, not \"{text}\".");
            }
            return value;
        }

        /// <summary>
        /// Lets /clock drive the Practice tab's Freeze Game Clock checkbox, so the box shows what
        /// a script set and the window's own cleanup (reapplying after a reset, unfreezing when it
        /// closes) covers it. <paramref name="apply"/> is the game's freeze; it runs first so a
        /// failure reaches the script, and ticking the box then applies it again.
        /// </summary>
        public static void BindClockToggle(CameraTabLayout layout, CheckBox toggle, Action<bool> apply)
        {
            layout.isClockFrozen = () => OnUiThread(toggle, () => toggle.Checked);
            layout.setClockFrozen = (frozen) => OnUiThread(toggle, () =>
            {
                apply(frozen);
                toggle.Checked = frozen;
                return frozen;
            });
        }

        /// <summary>Runs on the control's UI thread, passing its exception on to the request.</summary>
        private static T OnUiThread<T>(Control control, Func<T> action)
        {
            T result = default(T);
            Exception error = null;
            control.Invoke(new Action(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }));
            if (error != null)
            {
                throw new InvalidOperationException(error.Message);
            }
            return result;
        }

        private static bool Flag(Dictionary<string, string> values, string key)
        {
            string text;
            if (!values.TryGetValue(key, out text))
            {
                throw new FormatException($"Give {key}=1 or {key}=0.");
            }
            return text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, object> Error(string message)
        {
            return new Dictionary<string, object> { { "error", message } };
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in query.Split('&'))
            {
                int equals = pair.IndexOf('=');
                if (equals > 0)
                {
                    values[pair.Substring(0, equals)] = Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' '));
                }
            }
            return values;
        }

        private static string ReadLine(Stream stream)
        {
            StringBuilder line = new StringBuilder();
            while (line.Length < MaxRequestLineLength)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    return line.Length > 0 ? line.ToString() : null;
                }
                if (b == '\n')
                {
                    return line.ToString().TrimEnd('\r');
                }
                line.Append((char)b);
            }
            return line.ToString();
        }

        private static void WriteJson(Stream stream, string status, object answer)
        {
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(answer));
            string headers = $"HTTP/1.1 {status}\r\n"
                + "Content-Type: application/json; charset=utf-8\r\n"
                + $"Content-Length: {body.Length}\r\n"
                + "Cache-Control: no-store\r\n"
                + "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }
    }
}
