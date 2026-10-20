using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    public class sly1 : IGame, IAutosplitterAvailable
    {
        public static Sly1Addresses addr = Sly1Addresses.ForGame(Sly1Addresses.DefaultGameId);

        public sly1(IPS3API api, string gameNameId) : base(api)
        {
            addr = Sly1Addresses.ForGame(gameNameId);
        }

        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[]
        {
            (addr.levelId, 4),
            (addr.worldId, 4),
            (addr.loadingState, 4),
            (addr.w3Keys, 4),
            (addr.charmsCount, 4),
            (addr.livesCount, 4),
            (addr.coinCount, 4),
            (addr.transitionState, 4),
            (addr.clockwerkHealth, 4),
        };

        // TODO: Sly 1 position saving is not implemented yet.
        public override void SavePosition() { }
        public override void LoadPosition() { }

        /// <summary>The Load Game combo: reloads the current level.</summary>
        public override void LoadGame()
        {
            ReloadLevel();
        }

        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            SubscribeSlyButtons(sly1.addr.inputOffset);
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            int analogLSubID = api.SubMemory(pid, sly1.addr.analogOffsetLeft, 8, (value) =>
            {
                // Pressing D-pad also moves the stick in memory
                if ((Inputs.RawInputs & 0xF000) != 0)
                {
                    Inputs.ly = 0;
                    Inputs.lx = 0;
                    return;
                }

                Inputs.ly = -1 * BitConverter.ToSingle(value, 0);
                Inputs.lx = BitConverter.ToSingle(value, 4);
            });

            int analogRSubID = api.SubMemory(pid, sly1.addr.analogOffsetRight, 8, (value) =>
            {
                Inputs.ry = -1 * BitConverter.ToSingle(value, 0);
                Inputs.rx = BitConverter.ToSingle(value, 4);
            });
        }

        public void SetCoinCount(int coins)
        {
            api.WriteMemory(pid, sly1.addr.coinCount, ConvertIntToBytes(coins));
        }

        /// <summary>A level the Level dropdown lists: its index in the game's level list and its name.</summary>
        public class LevelEntry
        {
            public int index;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        // The game's level list: Splash, Paris and the Hideout, then 9 levels per world. The
        // world/level IDs map to it as (world - 1) * 9 + level + 3. Null entries are unused slots,
        // which have no entry in the reload table.
        private static readonly string[] LevelNames = new string[]
        {
            "Splash", "Paris", "Hideout",
            // Tide of Terror
            "A Stealthy Approach", "Prowling the Grounds", "High Class Heist", "Into the Machine",
            "A Cunning Disguise", "The Fire Down Below", "Treasure in the Depths", "The Gunboat Graveyard",
            "The Eye of the Storm",
            // Sunset Snake Eyes
            "A Rocky Start", "Muggshot's Turf", "Boneyard Casino", "Murray's Big Gamble", "At the Dog Track",
            "Two to Tango", "Straight to the Top", "Back Alley Heist", "Last Call",
            // Vicious Voodoo
            "The Dread Swamp Path", "The Swamp's Dark Centre", "The Lair of the Beast", "A Grave Undertaking",
            "Piranha Lake", "Descent into Danger", "A Ghastly Voyage", "Down Home Cooking", "A Deadly Dance",
            // Fire in the Sky
            "A Perilous Ascent", "Inside the Stronghold", "Flaming Temple of Flame", "The Unseen Foe",
            "The King of the Hill", "Rapid Fire Assault", "Duel by the Dragon", "A Desperate Race", "Flame Fu!",
            // A Cold Heart of Hate
            "A Hazardous Path", null, "Burning Rubber", "A Daring Rescue", "Bentley Comes Through",
            "A Temporary Truce", "Sinking Peril", null, "A Strange Reunion",
        };

        private static readonly string[] WorldNames = new string[]
        {
            null, "Tide of Terror", "Sunset Snake Eyes", "Vicious Voodoo", "Fire in the Sky", "A Cold Heart of Hate",
        };

        public const int LevelsPerWorld = 9;
        private const int FirstWorldLevelIndex = 3;

        /// <summary>Every level, in the game's order, named "World: Level".</summary>
        public static List<LevelEntry> GetLevels()
        {
            List<LevelEntry> levels = new List<LevelEntry>();
            for (int i = 0; i < LevelNames.Length; i++)
            {
                if (LevelNames[i] == null)
                {
                    continue;
                }
                levels.Add(new LevelEntry { index = i, name = LevelTitle(i) });
            }
            return levels;
        }

        public static string LevelTitle(int index)
        {
            if (index < 0 || index >= LevelNames.Length || LevelNames[index] == null)
            {
                return "-";
            }
            if (index < FirstWorldLevelIndex)
            {
                return LevelNames[index];
            }
            int world = (index - FirstWorldLevelIndex) / LevelsPerWorld + 1;
            return $"{WorldNames[world]}: {LevelNames[index]}";
        }

        /// <summary>The current level's index in the level list, or -1 if it isn't known.</summary>
        public int GetCurrentLevelIndex()
        {
            int world = ReadInt(sly1.addr.worldId);
            int level = ReadInt(sly1.addr.levelId);
            int index;
            if (world == 0)
            {
                // Level IDs 2 to 4: Splash, Paris, Hideout
                index = level - 2;
            }
            else
            {
                index = (world - 1) * LevelsPerWorld + level + FirstWorldLevelIndex;
            }
            return index >= 0 && index < LevelNames.Length && LevelNames[index] != null ? index : -1;
        }

        /// <summary>
        /// Loads a level, at one of its entrances or (entrance null) at the entrance the game has
        /// set, which is the level's start unless an earlier load chose another.
        /// </summary>
        public void LoadLevel(int index, int? entrance)
        {
            if (index < 0 || index >= LevelNames.Length || LevelNames[index] == null)
            {
                throw new ArgumentException("Not a level.");
            }

            // The reload table has no entries for the two unused level slots.
            int tableIndex = index;
            for (int i = 0; i < index; i++)
            {
                if (LevelNames[i] == null)
                {
                    tableIndex--;
                }
            }

            uint language = (uint)Math.Max(0, ReadInt(sly1.addr.language));
            uint entry = sly1.addr.reloadTable + (uint)tableIndex * sly1.addr.reloadEntrySize
                + language * sly1.addr.reloadLanguageSize;

            if (entrance.HasValue)
            {
                api.WriteMemory(pid, sly1.addr.reloadBlock + 0x14, ConvertIntToBytes(entrance.Value));
            }
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0x10, ConvertIntToBytes((int)entry));
            ReloadLevel();
        }

        /// <summary>Reloads the level the reload block points at: the current one, or the last loaded.</summary>
        public void ReloadLevel()
        {
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0xC, ConvertIntToBytes(1));
            api.WriteMemory(pid, sly1.addr.reloadBlock + 0x20, ConvertIntToBytes(1));
            api.WriteMemory(pid, sly1.addr.reloadBlock, ConvertIntToBytes(1));
        }

        /// <summary>The current level's entrance IDs, in the game's order. Empty while loading.</summary>
        public List<int> GetEntrances()
        {
            List<int> entrances = new List<int>();
            uint list = (uint)ReadInt(sly1.addr.entranceListPtr);
            if (list == 0)
            {
                return entrances;
            }
            uint entrance = (uint)ReadInt(list + sly1.addr.entranceListOffset);
            // A level has a few dozen entrances; the cap stops a bad read from looping.
            for (int i = 0; entrance != 0 && i < 256; i++)
            {
                entrances.Add(ReadInt(entrance + 0x8));
                entrance = (uint)ReadInt(entrance + 0x24);
            }
            return entrances;
        }

        public int GetLives()
        {
            return ReadInt(sly1.addr.livesCount);
        }

        public void SetLives(int lives)
        {
            api.WriteMemory(pid, sly1.addr.livesCount, ConvertIntToBytes(lives));
        }

        public int GetCharms()
        {
            return ReadInt(sly1.addr.charmsCount);
        }

        /// <summary>0 = None, 1 = Blue, 2 = Gold.</summary>
        public void SetCharms(int charms)
        {
            api.WriteMemory(pid, sly1.addr.charmsCount, ConvertIntToBytes(charms));
        }

        public int GetCoins()
        {
            return ReadInt(sly1.addr.coinCount);
        }

        private int livesFreezeSubID = -1;
        private int charmsFreezeSubID = -1;

        /// <summary>Holds the lives at their current count, or releases them.</summary>
        public void SetLivesFrozen(bool frozen)
        {
            SetFrozen(ref livesFreezeSubID, frozen, sly1.addr.livesCount, Math.Max(GetLives(), 1));
        }

        /// <summary>Holds a gold charm, so the next two hits don't cost a life, or releases it.</summary>
        public void SetCharmsFrozen(bool frozen)
        {
            SetFrozen(ref charmsFreezeSubID, frozen, sly1.addr.charmsCount, 2);
        }

        /// <summary>
        /// Forgets freezes the console dropped when the game reset, so turning them on again
        /// makes new ones.
        /// </summary>
        public void ForgetFreezes()
        {
            livesFreezeSubID = -1;
            charmsFreezeSubID = -1;
        }

        private void SetFrozen(ref int subID, bool frozen, uint address, int value)
        {
            if (subID >= 0)
            {
                try
                {
                    api.ReleaseSubID(subID);
                }
                catch
                {
                    // The connection may already have dropped it.
                }
                subID = -1;
            }
            if (frozen)
            {
                subID = api.FreezeMemory(pid, address, IPS3API.MemoryCondition.Any, (uint)value);
            }
        }

        /// <summary>Ends the dialogue line that's playing, or the FMV when there's no dialogue.</summary>
        public void SkipCinematic()
        {
            uint dialogue = (uint)ReadInt(sly1.addr.dialoguePtr);
            if (dialogue == 0)
            {
                api.WriteMemory(pid, sly1.addr.fmvSkip, ConvertIntToBytes(1));
            }
            else
            {
                api.WriteMemory(pid, dialogue + sly1.addr.dialogueSkipOffset, ConvertIntToBytes(0));
            }
        }

        private int ReadInt(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }
    }
}
