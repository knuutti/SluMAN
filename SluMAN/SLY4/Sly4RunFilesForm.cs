using System;
using System.Collections.Generic;

namespace SluMAN
{
    /// <summary>
    /// Sly 4's Run Files tab: goes to the hideout of the run file's episode's first job, with the
    /// gadgets saved for it in the Gadgets tab. Practice Mode starts it (Sly4Practice.LoadRunFile).
    /// </summary>
    public class Sly4RunFilesForm : RunFilesForm
    {
        // Each run file's episode, by the episode's name key.
        private static readonly Dictionary<string, string> Episodes = new Dictionary<string, string>
        {
            { "Episode 1", "Japan" },
            { "Episode 2", "West" },
            { "Episode 3", "IceAge" },
            { "Episode 4", "England" },
            { "Episode 5", "Arabia" },
        };

        private readonly sly4 game;
        private readonly Sly4Practice practice;

        public Sly4RunFilesForm(sly4 game, Sly4Practice practice)
            : base("Sly 4", Sly4Gadgets.RunFiles,
                "A run file starts in the hideout of its episode's first job. The gadgets it loads with are saved in the Gadgets tab, under Run file gadgets.")
        {
            this.game = game;
            this.practice = practice;
        }

        protected override void LoadRunFile(string runFile)
        {
            string episode = Episodes[runFile];
            sly4.JobEntry firstJob = null;
            foreach (sly4.JobEntry job in game.GetJobs())
            {
                if (string.Equals(job.episode, episode, StringComparison.OrdinalIgnoreCase))
                {
                    firstJob = job;
                    break;
                }
            }
            if (firstJob == null)
            {
                statusLine.Error($"The game's job list has no {sly4.Title(episode)} jobs.");
                return;
            }

            byte[] gadgets = Sly4Gadgets.LoadRunFile(runFile);
            practice.LoadRunFile(runFile, firstJob, gadgets);
            statusLine.Info(gadgets == null
                ? $"Loading {runFile}. No gadgets are saved for it, so they were left as they are."
                : $"Loading {runFile} with its saved gadgets.");
        }
    }
}
