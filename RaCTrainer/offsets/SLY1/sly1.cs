using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    public class Sly1Addresses
    {
        public uint inputOffset => 0x428BFC;
        public uint analogOffsetLeft => 0x428B94;
        public uint analogOffsetRight => 0x428BC8;
        public uint coinCount => 0x3E7FF4;
        public uint levelId => 0x3E7FE8;
        public uint worldId => 0x3E7FE4;
        public uint loadingState => 0xE5E940;
        public uint w3Keys => 0x3E7738;
        public uint transitionState => 0xE62AD0;
        public uint charmsCount => 0x3E7FF0;
        public uint livesCount => 0x3E7FEC;
        public uint clockwerkHealth => 0x3630941C;
    }

    public class sly1 : IGame, IAutosplitterAvailable
    {
        public static Sly1Addresses addr = new Sly1Addresses();

        public sly1(IPS3API api) : base(api)
        {
        }

        public IEnumerable<(uint addr, uint size)> AutosplitterAddresses => new (uint, uint)[]
        {
            (addr.levelId, 4),
            (addr.worldId, 4),
            (addr.loadingState, 4),
            (addr.w3Keys, 4),
            (addr.charmsCount, 4),
            (addr.livesCount, 4),
            (addr.coinCount, 4),
            (addr.transitionState, 4),
            (addr.clockwerkHealth, 4),
        };

        // TODO: Sly 1 position saving and reloading are not implemented yet.
        public override void SavePosition() { }
        public override void LoadPosition() { }
        public override void LoadGame() { }

        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        protected override void SetupInputDisplayMemorySubsButtons()
        {
            int buttonMaskSubID = api.SubMemory(pid, sly1.addr.inputOffset, 4, (value) =>
            {
                int slyButtonMask = BitConverter.ToInt32(value.Reverse().ToArray(), 0);

                int convertedMask = ConvertSlyButtonsToStandardFormat(slyButtonMask);

                Inputs.RawInputs = convertedMask;
                Inputs.Mask = Inputs.DecodeMask(convertedMask);
            });
        }

        private int ConvertSlyButtonsToStandardFormat(int slyMask)
        {
            int standardMask = 0;

            if ((slyMask & 0x0001) != 0) standardMask |= 0x100;   // Select
            if ((slyMask & 0x0008) != 0) standardMask |= 0x800;   // Start
            if ((slyMask & 0x0010) != 0) standardMask |= 0x1000;  // Up
            if ((slyMask & 0x0020) != 0) standardMask |= 0x2000;  // Right
            if ((slyMask & 0x0040) != 0) standardMask |= 0x4000;  // Down
            if ((slyMask & 0x0080) != 0) standardMask |= 0x8000;  // Left
            if ((slyMask & 0x0400) != 0) standardMask |= 0x4;     // L1
            if ((slyMask & 0x0100) != 0) standardMask |= 0x1;     // L2
            if ((slyMask & 0x0800) != 0) standardMask |= 0x8;     // R1
            if ((slyMask & 0x0200) != 0) standardMask |= 0x2;     // R2
            if ((slyMask & 0x1000) != 0) standardMask |= 0x10;    // Triangle
            if ((slyMask & 0x2000) != 0) standardMask |= 0x20;    // Circle
            if ((slyMask & 0x4000) != 0) standardMask |= 0x40;    // Cross
            if ((slyMask & 0x8000) != 0) standardMask |= 0x80;    // Square
            if ((slyMask & 0x0002) != 0) standardMask |= 0x200;  // L3
            if ((slyMask & 0x0004) != 0) standardMask |= 0x400;  // R3

            return standardMask;
        }

        protected override void SetupInputDisplayMemorySubsAnalogs()
        {
            int analogLSubID = api.SubMemory(pid, sly1.addr.analogOffsetLeft, 8, (value) =>
            {
                // Pressing D-pad also moves the stick in memory
                if ((Inputs.RawInputs & 0xF000) != 0)
                {
                    Inputs.ly = 0;
                    Inputs.lx = 0;
                    return;
                }

                Inputs.ly = -1 * BitConverter.ToSingle(value, 0);
                Inputs.lx = BitConverter.ToSingle(value, 4);
            });

            int analogRSubID = api.SubMemory(pid, sly1.addr.analogOffsetRight, 8, (value) =>
            {
                Inputs.ry = -1 * BitConverter.ToSingle(value, 0);
                Inputs.rx = BitConverter.ToSingle(value, 4);
            });
        }

        public void SetCoinCount(int coins)
        {
            api.WriteMemory(pid, sly1.addr.coinCount, ConvertIntToBytes(coins));
        }

        public void WriteMemoryRegion(uint startAddress, byte[] data)
        {
            var spliceSize = 256;
            for (int i = 0; i < data.Length; i += spliceSize)
            {
                int chunkSize = Math.Min(spliceSize, data.Length - i);
                byte[] chunk = new byte[chunkSize];
                Array.Copy(data, i, chunk, 0, chunkSize);
                api.WriteMemory(pid, startAddress + (uint)i, chunk);
            }
        }

        private byte[] ConvertIntToBytes(int value)
        {
            byte[] byteArray = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            return byteArray;
        }

        private static byte[] ConvertFloatToBytes(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public byte[] ReadMemoryRegion(uint startAddress, uint size)
        {
            return api.ReadMemory(pid, startAddress, size);
        }
    }
}
