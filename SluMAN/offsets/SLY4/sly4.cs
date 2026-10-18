using System;
using System.Linq;

namespace SluMAN
{
    public class sly4 : IGame
    {
        public static Sly4Addresses addr = Sly4Addresses.ForGame(Sly4Addresses.DefaultGameId);

        // Saved positions aren't per map yet: the level ID isn't confirmed.
        private const string PositionMapKey = "sly4";

        private const int NormalClampFreqMax = 60;
        private const int NormalClampFreqMin = 1;
        private const int SlowMotionClampFreqMax = 386;
        private const int SlowMotionClampFreqMin = 291;

        public sly4(IPS3API api, string gameNameId) : base(api)
        {
            addr = Sly4Addresses.ForGame(gameNameId);
        }

        public override void SavePosition()
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                throw new InvalidOperationException("The player isn't loaded.");
            }

            string position = api.ReadMemoryStr(pid, body + addr.bodyPositionOffset, 12);
            func.ChangeFileLines("config.txt", position, SavedPositionKey(PositionMapKey, selectedPositionIndex));
            OnPositionSaved();
        }

        public override void LoadPosition()
        {
            string position = func.GetConfigData("config.txt", SavedPositionKey(PositionMapKey, selectedPositionIndex));
            if (position == "")
            {
                throw new InvalidOperationException($"Slot {selectedPositionIndex + 1} has no saved position.");
            }

            byte[] bytes = IPS3API.HexToBytes(position);
            Teleport(ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8));
        }

        // TODO: The pad address isn't known yet, so there is no input display and combos never fire.
        public override void CheckInputs(object sender, EventArgs e)
        {
            RunCombos();
        }

        public bool IsPlayerLoaded()
        {
            return GetRigidBody() != 0;
        }

        public int GetCoinCount()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            byte[] bytes = api.ReadMemory(pid, gameManager + addr.coinCountOffset, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
        }

        public void SetCoinCount(int coins)
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                throw new InvalidOperationException("The game isn't loaded.");
            }
            api.WriteMemory(pid, gameManager + addr.coinCountOffset, ConvertIntToBytes(coins));
        }

        /// <summary>
        /// The player's X, Y and Z (Y is up), or null when the player isn't loaded. Read from the
        /// Havok body, which Teleport writes: the controller's copy sits lower, so teleporting the
        /// body to it would sink the player into the ground.
        /// </summary>
        public float[] GetPosition()
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                return null;
            }
            byte[] bytes = api.ReadMemory(pid, body + addr.bodyPositionOffset, 12);
            return new float[] { ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8) };
        }

        /// <summary>
        /// The player's velocity on X, Y and Z, or null when the player isn't loaded.
        /// </summary>
        public float[] GetVelocity()
        {
            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return null;
            }
            byte[] bytes = api.ReadMemory(pid, controller + addr.controllerVelocityOffset, 12);
            return new float[] { ToFloat(bytes, 0), ToFloat(bytes, 4), ToFloat(bytes, 8) };
        }

        /// <summary>
        /// Moves the player by writing the Havok body's motion state. The player's own position
        /// copies are refreshed from it every frame, so writing those does nothing.
        /// </summary>
        public void Teleport(float x, float y, float z)
        {
            uint body = GetRigidBody();
            if (body == 0)
            {
                throw new InvalidOperationException("The player isn't loaded.");
            }

            // The position, then the centre of mass at the start and end of the step. Havok moves
            // the body from the last two, so all three move by the same amount in one write. Their
            // W values hold the step time, so they stay as read.
            uint address = body + addr.bodyPositionOffset;
            byte[] state = api.ReadMemory(pid, address, 0x30);
            float[] delta = new float[]
            {
                x - ToFloat(state, 0),
                y - ToFloat(state, 4),
                z - ToFloat(state, 8),
            };

            for (int vector = 0; vector < 3; vector++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    int offset = vector * 16 + axis * 4;
                    byte[] value = ConvertFloatToBytes(ToFloat(state, offset) + delta[axis]);
                    Array.Copy(value, 0, state, offset, 4);
                }
            }

            api.WriteMemory(pid, address, state);
        }

        public void SetSlowMotion(bool enabled)
        {
            api.WriteMemory(pid, addr.clockClampFreqMax, ConvertIntToBytes(enabled ? SlowMotionClampFreqMax : NormalClampFreqMax));
            api.WriteMemory(pid, addr.clockClampFreqMin, ConvertIntToBytes(enabled ? SlowMotionClampFreqMin : NormalClampFreqMin));
        }

        public bool IsSlowMotionOn()
        {
            byte[] bytes = api.ReadMemory(pid, addr.clockClampFreqMax, 4);
            return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0) == SlowMotionClampFreqMax;
        }

        /// <summary>The player ref, or 0 while the player doesn't exist.</summary>
        private uint GetPlayerRef()
        {
            uint gameManager = ReadUInt(addr.gameManagerPtr);
            if (gameManager == 0)
            {
                return 0;
            }

            uint playerRef = ReadUInt(gameManager + addr.playerRefOffset);
            if (playerRef == 0)
            {
                return 0;
            }

            // Bit 1 is set while the player is live.
            if ((ReadUInt(playerRef + addr.playerFlagsOffset) & 2) == 0)
            {
                return 0;
            }
            return playerRef;
        }

        private uint GetCharacterController()
        {
            uint playerRef = GetPlayerRef();
            if (playerRef == 0)
            {
                return 0;
            }
            return ReadUInt(playerRef + addr.characterControllerOffset);
        }

        private uint GetRigidBody()
        {
            uint controller = GetCharacterController();
            if (controller == 0)
            {
                return 0;
            }

            uint characterBody = ReadUInt(controller + addr.characterBodyOffset);
            if (characterBody == 0)
            {
                return 0;
            }
            return ReadUInt(characterBody + addr.rigidBodyOffset);
        }

        private uint ReadUInt(uint address)
        {
            byte[] bytes = api.ReadMemory(pid, address, 4);
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }

        private static float ToFloat(byte[] bytes, int offset)
        {
            byte[] value = new byte[4];
            Array.Copy(bytes, offset, value, 0, 4);
            return BitConverter.ToSingle(value.Reverse().ToArray(), 0);
        }
    }
}
