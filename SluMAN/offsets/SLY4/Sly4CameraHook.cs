using System;
using System.Linq;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN set the gameplay camera. SetCamera (view, matrix) copies a camera's matrix into
    /// a view and builds the view from it; its first instruction is pointed at a stub that logs
    /// every call (who called, for which view) and, while the free camera is on, passes the matrix
    /// kept in the data area instead for the main view, or for one caller when a filter is set.
    /// The stub leaves every register SetCamera's callers can rely on as it was, the condition
    /// register included.
    ///
    /// Data layout: +0x0 on (non-zero), +0x4 the main view's address, +0x8 the log's last entry
    /// (a byte offset), +0xC only replace calls from this caller (0: calls for the main view),
    /// +0x10 the matrix (4 rows of 4 big-endian floats: the camera's axes down the first three
    /// columns, its position in the last row; 16-aligned for the game's vector loads), +0x50 the
    /// log: LogEntries entries of the caller's return address and the view.
    ///
    /// Like the code hook, RPCS3 only runs the patched code with the Interpreter PPU decoder.
    /// </summary>
    public class Sly4CameraHook
    {
        public const int MatrixSize = 0x40;

        private const uint ViewOffset = 0x4;
        private const uint LogIndexOffset = 0x8;
        private const uint CallerFilterOffset = 0xC;
        private const uint MatrixOffset = 0x10;
        private const uint LogOffset = 0x50;
        public const int LogEntries = 32;

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
        /// free camera (pass null to skip), then removes the hook: with the hook left in place
        /// (switched off) the game camera stayed broken until SluMAN closed, and closing removes
        /// it. The log is kept until the next Enable.
        /// </summary>
        public void Disable(byte[] gameMatrix)
        {
            if (gameMatrix != null)
            {
                SetMatrix(gameMatrix);
                Thread.Sleep(RestoreMs);
            }
            Uninstall();
        }

        /// <summary>
        /// The log of the last SetCamera calls: each one's caller (the address after its call)
        /// and the view it set. Empty entries are left out.
        /// </summary>
        public System.Collections.Generic.List<uint[]> ReadLog()
        {
            byte[] bytes = game.api.ReadMemory(game.pid, sly4.addr.freeCameraData + LogOffset, LogEntries * 8);
            System.Collections.Generic.List<uint[]> log = new System.Collections.Generic.List<uint[]>();
            for (int i = 0; i < LogEntries; i++)
            {
                uint caller = BitConverter.ToUInt32(bytes.Skip(i * 8).Take(4).Reverse().ToArray(), 0);
                uint view = BitConverter.ToUInt32(bytes.Skip(i * 8 + 4).Take(4).Reverse().ToArray(), 0);
                if (caller != 0)
                {
                    log.Add(new uint[] { caller, view });
                }
            }
            return log;
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
            game.api.WriteMemory(game.pid, sly4.addr.freeCameraData, new byte[LogOffset + LogEntries * 8]);
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
        /// caller. It only uses r5 to r8, which SetCamera sets before reading (it leaves r0 and r9
        /// to r12 alone, so its callers may keep values there across the call), and puts the
        /// condition register back before going on, since SetCamera doesn't touch that either.
        /// </summary>
        private byte[] BuildStub()
        {
            uint data = sly4.addr.freeCameraData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint stub = sly4.addr.freeCameraStub;
            uint logMask = (uint)(LogEntries - 1) * 8;

            uint[] words =
            {
                0x7D000026,                      //  0 mfcr r8
                0x3CA00000 | high,               //  1 lis r5, data@ha
                0x38A50000 | low,                //  2 addi r5, r5, data@l
                0x7CC802A6,                      //  3 mflr r6
                0x80E50000 | LogIndexOffset,     //  4 lwz r7, index(r5)
                0x38E70008,                      //  5 addi r7, r7, 8
                0x70E70000 | logMask,            //  6 andi. r7, r7, mask
                0x90E50000 | LogIndexOffset,     //  7 stw r7, index(r5)
                0x7CE72A14,                      //  8 add r7, r7, r5
                0x90C70000 | LogOffset,          //  9 stw r6, log(r7)
                0x90670000 | (LogOffset + 4),    // 10 stw r3, log + 4(r7)
                0x80E50000,                      // 11 lwz r7, 0(r5)
                0x2C070000,                      // 12 cmpwi r7, 0
                0x41820028,                      // 13 beq +0x28 (to 23: off)
                0x80E50000 | CallerFilterOffset, // 14 lwz r7, filter(r5)
                0x2C070000,                      // 15 cmpwi r7, 0
                0x4182000C,                      // 16 beq +0xC (to 19: no filter)
                0x7C063800,                      // 17 cmpw r6, r7
                0x4800000C,                      // 18 b +0xC (to 21)
                0x80E50000 | ViewOffset,         // 19 lwz r7, view(r5)
                0x7C033800,                      // 20 cmpw r3, r7
                0x40820008,                      // 21 bne +8 (to 23)
                0x38850000 | MatrixOffset,       // 22 addi r4, r5, matrix
                0x7D0FF120,                      // 23 mtcrf 0xFF, r8
                OriginalInstruction,             // 24 lfs f1, 0(r4)
                Branch(stub + 25 * 4, sly4.addr.setCameraFunction + 4, false),
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
