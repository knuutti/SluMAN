using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace SluMAN
{
    /// <summary>
    /// A small local HTTP API for scripts (e.g. Python) to drive the Sly 4 free camera, on
    /// 127.0.0.1 only, so nothing else on the network can reach it. Every request is a GET and
    /// every answer is JSON; angles are in degrees.
    /// <list type="bullet">
    /// <item><c>/camera</c>: the free camera: on, x, y, z, yaw, pitch, fov, far, and the game's
    /// own fov and far.</item>
    /// <item><c>/camera/set?x=&amp;y=&amp;z=&amp;yaw=&amp;pitch=&amp;fov=&amp;far=</c>: moves the free
    /// camera, turning it on first. Values left out stay as they are; far=0 is the game's.</item>
    /// <item><c>/camera/off</c>: turns the free camera off.</item>
    /// <item><c>/hud?hidden=1</c> or <c>0</c>, <c>/fog?off=1</c> or <c>0</c>: Hide HUD and No fog.</item>
    /// </list>
    /// </summary>
    public class Sly4CameraApi
    {
        public const int DefaultPort = 9675;
        private const string EnabledKey = "sly4CameraApi";
        private const string PortKey = "sly4CameraApiPort";
        private const int MaxRequestLineLength = 8192;

        private readonly sly4 game;
        private readonly Sly4FreeMove freeMove;
        private readonly object sync = new object();
        // One request at a time: they all drive the same camera.
        private readonly object requestLock = new object();
        private TcpListener listener;
        private volatile bool running;

        public Sly4CameraApi(sly4 game, Sly4FreeMove freeMove)
        {
            this.game = game;
            this.freeMove = freeMove;
        }

        public int Port { get; private set; } = DefaultPort;

        /// <summary>Why the API isn't running, or "" when it is (or is switched off).</summary>
        public string LastError { get; private set; } = "";

        public bool IsRunning => running;

        public string Url => $"http://127.0.0.1:{Port}/";

        /// <summary>Whether the API is on, remembered in config.txt. Turning it on starts it.</summary>
        public bool Enabled
        {
            get { return ReadConfig(EnabledKey) == "true"; }
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

        private static string ReadConfig(string key)
        {
            try
            {
                return func.GetConfigData("config.txt", key);
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
            if (!int.TryParse(ReadConfig(PortKey), out port) || port <= 0 || port >= 65536)
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
                thread.Name = "Sly 4 camera API";
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
                    return new Dictionary<string, object>
                    {
                        { "requests", new[]
                            {
                                "/camera",
                                "/camera/set?x=&y=&z=&yaw=&pitch=&fov=&far= (any of them; degrees; far=0 is the game's)",
                                "/camera/off",
                                "/hud?hidden=1|0",
                                "/fog?off=1|0",
                            }
                        },
                    };
                case "/camera":
                    return Camera();
                case "/camera/set":
                    SetCamera(values);
                    return Camera();
                case "/camera/off":
                    freeMove.Stop();
                    return Camera();
                case "/hud":
                    game.SetHudHidden(Flag(values, "hidden"));
                    return new Dictionary<string, object> { { "hidden", game.IsHudHidden() } };
                case "/fog":
                    bool off = Flag(values, "off");
                    if (!game.SetFogDisabled(off))
                    {
                        throw new InvalidOperationException("The game has no camera yet.");
                    }
                    return new Dictionary<string, object> { { "off", off } };
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
            bool on = freeMove.GetCamera(out position, out yaw, out pitch, out fov);
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
            camera["far"] = freeMove.FarDistance;
            camera["gameFov"] = CameraPath.Degrees(freeMove.GameFov);
            camera["gameFar"] = freeMove.GameFar;
            return camera;
        }

        private void SetCamera(Dictionary<string, string> values)
        {
            if (freeMove.Current != Sly4FreeMove.Mode.FreeCamera)
            {
                freeMove.Start(Sly4FreeMove.Mode.FreeCamera);
            }

            float[] position;
            float yaw;
            float pitch;
            float fov;
            if (!freeMove.GetCamera(out position, out yaw, out pitch, out fov))
            {
                throw new InvalidOperationException("The free camera didn't start.");
            }
            position[0] = Value(values, "x", position[0]);
            position[1] = Value(values, "y", position[1]);
            position[2] = Value(values, "z", position[2]);
            yaw = CameraPath.Radians(Value(values, "yaw", CameraPath.Degrees(yaw)));
            pitch = CameraPath.Radians(Value(values, "pitch", CameraPath.Degrees(pitch)));
            float fovDegrees = Value(values, "fov", CameraPath.Degrees(fov));
            if (fovDegrees <= 0)
            {
                throw new FormatException("fov must be above 0.");
            }

            if (values.ContainsKey("far"))
            {
                freeMove.FarDistance = Value(values, "far", 0);
            }
            freeMove.SetCamera(position, yaw, pitch, CameraPath.Radians(fovDegrees));
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
