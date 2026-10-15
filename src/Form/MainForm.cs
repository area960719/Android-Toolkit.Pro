using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AndroidToolkitPro
{
    class MainForm : Form
    {
        readonly ComboBox cmbDev = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
        readonly ListBox nav = new ListBox { Dock = DockStyle.Left, Width = 180, Font = new Font("Segoe UI", 11f), IntegralHeight = false };
        readonly Panel host = new Panel { Dock = DockStyle.Fill };
        readonly TextBox output = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Bottom,
            Height = 260, Font = new Font("Consolas", 9.5f), BackColor = Color.Black, ForeColor = Color.LightGreen
        };

        readonly ListBox lstApps = new ListBox { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, IntegralHeight = false };
        readonly TextBox txtFilter = new TextBox { Width = 200 };
        readonly ComboBox cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        List<string> allPkgs = new List<string>();

        readonly TextBox txtCmd = new TextBox();

        readonly CheckBox cbAwake = new CheckBox { Text = "Mantener despierto", AutoSize = true, Checked = true };
        readonly CheckBox cbOff = new CheckBox { Text = "Apagar pantalla del teléfono", AutoSize = true };
        readonly CheckBox cbNoCtl = new CheckBox { Text = "Solo ver (sin control)", AutoSize = true };
        readonly CheckBox cbRec = new CheckBox { Text = "Grabar video", AutoSize = true };
        readonly NumericUpDown nudSize = new NumericUpDown { Minimum = 0, Maximum = 4000, Value = 1024, Increment = 128, Width = 70 };
        readonly NumericUpDown nudBitrate = new NumericUpDown { Minimum = 1, Maximum = 64, Value = 8, Width = 55 };
        readonly TextBox txtIp = new TextBox { Width = 150 };

        List<Dev> devices = new List<Dev>();
        readonly Dictionary<string, Control> tools = new Dictionary<string, Control>();

        public MainForm()
        {
            Text = "Android-Toolkit.Pro";
            Width = 1150;
            Height = 780;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0) };
            top.Controls.Add(new Label { Text = "Dispositivo:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) });
            top.Controls.Add(cmbDev);
            top.Controls.Add(Btn("Actualizar", RefreshDevicesAsync, false));
            top.Controls.Add(Btn("Guardar salida", SaveOutputAsync, false));

            Controls.Add(host);
            Controls.Add(output);
            Controls.Add(nav);
            Controls.Add(top);

            tools["1. Información"]        = BuildInfo();
            tools["2. Batería"]            = BuildBattery();
            tools["3. Diagnóstico"]        = BuildDiagnostic();
            tools["4. Aplicaciones"]       = BuildApps();
            tools["5. Ajustes y teclas"]   = BuildSettings();
            tools["6. Pantalla (scrcpy)"]  = BuildScreen();
            tools["7. Consola ADB"]        = BuildConsole();
            tools["8. IMEI y datos"]       = BuildIdent();
            tools["9. Pruebas hardware"]   = BuildHardwareTests();
            tools["10. Informe de taller"] = BuildWorkReport();

            foreach (var k in tools.Keys) nav.Items.Add(k);

            nav.SelectedIndexChanged += (s, e) =>
            {
                if (nav.SelectedItem is string)
                {
                    host.Controls.Clear();
                    host.Controls.Add(tools[(string)nav.SelectedItem]);
                }
            };
            cmbDev.SelectedIndexChanged += (s, e) => OnDeviceChanged();

            Load += async (s, e) =>
            {
                nav.SelectedIndex = 0;

                if (!File.Exists(Adb.AdbPath) && !IsOnPath("adb.exe"))
                    Append("AVISO: no encontré adb.exe. Colócalo en:\n  " +
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools") + "\n");
                if (!File.Exists(Adb.ScrcpyPath) && !IsOnPath("scrcpy.exe"))
                    Append("AVISO: no encontré scrcpy.exe. Colócalo en:\n  " +
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Scrcpy") + "\n");

                await RefreshDevicesAsync();
            };
        }

        // ================================================================
        //  Utilidades de interfaz
        // ================================================================
        static bool IsOnPath(string exe)
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar)) return false;
            var paths = pathVar.Split(';');
            foreach (var p in paths)
            {
                try { if (File.Exists(Path.Combine(p.Trim(), exe))) return true; } catch { }
            }
            return false;
        }

        void Show(string text)
        {
            output.Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
            output.SelectionStart = output.TextLength;
            output.ScrollToCaret();
        }

        void Append(string text)
        {
            output.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
        }

        Button Btn(string text, Func<Task> act, bool needDevice = true)
        {
            var b = new Button { Text = text, AutoSize = true, Padding = new Padding(6, 3, 6, 3), Margin = new Padding(4) };
            b.Click += async (s, e) =>
            {
                if (needDevice && string.IsNullOrEmpty(Adb.Serial))
                {
                    MessageBox.Show("Primero conecta y selecciona un dispositivo (botón Actualizar).");
                    return;
                }
                b.Enabled = false;
                try { await act(); }
                catch (Exception ex) { Append("Error: " + ex.Message); }
                finally { b.Enabled = true; }
            };
            return b;
        }

        FlowLayoutPanel Flow(bool fill = false)
        {
            var f = new FlowLayoutPanel { Padding = new Padding(6), WrapContents = true };
            if (fill)
            {
                f.Dock = DockStyle.Fill;
                f.AutoScroll = true;
            }
            else
            {
                f.Dock = DockStyle.Top;
                f.AutoSize = true;
                f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            return f;
        }

        void Section(FlowLayoutPanel f, string title)
        {
            var l = new Label
            {
                Text = title, AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(4, 12, 4, 2)
            };
            f.Controls.Add(l);
            f.SetFlowBreak(l, true);
        }

        static string OutDir()
        {
            var d = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AndroidToolkitPro");
            Directory.CreateDirectory(d);
            return d;
        }

        async Task<string> Sh(string cmd, int t = 30000)
        {
            return (await Adb.ShellAsync(cmd, t)).Trim();
        }

        static Dictionary<string, string> ParseProps(string txt)
        {
            var d = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(txt, @"^\[(.+?)\]: \[(.*?)\]", RegexOptions.Multiline))
                d[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            return d;
        }

        static Dictionary<string, string> ParseKv(string txt)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(txt,
                @"^[ \t]*([A-Za-z][A-Za-z \-_]*?):[ \t]*(\S.*?)[ \t]*$", RegexOptions.Multiline))
                d[m.Groups[1].Value.Trim()] = m.Groups[2].Value;
            return d;
        }

        static string StatusName(int s)
        {
            switch (s)
            {
                case 2: return "Cargando";
                case 3: return "Descargando";
                case 4: return "No cargando";
                case 5: return "Llena";
                default: return "Desconocido";
            }
        }

        static string HealthName(int h)
        {
            switch (h)
            {
                case 2: return "Buena";
                case 3: return "SOBRECALENTADA";
                case 4: return "MUERTA";
                case 5: return "Sobrevoltaje";
                case 6: return "Fallo";
                case 7: return "Fría";
                default: return "Desconocida";
            }
        }

        // ================================================================
        //  Dispositivos
        // ================================================================
        async Task RefreshDevicesAsync()
        {
            devices = await Adb.DevicesAsync();
            cmbDev.Items.Clear();
            foreach (var d in devices)
                cmbDev.Items.Add(d.Serial + "   [" + d.State + "]   " + d.Desc);

            if (devices.Count > 0) cmbDev.SelectedIndex = 0;
            else
            {
                Adb.Serial = "";
                Show("No se detectan dispositivos.\n" +
                     "1) Activa 'Depuración USB' en el teléfono (Opciones de desarrollador).\n" +
                     "2) Conecta el cable y acepta el aviso en la pantalla del teléfono.\n" +
                     "3) Verifica que adb.exe esté en la carpeta 'Tools'.\n\n" + Adb.LastDevicesRaw);
            }
        }

        void OnDeviceChanged()
        {
            var i = cmbDev.SelectedIndex;
            if (i < 0 || i >= devices.Count) { Adb.Serial = ""; return; }
            var d = devices[i];
            Adb.Serial = d.Serial;
            if (d.State != "device")
                Show("El dispositivo está en estado '" + d.State + "'.\n" +
                     "- unauthorized: acepta 'Permitir depuración USB' en el teléfono.\n" +
                     "- offline: reconecta el cable.");
            else
                Show("Dispositivo seleccionado: " + d.Serial +
                     "\nElige una herramienta en el menú de la izquierda.");
        }

        Task SaveOutputAsync()
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "Texto|*.txt",
                FileName = "informe_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt",
                InitialDirectory = OutDir()
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    File.WriteAllText(dlg.FileName, output.Text, Encoding.UTF8);
            }
            return Task.FromResult(false);
        }

        // ================================================================
        //  1. INFORMACIÓN
        // ================================================================
        Control BuildInfo()
        {
            var p = new Panel { Dock = DockStyle.Fill };
            var f = Flow();
            f.Controls.Add(Btn("Leer información del teléfono", InfoAsync));
            p.Controls.Add(f);
            return p;
        }

        async Task InfoAsync()
        {
            Show("Leyendo información...");
            var props = ParseProps(await Adb.ShellAsync("getprop"));
            Func<string, string> G = k =>
            {
                string v;
                return props.TryGetValue(k, out v) && v.Length > 0 ? v : "—";
            };

            var mem = await Sh("cat /proc/meminfo");
            Func<string, string> Mem = key =>
            {
                var m = Regex.Match(mem, key + @":\s+(\d+) kB");
                return m.Success ? (long.Parse(m.Groups[1].Value) / 1024) + " MB" : "—";
            };

            var size = (await Sh("wm size")).Replace("\n", " | ");
            var dens = (await Sh("wm density")).Replace("\n", " | ");
            var up = await Sh("uptime");
            var df = await Sh("df -h /data");

            var sb = new StringBuilder();
            sb.AppendLine("=== INFORMACIÓN DEL DISPOSITIVO ===");
            sb.AppendLine("Marca / Modelo   : " + G("ro.product.brand") + " " + G("ro.product.model"));
            sb.AppendLine("Nombre en código : " + G("ro.product.device"));
            sb.AppendLine("Android          : " + G("ro.build.version.release") + " (SDK " + G("ro.build.version.sdk") + ")");
            sb.AppendLine("Parche seguridad : " + G("ro.build.version.security_patch"));
            sb.AppendLine("Compilación      : " + G("ro.build.display.id"));
            sb.AppendLine("Hardware / ABI   : " + G("ro.hardware") + " / " + G("ro.product.cpu.abi"));
            sb.AppendLine("Serie            : " + G("ro.serialno"));
            sb.AppendLine("Operador         : " + G("gsm.operator.alpha"));
            sb.AppendLine("Estado SIM       : " + G("gsm.sim.state"));
            sb.AppendLine("Bootloader bloq. : " + G("ro.boot.flash.locked") + "  (1 = bloqueado)");
            sb.AppendLine("Pantalla         : " + size);
            sb.AppendLine("Densidad         : " + dens);
            sb.AppendLine("RAM total / libre: " + Mem("MemTotal") + " / " + Mem("MemAvailable"));
            sb.AppendLine("Tiempo encendido : " + up);
            sb.AppendLine();
            sb.AppendLine("--- Almacenamiento (/data) ---");
            sb.AppendLine(df);
            Show(sb.ToString());
        }

        // ================================================================
        //  2. BATERÍA
        // ================================================================
        Control BuildBattery()
        {
            var p = new Panel { Dock = DockStyle.Fill };
            var f = Flow();
            f.Controls.Add(Btn("Probar batería", BatteryAsync));
            f.Controls.Add(Btn("Monitor 10 lecturas (20 s)", BatteryMonitorAsync));
            p.Controls.Add(f);
            return p;
        }

        async Task BatteryAsync()
        {
            Show("Leyendo batería...");
            var kv = ParseKv(await Adb.ShellAsync("dumpsys battery"));
            Func<string, int> I = k =>
            {
                string v; int n;
                return kv.TryGetValue(k, out v) && int.TryParse(v, out n) ? n : -1;
            };

            int level = I("level"), health = I("health"), status = I("status");
            int tRaw = I("temperature"), voltage = I("voltage"), counter = I("Charge counter");
            double temp = tRaw / 10.0;

            var stats = await Adb.ShellAsync("dumpsys batterystats | grep -i -m3 capacity", 45000);
            double design = 0;
            var cm = Regex.Match(stats, @"Capacity:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            if (cm.Success)
                double.TryParse(cm.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out design);

            double est = 0;
            if (counter > 0 && level >= 20) est = counter / 1000.0 / (level / 100.0);

            var cyc = await Sh("cat /sys/class/power_supply/battery/cycle_count");

            var sb = new StringBuilder();
            sb.AppendLine("=== PRUEBA DE BATERÍA ===");
            sb.AppendLine("Nivel            : " + level + "%");
            sb.AppendLine("Estado           : " + StatusName(status));
            sb.AppendLine("Salud reportada  : " + HealthName(health));
            sb.AppendLine("Temperatura      : " + (tRaw == -1 ? "—" : temp.ToString("0.0") + " °C"));
            sb.AppendLine("Voltaje          : " + voltage + " mV");

            string tech;
            sb.AppendLine("Tecnología       : " + (kv.TryGetValue("technology", out tech) ? tech : "—"));

            string usb, ac;
            kv.TryGetValue("USB powered", out usb);
            kv.TryGetValue("AC powered", out ac);
            sb.AppendLine("Cargador USB/AC  : " + (usb ?? "—") + " / " + (ac ?? "—"));

            string mc;
            if (kv.TryGetValue("Max charging current", out mc))
                sb.AppendLine("Corriente máx.   : " + mc + " µA");
            if (design > 0) sb.AppendLine("Capacidad diseño : " + design.ToString("0") + " mAh");
            if (est > 0) sb.AppendLine("Capacidad actual : ~" + est.ToString("0") + " mAh (estimación orientativa)");

            int cycles;
            if (int.TryParse(cyc, out cycles))
                sb.AppendLine("Ciclos de carga  : " + cycles);

            var warns = new List<string>();
            if (health != 2 && health != -1) warns.Add("Salud reportada: " + HealthName(health));
            if (tRaw != -1 && temp >= 45) warns.Add("Temperatura alta (45 °C o más)");
            if (voltage > 0 && voltage < 3400 && status != 2) warns.Add("Voltaje bajo");
            if (est > 0 && design > 0 && est / design < 0.8)
                warns.Add("Capacidad estimada " + (est / design).ToString("P0") + " de la original (desgaste)");

            sb.AppendLine();
            sb.AppendLine(warns.Count == 0
                ? "RESULTADO: sin problemas evidentes."
                : "AVISOS:\n - " + string.Join("\n - ", warns.ToArray()));
            sb.AppendLine("\nNota: la capacidad y los ciclos dependen de lo que el fabricante exponga; úsalos como referencia.");
            Show(sb.ToString());
        }

        async Task BatteryMonitorAsync()
        {
            var sb = new StringBuilder("=== MONITOR DE BATERÍA (cada 2 s) ===\n");
            for (int i = 1; i <= 10; i++)
            {
                var kv = ParseKv(await Adb.ShellAsync("dumpsys battery"));
                Func<string, string> V = k =>
                {
                    string x; return kv.TryGetValue(k, out x) ? x : "?";
                };
                sb.AppendLine(DateTime.Now.ToString("HH:mm:ss") +
                              "  nivel " + V("level") + "%  temp " + V("temperature") +
                              " (décimas °C)  voltaje " + V("voltage") +
                              " mV  estado " + V("status"));
                Show(sb.ToString());
                await Task.Delay(2000);
            }
        }

        // ================================================================
        //  3. DIAGNÓSTICO
        // ================================================================
        Control BuildDiagnostic()
        {
            var p = new Panel { Dock = DockStyle.Fill };
            var f = Flow();
            f.Controls.Add(Btn("Diagnóstico completo", DiagnosticAsync));
            f.Controls.Add(Btn("Guardar logcat", SaveLogcatAsync));
            Section(f, "Pruebas manuales (mira la pantalla del teléfono)");
            f.Controls.Add(Btn("Mostrar toques ON", () => SetAsync("show_touches", 1)));
            f.Controls.Add(Btn("Mostrar toques OFF", () => SetAsync("show_touches", 0)));
            f.Controls.Add(Btn("Test táctil (puntero) ON", () => SetAsync("pointer_location", 1)));
            f.Controls.Add(Btn("Test táctil OFF", () => SetAsync("pointer_location", 0)));
            f.Controls.Add(Btn("Brillo máximo", BrightMaxAsync));
            f.Controls.Add(Btn("Brillo bajo", BrightLowAsync));
            p.Controls.Add(f);
            return p;
        }

        async Task SetAsync(string key, int val)
        {
            var r = await Adb.ShellAsync("settings put system " + key + " " + val);
            Show(r.Length == 0 ? key + " = " + val + "  (hecho)" : r);
        }

        async Task BrightMaxAsync()
        {
            await Adb.ShellAsync("settings put system screen_brightness_mode 0");
            await Adb.ShellAsync("settings put system screen_brightness 255");
            Show("Brillo al máximo (modo manual).");
        }

        async Task BrightLowAsync()
        {
            await Adb.ShellAsync("settings put system screen_brightness 30");
            Show("Brillo bajo.");
        }

        async Task SaveLogcatAsync()
        {
            Show("Descargando logcat...");
            var txt = await Adb.ShellAsync("logcat -d", 90000);
            var file = Path.Combine(OutDir(), "logcat_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            File.WriteAllText(file, txt, Encoding.UTF8);
            Show("Logcat guardado en:\n" + file);
        }

        async Task DiagnosticAsync()
        {
            var sb = new StringBuilder("=== DIAGNÓSTICO COMPLETO ===\n");
            int ok = 0, warn = 0, fail = 0;

            Action<string, string, string> Add = (st, name, detail) =>
            {
                if (st == "OK") ok++; else if (st == "AVISO") warn++; else fail++;
                sb.AppendLine("[" + st + "] " + name + ": " + detail);
                Show(sb.ToString());
            };

            // 1) Conexión
            string state = "?";
            foreach (var d in devices) if (d.Serial == Adb.Serial) { state = d.State; break; }
            Add(state == "device" ? "OK" : "FALLO", "Conexión", Adb.Serial + " (" + state + ")");
            if (state != "device") return;

            // 2) Batería
            var kv = ParseKv(await Adb.ShellAsync("dumpsys battery"));
            Func<string, int> I = k =>
            {
                string v; int n;
                return kv.TryGetValue(k, out v) && int.TryParse(v, out n) ? n : -1;
            };
            int level = I("level"), health = I("health"), tRaw = I("temperature");
            Add(health == 2 ? "OK" : "AVISO", "Salud de batería", HealthName(health) + " (nivel " + level + "%)");
            if (tRaw != -1)
                Add(tRaw / 10.0 < 45 ? "OK" : "AVISO", "Temperatura", (tRaw / 10.0).ToString("0.0") + " °C");

            // 3) Almacenamiento
            var df = await Sh("df /data");
            var use = Regex.Match(df, @"\s(\d+)%");
            if (use.Success)
            {
                int u = int.Parse(use.Groups[1].Value);
                Add(u < 90 ? "OK" : "AVISO", "Almacenamiento", u + "% usado");
            }
            else Add("AVISO", "Almacenamiento", "no se pudo leer");

            // 4) RAM
            var mem = await Sh("cat /proc/meminfo");
            var mt = Regex.Match(mem, @"MemTotal:\s+(\d+)");
            var ma = Regex.Match(mem, @"MemAvailable:\s+(\d+)");
            if (mt.Success && ma.Success)
            {
                double tot = double.Parse(mt.Groups[1].Value);
                double av = double.Parse(ma.Groups[1].Value);
                Add(av / tot > 0.10 ? "OK" : "AVISO", "RAM",
                    (av / 1024).ToString("0") + " MB libres de " + (tot / 1024).ToString("0") + " MB");
            }

            // 5) Wi-Fi y Bluetooth
            var wifi = await Sh("dumpsys wifi | grep -m1 'Wi-Fi is'");
            Add(wifi.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) >= 0 ? "OK" : "AVISO",
                "Wi-Fi", wifi.Length == 0 ? "sin datos" : wifi);
            var bt = await Sh("settings get global bluetooth_on");
            Add(bt == "1" ? "OK" : "AVISO", "Bluetooth", bt == "1" ? "encendido" : "apagado o sin datos");

            // 6) SIM
            var sim = await Sh("getprop gsm.sim.state");
            bool simOk = sim.IndexOf("READY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         sim.IndexOf("LOADED", StringComparison.OrdinalIgnoreCase) >= 0;
            Add(simOk ? "OK" : "AVISO", "SIM",
                sim.Length == 0 ? "sin datos" : sim + (simOk ? "" : " (normal si no hay chip)"));

            // 7) Sensores
            var sens = await Adb.ShellAsync("dumpsys sensorservice", 45000);
            var sensorNames = new Dictionary<string, string>
            {
                { "Acelerómetro", "accelerometer" },
                { "Giroscopio", "gyroscope" },
                { "Proximidad", "proximity" },
                { "Luz ambiental", "light" },
                { "Brújula", "magnetic" }
            };
            foreach (var kv2 in sensorNames)
            {
                bool has = sens.IndexOf(kv2.Value, StringComparison.OrdinalIgnoreCase) >= 0;
                Add(has ? "OK" : "AVISO", "Sensor " + kv2.Key, has ? "detectado" : "no detectado");
            }

            // 8) Cámaras
            var cam = await Sh("dumpsys media.camera | grep -m1 'Number of camera devices'");
            Add(cam.Length > 0 ? "OK" : "AVISO", "Cámaras", cam.Length > 0 ? cam : "sin datos");

            // 9) Errores recientes
            var cr = await Adb.ShellAsync("logcat -b crash -d", 40000);
            int crashes = Regex.Matches(cr, "FATAL EXCEPTION|ANR in").Count;
            Add(crashes == 0 ? "OK" : "AVISO", "Cierres/errores recientes",
                crashes + " en el buffer de crash");

            // 10) Root
            var su = await Sh("which su");
            Add("OK", "Root", su.Length > 0 ? "posible acceso root: " + su : "sin root");

            sb.AppendLine();
            sb.AppendLine("RESULTADO: " + ok + " OK, " + warn + " avisos, " + fail + " fallos");
            Show(sb.ToString());
        }

        // ================================================================
        //  4. APLICACIONES
        // ================================================================
        Control BuildApps()
        {
            var p = new Panel { Dock = DockStyle.Fill };
            cmbType.Items.AddRange(new object[] { "Usuario", "Sistema", "Deshabilitadas", "Todas" });
            cmbType.SelectedIndex = 0;

            var top = Flow();
            top.Controls.Add(new Label { Text = "Tipo:", AutoSize = true, Margin = new Padding(4, 9, 2, 0) });
            top.Controls.Add(cmbType);
            top.Controls.Add(Btn("Listar apps", ListAppsAsync));
            top.Controls.Add(new Label { Text = "Buscar:", AutoSize = true, Margin = new Padding(14, 9, 2, 0) });
            top.Controls.Add(txtFilter);
            txtFilter.TextChanged += (s, e) => ApplyFilter();

            var bottom = Flow();
            bottom.Dock = DockStyle.Bottom;
            bottom.Controls.Add(Btn("Instalar APK…", InstallAsync));
            bottom.Controls.Add(Btn("Desinstalar", () => OnSelectedAsync("¿Desinstalar", UninstallOne, true)));
            bottom.Controls.Add(Btn("Restaurar (sistema)", () => OnSelectedAsync("¿Restaurar",
                pkg => Adb.ShellAsync("cmd package install-existing " + pkg), true)));
            bottom.Controls.Add(Btn("Deshabilitar", () => OnSelectedAsync("¿Deshabilitar",
                pkg => Adb.ShellAsync("pm disable-user --user 0 " + pkg), true)));
            bottom.Controls.Add(Btn("Habilitar", () => OnSelectedAsync("¿Habilitar",
                pkg => Adb.ShellAsync("pm enable " + pkg))));
            bottom.Controls.Add(Btn("Limpiar datos", () => OnSelectedAsync("¿Borrar los datos de",
                pkg => Adb.ShellAsync("pm clear " + pkg), true)));
            bottom.Controls.Add(Btn("Forzar cierre", () => OnSelectedAsync("¿Cerrar",
                pkg => Adb.ShellAsync("am force-stop " + pkg))));
            bottom.Controls.Add(Btn("Abrir", () => OnSelectedAsync("¿Abrir",
                pkg => Adb.ShellAsync("monkey -p " + pkg + " -c android.intent.category.LAUNCHER 1"))));
            bottom.Controls.Add(Btn("Extraer APK", () => OnSelectedAsync("¿Extraer", ExtractOne)));

            p.Controls.Add(lstApps);
            p.Controls.Add(bottom);
            p.Controls.Add(top);
            return p;
        }

        async Task ListAppsAsync()
        {
            string flag;
            switch (cmbType.SelectedIndex)
            {
                case 0: flag = "-3"; break;
                case 1: flag = "-s"; break;
                case 2: flag = "-d"; break;
                default: flag = ""; break;
            }
            var txt = await Adb.ShellAsync("pm list packages " + flag, 60000);
            allPkgs = txt.Split('\n')
                         .Select(l => l.Trim())
                         .Where(l => l.StartsWith("package:"))
                         .Select(l => l.Substring(8))
                         .OrderBy(x => x)
                         .ToList();
            ApplyFilter();
            Show(allPkgs.Count + " aplicaciones encontradas.\nSelecciona una o varias (Ctrl / Shift) y usa los botones de abajo.");
        }

        void ApplyFilter()
        {
            var f = txtFilter.Text.Trim();
            lstApps.BeginUpdate();
            lstApps.Items.Clear();
            foreach (var pkg in allPkgs.Where(x =>
                f.Length == 0 || x.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                lstApps.Items.Add(pkg);
            lstApps.EndUpdate();
        }

        async Task OnSelectedAsync(string title, Func<string, Task<string>> act, bool confirm = false)
        {
            var sel = lstApps.SelectedItems.Cast<string>().ToList();
            if (sel.Count == 0)
            {
                MessageBox.Show("Selecciona al menos una app de la lista.");
                return;
            }
            if (confirm && MessageBox.Show(title + " " + sel.Count + " app(s)?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var sb = new StringBuilder();
            foreach (var pkg in sel)
            {
                var r = (await act(pkg)).Trim();
                if (r.Length == 0) r = "OK";
                sb.AppendLine(pkg + ": " + r);
                Show(sb.ToString());
            }
        }

        async Task<string> UninstallOne(string pkg)
        {
            var r = await Adb.AdbAsync("uninstall " + pkg, 120000);
            if (r.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0) return r;
            var r2 = await Adb.ShellAsync("pm uninstall --user 0 " + pkg);
            return r2.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0
                ? "Quitada para el usuario 0 (app de sistema; se puede restaurar)"
                : r + " / " + r2;
        }

        async Task<string> ExtractOne(string pkg)
        {
            var paths = (await Adb.ShellAsync("pm path " + pkg)).Split('\n')
                            .Select(l => l.Trim())
                            .Where(l => l.StartsWith("package:"))
                            .Select(l => l.Substring(8))
                            .ToList();
            if (paths.Count == 0) return "no se encontró el APK";
            var dir = OutDir();
            var res = new List<string>();
            int n = 0;
            foreach (var path in paths)
            {
                n++;
                var dest = Path.Combine(dir, paths.Count == 1 ? pkg + ".apk" : pkg + "_" + n + ".apk");
                res.Add(await Adb.AdbAsync("pull " + path + " \"" + dest + "\"", 120000));
            }
            return string.Join(" | ", res.ToArray()) + "\nCarpeta: " + dir;
        }

        async Task InstallAsync()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "APK|*.apk",
                Multiselect = true,
                Title = "Elige uno o varios APK"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var sb = new StringBuilder();
                foreach (var file in dlg.FileNames)
                {
                    sb.AppendLine("Instalando " + Path.GetFileName(file) + " ...");
                    Show(sb.ToString());
                    var r = await Adb.AdbAsync("install -r \"" + file + "\"", 300000);
                    sb.AppendLine("   → " + r);
                    Show(sb.ToString());
                }
            }
        }

        // ================================================================
        //  5. AJUSTES Y TECLAS
        // ================================================================
        Control BuildSettings()
        {
            var f = Flow(true);

            Section(f, "Abrir ajustes del teléfono");
            var intents = new Dictionary<string, string>
            {
                { "Ajustes",                   "android.settings.SETTINGS" },
                { "Wi-Fi",                     "android.settings.WIFI_SETTINGS" },
                { "Bluetooth",                 "android.settings.BLUETOOTH_SETTINGS" },
                { "Pantalla",                  "android.settings.DISPLAY_SETTINGS" },
                { "Sonido",                    "android.settings.SOUND_SETTINGS" },
                { "Batería",                   "android.settings.BATTERY_SAVER_SETTINGS" },
                { "Aplicaciones",              "android.settings.APPLICATION_SETTINGS" },
                { "Almacenamiento",            "android.settings.INTERNAL_STORAGE_SETTINGS" },
                { "Info del teléfono",         "android.settings.DEVICE_INFO_SETTINGS" },
                { "Opciones de desarrollador", "android.settings.APPLICATION_DEVELOPMENT_SETTINGS" },
                { "Seguridad",                 "android.settings.SECURITY_SETTINGS" },
                { "Ubicación",                 "android.settings.LOCATION_SOURCE_SETTINGS" },
                { "Accesibilidad",             "android.settings.ACCESSIBILITY_SETTINGS" },
                { "Fecha y hora",              "android.settings.DATE_SETTINGS" },
                { "Idioma",                    "android.settings.LOCALE_SETTINGS" },
                { "Redes móviles",             "android.settings.NETWORK_OPERATOR_SETTINGS" },
                { "Modo avión",                "android.settings.AIRPLANE_MODE_SETTINGS" },
                { "NFC",                       "android.settings.NFC_SETTINGS" },
                { "Cuentas / sincronización",  "android.settings.SYNC_SETTINGS" }
            };
            foreach (var kv in intents)
            {
                var action = kv.Value;
                f.Controls.Add(Btn(kv.Key, () => IntentAsync(action)));
            }

            Section(f, "Teclas del teléfono");
            var keys = new Dictionary<string, int>
            {
                { "Inicio", 3 },
                { "Atrás", 4 },
                { "Recientes", 187 },
                { "Encender/Apagar pantalla", 26 },
                { "Volumen +", 24 },
                { "Volumen −", 25 },
                { "Silenciar", 164 },
                { "Cámara", 27 }
            };
            foreach (var kv in keys)
            {
                var code = kv.Value;
                f.Controls.Add(Btn(kv.Key, () => KeyAsync(code)));
            }

            Section(f, "Pantalla y energía");
            f.Controls.Add(Btn("Captura de pantalla", ScreenshotAsync));
            f.Controls.Add(Btn("Reiniciar", () => RebootAsync("")));
            f.Controls.Add(Btn("Reiniciar a Recovery", () => RebootAsync("recovery")));
            f.Controls.Add(Btn("Reiniciar a Bootloader", () => RebootAsync("bootloader")));
            f.Controls.Add(Btn("Apagar", () => RebootAsync("poweroff")));
            return f;
        }

        async Task IntentAsync(string action)
        {
            var r = await Adb.ShellAsync("am start -a " + action);
            Show(r);
        }

        Task KeyAsync(int code)
        {
            return Adb.ShellAsync("input keyevent " + code);
        }

        async Task ScreenshotAsync()
        {
            var file = Path.Combine(OutDir(), "captura_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            await Adb.ShellAsync("screencap -p /sdcard/_tk.png");
            var r = await Adb.AdbAsync("pull /sdcard/_tk.png \"" + file + "\"");
            await Adb.ShellAsync("rm /sdcard/_tk.png");
            Show(r + "\nGuardada en: " + file);
            if (File.Exists(file))
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }

        async Task RebootAsync(string mode)
        {
            if (MessageBox.Show("¿Seguro? El teléfono se reiniciará o apagará.", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            var r = mode == "poweroff"
                ? await Adb.ShellAsync("reboot -p")
                : await Adb.AdbAsync("reboot " + mode);
            Show(r.Length == 0 ? "Orden enviada." : r);
        }

        // ================================================================
        //  6. PANTALLA (scrcpy) y WI-FI
        // ================================================================
        Control BuildScreen()
        {
            var f = Flow(true);

            Section(f, "Duplicar y controlar la pantalla con scrcpy");
            f.Controls.Add(cbAwake);
            f.Controls.Add(cbOff);
            f.Controls.Add(cbNoCtl);
            f.Controls.Add(cbRec);
            f.SetFlowBreak(cbRec, true);
            f.Controls.Add(new Label { Text = "Resolución máx. (0 = nativa):", AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
            f.Controls.Add(nudSize);
            f.Controls.Add(new Label { Text = "Bitrate (Mbps):", AutoSize = true, Margin = new Padding(14, 8, 2, 0) });
            f.Controls.Add(nudBitrate);
            f.Controls.Add(Btn("▶ Iniciar scrcpy", StartScrcpy));

            Section(f, "ADB por Wi-Fi (conecta primero por USB)");
            f.Controls.Add(Btn("Obtener IP del teléfono", GetIpAsync));
            f.Controls.Add(Btn("Activar modo TCP/IP", async () => Show(await Adb.AdbAsync("tcpip 5555"))));
            f.Controls.Add(new Label { Text = "IP:", AutoSize = true, Margin = new Padding(10, 8, 2, 0) });
            f.Controls.Add(txtIp);
            f.Controls.Add(Btn("Conectar", ConnectWifiAsync, false));
            f.Controls.Add(Btn("Desconectar", DisconnectWifiAsync, false));
            return f;
        }

        Task StartScrcpy()
        {
            // Verificamos si scrcpy existe antes de intentar abrirlo
            if (!File.Exists(Adb.ScrcpyPath) && !IsOnPath("scrcpy.exe"))
            {
                MessageBox.Show(
                    "No se encontró scrcpy.exe.\n\n" +
                    "Para usar esta función, descarga scrcpy desde su página oficial en GitHub " +
                    "(https://github.com/Genymobile/scrcpy/releases/latest), descomprime el archivo ZIP " +
                    "y coloca TODOS los archivos dentro de la carpeta:\n\n" +
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Scrcpy"),
                    "scrcpy no encontrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return Task.FromResult(false);
            }

            var a = new StringBuilder("-s " + Adb.Serial + " --window-title \"Android-Toolkit.Pro - " + Adb.Serial + "\"");
            if (cbAwake.Checked) a.Append(" --stay-awake");
            if (cbOff.Checked) a.Append(" --turn-screen-off");
            if (cbNoCtl.Checked) a.Append(" --no-control");
            if (nudSize.Value > 0) a.Append(" --max-size " + ((int)nudSize.Value));
            a.Append(" -b " + ((int)nudBitrate.Value) + "M");
            string rec = null;
            if (cbRec.Checked)
            {
                rec = Path.Combine(OutDir(), "grabacion_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4");
                a.Append(" --record \"" + rec + "\"");
            }

            try
            {
                var psi = new ProcessStartInfo(Adb.ScrcpyPath, a.ToString())
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                if (File.Exists(Adb.AdbPath)) psi.EnvironmentVariables["ADB"] = Adb.AdbPath;
                Process.Start(psi);
                Show("scrcpy iniciado." + (rec != null ? "\nGrabando en: " + rec : ""));
            }
            catch (Exception ex)
            {
                Show("No se pudo iniciar scrcpy: " + ex.Message +
                     "\nColoca scrcpy en la carpeta Tools\\Scrcpy junto al programa.");
            }
            return Task.FromResult(false);
        }

        async Task GetIpAsync()
        {
            var r = await Adb.ShellAsync("ip -f inet addr show wlan0");
            var m = Regex.Match(r, @"inet (\d+\.\d+\.\d+\.\d+)");
            if (m.Success)
            {
                txtIp.Text = m.Groups[1].Value;
                Show("IP del teléfono: " + m.Groups[1].Value);
            }
            else Show("No se pudo leer la IP (¿Wi-Fi conectado?).\n" + r);
        }

        async Task ConnectWifiAsync()
        {
            var r = await Adb.RunAsync(Adb.AdbPath, "connect " + txtIp.Text.Trim() + ":5555", 20000);
            await RefreshDevicesAsync();
            Append(r);
        }

        async Task DisconnectWifiAsync()
        {
            var r = await Adb.RunAsync(Adb.AdbPath, "disconnect", 20000);
            await RefreshDevicesAsync();
            Append(r);
        }

        // ================================================================
        //  7. CONSOLA ADB
        // ================================================================
        Control BuildConsole()
        {
            var f = Flow(true);
            Section(f, "Escribe un comando (sin la palabra 'adb'). Ejemplo: shell getprop ro.product.model");
            txtCmd.Width = 560;
            f.Controls.Add(txtCmd);
            f.Controls.Add(Btn("Ejecutar", RunCmdAsync, false));
            txtCmd.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await RunCmdAsync();
                }
            };

            Section(f, "Atajos");
            f.Controls.Add(Btn("adb devices", async () => Show(await Adb.RunAsync(Adb.AdbPath, "devices -l")), false));
            f.Controls.Add(Btn("Reiniciar servidor ADB", RestartServerAsync, false));
            f.Controls.Add(Btn("Ver versión de adb", async () => Show(await Adb.RunAsync(Adb.AdbPath, "version")), false));
            return f;
        }

        async Task RunCmdAsync()
        {
            var c = txtCmd.Text.Trim();
            if (c.Length == 0) return;
            Show("> adb " + c + "\n");
            var pre = string.IsNullOrEmpty(Adb.Serial) ? "" : "-s " + Adb.Serial + " ";
            Append(await Adb.RunAsync(Adb.AdbPath, pre + c, 120000));
        }

        async Task RestartServerAsync()
        {
            await Adb.RunAsync(Adb.AdbPath, "kill-server");
            var r = await Adb.RunAsync(Adb.AdbPath, "start-server");
            await RefreshDevicesAsync();
            Append("Servidor ADB reiniciado.\n" + r);
        }

        // ================================================================
        //  8. IMEI Y DATOS DEL EQUIPO
        // ================================================================
        Control BuildIdent()
        {
            var f = Flow(true);

            Section(f, "Identificadores del equipo (para la orden de trabajo)");
            f.Controls.Add(Btn("Leer IMEI / MEID / Serie / Android ID", ReadIdentAsync));
            f.Controls.Add(Btn("Copiar salida al portapapeles", CopyOutputAsync, false));

            Section(f, "Notas");
            f.Controls.Add(new Label
            {
                Text = "En Android 10+ el IMEI solo se lee si el sistema lo permite.\n" +
                       "Alternativa fiable: marcar *#06# en el teclado del teléfono.\n" +
                       "La serie y el Android ID siempre se leen por adb.",
                AutoSize = true,
                MaximumSize = new Size(700, 0),
                ForeColor = Color.DimGray
            });
            return f;
        }

        Task CopyOutputAsync()
        {
            if (output.TextLength == 0) { Show("Nada para copiar."); return Task.FromResult(false); }
            Clipboard.SetText(output.Text);
            Append("(Salida copiada al portapapeles)");
            return Task.FromResult(false);
        }

        async Task ReadIdentAsync()
        {
            Show("Leyendo identificadores...");
            var sb = new StringBuilder("=== IDENTIFICADORES DEL EQUIPO ===\n");
            sb.AppendLine("Fecha                 : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            var serial = await Sh("getprop ro.serialno");
            if (string.IsNullOrWhiteSpace(serial)) serial = Adb.Serial;
            sb.AppendLine("Número de serie       : " + serial);

            sb.AppendLine("Marca / modelo        : " + await Sh("getprop ro.product.brand") + " " +
                          await Sh("getprop ro.product.model"));
            sb.AppendLine("Nombre en código      : " + await Sh("getprop ro.product.device"));
            sb.AppendLine("Android               : " + await Sh("getprop ro.build.version.release") +
                          " (SDK " + await Sh("getprop ro.build.version.sdk") + ")");

            var aid = await Sh("settings get secure android_id");
            sb.AppendLine("Android ID            : " + (aid.Length > 0 ? aid : "—"));

            sb.AppendLine("IMEI / MEID           : " + await ReadImeiAsync());

            var mac = await Sh("cat /sys/class/net/wlan0/address");
            sb.AppendLine("MAC Wi-Fi             : " + (mac.Length > 0 ? mac : "—"));
            sb.AppendLine("Operador              : " + await Sh("getprop gsm.operator.alpha"));
            sb.AppendLine("Estado SIM            : " + await Sh("getprop gsm.sim.state"));

            Show(sb.ToString());
        }

        async Task<string> ReadImeiAsync()
        {
            var p = await Sh("getprop | grep -iE 'imei|meid'");
            var m = Regex.Match(p, @"\[(?:gsm\.(?:imei|meid)|persist\..*imei)\]:\s*\[(\d{14,16})\]");
            if (m.Success) return m.Groups[2].Value;

            var s1 = await Sh("service call iphonesubinfo 1", 15000);
            var i1 = ParseIphoneSubInfo(s1);
            if (i1 != null) return i1;

            var s3 = await Sh("service call iphonesubinfo 3", 15000);
            var i3 = ParseIphoneSubInfo(s3);
            if (i3 != null) return i3;

            var d = await Sh("dumpsys iphonesubinfo", 15000);
            var md = Regex.Match(d, @"(?:Device ID|IMEI)\s*[=:]\s*(\d{14,16})");
            if (md.Success) return md.Groups[1].Value;

            return "No accesible (usa *#06# en el teléfono)";
        }

        static string ParseIphoneSubInfo(string raw)
        {
            var sb = new StringBuilder();
            foreach (Match q in Regex.Matches(raw, @"'([^']*)'"))
                sb.Append(q.Groups[1].Value);

            var digits = Regex.Replace(sb.ToString(), @"[^0-9]", "");
            return digits.Length >= 14 ? digits : null;
        }

        // ================================================================
        //  9. PRUEBAS DE HARDWARE
        // ================================================================
        Control BuildHardwareTests()
        {
            var f = Flow(true);

            Section(f, "Pantalla — colores sólidos (detecta píxeles muertos)");
            var colors = new Dictionary<string, string>
            {
                { "Rojo",   "#FF0000" },
                { "Verde",  "#00FF00" },
                { "Azul",   "#0000FF" },
                { "Blanco", "#FFFFFF" },
                { "Negro",  "#000000" },
                { "Gris",   "#808080" }
            };
            foreach (var kv in colors)
            {
                var h = kv.Value;
                f.Controls.Add(Btn(kv.Key, () => ShowColorAsync(h)));
            }

            Section(f, "Audio y vibración");
            f.Controls.Add(Btn("Tono 1 kHz x 2 s (marcador)", TonoAsync));
            f.Controls.Add(Btn("Vibrar 1 s", () => VibrateAsync(1000)));
            f.Controls.Add(Btn("Vibrar patrón", () => VibrateAsync(200)));

            Section(f, "Cámaras");
            f.Controls.Add(Btn("Abrir cámara trasera", () => ShellShowAsync("am start -a android.media.action.STILL_IMAGE_CAMERA")));
            f.Controls.Add(Btn("Abrir cámara frontal", () => ShellShowAsync("am start -a android.media.action.STILL_IMAGE_CAMERA_FRONT")));
            f.Controls.Add(Btn("Grabar video", () => ShellShowAsync("am start -a android.media.action.VIDEO_CAMERA")));

            Section(f, "Sensores");
            f.Controls.Add(Btn("Volcar sensores (dumpsys)", SensorsDumpAsync));

            Section(f, "Micrófono");
            f.Controls.Add(Btn("Abrir grabadora del teléfono",
                () => ShellShowAsync("am start -a android.provider.MediaStore.RECORD_SOUND")));

            Section(f, "Extras de taller");
            f.Controls.Add(Btn("Ver FRP / cuenta Google", FrpCheckAsync));
            f.Controls.Add(Btn("Modo seguro (reiniciar)", SafeModeAsync));
            f.Controls.Add(Btn("Borrar caché de TODAS las apps", ClearAllCachesAsync));
            f.Controls.Add(Btn("Backup de /sdcard (pull)", BackupSdcardAsync));

            return f;
        }

        async Task ShellShowAsync(string cmd, bool showResult = true)
        {
            var r = await Adb.ShellAsync(cmd);
            if (showResult) Show(r.Length == 0 ? "Hecho." : r);
        }

        async Task ShowColorAsync(string hex)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "AndroidToolkitPro");
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "color_" + hex.TrimStart('#') + ".png");
            if (!File.Exists(path))
            {
                using (var bmp = new Bitmap(1080, 1920))
                {
                    using (var g = Graphics.FromImage(bmp))
                        g.Clear(ColorTranslator.FromHtml(hex));
                    bmp.Save(path, ImageFormat.Png);
                }
            }
            await Adb.AdbAsync("push \"" + path + "\" /sdcard/_tk_color.png");
            await Adb.ShellAsync("am start -a android.intent.action.VIEW -d file:///sdcard/_tk_color.png -t image/png");
            Show("Mostrando " + hex + " en el teléfono. Pulsa Atrás para salir.");
        }

        async Task TonoAsync()
        {
            var r = await Adb.ShellAsync("am start -a android.intent.action.DIAL -d tel:123");
            Show("Se abrió el marcador. Presioná una tecla para probar el altavoz.\n" + r);
        }

        async Task VibrateAsync(int ms)
        {
            var r = await Adb.ShellAsync("cmd vibrator vibrate " + ms);
            if (r.IndexOf("Unknown", StringComparison.OrdinalIgnoreCase) >= 0 ||
                r.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                r.Length == 0)
                r = await Adb.ShellAsync("service call vibrator 2 i32 1000 i32 0 s16 android.os.VibrationEffect");
            Show(r.Length == 0 ? "Vibración " + ms + " ms enviada." : r);
        }

        async Task SensorsDumpAsync()
        {
            Show("Leyendo sensores...");
            var r = await Adb.ShellAsync("dumpsys sensorservice | head -n 120", 45000);
            Show("=== SENSORES DISPONIBLES ===\n" + r);
        }

        async Task FrpCheckAsync()
        {
            Show("Comprobando estado de FRP / cuentas...");
            var sb = new StringBuilder("=== ESTADO FRP / CUENTAS ===\n");

            var frp = await Sh("getprop ro.frp.pst");
            sb.AppendLine("Partición FRP         : " + (frp.Length > 0 ? frp : "—"));

            var acc = await Sh("dumpsys account | grep -m5 'Account {'");
            sb.AppendLine("\nCuentas configuradas:\n" + (acc.Length > 0 ? acc : "ninguna"));

            var google = await Sh("pm list packages | grep -i 'com.google.android.gms'");
            sb.AppendLine("\nGoogle Play Services  : " + (google.Length > 0 ? "instalado" : "NO instalado"));

            var dev = await Sh("settings get global device_provisioned");
            sb.AppendLine("Device provisioned    : " + dev);

            Show(sb.ToString());
        }

        async Task SafeModeAsync()
        {
            if (MessageBox.Show("¿Reiniciar en Modo Seguro? Solo apps del sistema arrancarán.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await Adb.ShellAsync("setprop persist.sys.safemode 1");
            await Adb.ShellAsync("reboot");
            Show("Modo seguro programado. El teléfono se reiniciará.");
        }

        async Task ClearAllCachesAsync()
        {
            if (MessageBox.Show("¿Borrar la caché de TODAS las apps del usuario?\n(No borra datos personales, solo caché)",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Show("Borrando caché de todas las apps...");
            var pkgs = (await Adb.ShellAsync("pm list packages -3", 60000))
                .Split('\n').Select(l => l.Trim())
                .Where(l => l.StartsWith("package:"))
                .Select(l => l.Substring(8)).ToList();

            var sb = new StringBuilder("Apps: " + pkgs.Count + "\n");
            int n = 0;
            foreach (var pkg in pkgs)
            {
                n++;
                sb.AppendLine("[" + n + "/" + pkgs.Count + "] " + pkg);
                Show(sb.ToString());
                await Adb.ShellAsync("pm clear --cache-only " + pkg, 15000);
            }
            sb.AppendLine("\nHecho.");
            Show(sb.ToString());
        }

        async Task BackupSdcardAsync()
        {
            var dest = Path.Combine(OutDir(), "backup_sdcard_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dest);
            Show("Copiando /sdcard ... (puede tardar)");
            var r = await Adb.AdbAsync("pull /sdcard/ \"" + dest + "\"", 900000);
            Show("Backup terminado.\n" + r + "\n\nCarpeta: " + dest);
        }

        // ================================================================
        //  10. INFORME DE TALLER
        // ================================================================
        Control BuildWorkReport()
        {
            var f = Flow(true);

            Section(f, "Datos del cliente");
            var txtNombre = new TextBox { Width = 250 };
            var txtTel    = new TextBox { Width = 150 };
            var txtEmail  = new TextBox { Width = 200 };
            f.Controls.Add(new Label { Text = "Nombre:",   AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
            f.Controls.Add(txtNombre);
            f.SetFlowBreak(txtNombre, true);
            f.Controls.Add(new Label { Text = "Teléfono:", AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
            f.Controls.Add(txtTel);
            f.Controls.Add(new Label { Text = "Email:",    AutoSize = true, Margin = new Padding(10, 8, 2, 0) });
            f.Controls.Add(txtEmail);
            f.SetFlowBreak(txtEmail, true);

            Section(f, "Falla reportada por el cliente");
            var txtFalla = new TextBox { Width = 640, Multiline = true, Height = 60 };
            f.Controls.Add(txtFalla);
            f.SetFlowBreak(txtFalla, true);

            Section(f, "Presupuesto y estado");
            var nudPresu = new NumericUpDown { Width = 110, DecimalPlaces = 2, Maximum = 1000000, Value = 0 };
            var cmbEstado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            cmbEstado.Items.AddRange(new object[]
            {
                "Recibido", "En diagnóstico", "Esperando repuesto",
                "En reparación", "Reparado — listo para entregar",
                "Entregado", "No reparable", "Presupuesto rechazado"
            });
            cmbEstado.SelectedIndex = 0;
            f.Controls.Add(new Label { Text = "Presupuesto:", AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
            f.Controls.Add(nudPresu);
            f.Controls.Add(new Label { Text = "Estado:", AutoSize = true, Margin = new Padding(14, 8, 2, 0) });
            f.Controls.Add(cmbEstado);
            f.SetFlowBreak(cmbEstado, true);

            Section(f, "Observaciones / repuestos / accesorios recibidos");
            var txtObs = new TextBox { Width = 640, Multiline = true, Height = 70 };
            f.Controls.Add(txtObs);
            f.SetFlowBreak(txtObs, true);

            Section(f, "Generar documento");
            f.Controls.Add(Btn("Generar informe completo (.txt)",
                () => GenerarInforme(txtNombre, txtTel, txtEmail, txtFalla, nudPresu, cmbEstado, txtObs), false));
            f.Controls.Add(Btn("Generar solo diagnóstico técnico", GenerarDiagnosticoTecnicoAsync, false));

            return f;
        }

        async Task GenerarInforme(TextBox tNombre, TextBox tTel, TextBox tEmail,
                                  TextBox tFalla, NumericUpDown nPresu,
                                  ComboBox cEstado, TextBox tObs)
        {
            if (tNombre.Text.Trim().Length == 0)
            {
                MessageBox.Show("Ingresá al menos el nombre del cliente.");
                return;
            }

            Show("Generando informe...");

            var sb = new StringBuilder();
            string ticket = "T" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string line = new string('=', 62);

            sb.AppendLine(line);
            sb.AppendLine("              ORDEN DE REPARACIÓN — TALLER DE MÓVILES");
            sb.AppendLine(line);
            sb.AppendLine("Ticket         : " + ticket);
            sb.AppendLine("Fecha          : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            sb.AppendLine();
            sb.AppendLine("--- DATOS DEL CLIENTE ---");
            sb.AppendLine("Nombre         : " + tNombre.Text.Trim());
            sb.AppendLine("Teléfono       : " + tTel.Text.Trim());
            sb.AppendLine("Email          : " + tEmail.Text.Trim());
            sb.AppendLine();
            sb.AppendLine("--- DATOS DEL EQUIPO ---");

            var props = ParseProps(await Adb.ShellAsync("getprop"));
            Func<string, string> G = k =>
            {
                string v; return props.TryGetValue(k, out v) && v.Length > 0 ? v : "—";
            };
            sb.AppendLine("Marca / Modelo : " + G("ro.product.brand") + " " + G("ro.product.model"));
            sb.AppendLine("Código         : " + G("ro.product.device"));
            sb.AppendLine("Android        : " + G("ro.build.version.release") + " (SDK " + G("ro.build.version.sdk") + ")");
            sb.AppendLine("N° de serie    : " + G("ro.serialno"));
            sb.AppendLine("IMEI / MEID    : " + await ReadImeiAsync());
            sb.AppendLine("Android ID     : " + await Sh("settings get secure android_id"));
            sb.AppendLine();
            sb.AppendLine("--- FALLA REPORTADA ---");
            sb.AppendLine(tFalla.Text.Trim());
            sb.AppendLine();
            sb.AppendLine("--- ESTADO DEL SERVICIO ---");
            sb.AppendLine("Estado actual  : " + cEstado.SelectedItem);
            sb.AppendLine("Presupuesto    : $" + nPresu.Value.ToString("N2"));
            sb.AppendLine();
            sb.AppendLine("--- OBSERVACIONES ---");
            sb.AppendLine(tObs.Text.Trim());
            sb.AppendLine();
            sb.AppendLine(line);
            sb.AppendLine("El cliente declara entregar el equipo en las condiciones aquí");
            sb.AppendLine("descritas y acepta el presupuesto indicado. El taller no se");
            sb.AppendLine("responsabiliza por datos no respaldados por el cliente.");
            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine("Firma del cliente       : ______________________________");
            sb.AppendLine();
            sb.AppendLine("Aclaración              : ______________________________");
            sb.AppendLine();
            sb.AppendLine("Firma del técnico       : ______________________________");
            sb.AppendLine();
            sb.AppendLine(line);
            sb.AppendLine("Documento generado por Android-Toolkit.Pro — " +
                          DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            var dir = Path.Combine(OutDir(), "Informes");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "Informe_" + ticket + "_" + Sanitize(tNombre.Text) + ".txt");
            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);

            Show(sb.ToString() + "\n\nGuardado en:\n" + file);

            if (MessageBox.Show("¿Abrir el informe ahora?", "Informe generado",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }

        async Task GenerarDiagnosticoTecnicoAsync()
        {
            Show("Ejecutando diagnóstico técnico...");
            var diag = new StringBuilder();
            diag.AppendLine("=== DIAGNÓSTICO TÉCNICO AUTOMÁTICO ===");
            diag.AppendLine("Fecha : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            diag.AppendLine("Equipo: " + await Sh("getprop ro.product.brand") + " " +
                            await Sh("getprop ro.product.model"));
            diag.AppendLine("Serie : " + await Sh("getprop ro.serialno"));
            diag.AppendLine();

            var kv = ParseKv(await Adb.ShellAsync("dumpsys battery"));
            Func<string, int> I = k =>
            {
                string v; int n;
                return kv.TryGetValue(k, out v) && int.TryParse(v, out n) ? n : -1;
            };
            diag.AppendLine("-- Batería --");
            diag.AppendLine("Nivel: " + I("level") + "%   Salud: " + HealthName(I("health")) +
                            "   Temp: " + (I("temperature") / 10.0).ToString("0.0") +
                            " °C   Voltaje: " + I("voltage") + " mV");

            var df = await Sh("df /data");
            var mUse = Regex.Match(df, @"\s(\d+)%");
            if (mUse.Success) diag.AppendLine("-- Almacenamiento -- Uso /data: " + mUse.Groups[1].Value + "%");

            var mem = await Sh("cat /proc/meminfo");
            var mT = Regex.Match(mem, @"MemTotal:\s+(\d+)");
            var mA = Regex.Match(mem, @"MemAvailable:\s+(\d+)");
            if (mT.Success && mA.Success)
            {
                double t = double.Parse(mT.Groups[1].Value);
                double a = double.Parse(mA.Groups[1].Value);
                diag.AppendLine("-- RAM -- " + (a / 1024).ToString("0") +
                                " MB libres de " + (t / 1024).ToString("0") + " MB");
            }

            diag.AppendLine();
            diag.AppendLine("-- Sensores detectados --");
            var sens = await Adb.ShellAsync("dumpsys sensorservice", 45000);
            foreach (var s in new[] { "accelerometer", "gyroscope", "proximity", "light", "magnetic", "pressure", "step" })
            {
                bool has = sens.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
                diag.AppendLine("  " + s.PadRight(15) + ": " + (has ? "OK" : "no detectado"));
            }

            var dir = Path.Combine(OutDir(), "Informes");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "Diagnostico_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            File.WriteAllText(file, diag.ToString(), Encoding.UTF8);
            Show(diag + "\n\nGuardado en:\n" + file);
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_').Trim('_');
        }
    }
}
