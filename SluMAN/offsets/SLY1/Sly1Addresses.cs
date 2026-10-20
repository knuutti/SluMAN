using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 1 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly1Addresses
    {
        public const string GameIdUS = "NPUA80663";
        public const string DefaultGameId = GameIdUS;

        private static readonly Dictionary<string, Sly1Addresses> versions = CreateVersions();

        public string GameId { get; private set; }
        public string DisplayName { get; private set; }

        // Inputs
        public uint inputOffset { get; private set; }
        public uint analogOffsetLeft { get; private set; }
        public uint analogOffsetRight { get; private set; }

        // Game state
        public uint coinCount { get; private set; }
        public uint levelId { get; private set; }
        public uint worldId { get; private set; }
        public uint loadingState { get; private set; }
        public uint w3Keys { get; private set; }
        public uint transitionState { get; private set; }
        public uint charmsCount { get; private set; }
        public uint livesCount { get; private set; }
        public uint clockwerkHealth { get; private set; }

        // Level loading. The reload block: +0 trigger, +C and +20 set with it, +10 the address of
        // the level's entry in the reload table, +14 the entrance to arrive at. The table has one
        // entry per level (two unused level slots have none), each holding the level's name once
        // per language.
        public uint reloadBlock { get; private set; }
        public uint reloadTable { get; private set; }
        public uint reloadEntrySize { get; private set; }
        public uint reloadLanguageSize { get; private set; }
        public uint language { get; private set; }

        // The current level's entrances: *(*entranceListPtr + entranceListOffset) is the first;
        // each has its ID at +8, its position at +40 and the next entrance at +24.
        public uint entranceListPtr { get; private set; }
        public uint entranceListOffset { get; private set; }

        // Cutscenes: *dialoguePtr is the dialogue playing (0 when none), ended by writing 0 at
        // +dialogueSkipOffset. With no dialogue, writing 1 to fmvSkip ends an FMV.
        public uint dialoguePtr { get; private set; }
        public uint dialogueSkipOffset { get; private set; }
        public uint fmvSkip { get; private set; }

        private Sly1Addresses()
        {
        }

        /// <summary>
        /// The addresses for the given title ID, or the default version's if it isn't supported.
        /// </summary>
        public static Sly1Addresses ForGame(string gameId)
        {
            Sly1Addresses found;
            if (gameId != null && versions.TryGetValue(gameId.Trim(), out found))
            {
                return found;
            }
            return versions[DefaultGameId];
        }

        public static bool IsSupportedGameId(string gameId)
        {
            return gameId != null && versions.ContainsKey(gameId.Trim());
        }

        /// <summary>
        /// A copy of this version under another title ID. Set only the addresses that differ on it.
        /// </summary>
        private Sly1Addresses CopyAs(string gameId, string displayName)
        {
            Sly1Addresses copy = (Sly1Addresses)MemberwiseClone();
            copy.GameId = gameId;
            copy.DisplayName = displayName;
            return copy;
        }

        private static Dictionary<string, Sly1Addresses> CreateVersions()
        {
            Sly1Addresses us = new Sly1Addresses
            {
                GameId = GameIdUS,
                DisplayName = "SLY 1 (NTSC, PSN)",

                inputOffset = 0x428BFC,
                analogOffsetLeft = 0x428B94,
                analogOffsetRight = 0x428BC8,

                coinCount = 0x3E7FF4,
                levelId = 0x3E7FE8,
                worldId = 0x3E7FE4,
                loadingState = 0xE5E940,
                w3Keys = 0x3E7738,
                transitionState = 0xE62AD0,
                charmsCount = 0x3E7FF0,
                livesCount = 0x3E7FEC,
                clockwerkHealth = 0x3630941C,

                // From SlyMultiTrainer by NiV-L-A (Sly 1 NTSC PS3 PSN)
                reloadBlock = 0xE62AC4,
                reloadTable = 0x3B54FC,
                reloadEntrySize = 0x6C,
                reloadLanguageSize = 0x20,
                language = 0x3E8010,
                entranceListPtr = 0xE5E8D8,
                entranceListOffset = 0x1A54,
                dialoguePtr = 0xE418B0,
                dialogueSkipOffset = 0x2E8,
                fmvSkip = 0xF516CC,
            };

            return new Dictionary<string, Sly1Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { us.GameId, us },
            };
        }
    }
}
