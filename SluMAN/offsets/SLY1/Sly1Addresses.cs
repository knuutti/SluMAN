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

        // The player. *slyEntityPtr is Sly's entity; when it's 0, or the int at +slyActiveOffset is 0
        // (Sly is in a vehicle), *vehicleEntityPtr is the one being played. An entity's transform
        // (a 4x4 matrix) is part of it at +transformOffset: the position at +positionOffset in it,
        // the velocity at +velocityOffset.
        public uint slyEntityPtr { get; private set; }
        public uint vehicleEntityPtr { get; private set; }
        public uint slyActiveOffset { get; private set; }
        public uint transformOffset { get; private set; }
        public uint positionOffset { get; private set; }
        public uint velocityOffset { get; private set; }
        // Infinite double jump: Sly's entity holds -1 at +infiniteJumpOffset1 and 1 at
        // +infiniteJumpOffset2 (the values SlyMultiTrainer freezes).
        public uint infiniteJumpOffset1 { get; private set; }
        public uint infiniteJumpOffset2 { get; private set; }

        // Save data. One world save per world (0 is Paris and the intro) from gameState +
        // worldSaveStart, each worldSaveSize long: first a level save per level, levelSaveSize
        // long with the level's flags at +0, then the world's key, vault and sprint counts and its
        // flags at the offsets below.
        public uint gameState { get; private set; }
        public uint worldSaveStart { get; private set; }
        public uint worldSaveSize { get; private set; }
        public uint levelSaveSize { get; private set; }
        public uint worldKeysOffset { get; private set; }
        public uint worldVaultsOffset { get; private set; }
        public uint worldSprintsOffset { get; private set; }
        public uint worldFlagsOffset { get; private set; }

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
                slyEntityPtr = 0x428D04,
                vehicleEntityPtr = 0xE3F790,
                slyActiveOffset = 0x1C,
                transformOffset = 0xD0,
                positionOffset = 0x30,
                velocityOffset = 0x80,
                infiniteJumpOffset1 = 0x2218,
                infiniteJumpOffset2 = 0x2488,
                // *0x3A4FC0; w3Keys above is world 3's key count in it
                gameState = 0x3E660C,
                worldSaveStart = 0x10,
                worldSaveSize = 0x44C,
                levelSaveSize = 0x78,
                worldKeysOffset = 0x438,
                worldVaultsOffset = 0x43C,
                worldSprintsOffset = 0x440,
                worldFlagsOffset = 0x448,
            };

            return new Dictionary<string, Sly1Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { us.GameId, us },
            };
        }
    }
}
