using System;
using System.IO;
using System.Windows.Forms;
using LMS.Data;
using LMS.Forms;
using LMS.Services;
using LMS.UI.Controls;

namespace LMS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Global Exception Handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => HandleException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex) HandleException(ex);
            };

            ApplicationConfiguration.Initialize();

            try
            {
                // Initialize SQLite Database and Default Settings
                AppDbContext.InitializeDatabase();
                SettingService.ReloadCache();

                // Launch Login Form
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void HandleException(Exception ex)
        {
            try
            {
                AuditService.Log("Unhandled Exception", "System", null, ex.ToString());

                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandlordManagementSystem", "Logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "error_log.txt");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
            }
            catch { }

            ModernMessageBox.ShowError(
                $"An unexpected application error occurred:\n\n{ex.Message}\n\nThe details have been safely logged. If this problem persists, please contact support.",
                "Application Error"
            );
        }
    }
}
