using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The debug menu's Episodes browser: every episode, its jobs and each job's goals, with the
    /// running job marked and its goals coloured done / current / upcoming. Jobs start (or go to
    /// their hub or hideout) and goals warp through Sly4Practice, which tracks the game call and
    /// reports on the status bar. The controls are built in code.
    /// </summary>
    public class Sly4JobsForm : Form, IRefreshOnShow
    {
        // Goal changes arrive through GoalsChanged; the timer catches the rest, such as the
        // running job changing when the goal watcher can't run (old webMAN API).
        private const int RefreshIntervalMs = 2000;
        private const string RunningSuffix = "  (running)";

        private static readonly Color DoneColor = Color.ForestGreen;
        private static readonly Color CurrentColor = Color.DarkOrange;
        private static readonly Color UpcomingColor = Color.Firebrick;

        /// <summary>What a goal node stands for.</summary>
        private class GoalTag
        {
            public sly4.JobEntry job;
            public int index;
        }

        private readonly sly4 game;
        private readonly Sly4Practice practice;

        private readonly TreeView tree = new TreeView();
        private readonly Button startJobButton = new Button();
        private readonly Button toHubButton = new Button();
        private readonly Button toHideoutButton = new Button();
        private readonly Button warpToGoalButton = new Button();
        private readonly Button reloadButton = new Button();
        private readonly Label legendLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        // Job nodes by job index.
        private readonly Dictionary<int, TreeNode> jobNodes = new Dictionary<int, TreeNode>();
        private int runningJob = -1;
        // How many goals the running job had when its node was last filled from the game.
        private int runningGoalCount = -1;

        public Sly4JobsForm(sly4 game, Sly4Practice practice)
        {
            this.game = game;
            this.practice = practice;
            Name = "Sly4JobsForm";
            Text = "SluMAN :: Jobs";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(tree.Right + 8, legendLabel.Bottom + 8);
            // The tree grows with the tab when the Practice window has a fixed size. Anchored
            // only now: an anchor keeps the distance to the window's edge it has when set.
            tree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            foreach (Control control in new Control[] { startJobButton, toHubButton, toHideoutButton, warpToGoalButton, reloadButton, legendLabel })
            {
                control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            }

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += refreshTimer_Tick;
            game.GoalsChanged += game_GoalsChanged;
            FormClosing += (sender, e) =>
            {
                refreshTimer.Stop();
                game.GoalsChanged -= game_GoalsChanged;
            };
            FormClosed += (sender, e) => refreshTimer.Dispose();

            LoadJobs();
            refreshTimer.Start();
        }

        private void BuildControls()
        {
            tree.Location = new Point(8, 8);
            tree.Size = new Size(440, 400);
            tree.HideSelection = false;
            tree.ShowNodeToolTips = true;
            tree.BeforeExpand += tree_BeforeExpand;
            tree.AfterSelect += (sender, e) => UpdateButtons();
            tree.NodeMouseDoubleClick += tree_NodeMouseDoubleClick;
            Controls.Add(tree);

            AddButton(startJobButton, "Start Job", 8, 85, (sender, e) => StartSelected(false, false));
            AddButton(toHubButton, "Go to Hub", 97, 75, (sender, e) => StartSelected(true, false));
            AddButton(toHideoutButton, "Go to Hideout", 176, 85, (sender, e) => StartSelected(false, true));
            AddButton(warpToGoalButton, "Warp to Goal", 265, 85, (sender, e) => WarpToSelected());
            AddButton(reloadButton, "Reload", 354, 54, (sender, e) => LoadJobs());

            legendLabel.AutoSize = true;
            legendLabel.Location = new Point(8, 442);
            legendLabel.ForeColor = SystemColors.GrayText;
            legendLabel.Text = "Running job: green done, orange current, red upcoming. Double-click a goal to warp.";
            Controls.Add(legendLabel);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(startJobButton, "Starts the selected job (or the selected goal's job) at its first goal.");
            tip.SetToolTip(toHubButton, "Goes to the selected job's episode hub without starting a job.");
            tip.SetToolTip(toHideoutButton, "Goes to the selected job's episode hideout without starting a job.");
            tip.SetToolTip(warpToGoalButton, "Warps to the selected goal. A job that isn't running is started first.");
            tip.SetToolTip(reloadButton, "Reads the job list from the game again.");
            FormClosed += (sender, e) => tip.Dispose();

            UpdateButtons();
        }

        private void AddButton(Button button, string text, int x, int width, EventHandler click)
        {
            button.Text = text;
            button.Location = new Point(x, 414);
            button.Size = new Size(width, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            Controls.Add(button);
        }

        /// <summary>Reads the episodes and jobs from the game and rebuilds the tree.</summary>
        private void LoadJobs()
        {
            List<sly4.JobEntry> jobs;
            try
            {
                jobs = game.GetJobs();
            }
            catch (Exception ex)
            {
                func.Status($"Couldn't read the jobs: {ex.Message}", true);
                return;
            }

            tree.BeginUpdate();
            tree.Nodes.Clear();
            jobNodes.Clear();
            Dictionary<string, TreeNode> episodeNodes = new Dictionary<string, TreeNode>();
            foreach (sly4.JobEntry job in jobs)
            {
                TreeNode episodeNode;
                if (!episodeNodes.TryGetValue(job.episode, out episodeNode))
                {
                    episodeNode = new TreeNode(job.EpisodeTitle);
                    episodeNode.Tag = job.episode;
                    tree.Nodes.Add(episodeNode);
                    episodeNodes[job.episode] = episodeNode;
                }

                TreeNode jobNode = new TreeNode(job.Title);
                jobNode.Tag = job;
                jobNode.ToolTipText = $"Job {job.index}: {job.name}";
                // Filled when first expanded; the placeholder gives it the expand button.
                jobNode.Nodes.Add(new TreeNode());
                episodeNode.Nodes.Add(jobNode);
                jobNodes[job.index] = jobNode;
            }
            tree.EndUpdate();

            runningJob = -1;
            runningGoalCount = -1;
            try
            {
                RefreshRunningJob();
            }
            catch
            {
                // Marked on the next refresh.
            }
            UpdateButtons();
        }

        /// <summary>Lists a job's goals: read from the game while it runs, otherwise the known names.</summary>
        private void FillGoals(TreeNode jobNode)
        {
            sly4.JobEntry job = (sly4.JobEntry)jobNode.Tag;
            List<string> names;
            try
            {
                names = game.GetGoalNames(job);
            }
            catch (Exception ex)
            {
                func.Status($"Couldn't read the goals: {ex.Message}", true);
                return;
            }

            tree.BeginUpdate();
            jobNode.Nodes.Clear();
            if (names.Count == 0)
            {
                TreeNode none = new TreeNode("(goals not known)");
                none.ForeColor = SystemColors.GrayText;
                jobNode.Nodes.Add(none);
            }
            for (int i = 0; i < names.Count; i++)
            {
                TreeNode goalNode = new TreeNode($"{i + 1}. {names[i]}");
                goalNode.Tag = new GoalTag { job = job, index = i };
                jobNode.Nodes.Add(goalNode);
            }
            tree.EndUpdate();

            if (job.index == runningJob)
            {
                runningGoalCount = names.Count;
                ColourGoals(jobNode);
            }
        }

        /// <summary>Marks the running job and colours its goals.</summary>
        private void RefreshRunningJob()
        {
            int running = game.GetRunningJobIndex();
            if (running != runningJob)
            {
                SetRunningMark(runningJob, false);
                SetRunningMark(running, true);
                runningJob = running;
                runningGoalCount = -1;
            }

            TreeNode jobNode;
            if (running < 0 || !jobNodes.TryGetValue(running, out jobNode) || !jobNode.IsExpanded)
            {
                return;
            }

            // The running job's goals are read from the game, which may differ from the known list.
            int count = game.GetGoalStates().Count;
            if (count != runningGoalCount)
            {
                FillGoals(jobNode);
            }
            else
            {
                ColourGoals(jobNode);
            }
        }

        private void SetRunningMark(int jobIndex, bool running)
        {
            TreeNode jobNode;
            if (jobIndex < 0 || !jobNodes.TryGetValue(jobIndex, out jobNode))
            {
                return;
            }

            string name = ((sly4.JobEntry)jobNode.Tag).Title;
            jobNode.Text = running ? name + RunningSuffix : name;
            jobNode.ForeColor = running ? CurrentColor : SystemColors.WindowText;
            if (jobNode.Parent != null)
            {
                jobNode.Parent.ForeColor = running ? CurrentColor : SystemColors.WindowText;
            }

            // A job that just started has its goals read from the game by RefreshRunningJob.
            if (!running)
            {
                foreach (TreeNode goalNode in jobNode.Nodes)
                {
                    if (goalNode.Tag is GoalTag)
                    {
                        goalNode.ForeColor = SystemColors.WindowText;
                    }
                }
            }
        }

        private void ColourGoals(TreeNode jobNode)
        {
            List<int> states = game.GetGoalStates();
            bool currentFound = false;
            foreach (TreeNode goalNode in jobNode.Nodes)
            {
                GoalTag goal = goalNode.Tag as GoalTag;
                if (goal == null || goal.index >= states.Count)
                {
                    continue;
                }

                Color colour;
                if (states[goal.index] == Sly4Addresses.GoalStateDone)
                {
                    colour = DoneColor;
                }
                else if (!currentFound && states[goal.index] >= 0)
                {
                    colour = CurrentColor;
                    currentFound = true;
                }
                else
                {
                    colour = UpcomingColor;
                }

                if (goalNode.ForeColor != colour)
                {
                    goalNode.ForeColor = colour;
                }
            }
        }

        public void RefreshFromGame()
        {
            if (tree.Nodes.Count == 0)
            {
                // The list couldn't be read when the tab opened; try again now.
                LoadJobs();
                return;
            }
            try
            {
                RefreshRunningJob();
            }
            catch
            {
                // The timer tries again.
            }
        }

        private void game_GoalsChanged()
        {
            // Raised on the goal watcher's thread: recolour the goals now rather than on the next tick.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() => refreshTimer_Tick(this, EventArgs.Empty)));
            }
            catch
            {
                // The form closed in between.
            }
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            // Only while this tab is the one showing.
            if (!Visible || (Parent != null && !Parent.Visible) || tree.Nodes.Count == 0)
            {
                return;
            }
            try
            {
                RefreshRunningJob();
            }
            catch
            {
                // The game is loading or reconnecting; the next tick tries again.
            }
        }

        private void tree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Tag is sly4.JobEntry)
            {
                FillGoals(e.Node);
            }
        }

        private void tree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is GoalTag)
            {
                tree.SelectedNode = e.Node;
                WarpToSelected();
            }
        }

        /// <summary>The selected job, or the job of the selected goal. Null for an episode.</summary>
        private sly4.JobEntry SelectedJob()
        {
            TreeNode node = tree.SelectedNode;
            if (node == null)
            {
                return null;
            }
            sly4.JobEntry job = node.Tag as sly4.JobEntry;
            if (job != null)
            {
                return job;
            }
            GoalTag goal = node.Tag as GoalTag;
            return goal != null ? goal.job : null;
        }

        private void UpdateButtons()
        {
            bool hasJob = SelectedJob() != null;
            startJobButton.Enabled = hasJob;
            toHubButton.Enabled = hasJob;
            toHideoutButton.Enabled = hasJob;
            warpToGoalButton.Enabled = tree.SelectedNode != null && tree.SelectedNode.Tag is GoalTag;
        }

        private void StartSelected(bool toHub, bool toHideout)
        {
            sly4.JobEntry job = SelectedJob();
            if (job != null)
            {
                practice.StartJob(job, toHub, toHideout);
            }
        }

        private void WarpToSelected()
        {
            GoalTag goal = tree.SelectedNode != null ? tree.SelectedNode.Tag as GoalTag : null;
            if (goal != null)
            {
                practice.WarpToGoal(goal.job, goal.index);
            }
        }
    }
}
