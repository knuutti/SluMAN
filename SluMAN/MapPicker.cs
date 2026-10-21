using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// A Map dropdown and a Spawn point dropdown, like Sly 4's Load Level: the map's own start
    /// first, then its spawn points from the game's spawn points file.
    /// </summary>
    public class MapPicker
    {
        private const string MapStart = "Map start";

        private readonly ComboBox mapComboBox;
        private readonly ComboBox spawnComboBox;
        private readonly string[] indicators;
        private readonly uint[] defaultSpawns;
        private readonly SpawnPoints spawnPoints;

        /// <param name="names">The maps as the Map dropdown lists them.</param>
        /// <param name="indicators">Each map's internal name, such as "Y$KFf_nightclub_exterior".</param>
        /// <param name="defaultSpawns">Each map's own start, loaded for Map start.</param>
        public MapPicker(ComboBox mapComboBox, ComboBox spawnComboBox, string[] names, string[] indicators, uint[] defaultSpawns, SpawnPoints spawnPoints)
        {
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

        private void FillSpawns()
        {
            spawnComboBox.Items.Clear();
            spawnComboBox.Items.Add(MapStart);
            int map = mapComboBox.SelectedIndex;
            if (map >= 0)
            {
                foreach (SpawnPoints.SpawnPoint spawn in spawnPoints.ForMap(indicators[map]))
                {
                    // Map start already loads the map's own start.
                    if (spawn.id != defaultSpawns[map])
                    {
                        spawnComboBox.Items.Add(spawn);
                    }
                }
            }
            spawnComboBox.SelectedIndex = 0;
            spawnComboBox.Enabled = spawnComboBox.Items.Count > 1;
        }
    }
}
