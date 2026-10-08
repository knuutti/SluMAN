using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;

namespace racman
{
    public interface IAutosplitterAvailable
    {
        IEnumerable<(uint addr, uint size)> AutosplitterAddresses { get; }
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

        public bool IsRunning { get; private set; } = false;

        public AutosplitterHelper()
        {
            mmfFile = MemoryMappedFile.CreateOrOpen("racman-autosplitter", mmfSize);
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

            foreach (var (addr, size) in autosplitter.AutosplitterAddresses)
            {
                int offset = pos;

                // Write the initial value to the memory. This is necessary because the autosplitter will only
                // trigger when the value changes. So at the start of the game all values will be 0;
                var initialValue = game.api.ReadMemory(pid, addr, size).Reverse().ToArray();
                WriteToMemory(offset, initialValue);

                subscriptionIDs.Add(game.api.SubMemory(pid, addr, size, (value) =>
                {
                    WriteToMemory(offset, value);
                }));
                pos += (int)size;
            }

            IsRunning = true;
        }
    }
}
