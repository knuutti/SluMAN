using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SluMAN
{
    /// <summary>
    /// Names for the Hackpack's levels, from data/hackpack_levels.txt: "level | English name"
    /// lines, the level as Scripts/Sly4HackPackLevels.xml names it (SluMAN hashes it the way the
    /// game does, see sly4.Hash) or its hash as 0x and 8 hex digits, and # starting a comment.
    /// Without an English name the level's own name shows. The game's level table lists every
    /// level (sly4.ReadHackpackLevels); the ones not in the file show as their hash.
    /// </summary>
    public static class HackpackLevels
    {
        private const string LevelFile = "data/hackpack_levels.txt";

        private static Dictionary<uint, string> names;
        // Each named level's line in the file, so the level list keeps the file's order.
        private static Dictionary<uint, int> order;

        /// <summary>The level's name, or null if the file doesn't name it.</summary>
        public static string GetName(uint levelHash)
        {
            Load();
            string name;
            return names.TryGetValue(levelHash, out name) ? name : null;
        }

        /// <summary>Where the file lists the level; after every named level if it doesn't.</summary>
        public static int OrderOf(uint levelHash)
        {
            Load();
            int index;
            return order.TryGetValue(levelHash, out index) ? index : int.MaxValue;
        }

        private static void Load()
        {
            if (names != null)
            {
                return;
            }
            names = new Dictionary<uint, string>();
            order = new Dictionary<uint, int>();

            string[] lines;
            try
            {
                lines = File.ReadAllLines(LevelFile);
            }
            catch
            {
                // No names: every level shows as its hash.
                return;
            }

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line == "" || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }
                string[] parts = line.Split('|');
                string level = parts[0].Trim();
                string name = parts.Length > 1 ? parts[1].Trim() : "";
                if (level == "")
                {
                    continue;
                }

                uint hash;
                if (!(level.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    && uint.TryParse(level.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hash)))
                {
                    hash = sly4.Hash(level);
                }
                if (name == "")
                {
                    name = level;
                }
                if (!names.ContainsKey(hash))
                {
                    names[hash] = name;
                    order[hash] = order.Count;
                }
            }
        }
    }
}
