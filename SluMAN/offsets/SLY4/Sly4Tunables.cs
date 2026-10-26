using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SluMAN
{
    /// <summary>
    /// Sly 4's tunables: the ~2,650 named settings the debug menu's Tuning browser edits. The game
    /// keeps a registry of pointers to them, and each tunable struct holds its own type (vtable),
    /// name, name hash and value, so the whole list is read from the running game.
    ///
    /// Values that should stick ("Keep") are saved in sly4_tunables.cfg in the user data folder,
    /// in the debug menu's own debugmenu.cfg format, and written again on every connect.
    /// </summary>
    public class Sly4Tunables
    {
        public enum TunableType
        {
            unknown,
            boolean,
            integer,
            single,
            vector2,
            vector3,
            color,
            text,
            textHash,
        }

        public class Tunable
        {
            public string name;
            public uint address;
            public TunableType type;
        }

        // Tunable struct layout
        private const uint NameOffset = 0x4;
        private const int NameLength = 0x40;
        private const uint ValueOffset = 0x48;
        // A 3-float vector's value is 16-byte aligned, at +0x50 (the game's constructors store it
        // there and its code reads it there; the debug menu's +0x48 is wrong for these).
        private const uint Vector3ValueOffset = 0x50;
        private const int TextLength = 0x40;
        // Enough for the longest value, a 64-byte string.
        private const uint StructReadSize = ValueOffset + TextLength;

        private static uint ValueOffsetOf(TunableType type)
        {
            return type == TunableType.vector3 ? Vector3ValueOffset : ValueOffset;
        }

        // The most read at once: Ratchetron's receive buffer is about 2 KB.
        private const uint MaxReadSize = 0x800;

        // Anything above this is a bad read, not the registry.
        private const uint MaxTunableCount = 10000;

        private static string SavedFile => sly4.addr.IsHackpack ? "hackpack_tunables.cfg" : "sly4_tunables.cfg";

        private readonly IGame game;
        private List<Tunable> tunables;
        private Dictionary<string, Tunable> byName;

        // Name -> value as the cfg file writes it, in the order they were kept.
        private readonly List<KeyValuePair<string, string>> saved = new List<KeyValuePair<string, string>>();

        public Sly4Tunables(IGame game)
        {
            this.game = game;
            LoadSavedFile();
        }

        public static string SavedFilePath => UserData.Path(SavedFile);

        /// <summary>Every tunable, read from the game the first time it's asked for.</summary>
        public List<Tunable> GetAll()
        {
            if (tunables == null)
            {
                Scan();
            }
            return tunables;
        }

        public Tunable Find(string name)
        {
            GetAll();
            Tunable tunable;
            return byName.TryGetValue(name, out tunable) ? tunable : null;
        }

        /// <summary>
        /// Reads the registry. The tunables live at fixed addresses, so this only needs to run once
        /// per connection to a game version.
        /// </summary>
        public void Scan()
        {
            uint count = ReadUInt(sly4.addr.tunableCount);
            if (count == 0 || count > MaxTunableCount)
            {
                throw new InvalidOperationException("The tunable list isn't readable yet.");
            }

            byte[] pointers = ReadLarge(sly4.addr.tunableRegistry, count * 4);
            List<uint> addresses = new List<uint>();
            for (int i = 0; i < count; i++)
            {
                uint address = ToUInt(pointers, i * 4);
                if (address != 0)
                {
                    addresses.Add(address);
                }
            }

            List<Tunable> found = new List<Tunable>();
            Dictionary<string, Tunable> names = new Dictionary<string, Tunable>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<uint, byte[]> entry in ReadStructs(addresses))
            {
                byte[] data = entry.Value;
                string name = ReadText(data, (int)NameOffset, NameLength);
                if (name.Length == 0 || names.ContainsKey(name))
                {
                    continue;
                }
                Tunable tunable = new Tunable
                {
                    name = name,
                    address = entry.Key,
                    type = TypeOf(ToUInt(data, 0)),
                };
                found.Add(tunable);
                names[name] = tunable;
            }

            tunables = found.OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase).ToList();
            byName = names;
        }

        /// <summary>The values of the given tunables, formatted for display, keyed by tunable.</summary>
        public Dictionary<Tunable, string> ReadValues(IEnumerable<Tunable> list)
        {
            Dictionary<uint, Tunable> byAddress = new Dictionary<uint, Tunable>();
            foreach (Tunable tunable in list)
            {
                byAddress[tunable.address] = tunable;
            }

            Dictionary<Tunable, string> values = new Dictionary<Tunable, string>();
            foreach (KeyValuePair<uint, byte[]> entry in ReadStructs(byAddress.Keys))
            {
                Tunable tunable = byAddress[entry.Key];
                values[tunable] = Format(tunable.type, entry.Value, (int)ValueOffsetOf(tunable.type));
            }
            return values;
        }

        public string ReadValue(Tunable tunable)
        {
            byte[] data = game.api.ReadMemory(game.pid, tunable.address + ValueOffsetOf(tunable.type), (uint)ValueSize(tunable.type));
            return Format(tunable.type, data, 0);
        }

        /// <summary>
        /// Writes a value typed as it's displayed (e.g. "1.5, 0, 2" for a vector). Throws a
        /// FormatException when it doesn't parse for the tunable's type.
        /// </summary>
        public void WriteValue(Tunable tunable, string value)
        {
            byte[] bytes = Parse(tunable.type, value);
            game.api.WriteMemory(game.pid, tunable.address + ValueOffsetOf(tunable.type), bytes);
        }

        public bool IsKept(Tunable tunable)
        {
            return saved.Any(s => string.Equals(s.Key, tunable.name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Keeps a tunable at its value in the game right now, or forgets it.</summary>
        public void SetKept(Tunable tunable, bool keep)
        {
            saved.RemoveAll(s => string.Equals(s.Key, tunable.name, StringComparison.OrdinalIgnoreCase));
            if (keep)
            {
                saved.Add(new KeyValuePair<string, string>(tunable.name, ToCfgValue(tunable.type, ReadValue(tunable))));
            }
            WriteSavedFile();
        }

        public int KeptCount => saved.Count;

        /// <summary>
        /// Writes every kept value into the game. Returns how many were written; names the game
        /// doesn't have are skipped.
        /// </summary>
        public int ApplyKept()
        {
            if (saved.Count == 0)
            {
                return 0;
            }

            int applied = 0;
            foreach (KeyValuePair<string, string> entry in saved)
            {
                Tunable tunable = Find(entry.Key);
                if (tunable == null)
                {
                    continue;
                }
                try
                {
                    WriteValue(tunable, entry.Value);
                    applied++;
                }
                catch (FormatException)
                {
                    // A hand-edited line that doesn't parse; the rest still apply.
                }
            }
            return applied;
        }

        /// <summary>
        /// Writes and keeps every value in a debug menu debugmenu.cfg (or a sly4_tunables.cfg).
        /// Returns how many were applied; <paramref name="unknown"/> lists the names this game
        /// doesn't have or values that didn't parse.
        /// </summary>
        public int Import(string path, out List<string> unknown)
        {
            unknown = new List<string>();
            int applied = 0;
            foreach (KeyValuePair<string, string> entry in ReadCfg(path))
            {
                Tunable tunable = Find(entry.Key);
                if (tunable == null)
                {
                    unknown.Add(entry.Key);
                    continue;
                }
                try
                {
                    WriteValue(tunable, entry.Value);
                }
                catch (FormatException)
                {
                    unknown.Add(entry.Key);
                    continue;
                }
                saved.RemoveAll(s => string.Equals(s.Key, tunable.name, StringComparison.OrdinalIgnoreCase));
                saved.Add(new KeyValuePair<string, string>(tunable.name, entry.Value));
                applied++;
            }
            WriteSavedFile();
            return applied;
        }

        public static string TypeName(TunableType type)
        {
            switch (type)
            {
                case TunableType.boolean: return "bool";
                case TunableType.integer: return "int";
                case TunableType.single: return "float";
                case TunableType.vector2: return "vec2";
                case TunableType.vector3: return "vec3";
                case TunableType.color: return "color";
                case TunableType.text: return "string";
                case TunableType.textHash: return "hash";
                default: return "?";
            }
        }

        private static TunableType TypeOf(uint vtable)
        {
            Sly4Addresses addr = sly4.addr;
            if (vtable == addr.tunableVtableBool) return TunableType.boolean;
            if (vtable == addr.tunableVtableInt) return TunableType.integer;
            if (vtable == addr.tunableVtableFloat) return TunableType.single;
            if (vtable == addr.tunableVtableVec2) return TunableType.vector2;
            if (vtable == addr.tunableVtableVec3) return TunableType.vector3;
            if (vtable == addr.tunableVtableColor) return TunableType.color;
            if (vtable == addr.tunableVtableString) return TunableType.text;
            if (vtable == addr.tunableVtableStringHash) return TunableType.textHash;
            return TunableType.unknown;
        }

        private static int ValueSize(TunableType type)
        {
            switch (type)
            {
                case TunableType.boolean: return 1;
                case TunableType.vector2: return 8;
                case TunableType.vector3: return 12;
                case TunableType.text: return TextLength;
                default: return 4;
            }
        }

        private static string Format(TunableType type, byte[] data, int offset)
        {
            switch (type)
            {
                case TunableType.boolean:
                    return data[offset] != 0 ? "1" : "0";
                case TunableType.integer:
                    return ((int)ToUInt(data, offset)).ToString(CultureInfo.InvariantCulture);
                case TunableType.single:
                    return FormatFloat(ToFloat(data, offset));
                case TunableType.vector2:
                    return FormatFloat(ToFloat(data, offset)) + ", " + FormatFloat(ToFloat(data, offset + 4));
                case TunableType.vector3:
                    return FormatFloat(ToFloat(data, offset)) + ", " + FormatFloat(ToFloat(data, offset + 4)) + ", " + FormatFloat(ToFloat(data, offset + 8));
                case TunableType.color:
                    return $"{data[offset]}, {data[offset + 1]}, {data[offset + 2]}, {data[offset + 3]}";
                case TunableType.text:
                    return ReadText(data, offset, TextLength);
                default:
                    return "0x" + ToUInt(data, offset).ToString("X8");
            }
        }

        private static byte[] Parse(TunableType type, string value)
        {
            string text = value.Trim();
            switch (type)
            {
                case TunableType.boolean:
                    return new byte[] { (byte)(ParseBool(text) ? 1 : 0) };
                case TunableType.integer:
                    return ToBytes((uint)int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture));
                case TunableType.single:
                    return ToBytes(ParseFloat(text.Replace(',', '.')));
                case TunableType.vector2:
                case TunableType.vector3:
                    {
                        int count = type == TunableType.vector2 ? 2 : 3;
                        string[] parts = SplitList(text, count);
                        byte[] bytes = new byte[count * 4];
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(ToBytes(ParseFloat(parts[i])), 0, bytes, i * 4, 4);
                        }
                        return bytes;
                    }
                case TunableType.color:
                    {
                        string[] parts = SplitList(text, 4);
                        byte[] bytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            bytes[i] = byte.Parse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                        }
                        return bytes;
                    }
                case TunableType.text:
                    {
                        if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
                        {
                            text = text.Substring(1, text.Length - 2);
                        }
                        byte[] chars = Encoding.ASCII.GetBytes(text);
                        if (chars.Length > TextLength - 1)
                        {
                            throw new FormatException($"A string tunable holds at most {TextLength - 1} characters.");
                        }
                        // Padded with zeros, so the old string's tail doesn't show after a shorter one.
                        byte[] bytes = new byte[TextLength];
                        Array.Copy(chars, bytes, chars.Length);
                        return bytes;
                    }
                default:
                    {
                        string hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2) : text;
                        return ToBytes(uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    }
            }
        }

        /// <summary>A displayed value as debugmenu.cfg writes it, so the file stays readable by the menu.</summary>
        private static string ToCfgValue(TunableType type, string value)
        {
            switch (type)
            {
                case TunableType.vector2:
                case TunableType.vector3:
                case TunableType.color:
                    return value.Replace(" ", "");
                case TunableType.text:
                    return "\"" + value + "\"";
                default:
                    return value;
            }
        }

        private static bool ParseBool(string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "on":
                    return true;
                case "0":
                case "false":
                case "off":
                    return false;
                default:
                    throw new FormatException("A bool is 1 or 0.");
            }
        }

        private static float ParseFloat(string text)
        {
            return float.Parse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static string[] SplitList(string text, int count)
        {
            string[] parts = text.Split(',');
            if (parts.Length != count)
            {
                throw new FormatException($"Expected {count} values separated by commas.");
            }
            return parts.Select(p => p.Trim()).ToArray();
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private void LoadSavedFile()
        {
            saved.Clear();
            if (File.Exists(SavedFilePath))
            {
                saved.AddRange(ReadCfg(SavedFilePath));
            }
        }

        private void WriteSavedFile()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# Sly4 Debug Menu - Saved Settings");
            foreach (KeyValuePair<string, string> entry in saved)
            {
                text.AppendLine($"\"{entry.Key}\" = {entry.Value}");
            }
            File.WriteAllText(SavedFilePath, text.ToString());
        }

        /// <summary>Reads "Name" = value lines, the format the debug menu saves.</summary>
        private static List<KeyValuePair<string, string>> ReadCfg(string path)
        {
            List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }
                string name = line.Substring(0, equals).Trim().Trim('"');
                string value = line.Substring(equals + 1).Trim();
                if (name.Length > 0)
                {
                    entries.RemoveAll(e => string.Equals(e.Key, name, StringComparison.OrdinalIgnoreCase));
                    entries.Add(new KeyValuePair<string, string>(name, value));
                }
            }
            return entries;
        }

        /// <summary>
        /// Reads each struct at <paramref name="addresses"/>. Neighbouring structs are read together,
        /// so the whole registry takes a few hundred reads instead of thousands.
        /// </summary>
        private List<KeyValuePair<uint, byte[]>> ReadStructs(IEnumerable<uint> addresses)
        {
            List<uint> sorted = addresses.Distinct().OrderBy(a => a).ToList();
            List<KeyValuePair<uint, byte[]>> structs = new List<KeyValuePair<uint, byte[]>>();

            int index = 0;
            while (index < sorted.Count)
            {
                uint start = sorted[index];
                int last = index;
                while (last + 1 < sorted.Count && sorted[last + 1] + StructReadSize - start <= MaxReadSize)
                {
                    last++;
                }

                uint size = sorted[last] + StructReadSize - start;
                byte[] block = game.api.ReadMemory(game.pid, start, size);
                for (int i = index; i <= last; i++)
                {
                    byte[] data = new byte[StructReadSize];
                    Array.Copy(block, (int)(sorted[i] - start), data, 0, (int)StructReadSize);
                    structs.Add(new KeyValuePair<uint, byte[]>(sorted[i], data));
                }
                index = last + 1;
            }
            return structs;
        }

        private byte[] ReadLarge(uint address, uint size)
        {
            byte[] result = new byte[size];
            uint done = 0;
            while (done < size)
            {
                uint chunk = Math.Min(MaxReadSize, size - done);
                byte[] part = game.api.ReadMemory(game.pid, address + done, chunk);
                Array.Copy(part, 0, result, done, chunk);
                done += chunk;
            }
            return result;
        }

        private uint ReadUInt(uint address)
        {
            return ToUInt(game.api.ReadMemory(game.pid, address, 4), 0);
        }

        private static string ReadText(byte[] data, int offset, int maxLength)
        {
            int length = 0;
            while (length < maxLength && offset + length < data.Length && data[offset + length] != 0)
            {
                length++;
            }
            return Encoding.ASCII.GetString(data, offset, length);
        }

        private static uint ToUInt(byte[] data, int offset)
        {
            return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
        }

        private static float ToFloat(byte[] data, int offset)
        {
            byte[] value = new byte[4];
            Array.Copy(data, offset, value, 0, 4);
            return BitConverter.ToSingle(value.Reverse().ToArray(), 0);
        }

        private static byte[] ToBytes(uint value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        private static byte[] ToBytes(float value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
    }
}
