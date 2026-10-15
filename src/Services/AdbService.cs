using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidToolkitPro
{
    public class Dev
    {
        public string Serial { get; private set; }
        public string State { get; private set; }
        public string Desc { get; private set; }

        public Dev(string serial, string state, string desc)
        {
            Serial = serial;
            State = state;
            Desc = desc;
        }
    }

    public static class Adb
    {
        public static string Serial = "";
        public static string LastDevicesRaw = "";

        static string Find(string fallback, params string[] rel)
        {
            foreach (var r in rel)
            {
                var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, r);
                if (File.Exists(p)) return p;
            }
            return fallback;
        }

        public static string AdbPath
        {
            get { return Find("adb", "Tools\\adb.exe", "platform-tools\\adb.exe", "adb.exe"); }
        }

        public static string ScrcpyPath
        {
            get { return Find("scrcpy", "Tools\\Scrcpy\\scrcpy.exe", "scrcpy\\scrcpy.exe", "scrcpy.exe"); }
        }

        public static async Task<string> RunAsync(string fileName, string args, int timeoutMs = 30000)
        {
            var psi = new ProcessStartInfo(fileName, args);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            try
            {
                using (var p = new Process())
                {
                    p.StartInfo = psi;
                    p.Start();
                    var outTask = p.StandardOutput.ReadToEndAsync();
                    var errTask = p.StandardError.ReadToEndAsync();

                    bool exited = await Task.Run(() => p.WaitForExit(timeoutMs));
                    if (!exited)
                    {
                        try { p.Kill(); } catch { }
                        return "[Tiempo agotado]";
                    }

                    var o = await outTask;
                    var e = await errTask;
                    var all = o + (string.IsNullOrWhiteSpace(e) ? "" : "\n" + e);
                    return all.Replace("\r\n", "\n").Trim();
                }
            }
            catch (Exception ex)
            {
                return "[Error] " + ex.Message + "  (¿está adb.exe en la carpeta 'Tools'?)";
            }
        }

        public static Task<string> AdbAsync(string args, int timeoutMs = 30000)
        {
            var pre = string.IsNullOrEmpty(Serial) ? "" : "-s " + Serial + " ";
            return RunAsync(AdbPath, pre + args, timeoutMs);
        }

        public static Task<string> ShellAsync(string cmd, int timeoutMs = 30000)
        {
            return AdbAsync("shell \"" + cmd.Replace("\"", "\\\"") + "\"", timeoutMs);
        }

        public static async Task<List<Dev>> DevicesAsync()
        {
            LastDevicesRaw = await RunAsync(AdbPath, "devices -l", 20000);
            var list = new List<Dev>();
            foreach (var raw in LastDevicesRaw.Split('\n'))
            {
                var l = raw.Trim();
                if (l.Length == 0 || l.StartsWith("*") || l.StartsWith("[") || l.StartsWith("List of")) continue;
                var parts = Regex.Split(l, @"\s+");
                if (parts.Length < 2) continue;
                list.Add(new Dev(parts[0], parts[1], string.Join(" ", parts.Skip(2).ToArray())));
            }
            return list;
        }
    }
}
