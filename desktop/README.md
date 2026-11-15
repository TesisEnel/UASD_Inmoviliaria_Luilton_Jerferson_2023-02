# Administracion Windows Forms

Cliente Windows Forms incluido en Inmobiliaria.sln, requiere Windows y .NET SDK 10 con soporte Windows Desktop.

Con la API iniciada:

```powershell
dotnet run --project .\desktop\Inmobiliaria.Admin
```

La URL API predeterminada es http://localhost:5180; puede cambiarse en la pantalla de inicio o mediante INMOBILIARIA_API_URL. APIs remotas requieren HTTPS. Introducir el correo y contrasena de un administrador de LA MISMA base que usa esa API. No usar la contrasena SQL: se trata de la cuenta de usuario de la aplicacion. No hay credenciales incluidas ni acceso SQL desde Forms.

Vendedores pendientes: muestra ID, nombre y correo, permite aprobar o rechazar el seleccionado, exige motivo de rechazo y confirmacion. La API valida cuenta activa y nivel administrativo, conserva historial y notifica al vendedor. Reportes permite consultar hasta 200 registros; su resolucion y las suspensiones siguen pendientes.

El token vive solamente en memoria y se elimina al cerrar sesion. La contrasena se limpia despues del intento de login. Al expirar la sesion debe iniciarse otra vez. El cliente no guarda usuarios, contrasenas ni tokens en archivos. Cerrar sesion permite conectar a otra API.
