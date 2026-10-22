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
        public uint language { get; private set; } // 0 on versions with one language
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

        // Cameras (the CM struct). cameraPointer is the main one; the game also keeps a count and
        // an array of pointers to all of them.
        public uint cameraCount { get; private set; }
        public uint cameraArray { get; private set; }
        // 4x4: X, Y, Z axes, then position. Where the PS2 version has it; the free camera checks
        // it holds a camera matrix and searches the struct when it doesn't (sly3.FindCameraMatrix).
        public uint cameraMatrixOffset { get; private set; }
        // Copies of the camera matrix the game draws from, which it copies the one above into
        // every frame: one in the camera struct and one in the game's data. Writing only the
        // matrix above doesn't move the view; the free camera writes these too.
        public uint cameraDrawMatrixOffset { get; private set; }
        public uint cameraDrawMatrix { get; private set; }
        // Camera hook (free camera without flicker): every object's world matrix (+0x90) is
        // updated from its local one (+0x50) by one function, when the object is marked changed
        // (+0x44 negative) and has no parent (+0x40). Its first instruction is pointed at a stub,
        // written over a function the game never calls, which for the camera SluMAN drives first
        // copies in SluMAN's matrix from the stub's data (unused end of the data segment).
        public uint worldMatrixUpdateFunction { get; private set; }
        public uint worldMatrixUpdateFirstInstruction { get; private set; }
        public uint cameraHookStub { get; private set; }
        // The never-called function's first words, which the stub may only be written over.
        public uint[] cameraHookStubOriginal { get; private set; }
        public uint cameraHookData { get; private set; }
        public uint objectParentOffset { get; private set; }
        public uint objectChangedOffset { get; private set; }
        public uint cameraFarClipOffset { get; private set; }
        public uint cameraFovTargetOffset { get; private set; } // the game eases its field of view to it

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
                language = 0x7E28F0, // From SluMAN-EXT (utility.lua)
                slyCharacterPtr = 0x5ED940,
                activeCharacterPtr = 0x5EC654,
                playerEntityPointer = 0x5EC654,

                transformOffset = 0x44,
                coordsOffsetX = 0x130,
                coordsOffsetY = 0x134,
                coordsOffsetZ = 0x138,
                healthEntityOffset = 0x168,
                gadgetPowerEntityOffset = 0x170,

                // Writing 0 ends the dialogue line; frozen to 0, no line plays. Behaves better
                // than the dialogue's own state at 0x39B13F70.
                dialogueState = 0x788CB8,
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

                cameraCount = 0x7DA530,
                cameraArray = 0x7DA534,
                cameraMatrixOffset = 0x50,
                // Raising either one's position moves the view (Find camera); 0x7D6590, another
                // copy, doesn't.
                cameraDrawMatrixOffset = 0x90,
                cameraDrawMatrix = 0x7DA890,
                cameraFarClipOffset = 0x114,

                worldMatrixUpdateFunction = 0x220118,
                worldMatrixUpdateFirstInstruction = 0xF821FF41, // stdu r1, -0xC0(r1)
                // A small math function (0x80 bytes) nothing calls, branches into or points to.
                // Not past the code segment's end: RPCS3's static interpreter can only run code
                // inside the segment, and jumped to 0 there.
                cameraHookStub = 0x22C2D4,
                cameraHookStubOriginal = new uint[]
                {
                    0xF821FF51, 0x7C0802A6, 0xDBA10098, 0xFFA00890, 0xEC230828, 0xDBC100A0,
                    0xDBE100A8, 0xFFC02090, 0xFFE01090, 0xF80100C0, 0xFB810078, 0xFBA10080,
                    0x7D1C4378, 0x7CFD3B78, 0x4BFFFF6D, 0x7BA70020, 0xFC600890, 0x7B880020,
                    0xFC40F890, 0xC022051C, 0xFC80F090, 0x4BFFFBA1,
                },
                // The data segment ends at 0x8ABAC8, in a page the game has mapped.
                cameraHookData = 0x8ABB00,
                objectParentOffset = 0x40,
                objectChangedOffset = 0x44,
                cameraFovTargetOffset = 0x11C,

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
