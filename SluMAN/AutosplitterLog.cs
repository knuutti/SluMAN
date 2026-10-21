using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SluMAN
{
    /// <summary>
    /// Writes every value the autosplitter shares with LiveSplit to a text file, with the time it
    /// arrived, for working out why a split was missed. One file per autosplitter start, in the
    /// user data folder's autosplitter_logs. Values arrive on the API's threads.
    /// </summary>
    public class AutosplitterLog : IDisposable
    {
        public const string FolderName = "autosplitter_logs";

        private readonly object writeLock = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private StreamWriter writer;

        public string FilePath { get; private set; }

        public AutosplitterLog(string gameName)
        {
            string folder = UserData.Path(FolderName);
            Directory.CreateDirectory(folder);
            string safeName = gameName.Replace(' ', '_');
            FilePath = Path.Combine(folder, $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            writer = new StreamWriter(FilePath, false, Encoding.UTF8);
            writer.AutoFlush = true;
            writer.WriteLine($"# SluMAN autosplitter log: {gameName}, started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine("# Fields are numbered in the order the .asl reads them. Values are as in game memory");
            writer.WriteLine("# (big-endian hex), with 4-byte values also shown as an integer and a float.");
            writer.WriteLine("# time | ms since start | field | address | value");
        }

        /// <summary>Notes a field the autosplitter shares: its number, offset in the MMF and address.</summary>
        public void Field(int index, int offset, uint address, uint size)
        {
            Write($"# field {index}: MMF offset {offset}, address 0x{address:X8}, {size} bytes");
        }

        /// <summary>A field's value, as the autosplitter writes it: little-endian bytes.</summary>
        public void Value(int index, uint address, byte[] littleEndian, bool initial)
        {
            byte[] bigEndian = (byte[])littleEndian.Clone();
            Array.Reverse(bigEndian);
            StringBuilder line = new StringBuilder();
            line.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
            line.Append(" | ");
            line.Append(clock.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture).PadLeft(10));
            line.Append(" | ");
            line.Append(index.ToString().PadLeft(2));
            line.Append(" | 0x");
            line.Append(address.ToString("X8"));
            line.Append(" | ");
            line.Append(BitConverter.ToString(bigEndian).Replace("-", ""));
            if (littleEndian.Length == 4)
            {
                line.Append(" | int ");
                line.Append(BitConverter.ToInt32(littleEndian, 0));
                line.Append(" | float ");
                line.Append(BitConverter.ToSingle(littleEndian, 0).ToString("R", CultureInfo.InvariantCulture));
            }
            if (initial)
            {
                line.Append(" | initial");
            }
            Write(line.ToString());
        }

        private void Write(string line)
        {
            lock (writeLock)
            {
                if (writer == null)
                {
                    return;
                }
                try
                {
                    writer.WriteLine(line);
                }
                catch
                {
                    // Disk full or the file went away: the autosplitter keeps working.
                }
            }
        }

        public void Dispose()
        {
            lock (writeLock)
            {
                if (writer != null)
                {
                    writer.Dispose();
                    writer = null;
                }
            }
        }
    }
}
