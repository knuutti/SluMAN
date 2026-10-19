using System;
using System.Linq;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN set the main view's camera. SetCamera (view, matrix) copies a camera's matrix
    /// into a view and builds the view from it; its first instruction is pointed at a stub that,
    /// while the free camera is on and the view is the main one, passes the matrix kept in the data
    /// area instead. Every other view (UI, shadows) keeps the game's matrix.
    ///
    /// Data layout: +0x0 on (non-zero), +0x4 the main view's address, +0x10 the matrix (X, Y and
    /// Z axes, then the position, 4 big-endian floats each; 16-aligned for the game's vector loads).
    ///
    /// Like the code hook, RPCS3 only runs the patched code with the Interpreter PPU decoder.
    /// </summary>
    public class Sly4CameraHook
    {
        public const int MatrixSize = 0x40;

        private const uint MatrixOffset = 0x10;

        // SetCamera's first instruction: lfs f1, 0(r4).
        private const uint OriginalInstruction = 0xC0240000;

        private readonly IGame game;

        public Sly4CameraHook(IGame game)
        {
            this.game = game;
        }

        /// <summary>Uses the matrix for the view from now on (the matrix is written first).</summary>
        public void Enable(uint view, byte[] matrix)
        {
            Install();
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + MatrixOffset, matrix);
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + 4, ToBytes(view));
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(1));
        }

        public void SetMatrix(byte[] matrix)
        {
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + MatrixOffset, matrix);
        }

        /// <summary>Gives the view back to the game's camera. The hook stays in place.</summary>
        public void Disable()
        {
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
        }

        /// <summary>Puts SetCamera back as it was. Call before disconnecting.</summary>
        public void Uninstall()
        {
            if (ReadUInt(sly4.addr.setCameraFunction) == HookBranch())
            {
                game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
                game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(OriginalInstruction));
            }
        }

        private void Install()
        {
            uint first = ReadUInt(sly4.addr.setCameraFunction);
            byte[] stub = BuildStub();
            if (first == HookBranch())
            {
                byte[] current = game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length);
                if (current.SequenceEqual(stub))
                {
                    return;
                }
                // An older stub: unhook while it's replaced.
                game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(OriginalInstruction));
            }
            else if (first != OriginalInstruction)
            {
                throw new InvalidOperationException("The game's camera code isn't what SluMAN expects, so the free camera isn't available on this version.");
            }

            // Off until Enable writes a matrix.
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraStub, stub);
            byte[] written = game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length);
            if (!written.SequenceEqual(stub))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            // Last, so the game never jumps to a half-written stub.
            game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(HookBranch()));
        }

        private uint HookBranch()
        {
            return Branch(sly4.addr.setCameraFunction, sly4.addr.freeCameraStub);
        }

        /// <summary>
        /// The stub's PowerPC code. r11 and r12 are free to use at a function's start; r3 is the
        /// view and r4 the matrix.
        /// </summary>
        private byte[] BuildStub()
        {
            uint data = sly4.addr.freeCameraData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint stub = sly4.addr.freeCameraStub;

            uint[] words =
            {
                0x3D600000 | high,               // lis r11, data@ha
                0x396B0000 | low,                // addi r11, r11, data@l
                0x818B0000,                      // lwz r12, 0(r11)
                0x2C0C0000,                      // cmpwi r12, 0
                0x41820014,                      // beq +0x14 (to the original instruction)
                0x818B0004,                      // lwz r12, 4(r11)
                0x7C036000,                      // cmpw r3, r12
                0x40820008,                      // bne +8
                0x388B0000 | MatrixOffset,       // addi r4, r11, matrix
                OriginalInstruction,             // lfs f1, 0(r4)
                Branch(stub + 10 * 4, sly4.addr.setCameraFunction + 4),
            };

            byte[] bytes = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(ToBytes(words[i]), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        /// <summary>A relative b instruction from one address to another.</summary>
        private static uint Branch(uint from, uint to)
        {
            return 0x48000000 | ((to - from) & 0x3FFFFFC);
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
