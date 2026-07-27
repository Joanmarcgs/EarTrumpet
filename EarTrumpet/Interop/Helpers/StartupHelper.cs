using Microsoft.Win32;
using System.Reflection;

namespace EarTrumpet.Interop.Helpers
{
    // Toggles a plain HKCU Run-key entry rather than the Store app StartupTask API, since this
    // build has no package identity to register one under.
    static class StartupHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "BurxatMixerForEarTrumpet";

        public static bool IsEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    return key?.GetValue(RunValueName) != null;
                }
            }
            set
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (value)
                    {
                        key.SetValue(RunValueName, $"\"{Assembly.GetEntryAssembly().Location}\"");
                    }
                    else
                    {
                        key.DeleteValue(RunValueName, false);
                    }
                }
            }
        }
    }
}
