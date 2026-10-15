# 🚀 Android-Toolkit.Pro L-A

![Estado](https://img.shields.io/badge/Estado-Activo-brightgreen)
![Plataforma](https://img.shields.io/badge/Plataforma-Windows-blue)
![Lenguaje](https://img.shields.io/badge/Lenguaje-C%23-purple)
![Licencia](https://img.shields.io/badge/Licencia-MIT-green)

Una potente herramienta de escritorio desarrollada en **C#** que integra la funcionalidad de **Scrcpy** para ofrecer una experiencia de control y espejo de dispositivos Android mucho más intuitiva, rápida y fácil de usar.

## ✨ Características Principales
*   **Interfaz Gráfica (GUI):** Controla tu dispositivo Android sin necesidad de usar la línea de comandos.
*   **Conexión Rápida:** Inicia el espejo con un solo clic.
*   **Integración Total:** Incluye todos los binarios y dependencias necesarias de Scrcpy (FFmpeg, SDL, etc.).
*   **Bajo Consumo:** Optimizado para funcionar sin consumir demasiados recursos del sistema.

## 📸 Capturas de Pantalla
*(Aquí puedes arrastrar y soltar imágenes o GIFs de tu herramienta funcionando)*
![Interfaz de Android-Toolkit.Pro](ruta/a/tu/imagen.png)

## 🛠️ Requisitos del Sistema
*   **Sistema Operativo:** Windows 10 / 11 (64-bit).
*   **Framework:** .NET Framework 4.5.2 en adelante.
*   **Dispositivo Android:** Android 5.0 (API 21) o superior.
*   **Controladores:** Es posible que necesites instalar los controladores USB de tu fabricante (Samsung, Xiaomi, etc.) para que la depuración USB funcione correctamente.

## 📥 Instalación y Uso

1.  Ve a la sección de [**Releases**](enlace/a/tus/releases) y descarga la última versión de la herramienta (`.zip` o `.exe`).
2.  Descomprime el archivo en una carpeta de tu preferencia (si descargaste el `.zip`).
3.  En tu teléfono Android, ve a **Ajustes > Acerca del teléfono** y toca 7 veces "Número de compilación" para activar las **Opciones de desarrollador**.
4.  Activa la **Depuración USB** en las Opciones de desarrollador.
5.  Conecta tu teléfono a la PC mediante un cable USB. Acepta el mensaje de "Permitir depuración USB" en tu teléfono.
6.  Ejecuta **`Android-Toolkit.Pro.exe`** y ¡listo! Haz clic en "Iniciar" para ver tu pantalla en la PC.

## 💻 Compilación desde el código fuente (Para Desarrolladores)
Si deseas compilar el proyecto tú mismo:

1.  Clona este repositorio:
    ```bash
    git clone https://github.com/area.960719/Android-Toolkit.Pro.git
    ```
2.  Abre la solución (`.sln`) en **Visual Studio 2022**.
3.  Asegúrate de tener instalado el SDK de .NET Framework 4.5.2 o superior.
4.  **Importante:** Verifica que todos los archivos de Scrcpy (`scrcpy.exe`, `SDL3.dll`, `avcodec-63.dll`, etc.) estén en la carpeta del proyecto y tengan la propiedad **"Copiar en el directorio de salida"** configurada como *"Copiar si es más reciente"* en Visual Studio.
5.  Compila la solución (F6) y ejecuta el proyecto (F5).

## 🚨 Solución de Problemas Frecuentes

*   **El programa se cierra al iniciar o da error de DLL:** Asegúrate de que los archivos `.dll` de Scrcpy estén en la misma carpeta que tu ejecutable.
*   **El teléfono no aparece / Error de ADB:** Desconecta y vuelve a conectar el cable USB. Revisa que la "Depuración USB" siga activada. Si el problema persiste, abre una terminal en la carpeta y ejecuta `adb kill-server` y luego `adb start-server`.
*   **Pantalla negra al iniciar el espejo:** Algunos teléfonos requieren que desbloquees la pantalla antes de iniciar la conexión, o que actives la opción "Simular ubicaciones falsas" en opciones de desarrollador.

## 🤝 Créditos y Agradecimientos
Este proyecto no sería posible sin el increíble trabajo del equipo de **Genymobile**.
*   **Scrcpy:** [https://github.com/Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) (Licencia Apache 2.0).
*   **FFmpeg:** Por las librerías de decodificación de video.
*   **SDL:** Por el renderizado de la ventana.

## 📄 Licencia
Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.

---
## 👨‍💻 Autor
Desarrollado por **Luis Alberto**
*   **GitHub:** [@area.960719](https://github.com/area.960719)
*   **Contacto:** # 55745251
