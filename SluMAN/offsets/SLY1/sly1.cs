using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
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
            SubscribeSlyButtons(sly1.addr.inputOffset);
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
    }
}
