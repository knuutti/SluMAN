using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 4's gadgets (ThiefNet powerups): one state byte each, in order, in the game manager's
    /// gadget array (Sly4Addresses.gadgetOffset). Also keeps the gadgets each run file loads
    /// with, in config.txt.
    /// </summary>
    public static class Sly4Gadgets
    {
        // The state values SluMAN writes. Buying a powerup in game sets 3. The bits look like
        // seen / bought / available; 5 is a gadget ThiefNet shows as new. Other values are kept
        // as they are.
        public const byte Locked = 0;
        public const byte InThiefNet = 1;
        public const byte Owned = 3;

        public class Gadget
        {
            public int index;
            public string name;
            public string character;
        }

        public const string Sly = "Sly";
        public const string Murray = "Murray";
        public const string Bentley = "Bentley";
        public const string Carmelita = "Carmelita";

        public static readonly string[] CharacterNames = { Sly, Murray, Bentley, Carmelita };

        /// <summary>Every gadget, by its index in the array.</summary>
        public static readonly Gadget[] All = CreateGadgets();

        public static int Count => All.Length;

        private static Gadget[] CreateGadgets()
        {
            string[,] gadgets = new string[,]
            {
                { "Paraglider", Sly },
                { "Cane Swat Combo", Sly },
                { "Mega Charge Attack", Sly },
                { "Silent Obliteration", Sly },
                { "Rail Sprint", Sly },
                { "Steerable Cane Spin", Sly },
                { "Smoke Bomb", Sly },
                { "Meter Recharger", Sly },
                { "Run While Charging", Sly },
                { "Flash Bomb", Sly },
                { "Moving Pickpocket", Sly },
                { "Sprint While Charging", Sly },
                { "Cane Push", Sly },
                { "Paraglider Dive Attack", Sly },
                { "Silent Sprint", Sly },
                { "Air Smash", Sly },
                { "Elbo El Drop'o Combo", Murray },
                { "Mega Charge Attack", Murray },
                { "El Drop'o", Murray },
                { "Looty Shake", Murray },
                { "Steerable Fist Spin", Murray },
                { "Clothesline", Murray },
                { "Juggernaut Throw", Murray },
                { "Fists of Flame", Murray },
                { "Run While Charging", Murray },
                { "Raging Inferno Flop", Murray },
                { "Light as a Feather", Murray },
                { "Dukes of Dynamite", Murray },
                { "Sprint While Charging", Murray },
                { "Clothesline a lo Macho", Murray },
                { "Diablo Fire Slam", Murray },
                { "Licks of Lightning", Murray },
                { "Cuffs of Confusion", Murray },
                { "Meter Recharger", Murray },
                { "Mega Stomp", Murray },
                { "El Drop'o a lo Macho", Murray },
                { "Sleep Ammo", Bentley },
                { "Adrenaline Burst", Bentley },
                { "Bomb Kick", Bentley },
                { "Shock Ammo", Bentley },
                { "Trigger Bomb", Bentley },
                { "Heat Seeking", Bentley },
                { "Confusion Ammo", Bentley },
                { "Multi Bomb", Bentley },
                { "Throwing Shield", Bentley },
                { "Bomb Blast v2.0", Bentley },
                { "Shrink Ammo", Bentley },
                { "Scorching Hover Pack", Bentley },
                { "Bomb Drop v2.0", Bentley },
                { "Bend it like Bentley", Bentley },
                { "Homing Darts", Bentley },
                { "Meter Recharger", Bentley },
                { "Adrenaline Burst Plus", Bentley },
                { "Moving Pickpocket", Bentley },
                { "Auto Fire", Carmelita },
                { "Charge Shot", Carmelita },
                { "Stun Shot", Carmelita },
                { "Triple Shot", Carmelita },
                { "Super Jump", Carmelita },
            };

            Gadget[] all = new Gadget[gadgets.GetLength(0)];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = new Gadget { index = i, name = gadgets[i, 0], character = gadgets[i, 1] };
            }
            return all;
        }

        /// <summary>The state's name as the Gadgets tab shows it.</summary>
        public static string StateName(byte state)
        {
            switch (state)
            {
                case Locked: return "Locked";
                case InThiefNet: return "In ThiefNet";
                case Owned: return "Owned";
                default: return $"Other ({state})";
            }
        }

        // Run files: each one's gadgets are kept in config.txt as hex, one byte per gadget, under
        // Sly4_<key>_GadgetStates. Sly 3's are Episode1_GadgetUnlocks and so on, hence the prefix.

        /// <summary>The run files, as the Gadgets and Run Files tabs list them.</summary>
        public static readonly string[] RunFiles =
        {
            "Episode 1", "Episode 2", "Episode 3", "Episode 4", "Episode 5", "Epilogue",
        };

        private static string RunFileKey(string runFile)
        {
            return "Sly4_" + runFile.Replace(" ", "") + "_GadgetStates";
        }

        /// <summary>The gadget states saved for a run file, or null if none are.</summary>
        public static byte[] LoadRunFile(string runFile)
        {
            string hex = func.GetConfigData("config.txt", RunFileKey(runFile));
            if (string.IsNullOrEmpty(hex))
            {
                return null;
            }

            byte[] states = IPS3API.HexToBytes(hex.Trim());
            if (states.Length != Count)
            {
                throw new FormatException($"The saved gadgets for {runFile} are {states.Length} bytes, not {Count}.");
            }
            return states;
        }

        public static void SaveRunFile(string runFile, byte[] states)
        {
            func.ChangeFileLines("config.txt", BitConverter.ToString(states).Replace("-", ""), RunFileKey(runFile));
        }
    }
}
