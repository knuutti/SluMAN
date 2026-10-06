using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using racman.Memory;
using System.Diagnostics;
using AutoUpdaterDotNET;

namespace racman
{
    public partial class AttachPS3Form : Form
    {
        bool useOldAPI = false;

        public static RacManConsole console;

        public static RacmanScripting scripting;

        static ModLoaderForm modLoaderForm;
        static MemoryForm memoryForm;
        public static bool notSupported = false;

        public AttachPS3Form()
        {
            InitializeComponent();

            RacManConsole.RedirectOutput();

            console = new RacManConsole();
            scripting = new RacmanScripting();

            currentVerLabel.Text = "SluMAN v" + Assembly.GetEntryAssembly().GetName().Version.ToString(3);

            AutoUpdater.RunUpdateAsAdmin = false;
            // Handling this event stops AutoUpdater from opening its dialog by itself.
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            if (File.Exists(Environment.CurrentDirectory + @"\config.txt"))
            {
                ip = func.GetConfigData("config.txt", "ip");//ip = File.ReadAllText(Environment.CurrentDirectory + @"\config.txt");
            }
            else
            {
                // Try to copy the template config.txt from the source directory
                string sourceConfigPath = Path.Combine(Application.StartupPath, "..", "..", "..", "config.txt");
                if (File.Exists(sourceConfigPath))
                {
                    File.Copy(sourceConfigPath, "config.txt");
                }
                else
                {
                    // Fallback: create empty config if template not found
                    var config = File.Create("config.txt");
                    config.Close();
                }
            }
            IPTextBox.Text = ip;

            // Make a confirm alert dialog to make sure the user confirms to the terms of service
            // If they don't, close the program
            var tos = func.GetConfigData("config.txt", "tos");

            if (tos == "")
            {
                var dialogResult = MessageBox.Show("By using this program, you agree that trans rights are human rights?", "Terms of Service", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    // Show a dialog that explains why it's important to agree to the terms of service
                    MessageBox.Show("Get fucked.");
                    Environment.Exit(0);
                }
                else
                {
                    func.ChangeFileLines("config.txt", "yes", "tos");
                }
            }

            ConfigureCombos.GetCombos();

#if !DEBUG
            if (func.GetConfigData("config.txt", CheckForUpdatesKey) != "false")
            {
                StartUpdateCheck(false);
            }
#endif
        }

        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/knuutti/SluMAN/master/update.xml";
        private const string CheckForUpdatesKey = "checkForUpdates";

        private UpdateInfoEventArgs availableUpdate = null;
        private bool manualUpdateCheck = false;

        /// <summary>
        /// Checks update.xml in the background. The result shows next to the version label; nothing
        /// opens unless the user clicks it or the release is marked mandatory.
        /// </summary>
        private void StartUpdateCheck(bool manual)
        {
            manualUpdateCheck = manual;
            if (manual)
            {
                ShowUpdateText("Checking...", false);
            }
            AutoUpdater.Start(UpdateXmlUrl);
        }

        private void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AutoUpdater_CheckForUpdateEvent(args)));
                return;
            }

            bool manual = manualUpdateCheck;
            manualUpdateCheck = false;

            if (args == null || args.Error != null)
            {
                Console.WriteLine($"Update check failed: {(args == null ? "no response" : args.Error.Message)}");
                if (manual)
                {
                    ShowUpdateText("Check failed", false);
                }
                return;
            }

            if (!args.IsUpdateAvailable)
            {
                availableUpdate = null;
                if (manual)
                {
                    ShowUpdateText("Up to date", false);
                }
                else
                {
                    updateLinkLabel.Visible = false;
                }
                return;
            }

            availableUpdate = args;
            string version = args.CurrentVersion.ToString();
            Version parsed;
            if (Version.TryParse(version, out parsed))
            {
                version = parsed.ToString(3);
            }
            ShowUpdateText($"v{version} available", true);
            toolTip.SetToolTip(updateLinkLabel, $"SluMAN {version} is available. Click to see what's new and update.");

            if (args.Mandatory != null && args.Mandatory.Value)
            {
                AutoUpdater.ShowUpdateForm(args);
            }
        }

        private void ShowUpdateText(string text, bool isLink)
        {
            updateLinkLabel.Text = text;
            updateLinkLabel.LinkArea = isLink ? new LinkArea(0, text.Length) : new LinkArea(0, 0);
            updateLinkLabel.Visible = true;
            if (!isLink)
            {
                toolTip.SetToolTip(updateLinkLabel, null);
            }
        }

        private void updateLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (availableUpdate != null)
            {
                AutoUpdater.ShowUpdateForm(availableUpdate);
            }
        }

        public static string ip;
        public static int pid;
        public static string game;
        public static string gameName;

        private void AttachPS3Form_Load(object sender, EventArgs e)
        {

        }

        private void AttachGameEvent(bool speedrunMode)
        {
            if (rpcs3CheckBox.Checked)
            {
                func.api = new RPCS3("FUCK");
                Attach(func.api, speedrunMode);
                return;
            }

            ip = IPTextBox.Text;
            func.ChangeFileLines("config.txt", Convert.ToString(ip), "ip");

            func.api = this.useOldAPI ? (IPS3API)new WebMAN(ip) : (IPS3API)new Ratchetron(ip);

            if (!this.useOldAPI)
            {
                if (!func.PrepareRatchetron(ip))
                {
                    return;
                }
            }

            Attach(func.api, speedrunMode);
        }

        private void attachButton_Click(object sender, EventArgs e)
        {
            AttachGameEvent(false);
        }

        private void Attach(IPS3API api, Boolean speedrunMode = false)
        {
            if (!api.Connect())
            {
                if (api is RPCS3)
                {
                    MessageBox.Show("Couldn't connect to RPCS3. Start the game in RPCS3, then try again.", "Couldn't connect");
                }
                else
                {
                    MessageBox.Show($"Couldn't connect to the PS3 at {ip}. Check the IP address and that webMAN MOD is running.", "Couldn't connect");
                }
                return;
            }

            try
            {
                game = func.current_game(ip);
                pid = func.current_pid(ip);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                string source = api is RPCS3 ? "RPCS3" : $"the PS3 at {ip}. Check that webMAN MOD is running";
                MessageBox.Show($"Couldn't read the running game from {source}.", "Couldn't connect");
                return;
            }

            if (pid == 0)
            {
                MessageBox.Show("Start the game before attaching SluMAN.", "Game is not running");
                return;
            }

            if (game == "NPEA00343") // Sly 3 (PAL, Digital)
            {
                if (speedrunMode)
                {
                    Hide();
                    func.api.Notify($"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected (Speedrun Mode)");
                    SLY3Speedrun sly3 = new SLY3Speedrun(new sly3(func.api));
                    gameName = "SLY 3 (PAL, PSN)";
                    sly3.ShowDialog();
                }
                else
                {
                    Hide();
                    func.api.Notify($"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected (Practice Mode)");
                    SLY3Form sly3 = new SLY3Form(new sly3(func.api));
                    gameName = "SLY 3 (PAL, PSN)";
                    sly3.ShowDialog();
                }
            }
            else if (game == "NPUA80663")
            {
                Hide();
                func.api.Notify($"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected (Speedrun Mode)");
                Sly1Speedrun sly1 = new Sly1Speedrun(new sly1(func.api));
                gameName = "SLY 1 (NTSC, PSN)";
                sly1.ShowDialog();
            }
            else if (sly2.SupportsGameId(game))
            {
                if (speedrunMode)
                {
                    Hide();
                    func.api.Notify($"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected (Speedrun Mode)");
                    SLY2Speedrun sly2Speedrun = new SLY2Speedrun(new sly2(func.api, game), game);
                    gameName = sly2.GetDisplayName(game);
                    sly2Speedrun.ShowDialog();
                }
                else
                {
                    Hide();
                    func.api.Notify($"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected (Practice Mode)");
                    Sly2Practice sly2Practice = new Sly2Practice(new sly2(func.api, game), game);
                    gameName = sly2.GetDisplayName(game);
                    sly2Practice.ShowDialog();
                }
            }
            else
            {
                if (game.Length > 0)
                {
                    MessageBox.Show($"{game} isn't supported yet. You can still apply mods if you have any.");

                    if ((Application.OpenForms["ModLoaderForm"] as ModLoaderForm) != null)
                    {
                        modLoaderForm.Activate();
                    }
                    else
                    {
                        // Set first: these windows show the app-wide messages when no game form is open.
                        notSupported = true;
                        modLoaderForm = new ModLoaderForm();
                        modLoaderForm.Show();
                        memoryForm = new MemoryForm();
                        memoryForm.Show();
                    }
                }
                else
                {
                    MessageBox.Show("Game isn't running or isn't supported yet.");
                }
            }
        }

        private void currentVerLabel_Click(object sender, EventArgs e)
        {
            StartUpdateCheck(true);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.useOldAPI = ((CheckBox)sender).Checked;
        }

        private void IPTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                attachButton_Click(IPTextBox, e);
            }
        }

        private void attachPS3SpeedrunModeButton_Click(object sender, EventArgs e)
        {

            AttachGameEvent(true);
        }

        private void AttachPS3Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (func.api != null)
            {
                func.api.Disconnect();
            }
        }
    }
}
