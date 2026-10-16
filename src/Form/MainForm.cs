using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace AndroidToolkitPro
{
    public class MainForm : Form
    {
        // =========================================================
        // CONTROLES DE LA INTERFAZ
        // =========================================================
        private TabControl tabControl;

        // Pestaña Fastboot
        private TabPage tabFastboot;
        private Button btnEjecutarFastboot;
        private TextBox txtSalidaFastboot;

        // Pestaña APKs
        private TabPage tabApks;
        private Button btnInstalarApk;
        private Button btnDesinstalarApk;
        private TextBox txtPackageName;
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
        // CONSTRUCTOR (Aquí se crea toda la interfaz visual)
        // =========================================================
        public MainForm()
        {
            this.Text = "Android Toolkit Pro";
            this.Size = new Size(720, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 450);

            // Crear el control de pestañas
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);

            // Inicializar cada pestaña
            InicializarTabFastboot();
            InicializarTabApks();
            InicializarTabArchivos();
            InicializarTabLogcat();
        }

        // =========================================================
        // PESTAÑA 1: FASTBOOT (Tu código original adaptado)
        // =========================================================
        private void InicializarTabFastboot()
        {
            tabFastboot = new TabPage("Fastboot");
            tabControl.TabPages.Add(tabFastboot);

            btnEjecutarFastboot = new Button();
            btnEjecutarFastboot.Text = "Ejecutar 'fastboot devices'";
            btnEjecutarFastboot.Location = new Point(20, 20);
            btnEjecutarFastboot.Size = new Size(220, 40);
            btnEjecutarFastboot.Click += new EventHandler(this.btnEjecutarFastboot_Click);
            tabFastboot.Controls.Add(btnEjecutarFastboot);

            txtSalidaFastboot = new TextBox();
            txtSalidaFastboot.Location = new Point(20, 80);
            txtSalidaFastboot.Size = new Size(650, 400);
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

            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            string rutaFastboot = Path.Combine(rutaBase, "Tools", "PlatformTools", "fastboot.exe");

            txtSalidaFastboot.AppendText("Buscando fastboot.exe en: " + rutaFastboot + "\r\n");

            if (!File.Exists(rutaFastboot))
            {
                txtSalidaFastboot.AppendText("\r\n❌ ERROR: No se encontró fastboot.exe en la ruta especificada.\r\n");
                return;
            }

            txtSalidaFastboot.AppendText("✅ Archivo encontrado. Ejecutando comando...\r\n\r\n");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = rutaFastboot,
                Arguments = "devices",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process proceso = Process.Start(psi))
                {
                    string salida = proceso.StandardOutput.ReadToEnd();
                    string error = proceso.StandardError.ReadToEnd();
                    proceso.WaitForExit();

                    if (!string.IsNullOrEmpty(salida))
                        txtSalidaFastboot.AppendText("--- RESPUESTA DE FASTBOOT ---\r\n" + salida + "\r\n");
                    else
                        txtSalidaFastboot.AppendText("Fastboot no devolvió ningún dispositivo. (Revisa la conexión).\r\n");

                    if (!string.IsNullOrEmpty(error))
                        txtSalidaFastboot.AppendText("\r\n--- ERRORES ---\r\n" + error + "\r\n");
                }
            }
            catch (Exception ex)
            {
                txtSalidaFastboot.AppendText("\r\n❌ ERROR CRÍTICO:\r\n" + ex.Message);
            }
        }

        // =========================================================
        // PESTAÑA 2: GESTOR DE APKS
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

            btnDesinstalarApk = new Button();
            btnDesinstalarApk.Text = "Desinstalar APK";
            btnDesinstalarApk.Location = new Point(180, 20);
            btnDesinstalarApk.Size = new Size(150, 40);
            btnDesinstalarApk.Click += new EventHandler(this.btnDesinstalarApk_Click);
            tabApks.Controls.Add(btnDesinstalarApk);

            Label lblPackage = new Label();
            lblPackage.Text = "Paquete a desinstalar:";
            lblPackage.Location = new Point(20, 80);
            lblPackage.AutoSize = true;
            tabApks.Controls.Add(lblPackage);

            txtPackageName = new TextBox();
            txtPackageName.Location = new Point(20, 105);
            txtPackageName.Size = new Size(310, 25);
            txtPackageName.Text = "com.ejemplo.app";
            tabApks.Controls.Add(txtPackageName);

            txtSalidaApks = new TextBox();
            txtSalidaApks.Location = new Point(20, 145);
            txtSalidaApks.Size = new Size(650, 335);
            txtSalidaApks.Multiline = true;
            txtSalidaApks.ScrollBars = ScrollBars.Vertical;
            txtSalidaApks.ReadOnly = true;
            txtSalidaApks.Font = new Font("Consolas", 10);
            txtSalidaApks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabApks.Controls.Add(txtSalidaApks);
        }

        private void btnInstalarApk_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos APK (*.apk)|*.apk";
            ofd.Title = "Selecciona la APK para instalar";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtSalidaApks.AppendText("Instalando: " + ofd.SafeFileName + "...\r\n");
                string resultado = AdbHelper.RunCommand($"install -r \"{ofd.FileName}\"");
                txtSalidaApks.AppendText(resultado + "\r\n\r\n");
            }
        }

        private void btnDesinstalarApk_Click(object sender, EventArgs e)
        {
            string paquete = txtPackageName.Text.Trim();

            if (string.IsNullOrEmpty(paquete))
            {
                MessageBox.Show("Escribe el nombre del paquete (ej: com.whatsapp)");
                return;
            }

            txtSalidaApks.AppendText("Desinstalando: " + paquete + "...\r\n");
            string resultado = AdbHelper.RunCommand($"uninstall {paquete}");
            txtSalidaApks.AppendText(resultado + "\r\n\r\n");
        }

        // =========================================================
        // PESTAÑA 3: EXPLORADOR DE ARCHIVOS (PUSH / PULL)
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
            txtSalidaArchivos.Size = new Size(650, 335);
            txtSalidaArchivos.Multiline = true;
            txtSalidaArchivos.ScrollBars = ScrollBars.Vertical;
            txtSalidaArchivos.ReadOnly = true;
            txtSalidaArchivos.Font = new Font("Consolas", 10);
            txtSalidaArchivos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabArchivos.Controls.Add(txtSalidaArchivos);
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Selecciona el archivo para enviar al teléfono";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string destinoAndroid = "/sdcard/Download/";
                txtSalidaArchivos.AppendText($"Enviando {ofd.SafeFileName} a {destinoAndroid}...\r\n");
                string resultado = AdbHelper.RunCommand($"push \"{ofd.FileName}\" {destinoAndroid}");
                txtSalidaArchivos.AppendText(resultado + "\r\n¡Envío completado!\r\n\r\n");
            }
        }

        private void btnPull_Click(object sender, EventArgs e)
        {
            string rutaAndroid = txtRutaAndroid.Text.Trim();

            if (string.IsNullOrEmpty(rutaAndroid))
            {
                MessageBox.Show("Escribe la ruta del archivo en el teléfono (ej: /sdcard/Download/foto.jpg)");
                return;
            }

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Selecciona la carpeta donde guardar el archivo en tu PC";

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtSalidaArchivos.AppendText($"Descargando {rutaAndroid}...\r\n");
                string resultado = AdbHelper.RunCommand($"pull \"{rutaAndroid}\" \"{fbd.SelectedPath}\"");
                txtSalidaArchivos.AppendText(resultado + "\r\n¡Descarga completada!\r\n\r\n");
            }
        }

        // =========================================================
        // PESTAÑA 4: LOGCAT EN TIEMPO REAL
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
            rtbLogcat.Size = new Size(650, 400);
            rtbLogcat.ReadOnly = true;
            rtbLogcat.Font = new Font("Consolas", 9);
            rtbLogcat.BackColor = Color.Black;
            rtbLogcat.ForeColor = Color.LightGreen;
            rtbLogcat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabLogcat.Controls.Add(rtbLogcat);
        }

        private void btnIniciarLogcat_Click(object sender, EventArgs e)
        {
            if (!_logcatActivo)
            {
                rtbLogcat.Clear();
                _logcatProcess = new Process();
                _logcatProcess.StartInfo.FileName = AdbHelper.AdbPath;
                _logcatProcess.StartInfo.Arguments = "logcat -v brief";
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
            // Colorear según el tipo de mensaje
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
    // CLASE DE AYUDA (AdbHelper) - Incluida aquí para que no crees otro archivo
    // =========================================================
    public static class AdbHelper
    {
        public static string AdbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "PlatformTools", "adb.exe");

        public static string RunCommand(string arguments)
        {
            if (!File.Exists(AdbPath)) 
                return "Error: No se encontró adb.exe en " + AdbPath;

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
