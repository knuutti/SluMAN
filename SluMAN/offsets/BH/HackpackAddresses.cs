using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Bentley's Hackpack addresses. The game is built on Sly 4's engine, with the same game
    /// manager, views, renderer, tunables and pad, so each version is a Sly4Addresses instance and
    /// the sly4 class runs it. Research notes: .claude/hackpack-research.md.
    /// </summary>
    public partial class Sly4Addresses
    {
        public const string GameIdHackpackPALDigital = "NPEA00427";

        // The game's level table (Scripts/Sly4HackPackLevels.xml, read at boot): a binary tree
        // whose root is at *(*(levelTable + levelTableRootOffset)). Each node holds a level's hash
        // at +LevelNodeHashOffset, its record at +LevelNodeRecordOffset and its two children at
        // +LevelNodeLeftOffset and +LevelNodeRightOffset. LoadLevel looks the hash it's given up
        // here. The record has a path at +levelRecordPathOffset (the game checks it for "Dev/").
        public uint levelTable { get; private set; }
        public uint levelTableRootOffset { get; private set; }
        public uint levelRecordPathOffset { get; private set; }
        public const uint LevelNodeHashOffset = 0x4;
        public const uint LevelNodeRecordOffset = 0x8;
        public const uint LevelNodeLeftOffset = 0xC;
        public const uint LevelNodeRightOffset = 0x10;

        public static bool IsHackpackGameId(string gameId)
        {
            Sly4Addresses found;
            return gameId != null && versions.TryGetValue(gameId.Trim(), out found) && found.IsHackpack;
        }

        private static List<Sly4Addresses> CreateHackpackVersions()
        {
            Sly4Addresses palDigital = new Sly4Addresses
            {
                GameId = GameIdHackpackPALDigital,
                DisplayName = "BENTLEY'S HACKPACK (PAL, Digital)",
                IsHackpack = true,

                // The app object at 0xE97060 (the game manager pointer at +0, then a 0x2EC0-byte
                // entry per player with the pad at +0xCC, as in Sly 4). The pad's CellPadData is at
                // +0xA4, 0xC later than Sly 4's, so the buttons are at +0xAC; the sticks it works
                // out are where Sly 4 has them.
                padPtr = 0xE9712C,
                padButtonsOffset = 0xAC,
                padAnalogLeftOffset = 0x20,
                padAnalogRightOffset = 0x28,

                gameManagerPtr = 0xD05B0C,
                episodeManagerPtr = 0xCDEBD4,

                // The game manager's fields past 0x128000 are 0x600 later than Sly 4's. Its player
                // and goal fields weren't looked for: nothing in the Hackpack has a character, and
                // the goals aren't read.
                loadPhaseOffset = 0x32,
                inGameOffset = 0x2F,
                currentLevelHashOffset = 0x128714,
                loadingLevelHashOffset = 0x128718,
                spawnCharacterOffset = 0x12871C,
                spawnPointOffset = 0x128760,

                // The episode manager is Sly 4's: 4 episodes and 128 job slots.
                episodeArrayOffset = 0x14,
                episodeCountOffset = 0x18,
                jobArrayOffset = 0x1C,
                jobCountOffset = 0x20,
                currentEpisodeOffset = 0x24,
                currentJobOffset = 0x28,
                failedGoalOffset = 0x4C,

                loadLevelFunction = 0x336384,

                // The main loop (0x330600) calls the app's frame update with the app in r3.
                hookCallSite = 0x33065C,
                hookCallTarget = 0x328648,
                // The stubs go in the last page of the code segment, past its end (0xC868E8), and
                // the data in the last page of the data segment, past the end of .bss (0x1026D38).
                hookCodeCave = 0xC86A00,
                hookMailbox = 0x1026E00,

                mainViewOffset = 0x128768,
                viewCameraMatrixOffset = 0x330,
                viewMatrixOffset = 0x2F0,
                viewFovOffset = 0x38,
                viewFarOffset = 0x40,
                viewFogDistanceOffsets = new uint[] { 0x88, 0xC8 },
                viewUpdateCopyCall = 0x3DC458,
                viewCopyFunction = 0x3DAD90,
                calcRenderMatricesFunction = 0x3DB9D0,
                viewOverrideStub = 0xC86B00,
                viewOverrideData = 0x1026E40,
                viewFlagsOffset = 0x34,
                viewEffectsOffset = 0x290,
                effectDofOffset = 0xC,
                effectEnabledOffset = 0x4,

                // In the frame's render submission (0x3275E8), with the game manager in r31 and the
                // main view in r27 as in Sly 4. The mechanic is at gm + 0x1287D4 and its layer
                // views at +0x13C, +0x140 and +0x144.
                hackPreRenderCall = 0x32780C,
                hackPreRenderFunction = 0x2BCFA8,
                hackPreRenderStub = 0xC86C00,
                hackBottomLayerOffset = 0x140,

                drawListCapacityOffset = 0x9C,
                drawListPushDescriptors = new uint[] { 0xCB83D8, 0xCB83E0, 0xCB89C8, 0xCB89D8, 0xCB89E8 },
                drawListPushFunctions = new uint[] { 0x3DDC2C, 0x3DDC48, 0x59BD20, 0x59BE90, 0x59C000 },
                drawListGuardStubs = 0xC86D00,

                clockClampFreqMax = 0xE9D13C,
                clockClampFreqMin = 0xE9D0F0,
                cheatInvulnerable = 0xDE45F0,
                cheatUndetectable = 0xD32268,
                uiFov = 0xE9ED9C,
                gameToc = 0xCCC880,
                cutSceneEnable = 0xE9E1D8,

                tunableRegistry = 0xEBAA5C,
                tunableCount = 0xEBD61C,

                tunableVtableBool = 0x8D2664,
                tunableVtableInt = 0x8D6080,
                tunableVtableFloat = 0x8D2698,
                tunableVtableVec2 = 0x8D7850,
                tunableVtableVec3 = 0x8D7884,
                tunableVtableColor = 0x8D26CC,
                tunableVtableString = 0x8DB780,
                tunableVtableStringHash = 0x8D7ED0,

                levelTable = 0xEBA94C,
                levelTableRootOffset = 0xC,
                levelRecordPathOffset = 0x80,
            };

            return new List<Sly4Addresses> { palDigital };
        }
    }
}
