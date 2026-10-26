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
        // Which parts of the game read the controller (g_grfjoyt): bit inputTargetPlayer is the
        // player (Sly's moves and the binocucom). The game works it out from its state flags
        // (g_grfusr) whenever they change: 0 if any of userStateOff, else 5 with
        // userStateMenu, else 4 with userStateHandsOff (cutscenes), else 7.
        public uint inputTargets { get; private set; }
        public uint userState { get; private set; }

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
        // Powerups (thief moves): the unlocked ones as bits at gameState + powerupsOffset (on PS3
        // 4 further on than on PS2), the last one equipped at gameState + lastPowerupOffset (-1
        // for none), and the one equipped now at equippedPowerup (0 Dive to 6 Stun, -1 none).
        public uint powerupsOffset { get; private set; }
        public uint lastPowerupOffset { get; private set; }
        public uint equippedPowerup { get; private set; }

        // The camera (the CM struct, laid out as on the PS2 version). *cameraPointer is the main
        // one: its position (a vector) at +cameraPositionOffset, its rotation (forward, left and
        // up rows, each a vector) at +cameraRotationOffset, the LOD scale the game works out from
        // the field of view (max(1, *(+cameraLodFovOffset) / fov)) at +cameraLodScaleOffset, the
        // projection matrix at +cameraProjectionOffset, the field of view (radians, vertical) at
        // +cameraFovOffset (eased to +cameraFovTargetOffset; the renderer is also handed it, not
        // only the matrices), the aspect ratio at +cameraAspectOffset, and the frustum (tan of half
        // the field of view times the aspect ratio, then tan of half the field of view) at
        // +cameraFrustumOffset. The fog amount (0 to 1) is at +cameraFogOffset.
        public uint cameraPointer { get; private set; }
        public uint cameraPositionOffset { get; private set; }
        public uint cameraRotationOffset { get; private set; }
        public uint cameraLodScaleOffset { get; private set; }
        public uint cameraProjectionOffset { get; private set; }
        public uint cameraLodFovOffset { get; private set; }
        public uint cameraFovOffset { get; private set; }
        public uint cameraFovTargetOffset { get; private set; }
        public uint cameraAspectOffset { get; private set; }
        public uint cameraFrustumOffset { get; private set; }
        public uint cameraFogOffset { get; private set; }

        // Camera hook (free camera): the game's camera matrix update (UpdateCmMat4, the camera in
        // r3) works out the matrices everything is drawn with from the camera's position,
        // rotation, projection and frustum, and every place that sets the camera (its field of
        // view too) calls it just
        // after. Every call to it (bl, and one tail call with b) is pointed at a stub in padding
        // between two ELF sections, which for the camera SluMAN drives first copies SluMAN's
        // values in from the stub's data (past the end of BSS, in a page the game has mapped).
        // RPCS3's static interpreter fixes the kind of instruction at every address it found code
        // at, so only bl to bl and b to b, and the stub must be in bytes of the code segment that
        // were never code.
        public uint cameraMatrixUpdateFunction { get; private set; }
        public uint[] cameraMatrixUpdateCalls { get; private set; }
        public uint[] cameraMatrixUpdateTailCalls { get; private set; }
        public uint cameraHookStub { get; private set; }
        public uint cameraHookData { get; private set; }

        // Hide HUD: every HUD element is a blot, drawn through its vtable's Draw entry (a function
        // descriptor's address) at +blotDrawSlotOffset. Each row is a blot vtable and the Draw
        // entry it has; Hide HUD points them all at emptyFunctionDescriptor (a function that only
        // returns). The vtables are data, so this works with every RPCS3 PPU decoder.
        public uint[,] blotVtableDraws { get; private set; }
        public uint blotDrawSlotOffset { get; private set; }
        public uint emptyFunctionDescriptor { get; private set; }

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
                // From UpdateGrfjoytFromGrfusr (0x83B78)
                inputTargets = 0x428B4C,
                userState = 0x428B48,
                // *0x3A4FC0; w3Keys above is world 3's key count in it
                gameState = 0x3E660C,
                worldSaveStart = 0x10,
                worldSaveSize = 0x44C,
                levelSaveSize = 0x78,
                worldKeysOffset = 0x438,
                worldVaultsOffset = 0x43C,
                worldSprintsOffset = 0x440,
                worldFlagsOffset = 0x448,
                // From InitGameState (0x704CC) and UpdateJtActivePowerUp (0x1218E0)
                powerupsOffset = 0x19F4,
                lastPowerupOffset = 0x1A00,
                equippedPowerup = 0x39F9B0,

                cameraPointer = 0x3E6340,
                cameraPositionOffset = 0x40,
                cameraRotationOffset = 0x80,
                cameraLodScaleOffset = 0xB0,
                cameraProjectionOffset = 0xC0,
                cameraLodFovOffset = 0x1C0,
                cameraFovOffset = 0x1C4,
                cameraFovTargetOffset = 0x1C8,
                cameraAspectOffset = 0x1E0,
                cameraFrustumOffset = 0x1F0,
                cameraFogOffset = 0x200,

                cameraMatrixUpdateFunction = 0x4B0CC,
                // SetCm, SetCmLookAtSmooth, SetCmPosMat, UpdateCplook
                cameraMatrixUpdateCalls = new uint[] { 0x4B78C, 0x4B954, 0x4BD3C, 0x52088 },
                // RecalcCmFrustrum
                cameraMatrixUpdateTailCalls = new uint[] { 0x4BE74 },
                // Padding between two ELF sections (0x2FA298 to 0x2FA300), zeros that aren't code.
                cameraHookStub = 0x2FA298,
                // BSS ends at 0xFC02C8.
                cameraHookData = 0xFC0300,

                blotVtableDraws = new uint[,]
                {
                    { 0x3ABE70, 0x388540 }, { 0x3ABEBC, 0x388530 }, { 0x3ABF08, 0x388530 }, { 0x3ABF54, 0x388530 },
                    { 0x3ABFA0, 0x388530 }, { 0x3ABFEC, 0x388530 }, { 0x3AC038, 0x388530 }, { 0x3AC084, 0x388530 },
                    { 0x3AC0D0, 0x388530 }, { 0x3AC11C, 0x388530 }, { 0x3AC168, 0x388530 }, { 0x3AC1B4, 0x388530 },
                    { 0x3AC200, 0x388530 }, { 0x3AC24C, 0x388530 }, { 0x3AC298, 0x388530 }, { 0x3AC2E4, 0x388530 },
                    { 0x3AC330, 0x388530 }, { 0x3AC37C, 0x388580 }, { 0x3AC3C8, 0x388550 }, { 0x3AC414, 0x388548 },
                    { 0x3AC460, 0x3885A0 }, { 0x3AC4AC, 0x388568 }, { 0x3AC4F8, 0x388590 }, { 0x3AC544, 0x388578 },
                    { 0x3AC590, 0x388558 }, { 0x3AC5DC, 0x388598 }, { 0x3AC628, 0x388520 }, { 0x3AC674, 0x388540 },
                    { 0x3AC73C, 0x387760 }, { 0x3AC788, 0x3850F0 }, { 0x3AE3C8, 0x38A920 }, { 0x3B4AC0, 0x386AB8 },
                    { 0x3B4FB0, 0x38B308 }, { 0x3B5194, 0x386388 }, { 0x3B51E0, 0x386380 }, { 0x3B522C, 0x38A618 },
                },
                blotDrawSlotOffset = 0x24,
                // The descriptor of 0x10750, a lone blr.
                emptyFunctionDescriptor = 0x383588,
            };

            return new Dictionary<string, Sly1Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { us.GameId, us },
            };
        }
    }
}
