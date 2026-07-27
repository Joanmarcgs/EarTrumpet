using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace BurxatMixerLauncher
{
    internal static class Program
    {
        private const string BurxatMixerArg = "--burxat-mixer";

        [STAThread]
        private static void Main()
        {
            var launcherDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var earTrumpetPath = Path.Combine(launcherDirectory, "EarTrumpet.exe");

            if (!File.Exists(earTrumpetPath))
            {
                MessageBox.Show(
                    "EarTrumpet.exe wasn't found next to this launcher. Keep BurxatMixer.exe in the same folder as EarTrumpet.exe.",
                    "Burxat's Mixer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Whether EarTrumpet is already running or not, it handles this flag itself: a fresh
            // instance opens the mixer once startup finishes, and a second launch forwards the
            // request to the already-running instance over a named pipe before exiting.
            Process.Start(new ProcessStartInfo(earTrumpetPath, BurxatMixerArg)
            {
                WorkingDirectory = launcherDirectory,
            });
        }
    }
}
