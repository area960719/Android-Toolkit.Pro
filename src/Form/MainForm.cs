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

        // Pestaña Herramientas Avanzadas
        private TabPage tabAvanzado;
        private Button btnGetvar;
        private Button btnFlashBoot;
        private Button btnFlashRecovery;
        private Button btnSideload;
        private Button btnRestablecerFabrica; // NUEVO BOTÓN
        private TextBox txtSalidaAvanzado;

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
        // CONSTRUCTOR
        // =========================================================
        public MainForm()
        {
            this.Text = "Android Toolkit Pro";
            this.Size = new Size(750, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 450);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);

            InicializarTabFastboot();
            InicializarTabAvanzado();
            InicializarTabApks();
            InicializarTabArchivos();
            InicializarTabLogcat();
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
            txtSalidaFastboot.Size = new Size(680, 430);
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
            
            if (resultado.Contains("Error: No se encontró"))
            {
                txtSalidaFastboot.AppendText(resultado + "\r\n");
                return;
            }
            
            txtSalidaFastboot.AppendText("--- RESPUESTA DE FASTBOOT ---\r\n" + resultado + "\r\n");
        }

        // =========================================================
        // PESTAÑA 2: HERRAMIENTAS AVANZADAS
        // =========================================================
        private void InicializarTabAvanzado()
        {
            tabAvanzado = new TabPage("Herramientas Avanzadas");
            tabControl.TabPages.Add(tabAvanzado);

            // Fila 1 de botones
            btnGetvar = new Button();
            btnGetvar.Text = "Info del Dispositivo (getvar all)";
            btnGetvar.Location = new Point(20, 20);
            btnGetvar.Size = new Size(200, 40);
            btnGetvar.Click += new EventHandler(this.btnGetvar_Click);
            tabAvanzado.Controls.Add(btnGetvar);

            btnFlashBoot = new Button();
            btnFlashBoot.Text = "Flashear Boot.img";
            btnFlashBoot.Location = new Point(230, 20);
            btnFlashBoot.Size = new Size(150, 40);
            btnFlashBoot.Click += new EventHandler(this.btnFlashBoot_Click);
            tabAvanzado.Controls.Add(btnFlashBoot);

            btnFlashRecovery = new Button();
            btnFlashRecovery.Text = "Flashear Recovery.img";
            btnFlashRecovery.Location = new Point(390, 20);
            btnFlashRecovery.Size = new Size(150, 40);
            btnFlashRecovery.Click += new EventHandler(this.btnFlashRecovery_Click);
            tabAvanzado.Controls.Add(btnFlashRecovery);

            // Fila 2 de botones
            btnSideload = new Button();
            btnSideload.Text = "ADB Sideload (Recovery)";
            btnSideload.Location = new Point(20, 70);
            btnSideload.Size = new Size(200, 40);
            btnSideload.Click += new EventHandler(this.btnSideload_Click);
            tabAvanzado.Controls.Add(btnSideload);

            // NUEVO BOTÓN: RESTABLECER FÁBRICA
            btnRestablecerFabrica = new Button();
            btnRestablecerFabrica.Text = "⚠️ Restablecer Fábrica";
            btnRestablecerFabrica.Location = new Point(230, 70);
            btnRestablecerFabrica.Size = new Size(180, 40);
            btnRestablecerFabrica.BackColor = Color.LightCoral;
            btnRestablecerFabrica.Click += new EventHandler(this.btnRestablecerFabrica_Click);
            tabAvanzado.Controls.Add(btnRestablecerFabrica);

            // Caja de texto de salida (se ajusta un poco hacia abajo para hacer espacio)
            txtSalidaAvanzado = new TextBox();
            txtSalidaAvanzado.Location = new Point(20, 130);
            txtSalidaAvanzado.Size = new Size(680, 380);
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
            string resultado = AdbHelper.RunFastbootCommand("getvar all");
            txtSalidaAvanzado.AppendText(resultado + "\r\n");
        }

        private void btnFlashBoot_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imágenes (*.img)|*.img";
            ofd.Title = "Selecciona el archivo boot.img";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                DialogResult confirm = MessageBox.Show(
                    "⚠️ ADVERTENCIA: Vas a flashear la partición BOOT.\n\n" +
                    "Si el archivo es incorrecto, el teléfono podría no encender.\n\n" +
                    "¿Estás seguro de continuar?",
                    "Confirmar Flasheo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    txtSalidaAvanzado.AppendText($"Flasheando {ofd.SafeFileName} en la partición boot...\r\n");
                    string resultado = AdbHelper.RunFastbootCommand($"flash boot \"{ofd.FileName}\"");
                    txtSalidaAvanzado.AppendText(resultado + "\r\n\r\n");
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
                DialogResult confirm = MessageBox.Show(
                    "⚠️ ADVERTENCIA: Vas a flashear la partición RECOVERY.\n\n" +
                    "Asegúrate de que el archivo sea compatible con tu modelo exacto.\n\n" +
                    "¿Estás seguro de continuar?",
                    "Confirmar Flasheo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    txtSalidaAvanzado.AppendText($"Flasheando {ofd.SafeFileName} en la partición recovery...\r\n");
                    string resultado = AdbHelper.RunFastbootCommand($"flash recovery \"{ofd.FileName}\"");
                    txtSalidaAvanzado.AppendText(resultado + "\r\n\r\n");
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
                txtSalidaAvanzado.AppendText("Asegúrate de que el teléfono esté en Recovery y hayas seleccionado 'Apply update from ADB'.\r\n\r\n");
                string resultado = AdbHelper.RunCommand($"sideload \"{ofd.FileName}\"");
                txtSalidaAvanzado.AppendText(resultado + "\r\n\r\n");
            }
        }

        // =========================================================
        // EVENTO DEL NUEVO BOTÓN: RESTABLECER FÁBRICA
        // =========================================================
        private void btnRestablecerFabrica_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "🚨 ¡ADVERTENCIA EXTREMA! 🚨\n\n" +
                "Esta acción borrará TODOS los datos del teléfono (Fotos, contactos, aplicaciones, etc.).\n\n" +
                "El dispositivo debe estar en modo Fastboot.\n\n" +
                "¿Estás COMPLETAMENTE seguro de que deseas continuar?",
                "Confirmar Restablecimiento de Fábrica", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Stop);

            if (confirm == DialogResult.Yes)
            {
                txtSalidaAvanzado.AppendText("Iniciando restablecimiento de fábrica (Wipe de datos)...\r\n");
                
                // OPCIÓN A: Usar el comando nativo de Fastboot para borrar datos (Recomendado)
                string resultado = AdbHelper.RunFastbootCommand("-w");
                txtSalidaAvanzado.AppendText(resultado + "\r\n\r\n");

                // OPCIÓN B: Si quieres usar tu archivo RestablecimientoFabrica.cs, 
                // borra las dos líneas de arriba (OPCIÓN A) y descomenta esto:
                /*
                try {
                    RestablecimientoFabrica reset = new RestablecimientoFabrica();
                    reset.Ejecutar(); // Cambia "Ejecutar" por el método real que hayas creado
                    txtSalidaAvanzado.AppendText("Restablecimiento completado usando la clase personalizada.\r\n");
                }
                catch (Exception ex) {
                    txtSalidaAvanzado.AppendText("Error al llamar a RestablecimientoFabrica: " + ex.Message + "\r\n");
                }
                */
            }
        }

        // =========================================================
        // PESTAÑA 3: GESTOR DE APKS
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
            txtSalidaApks.Size = new Size(680, 365);
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
            if (string.IsNullOrEmpty(paquete)) { MessageBox.Show("Escribe el nombre del paquete."); return; }

            txtSalidaApks.AppendText("Desinstalando: " + paquete + "...\r\n");
            string resultado = AdbHelper.RunCommand($"uninstall {paquete}");
            txtSalidaApks.AppendText(resultado + "\r\n\r\n");
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
            txtSalidaArchivos.Size = new Size(680, 365);
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
            if (string.IsNullOrEmpty(rutaAndroid)) { MessageBox.Show("Escribe la ruta del archivo."); return; }

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
            rtbLogcat.Size = new Size(680, 430);
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
