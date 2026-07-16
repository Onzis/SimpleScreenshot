using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Screenshoter
{
    // Автозапуск через HKCU\...\Run (не требует прав администратора).
    public static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "SnapFlow";

        private static string ExePath =>
            Process.GetCurrentProcess().MainModule?.FileName
            ?? Environment.ProcessPath ?? "";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                var val = key?.GetValue(ValueName) as string;
                return !string.IsNullOrEmpty(val);
            }
            catch { return false; }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                                ?? Registry.CurrentUser.CreateSubKey(RunKey);
                if (key == null) return;

                if (enabled)
                    key.SetValue(ValueName, $"\"{ExePath}\"");
                else
                    key.DeleteValue(ValueName, false);
            }
            catch { /* игнорируем ошибки реестра */ }
        }
    }
}
