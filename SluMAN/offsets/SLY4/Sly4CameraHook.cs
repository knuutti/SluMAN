using System;
using System.Linq;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN set the gameplay camera. Each frame the main render hands the main view the game
    /// camera's matrix with SetMainViewCamera (view, matrix), its only caller. That function's
    /// first instruction is pointed at a stub that notes the view and the game camera's matrix and,
    /// while the free camera is on, passes the matrix kept in the data area instead. The stub
    /// leaves every register the caller can rely on as it was, the condition register included.
    ///
    /// Data layout: +0x0 on (non-zero), +0x4 the view and +0x8 the game camera's matrix (both
    /// written by the stub), +0x10 the matrix (the camera's X, Y and Z axes, then its position, as
    /// 4 big-endian floats each; 16-aligned for the game's vector loads).
    ///
    /// Like the code hook, RPCS3 only runs the patched code with the Interpreter PPU decoder.
    /// </summary>
    public class Sly4CameraHook
    {
        public const int MatrixSize = 0x40;

        private const uint GameMatrixOffset = 0x8;
        private const uint MatrixOffset = 0x10;

        // The first instruction of both camera functions: lfs f1, 0(r4).
        private const uint OriginalInstruction = 0xC0240000;

        // How long to wait for the game to run the stub once, to learn the game camera's matrix.
        private const int FirstFrameTimeoutMs = 1000;

        private readonly IGame game;

        public Sly4CameraHook(IGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Installs the hook (switched off) and waits for a frame, then returns the address of the
        /// game camera's matrix, which the game keeps up to date.
        /// </summary>
        public uint Prepare()
        {
            Install();
            DateTime deadline = DateTime.Now.AddMilliseconds(FirstFrameTimeoutMs);
            while (DateTime.Now < deadline)
            {
                uint matrix = ReadUInt(sly4.addr.freeCameraData + GameMatrixOffset);
                if (matrix != 0)
                {
                    return matrix;
                }
                Thread.Sleep(20);
            }
            Uninstall();
            throw new InvalidOperationException("The game didn't run the camera code. On RPCS3, set the PPU decoder to Interpreter (static).");
        }

        /// <summary>Uses the matrix from now on (written before the switch). Call Prepare first.</summary>
        public void Enable(byte[] matrix)
        {
            SetMatrix(matrix);
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(1));
        }

        public void SetMatrix(byte[] matrix)
        {
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData + MatrixOffset, matrix);
        }

        /// <summary>Gives the view back to the game's camera and removes the hook.</summary>
        public void Disable()
        {
            Uninstall();
        }

        /// <summary>Puts the camera code back as it was. Call before disconnecting.</summary>
        public void Uninstall()
        {
            uint hookBranch = Branch(sly4.addr.mainViewCameraFunction, sly4.addr.freeCameraStub, false);
            if (ReadUInt(sly4.addr.mainViewCameraFunction) == hookBranch)
            {
                game.api.WriteMemory(game.pid, sly4.addr.mainViewCameraFunction, ToBytes(OriginalInstruction));
            }
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, ToBytes(0));

            // Earlier versions hooked SetCamera, or the calls to it, instead.
            if (ReadUInt(sly4.addr.setCameraFunction) == Branch(sly4.addr.setCameraFunction, sly4.addr.freeCameraStub, false))
            {
                game.api.WriteMemory(game.pid, sly4.addr.setCameraFunction, ToBytes(OriginalInstruction));
            }
            foreach (uint call in sly4.addr.gameplayCameraCalls)
            {
                if (ReadUInt(call) == Branch(call, sly4.addr.freeCameraStub, true))
                {
                    game.api.WriteMemory(game.pid, call, ToBytes(Branch(call, sly4.addr.setCameraFunction, true)));
                }
            }
        }

        private void Install()
        {
            uint first = ReadUInt(sly4.addr.mainViewCameraFunction);
            if (first != OriginalInstruction && first != Branch(sly4.addr.mainViewCameraFunction, sly4.addr.freeCameraStub, false))
            {
                throw new InvalidOperationException("The game's camera code isn't what SluMAN expects, so the free camera isn't available on this version.");
            }

            // Always from scratch, so the noted matrix is from this install.
            Uninstall();
            byte[] stub = BuildStub();
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, new byte[MatrixOffset]);
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraStub, stub);
            if (!game.api.ReadMemory(game.pid, sly4.addr.freeCameraStub, (uint)stub.Length).SequenceEqual(stub))
            {
                throw new InvalidOperationException("Couldn't write the camera code into the game's memory.");
            }
            // Last, so the game never jumps to a half-written stub.
            game.api.WriteMemory(game.pid, sly4.addr.mainViewCameraFunction, ToBytes(Branch(sly4.addr.mainViewCameraFunction, sly4.addr.freeCameraStub, false)));
        }

        /// <summary>
        /// The stub's PowerPC code: r3 is the view and r4 the matrix. It only uses r5 to r8, which
        /// the camera function sets before reading (it leaves r0 and r9 to r12 alone, so its caller
        /// may keep values there), and puts the condition register back, which it doesn't touch
        /// either.
        /// </summary>
        private static byte[] BuildStub()
        {
            uint data = sly4.addr.freeCameraData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint stub = sly4.addr.freeCameraStub;

            uint[] words =
            {
                0x7D000026,                      //  0 mfcr r8
                0x3CA00000 | high,               //  1 lis r5, data@ha
                0x38A50000 | low,                //  2 addi r5, r5, data@l
                0x90650004,                      //  3 stw r3, 4(r5)
                0x90850000 | GameMatrixOffset,   //  4 stw r4, game matrix(r5)
                0x80E50000,                      //  5 lwz r7, 0(r5)
                0x2C070000,                      //  6 cmpwi r7, 0
                0x41820008,                      //  7 beq +8 (to 9: off)
                0x38850000 | MatrixOffset,       //  8 addi r4, r5, matrix
                0x7D0FF120,                      //  9 mtcrf 0xFF, r8
                OriginalInstruction,             // 10 lfs f1, 0(r4)
                Branch(stub + 11 * 4, sly4.addr.mainViewCameraFunction + 4, false),
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
