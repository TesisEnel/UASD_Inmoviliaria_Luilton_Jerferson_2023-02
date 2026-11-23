# Inmobiliaria para Android

Cliente nativo en Kotlin y Jetpack Compose para compradores y vendedores. Usa los mismos endpoints `/api/v1` y permisos que Blazor. Incluye catálogo, galería, registro, inicio de sesión, gestión de propiedades y publicaciones, solicitudes, visitas, reservas, contratos académicos, pagos simulados y notificaciones dentro de la app.

## Abrir y compilar

Abre la carpeta `android` en Android Studio y selecciona el JDK integrado de Android Studio. Instala Android SDK 37 si falta. La aplicación se instala en Android 8.0 (API 26) o posterior.

Desde la raíz del repositorio en Windows:

```powershell
Set-Location android
.\gradlew.bat assembleDebug lintDebug
```

El APK de depuración se genera en `app/build/outputs/apk/debug/app-debug.apk`. En Android Studio puedes instalarlo con **Run**.

## Conectar con la API

- **Emulador.** Inicia la API local en el puerto 5180 según el [recorrido local](../docs/recorrido-local.md). La dirección inicial del APK de depuración es `http://10.0.2.2:5180/`.
- **Teléfono.** En **Menú → Conexión**, introduce la URL HTTPS de la API. Para probar el APK de depuración por USB, ejecuta `adb reverse tcp:5180 tcp:5180` y usa `http://127.0.0.1:5180/`.
- **Release.** Configura una URL HTTPS con certificado válido. Para distribuir el APK release se requiere firmarlo con la clave de distribución.

La configuración del servidor se guarda en el dispositivo. La sesión JWT se conserva en memoria y termina al cerrar sesión, cambiar de servidor o cerrar el proceso. La aprobación de vendedores se realiza desde el panel administrativo existente.

## Recorrido de prueba

1. Registra un vendedor. Tras la aprobación administrativa, inicia sesión, crea una propiedad, agrega fotos y publica el alquiler.
2. Registra un comprador. Abre la publicación desde el catálogo, envía una solicitud y consulta su estado.
3. Como vendedor, acepta la solicitud y propone una visita y una reserva. Como comprador, confirma la asistencia y la reserva.
4. Como vendedor, crea el contrato académico. Como comprador, confirma el acuerdo. Como vendedor, registra un pago simulado y finaliza el alquiler.
5. Consulta las notificaciones, abre una solicitud desde su enlace y marca la notificación como leída. Las fechas de visitas, reservas y contratos se muestran en hora dominicana.
