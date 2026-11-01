using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SluMAN
{
    /// <summary>
    /// The skin editor's side of <see cref="ObsPadServer"/>: the list of skins and saving one.
    /// <para>
    /// Any web page the user opens can send requests to 127.0.0.1, so a save is only taken when
    /// the request comes from the editor page itself: the Host must be this server (stops DNS
    /// rebinding), an Origin, if sent, must be too, and it must carry the
    /// <see cref="EditorHeader"/> header, which a page elsewhere can't send without a CORS
    /// preflight that this server never answers. The answers carry no
    /// Access-Control-Allow-Origin, so other pages can't read them either.
    /// </para>
    /// </summary>
    public static class SkinEditorApi
    {
        public const string EditorHeader = "X-SluMAN-Editor";

        /// <summary>The largest request body taken: a 16 MB sprite sheet in base64 and the parts.</summary>
        private const int MaxBodyBytes = 24 * 1024 * 1024;
        private const int MaxImageBytes = 16 * 1024 * 1024;
        private const int MaxImageSide = 8192;
        private const int MaxPitch = 1000;
        /// <summary>How far outside the base a part may be drawn; far more than any skin needs.</summary>
        private const int MaxDrawOffset = 20000;

        private static readonly byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>What the editor sends to /skins/save.</summary>
        private class SaveRequest
        {
            public string name = null;
            public bool overwrite = false;
            public bool select = false;
            public int analogPitch = 32;
            public Dictionary<string, InputPlot> parts = null;
            /// <summary>The sprite sheet, a PNG, in base64.</summary>
            public string png = null;
        }

        /// <summary>GET /skins: every skin, and whether it's the user's own.</summary>
        public static void WriteSkinList(Stream stream)
        {
            List<object> skins = new List<object>();
            foreach (string skinName in ControllerSkin.SkinNames())
            {
                skins.Add(new { name = skinName, user = ControllerSkin.IsUserSkin(skinName) });
            }
            WriteJson(stream, "200 OK", new { skins = skins, selected = ObsPadServer.SelectedSkin });
        }

        /// <summary>POST /skins/save.</summary>
        public static void HandleSave(Stream stream, Dictionary<string, string> headers, int port)
        {
            // The body is read before anything is answered: closing a connection with data
            // still unread resets it, and the page would see a network error instead of why.
            string lengthText;
            int length;
            if (!headers.TryGetValue("Content-Length", out lengthText) || !int.TryParse(lengthText, out length) || length < 0)
            {
                WriteError(stream, "411 Length Required", "bad-request", "The request has no Content-Length.");
                return;
            }
            if (length > MaxBodyBytes)
            {
                WriteError(stream, "413 Payload Too Large", "too-large", $"The skin is too large: the sprite sheet can be at most {MaxImageBytes / (1024 * 1024)} MB.");
                Discard(stream, length);
                return;
            }

            byte[] body = ReadBody(stream, length);
            if (body == null)
            {
                // The page went away before sending everything.
                return;
            }

            string refusal = RequestProblem(headers, port);
            if (refusal != null)
            {
                WriteError(stream, "403 Forbidden", "forbidden", refusal);
                return;
            }

            SaveRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<SaveRequest>(Encoding.UTF8.GetString(body));
            }
            catch (Exception ex)
            {
                WriteError(stream, "400 Bad Request", "bad-request", $"The request couldn't be read: {ex.Message}");
                return;
            }
            if (request == null)
            {
                WriteError(stream, "400 Bad Request", "bad-request", "The request is empty.");
                return;
            }

            string skinName = request.name == null ? "" : request.name;
            string nameProblem = ControllerSkin.NameProblem(skinName);
            if (nameProblem != null)
            {
                WriteError(stream, "400 Bad Request", "name", nameProblem);
                return;
            }
            if (ControllerSkin.IsShippedName(skinName))
            {
                WriteError(stream, "409 Conflict", "shipped", $"SluMAN comes with a skin named \"{skinName}\". Give yours another name.");
                return;
            }
            if (!request.overwrite && Directory.Exists(Path.Combine(ControllerSkin.UserSkinsFolder, skinName)))
            {
                WriteError(stream, "409 Conflict", "exists", $"You already have a skin named \"{skinName}\".");
                return;
            }

            byte[] png;
            try
            {
                png = Convert.FromBase64String(request.png == null ? "" : request.png);
            }
            catch (FormatException)
            {
                WriteError(stream, "400 Bad Request", "image", "The sprite sheet isn't valid base64.");
                return;
            }

            int width, height;
            string imageProblem = PngProblem(png, out width, out height);
            if (imageProblem != null)
            {
                WriteError(stream, "400 Bad Request", "image", imageProblem);
                return;
            }

            if (request.analogPitch < 0 || request.analogPitch > MaxPitch)
            {
                WriteError(stream, "400 Bad Request", "parts", $"The stick distance must be between 0 and {MaxPitch}.");
                return;
            }

            string partsProblem = PartsProblem(request.parts, width, height);
            if (partsProblem != null)
            {
                WriteError(stream, "400 Bad Request", "parts", partsProblem);
                return;
            }

            try
            {
                ControllerSkin.Save(skinName, png, request.analogPitch, request.parts, request.select);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skin editor: couldn't save \"{skinName}\": {ex}");
                WriteError(stream, "500 Internal Server Error", "write", $"Couldn't save the skin: {ex.Message}");
                return;
            }

            func.Status($"Saved the input display skin \"{skinName}\".");
            WriteJson(stream, "200 OK", new { ok = true, name = skinName });
        }

        /// <summary>Why the request isn't taken as the editor's, or null when it is.</summary>
        private static string RequestProblem(Dictionary<string, string> headers, int port)
        {
            string host;
            headers.TryGetValue("Host", out host);
            string[] ownHosts = { $"127.0.0.1:{port}", $"localhost:{port}" };
            if (host == null || !ownHosts.Contains(host.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return "Saving only works from the skin editor page that SluMAN serves.";
            }

            string origin;
            if (headers.TryGetValue("Origin", out origin) && !ownHosts.Any(own => string.Equals(origin.Trim(), "http://" + own, StringComparison.OrdinalIgnoreCase)))
            {
                return "Saving only works from the skin editor page that SluMAN serves.";
            }

            string editor;
            if (!headers.TryGetValue(EditorHeader, out editor) || editor.Trim() != "1")
            {
                return "Saving only works from the skin editor page that SluMAN serves.";
            }
            return null;
        }

        private static byte[] ReadBody(Stream stream, int length)
        {
            byte[] body = new byte[length];
            int read = 0;
            while (read < length)
            {
                int count = stream.Read(body, read, length - read);
                if (count <= 0)
                {
                    return null;
                }
                read += count;
            }
            return body;
        }

        /// <summary>
        /// Reads and drops what's left of a body too large to take, so the answer gets through,
        /// up to four times the limit; past that the connection is just closed.
        /// </summary>
        private static void Discard(Stream stream, int length)
        {
            if (length > 4 * MaxBodyBytes)
            {
                return;
            }
            byte[] buffer = new byte[64 * 1024];
            int left = length;
            try
            {
                while (left > 0)
                {
                    int count = stream.Read(buffer, 0, Math.Min(buffer.Length, left));
                    if (count <= 0)
                    {
                        return;
                    }
                    left -= count;
                }
            }
            catch (IOException)
            {
                // Gone, or too slow.
            }
        }

        /// <summary>Why <paramref name="png"/> can't be a sprite sheet, or null; its size from the PNG header.</summary>
        private static string PngProblem(byte[] png, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (png.Length > MaxImageBytes)
            {
                return $"The sprite sheet can be at most {MaxImageBytes / (1024 * 1024)} MB.";
            }
            // Signature, then the IHDR chunk: length, "IHDR", width, height.
            if (png.Length < 24 || !png.Take(8).SequenceEqual(pngSignature) || Encoding.ASCII.GetString(png, 12, 4) != "IHDR")
            {
                return "The sprite sheet must be a PNG.";
            }
            width = BitConverter.ToInt32(png.Skip(16).Take(4).Reverse().ToArray(), 0);
            height = BitConverter.ToInt32(png.Skip(20).Take(4).Reverse().ToArray(), 0);
            if (width <= 0 || height <= 0 || width > MaxImageSide || height > MaxImageSide)
            {
                return $"The sprite sheet can be at most {MaxImageSide} × {MaxImageSide} pixels.";
            }
            return null;
        }

        /// <summary>Why the parts can't make a skin, or null.</summary>
        private static string PartsProblem(Dictionary<string, InputPlot> parts, int width, int height)
        {
            if (parts == null || !parts.ContainsKey("base"))
            {
                return "The skin needs a base.";
            }

            List<string> known = ControllerSkin.PartNames();
            foreach (KeyValuePair<string, InputPlot> part in parts)
            {
                if (!known.Contains(part.Key))
                {
                    return $"\"{part.Key}\" isn't a part a skin can have.";
                }

                InputPlot plot = part.Value;
                if (plot.spriteWidth <= 0 || plot.spriteHeight <= 0)
                {
                    return $"{part.Key} has no size.";
                }
                // In longs, so huge numbers can't wrap around into the image.
                if (plot.spriteX < 0 || plot.spriteY < 0
                    || (long)plot.spriteX + plot.spriteWidth > width || (long)plot.spriteY + plot.spriteHeight > height)
                {
                    return $"{part.Key} goes outside the sprite sheet ({width} × {height}).";
                }
                if (Math.Abs((long)plot.drawX) > MaxDrawOffset || Math.Abs((long)plot.drawY) > MaxDrawOffset)
                {
                    return $"{part.Key} is placed too far from the base.";
                }
            }
            return null;
        }

        private static void WriteError(Stream stream, string status, string reason, string message)
        {
            WriteJson(stream, status, new { ok = false, reason = reason, error = message });
        }

        private static void WriteJson(Stream stream, string status, object value)
        {
            ObsPadServer.WriteResponse(stream, status, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)), false);
        }
    }
}
