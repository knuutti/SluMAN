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
    public class sly1 : IGame, IAutosplitterAvailable
    {
        public static Sly1Addresses addr = Sly1Addresses.ForGame(Sly1Addresses.DefaultGameId);

        public sly1(IPS3API api, string gameNameId) : base(api)
        {
            addr = Sly1Addresses.ForGame(gameNameId);
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
