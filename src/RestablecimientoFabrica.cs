using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AndroidToolkitPro
{
    public static class RestablecimientoFabrica
    {
        // Busca adb.exe en las carpetas donde lo tienes en el proyecto
        private static string RutaAdb
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatos =
                {
                    Path.Combine(baseDir, "adb.exe"),
                    Path.Combine(baseDir, "Tools", "PlatformTools", "adb.exe"),
                    Path.Combine(baseDir, "PlatformTools", "adb.exe")
                };

                foreach (string ruta in candidatos)
                {
                    if (File.Exists(ruta)) return ruta;
                }
                return "adb";
            }
        }

        private static string EjecutarAdb(string argumentos, int esperaMs = 15000)
        {
            var info = new ProcessStartInfo
            {
                FileName = RutaAdb,
                Arguments = argumentos,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proceso = Process.Start(info))
            {
                string salida = proceso.StandardOutput.ReadToEnd();
                string error = proceso.StandardError.ReadToEnd();
                proceso.WaitForExit(esperaMs);
                return string.IsNullOrWhiteSpace(salida) ? error.Trim() : salida.Trim();
            }
        }

        private static bool HayDispositivo(out string mensaje)
        {
            string r;
            try
            {
                r = EjecutarAdb("devices");
            }
            catch (Exception)
            {
                mensaje = "No se encuentra adb.exe. Revisa la carpeta Tools/PlatformTools.";
                return false;
            }

            var lineas = r.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int autorizados = 0;
            bool noAutorizado = false;

            for (int i = 1; i < lineas.Length; i++)
            {
                if (lineas[i].EndsWith("\tdevice")) autorizados++;
                else if (lineas[i].EndsWith("\tunauthorized")) noAutorizado = true;
            }

            if (noAutorizado)
            {
                mensaje = "El teléfono no está autorizado. Acepta el aviso de depuración USB en la pantalla.";
                return false;
            }
            if (autorizados == 0)
            {
                mensaje = "No se detecta ningún teléfono. Activa la depuración USB y revisa el cable.";
                return false;
            }
            if (autorizados > 1)
            {
                mensaje = "Hay más de un dispositivo conectado. Deja solo el que vas a reparar.";
                return false;
            }
            mensaje = "Dispositivo detectado.";
            return true;
        }

        private static string Propiedad(string nombre)
        {
            return EjecutarAdb("shell getprop " + nombre);
        }

        private static int Bateria()
        {
            string r = EjecutarAdb("shell dumpsys battery");
            foreach (string linea in r.Split('\n'))
            {
                string l = linea.Trim();
                if (l.StartsWith("level:"))
                {
                    int nivel;
                    if (int.TryParse(l.Substring(6).Trim(), out nivel)) return nivel;
                }
            }
            return -1;
        }

        private static string PedirTexto(IWin32Window dueno, string titulo, string mensaje)
        {
            using (var f = new Form())
            using (var etiqueta = new Label())
            using (var caja = new TextBox())
            using (var aceptar = new Button())
            using (var cancelar = new Button())
            {
                f.Text = titulo;
                f.ClientSize = new Size(340, 130);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MinimizeBox = false;
                f.MaximizeBox = false;

                etiqueta.Text = mensaje;
                etiqueta.SetBounds(12, 12, 316, 30);

                caja.SetBounds(12, 50, 316, 24);

                aceptar.Text = "Aceptar";
                aceptar.DialogResult = DialogResult.OK;
                aceptar.SetBounds(172, 90, 75, 28);

                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(253, 90, 75, 28);

                f.Controls.AddRange(new Control[] { etiqueta, caja, aceptar, cancelar });
                f.AcceptButton = aceptar;
                f.CancelButton = cancelar;

                return f.ShowDialog(dueno) == DialogResult.OK ? caja.Text : "";
            }
        }

        // Método público: es el que llamas desde el botón
        public static void Restablecer(IWin32Window ventana)
        {
            string msg;
            if (!HayDispositivo(out msg))
            {
                MessageBox.Show(ventana, msg, "Restablecer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string modelo = Propiedad("ro.product.model");
            string android = Propiedad("ro.build.version.release");
            int bateria = Bateria();

            if (bateria >= 0 && bateria < 30)
            {
                MessageBox.Show(ventana,
                    "La batería está en " + bateria + "%. Carga el teléfono antes de continuar.",
                    "Batería baja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string resumen =
                "Teléfono: " + modelo + "\n" +
                "Android: " + android + "\n" +
                "Batería: " + (bateria >= 0 ? bateria + "%" : "desconocida") + "\n\n" +
                "Se borrarán TODOS los datos y ajustes. El sistema no se toca.\n\n" +
                "Antes de seguir confirma que:\n" +
                " - Hiciste copia de los datos.\n" +
                " - Quitaste las cuentas Google y Samsung, o recuerdas sus contraseñas.\n" +
                " - El equipo es tuyo o del cliente y lo anotaste en tu registro.\n\n" +
                "¿Continuar?";

            if (MessageBox.Show(ventana, resumen, "Restablecer de fábrica",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string escrito = PedirTexto(ventana, "Confirmación", "Escribe BORRAR para confirmar.");
            if (!string.Equals(escrito.Trim(), "BORRAR", StringComparison.Ordinal))
            {
                MessageBox.Show(ventana, "Cancelado. No se hizo ningún cambio.", "Restablecer");
                return;
            }

            // Intento 1: orden directa por ADB (en muchos Samsung sin root no tiene permiso)
            EjecutarAdb("shell am broadcast -a android.intent.action.FACTORY_RESET -p android");
            Thread.Sleep(15000);

            string estado;
            if (!HayDispositivo(out estado))
            {
                MessageBox.Show(ventana,
                    "El teléfono se desconectó: parece que el restablecimiento está en curso.\n" +
                    "No lo desconectes ni lo apagues. Cuando termine, configúralo de nuevo.",
                    "Restableciendo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Intento 2: recovery, confirmando el borrado en el teléfono
            EjecutarAdb("reboot recovery");
            MessageBox.Show(ventana,
                "El teléfono no aceptó el borrado directo, así que se reinicia en recovery.\n\n" +
                "En la pantalla del teléfono:\n" +
                "1. Muévete con volumen arriba y abajo.\n" +
                "2. Elige 'Wipe data/factory reset' y confirma con encendido.\n" +
                "3. Confirma 'Factory data reset'.\n" +
                "4. Al terminar, elige 'Reboot system now'.",
                "Siguiente paso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
