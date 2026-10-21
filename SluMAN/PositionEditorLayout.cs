using System;

namespace SluMAN
{
    /// <summary>
    /// Where a game keeps what the Position Editor reads and writes. Offsets are relative to the
    /// player entity or its transform; an offset of 0 means it isn't known for this game, and the
    /// editor shows N/A for it (or skips it, for velocity).
    /// </summary>
    public class PositionEditorLayout
    {
        /// <summary>Shown in the window title, such as "Sly 3".</summary>
        public string gameName;

        /// <summary>
        /// Prefix for the warp files: data/{prefix}_warp_locations.txt (shipped) and
        /// {prefix}_user_warps.txt (the user's own).
        /// </summary>
        public string warpFilePrefix;

        /// <summary>Address holding the pointer to the active character's entity.</summary>
        public uint activeCharacterPtr;

        /// <summary>
        /// Reads the active character's entity (0 if there's none), for games where it isn't
        /// behind one pointer. Used instead of activeCharacterPtr when set.
        /// </summary>
        public Func<uint> readEntity;

        /// <summary>Address of the current map's name, such as "Y$KFv_ext".</summary>
        public uint mapNameAddress;

        /// <summary>
        /// Reads the name the current map's warps and position slots are kept under, for games
        /// without a map name in memory. Used instead of mapNameAddress when set.
        /// </summary>
        public Func<string> readMapIndicator;

        /// <summary>Entity offset of the pointer to the transform.</summary>
        public uint transformOffset;

        /// <summary>
        /// The transform is part of the entity, at transformOffset, rather than pointed to from
        /// there.
        /// </summary>
        public bool transformInEntity;

        /// <summary>Transform offset of X; Y and Z follow at +4 and +8.</summary>
        public uint positionOffset;

        /// <summary>Transform offset of X velocity; Y and Z follow at +4 and +8. 0 if unknown.</summary>
        public uint velocityOffset;

        // Entity offsets for the Character Info panel. 0 if unknown.
        public uint entityIdOffset;
        public uint healthOffset;
        public uint gadgetPowerOffset;
        public uint opacityOffset;
        public uint rotationOffset;

        /// <summary>
        /// Entity offset the editor writes 0 to while Infinite Jump is on and the editor is open.
        /// 0 if the game handles Infinite Jump elsewhere.
        /// </summary>
        public uint infiniteJumpOffset;

        /// <summary>
        /// Applies Infinite Jump to the player entity, for games that need more than one write.
        /// Called each tick instead of using infiniteJumpOffset when set.
        /// </summary>
        public Action<uint> applyInfiniteJump;

        /// <summary>True if any Character Info field is known; the panel is hidden otherwise.</summary>
        public bool HasCharacterInfo
        {
            get
            {
                return entityIdOffset != 0 || healthOffset != 0 || gadgetPowerOffset != 0
                    || opacityOffset != 0 || rotationOffset != 0;
            }
        }
    }

    /// <summary>
    /// The Practice window that opened the Position Editor. While the editor is open it runs Fly
    /// Mode and Infinite Jump itself, so it asks the window whether they're on.
    /// </summary>
    public interface IPositionEditorHost
    {
        bool FlyModeEnabled { get; }
        bool InfiniteJumpEnabled { get; }
    }
}
