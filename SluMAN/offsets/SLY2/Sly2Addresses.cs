using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 2 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly2Addresses
    {
        public const string GameIdKOR = "NPHA80175";
        public const string GameIdUS = "NPUA80664";
        public const string DefaultGameId = GameIdKOR;

        private static readonly Dictionary<string, Sly2Addresses> versions = CreateVersions();

        public string GameId { get; private set; }
        public string DisplayName { get; private set; }

        // Inputs
        public uint inputOffset { get; private set; }
        public uint analogOffsetLeft { get; private set; }
        public uint analogOffsetRight { get; private set; }

        // Pointers
        public uint slyCharacterPtr { get; private set; }
        public uint activeCharacterPtr { get; private set; }

        // Offsets
        public uint transformOffset { get; private set; }
        public uint coordsOffsetX { get; private set; }
        public uint coordsOffsetY { get; private set; }
        public uint coordsOffsetZ { get; private set; }

        // Utility
        public uint coinCount { get; private set; }
        public uint currentCharacter { get; private set; }
        public uint cameraFov { get; private set; }
        public uint cameraParameter { get; private set; }
        public uint loadingState { get; private set; }
        public uint currentJobId { get; private set; }
        public uint currentCheckpointId { get; private set; }
        public uint currentMapId { get; private set; }
        public uint gameSpeed { get; private set; }
        public uint pauseMenuState { get; private set; }

        // Cutscene skipping
        public uint dialogueState { get; private set; }
        public uint dialogueFrameCounter { get; private set; }
        public uint fmvState { get; private set; }

        // Loading
        public uint mapAOB { get; private set; }
        public uint spawnLocation { get; private set; }
        public uint loadType { get; private set; }
        public uint loadTrigger { get; private set; }

        // Gadgets
        public uint gadgetUnlocks { get; private set; }
        public uint gadgetBindsSly { get; private set; }
        public uint gadgetBindsBentley { get; private set; }
        public uint gadgetBindsMurray { get; private set; }

        // Values for autosplitter
        public uint parisStarted { get; private set; }
        public uint templeStarted { get; private set; }
        public uint prisonStarted { get; private set; }
        public uint castleStarted { get; private set; }
        public uint trainStarted { get; private set; } // 2 = CC not started, 3 = CC started
        public uint sawmillStarted { get; private set; }
        public uint blimpStarted { get; private set; }
        public uint parisCinemaState { get; private set; }
        public uint rajanHealth { get; private set; }

        // Per-character health and gadget power (juice)
        public uint healthSly { get; private set; }
        public uint healthBentley { get; private set; }
        public uint healthMurray { get; private set; }
        public uint juiceSly { get; private set; }
        public uint juiceBentley { get; private set; }
        public uint juiceMurray { get; private set; }

        public enum LoadTypes : uint
        {
            Fast = 0,
            Normal = 6,
            Reset = 15,
            RunFile = 18,
            Job = 134,
        }

        private Sly2Addresses()
        {
        }

        /// <summary>
        /// The addresses for the given title ID, or the default version's if it isn't supported.
        /// </summary>
        public static Sly2Addresses ForGame(string gameId)
        {
            Sly2Addresses found;
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
        private Sly2Addresses CopyAs(string gameId, string displayName)
        {
            Sly2Addresses copy = (Sly2Addresses)MemberwiseClone();
            copy.GameId = gameId;
            copy.DisplayName = displayName;
            return copy;
        }

        private static Dictionary<string, Sly2Addresses> CreateVersions()
        {
            Sly2Addresses kor = new Sly2Addresses
            {
                GameId = GameIdKOR,
                DisplayName = "SLY 2 (KOR, PSN)",

                inputOffset = 0x500F76,
                analogOffsetLeft = 0x500EFC,
                analogOffsetRight = 0x500F30,

                slyCharacterPtr = 0x502100,
                activeCharacterPtr = 0x49E290,

                transformOffset = 0x44,
                coordsOffsetX = 0x130,
                coordsOffsetY = 0x134,
                coordsOffsetZ = 0x138,

                coinCount = 0x7A83B0,
                currentCharacter = 0x7A830C,
                cameraFov = 0x49E054,
                cameraParameter = 0x49E07C,
                loadingState = 0x7A7200,
                currentJobId = 0x4FEBF4,
                currentCheckpointId = 0x4FEBF8,
                currentMapId = 0x7B4CE0,
                gameSpeed = 0x49DF80,
                pauseMenuState = 0x4FFE84,

                dialogueState = 0x39E4BF70,
                dialogueFrameCounter = 0x39E4BF54,
                fmvState = 0x9066FC,

                mapAOB = 0x7B4C58,
                spawnLocation = 0x7B4C98,
                loadType = 0x7B4C54,
                loadTrigger = 0x7B4C50,

                gadgetUnlocks = 0x7A83A8,
                gadgetBindsSly = 0x7A836C,
                gadgetBindsBentley = 0x7A8384,
                gadgetBindsMurray = 0x7A839C,

                parisStarted = 0x7A9D84,
                templeStarted = 0x7AA9F0,
                prisonStarted = 0x7AB38C,
                castleStarted = 0x7ABBE8,
                trainStarted = 0x7ABF28,
                sawmillStarted = 0x7AC818,
                blimpStarted = 0x7AD150,
                parisCinemaState = 0x7A9AC0,
                rajanHealth = 0x35C2AE2C,

                // From SluMAN-EXT (utility.lua)
                healthSly = 0x7A8360,
                healthBentley = 0x7A8378,
                healthMurray = 0x7A8390,
                juiceSly = 0x7A8364,
                juiceBentley = 0x7A837C,
                juiceMurray = 0x7A8394,
            };

            // The NTSC-U PSN release matches the Korean addresses wherever the community address
            // sheet lists both, so it's assumed to be the same build. Not tested on NTSC-U yet.
            Sly2Addresses us = kor.CopyAs(GameIdUS, "SLY 2 (NTSC, PSN)");

            return new Dictionary<string, Sly2Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { kor.GameId, kor },
                { us.GameId, us },
            };
        }
    }
}
