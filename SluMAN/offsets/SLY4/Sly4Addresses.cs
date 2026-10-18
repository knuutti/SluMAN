using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 4 addresses. Each supported game version is one instance, created in CreateVersions.
    /// </summary>
    public class Sly4Addresses
    {
        public const string GameIdPALDisc = "BCES01284";
        public const string DefaultGameId = GameIdPALDisc;

        private static readonly Dictionary<string, Sly4Addresses> versions = CreateVersions();

        public string GameId { get; private set; }
        public string DisplayName { get; private set; }

        // Pointers
        public uint gameManagerPtr { get; private set; }

        // Game manager offsets
        public uint coinCountOffset { get; private set; }
        public uint playerRefOffset { get; private set; }

        // Player offsets (from the player ref)
        public uint playerFlagsOffset { get; private set; }
        public uint characterControllerOffset { get; private set; }

        // Character controller offsets
        public uint controllerVelocityOffset { get; private set; }
        public uint characterBodyOffset { get; private set; }

        // Havok offsets: hkpCharacterRigidBody to its hkpRigidBody, then the body's motion state
        public uint rigidBodyOffset { get; private set; }
        public uint bodyPositionOffset { get; private set; }

        // Tunables (the value, at +0x48 of each tunable struct)
        public uint clockClampFreqMax { get; private set; }
        public uint clockClampFreqMin { get; private set; }

        private Sly4Addresses()
        {
        }

        /// <summary>
        /// The addresses for the given title ID, or the default version's if it isn't supported.
        /// </summary>
        public static Sly4Addresses ForGame(string gameId)
        {
            Sly4Addresses found;
            if (gameId != null && versions.TryGetValue(gameId.Trim(), out found))
            {
                return found;
            }
            return versions[DefaultGameId];
        }

        public static bool IsSupportedGameId(string gameId)
        {
            return gameId != null && versions.ContainsKey(gameId.Trim());
        }

        /// <summary>
        /// A copy of this version under another title ID. Set only the addresses that differ on it.
        /// </summary>
        private Sly4Addresses CopyAs(string gameId, string displayName)
        {
            Sly4Addresses copy = (Sly4Addresses)MemberwiseClone();
            copy.GameId = gameId;
            copy.DisplayName = displayName;
            return copy;
        }

        private static Dictionary<string, Sly4Addresses> CreateVersions()
        {
            Sly4Addresses palDisc = new Sly4Addresses
            {
                GameId = GameIdPALDisc,
                DisplayName = "SLY 4 (PAL, Disc)",

                gameManagerPtr = 0xF65CA0,

                coinCountOffset = 0x70,
                playerRefOffset = 0x1078,

                playerFlagsOffset = 0xC0,
                characterControllerOffset = 0x18C,

                controllerVelocityOffset = 0x250,
                characterBodyOffset = 0x20,

                rigidBodyOffset = 0x10,
                bodyPositionOffset = 0x120,

                clockClampFreqMax = 0x10B7AA4,
                // The tunable right before Debug.Clock.ClampFreqMax; its name isn't confirmed.
                clockClampFreqMin = 0x10B7A58,
            };

            return new Dictionary<string, Sly4Addresses>(StringComparer.OrdinalIgnoreCase)
            {
                { palDisc.GameId, palDisc },
            };
        }
    }
}
