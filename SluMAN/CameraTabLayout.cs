using System;

namespace SluMAN
{
    /// <summary>
    /// A game's free camera, as the Camera tab drives it: flying, field of view and path playback.
    /// Raise the events on the UI thread.
    /// </summary>
    public interface IFreeCamera
    {
        event Action ModeChanged;
        event Action KeyframeRequested;
        event Action PlaybackFinished;
        /// <summary>A message for the status line, and whether it's an error.</summary>
        event Action<string, bool> Notice;

        bool IsOn { get; }
        /// <summary>Whether the game's field of view could be read, so zoom works.</summary>
        bool FovKnown { get; }
        float CameraSpeed { get; set; }
        /// <summary>Radians per second at full stick.</summary>
        float LookSpeed { get; set; }
        /// <summary>The free camera's field of view, in radians.</summary>
        float Fov { get; set; }
        /// <summary>The game's field of view when the free camera started, in radians.</summary>
        float GameFov { get; }
        bool CharacterControl { get; set; }
        bool IsPlaying { get; }
        float PlaybackTime { get; }

        void Start();
        void Stop();
        void Play(CameraPath path, bool loop);
        void StopPlayback();
        bool GetCamera(out float[] position, out float cameraYaw, out float cameraPitch, out float cameraFov);
        bool SetCamera(float[] position, float cameraYaw, float cameraPitch, float cameraFov);
    }

    /// <summary>A local HTTP API scripts drive the free camera through, shown as a checkbox on the Camera tab.</summary>
    public interface ICameraScriptApi
    {
        int Port { get; }
        bool Enabled { get; set; }
        bool IsRunning { get; }
        /// <summary>Why the API last failed to start, or "".</summary>
        string LastError { get; }
        string Url { get; }
    }

    /// <summary>
    /// Reads where the game is, for saving camera paths: the name paths are saved under ("" when
    /// in no level), and the name to show for it.
    /// </summary>
    public delegate string ReadCameraPlace(out string displayName);

    /// <summary>What the shared Camera tab (CameraForm) needs from a game.</summary>
    public class CameraTabLayout
    {
        public IFreeCamera freeCamera;

        /// <summary>The folder the game's camera paths are saved in (a CameraPath folder).</summary>
        public string pathFolder;

        /// <summary>What paths are saved per: "level" or "map".</summary>
        public string placeWord = "level";
        public ReadCameraPlace readPlace;

        public Func<bool> isHudHidden;
        public Action<bool> setHudHidden;
        public string hudToolTip;

        /// <summary>Null if the game can't tell; the box then starts unticked.</summary>
        public Func<bool> isFogDisabled;
        /// <summary>False if the game has no camera to change yet.</summary>
        public Func<bool, bool> setFogDisabled;
        public string fogOffMessage = "Fog moved out of sight.";
        public string fogToolTip = "Turns the fog off, and keeps it off after loads. Untick to put it back.";

        /// <summary>The middle of the controls help: what happens to the player, and when the camera turns off.</summary>
        public string playerHelp;
        public string characterControlToolTip =
            "The free camera stays where it is, or keeps playing its path, and the controller\n" +
            "moves the character as usual. Untick to fly the camera again.";

        public int minMoveSpeed = 10;
        public int maxMoveSpeed = 20000;
        public int moveSpeedIncrement = 100;

        /// <summary>The field of view's range, in radians.</summary>
        public float minFov;
        public float maxFov;
        public int fovDecimals = 2;
        /// <summary>The field of view box's first value in degrees; 0 for the game's.</summary>
        public float startFovDegrees;
        public string fovToolTip = "The free camera's field of view. R2 zooms in, L2 out.";

        /// <summary>Which axis is up, for the Set Camera tooltip.</summary>
        public string upAxis = "Z";

        /// <summary>
        /// The free camera's far distance (0 for the game's) and the game's own. A Far distance
        /// row shows when set.
        /// </summary>
        public Func<float> getFarDistance;
        public Action<float> setFarDistance;
        public Func<float> getGameFar;

        /// <summary>Shown as a checkbox below Playback when set.</summary>
        public ICameraScriptApi scriptApi;
    }
}
