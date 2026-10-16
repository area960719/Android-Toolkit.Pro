using System;
using System.Diagnostics;
using System.IO;

namespace AndroidToolkitPro
{
    public static class AdbHelper
    {
        public static string AdbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "PlatformTools", "adb.exe");

        // Método para ejecutar comandos que terminan rápido (install, uninstall, push, pull)
        public static string RunCommand(string arguments)
        {
            if (!File.Exists(AdbPath)) return "Error: No se encontró adb.exe en " + AdbPath;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = AdbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return string.IsNullOrEmpty(error) ? output : output + "\nERROR: " + error;
                }
            }
            catch (Exception ex)
            {
                return "Error crítico: " + ex.Message;
            }
        }
    }
}
