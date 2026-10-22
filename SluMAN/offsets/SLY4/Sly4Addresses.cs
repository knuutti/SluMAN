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
        // It's on the heap, so it moves when the heap layout does (with the archive replaced by
        // loose files, for one); padPtr is the static pointer the game keeps to player 1's pad,
        // set once at boot. Buttons are the two button bytes as a 4-byte word: 00, Select..Left,
        // 00, L2..Square. Each stick is two big-endian floats, X then Y.
        public uint padPtr { get; private set; }
        public uint padButtonsOffset { get; private set; }
        public uint padAnalogLeftOffset { get; private set; }
        public uint padAnalogRightOffset { get; private set; }

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
        // The ID StartJobByInst's third argument is matched against
        public uint goalIdOffset { get; private set; }

        // Game functions, called through the code hook
        public uint startJobByInstFunction { get; private set; }
        public uint teleportToGoalFunction { get; private set; }
        public uint loadLevelFunction { get; private set; }
        public uint abandonCurrentJobFunction { get; private set; }

        // Code hook: a call made once per frame is pointed at a stub in the unused end of the code
        // segment, which runs the call queued in the mailbox (unused end of the data segment).
        // The call is the main loop's call to the frame's game update, on the main thread, where
        // the game makes these calls itself (StartJobByInst runs inside that update).
        public uint hookCallSite { get; private set; }
        public uint hookCallTarget { get; private set; }
        // The call SluMAN 1.1.1 and earlier hooked instead, and the stub it ran (the start of
        // SluMAN's code in the cave). The call is made on AglRenderThread, and game logic run
        // there races the render thread: starting a job in the current episode crashes the game.
        // Put back on install, in case a game is still hooked there.
        public uint legacyHookCallSite { get; private set; }
        public uint legacyHookCallTarget { get; private set; }
        public uint legacyHookCodeCave { get; private set; }
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
        // The view's far clipping distance (near is just before it, at +0x3C)
        public uint viewFarOffset { get; private set; }
        // The view's two fog blocks, each: 2 flag bytes and the colour as RGBA bytes (at -8),
        // then the fog's start and end distances (floats), then the colour as 4 floats. The
        // level's Fog settings are copied here when it loads.
        public uint[] viewFogDistanceOffsets { get; private set; }

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
        // Hide HUD: the UI.3D.FOV tunable's value (a float, 65 by default; the tunable is at
        // 0x10B96C4). The HUD is drawn in 3D with this field of view, so at 0 it's off screen.
        // (Debug.NoHUD only applies outside gameplay and hides the menu cursors; hiding the
        // InGame.HUD screen doesn't hide the health bar or radar.)
        public uint uiFov { get; private set; }

        // A GUI node's vtable entry at +hudShowVtableOffset points to the function descriptor
        // (code, TOC) of a method that shows (1) or hides (0) it. The descriptor's TOC must be the
        // game's (gameToc), which the code hook runs with.
        public uint hudShowVtableOffset { get; private set; }
        public uint gameToc { get; private set; }

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

                // The pad pointer is in the player array that the boot code at 0x463000 fills
                // (0x10B19E0, a 0x2EC0-byte entry per player, the pad at +0xCC). The buttons are
                // in the CellPadData the pad reads into at +0x98.
                padPtr = 0x10B1AAC,
                padButtonsOffset = 0xA0,
                padAnalogLeftOffset = 0x20,
                padAnalogRightOffset = 0x28,

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
                goalIdOffset = 0xA00,

                startJobByInstFunction = 0x2E0930,
                teleportToGoalFunction = 0x2E0498,
                loadLevelFunction = 0x4674C4,
                abandonCurrentJobFunction = 0x2E5060,

                hookCallSite = 0x463D20,
                hookCallTarget = 0x45B858,
                legacyHookCallSite = 0x5221DC,
                legacyHookCallTarget = 0x56AEFC,
                legacyHookCodeCave = 0xF05200,
                // Not 0xF05200, where the old stub was: RPCS3 keeps running code it has already
                // run as it was decoded then, so a stub rewritten in place in a game the old one
                // ran in mixes the two and crashes.
                hookCodeCave = 0xF05500,
                hookMailbox = 0x11F8A00,

                mainViewOffset = 0x128168,
                viewCameraMatrixOffset = 0x330,
                viewMatrixOffset = 0x2F0,
                viewFovOffset = 0x38,
                viewFarOffset = 0x40,
                viewFogDistanceOffsets = new uint[] { 0x88, 0xC8 },
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
                uiFov = 0x10B970C,
                hudShowVtableOffset = 88,
                gameToc = 0xF50800,

                guiRootList = 0xF66868,
                guiNodeHashOffset = 0x80,
                guiNodeParentOffset = 0x94,
                guiNodeFirstChildOffset = 0x98,
                guiNodeNextSiblingOffset = 0x9C,
                guiNamesStart = 0xAB0000,
                guiNamesEnd = 0xB70000,
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
