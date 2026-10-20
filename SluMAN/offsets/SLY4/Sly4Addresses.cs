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
        // The view's vertical field of view in radians; CalcRenderMatrices builds the projection
        // from it (with tan(fov / 2)), the aspect ratio at +0x70 and near / far at +0x3C / +0x40
        public uint viewFovOffset { get; private set; }

        // View override (free camera): each frame the game rebuilds each view's render matrices
        // and then copies the view for drawing; that copy call is pointed at a stub (after the
        // code hook's in the code cave) which, for the main view, first copies in the matrices
        // kept in the data area (the view matrix, then the camera matrix) and rebuilds the render
        // matrices from them
        public uint viewUpdateCopyCall { get; private set; }
        public uint viewCopyFunction { get; private set; }
        public uint calcRenderMatricesFunction { get; private set; }
        public uint viewOverrideStub { get; private set; }
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
        // Hide HUD. FindInstance finds a GUI screen by name ("InGame.HUD"); the screen's vtable
        // entry at +hudShowVtableOffset points to the function descriptor (code, TOC) of a method
        // that shows (1) or hides (0) it. The descriptor's TOC must be the game's (gameToc), which
        // the code hook runs with. The game also shows InGame.HUD from a flag or forced on
        // (around 0x46ACAC); Hide HUD makes both arguments li r4, 0 so that doesn't undo it.
        // (Debug.NoHUD, at 0x10B7AF0, only applies outside gameplay and hides the menu cursors.)
        public uint findInstanceFunction { get; private set; }
        public uint hudShowVtableOffset { get; private set; }
        public uint gameToc { get; private set; }
        public uint hudScreenName { get; private set; }
        public uint hudInGameHudFlagArgument { get; private set; }
        public uint hudInGameHudForcedArgument { get; private set; }

        // The GUI tree (the GUI tab). *guiRootList is the first top-level node; each node has its
        // name's hash (sly4.Hash of one dotted part) at +guiNodeHashOffset, its parent, first
        // child and next sibling at the next offsets. Names are found by hashing the strings in
        // the read-only data from guiNamesStart to guiNamesEnd.
        public uint guiRootList { get; private set; }
        public uint guiNodeHashOffset { get; private set; }
        public uint guiNodeParentOffset { get; private set; }
        public uint guiNodeFirstChildOffset { get; private set; }
        public uint guiNodeNextSiblingOffset { get; private set; }
        public uint guiNamesStart { get; private set; }
        public uint guiNamesEnd { get; private set; }
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
                viewFovOffset = 0x38,
                viewUpdateCopyCall = 0x512078,
                viewCopyFunction = 0x5109B0,
                calcRenderMatricesFunction = 0x5115F0,
                // Not 0xF05300: earlier versions put other code there, which a game left running
                // since then may still call.
                viewOverrideStub = 0xF05400,
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
                findInstanceFunction = 0x4D5544,
                hudShowVtableOffset = 88,
                gameToc = 0xF50800,
                // Unused end of the data segment, after the view override's data.
                hudScreenName = 0x11F8B00,

                guiRootList = 0xF66868,
                guiNodeHashOffset = 0x80,
                guiNodeParentOffset = 0x94,
                guiNodeFirstChildOffset = 0x98,
                guiNodeNextSiblingOffset = 0x9C,
                guiNamesStart = 0xAB0000,
                guiNamesEnd = 0xB70000,
                hudInGameHudFlagArgument = 0x46ACCC,
                hudInGameHudForcedArgument = 0x46AD00,
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
