using System;
using System.Linq;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN set the gameplay camera. SetCamera (view, matrix) copies a camera's matrix into
    /// a view and builds the view from it; its first instruction is pointed at a stub that, for the
    /// main view only, notes who called (to tell the gameplay camera from menus) and, while the
    /// free camera is on, passes the matrix kept in the data area instead. Every other view (UI,
    /// shadows) keeps the game's matrix.
    ///
    /// Data layout: +0x0 on (non-zero), +0x4 the main view's address, +0x8 the last caller for the
    /// main view (written by the stub), +0xC only replace calls from this caller (0 for any),
    /// +0x10 the matrix (4 rows of 4 big-endian floats: the camera's axes down the first three
    /// columns, its position in the last row; 16-aligned for the game's vector loads).
    ///
    /// Like the code hook, RPCS3 only runs the patched code with the Interpreter PPU decoder.
    /// </summary>
    public class Sly4CameraHook
    {
        public const int MatrixSize = 0x40;

        private const uint ViewOffset = 0x4;
        private const uint LastCallerOffset = 0x8;
        private const uint CallerFilterOffset = 0xC;
        private const uint MatrixOffset = 0x10;

        // SetCamera's first instruction: lfs f1, 0(r4).
        private const uint OriginalInstruction = 0xC0240000;

        // How long the game camera's own matrix is handed back before the switch turns off, so a
        // camera that only sends its matrix when it moves still ends up where it was.
        private const int RestoreMs = 100;

        private readonly IGame game;

        public Sly4CameraHook(IGame game)
        {
            this.game = game;
        }

        /// <summary>Uses the matrix for the view from now on (written before the switch).</summary>
        public void Enable(uint view, byte[] matrix)
        {
            Install(view);
            SetMatrix(matrix);
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(1));
        }

        public void SetMatrix(byte[] matrix)
        {
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + MatrixOffset, matrix);
        }

        /// <summary>
        /// Gives the view back to the game's camera, first putting back its matrix from before the
        /// free camera (pass null to skip). The hook stays in place.
        /// </summary>
        public void Disable(byte[] gameMatrix)
        {
            if (gameMatrix != null)
            {
                SetMatrix(gameMatrix);
                Thread.Sleep(RestoreMs);
            }
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
        }

        /// <summary>The address that last set the main view's camera, 0 if none yet.</summary>
        public uint GetLastCaller()
        {
            return ReadUInt(sly4.addr.freeCameraData + LastCallerOffset);
        }

        /// <summary>Puts SetCamera back as it was. Call before disconnecting.</summary>
        public void Uninstall()
        {
            if (ReadUInt(sly4.addr.setCameraFunction) == HookBranch())
            {
                game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
                game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(OriginalInstruction));
            }
            // An earlier version hooked the gameplay camera's calls instead.
            foreach (uint call in sly4.addr.gameplayCameraCalls)
            {
                if (ReadUInt(call) == Branch(call, sly4.addr.freeCameraStub, true))
                {
                    game.api.WriteMemory(game.pid, call, ToBytes(Branch(call, sly4.addr.setCameraFunction, true)));
                }
            }
        }

        private void Install(uint view)
        {
            byte[] stub = BuildStub();
            uint first = ReadUInt(sly4.addr.setCameraFunction);
            bool stubCurrent = game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length).SequenceEqual(stub);
            if (first == HookBranch() && stubCurrent)
            {
                game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + ViewOffset, ToBytes(view));
                return;
            }
            if (first != OriginalInstruction && first != HookBranch())
            {
                throw new InvalidOperationException("The game's camera code isn't what SluMAN expects, so the free camera isn't available on this version.");
            }

            // Unhook while the stub is (re)written, so the game never runs it half-written.
            Uninstall();
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, new byte[MatrixOffset]);
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + ViewOffset, ToBytes(view));
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraStub, stub);
            if (!game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length).SequenceEqual(stub))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            // Last, so the game never jumps to a half-written stub.
            game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(HookBranch()));
        }

        private uint HookBranch()
        {
            return Branch(sly4.addr.setCameraFunction, sly4.addr.freeCameraStub, false);
        }

        /// <summary>
        /// The stub's PowerPC code: r3 is the view, r4 the matrix, and the link register the
        /// caller. It only uses r5 to r7, which SetCamera sets before reading: SetCamera leaves
        /// r0 and r9 to r12 alone, so its callers may keep values there across the call.
        /// </summary>
        private byte[] BuildStub()
        {
            uint data = sly4.addr.freeCameraData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint stub = sly4.addr.freeCameraStub;

            uint[] words =
            {
                0x3CA00000 | high,               //  0 lis r5, data@ha
                0x38A50000 | low,                //  1 addi r5, r5, data@l
                0x80C50000 | ViewOffset,         //  2 lwz r6, view(r5)
                0x7C033000,                      //  3 cmpw r3, r6
                0x40820030,                      //  4 bne +0x30 (to 16, the original instruction)
                0x7CC802A6,                      //  5 mflr r6
                0x90C50000 | LastCallerOffset,   //  6 stw r6, caller(r5)
                0x80E50000,                      //  7 lwz r7, 0(r5)
                0x2C070000,                      //  8 cmpwi r7, 0
                0x4182001C,                      //  9 beq +0x1C (to 16)
                0x80E50000 | CallerFilterOffset, // 10 lwz r7, filter(r5)
                0x2C070000,                      // 11 cmpwi r7, 0
                0x4182000C,                      // 12 beq +0xC (to 15: any caller)
                0x7C063800,                      // 13 cmpw r6, r7
                0x40820008,                      // 14 bne +8 (to 16)
                0x38850000 | MatrixOffset,       // 15 addi r4, r5, matrix
                OriginalInstruction,             // 16 lfs f1, 0(r4)
                Branch(stub + 17 * 4, sly4.addr.setCameraFunction + 4, false),
            };

            byte[] bytes = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(ToBytes(words[i]), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        /// <summary>A relative b (or bl) instruction from one address to another.</summary>
        private static uint Branch(uint from, uint to, bool link)
        {
            return 0x48000000 | ((to - from) & 0x3FFFFFC) | (link ? 1u : 0u);
        }

        private uint ReadUInt(uint address)
        {
            byte[] bytes = game.api.ReadMemory(game.pid, address, 4);
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }

        private static byte[] ToBytes(uint value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
    }
}
