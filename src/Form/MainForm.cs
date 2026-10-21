using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AndroidToolkitPro
{
    public class MainForm : Form
    {
        // =========================================================
        // CONTROLES GLOBALES (Selector de Dispositivo)
        // =========================================================
        private Panel panelSuperior;
        private ComboBox cmbDispositivos;
        private Button btnRefrescarDispositivos;
        private Label lblDispositivoActual;

        private TabControl tabControl;

        // Pestaña Fastboot
        private TabPage tabFastboot;
        private Button btnEjecutarFastboot;
        private TextBox txtSalidaFastboot;

        // Pestaña Herramientas Avanzadas
        private TabPage tabAvanzado;
        private Button btnGetvar;
        private Button btnFlashBoot;
        private Button btnFlashRecovery;
        private Button btnSideload;
        private TextBox txtSalidaAvanzado;

        // Pestaña APKs (Ahora con ComboBox)
        private TabPage tabApks;
        private Button btnInstalarApk;
        private Button btnDesinstalarApk;
        private ComboBox cmbAppsInstaladas;
        private Button btnCargarApps;
        private TextBox txtSalidaApks;

        // Pestaña Archivos (Push/Pull)
        private TabPage tabArchivos;
        private Button btnPush;
        private Button btnPull;
        private TextBox txtRutaAndroid;
        private TextBox txtSalidaArchivos;

        // Pestaña Logcat
        private TabPage tabLogcat;
        private Button btnIniciarLogcat;
        private RichTextBox rtbLogcat;
        private Process _logcatProcess;
        private bool _logcatActivo = false;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================
        public MainForm()
        {
            this.Text = "Android Toolkit Pro";
            this.Size = new Size(780, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(650, 500);

            // 1. Crear el Panel Superior (Selector de Dispositivo)
            InicializarPanelSuperior();

            // 2. Crear el Panel de Pestañas (debajo del panel superior)
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);
            // Asegurarse de que el TabControl esté debajo del panel superior
            tabControl.BringToFront(); 
            panelSuperior.SendToBack();

            InicializarTabFastboot();
            InicializarTabAvanzado();
            InicializarTabApks();
            InicializarTabArchivos();
            InicializarTabLogcat();

            // Cargar dispositivos al abrir el programa
            RefrescarDispositivos();
        }

        // =========================================================
        // PANEL SUPERIOR: SELECTOR DE DISPOSITIVO
        // =========================================================
        private void InicializarPanelSuperior()
        {
            panelSuperior = new Panel();
            panelSuperior.Dock = DockStyle.Top;
            panelSuperior.Height = 50;
            panelSuperior.BackColor = Color.LightGray;
            this.Controls.Add(panelSuperior);

            lblDispositivoActual = new Label();
            lblDispositivoActual.Text = "Dispositivo conectado:";
            lblDispositivoActual.Location = new Point(10, 15);
            lblDispositivoActual.AutoSize = true;
            lblDispositivoActual.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            panelSuperior.Controls.Add(lblDispositivoActual);

            cmbDispositivos = new ComboBox();
            cmbDispositivos.Location = new Point(150, 12);
            cmbDispositivos.Width = 300;
            cmbDispositivos.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDispositivos.SelectedIndexChanged += (s, e) => { /* Actualizar algo si es necesario */ };
            panelSuperior.Controls.Add(cmbDispositivos);

            btnRefrescarDispositivos = new Button();
            btnRefrescarDispositivos.Text = "🔄 Refrescar Lista";
            btnRefrescarDispositivos.Location = new Point(460, 10);
            btnRefrescarDispositivos.Size = new Size(120, 28);
            btnRefrescarDispositivos.Click += new EventHandler(this.btnRefrescarDispositivos_Click);
            panelSuperior.Controls.Add(btnRefrescarDispositivos);
        }

        private void btnRefrescarDispositivos_Click(object sender, EventArgs e)
        {
            RefrescarDispositivos();
        }

        private void RefrescarDispositivos()
        {
            cmbDispositivos.Items.Clear();
            string resultado = AdbHelper.RunCommand("devices");

            // Parsear la salida de 'adb devices'
            string[] lineas = resultado.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string linea in lineas)
            {
                if (linea.Contains("\tdevice"))
                {
                    string serial = linea.Split('\t')[0].Trim();
                    // Obtener el modelo del teléfono para mostrarlo bonito
                    string modelo = AdbHelper.RunCommand($"-s {serial} shell getprop ro.product.model").Trim();
                    
                    if (string.IsNullOrEmpty(modelo) || modelo.Contains("Error"))
                        modelo = "Desconocido";

                    cmbDispositivos.Items.Add($"{modelo} ({serial})");
                }
            }

            if (cmbDispositivos.Items.Count > 0)
            {
                cmbDispositivos.SelectedIndex = 0;
            }
            else
            {
                cmbDispositivos.Items.Add("No hay dispositivos conectados");
                cmbDispositivos.SelectedIndex = 0;
            }
        }

        // Función auxiliar para obtener el serial del dispositivo seleccionado
        private string ObtenerSerialSeleccionado()
        {
            if (cmbDispositivos.SelectedItem == null || cmbDispositivos.SelectedItem.ToString().Contains("No hay"))
                return null;

            string item = cmbDispositivos.SelectedItem.ToString();
            // Extrae lo que está entre paréntesis: Modelo (SERIAL)
            int inicio = item.LastIndexOf('(') + 1;
            int fin = item.LastIndexOf(')');
            return item.Substring(inicio, fin - inicio);
        }

        // =========================================================
        // PESTAÑA 1: FASTBOOT BÁSICO
        // =========================================================
        private void InicializarTabFastboot()
        {
            tabFastboot = new TabPage("Fastboot Básico");
            tabControl.TabPages.Add(tabFastboot);

            btnEjecutarFastboot = new Button();
            btnEjecutarFastboot.Text = "Ejecutar 'fastboot devices'";
            btnEjecutarFastboot.Location = new Point(20, 20);
            btnEjecutarFastboot.Size = new Size(220, 40);
            btnEjecutarFastboot.Click += new EventHandler(this.btnEjecutarFastboot_Click);
            tabFastboot.Controls.Add(btnEjecutarFastboot);

            txtSalidaFastboot = new TextBox();
            txtSalidaFastboot.Location = new Point(20, 80);
            txtSalidaFastboot.Size = new Size(710, 480);
            txtSalidaFastboot.Multiline = true;
            txtSalidaFastboot.ScrollBars = ScrollBars.Vertical;
            txtSalidaFastboot.ReadOnly = true;
            txtSalidaFastboot.Font = new Font("Consolas", 10);
            txtSalidaFastboot.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabFastboot.Controls.Add(txtSalidaFastboot);
        }

        private void btnEjecutarFastboot_Click(object sender, EventArgs e)
        {
            txtSalidaFastboot.Clear();
            txtSalidaFastboot.AppendText("Iniciando prueba de Fastboot...\r\n\r\n");
            string resultado = AdbHelper.RunFastbootCommand("devices");
            txtSalidaFastboot.AppendText("--- RESPUESTA DE FASTBOOT ---\r\n" + resultado + "\r\n");
        }

        // =========================================================
        // PESTAÑA 2: HERRAMIENTAS AVANZADAS
        // =========================================================
        private void InicializarTabAvanzado()
        {
            tabAvanzado = new TabPage("Herramientas Avanzadas");
            tabControl.TabPages.Add(tabAvanzado);

            btnGetvar = new Button();
            btnGetvar.Text = "Info del Dispositivo (getvar all)";
            btnGetvar.Location = new Point(20, 20);
            btnGetvar.Size = new Size(220, 40);
            btnGetvar.Click += new EventHandler(this.btnGetvar_Click);
            tabAvanzado.Controls.Add(btnGetvar);

            btnFlashBoot = new Button();
            btnFlashBoot.Text = "Flashear Boot.img";
            btnFlashBoot.Location = new Point(250, 20);
            btnFlashBoot.Size = new Size(160, 40);
            btnFlashBoot.Click += new EventHandler(this.btnFlashBoot_Click);
            tabAvanzado.Controls.Add(btnFlashBoot);

            btnFlashRecovery = new Button();
            btnFlashRecovery.Text = "Flashear Recovery.img";
            btnFlashRecovery.Location = new Point(420, 20);
            btnFlashRecovery.Size = new Size(170, 40);
            btnFlashRecovery.Click += new EventHandler(this.btnFlashRecovery_Click);
            tabAvanzado.Controls.Add(btnFlashRecovery);

            btnSideload = new Button();
            btnSideload.Text = "ADB Sideload (Recovery)";
            btnSideload.Location = new Point(600, 20);
            btnSideload.Size = new Size(130, 40);
            btnSideload.Click += new EventHandler(this.btnSideload_Click);
            tabAvanzado.Controls.Add(btnSideload);

            txtSalidaAvanzado = new TextBox();
            txtSalidaAvanzado.Location = new Point(20, 80);
            txtSalidaAvanzado.Size = new Size(710, 480);
            txtSalidaAvanzado.Multiline = true;
            txtSalidaAvanzado.ScrollBars = ScrollBars.Vertical;
            txtSalidaAvanzado.ReadOnly = true;
            txtSalidaAvanzado.Font = new Font("Consolas", 10);
            txtSalidaAvanzado.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabAvanzado.Controls.Add(txtSalidaAvanzado);
        }

        private void btnGetvar_Click(object sender, EventArgs e)
        {
            txtSalidaAvanzado.Clear();
            txtSalidaAvanzado.AppendText("Obteniendo información del dispositivo...\r\n\r\n");
            txtSalidaAvanzado.AppendText(AdbHelper.RunFastbootCommand("getvar all") + "\r\n");
        }

        private void btnFlashBoot_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imágenes (*.img)|*.img";
            ofd.Title = "Selecciona el archivo boot.img";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (MessageBox.Show("⚠️ ADVERTENCIA: Vas a flashear BOOT.\n¿Continuar?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    txtSalidaAvanzado.AppendText($"Flasheando {ofd.SafeFileName}...\r\n");
                    txtSalidaAvanzado.AppendText(AdbHelper.RunFastbootCommand($"flash boot \"{ofd.FileName}\"") + "\r\n\r\n");
                }
            }
        }

        private void btnFlashRecovery_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imágenes (*.img)|*.img";
            ofd.Title = "Selecciona el archivo recovery.img";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (MessageBox.Show("⚠️ ADVERTENCIA: Vas a flashear RECOVERY.\n¿Continuar?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    txtSalidaAvanzado.AppendText($"Flasheando {ofd.SafeFileName}...\r\n");
                    txtSalidaAvanzado.AppendText(AdbHelper.RunFastbootCommand($"flash recovery \"{ofd.FileName}\"") + "\r\n\r\n");
                }
            }
        }

        private void btnSideload_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos ZIP (*.zip)|*.zip";
            ofd.Title = "Selecciona el archivo ZIP para Sideload";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtSalidaAvanzado.AppendText($"Iniciando Sideload de {ofd.SafeFileName}...\r\n");
                txtSalidaAvanzado.AppendText(AdbHelper.RunCommand($"sideload \"{ofd.FileName}\"") + "\r\n\r\n");
            }
        }

        // =========================================================
        // PESTAÑA 3: GESTOR DE APKS (CON SELECTOR DE APPS)
        // =========================================================
        private void InicializarTabApks()
        {
            tabApks = new TabPage("Gestor de APKs");
            tabControl.TabPages.Add(tabApks);

            btnInstalarApk = new Button();
            btnInstalarApk.Text = "Instalar APK";
            btnInstalarApk.Location = new Point(20, 20);
            btnInstalarApk.Size = new Size(150, 40);
            btnInstalarApk.Click += new EventHandler(this.btnInstalarApk_Click);
            tabApks.Controls.Add(btnInstalarApk);

            btnCargarApps = new Button();
            btnCargarApps.Text = "🔄 Cargar Apps Instaladas";
            btnCargarApps.Location = new Point(180, 20);
            btnCargarApps.Size = new Size(180, 40);
            btnCargarApps.Click += new EventHandler(this.btnCargarApps_Click);
            tabApks.Controls.Add(btnCargarApps);

            cmbAppsInstaladas = new ComboBox();
            cmbAppsInstaladas.Location = new Point(20, 80);
            cmbAppsInstaladas.Size = new Size(340, 25);
            cmbAppsInstaladas.DropDownStyle = ComboBoxStyle.DropDownList;
            tabApks.Controls.Add(cmbAppsInstaladas);

            btnDesinstalarApk = new Button();
            btnDesinstalarApk.Text = "Desinstalar App Seleccionada";
            btnDesinstalarApk.Location = new Point(370, 75);
            btnDesinstalarApk.Size = new Size(200, 32);
            btnDesinstalarApk.Click += new EventHandler(this.btnDesinstalarApk_Click);
            tabApks.Controls.Add(btnDesinstalarApk);

            txtSalidaApks = new TextBox();
            txtSalidaApks.Location = new Point(20, 130);
            txtSalidaApks.Size = new Size(710, 430);
            txtSalidaApks.Multiline = true;
            txtSalidaApks.ScrollBars = ScrollBars.Vertical;
            txtSalidaApks.ReadOnly = true;
            txtSalidaApks.Font = new Font("Consolas", 10);
            txtSalidaApks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabApks.Controls.Add(txtSalidaApks);
        }

        private void btnCargarApps_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            cmbAppsInstaladas.Items.Clear();
            txtSalidaApks.AppendText("Cargando lista de aplicaciones (esto puede tardar unos segundos)...\r\n");
            
            // Comando para listar solo apps de usuario (-3) y mostrarlas como "package:com.ejemplo"
            string resultado = AdbHelper.RunCommand($"-s {serial} shell pm list packages -3");
            
            string[] lineas = resultado.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int contador = 0;
            foreach (string linea in lineas)
            {
                if (linea.StartsWith("package:"))
                {
                    string paquete = linea.Replace("package:", "").Trim();
                    cmbAppsInstaladas.Items.Add(paquete);
                    contador++;
                }
            }
            txtSalidaApks.AppendText($"Se cargaron {contador} aplicaciones.\r\n\r\n");
        }

        private void btnInstalarApk_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos APK (*.apk)|*.apk";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtSalidaApks.AppendText("Instalando: " + ofd.SafeFileName + "...\r\n");
                string resultado = AdbHelper.RunCommand($"-s {serial} install -r \"{ofd.FileName}\"");
                txtSalidaApks.AppendText(resultado + "\r\n\r\n");
            }
        }

        private void btnDesinstalarApk_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            if (cmbAppsInstaladas.SelectedItem == null)
            {
                MessageBox.Show("Primero haz clic en 'Cargar Apps Instaladas' y selecciona una app de la lista.");
                return;
            }

            string paquete = cmbAppsInstaladas.SelectedItem.ToString();

            if (MessageBox.Show($"¿Estás seguro de desinstalar '{paquete}' del teléfono {serial}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                txtSalidaApks.AppendText("Desinstalando: " + paquete + "...\r\n");
                string resultado = AdbHelper.RunCommand($"-s {serial} uninstall {paquete}");
                txtSalidaApks.AppendText(resultado + "\r\n\r\n");
                cmbAppsInstaladas.Items.Remove(paquete); // Quitarlo de la lista visual
            }
        }

        // =========================================================
        // PESTAÑA 4: EXPLORADOR DE ARCHIVOS (PUSH / PULL)
        // =========================================================
        private void InicializarTabArchivos()
        {
            tabArchivos = new TabPage("Archivos (Push/Pull)");
            tabControl.TabPages.Add(tabArchivos);

            btnPush = new Button();
            btnPush.Text = "Enviar a Teléfono (Push)";
            btnPush.Location = new Point(20, 20);
            btnPush.Size = new Size(200, 40);
            btnPush.Click += new EventHandler(this.btnPush_Click);
            tabArchivos.Controls.Add(btnPush);

            btnPull = new Button();
            btnPull.Text = "Traer de Teléfono (Pull)";
            btnPull.Location = new Point(230, 20);
            btnPull.Size = new Size(200, 40);
            btnPull.Click += new EventHandler(this.btnPull_Click);
            tabArchivos.Controls.Add(btnPull);

            Label lblRuta = new Label();
            lblRuta.Text = "Ruta del archivo en el teléfono (para Pull):";
            lblRuta.Location = new Point(20, 80);
            lblRuta.AutoSize = true;
            tabArchivos.Controls.Add(lblRuta);

            txtRutaAndroid = new TextBox();
            txtRutaAndroid.Location = new Point(20, 105);
            txtRutaAndroid.Size = new Size(410, 25);
            txtRutaAndroid.Text = "/sdcard/Download/";
            tabArchivos.Controls.Add(txtRutaAndroid);

            txtSalidaArchivos = new TextBox();
            txtSalidaArchivos.Location = new Point(20, 145);
            txtSalidaArchivos.Size = new Size(710, 415);
            txtSalidaArchivos.Multiline = true;
            txtSalidaArchivos.ScrollBars = ScrollBars.Vertical;
            txtSalidaArchivos.ReadOnly = true;
            txtSalidaArchivos.Font = new Font("Consolas", 10);
            txtSalidaArchivos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabArchivos.Controls.Add(txtSalidaArchivos);
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string destinoAndroid = "/sdcard/Download/";
                txtSalidaArchivos.AppendText($"Enviando {ofd.SafeFileName} a {destinoAndroid}...\r\n");
                string resultado = AdbHelper.RunCommand($"-s {serial} push \"{ofd.FileName}\" {destinoAndroid}");
                txtSalidaArchivos.AppendText(resultado + "\r\n¡Envío completado!\r\n\r\n");
            }
        }

        private void btnPull_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            string rutaAndroid = txtRutaAndroid.Text.Trim();
            if (string.IsNullOrEmpty(rutaAndroid)) { MessageBox.Show("Escribe la ruta del archivo."); return; }

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtSalidaArchivos.AppendText($"Descargando {rutaAndroid}...\r\n");
                string resultado = AdbHelper.RunCommand($"-s {serial} pull \"{rutaAndroid}\" \"{fbd.SelectedPath}\"");
                txtSalidaArchivos.AppendText(resultado + "\r\n¡Descarga completada!\r\n\r\n");
            }
        }

        // =========================================================
        // PESTAÑA 5: LOGCAT EN TIEMPO REAL
        // =========================================================
        private void InicializarTabLogcat()
        {
            tabLogcat = new TabPage("Logcat");
            tabControl.TabPages.Add(tabLogcat);

            btnIniciarLogcat = new Button();
            btnIniciarLogcat.Text = "Iniciar Logcat";
            btnIniciarLogcat.Location = new Point(20, 20);
            btnIniciarLogcat.Size = new Size(180, 40);
            btnIniciarLogcat.Click += new EventHandler(this.btnIniciarLogcat_Click);
            tabLogcat.Controls.Add(btnIniciarLogcat);

            rtbLogcat = new RichTextBox();
            rtbLogcat.Location = new Point(20, 80);
            rtbLogcat.Size = new Size(710, 480);
            rtbLogcat.ReadOnly = true;
            rtbLogcat.Font = new Font("Consolas", 9);
            rtbLogcat.BackColor = Color.Black;
            rtbLogcat.ForeColor = Color.LightGreen;
            rtbLogcat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabLogcat.Controls.Add(rtbLogcat);
        }

        private void btnIniciarLogcat_Click(object sender, EventArgs e)
        {
            string serial = ObtenerSerialSeleccionado();
            if (serial == null && !_logcatActivo) { MessageBox.Show("Selecciona un dispositivo primero."); return; }

            if (!_logcatActivo)
            {
                rtbLogcat.Clear();
                _logcatProcess = new Process();
                _logcatProcess.StartInfo.FileName = AdbHelper.AdbPath;
                // Agregamos el serial para apuntar al teléfono correcto
                _logcatProcess.StartInfo.Arguments = $"-s {serial} logcat -v brief"; 
                _logcatProcess.StartInfo.RedirectStandardOutput = true;
                _logcatProcess.StartInfo.RedirectStandardError = true;
                _logcatProcess.StartInfo.UseShellExecute = false;
                _logcatProcess.StartInfo.CreateNoWindow = true;

                _logcatProcess.OutputDataReceived += new DataReceivedEventHandler(LogcatOutputHandler);

                try
                {
                    _logcatProcess.Start();
                    _logcatProcess.BeginOutputReadLine();

                    _logcatActivo = true;
                    btnIniciarLogcat.Text = "Detener Logcat";
                    btnIniciarLogcat.BackColor = Color.LightCoral;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al iniciar Logcat: " + ex.Message);
                }
            }
            else
            {
                if (_logcatProcess != null && !_logcatProcess.HasExited)
                {
                    try { _logcatProcess.Kill(); } catch { }
                    _logcatProcess.Dispose();
                }
                _logcatActivo = false;
                btnIniciarLogcat.Text = "Iniciar Logcat";
                btnIniciarLogcat.BackColor = SystemColors.Control;
            }
        }

        private void LogcatOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                if (rtbLogcat.InvokeRequired)
                {
                    rtbLogcat.Invoke(new Action(() => AgregarLog(rtbLogcat, outLine.Data)));
                }
                else
                {
                    AgregarLog(rtbLogcat, outLine.Data);
                }
            }
        }

        private void AgregarLog(RichTextBox rtb, string texto)
        {
            if (texto.Contains(" E ") || texto.Contains(" F "))
                rtb.SelectionColor = Color.Red;
            else if (texto.Contains(" W "))
                rtb.SelectionColor = Color.Yellow;
            else if (texto.Contains(" I "))
                rtb.SelectionColor = Color.LightGreen;
            else
                rtb.SelectionColor = Color.White;

            rtb.AppendText(texto + "\n");
            rtb.ScrollToCaret();
        }
    }

    // =========================================================
    // CLASE DE AYUDA (ADB Y FASTBOOT)
    // =========================================================
    public static class AdbHelper
    {
        public static string AdbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "PlatformTools", "adb.exe");
        public static string FastbootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "PlatformTools", "fastboot.exe");

        public static string RunCommand(string arguments)
        {
            return RunProcess(AdbPath, arguments);
        }

        public static string RunFastbootCommand(string arguments)
        {
            return RunProcess(FastbootPath, arguments);
        }

        private static string RunProcess(string exePath, string arguments)
        {
            if (!File.Exists(exePath)) 
                return "Error: No se encontró " + Path.GetFileName(exePath) + " en " + exePath;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exePath,
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
                    
                    if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(output))
                        return "ERROR: " + error;
                        
                    return output + (string.IsNullOrEmpty(error) ? "" : "\nADVERTENCIA: " + error);
                }
            }
            catch (Exception ex)
            {
                return "Error crítico: " + ex.Message;
            }
        }
    }
}
