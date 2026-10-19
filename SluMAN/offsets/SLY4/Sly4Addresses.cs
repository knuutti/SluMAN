using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 4 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly4Addresses
    {
        public const string GameIdPALDisc = "BCES01284";
        public const string GameIdPALDigital = "NPEA00429";
        public const string DefaultGameId = GameIdPALDisc;

        private static readonly Dictionary<string, Sly4Addresses> versions = CreateVersions();

        public string GameId { get; private set; }
        public string DisplayName { get; private set; }

        // Inputs: the game's pad object, which holds the controller data the game read this frame.
        // Buttons are the two button bytes as a 4-byte word: 00, Select..Left, 00, L2..Square.
        // Each stick is two big-endian floats, X then Y.
        public uint inputButtons { get; private set; }
        public uint analogLeft { get; private set; }
        public uint analogRight { get; private set; }

        // Pointers
        public uint gameManagerPtr { get; private set; }
        public uint episodeManagerPtr { get; private set; }

        // Game manager offsets
        public uint coinCountOffset { get; private set; }
        public uint playerRefOffset { get; private set; }
        public uint loadPhaseOffset { get; private set; }
        public uint inGameOffset { get; private set; }
        public uint goalArrayOffset { get; private set; }
        public uint goalCountOffset { get; private set; }
        public uint currentLevelHashOffset { get; private set; }
        public uint loadingLevelHashOffset { get; private set; }
        // The character to spawn as on the next load (a name, up to 64 bytes); empty for the
        // level's own. Door transitions fill it to keep the character, and the load clears it.
        public uint spawnCharacterOffset { get; private set; }
        // The named object to spawn at on the next load (its hash); 0 for the level's default
        // start. Doors fill it with their arrival point, and the load clears it.
        public uint spawnPointOffset { get; private set; }

        // Episode manager offsets
        public uint episodeArrayOffset { get; private set; }
        public uint episodeCountOffset { get; private set; }
        public uint jobArrayOffset { get; private set; }
        public uint jobCountOffset { get; private set; }
        public uint currentEpisodeOffset { get; private set; }
        public uint currentJobOffset { get; private set; }
        public uint failedGoalOffset { get; private set; }

        // Job offsets
        public uint jobEpisodeIndexOffset { get; private set; }

        // Goal offsets
        public uint goalStateOffset { get; private set; }
        public uint goalNameOffset { get; private set; }

        // Game functions, called through the code hook
        public uint startJobByInstFunction { get; private set; }
        public uint teleportToGoalFunction { get; private set; }
        public uint loadLevelFunction { get; private set; }
        public uint abandonCurrentJobFunction { get; private set; }

        // Code hook: a call made once per frame is pointed at a stub in the unused end of the code
        // segment, which runs the call queued in the mailbox (unused end of the data segment)
        public uint hookCallSite { get; private set; }
        public uint hookCallTarget { get; private set; }
        public uint hookCodeCave { get; private set; }
        public uint hookMailbox { get; private set; }

        // The main gameplay view (from the game manager). Its camera matrix is at
        // +viewCameraMatrixOffset (the camera's X, Y and Z axes, then its position, as 4 floats
        // each, the 4th of each holding something else) and the view matrix, its inverse, at
        // +viewMatrixOffset.
        public uint mainViewOffset { get; private set; }
        public uint viewMatrixOffset { get; private set; }
        public uint viewCameraMatrixOffset { get; private set; }
        // The view's render copy, made from it each frame for drawing
        public uint viewRenderCopyOffset { get; private set; }

        // View override (free camera): a routine in the code cave, run every frame by the code
        // hook while the main view is drawn, copies the matrices kept in the data area (the view
        // matrix, then the camera matrix) into the render copy and has CalcRenderMatrices rebuild
        // the rest from them
        public uint calcRenderMatricesFunction { get; private set; }
        public uint viewOverrideRoutine { get; private set; }
        public uint viewOverrideData { get; private set; }

        // Player offsets (from the player ref)
        public uint playerFlagsOffset { get; private set; }
        public uint characterControllerOffset { get; private set; }

        // Character controller offsets
        public uint controllerVelocityOffset { get; private set; }
        public uint characterBodyOffset { get; private set; }

        // Havok offsets: hkpCharacterRigidBody to its hkpRigidBody, then the body's motion state
        public uint rigidBodyOffset { get; private set; }
        public uint bodyPositionOffset { get; private set; }

        // Tunables (the value, at +0x48 of each tunable struct)
        public uint clockClampFreqMax { get; private set; }
        public uint clockClampFreqMin { get; private set; }
        public uint cheatInvulnerable { get; private set; }
        public uint cheatUndetectable { get; private set; }
        public uint cutSceneEnable { get; private set; }

        // Tunable registry: an array of pointers to every tunable struct, and its length
        public uint tunableRegistry { get; private set; }
        public uint tunableCount { get; private set; }

        // Tunable struct vtables, one per value type
        public uint tunableVtableBool { get; private set; }
        public uint tunableVtableInt { get; private set; }
        public uint tunableVtableFloat { get; private set; }
        public uint tunableVtableVec2 { get; private set; }
        public uint tunableVtableVec3 { get; private set; }
        public uint tunableVtableColor { get; private set; }
        public uint tunableVtableString { get; private set; }
        public uint tunableVtableStringHash { get; private set; }

        // Entry sizes in the episode and job arrays
        public const uint EpisodeStride = 0x35C;
        public const uint JobStride = 0x218;

        // Goal state once the goal is done
        public const byte GoalStateDone = 5;

        private Sly4Addresses()
        {
        }

        /// <summary>
        /// The addresses for the given title ID, or the default version's if it isn't supported.
        /// </summary>
        public static Sly4Addresses ForGame(string gameId)
        {
            Sly4Addresses found;
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
        private Sly4Addresses CopyAs(string gameId, string displayName)
        {
            Sly4Addresses copy = (Sly4Addresses)MemberwiseClone();
            copy.GameId = gameId;
            copy.DisplayName = displayName;
            return copy;
        }

        private static Dictionary<string, Sly4Addresses> CreateVersions()
        {
            Sly4Addresses palDisc = new Sly4Addresses
            {
                GameId = GameIdPALDisc,
                DisplayName = "SLY 4 (PAL, Disc)",

                // From the RaCMAN input viewer for NPEA00429; the pad object is at 0x30A1C040.
                inputButtons = 0x30A1C0E0,
                analogLeft = 0x30A1C060,
                analogRight = 0x30A1C068,

                gameManagerPtr = 0xF65CA0,
                episodeManagerPtr = 0xF64C20,

                coinCountOffset = 0x70,
                playerRefOffset = 0x1078,
                loadPhaseOffset = 0x32,
                inGameOffset = 0x2F,
                goalArrayOffset = 0x10C0,
                goalCountOffset = 0x10C8,
                currentLevelHashOffset = 0x128114,
                loadingLevelHashOffset = 0x128118,
                spawnCharacterOffset = 0x12811C,
                spawnPointOffset = 0x128160,

                episodeArrayOffset = 0x14,
                episodeCountOffset = 0x18,
                jobArrayOffset = 0x1C,
                jobCountOffset = 0x20,
                currentEpisodeOffset = 0x24,
                currentJobOffset = 0x28,
                failedGoalOffset = 0x4C,

                jobEpisodeIndexOffset = 0x1F4,

                goalStateOffset = 0x12,
                goalNameOffset = 0x1C,

                startJobByInstFunction = 0x2E0930,
                teleportToGoalFunction = 0x2E0498,
                loadLevelFunction = 0x4674C4,
                abandonCurrentJobFunction = 0x2E5060,

                hookCallSite = 0x5221DC,
                hookCallTarget = 0x56AEFC,
                hookCodeCave = 0xF05200,
                hookMailbox = 0x11F8A00,

                mainViewOffset = 0x128168,
                viewCameraMatrixOffset = 0x330,
                viewMatrixOffset = 0x2F0,
                viewRenderCopyOffset = 0x2A0,
                calcRenderMatricesFunction = 0x5115F0,
                viewOverrideRoutine = 0xF05300,
                viewOverrideData = 0x11F8A40,

                playerFlagsOffset = 0xC0,
                characterControllerOffset = 0x18C,

                controllerVelocityOffset = 0x250,
                characterBodyOffset = 0x20,

                rigidBodyOffset = 0x10,
                bodyPositionOffset = 0x120,

                clockClampFreqMax = 0x10B7AA4,
                clockClampFreqMin = 0x10B7A58,
                cheatInvulnerable = 0x10388F0,
                cheatUndetectable = 0xF9BC28,
                // Game.CutSceneEnable, 1 by default
                cutSceneEnable = 0x10B8B48,

                tunableRegistry = 0x10C2DBC,
                tunableCount = 0x10C597C,

                tunableVtableBool = 0xB0D934,
                tunableVtableInt = 0xB0D968,
                tunableVtableFloat = 0xB0D99C,
                tunableVtableVec2 = 0xB13940,
                tunableVtableVec3 = 0xB0D9D0,
                tunableVtableColor = 0xB0DA04,
                tunableVtableString = 0xB0DA38,
                tunableVtableStringHash = 0xB0DA6C,
            };

            Sly4Addresses palDigital = palDisc.CopyAs(GameIdPALDigital, "SLY 4 (PAL, Digital)");

            return new Dictionary<string, Sly4Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { palDisc.GameId, palDisc },
                { palDigital.GameId, palDigital },
            };
        }
    }
}
