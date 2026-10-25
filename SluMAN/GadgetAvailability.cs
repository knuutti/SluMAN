using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SluMAN
{
    /// <summary>
    /// Which gadgets a run file may have. Each game's gadget table gives every gadget the episode
    /// it is available from: the first episode whose run file may have it, as it's in ThiefNet by
    /// the time that episode starts (0: from the start of the game). Saving a run file's gadgets
    /// removes the ones that aren't available yet, and the Gadgets tab says which.
    /// </summary>
    public static class GadgetAvailability
    {
        // How many removed gadgets the warning names before "and N more".
        private const int NamedInWarning = 5;

        /// <summary>The episode a run file starts, from its name ("Episode 6 (CE)" is 6).</summary>
        public static int RunFileEpisode(string runFile)
        {
            Match match = Regex.Match(runFile, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }

        public static bool IsAvailable(int availableFromEpisode, string runFile)
        {
            return availableFromEpisode <= RunFileEpisode(runFile);
        }

        /// <summary>The inline warning for the gadgets removed from a run file, or "" if none were.</summary>
        public static string RemovedWarning(string runFile, List<string> removed)
        {
            if (removed.Count == 0)
            {
                return "";
            }

            List<string> named = removed.Count > NamedInWarning ? removed.GetRange(0, NamedInWarning) : removed;
            string more = removed.Count > NamedInWarning ? $" and {removed.Count - NamedInWarning} more" : "";
            return $"Not in ThiefNet yet at the start of {runFile}, so not saved: {string.Join(", ", named)}{more}.";
        }
    }
}
