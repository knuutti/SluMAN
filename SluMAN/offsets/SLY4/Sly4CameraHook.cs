using System;
using System.Linq;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN set the gameplay camera. The camera update hands the main view its matrix with
    /// SetCamera (view, matrix), from one call per camera mode. Those calls are pointed at a stub
    /// that, while the free camera is on, passes the matrix kept in the data area instead, then
    /// carries on into SetCamera. Menus, load screens and every other view call SetCamera from
    /// elsewhere, so they keep the game's cameras; turning the free camera off hands the view back
    /// to the camera update on its next frame.
    ///
    /// Data layout: +0x0 on (non-zero), +0x10 the matrix (X, Y and Z axes, then the position, 4
    /// big-endian floats each; 16-aligned for the game's vector loads).
    ///
    /// Like the code hook, RPCS3 only runs the patched code with the Interpreter PPU decoder.
    /// </summary>
    public class Sly4CameraHook
    {
        public const int MatrixSize = 0x40;

        private const uint MatrixOffset = 0x10;

        private readonly IGame game;

        public Sly4CameraHook(IGame game)
        {
            this.game = game;
        }

        /// <summary>Uses the matrix from now on (written before the switch is turned on).</summary>
        public void Enable(byte[] matrix)
        {
            Install();
            SetMatrix(matrix);
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

        /// <summary>Points the calls back at SetCamera. Call before disconnecting.</summary>
        public void Uninstall()
        {
            foreach (uint call in sly4.addr.gameplayCameraCalls)
            {
                if (ReadUInt(call) == HookCall(call))
                {
                    game.api.WriteMemory(game.pid, call, ToBytes(OriginalCall(call)));
                }
            }
            try { Disable(); } catch { }
        }

        private void Install()
        {
            byte[] stub = BuildStub();
            bool hooked = sly4.addr.gameplayCameraCalls.All(call => ReadUInt(call) == HookCall(call));
            if (hooked && game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length).SequenceEqual(stub))
            {
                return;
            }

            foreach (uint call in sly4.addr.gameplayCameraCalls)
            {
                uint instruction = ReadUInt(call);
                if (instruction != OriginalCall(call) && instruction != HookCall(call))
                {
                    throw new InvalidOperationException("The game's camera code isn't what SluMAN expects, so the free camera isn't available on this version.");
                }
            }

            // Unhook while the stub is (re)written, so the game never runs it half-written. This
            // also undoes the earlier version of this hook, which patched SetCamera itself.
            Uninstall();
            const uint setCameraFirstInstruction = 0xC0240000; // lfs f1, 0(r4)
            if (ReadUInt(sly4.addr.setCameraFunction) == Branch(sly4.addr.setCameraFunction, sly4.addr.freeCameraStub, false))
            {
                game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(setCameraFirstInstruction));
            }

            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraStub, stub);
            if (!game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length).SequenceEqual(stub))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            foreach (uint call in sly4.addr.gameplayCameraCalls)
            {
                game.api.WriteMemory(game.pid, call, ToBytes(HookCall(call)));
            }
        }

        private static uint OriginalCall(uint call)
        {
            return Branch(call, sly4.addr.setCameraFunction, true);
        }

        private static uint HookCall(uint call)
        {
            return Branch(call, sly4.addr.freeCameraStub, true);
        }

        /// <summary>
        /// The stub's PowerPC code: r3 is the view and r4 the matrix, and r11 and r12 are free to
        /// use. It branches into SetCamera without linking, so SetCamera returns to the caller.
        /// </summary>
        private static byte[] BuildStub()
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
                0x41820008,                      // beq +8
                0x388B0000 | MatrixOffset,       // addi r4, r11, matrix
                Branch(stub + 6 * 4, sly4.addr.setCameraFunction, false),
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
