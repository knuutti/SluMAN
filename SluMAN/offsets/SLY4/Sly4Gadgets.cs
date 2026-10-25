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
            // The first episode whose run file may have it (GadgetAvailability).
            public int availableFrom;
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
            // The third value is the episode each gadget is available from (GadgetAvailability):
            // 0 until they're filled in.
            Gadget[] all = new Gadget[]
            {
                G("Paraglider", Sly, 0),
                G("Cane Swat Combo", Sly, 0),
                G("Mega Charge Attack", Sly, 0),
                G("Silent Obliteration", Sly, 0),
                G("Rail Sprint", Sly, 0),
                G("Steerable Cane Spin", Sly, 0),
                G("Smoke Bomb", Sly, 0),
                G("Meter Recharger", Sly, 0),
                G("Run While Charging", Sly, 0),
                G("Flash Bomb", Sly, 0),
                G("Moving Pickpocket", Sly, 0),
                G("Sprint While Charging", Sly, 0),
                G("Cane Push", Sly, 0),
                G("Paraglider Dive Attack", Sly, 0),
                G("Silent Sprint", Sly, 0),
                G("Air Smash", Sly, 0),
                G("Elbo El Drop'o Combo", Murray, 0),
                G("Mega Charge Attack", Murray, 0),
                G("El Drop'o", Murray, 0),
                G("Looty Shake", Murray, 0),
                G("Steerable Fist Spin", Murray, 0),
                G("Clothesline", Murray, 0),
                G("Juggernaut Throw", Murray, 0),
                G("Fists of Flame", Murray, 0),
                G("Run While Charging", Murray, 0),
                G("Raging Inferno Flop", Murray, 0),
                G("Light as a Feather", Murray, 0),
                G("Dukes of Dynamite", Murray, 0),
                G("Sprint While Charging", Murray, 0),
                G("Clothesline a lo Macho", Murray, 0),
                G("Diablo Fire Slam", Murray, 0),
                G("Licks of Lightning", Murray, 0),
                G("Cuffs of Confusion", Murray, 0),
                G("Meter Recharger", Murray, 0),
                G("Mega Stomp", Murray, 0),
                G("El Drop'o a lo Macho", Murray, 0),
                G("Sleep Ammo", Bentley, 0),
                G("Adrenaline Burst", Bentley, 0),
                G("Bomb Kick", Bentley, 0),
                G("Shock Ammo", Bentley, 0),
                G("Trigger Bomb", Bentley, 0),
                G("Heat Seeking", Bentley, 0),
                G("Confusion Ammo", Bentley, 0),
                G("Multi Bomb", Bentley, 0),
                G("Throwing Shield", Bentley, 0),
                G("Bomb Blast v2.0", Bentley, 0),
                G("Shrink Ammo", Bentley, 0),
                G("Scorching Hover Pack", Bentley, 0),
                G("Bomb Drop v2.0", Bentley, 0),
                G("Bend it like Bentley", Bentley, 0),
                G("Homing Darts", Bentley, 0),
                G("Meter Recharger", Bentley, 0),
                G("Adrenaline Burst Plus", Bentley, 0),
                G("Moving Pickpocket", Bentley, 0),
                G("Auto Fire", Carmelita, 0),
                G("Charge Shot", Carmelita, 0),
                G("Stun Shot", Carmelita, 0),
                G("Triple Shot", Carmelita, 0),
                G("Super Jump", Carmelita, 0),
            };

            for (int i = 0; i < all.Length; i++)
            {
                all[i].index = i;
            }
            return all;
        }

        private static Gadget G(string name, string character, int availableFrom)
        {
            return new Gadget { name = name, character = character, availableFrom = availableFrom };
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
            "Episode 1", "Episode 2", "Episode 3", "Episode 4", "Episode 5",
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

        /// <summary>
        /// Locks the gadgets in states that aren't available yet at the start of the run file, and
        /// returns their names.
        /// </summary>
        public static List<string> RemoveUnavailable(string runFile, byte[] states)
        {
            List<string> removed = new List<string>();
            foreach (Gadget gadget in All)
            {
                if (states[gadget.index] != Locked && !GadgetAvailability.IsAvailable(gadget.availableFrom, runFile))
                {
                    states[gadget.index] = Locked;
                    removed.Add($"{gadget.name} ({gadget.character})");
                }
            }
            return removed;
        }

        public static void SaveRunFile(string runFile, byte[] states)
        {
            func.ChangeFileLines("config.txt", BitConverter.ToString(states).Replace("-", ""), RunFileKey(runFile));
        }
    }
}
