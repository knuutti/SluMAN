using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// Finds which code sets the main view's camera, to build the free camera on. Every call to
    /// the two functions that set a view's camera is pointed, for a moment, at a stub that logs
    /// the call (the address it returns to and the view) and goes on into the function. Calls are
    /// patched rather than the functions' first instructions, like the code hook, since patched
    /// calls are known to run. The stubs only use r5 to r8, which both functions set before
    /// reading, and put the condition register back.
    ///
    /// Data layout: +0x0 the log's last entry (a byte offset), then LogEntries entries of the
    /// return address and the view.
    /// </summary>
    public class Sly4CameraLog
    {
        public const int LogEntries = 64;

        private const uint LogOffset = 0x10;

        private readonly IGame game;

        public Sly4CameraLog(IGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Logs the calls for a while, puts the calls back, and returns the log: each entry's
        /// return address and view, oldest first. Empty when none of the calls ran.
        /// </summary>
        public List<uint[]> Capture(int milliseconds)
        {
            uint[] functions = sly4.addr.cameraFunctions;
            uint[] stubs = new uint[functions.Length];
            for (int i = 0; i < functions.Length; i++)
            {
                stubs[i] = sly4.addr.cameraLogStubs + (uint)i * 0x40;
            }

            List<uint> patched = new List<uint>();
            try
            {
                game.api.WriteMemory(game.pid, sly4.addr.cameraLogData, new byte[LogOffset + LogEntries * 8]);
                for (int i = 0; i < functions.Length; i++)
                {
                    byte[] stub = BuildStub(stubs[i], functions[i]);
                    game.api.WriteMemory(game.pid, stubs[i], stub);
                    if (!game.api.ReadMemory(game.pid, stubs[i], (uint)stub.Length).SequenceEqual(stub))
                    {
                        throw new InvalidOperationException("Couldn't write the logging code into the game's memory.");
                    }
                }

                foreach (uint call in sly4.addr.cameraFunctionCalls)
                {
                    int target = Array.IndexOf(functions, CallTarget(call));
                    if (target < 0)
                    {
                        continue;
                    }
                    game.api.WriteMemory(game.pid, call, ToBytes(Branch(call, stubs[target], true)));
                    patched.Add(call);
                }
                if (patched.Count == 0)
                {
                    throw new InvalidOperationException("The game's camera code isn't what SluMAN expects.");
                }

                Thread.Sleep(milliseconds);
            }
            finally
            {
                for (int i = 0; i < functions.Length; i++)
                {
                    foreach (uint call in patched)
                    {
                        if (ReadUInt(call) == Branch(call, stubs[i], true))
                        {
                            game.api.WriteMemory(game.pid, call, ToBytes(Branch(call, functions[i], true)));
                        }
                    }
                }
            }

            return ReadLog();
        }

        /// <summary>Where a bl instruction in the game goes, or 0 if it isn't one.</summary>
        private uint CallTarget(uint call)
        {
            uint instruction = ReadUInt(call);
            if ((instruction & 0xFC000003) != 0x48000001)
            {
                return 0;
            }
            int offset = (int)(instruction & 0x3FFFFFC);
            if ((offset & 0x2000000) != 0)
            {
                offset -= 0x4000000;
            }
            return (uint)(call + offset);
        }

        private List<uint[]> ReadLog()
        {
            byte[] bytes = game.api.ReadMemory(game.pid, sly4.addr.cameraLogData, LogOffset + LogEntries * 8);
            uint last = ToUInt(bytes, 0) / 8;
            List<uint[]> log = new List<uint[]>();
            for (int i = 1; i <= LogEntries; i++)
            {
                int entry = (int)((last + i) % LogEntries);
                uint caller = ToUInt(bytes, (int)LogOffset + entry * 8);
                uint view = ToUInt(bytes, (int)LogOffset + entry * 8 + 4);
                if (caller != 0)
                {
                    log.Add(new uint[] { caller, view });
                }
            }
            return log;
        }

        /// <summary>
        /// The stub's PowerPC code: log the return address and r3, then branch into the function
        /// without linking, so it returns to the caller.
        /// </summary>
        private static byte[] BuildStub(uint stub, uint function)
        {
            uint data = sly4.addr.cameraLogData;
            uint high = ((data + 0x8000) >> 16) & 0xFFFF;
            uint low = data & 0xFFFF;
            uint mask = (uint)(LogEntries - 1) * 8;

            uint[] words =
            {
                0x7D000026,                      //  0 mfcr r8
                0x3CA00000 | high,               //  1 lis r5, data@ha
                0x38A50000 | low,                //  2 addi r5, r5, data@l
                0x7CC802A6,                      //  3 mflr r6
                0x80E50000,                      //  4 lwz r7, 0(r5)
                0x38E70008,                      //  5 addi r7, r7, 8
                0x70E70000 | mask,               //  6 andi. r7, r7, mask
                0x90E50000,                      //  7 stw r7, 0(r5)
                0x7CE72A14,                      //  8 add r7, r7, r5
                0x90C70000 | LogOffset,          //  9 stw r6, log(r7)
                0x90670000 | (LogOffset + 4),    // 10 stw r3, log + 4(r7)
                0x7D0FF120,                      // 11 mtcrf 0xFF, r8
                Branch(stub + 12 * 4, function, false),
            };

            byte[] bytes = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(ToBytes(words[i]), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        private static uint Branch(uint from, uint to, bool link)
        {
            return 0x48000000 | ((to - from) & 0x3FFFFFC) | (link ? 1u : 0u);
        }

        private uint ReadUInt(uint address)
        {
            return ToUInt(game.api.ReadMemory(game.pid, address, 4), 0);
        }

        private static uint ToUInt(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]);
        }

        private static byte[] ToBytes(uint value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
    }
}
