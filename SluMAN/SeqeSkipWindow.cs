using System;
using System.Diagnostics;
using System.Threading;

namespace SluMAN
{
    /// <summary>
    /// The cutscene skip of Sly 2's PS4 release, for Sly 2 and 3 (research in
    /// .claude/cutscene-skip-research.md). A cutscene is a script of commands, and each frame the
    /// script's thread asks FIsSeqeComplete whether the command it's on is done. For a while after
    /// a press, this makes it answer yes for the commands that wait on dialogue and voice, so the
    /// script runs on until something else holds it. FIsSeqeComplete is a switch on the command
    /// through a jump table: the window points these commands' entries at the function's default
    /// case, which returns true. The table is data, so this works with every PPU decoder.
    /// </summary>
    public class SeqeSkipWindow
    {
        /// <summary>What to look for in a game's code.</summary>
        public class Code
        {
            public uint SearchStart;
            public uint SearchEnd;
            // FIsSeqeComplete's switch up to the table; 0 is an instruction that varies. The
            // second has to be the bgt to the default case.
            public uint[] Switch;
            public uint ReturnTrue; // The default case's first instruction.
            public uint FirstCommand; // The table's first entry is this command's.
            public uint[] Commands; // The commands to skip.
            public uint[] CaseCode; // The first instruction of each one's case.
        }

        private const int WindowMs = 500; // About the PS4 release's 30 checks a press.
        // While the game is paused the script doesn't run, so paused time doesn't count towards
        // the window (a combo with Start in it pauses the game on the same press). It's checked
        // this often, and the window closes after this long whatever the pause flag says.
        private const int PausePollMs = 50;
        private const int MaxHoldMs = 30000;
        private const uint ChunkSize = 1024; // A single read is limited to about 2 KB.

        private readonly IGame game;
        private readonly Code code;
        private readonly Func<bool> isPaused;
        private readonly object windowLock = new object();
        private readonly Stopwatch held = new Stopwatch(); // Since the window opened.
        private readonly Stopwatch running = new Stopwatch(); // Unpaused time since then.
        private Timer timer;
        private uint table;
        private uint returnTrueEntry;
        private uint[] entries;
        private bool open;

        /// <param name="isPaused">Whether the game is paused, or null if the game has no flag
        /// for it, in which case the window lasts WindowMs whatever happens.</param>
        public SeqeSkipWindow(IGame game, Code code, Func<bool> isPaused = null)
        {
            this.game = game;
            this.code = code;
            this.isPaused = isPaused;
        }

        /// <summary>
        /// Opens the window, or makes one that's open last longer. False when the table wasn't
        /// found.
        /// </summary>
        public bool Open()
        {
            lock (windowLock)
            {
                if (table == 0 && !FindTable())
                {
                    Console.WriteLine("Cutscene skip: FIsSeqeComplete's jump table wasn't found.");
                    return false;
                }
                if (timer == null)
                {
                    timer = new Timer(timer_Tick);
                }
                // Before the writes: a window that only partly opened still has to close.
                held.Restart();
                running.Restart();
                timer.Change(isPaused == null ? WindowMs : PausePollMs, Timeout.Infinite);
                if (!open)
                {
                    open = true;
                    foreach (uint command in code.Commands)
                    {
                        game.api.WriteMemory(game.pid, Entry(command), Bytes(returnTrueEntry));
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Puts the jump table back. It stays marked open if that fails, so the next press closes
        /// it again.
        /// </summary>
        public void Close()
        {
            lock (windowLock)
            {
                if (!open)
                {
                    return;
                }
                for (int i = 0; i < code.Commands.Length; i++)
                {
                    game.api.WriteMemory(game.pid, Entry(code.Commands[i]), Bytes(entries[i]));
                }
                open = false;
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                held.Stop();
                running.Stop();
            }
        }

        private void timer_Tick(object state)
        {
            try
            {
                if (KeepOpen())
                {
                    return;
                }
                Close();
            }
            catch (Exception ex)
            {
                // A game reset puts the table back by itself.
                Console.WriteLine($"Cutscene skip couldn't close: {ex.Message}");
            }
        }

        /// <summary>
        /// Counts the window's time only while the game runs, and sets the timer again if it has
        /// some left. True if it stays open.
        /// </summary>
        private bool KeepOpen()
        {
            if (isPaused == null)
            {
                return false;
            }
            lock (windowLock)
            {
                if (!open || held.ElapsedMilliseconds >= MaxHoldMs)
                {
                    return false;
                }
                bool paused;
                try
                {
                    paused = isPaused();
                }
                catch
                {
                    paused = false;
                }
                if (paused)
                {
                    running.Stop();
                }
                else
                {
                    running.Start();
                }
                if (running.ElapsedMilliseconds >= WindowMs)
                {
                    return false;
                }
                timer.Change(PausePollMs, Timeout.Infinite);
                return true;
            }
        }

        private uint Entry(uint command)
        {
            return table + (command - code.FirstCommand) * 4;
        }

        /// <summary>
        /// Finds the jump table by the code of its switch, and checks where its entries and
        /// default case lead. False, with nothing set, if anything isn't as expected.
        /// </summary>
        private bool FindTable()
        {
            uint size = code.SearchEnd - code.SearchStart;
            byte[] bytes = new byte[size];
            for (uint done = 0; done < size; done += ChunkSize)
            {
                byte[] chunk = game.api.ReadMemory(game.pid, code.SearchStart + done, Math.Min(ChunkSize, size - done));
                Array.Copy(chunk, 0, bytes, done, chunk.Length);
            }

            for (uint at = 0; at + code.Switch.Length * 4 <= size; at += 4)
            {
                if (IsSwitch(bytes, at) && ReadTable(code.SearchStart + at, Word(bytes, at + 4)))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSwitch(byte[] bytes, uint at)
        {
            for (uint i = 0; i < code.Switch.Length; i++)
            {
                if (code.Switch[i] != 0 && Word(bytes, at + i * 4) != code.Switch[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <param name="switchAddress">Where the switch's code starts.</param>
        /// <param name="branch">Its second instruction, the bgt to the default case.</param>
        private bool ReadTable(uint switchAddress, uint branch)
        {
            uint foundTable = switchAddress + (uint)code.Switch.Length * 4;
            uint returnTrue = switchAddress + 4 + (branch & 0xFFFC);
            if (branch >> 16 != 0x419D || ReadUInt(returnTrue) != code.ReturnTrue)
            {
                return false;
            }

            uint[] found = new uint[code.Commands.Length];
            for (int i = 0; i < found.Length; i++)
            {
                found[i] = ReadUInt(foundTable + (code.Commands[i] - code.FirstCommand) * 4);
                if (ReadUInt(foundTable + found[i]) != code.CaseCode[i])
                {
                    return false;
                }
            }

            table = foundTable;
            returnTrueEntry = returnTrue - foundTable;
            entries = found;
            return true;
        }

        private uint ReadUInt(uint address)
        {
            return Word(game.api.ReadMemory(game.pid, address, 4), 0);
        }

        private static uint Word(byte[] bytes, uint at)
        {
            return (uint)(bytes[at] << 24 | bytes[at + 1] << 16 | bytes[at + 2] << 8 | bytes[at + 3]);
        }

        private static byte[] Bytes(uint value)
        {
            return new byte[]
            {
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value,
            };
        }
    }
}
