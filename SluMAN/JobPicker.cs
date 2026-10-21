using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// A Job dropdown and a Checkpoint dropdown over a game's flat job list, like Sly 4's Job and
    /// Goal dropdowns. In the flat list a job is followed by its checkpoints, each written as
    /// " [ABBR] Name"; <see cref="SelectedEntry"/> gives back the flat list's entry for the
    /// selection, which is what the game's LoadJob takes.
    /// </summary>
    public class JobPicker
    {
        private const string JobStart = "Job start";

        private class Job
        {
            public string name;
            // The checkpoints' flat list entries, " [ABBR] Name".
            public List<string> checkpoints = new List<string>();

            public override string ToString()
            {
                return name;
            }
        }

        private readonly ComboBox jobComboBox;
        private readonly ComboBox checkpointComboBox;

        /// <param name="entries">The flat job list. A checkpoint before any job is skipped.</param>
        public JobPicker(ComboBox jobComboBox, ComboBox checkpointComboBox, IEnumerable<string> entries)
        {
            this.jobComboBox = jobComboBox;
            this.checkpointComboBox = checkpointComboBox;

            jobComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            checkpointComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            jobComboBox.Items.Clear();

            Job current = null;
            foreach (string entry in entries)
            {
                if (IsCheckpoint(entry))
                {
                    if (current != null)
                    {
                        current.checkpoints.Add(entry);
                    }
                }
                else
                {
                    current = new Job { name = entry };
                    jobComboBox.Items.Add(current);
                }
            }

            jobComboBox.SelectedIndexChanged += (sender, e) => FillCheckpoints();
            if (jobComboBox.Items.Count > 0)
            {
                jobComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// The flat list's entry for the selected job and checkpoint: the job's own entry for
        /// Job start, otherwise the checkpoint's. Null with no job selected.
        /// </summary>
        public string SelectedEntry
        {
            get
            {
                Job job = jobComboBox.SelectedItem as Job;
                if (job == null)
                {
                    return null;
                }
                int checkpoint = checkpointComboBox.SelectedIndex;
                return checkpoint <= 0 ? job.name : job.checkpoints[checkpoint - 1];
            }
        }

        private void FillCheckpoints()
        {
            checkpointComboBox.Items.Clear();
            checkpointComboBox.Items.Add(JobStart);
            Job job = jobComboBox.SelectedItem as Job;
            if (job != null)
            {
                foreach (string checkpoint in job.checkpoints)
                {
                    checkpointComboBox.Items.Add(CheckpointName(checkpoint));
                }
            }
            checkpointComboBox.SelectedIndex = 0;
            // A job without checkpoints only loads from its start.
            checkpointComboBox.Enabled = checkpointComboBox.Items.Count > 1;
        }

        private static bool IsCheckpoint(string entry)
        {
            return entry.TrimStart().StartsWith("[") && entry.Contains("]");
        }

        /// <summary>" [BnE] Sneak" -> "Sneak"</summary>
        private static string CheckpointName(string entry)
        {
            return entry.Substring(entry.IndexOf(']') + 1).Trim();
        }
    }
}
