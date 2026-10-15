# 🚀 [Android-Toolkit.Pro. L-A]

![Estado](https://img.shields.io/badge/Estado-Activo-brightgreen)
![Plataforma](https://img.shields.io/badge/Plataforma-Windows-blue)
![Lenguaje](https://img.shields.io/badge/Lenguaje-C%23-purple)
![Licencia](https://img.shields.io/badge/Licencia-MIT-green)

Una potente herramienta de escritorio desarrollada en **C#** que integra la funcionalidad de **Scrcpy** para ofrecer una experiencia de control y espejo de dispositivos Android mucho más intuitiva, rápida y fácil de usar.

## 📸 Capturas de Pantalla
*(Aquí puedes arrastrar y soltar imágenes o GIFs de tu herramienta funcionando)*
![Interfaz de la Herramienta](ruta/a/tu/imagen.png)

## ✨ Características Principales
*   **Interfaz Gráfica (GUI):** Controla tu dispositivo Android sin necesidad de usar la línea de comandos.
*   **Conexión Rápida:** Inicia el espejo con un solo clic.
*   **Integración Total:** Incluye todos los binarios y dependencias necesarias de Scrcpy (FFmpeg, SDL, etc.).
*   **Bajo Consumo:** Optimizado para funcionar sin consumir demasiados recursos del sistema.
*   **Personalizable:** [Añade aquí cualquier función extra que hayas creado, como atajos de teclado, grabación de pantalla, etc.]

## 🛠️ Requisitos del Sistema
*   **Sistema Operativo:** Windows 10 / 11 (64-bit).
*   **Framework:** .NET [Versión que uses, ej. 6.0 o 8.0] Runtime.
*   **Dispositivo Android:** Android 5.0 (API 21) o superior.
*   **Controladores:** Es posible que necesites instalar los controladores USB de tu fabricante (Samsung, Xiaomi, etc.) para que la depuración USB funcione correctamente.

## 📥 Instalación y Uso

1.  Ve a la sección de [**Releases**](enlace/a/tus/releases) y descarga la última versión de la herramienta (`.zip` o `.exe`).
2.  Descomprime el archivo en una carpeta de tu preferencia (si descargaste el `.zip`).
3.  En tu teléfono Android, ve a **Ajustes > Acerca del teléfono** y toca 7 veces "Número de compilación" para activar las **Opciones de desarrollador**.
4.  Activa la **Depuración USB** en las Opciones de desarrollador.
5.  Conecta tu teléfono a la PC mediante un cable USB. Acepta el mensaje de "Permitir depuración USB" en tu teléfono.
6.  Ejecuta **`[NombreDeTuApp].exe`** y ¡listo! Haz clic en "Iniciar" para ver tu pantalla en la PC.

## 💻 Compilación desde el código fuente
Si deseas compilar el proyecto tú mismo:

1.  Clona este repositorio:
    ```bash
    git clone https://github.com/TuUsuario/TuRepositorio.git
    ```
2.  Abre la solución (`.sln`) en **Visual Studio 2022**.
3.  Restaura los paquetes NuGet y compila el proyecto.
4.  Asegúrate de que los archivos de Scrcpy (`scrcpy.exe`, `SDL3.dll`, `avcodec-63.dll`, etc.) estén en la carpeta de salida (`bin/Debug` o `bin/Release`).

## 🤝 Créditos y Agradecimientos
Este proyecto no sería posible sin el increíble trabajo del equipo de **Genymobile**.
*   **Scrcpy:** [https://github.com/Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) (Licencia Apache 2.0).
*   **FFmpeg:** Por las librerías de decodificación de video.
*   **SDL:** Por el renderizado de la ventana.

## 📄 Licencia
Este proyecto está bajo la Licencia [MIT / Apache 2.0 / etc.]. Consulta el archivo `LICENSE` para más detalles.

---
Desarrollado con ❤️ por [Tu Nombre/Usuario de GitHub]
