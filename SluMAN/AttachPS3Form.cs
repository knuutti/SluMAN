using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using SluMAN.Memory;
using System.Diagnostics;
using AutoUpdaterDotNET;

namespace SluMAN
{
    public partial class AttachPS3Form : Form
    {
        bool useOldAPI = false;

        public static ConsoleForm console;

        public static ScriptingForm scripting;

        static ModLoaderForm modLoaderForm;
        static MemoryForm memoryForm;
        public static bool notSupported = false;

        public AttachPS3Form()
        {
            InitializeComponent();

            ConsoleForm.RedirectOutput();

            console = new ConsoleForm();
            scripting = new ScriptingForm();

            currentVerLabel.Text = "SluMAN " + func.VersionText;

            AutoUpdater.RunUpdateAsAdmin = false;
            // Handling this event stops AutoUpdater from opening its dialog by itself.
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            // config.txt always exists: UserData.Prepare creates it at startup.
            ip = func.GetConfigData("config.txt", "ip");
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

            // The OBS input display page; a busy port is only logged, the Input Display menu shows it.
            ObsPadServer.StartFromConfig();

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
            if (func.IsDevBuild)
            {
                // Local builds have no real version to compare against update.xml.
                if (manual)
                {
                    ShowUpdateText("Dev build, no update check", false);
                }
                return;
            }

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

        // Bumped for every attach and on cancel. The background connect checks it, so the result of
        // a cancelled attempt is thrown away when it finally arrives.
        private int attachAttempt = 0;
        private bool attaching = false;
        private Button cancelButton = null;
        private string cancelButtonText;

        /// <summary>What the background connect found, or why it failed.</summary>
        private class AttachResult
        {
            public string error;
            public string errorTitle = "Couldn't connect";
            public string game;
            public int pid;
        }

        private void AttachGameEvent(bool speedrunMode, Button clickedButton)
        {
            if (attaching)
            {
                return;
            }

            // Close the previous connection first. The server only tells one client when the game
            // closes or starts, so a leftover connection steals reconnects from the new game form.
            if (func.api != null)
            {
                try { func.api.Disconnect(); } catch { }
            }

            IPS3API api;
            if (rpcs3CheckBox.Checked)
            {
                api = new RPCS3("FUCK");
            }
            else
            {
                ip = IPTextBox.Text.Trim();
                IPAddress parsedIp;
                if (!IPAddress.TryParse(ip, out parsedIp))
                {
                    MessageBox.Show(this, $"\"{ip}\" isn't an IP address. Enter the PS3's IP address, for example 192.168.1.10. webMAN MOD shows it on the PS3's home screen.", "Check the IP address");
                    IPTextBox.Focus();
                    return;
                }
                IPTextBox.Text = ip;
                func.ChangeFileLines("config.txt", Convert.ToString(ip), "ip");

                api = this.useOldAPI ? (IPS3API)new WebMAN(ip) : (IPS3API)new Ratchetron(ip);

                Ratchetron ratchetron = api as Ratchetron;
                if (ratchetron != null)
                {
                    // Offers a firewall rule when live data from the PS3 never arrives.
                    ratchetron.DataChannelSilent += () => FirewallHelper.OnDataChannelSilent(ratchetron);
                }
            }

            // Connecting can take several seconds when the PS3 doesn't answer, so it runs in the
            // background and the form shows how far it got.
            int attempt = ++attachAttempt;
            SetAttaching(true, clickedButton);
            Thread worker = new Thread(() =>
            {
                AttachResult result = ConnectToGame(api, attempt);
                RunOnForm(() => FinishAttach(api, attempt, speedrunMode, result));
            });
            worker.IsBackground = true;
            worker.Name = "Attach";
            worker.Start();
        }

        /// <summary>
        /// Connects and reads the running game. Runs on a background thread.
        /// </summary>
        private AttachResult ConnectToGame(IPS3API api, int attempt)
        {
            AttachResult result = new AttachResult();
            bool isRpcs3 = api is RPCS3;

            try
            {
                if (api is Ratchetron)
                {
                    ShowAttachProgress(attempt, "Reaching webMAN MOD...");
                    result.error = func.PrepareRatchetron(ip);
                    if (result.error != null)
                    {
                        return result;
                    }
                }

                ShowAttachProgress(attempt, isRpcs3 ? "Connecting to RPCS3..." : "Connecting to the PS3...");
                if (!api.Connect())
                {
                    if (isRpcs3)
                    {
                        result.error = "Couldn't connect to RPCS3. Start the game in RPCS3, then try again.";
                    }
                    else
                    {
                        result.error = $"Couldn't connect to the PS3 at {ip}.\n\nCheck that the IP address is right, the PS3 is connected to the same network and webMAN MOD is running.";
                    }
                    return result;
                }

                ShowAttachProgress(attempt, "Reading the running game...");
                result.game = api.getGameTitleID();
                result.pid = api.getCurrentPID();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                string source = isRpcs3 ? "RPCS3" : $"the PS3 at {ip}. Check that webMAN MOD is running";
                result.error = $"Couldn't read the running game from {source}.";
                return result;
            }

            if (result.pid == 0)
            {
                result.error = "Start the game before attaching SluMAN.";
                result.errorTitle = "Game is not running";
            }
            return result;
        }

        /// <summary>
        /// Back on the UI thread once the background connect is done.
        /// </summary>
        private void FinishAttach(IPS3API api, int attempt, bool speedrunMode, AttachResult result)
        {
            if (attempt != attachAttempt)
            {
                // Cancelled, or the form closed. Don't leave the connection open.
                try { api.Disconnect(); } catch { }
                return;
            }

            SetAttaching(false, null);

            if (result.error != null)
            {
                try { api.Disconnect(); } catch { }
                SetStatus(result.errorTitle, true);
                MessageBox.Show(this, result.error, result.errorTitle);
                return;
            }

            SetStatus("", false);
            func.api = api;
            game = result.game;
            pid = result.pid;
            Attach(speedrunMode);
        }

        private void CancelAttach()
        {
            attachAttempt++;
            SetAttaching(false, null);
            SetStatus("Cancelled", false);
        }

        private void SetAttaching(bool value, Button clickedButton)
        {
            attaching = value;
            IPTextBox.Enabled = !value;
            rpcs3CheckBox.Enabled = !value;

            if (value)
            {
                // The clicked button becomes the cancel button; the other one is off meanwhile.
                cancelButton = clickedButton;
                cancelButtonText = clickedButton.Text;
                clickedButton.Text = "Cancel";
                attachButton.Enabled = clickedButton == attachButton;
                attachRestrictedButton.Enabled = clickedButton == attachRestrictedButton;
                clickedButton.Focus();
            }
            else
            {
                if (cancelButton != null)
                {
                    cancelButton.Text = cancelButtonText;
                    cancelButton = null;
                }
                attachButton.Enabled = true;
                attachRestrictedButton.Enabled = true;
            }
        }

        private void SetStatus(string text, bool isError)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
        }

        /// <summary>Shows a connect step on the status line, unless the attempt was cancelled.</summary>
        private void ShowAttachProgress(int attempt, string text)
        {
            RunOnForm(() =>
            {
                if (attempt == attachAttempt)
                {
                    SetStatus(text, false);
                }
            });
        }

        /// <summary>Runs the action on the UI thread. Does nothing once the form is gone.</summary>
        private void RunOnForm(Action action)
        {
            try
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(action);
                }
            }
            catch (InvalidOperationException)
            {
                // The form closed while connecting.
            }
        }

        private void attachButton_Click(object sender, EventArgs e)
        {
            if (attaching)
            {
                CancelAttach();
                return;
            }
            AttachGameEvent(false, attachButton);
        }

        /// <summary>
        /// True while a game form is closing to switch to another game. The game forms check it so
        /// closing doesn't exit the app.
        /// </summary>
        public static bool switchPending = false;
        private static bool pendingSpeedrunMode = false;

        public static bool IsSupportedTitle(string titleId)
        {
            return Sly1Addresses.IsSupportedGameId(titleId)
                || Sly2Addresses.IsSupportedGameId(titleId)
                || Sly3Addresses.IsSupportedGameId(titleId);
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

        private void Attach(Boolean speedrunMode)
        {
            ShowGame(speedrunMode);

            // The game form's dialog has returned. If it closed to switch games, attach to the new
            // one. Show this form first so the progress and a failed attach are on screen.
            if (switchPending)
            {
                switchPending = false;
                bool mode = pendingSpeedrunMode;
                BeginInvoke(new Action(() =>
                {
                    Show();
                    AttachGameEvent(mode, mode ? attachRestrictedButton : attachButton);
                }));
            }
        }

        /// <summary>
        /// Opens the form for the attached game. Blocks until it closes.
        /// </summary>
        private void ShowGame(Boolean speedrunMode)
        {
            if (Sly3Addresses.IsSupportedGameId(game))
            {
                if (speedrunMode)
                {
                    Hide();
                    func.api.Notify($"SluMAN {func.VersionText} connected (Speedrun Mode)");
                    Sly3Speedrun sly3Speedrun = new Sly3Speedrun(new sly3(func.api, game));
                    gameName = sly3.addr.DisplayName;
                    sly3Speedrun.ShowDialog();
                }
                else
                {
                    Hide();
                    func.api.Notify($"SluMAN {func.VersionText} connected (Practice Mode)");
                    Sly3Practice sly3Practice = new Sly3Practice(new sly3(func.api, game));
                    gameName = sly3.addr.DisplayName;
                    sly3Practice.ShowDialog();
                }
            }
            else if (Sly1Addresses.IsSupportedGameId(game))
            {
                Hide();
                func.api.Notify($"SluMAN {func.VersionText} connected (Speedrun Mode)");
                Sly1Speedrun sly1Speedrun = new Sly1Speedrun(new sly1(func.api, game));
                gameName = sly1.addr.DisplayName;
                sly1Speedrun.ShowDialog();
            }
            else if (Sly2Addresses.IsSupportedGameId(game))
            {
                if (speedrunMode)
                {
                    Hide();
                    func.api.Notify($"SluMAN {func.VersionText} connected (Speedrun Mode)");
                    Sly2Speedrun sly2Speedrun = new Sly2Speedrun(new sly2(func.api, game));
                    gameName = sly2.addr.DisplayName;
                    sly2Speedrun.ShowDialog();
                }
                else
                {
                    Hide();
                    func.api.Notify($"SluMAN {func.VersionText} connected (Practice Mode)");
                    Sly2Practice sly2Practice = new Sly2Practice(new sly2(func.api, game));
                    gameName = sly2.addr.DisplayName;
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
                // Swallowed so Windows doesn't beep.
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!attaching)
                {
                    AttachGameEvent(false, attachButton);
                }
            }
        }

        private void attachPS3SpeedrunModeButton_Click(object sender, EventArgs e)
        {
            if (attaching)
            {
                CancelAttach();
                return;
            }
            AttachGameEvent(true, attachRestrictedButton);
        }

        private void AttachPS3Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // A connect still running in the background is dropped when it finishes.
            attachAttempt++;
            if (func.api != null)
            {
                func.api.Disconnect();
            }
        }
    }
}
