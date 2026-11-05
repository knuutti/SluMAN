using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace SluMAN
{
    /// <summary>
    /// Serves the input display as a web page for an OBS Browser Source, on 127.0.0.1 only, so it
    /// needs no firewall rule and nothing else on the network can reach it.
    /// <list type="bullet">
    /// <item><c>/pad</c>: the page (ObsPad.html, embedded in SluMAN.exe).</item>
    /// <item><c>/skin.json</c>, <c>/skin.png</c>: the skin's layout and sprite sheet.</item>
    /// <item><c>/events</c>: the buttons and sticks as server-sent events.</item>
    /// <item><c>/editor</c>, <c>/skins</c>, <c>POST /skins/save</c>: the skin editor
    /// (SkinEditor.html, embedded too) and its <see cref="SkinEditorApi"/>.</item>
    /// </list>
    /// Each address takes <c>?skin=&lt;folder&gt;</c>; without it the skin chosen in the Input Display
    /// is used.
    /// </summary>
    public static class ObsPadServer
    {
        public const int DefaultPort = 9674;
        private const string EnabledKey = "obsPadEnabled";
        private const string PortKey = "obsPadPort";

        private const int MaxEventClients = 8;
        // About 60 updates a second at most, and only when something changed.
        private const int EventIntervalMs = 16;
        // Sent even when nothing changes, so the page can tell SluMAN is still there.
        private const int KeepAliveMs = 1000;
        private const int MaxRequestLineLength = 8192;
        private const int MaxHeaderLines = 100;

        private static readonly object sync = new object();
        private static TcpListener listener;
        private static volatile bool running;
        private static int eventClients;

        public static int Port { get; private set; } = DefaultPort;

        /// <summary>Why the server isn't running, or "" when it is (or is switched off).</summary>
        public static string LastError { get; private set; } = "";

        public static bool IsRunning => running;

        public static string Url => $"http://127.0.0.1:{Port}/pad";

        /// <summary>The skin editor page.</summary>
        public static string EditorUrl => $"http://127.0.0.1:{Port}/editor";

        /// <summary>
        /// Opens the skin editor in the browser, with the named skin open, or the Open Skin dialog
        /// when skinName is null. It's served by the same server as the OBS page, so that is
        /// switched on first if it's off. What happened goes to status (message, is error).
        /// </summary>
        public static void OpenSkinEditor(string skinName, Action<string, bool> status)
        {
            if (!IsRunning)
            {
                Enabled = true;
                if (!IsRunning)
                {
                    status(LastError, true);
                    return;
                }
                status("Turned Serve for OBS on: the skin editor runs on the same server.", false);
            }

            string url = skinName == null ? EditorUrl : EditorUrl + "?skin=" + Uri.EscapeDataString(skinName);
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                status($"Couldn't open the browser ({ex.Message}). Open {url} yourself.", true);
            }
        }

        /// <summary>
        /// The skin chosen in the Input Display. Kept here so the event stream doesn't read
        /// config.txt 60 times a second.
        /// </summary>
        public static volatile string SelectedSkin = "";

        public static bool Enabled
        {
            get { return ReadConfig(EnabledKey) != "false"; }
            set
            {
                WriteConfig(value ? "true" : "false", EnabledKey);
                if (value)
                {
                    Start(ConfiguredPort);
                }
                else
                {
                    Stop();
                    LastError = "";
                }
            }
        }

        public static int ConfiguredPort
        {
            get
            {
                int port;
                if (int.TryParse(ReadConfig(PortKey), out port) && port > 0 && port < 65536)
                {
                    return port;
                }
                return DefaultPort;
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
                // Not remembered; the server still does what was asked.
            }
        }

        /// <summary>Starts the server if the setting is on. Called once when SluMAN starts.</summary>
        public static void StartFromConfig()
        {
            SelectedSkin = ControllerSkin.SelectedSkinName();
            if (Enabled)
            {
                Start(ConfiguredPort);
            }
        }

        /// <summary>Uses another port from now on, restarting the server if it's on.</summary>
        public static bool ChangePort(int port)
        {
            WriteConfig(port.ToString(), PortKey);
            Port = port;
            return !Enabled || Start(port);
        }

        /// <summary>
        /// Listens on 127.0.0.1:<paramref name="port"/>. Returns false, with the reason in
        /// <see cref="LastError"/>, when the port can't be used.
        /// </summary>
        public static bool Start(int port)
        {
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
                    LastError = $"Port {port} is in use by another program. Choose another port.";
                    Console.WriteLine($"OBS input display: couldn't listen on {port}: {ex.Message}");
                    return false;
                }

                LastError = "";
                running = true;
                TcpListener current = listener;
                Thread thread = new Thread(() => AcceptLoop(current));
                thread.IsBackground = true;
                thread.Name = "OBS input display";
                thread.Start();
                Console.WriteLine($"OBS input display: serving {Url}");
                return true;
            }
        }

        public static void Stop()
        {
            lock (sync)
            {
                StopLocked();
            }
        }

        private static void StopLocked()
        {
            running = false;
            if (listener != null)
            {
                try { listener.Stop(); } catch { }
                listener = null;
            }
        }

        private static void AcceptLoop(TcpListener current)
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

        private static void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000;

                    string requestLine = ReadLine(stream);
                    if (requestLine == null)
                    {
                        return;
                    }
                    Dictionary<string, string> headers = ReadHeaders(stream);

                    string[] parts = requestLine.Split(' ');
                    if (parts.Length < 2)
                    {
                        WriteResponse(stream, "400 Bad Request", "text/plain", Encoding.UTF8.GetBytes("Bad request."), false);
                        return;
                    }

                    string method = parts[0];
                    string path = parts[1];
                    string query = "";
                    int queryStart = path.IndexOf('?');
                    if (queryStart >= 0)
                    {
                        query = path.Substring(queryStart + 1);
                        path = path.Substring(0, queryStart);
                    }

                    if (method == "POST" && path == "/skins/save")
                    {
                        SkinEditorApi.HandleSave(stream, headers, Port);
                        return;
                    }
                    if (method != "GET")
                    {
                        // No CORS headers: a preflight from another site's page fails here.
                        WriteResponse(stream, "405 Method Not Allowed", "text/plain", Encoding.UTF8.GetBytes("Only GET is supported here."), false);
                        return;
                    }

                    string requestedSkin = QueryValue(query, "skin");
                    // Only a real skin folder; anything else falls back to the chosen skin.
                    string skinName = ControllerSkin.Exists(requestedSkin) ? requestedSkin : SelectedSkin;

                    switch (path)
                    {
                        case "/":
                        case "/pad":
                            WriteResponse(stream, "200 OK", "text/html; charset=utf-8", ResourceBytes("ObsPad.html"));
                            break;
                        case "/editor":
                            WriteResponse(stream, "200 OK", "text/html; charset=utf-8", ResourceBytes("SkinEditor.html"), false);
                            break;
                        case "/skins":
                            SkinEditorApi.WriteSkinList(stream);
                            break;
                        case "/skin.json":
                            WriteSkinJson(stream, skinName);
                            break;
                        case "/skin.png":
                            WriteSkinImage(stream, skinName);
                            break;
                        case "/events":
                            StreamEvents(stream);
                            break;
                        case "/favicon.ico":
                            // Browsers ask for one; answer quietly instead of logging a 404.
                            WriteResponse(stream, "204 No Content", "image/x-icon", new byte[0]);
                            break;
                        default:
                            WriteResponse(stream, "404 Not Found", "text/plain", Encoding.UTF8.GetBytes("Not found. The input display is at /pad."));
                            break;
                    }
                }
                catch (IOException)
                {
                    // The browser went away.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OBS input display: {ex.Message}");
                }
            }
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

        /// <summary>The request's headers, by name in any case. A repeated header keeps its last value.</summary>
        private static Dictionary<string, string> ReadHeaders(Stream stream)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < MaxHeaderLines; i++)
            {
                string header = ReadLine(stream);
                if (header == null || header.Length == 0)
                {
                    break;
                }
                int colon = header.IndexOf(':');
                if (colon > 0)
                {
                    headers[header.Substring(0, colon).Trim()] = header.Substring(colon + 1).Trim();
                }
            }
            return headers;
        }

        private static string QueryValue(string query, string key)
        {
            foreach (string pair in query.Split('&'))
            {
                int equals = pair.IndexOf('=');
                if (equals > 0 && pair.Substring(0, equals) == key)
                {
                    return Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' '));
                }
            }
            return "";
        }

        /// <summary>
        /// Sends a whole response. <paramref name="anyOrigin"/> lets pages elsewhere read it: fine
        /// for the OBS page's files, not for the skin editor's.
        /// </summary>
        public static void WriteResponse(Stream stream, string status, string contentType, byte[] body, bool anyOrigin = true)
        {
            string headers = $"HTTP/1.1 {status}\r\n"
                + $"Content-Type: {contentType}\r\n"
                + $"Content-Length: {body.Length}\r\n"
                + "Cache-Control: no-store\r\n"
                + (anyOrigin ? "Access-Control-Allow-Origin: *\r\n" : "")
                + "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static readonly Dictionary<string, byte[]> resources = new Dictionary<string, byte[]>();

        /// <summary>A page embedded in SluMAN.exe, by its LogicalName.</summary>
        private static byte[] ResourceBytes(string name)
        {
            lock (resources)
            {
                byte[] bytes;
                if (!resources.TryGetValue(name, out bytes))
                {
                    using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                    using (MemoryStream copy = new MemoryStream())
                    {
                        resource.CopyTo(copy);
                        bytes = copy.ToArray();
                    }
                    resources[name] = bytes;
                }
                return bytes;
            }
        }

        private static void WriteSkinJson(Stream stream, string skinName)
        {
            ControllerSkin skin;
            try
            {
                skin = ControllerSkin.Load(skinName, false);
            }
            catch (Exception ex)
            {
                WriteResponse(stream, "404 Not Found", "text/plain", Encoding.UTF8.GetBytes($"Couldn't read the skin \"{skinName}\": {ex.Message}"));
                return;
            }

            InputPlot basePlot;
            skin.buttons.TryGetValue("base", out basePlot);

            string json = JsonConvert.SerializeObject(new
            {
                name = skin.name,
                analogPitch = skin.analogPitch,
                width = basePlot.spriteWidth,
                height = basePlot.spriteHeight,
                buttons = skin.buttons,
            });
            WriteResponse(stream, "200 OK", "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
        }

        private static void WriteSkinImage(Stream stream, string skinName)
        {
            try
            {
                ControllerSkin skin = ControllerSkin.Load(skinName, false);
                WriteResponse(stream, "200 OK", "image/png", File.ReadAllBytes(skin.imagePath));
            }
            catch (Exception ex)
            {
                WriteResponse(stream, "404 Not Found", "text/plain", Encoding.UTF8.GetBytes($"Couldn't read the skin image: {ex.Message}"));
            }
        }

        private static string CurrentState()
        {
            // Rounded so tiny stick noise doesn't send an update every frame. "rev" goes up when a
            // skin is saved, so a page showing it loads it again.
            return string.Format(CultureInfo.InvariantCulture,
                "{{\"mask\":{0},\"lx\":{1:0.000},\"ly\":{2:0.000},\"rx\":{3:0.000},\"ry\":{4:0.000},\"skin\":{5},\"rev\":{6}}}",
                Inputs.RawInputs, Inputs.lx, Inputs.ly, Inputs.rx, Inputs.ry, JsonConvert.ToString(SelectedSkin), ControllerSkin.Revision);
        }

        private static void StreamEvents(Stream stream)
        {
            if (Interlocked.Increment(ref eventClients) > MaxEventClients)
            {
                Interlocked.Decrement(ref eventClients);
                WriteResponse(stream, "503 Service Unavailable", "text/plain", Encoding.UTF8.GetBytes($"At most {MaxEventClients} sources can watch at once."));
                return;
            }

            try
            {
                byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n"
                    + "Content-Type: text/event-stream\r\n"
                    + "Cache-Control: no-store\r\n"
                    + "Access-Control-Allow-Origin: *\r\n"
                    + "Connection: keep-alive\r\n\r\n");
                stream.Write(headers, 0, headers.Length);
                stream.Flush();

                string lastState = null;
                DateTime lastSent = DateTime.MinValue;

                while (running)
                {
                    string state = CurrentState();
                    if (state != lastState || (DateTime.UtcNow - lastSent).TotalMilliseconds >= KeepAliveMs)
                    {
                        byte[] message = Encoding.UTF8.GetBytes("data: " + state + "\n\n");
                        stream.Write(message, 0, message.Length);
                        stream.Flush();
                        lastState = state;
                        lastSent = DateTime.UtcNow;
                    }
                    Thread.Sleep(EventIntervalMs);
                }
            }
            finally
            {
                Interlocked.Decrement(ref eventClients);
            }
        }
    }
}
