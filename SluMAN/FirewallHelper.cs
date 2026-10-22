using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Lets live data from the PS3 through Windows Firewall. The PS3 sends subscription updates
    /// (input display, combos, autosplitter, memory watches) to SluMAN over UDP, and the firewall
    /// drops them silently when SluMAN isn't allowed, while everything sent over TCP still works.
    /// </summary>
    public static class FirewallHelper
    {
        private const string RuleName = "SluMAN";
        private const string DisableWarningKey = "DisableFirewallWarning";

        // Asked at most once per session; declining also persists the choice in config.txt.
        private static bool offeredThisSession = false;
        private static int offerQueued = 0;

        /// <summary>
        /// Called when the PS3 is connected but no live data arrives. Asks once per session
        /// whether to add a firewall rule. Safe to call from any thread.
        /// </summary>
        public static void OnDataChannelSilent(Ratchetron api)
        {
            if (offeredThisSession || WarningDisabled())
            {
                return;
            }

            // A reconnect can raise the event again before the UI thread handles the first one.
            // Queue only one dialog, otherwise each queued callback can show its own prompt.
            if (Interlocked.Exchange(ref offerQueued, 1) != 0)
            {
                return;
            }

            Form owner = Program.AttachPS3Form;
            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated)
            {
                Interlocked.Exchange(ref offerQueued, 0);
                return;
            }

            try
            {
                owner.BeginInvoke(new Action(() => Offer(api)));
            }
            catch
            {
                // Closing.
                Interlocked.Exchange(ref offerQueued, 0);
            }
        }

        private static void Offer(Ratchetron api)
        {
            if (offeredThisSession || WarningDisabled())
            {
                return;
            }
            offeredThisSession = true;

            DialogResult answer = MessageBox.Show(
                "SluMAN is connected to the PS3, but no live data is arriving, so the input display, combos, " +
                "autosplitter and memory watches won't update.\n\n" +
                "Windows Firewall is probably blocking it. This happens when its prompt was cancelled the first time SluMAN ran.\n\n" +
                "Add a firewall rule that allows SluMAN now? Windows will ask for administrator permission.\n\n" +
                "Choose No to disable this warning in the future.",
                "Live data blocked",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                func.ChangeFileLines("config.txt", "true", DisableWarningKey);
                func.Status("Live data warning disabled. Remove \"DisableFirewallWarning\" from config.txt to enable it again.", true);
                return;
            }

            string error;
            if (!AllowSluMAN(out error))
            {
                func.Status(error, true);
                return;
            }

            func.Status("Added a firewall rule for SluMAN. Checking for live data...");
            ConfirmDataArrives(api);
        }

        private static bool WarningDisabled()
        {
            return string.Equals(func.GetConfigData("config.txt", DisableWarningKey), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Replaces SluMAN.exe's inbound firewall rules with one that allows it. Windows adds block
        /// rules when its prompt is cancelled, and a block rule wins over an allow rule, so those
        /// are removed first. Needs administrator permission; Windows asks for it.
        /// </summary>
        public static bool AllowSluMAN(out string error)
        {
            error = "";
            string exePath = Application.ExecutablePath.Replace("'", "''");

            string script =
                "$ErrorActionPreference = 'Stop'\n" +
                "try {\n" +
                $"  $program = '{exePath}'\n" +
                "  Get-NetFirewallApplicationFilter -Program $program -ErrorAction SilentlyContinue |\n" +
                "    Get-NetFirewallRule | Where-Object { $_.Direction -eq 'Inbound' } | Remove-NetFirewallRule\n" +
                $"  New-NetFirewallRule -DisplayName '{RuleName}' -Direction Inbound -Action Allow -Program $program -Profile Any | Out-Null\n" +
                "  exit 0\n" +
                "} catch { exit 1 }\n";

            // EncodedCommand is UTF-16 base64, so no path can break the quoting.
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            ProcessStartInfo startInfo = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded);
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (!process.WaitForExit(30000))
                    {
                        error = "Adding the firewall rule took too long. Try again, or allow SluMAN in Windows Defender Firewall settings.";
                        return false;
                    }
                    if (process.ExitCode != 0)
                    {
                        error = "Windows couldn't add the firewall rule. Allow SluMAN in Windows Defender Firewall settings instead.";
                        return false;
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // ERROR_CANCELLED: the administrator prompt was declined.
                error = "The firewall rule wasn't added because administrator permission was declined.";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Couldn't add the firewall rule: {ex.Message}";
                return false;
            }

            Console.WriteLine($"Firewall: inbound rule \"{RuleName}\" added for {Application.ExecutablePath}");
            return true;
        }

        /// <summary>Reports whether live data starts arriving after the rule was added.</summary>
        private static void ConfirmDataArrives(Ratchetron api)
        {
            long before = api.ReceivedDataPackets;
            Thread thread = new Thread(() =>
            {
                Thread.Sleep(4000);
                if (api.ReceivedDataPackets > before)
                {
                    func.Status("Live data from the PS3 is arriving now.");
                }
                else
                {
                    func.Status("Still no live data. Another firewall or antivirus may be blocking it, or the PC and PS3 are on different networks.", true);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
