using System;
using System.Collections.Generic;
using System.IO;

namespace SluMAN
{
    /// <summary>
    /// A game's named spawn points per map, from data/sN_spawn_points.txt: "map | spawn ID |
    /// English name" lines, the map without its "Y$KF" prefix, and # starting a comment. A spawn
    /// point without a name shows as its ID.
    /// </summary>
    public class SpawnPoints
    {
        public class SpawnPoint
        {
            public uint id;
            public string name;

            public override string ToString()
            {
                return string.IsNullOrEmpty(name) ? id.ToString() : name;
            }
        }

        private const string MapPrefix = "Y$KF";

        private readonly Dictionary<string, List<SpawnPoint>> byMap = new Dictionary<string, List<SpawnPoint>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Reads the file; a missing or unreadable file gives no spawn points.</summary>
        public SpawnPoints(string path)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                return;
            }

            foreach (string line in lines)
            {
                // Everything after a # is a comment.
                string trimmed = line;
                int comment = trimmed.IndexOf('#');
                if (comment >= 0)
                {
                    trimmed = trimmed.Substring(0, comment);
                }
                trimmed = trimmed.Trim();
                if (trimmed == "")
                {
                    continue;
                }

                string[] parts = trimmed.Split('|');
                uint id;
                if (parts.Length < 2 || !uint.TryParse(parts[1].Trim(), out id))
                {
                    continue;
                }

                string map = StripPrefix(parts[0].Trim());
                List<SpawnPoint> spawns;
                if (!byMap.TryGetValue(map, out spawns))
                {
                    spawns = new List<SpawnPoint>();
                    byMap[map] = spawns;
                }
                spawns.Add(new SpawnPoint { id = id, name = parts.Length > 2 ? parts[2].Trim() : "" });
            }
        }

        /// <summary>The map's spawn points in file order, by map name with or without "Y$KF".</summary>
        public List<SpawnPoint> ForMap(string map)
        {
            List<SpawnPoint> spawns;
            return byMap.TryGetValue(StripPrefix(map), out spawns) ? spawns : new List<SpawnPoint>();
        }

        private static string StripPrefix(string map)
        {
            return map.StartsWith(MapPrefix) ? map.Substring(MapPrefix.Length) : map;
        }
    }
}
