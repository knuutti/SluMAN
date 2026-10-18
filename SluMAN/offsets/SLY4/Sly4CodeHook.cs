using System;
using System.Linq;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN call Sly 4's own functions without an SPRX. A call the game makes once per
    /// frame is redirected to a small stub written into unused code memory. The stub runs the call
    /// queued in a mailbox in unused data memory, clears it, then continues into the original
    /// function, so game functions run on the game's own thread like the debug menu's do.
    ///
    /// Mailbox layout: +0x0 function address (0 = nothing queued), +0x4 return value,
    /// +0x8..+0x24 arguments r3 to r10, +0x28 the 9th argument, which goes on the stack.
    ///
    /// RPCS3 only runs the patched code with the Interpreter PPU decoder; the recompilers keep
    /// running the code as it was compiled, so queued calls never run there.
    /// </summary>
    public class Sly4CodeHook
    {
        public const int MaxArguments = 9;

        private const uint MailboxSize = 0x2C;

        // The stub's first instructions (stack frame setup), to recognise an older version of it.
        private static readonly byte[] StubPrologue = { 0xF8, 0x21, 0xFF, 0x81, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0x90 };

        private readonly IGame game;

        public Sly4CodeHook(IGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Queues a call to a game function with up to nine 32-bit arguments. It runs on the
        /// game's next frame; IsCallPending turns false once it has.
        /// </summary>
        public void Call(uint function, params uint[] arguments)
        {
            if (arguments.Length > MaxArguments)
            {
                throw new ArgumentException($"A call takes at most {MaxArguments} arguments.");
            }

            Install();
            if (IsCallPending())
            {
                throw new InvalidOperationException("The previous call hasn't run yet.");
            }

            byte[] args = new byte[MaxArguments * 4];
            for (int i = 0; i < arguments.Length; i++)
            {
                Array.Copy(ToBytes(arguments[i]), 0, args, i * 4, 4);
            }

            // The arguments first: the stub starts as soon as it sees the function address.
            game.api.WriteMemory(game.pid, sly4.addr.hookMailbox + 8, args);
            game.api.WriteMemory(game.pid, sly4.addr.hookMailbox, ToBytes(function));
        }

        public bool IsCallPending()
        {
            return ReadUInt(sly4.addr.hookMailbox) != 0;
        }

        /// <summary>Drops a queued call that hasn't run, so it can't run later by surprise.</summary>
        public void CancelCall()
        {
            game.api.WriteMemory(game.pid, sly4.addr.hookMailbox, ToBytes(0));
        }

        /// <summary>Points the per-frame call back at the original function. The stub stays.</summary>
        public void Uninstall()
        {
            if (ReadUInt(sly4.addr.hookCallSite) == HookCall())
            {
                game.api.WriteMemory(game.pid, sly4.addr.hookCallSite, ToBytes(OriginalCall()));
                // A call queued now would otherwise run the next time the hook is installed.
                CancelCall();
            }
        }

        /// <summary>
        /// Writes the stub and redirects the per-frame call, unless that's done already. Refuses
        /// when the memory isn't what this game version should have, so a different build or
        /// another mod isn't overwritten.
        /// </summary>
        private void Install()
        {
            uint callSite = ReadUInt(sly4.addr.hookCallSite);
            byte[] stub = BuildStub();
            byte[] cave = game.api.ReadMemory(game.pid, sly4.addr.hookCodeCave, (uint)stub.Length);
            bool caveHasStub = cave.SequenceEqual(stub);

            if (callSite == HookCall() && caveHasStub)
            {
                return;
            }
            if (callSite != OriginalCall() && callSite != HookCall())
            {
                throw new InvalidOperationException("The game's code isn't what SluMAN expects, so warps aren't available on this version.");
            }

            bool caveHasOldStub = cave.Take(StubPrologue.Length).SequenceEqual(StubPrologue);
            if (!caveHasStub && !caveHasOldStub && cave.Any(b => b != 0))
            {
                throw new InvalidOperationException("The memory for the warp code is already in use.");
            }

            if (callSite == HookCall() && !caveHasStub)
            {
                // Unhook while the stub is replaced, so the game never runs it half-written.
                game.api.WriteMemory(game.pid, sly4.addr.hookCallSite, ToBytes(OriginalCall()));
            }
            game.api.WriteMemory(game.pid, sly4.addr.hookMailbox, new byte[MailboxSize]);
            if (!caveHasStub)
            {
                game.api.WriteMemory(game.pid, sly4.addr.hookCodeCave, stub);
                byte[] written = game.api.ReadMemory(game.pid, sly4.addr.hookCodeCave, (uint)stub.Length);
                if (!written.SequenceEqual(stub))
                {
                    throw new InvalidOperationException("Couldn't write the warp code into the game's memory.");
                }
            }
            // Last, so the game never jumps to a half-written stub.
            game.api.WriteMemory(game.pid, sly4.addr.hookCallSite, ToBytes(HookCall()));
            if (ReadUInt(sly4.addr.hookCallSite) != HookCall())
            {
                throw new InvalidOperationException("Couldn't hook the game's code.");
            }
        }

        private uint OriginalCall()
        {
            return Branch(sly4.addr.hookCallSite, sly4.addr.hookCallTarget, true);
        }

        private uint HookCall()
        {
            return Branch(sly4.addr.hookCallSite, sly4.addr.hookCodeCave, true);
        }

        /// <summary>
        /// The stub's PowerPC code. Game functions share the module's TOC, so r2 needs no change,
        /// and the hooked function takes no arguments, so r3 to r10 are free to use.
        /// </summary>
        private byte[] BuildStub()
        {
            uint mailbox = sly4.addr.hookMailbox;
            uint high = ((mailbox + 0x8000) >> 16) & 0xFFFF;
            uint low = mailbox & 0xFFFF;

            uint[] words = new uint[29];
            words[0] = 0xF821FF81;           // stdu r1, -0x80(r1)
            words[1] = 0x7C0802A6;           // mflr r0
            words[2] = 0xF8010090;           // std r0, 0x90(r1)
            words[3] = 0x3D600000 | high;    // lis r11, mailbox@ha
            words[4] = 0x396B0000 | low;     // addi r11, r11, mailbox@l
            words[5] = 0x818B0000;           // lwz r12, 0(r11)
            words[6] = 0x2C0C0000;           // cmpwi r12, 0
            words[7] = 0x41820048;           // beq +0x48 (to the epilogue)
            for (uint reg = 3; reg <= 10; reg++)
            {
                // lwz rN, 8 + (N - 3) * 4(r11)
                words[5 + reg] = (32u << 26) | (reg << 21) | (11u << 16) | (8 + (reg - 3) * 4);
            }
            // The 9th argument goes in the caller's parameter area, at 0x70 in this frame.
            words[16] = 0x800B0028;          // lwz r0, 0x28(r11)
            words[17] = 0xF8010070;          // std r0, 0x70(r1)
            words[18] = 0x7D8903A6;          // mtctr r12
            words[19] = 0x4E800421;          // bctrl
            words[20] = 0x3D600000 | high;   // lis r11, mailbox@ha
            words[21] = 0x396B0000 | low;    // addi r11, r11, mailbox@l
            words[22] = 0x906B0004;          // stw r3, 4(r11)
            words[23] = 0x38000000;          // li r0, 0
            words[24] = 0x900B0000;          // stw r0, 0(r11)
            words[25] = 0xE8010090;          // ld r0, 0x90(r1)
            words[26] = 0x7C0803A6;          // mtlr r0
            words[27] = 0x38210080;          // addi r1, r1, 0x80
            words[28] = Branch(sly4.addr.hookCodeCave + 28 * 4, sly4.addr.hookCallTarget, false);

            byte[] stub = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                Array.Copy(ToBytes(words[i]), 0, stub, i * 4, 4);
            }
            return stub;
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
