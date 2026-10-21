using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// A Map dropdown and a Spawn point dropdown, like Sly 4's Load Level: the map's own start
    /// first, then its spawn points from the game's spawn points file, then the ones the player
    /// has arrived at before (see Remember).
    /// </summary>
    public class MapPicker
    {
        private const string MapStart = "Map start";

        private readonly ComboBox mapComboBox;
        private readonly ComboBox spawnComboBox;
        private readonly string[] indicators;
        private readonly uint[] defaultSpawns;
        private readonly SpawnPoints spawnPoints;
        // In the user data folder: every spawn point arrived at, so a map's spawn points can be
        // loaded once they've been visited. Null when not remembering.
        private readonly string seenFile;
        private SpawnPoints seenSpawns;

        /// <param name="names">The maps as the Map dropdown lists them.</param>
        /// <param name="indicators">Each map's internal name, such as "Y$KFf_nightclub_exterior".</param>
        /// <param name="defaultSpawns">Each map's own start, loaded for Map start.</param>
        /// <param name="seenFile">The user data file Remember writes to, or null.</param>
        public MapPicker(ComboBox mapComboBox, ComboBox spawnComboBox, string[] names, string[] indicators, uint[] defaultSpawns, SpawnPoints spawnPoints, string seenFile = null)
        {
            this.seenFile = seenFile;
            seenSpawns = new SpawnPoints(seenFile);
            this.mapComboBox = mapComboBox;
            this.spawnComboBox = spawnComboBox;
            this.indicators = indicators;
            this.defaultSpawns = defaultSpawns;
            this.spawnPoints = spawnPoints;

            mapComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            spawnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            mapComboBox.Items.Clear();
            mapComboBox.Items.AddRange(names);
            mapComboBox.SelectedIndexChanged += (sender, e) => FillSpawns();
            if (mapComboBox.Items.Count > 0)
            {
                mapComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>The spawn ID to load the selected map at.</summary>
        public uint SelectedSpawn
        {
            get
            {
                SpawnPoints.SpawnPoint spawn = spawnComboBox.SelectedItem as SpawnPoints.SpawnPoint;
                if (spawn != null)
                {
                    return spawn.id;
                }
                int map = mapComboBox.SelectedIndex;
                return map >= 0 ? defaultSpawns[map] : 0;
            }
        }

        /// <summary>
        /// Notes the spawn point the player has just arrived at on a map (after a load), so it can
        /// be picked from then on. Spawn points already known are left alone.
        /// </summary>
        public void Remember(string indicator, uint spawn)
        {
            if (seenFile == null || string.IsNullOrEmpty(indicator) || spawn == 0 || spawn == uint.MaxValue)
            {
                return;
            }
            if (Contains(spawnPoints, indicator, spawn) || Contains(seenSpawns, indicator, spawn))
            {
                return;
            }

            try
            {
                List<string> lines = new List<string>();
                if (!File.Exists(seenFile))
                {
                    lines.Add("# Spawn points SluMAN has seen the game arrive at: map | spawn ID. Written by SluMAN.");
                }
                lines.Add($"{SpawnPoints.StripPrefix(indicator)} | {spawn}");
                File.AppendAllLines(seenFile, lines);
                seenSpawns = new SpawnPoints(seenFile);
            }
            catch
            {
                // Not writable: the spawn point just isn't remembered.
                return;
            }

            int map = mapComboBox.SelectedIndex;
            if (map >= 0 && SpawnPoints.StripPrefix(indicators[map]) == SpawnPoints.StripPrefix(indicator))
            {
                FillSpawns();
            }
        }

        private static bool Contains(SpawnPoints points, string indicator, uint spawn)
        {
            foreach (SpawnPoints.SpawnPoint point in points.ForMap(indicator))
            {
                if (point.id == spawn)
                {
                    return true;
                }
            }
            return false;
        }

        private void FillSpawns()
        {
            spawnComboBox.Items.Clear();
            spawnComboBox.Items.Add(MapStart);
            int map = mapComboBox.SelectedIndex;
            if (map >= 0)
            {
                List<uint> listed = new List<uint>();
                // Map start already loads the map's own start.
                listed.Add(defaultSpawns[map]);
                foreach (SpawnPoints source in new SpawnPoints[] { spawnPoints, seenSpawns })
                {
                    foreach (SpawnPoints.SpawnPoint spawn in source.ForMap(indicators[map]))
                    {
                        if (!listed.Contains(spawn.id))
                        {
                            listed.Add(spawn.id);
                            spawnComboBox.Items.Add(spawn);
                        }
                    }
                }
            }
            spawnComboBox.SelectedIndex = 0;
            spawnComboBox.Enabled = spawnComboBox.Items.Count > 1;
        }
    }
}
