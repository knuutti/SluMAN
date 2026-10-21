using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;

namespace SluMAN
{
    public interface IAutosplitterAvailable
    {
        IEnumerable<(uint addr, uint size)> AutosplitterAddresses { get; }
    }

    /// <summary>
    /// A game whose autosplitter also needs values SluMAN works out itself rather than reads from
    /// one address, such as Sly 4's current goal. They're written after the subscribed addresses.
    /// </summary>
    public interface IAutosplitterComputedValues
    {
        /// <summary>
        /// Starts keeping the values up to date. <c>write</c> takes an offset from the start of the
        /// computed values and the value's little-endian bytes, and may be called from any thread.
        /// </summary>
        void StartComputedValues(Action<int, byte[]> write);

        void StopComputedValues();
    }

    public class AutosplitterHelper
    {
        public static int mmfAddressBytes = 128;
        public static int mmfConfigBytes = 256;
        public static int mmfSize = mmfAddressBytes + mmfConfigBytes;

        MemoryMappedFile mmfFile;
        MemoryMappedViewStream mmfStream;
        BinaryWriter writer;

        List<int> subscriptionIDs = new List<int>();

        IGame currentGame = null;
        IAutosplitterComputedValues computedValues = null;

        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Set before starting to log every value the autosplitter shares. Closed when it stops.
        /// </summary>
        public AutosplitterLog Log { get; set; }

        public AutosplitterHelper()
        {
            mmfFile = MemoryMappedFile.CreateOrOpen("SluMAN-autosplitter", mmfSize);
            mmfStream = mmfFile.CreateViewStream();
            writer = new BinaryWriter(mmfStream);
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Must start autosplitter before stopping.");
            }

            IsRunning = false;
            if (Log != null)
            {
                Log.Dispose();
            }
            if (computedValues != null)
            {
                computedValues.StopComputedValues();
                computedValues = null;
            }
            // Under the lock, so a subscription callback can't write to the closed view.
            lock (writeLock)
            {
                mmfStream.Close();
                writer?.Close();
                writer = null;
            }

            if (currentGame == null) return;

            foreach (int subID in subscriptionIDs)
            {
                this.currentGame.api.ReleaseSubID(subID);
            }
        }

        private readonly object writeLock = new object();
        private void WriteToMemory(int offset, byte[] value)
        {
            lock (writeLock)
            {
                if (writer != null)
                {
                    writer.Seek(offset, SeekOrigin.Begin);
                    writer.Write(value, 0, value.Length);
                }
            }
        }

        public void StartAutosplitterForGame(IGame game)
        {
            IAutosplitterAvailable autosplitter = game as IAutosplitterAvailable;
            if (autosplitter == null) throw new NotSupportedException("This game doesn't support an autosplitter yet.");
            currentGame = game;

            // The game form keeps game.pid current across reconnects; asking the API would cost a
            // round trip to the PS3 for every address.
            int pid = game.pid;
            int pos = 0;
            int index = 0;
            AutosplitterLog log = Log;

            foreach (var (addr, size) in autosplitter.AutosplitterAddresses)
            {
                int offset = pos;
                int field = index;
                uint address = addr;
                if (log != null)
                {
                    log.Field(field, offset, address, size);
                }

                // Write the initial value to the memory. This is necessary because the autosplitter will only
                // trigger when the value changes. So at the start of the game all values will be 0;
                var initialValue = game.api.ReadMemory(pid, addr, size).Reverse().ToArray();
                WriteToMemory(offset, initialValue);
                if (log != null)
                {
                    log.Value(field, address, initialValue, true);
                }

                subscriptionIDs.Add(game.api.SubMemory(pid, addr, size, (value) =>
                {
                    WriteToMemory(offset, value);
                    if (log != null)
                    {
                        log.Value(field, address, value, false);
                    }
                }));
                pos += (int)size;
                index++;
            }

            if (game is IAutosplitterComputedValues computed)
            {
                int start = pos;
                computed.StartComputedValues((offset, value) => WriteToMemory(start + offset, value));
                computedValues = computed;
            }

            IsRunning = true;
        }
    }
}
