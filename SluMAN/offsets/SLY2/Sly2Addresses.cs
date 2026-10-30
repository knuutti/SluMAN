using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 2 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly2Addresses
    {
        public const string GameIdKorDigital = "NPHA80175";
        public const string GameIdNtscDigital = "NPUA80664";
        public const string GameIdPalDigital = "NPEA00342";

        public const string DefaultGameId = GameIdKorDigital;

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
        public uint vjoyComponentOffset { get; private set; }
        public uint entityIsControllableOffset { get; private set; }
        public uint coordsOffsetX { get; private set; }
        public uint coordsOffsetY { get; private set; }
        public uint coordsOffsetZ { get; private set; }

        // Utility
        public uint coinCount { get; private set; }
        public uint language { get; private set; } // 0 on versions with one language
        public uint currentCharacter { get; private set; }
        // The field of view the camera eases to; the one it has is 4 bytes before.
        public uint cameraFov { get; private set; }
        public uint cameraParameter { get; private set; }
        public uint hudScaleX { get; private set; }
        public uint hudScaleY { get; private set; }
        public uint hudPositionX { get; private set; }
        public uint hudPositionY { get; private set; }
        public uint fogAmount { get; private set; }
        // How far the game draws (a float, set per map: 75000 on one), or 0 when unknown.
        public uint renderDistance { get; private set; }
        // A camera matrix copy found in the global camera region. The rendered camera may be
        // made from another copy; the Camera research tab searches for that source.
        public uint cameraMatrixAddress { get; private set; }
        // The matrix the renderer consumes directly, confirmed on the KOR build.
        public uint cameraRenderMatrix { get; private set; }
        // Camera hook (free camera): the camera object (static, its matrix at +0x60, the
        // position at +0x90) is set by a look-at and a matrix setter, which both end in one
        // function that rebuilds everything drawn from the matrix (view matrices, the frustum
        // culling uses). Its call (bl) and the two jumps to it (b) are pointed at a stub in
        // padding between two ELF sections, which for the camera first copies in SluMAN's matrix
        // from the stub's data (past the data segment's end). RPCS3's static interpreter keeps
        // the kind of instruction it found at an address, so the sites stay bl and b.
        public uint cameraObject { get; private set; }
        public uint cameraRebuildFunction { get; private set; }
        public uint[] cameraRebuildCalls { get; private set; }
        public uint cameraHookCaveAddress { get; private set; }
        public uint cameraHookData { get; private set; }
        // Orthographic free camera. The PS3 renderer draws the world through its own view
        // (renderView), whose projection it builds from the field of view, aspect, near and far
        // the camera rebuild hands it: the frame setup calls SetPerspective (setPerspectiveCall
        // to setPerspectiveFunction, which sets the view's type at +0x30 to 1, puts the matrix
        // at +0xB0 and ends in viewUpdateFunction, which works out everything else from it). The
        // render stub (orthoRenderStub, which goes on at orthoRenderStubTail) puts in SluMAN's
        // orthographic matrix instead. The game's own culling uses a cone from the camera, built
        // by frustumBuildFunction (its two calls in the rebuild, frustumBuildCalls) from how wide
        // it is each way; the frustum stub (orthoFrustumStub) widens it to the half of the world
        // in front of the camera. Both stubs read the hook data, and 0 here means no
        // orthographic camera. NTSC's padding is the smallest, which is why the render stub is
        // in two parts.
        public uint renderView { get; private set; }
        public uint setPerspectiveCall { get; private set; }
        public uint setPerspectiveFunction { get; private set; }
        public uint viewUpdateFunction { get; private set; }
        public uint[] frustumBuildCalls { get; private set; }
        public uint frustumBuildFunction { get; private set; }
        public uint orthoRenderStub { get; private set; }
        public uint orthoRenderStubTail { get; private set; }
        public uint orthoFrustumStub { get; private set; }
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

        // Start of each episode's save data, indexed by episode (0 = prologue). Job loading and
        // run files write their saved bytes here.
        public uint[] episodeData { get; private set; }

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
            Sly2Addresses korDigital = new Sly2Addresses
            {
                GameId = GameIdKorDigital,
                DisplayName = "SLY 2 (KOR, PSN)",

                inputOffset = 0x500F76,
                analogOffsetLeft = 0x500EFC,
                analogOffsetRight = 0x500F30,

                slyCharacterPtr = 0x502100,
                activeCharacterPtr = 0x49E290,

                transformOffset = 0x44,
                vjoyComponentOffset = 0x140,
                entityIsControllableOffset = 0x80, // 0 = not controllable, 1 = controllable
                coordsOffsetX = 0x130,
                coordsOffsetY = 0x134,
                coordsOffsetZ = 0x138,

                coinCount = 0x7A83B0,
                currentCharacter = 0x7A830C,
                cameraFov = 0x49E054,
                cameraParameter = 0x49E07C,
                hudScaleX = 0x45DB40,
                hudScaleY = 0x45DB4C,
                hudPositionX = 0x45DB50,
                hudPositionY = 0x45DB54,
                fogAmount = 0x49E278,
                renderDistance = 0x49E080,
                cameraMatrixAddress = 0x49E090,
                cameraRenderMatrix = 0x8C2210,
                cameraObject = 0x49E030,
                cameraRebuildFunction = 0x3ECCC,
                cameraRebuildCalls = new uint[] { 0x3F36C, 0x3F3D4, 0x3F718 },
                // Padding from 0x344988 to 0x344A00; the data segment ends at 0x975548.
                cameraHookCaveAddress = 0x344990,
                cameraHookData = 0x975580,
                renderView = 0x8C24B0,
                setPerspectiveCall = 0x1B086C,
                setPerspectiveFunction = 0x1C8250,
                viewUpdateFunction = 0x1C7F70,
                frustumBuildCalls = new uint[] { 0x3EFC8, 0x3F054 },
                frustumBuildFunction = 0x3E938,
                // In padding between ELF sections, 0x317D28 to 0x317D80.
                orthoRenderStub = 0x317D28,
                orthoRenderStubTail = 0x317D54,
                orthoFrustumStub = 0x317D64,
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

                episodeData = new uint[]
                {
                    0x7A9590, 0x7A96D8, 0x7A9E8C, 0x7AA5CC, 0x7AAC34,
                    0x7AB4CC, 0x7ABCC4, 0x7AC264, 0x7ACBEC,
                },

                healthSly = 0x7A8360,
                healthBentley = 0x7A8378,
                healthMurray = 0x7A8390,
                juiceSly = 0x7A8364,
                juiceBentley = 0x7A837C,
                juiceMurray = 0x7A8394,
            };

            Sly2Addresses ntscDigital = korDigital.CopyAs(GameIdNtscDigital, "SLY 2 (NTSC, PSN)");
            Sly2Addresses palDigital = korDigital.CopyAs(GameIdPalDigital, "SLY 2 (PAL, PSN)");

            ntscDigital.cameraHookCaveAddress = 0x344290;
            // The renderer's code is 0x69C earlier than KOR's; the camera rebuild is where KOR
            // has it. The padding at 0x317650 only fits the render stub's head; its tail and the
            // frustum stub go after the camera hook stub, to 0x344300.
            ntscDigital.setPerspectiveCall = 0x1B01D0;
            ntscDigital.setPerspectiveFunction = 0x1C7BB4;
            ntscDigital.viewUpdateFunction = 0x1C78D4;
            ntscDigital.orthoRenderStub = 0x317650;
            ntscDigital.orthoRenderStubTail = 0x3442DC;
            ntscDigital.orthoFrustumStub = 0x3442EC;
            ntscDigital.hudScaleX = 0x45DB60;
            ntscDigital.hudScaleY = 0x45DB6C;
            ntscDigital.hudPositionX = 0x45DB70;
            ntscDigital.hudPositionY = 0x45DB74;

            palDigital.coinCount = 0x7A8330;
            palDigital.currentCharacter = 0x7A828C;
            palDigital.healthSly = 0x7A82E0;
            palDigital.healthBentley = 0x7A82F8;
            palDigital.healthMurray = 0x7A8310;
            palDigital.juiceSly = 0x7A82E4;
            palDigital.juiceBentley = 0x7A82FC;
            palDigital.juiceMurray = 0x7A8314;
            palDigital.gadgetUnlocks = 0x7A8328;
            palDigital.gadgetBindsSly = 0x7A82EC;
            palDigital.gadgetBindsBentley = 0x7A8304;
            palDigital.gadgetBindsMurray = 0x7A831C;
            palDigital.language = 0x819E88;
            palDigital.gameSpeed = 0x49DF14;
            palDigital.loadTrigger = 0x7B4BD0;
            palDigital.mapAOB = 0x7B4BD8;
            palDigital.spawnLocation = 0x7B4C18;
            palDigital.loadType = 0x7B4BD4;
            palDigital.inputOffset = 0x500EF6;
            palDigital.analogOffsetLeft = 0x500E7C;
            palDigital.analogOffsetRight = 0x500EB0;
            palDigital.activeCharacterPtr = 0x49E210;
            palDigital.slyCharacterPtr = 0x502080;
            palDigital.currentJobId = 0x4FEB74;
            palDigital.currentCheckpointId = 0x4FEB78;
            palDigital.pauseMenuState = 0x4FFE04;
            palDigital.fmvState = 0x90667C;
            palDigital.rajanHealth = 0x35C2E4EC;
            palDigital.loadingState = 0x7A7180;
            palDigital.currentMapId = 0x7B4C60;
            palDigital.cameraFov = 0x49DFD4;
            palDigital.cameraParameter = 0x49DFFC;
            palDigital.hudScaleX = 0x45DAC0;
            palDigital.hudScaleY = 0x45DACC;
            palDigital.hudPositionX = 0x45DAE0;
            palDigital.hudPositionY = 0x45DAE4;
            palDigital.fogAmount = 0x49E1F8;
            // Unchecked: where the 0x80 shift of the other camera globals puts it.
            palDigital.renderDistance = 0x49E000;
            palDigital.cameraMatrixAddress = 0x49E010;
            palDigital.cameraRenderMatrix = 0x8C2190;
            palDigital.cameraObject = 0x49DFB0;
            palDigital.cameraHookCaveAddress = 0x344310;
            // The renderer's code is 0x698 earlier than KOR's, its view 0x80 earlier; the
            // camera rebuild is where KOR has it. Padding from 0x317688 to 0x317700.
            palDigital.renderView = 0x8C2430;
            palDigital.setPerspectiveCall = 0x1B01D4;
            palDigital.setPerspectiveFunction = 0x1C7BB8;
            palDigital.viewUpdateFunction = 0x1C78D8;
            palDigital.orthoRenderStub = 0x317688;
            palDigital.orthoRenderStubTail = 0x3176B4;
            palDigital.orthoFrustumStub = 0x3176C4;
            // palDigital.cameraHookData = 0;

            palDigital.episodeData = new uint[]
            {
                0x7A9510, 0x7A9658, 0x7A9E0C, 0x7AA54C, 0x7AABB4,
                0x7AB44C, 0x7ABC44, 0x7AC1E4, 0x7ACB6C,
            };
            palDigital.parisCinemaState = 0x7A9A40;
            palDigital.parisStarted = 0x7A9D04;
            palDigital.templeStarted = 0x7AA970;
            palDigital.prisonStarted = 0x7AB30C;
            palDigital.castleStarted = 0x7ABB68;
            palDigital.trainStarted = 0x7ABEA8;
            palDigital.sawmillStarted = 0x7AC798;
            palDigital.blimpStarted = 0x7AD0D0;

            return new Dictionary<string, Sly2Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { korDigital.GameId, korDigital },
                { ntscDigital.GameId, ntscDigital },
                { palDigital.GameId, palDigital },
            };
        }
    }
}
