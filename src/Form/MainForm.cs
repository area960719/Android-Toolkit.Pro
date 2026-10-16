using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace AndroidToolkitPro
{
    public class MainForm : Form
    {
        // Declaración de los controles de la interfaz
        private Button btnEjecutarFastboot;
        private TextBox txtSalida;
        private Label lblTitulo;

        public MainForm()
        {
            // Configuración básica de la ventana
            this.Text = "Android Toolkit Pro - Probador de Fastboot";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1. Título
            lblTitulo = new Label();
            lblTitulo.Text = "Herramienta de diagnóstico Fastboot";
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 20);
            lblTitulo.AutoSize = true;
            this.Controls.Add(lblTitulo);

            // 2. Botón para ejecutar Fastboot
            btnEjecutarFastboot = new Button();
            btnEjecutarFastboot.Text = "Ejecutar 'fastboot devices'";
            btnEjecutarFastboot.Location = new Point(20, 60);
            btnEjecutarFastboot.Size = new Size(200, 40);
            btnEjecutarFastboot.Click += new EventHandler(this.btnEjecutarFastboot_Click);
            this.Controls.Add(btnEjecutarFastboot);

            // 3. Caja de texto para ver la respuesta
            txtSalida = new TextBox();
            txtSalida.Location = new Point(20, 120);
            txtSalida.Size = new Size(540, 260);
            txtSalida.Multiline = true;
            txtSalida.ScrollBars = ScrollBars.Vertical;
            txtSalida.ReadOnly = true;
            txtSalida.Font = new Font("Consolas", 10);
            this.Controls.Add(txtSalida);
        }

        // Evento que se ejecuta al presionar el botón
        private void btnEjecutarFastboot_Click(object sender, EventArgs e)
        {
            txtSalida.Clear();
            txtSalida.AppendText("Iniciando prueba de Fastboot...\r\n\r\n");

            // 1. Obtener la ruta donde se está ejecutando el .exe actual
            // AppDomain.CurrentDomain.BaseDirectory siempre apunta a la carpeta donde está tu programa
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            
            // 2. Construir la ruta respetando tu estructura de carpetas (Tools/PlatformTools)
            string rutaFastboot = Path.Combine(rutaBase, "Tools", "PlatformTools", "fastboot.exe");

            txtSalida.AppendText("Buscando fastboot.exe en: " + rutaFastboot + "\r\n");

            // 3. Verificar si el archivo existe
            if (!File.Exists(rutaFastboot))
            {
                txtSalida.AppendText("\r\n❌ ERROR: No se encontró fastboot.exe en la ruta especificada.\r\n");
                txtSalida.AppendText("Asegúrate de que la carpeta 'Tools/PlatformTools' esté junto al .exe de tu programa.\r\n");
                return;
            }

            txtSalida.AppendText("✅ Archivo encontrado. Ejecutando comando...\r\n\r\n");

            // 4. Configurar el proceso para ejecutar fastboot
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = rutaFastboot,
                Arguments = "devices", // Este comando lista los dispositivos conectados en modo fastboot
                RedirectStandardOutput = true, // Capturar la respuesta normal
                RedirectStandardError = true,  // Capturar errores
                UseShellExecute = false,
                CreateNoWindow = true // Para que no salga la ventana negra de CMD
            };

            try
            {
                using (Process proceso = Process.Start(psi))
                {
                    // Leer la salida y los errores
                    string salida = proceso.StandardOutput.ReadToEnd();
                    string error = proceso.StandardError.ReadToEnd();
                    
                    proceso.WaitForExit();

                    // Mostrar resultados en la caja de texto
                    if (!string.IsNullOrEmpty(salida))
                    {
                        txtSalida.AppendText("--- RESPUESTA DE FASTBOOT ---\r\n");
                        txtSalida.AppendText(salida + "\r\n");
                    }
                    else
                    {
                        txtSalida.AppendText("Fastboot no devolvió ningún dispositivo. (Revisa que el teléfono esté conectado en modo Fastboot).\r\n");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        txtSalida.AppendText("\r\n--- ERRORES ---\r\n");
                        txtSalida.AppendText(error + "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                // 5. Capturar errores críticos (como problemas de 32/64 bits)
                txtSalida.AppendText("\r\n❌ ERROR CRÍTICO AL EJECUTAR FASTBOOT:\r\n");
                txtSalida.AppendText(ex.Message + "\r\n");
                txtSalida.AppendText("\r\nSugerencia: Si el error menciona 'BadImageFormatException', asegúrate de estar compilando tu proyecto para x64 (64 bits).");
            }
        }
    }
}
