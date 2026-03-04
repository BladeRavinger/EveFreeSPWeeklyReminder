using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Timers;
using Microsoft.Win32;

namespace EveFreeSPWeeklyReminder
{
    static class Program
    {

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private System.Timers.Timer checkTimer;
        private bool waitingForTargetApp = false;

        private readonly string TargetProcessName = "exefile";

        private readonly string SaveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveWeeklyReminder_LastRun.txt");

        public TrayApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true,
                Text = "EVE Free SP Reminder"
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.CheckOnClick = true;
            startupItem.Checked = IsRunAtStartup();
            startupItem.CheckedChanged += (s, e) => SetRunAtStartup(startupItem.Checked);
            menu.Items.Add(startupItem);

            menu.Items.Add("Uninstall / Remove", null, (s, e) => OpenUninstallSettings());

            menu.Items.Add("-");

            menu.Items.Add("Force Trigger (Test)", null, (s, e) => ForceTest());

            menu.Items.Add("-");

            menu.Items.Add("Hide Tray Icon", null, (s, e) => HideIcon());
            menu.Items.Add("Stop and Exit", null, (s, e) => ExitApp());

            trayIcon.ContextMenuStrip = menu;

            checkTimer = new System.Timers.Timer(60000);
            checkTimer.Elapsed += (s, e) => CheckLogic();
            checkTimer.Start();

            CheckLogic();
        }

        private void ForceTest()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            ShowNotification("Test Mode Activated", "Bypassing the time check. Now waiting for EVE Online to open...");
            waitingForTargetApp = true;
        }

        private void CheckLogic()
        {
            DateTime nowUtc = DateTime.UtcNow;

            if (waitingForTargetApp)
            {
                Process[] processes = Process.GetProcessesByName(TargetProcessName);
                if (processes.Length > 0)
                {
                    ShowNotification("Free SP Reminder", "EVE Online is running. Don't forget to claim your weekly free Skill Points from the New Eden Store!");

                    File.WriteAllText(SaveFilePath, nowUtc.ToString("O"));

                    waitingForTargetApp = false;
                }

                return;
            }

            if (nowUtc.DayOfWeek == DayOfWeek.Tuesday && nowUtc.Hour >= 13)
            {
                if (File.Exists(SaveFilePath))
                {
                    string savedDateStr = File.ReadAllText(SaveFilePath);
                    if (DateTime.TryParse(savedDateStr, out DateTime lastRunDate))
                    {
                        TimeSpan timeSinceLastRun = nowUtc - lastRunDate;
                        if (timeSinceLastRun.TotalDays < 6)
                        {
                            return;
                        }
                    }
                }

                ShowNotification("EVE Online Weekly Reset", "Your weekly free Skill Points are now available. Launch EVE Online to claim them.");

                waitingForTargetApp = true;
            }
        }

        private void ShowNotification(string title, string text)
        {
            trayIcon.ShowBalloonTip(5000, title, text, ToolTipIcon.Info);
        }

        private void ExitApp()
        {
            checkTimer.Stop();
            checkTimer.Dispose();

            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }

        private void HideIcon()
        {
            trayIcon.Visible = false;
        }

        private void OpenUninstallSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
            }
            catch (Exception)
            {
                ShowNotification("Action Required", "To fully uninstall, uncheck 'Start with Windows' and delete this executable file.");
            }
        }

        private bool IsRunAtStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
            {
                if (key != null)
                {
                    object value = key.GetValue("EveWeeklyReminder");
                    return value != null && value.ToString() == Application.ExecutablePath;
                }
            }
            return false;
        }

        private void SetRunAtStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (enable)
                {
                    key.SetValue("EveWeeklyReminder", Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue("EveWeeklyReminder", false);
                }
            }
        }
    }
}