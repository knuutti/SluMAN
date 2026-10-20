using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SluMAN
{
    /// <summary>
    /// A free camera animation: keyframes the camera passes through, each with the time it takes
    /// to reach the next one. Saved per level in the user data folder's camera paths.
    /// <list type="bullet">
    /// <item>Per keyframe (the default): each segment runs on its own with its keyframe's easing.
    /// Positions follow a Catmull-Rom curve, so the path has no corners, but speed and turning
    /// change where segments meet, so keyframes show.</item>
    /// <item>Smooth: the whole path is one curve in time. Position, yaw and pitch keep their speed
    /// through every keyframe (cubic Hermite with time-weighted tangents), easing only at the
    /// start and end. Easing settings are ignored.</item>
    /// </list>
    /// Yaw always turns the short way round.
    /// </summary>
    public class Sly4CameraPath
    {
        public enum Easing { Linear, EaseInOut, Hold }

        public class Keyframe
        {
            public float x;
            public float y;
            public float z;
            /// <summary>Radians, as the free camera uses them.</summary>
            public float yaw;
            public float pitch;
            /// <summary>Seconds from this keyframe to the next. The last keyframe's isn't used.</summary>
            public float duration = 2f;
            public Easing easing = Easing.EaseInOut;
        }

        public readonly List<Keyframe> Keyframes = new List<Keyframe>();

        /// <summary>Plays as one continuous curve instead of segment by segment.</summary>
        public bool Smooth;

        /// <summary>Seconds from the first keyframe to the last.</summary>
        public float TotalDuration
        {
            get
            {
                float total = 0;
                for (int i = 0; i < Keyframes.Count - 1; i++)
                {
                    total += Math.Max(0, Keyframes[i].duration);
                }
                return total;
            }
        }

        /// <summary>When the animation reaches keyframe <paramref name="index"/>, in seconds.</summary>
        public float TimeOf(int index)
        {
            float time = 0;
            for (int i = 0; i < index && i < Keyframes.Count - 1; i++)
            {
                time += Math.Max(0, Keyframes[i].duration);
            }
            return time;
        }

        /// <summary>
        /// Where the camera is <paramref name="time"/> seconds into the animation. Before the start
        /// it's at the first keyframe, after the end at the last. Needs at least one keyframe.
        /// </summary>
        public void Evaluate(float time, float[] position, out float yaw, out float pitch)
        {
            int count = Keyframes.Count;
            if (count == 1 || time <= 0)
            {
                Set(Keyframes[0], position, out yaw, out pitch);
                return;
            }
            if (Smooth)
            {
                EvaluateSmooth(time, position, out yaw, out pitch);
                return;
            }

            // Find the segment: keyframe i to i + 1, and how far along it (0 to 1).
            int segment = count - 1;
            float t = 1;
            float start = 0;
            for (int i = 0; i < count - 1; i++)
            {
                float duration = Math.Max(0, Keyframes[i].duration);
                if (time < start + duration)
                {
                    segment = i;
                    t = (time - start) / duration;
                    break;
                }
                start += duration;
            }
            if (segment == count - 1)
            {
                Set(Keyframes[count - 1], position, out yaw, out pitch);
                return;
            }

            Keyframe from = Keyframes[segment];
            Keyframe to = Keyframes[segment + 1];
            t = Ease(t, from.easing);

            // The keyframes either side shape the curve; at the ends the end keyframe stands in.
            Keyframe before = Keyframes[Math.Max(0, segment - 1)];
            Keyframe after = Keyframes[Math.Min(count - 1, segment + 2)];
            position[0] = CatmullRom(before.x, from.x, to.x, after.x, t);
            position[1] = CatmullRom(before.y, from.y, to.y, after.y, t);
            position[2] = CatmullRom(before.z, from.z, to.z, after.z, t);

            yaw = from.yaw + ShortestTurn(from.yaw, to.yaw) * t;
            pitch = from.pitch + (to.pitch - from.pitch) * t;
        }

        /// <summary>
        /// The Smooth path: a cubic Hermite curve per value through every keyframe, with each
        /// keyframe's rate of change the time-weighted average of the segments either side, so
        /// nothing jumps at a keyframe. The ends have zero rate, so the path eases in and out.
        /// </summary>
        private void EvaluateSmooth(float time, float[] position, out float yaw, out float pitch)
        {
            int count = Keyframes.Count;
            float[] times = new float[count];
            for (int i = 1; i < count; i++)
            {
                times[i] = times[i - 1] + Math.Max(0, Keyframes[i - 1].duration);
            }
            if (time >= times[count - 1])
            {
                Set(Keyframes[count - 1], position, out yaw, out pitch);
                return;
            }

            // Yaw unwrapped, so each step between keyframes is the short way round.
            float[] yaws = new float[count];
            yaws[0] = Keyframes[0].yaw;
            for (int i = 1; i < count; i++)
            {
                yaws[i] = yaws[i - 1] + ShortestTurn(Keyframes[i - 1].yaw, Keyframes[i].yaw);
            }

            int segment = 0;
            while (segment < count - 2 && time >= times[segment + 1])
            {
                segment++;
            }
            float duration = times[segment + 1] - times[segment];
            float s = duration <= 0 ? 1 : (time - times[segment]) / duration;

            position[0] = SmoothValue(k => Keyframes[k].x, times, segment, s);
            position[1] = SmoothValue(k => Keyframes[k].y, times, segment, s);
            position[2] = SmoothValue(k => Keyframes[k].z, times, segment, s);
            yaw = SmoothValue(k => yaws[k], times, segment, s);
            pitch = SmoothValue(k => Keyframes[k].pitch, times, segment, s);
        }

        /// <summary>One value on the Smooth curve, <paramref name="s"/> (0 to 1) into a segment.</summary>
        private static float SmoothValue(Func<int, float> value, float[] times, int segment, float s)
        {
            float duration = times[segment + 1] - times[segment];
            float from = value(segment);
            float to = value(segment + 1);
            float fromRate = Rate(value, times, segment);
            float toRate = Rate(value, times, segment + 1);

            float s2 = s * s;
            float s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * from
                + (s3 - 2 * s2 + s) * duration * fromRate
                + (-2 * s3 + 3 * s2) * to
                + (s3 - s2) * duration * toRate;
        }

        /// <summary>
        /// How fast a value changes as the path passes keyframe <paramref name="index"/> (per
        /// second): the two segments' speeds, each weighted by the other's duration. Zero at the ends.
        /// </summary>
        private static float Rate(Func<int, float> value, float[] times, int index)
        {
            if (index == 0 || index == times.Length - 1)
            {
                return 0;
            }
            float before = times[index] - times[index - 1];
            float after = times[index + 1] - times[index];
            if (before <= 0 || after <= 0)
            {
                return 0;
            }
            float speedBefore = (value(index) - value(index - 1)) / before;
            float speedAfter = (value(index + 1) - value(index)) / after;
            return (speedBefore * after + speedAfter * before) / (before + after);
        }

        private static void Set(Keyframe key, float[] position, out float yaw, out float pitch)
        {
            position[0] = key.x;
            position[1] = key.y;
            position[2] = key.z;
            yaw = key.yaw;
            pitch = key.pitch;
        }

        private static float Ease(float t, Easing easing)
        {
            switch (easing)
            {
                case Easing.EaseInOut:
                    return t * t * (3 - 2 * t);
                case Easing.Hold:
                    // Stays at this keyframe, then cuts to the next.
                    return 0;
                default:
                    return t;
            }
        }

        /// <summary>The curve through p1 (t = 0) and p2 (t = 1), shaped by p0 and p3.</summary>
        private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3);
        }

        /// <summary>The turn from one yaw to another, between -pi and pi.</summary>
        private static float ShortestTurn(float from, float to)
        {
            double turn = (to - from) % (2 * Math.PI);
            if (turn > Math.PI)
            {
                turn -= 2 * Math.PI;
            }
            else if (turn < -Math.PI)
            {
                turn += 2 * Math.PI;
            }
            return (float)turn;
        }

        // --- Files ---
        // camerapaths/<level>-<name>.txt, one keyframe per line:
        // x y z yaw pitch duration easing (yaw and pitch in degrees, so the file is readable).
        // A line "smooth" plays the path as one continuous curve.

        private const string SmoothLine = "smooth";

        private const string FileExtension = ".txt";

        public static string Folder => UserData.Path("camerapaths");

        private static string FilePrefix(string levelName)
        {
            return SafeFileName(levelName) + "-";
        }

        public static string FilePath(string levelName, string pathName)
        {
            return Path.Combine(Folder, FilePrefix(levelName) + pathName + FileExtension);
        }

        /// <summary>The names of the paths saved for a level.</summary>
        public static List<string> SavedNames(string levelName)
        {
            List<string> names = new List<string>();
            if (!Directory.Exists(Folder))
            {
                return names;
            }
            string prefix = FilePrefix(levelName);
            foreach (string file in Directory.GetFiles(Folder, prefix + "*" + FileExtension))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                names.Add(name.Substring(prefix.Length));
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        public static bool IsValidName(string pathName)
        {
            return pathName.Trim() != "" && pathName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public void Save(string levelName, string pathName)
        {
            Directory.CreateDirectory(Folder);
            List<string> lines = new List<string>();
            lines.Add("# SluMAN Sly 4 camera path: x y z yaw pitch duration easing (degrees, seconds)");
            if (Smooth)
            {
                lines.Add(SmoothLine);
            }
            foreach (Keyframe key in Keyframes)
            {
                lines.Add(string.Join(" ",
                    Format(key.x), Format(key.y), Format(key.z),
                    Format(Degrees(key.yaw)), Format(Degrees(key.pitch)),
                    Format(key.duration), key.easing.ToString()));
            }
            File.WriteAllLines(FilePath(levelName, pathName), lines);
        }

        public static Sly4CameraPath Load(string levelName, string pathName)
        {
            Sly4CameraPath path = new Sly4CameraPath();
            foreach (string line in File.ReadAllLines(FilePath(levelName, pathName)))
            {
                string trimmed = line.Trim();
                if (trimmed == "" || trimmed.StartsWith("#"))
                {
                    continue;
                }
                if (string.Equals(trimmed, SmoothLine, StringComparison.OrdinalIgnoreCase))
                {
                    path.Smooth = true;
                    continue;
                }
                string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6)
                {
                    throw new FormatException($"Not a keyframe: \"{trimmed}\"");
                }
                Keyframe key = new Keyframe();
                key.x = Parse(parts[0]);
                key.y = Parse(parts[1]);
                key.z = Parse(parts[2]);
                key.yaw = Radians(Parse(parts[3]));
                key.pitch = Radians(Parse(parts[4]));
                key.duration = Parse(parts[5]);
                Easing easing;
                if (parts.Length > 6 && Enum.TryParse(parts[6], true, out easing))
                {
                    key.easing = easing;
                }
                path.Keyframes.Add(key);
            }
            return path;
        }

        public static void Delete(string levelName, string pathName)
        {
            File.Delete(FilePath(levelName, pathName));
        }

        public static float Degrees(float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }

        public static float Radians(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float Parse(string text)
        {
            return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static string SafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
