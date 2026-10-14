using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 3 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly3Addresses
    {
        public const string GameIdPAL = "NPEA00343";
        public const string DefaultGameId = GameIdPAL;

        private static readonly Dictionary<string, Sly3Addresses> versions = CreateVersions();

        public string GameId { get; private set; }
        public string DisplayName { get; private set; }

        // Inputs
        public uint inputOffset { get; private set; }
        public uint analogOffsetLeft { get; private set; }
        public uint analogOffsetRight { get; private set; }

        // Pointers
        public uint coinCount { get; private set; }
        public uint slyCharacterPtr { get; private set; }
        public uint activeCharacterPtr { get; private set; }
        public uint playerEntityPointer { get; private set; }

        // Entity struct offsets (from activeCharacterPtr dereference)
        public uint transformOffset { get; private set; }
        public uint coordsOffsetX { get; private set; }
        public uint coordsOffsetY { get; private set; }
        public uint coordsOffsetZ { get; private set; }
        public uint healthEntityOffset { get; private set; }
        public uint gadgetPowerEntityOffset { get; private set; }

        // Cutscene skipping
        public uint dialogueState { get; private set; }
        public uint dialogueFrameCounter { get; private set; }
        public uint fmvState { get; private set; }

        // Loading
        public uint mapAOB { get; private set; }
        public uint spawnLocation { get; private set; }
        public uint loadType { get; private set; }
        public uint loadTrigger { get; private set; } // Set to 1 to trigger load

        // Gadgets
        public uint gadgetUnlocks { get; private set; }
        public uint gadgetBindsSly { get; private set; }
        public uint gadgetBindsBentley { get; private set; }
        public uint gadgetBindsMurray { get; private set; }

        // Run file specific addresses
        public uint suckValue { get; private set; }
        public uint currentCharacter { get; private set; }
        public uint cameraFov { get; private set; }

        // Autosplitter addresses
        public uint loadingState { get; private set; }
        public uint currentJob { get; private set; }
        public uint currentCheckpoint { get; private set; }
        public uint currentMap { get; private set; }
        public uint gameSpeed { get; private set; }
        public uint mtcTimerValue { get; private set; }
        public uint pauseLock { get; private set; }
        public uint guardAIAddress { get; private set; }
        public uint deathBarriersPointer { get; private set; }
        public uint cameraPointer { get; private set; }

        // Episode 1 specific addresses
        public uint veniceStarted { get; private set; }
        public uint outbackStarted { get; private set; }
        public uint chinaStarted { get; private set; }
        public uint pirateStarted { get; private set; }

        public enum LoadTypes : uint
        {
            Fast = 0,
            Normal = 6,
            Reset = 15,
            RunFile = 18,
            Job = 134,
        }

        private Sly3Addresses()
        {
        }

        /// <summary>
        /// The addresses for the given title ID, or the default version's if it isn't supported.
        /// </summary>
        public static Sly3Addresses ForGame(string gameId)
        {
            Sly3Addresses found;
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
        private Sly3Addresses CopyAs(string gameId, string displayName)
        {
            Sly3Addresses copy = (Sly3Addresses)MemberwiseClone();
            copy.GameId = gameId;
            copy.DisplayName = displayName;
            return copy;
        }

        private static Dictionary<string, Sly3Addresses> CreateVersions()
        {
            Sly3Addresses pal = new Sly3Addresses
            {
                GameId = GameIdPAL,
                DisplayName = "SLY 3 (PAL, PSN)",

                inputOffset = 0x5EC5AA,
                analogOffsetLeft = 0x5EC5F0,
                analogOffsetRight = 0x5EC61C,

                coinCount = 0x6CC808,
                slyCharacterPtr = 0x5ED940,
                activeCharacterPtr = 0x5EC654,
                playerEntityPointer = 0x5EC654,

                transformOffset = 0x44,
                coordsOffsetX = 0x130,
                coordsOffsetY = 0x134,
                coordsOffsetZ = 0x138,
                healthEntityOffset = 0x168,
                gadgetPowerEntityOffset = 0x170,

                dialogueState = 0x39B13F70,
                dialogueFrameCounter = 0x39B13F54,
                fmvState = 0x83C8BC,

                mapAOB = 0x78D2C8,
                spawnLocation = 0x78D308,
                loadType = 0x78D2C4,
                loadTrigger = 0x78D2C0,

                gadgetUnlocks = 0x6CC7F8,
                gadgetBindsSly = 0x6CC7B0,
                gadgetBindsBentley = 0x6CC7BC,
                gadgetBindsMurray = 0x6CC7C8,

                suckValue = 0x589A3C,
                currentCharacter = 0x5EA000,
                cameraFov = 0x7F8680,

                loadingState = 0x6CB600,
                currentJob = 0x5EB488,
                currentCheckpoint = 0x5EB48C,
                currentMap = 0x78D398,
                gameSpeed = 0x5898B8,
                mtcTimerValue = 0x7DAB2C,
                pauseLock = 0x5EC6C4,
                guardAIAddress = 0x5EC6CC,
                deathBarriersPointer = 0x78C6E4,
                cameraPointer = 0x78CE2C,

                veniceStarted = 0x6CE0B4,
                outbackStarted = 0x6CEA80,
                chinaStarted = 0x6D0288,
                pirateStarted = 0x6D1110,
            };

            return new Dictionary<string, Sly3Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { pal.GameId, pal },
            };
        }
    }
}
