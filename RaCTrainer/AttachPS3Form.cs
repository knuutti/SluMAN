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

#if !DEBUG
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.Start("https://raw.githubusercontent.com/knuutti/SluMAN/master/update.xml");
#endif

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

        /// <summary>
        /// True while a game form is closing to switch to another game. The game forms check it so
        /// closing doesn't exit the app.
        /// </summary>
        public static bool switchPending = false;
        private static bool pendingSpeedrunMode = false;

        public static bool IsSupportedTitle(string titleId)
        {
            return titleId == "NPEA00343" || titleId == "NPUA80663" || sly2.SupportsGameId(titleId);
        }

        /// <summary>
        /// Called by a game form just before it closes to switch games: once its window is gone,
        /// attach again in the given mode.
        /// </summary>
        public static void RequestAttachAfterClose(bool speedrunMode)
        {
            switchPending = true;
            pendingSpeedrunMode = speedrunMode;
        }

        private void Attach(IPS3API api, Boolean speedrunMode = false)
        {
            AttachAndShowGame(api, speedrunMode);

            // The game form's dialog has returned. If it closed to switch games, attach to the new
            // one. Show this form first so a failed attach leaves something on screen.
            if (switchPending)
            {
                switchPending = false;
                bool mode = pendingSpeedrunMode;
                BeginInvoke(new Action(() =>
                {
                    Show();
                    AttachGameEvent(mode);
                }));
            }
        }

        private void AttachAndShowGame(IPS3API api, Boolean speedrunMode)
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
