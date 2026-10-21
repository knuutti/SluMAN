using System;
using System.Collections.Generic;
using System.Linq;

namespace SluMAN
{
    /// <summary>
    /// Lets SluMAN call Sly 4's own functions without an SPRX. The main loop's per-frame call to
    /// the game update is redirected to a small stub written into unused code memory. The stub
    /// runs the call queued in a mailbox in unused data memory, clears it, then continues into the
    /// update with its registers as they were, so game functions run on the main thread just
    /// before the update, where the game makes the same calls itself.
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

        // Cleared on install. Includes +0x2C to +0x37, where an earlier version kept a call to
        // repeat every frame, so a stub of that version left in a running game stops calling it.
        private const uint MailboxSize = 0x38;

        // The stub's frame: the 9th argument at 0x70 (the parameter area), r3 to r10 saved from
        // 0x80, and the return address in the caller's frame, at FrameSize + 0x10.
        private const uint FrameSize = 0xC0;
        private const uint SavedRegisters = 0x80;

        // The first instructions (stack frame setup) of each stub version, to recognise an older one.
        private static readonly byte[][] StubPrologues =
        {
            // SluMAN 1.1.1 and earlier: a 0x80 frame, hooked on the render thread.
            new byte[] { 0xF8, 0x21, 0xFF, 0x81, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0x90 },
            // Now: a 0xC0 frame that keeps r3 to r10.
            new byte[] { 0xF8, 0x21, 0xFF, 0x41, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0xD0 },
        };

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

        /// <summary>What the last call returned (r3), once IsCallPending is false.</summary>
        public uint ReturnValue()
        {
            return ReadUInt(sly4.addr.hookMailbox + 4);
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
            // An older SluMAN hooked another call, whose stub this one replaces: unhook it first,
            // so it never runs this stub, which continues into a different function.
            if (ReadUInt(sly4.addr.legacyHookCallSite) == Branch(sly4.addr.legacyHookCallSite, sly4.addr.hookCodeCave, true))
            {
                game.api.WriteMemory(game.pid, sly4.addr.legacyHookCallSite,
                    ToBytes(Branch(sly4.addr.legacyHookCallSite, sly4.addr.legacyHookCallTarget, true)));
            }

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

            bool caveHasOldStub = StubPrologues.Any(prologue => cave.Take(prologue.Length).SequenceEqual(prologue));
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
        /// The stub's PowerPC code. Game functions share the module's TOC, so r2 needs no change.
        /// The update takes arguments (r3 at least), so r3 to r10 are saved first and put back
        /// before continuing into it; r0, r11 and r12 are free at a call.
        /// </summary>
        private byte[] BuildStub()
        {
            uint mailbox = sly4.addr.hookMailbox;
            uint high = ((mailbox + 0x8000) >> 16) & 0xFFFF;
            uint low = mailbox & 0xFFFF;

            List<uint> words = new List<uint>();
            words.Add(0xF8210001 | (0x10000 - FrameSize));     // stdu r1, -FrameSize(r1)
            words.Add(0x7C0802A6);                             // mflr r0
            words.Add(0xF8010000 | (FrameSize + 0x10));        // std r0, FrameSize + 0x10(r1)
            for (uint reg = 3; reg <= 10; reg++)
            {
                words.Add(Ds(62, reg, SavedRegisters + (reg - 3) * 8));  // std rN, saved(r1)
            }
            words.Add(0x3D600000 | high);                      // lis r11, mailbox@ha
            words.Add(0x396B0000 | low);                       // addi r11, r11, mailbox@l
            words.Add(0x818B0000);                             // lwz r12, 0(r11)
            words.Add(0x2C0C0000);                             // cmpwi r12, 0
            int skip = words.Count;
            words.Add(0);                                      // beq epilogue, set below
            for (uint reg = 3; reg <= 10; reg++)
            {
                // lwz rN, 8 + (N - 3) * 4(r11)
                words.Add((32u << 26) | (reg << 21) | (11u << 16) | (8 + (reg - 3) * 4));
            }
            // The 9th argument goes in the caller's parameter area, at 0x70 in this frame.
            words.Add(0x800B0028);                             // lwz r0, 0x28(r11)
            words.Add(0xF8010070);                             // std r0, 0x70(r1)
            words.Add(0x7D8903A6);                             // mtctr r12
            words.Add(0x4E800421);                             // bctrl
            words.Add(0x3D600000 | high);                      // lis r11, mailbox@ha
            words.Add(0x396B0000 | low);                       // addi r11, r11, mailbox@l
            words.Add(0x906B0004);                             // stw r3, 4(r11)
            words.Add(0x38000000);                             // li r0, 0
            words.Add(0x900B0000);                             // stw r0, 0(r11)
            words[skip] = 0x41820000 | (uint)((words.Count - skip) * 4);  // beq epilogue
            for (uint reg = 3; reg <= 10; reg++)
            {
                words.Add(Ds(58, reg, SavedRegisters + (reg - 3) * 8));  // ld rN, saved(r1)
            }
            words.Add(0xE8010000 | (FrameSize + 0x10));        // ld r0, FrameSize + 0x10(r1)
            words.Add(0x7C0803A6);                             // mtlr r0
            words.Add(0x38210000 | FrameSize);                 // addi r1, r1, FrameSize
            words.Add(Branch(sly4.addr.hookCodeCave + (uint)words.Count * 4, sly4.addr.hookCallTarget, false));

            byte[] stub = new byte[words.Count * 4];
            for (int i = 0; i < words.Count; i++)
            {
                Array.Copy(ToBytes(words[i]), 0, stub, i * 4, 4);
            }
            return stub;
        }

        /// <summary>A std (opcode 62) or ld (58) of a register at an offset from r1.</summary>
        private static uint Ds(uint opcode, uint reg, uint offset)
        {
            return (opcode << 26) | (reg << 21) | (1u << 16) | (offset & 0xFFFC);
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
