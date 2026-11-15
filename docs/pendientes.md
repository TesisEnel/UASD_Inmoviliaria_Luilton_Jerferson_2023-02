# Pendientes para producto completo
1. Validar SQL en SQL Server/Azure y contrastar con el archivo original. Pruebas integrales y concurrencia.
2. Triggers notificaciones de vendedores/reportes, CHECK restantes y coherencia de roles/perfiles.
3. CRUD de propiedades y envio/consulta de solicitudes implementados. Pendientes: decisiones sobre solicitudes, imagenes y Blob Storage, favoritos, busquedas y visitas.
4. Reservas sin solapamientos, contratos academicos, cierre y pagos simulados implementados. Pendientes: mensajeria, creacion/resolucion de reportes y politicas de anulacion de pagos.
5. Forms implementa login administrativo, aprobacion/rechazo de vendedores y consulta de reportes. Android y APK firmado pendientes. Blazor incluye contratos/pagos academicos y notificaciones, ademas del recorrido de solicitudes, visitas y reservas.
6. Suspension/reactivacion administrativa, endpoints de auditoria y matriz completa de permisos.
7. Registro/login: rate limit especifico, recuperacion, bloqueo, verificacion, revocacion, normalizacion robusta y auditoria.
8. Unicidad de documento entre perfiles requiere pruebas concurrentes y/o restriccion de identidad central revisada.
9. Publicaciones concurrentes: definir anuncios activos por propiedad y restricciones antes de produccion.
10. CI de equipo, PR y revision humana. No se incluyen commits inventados.
11. OpenAPI/Scalar disponibles en Development; ampliar documentacion y pruebas al agregar nuevos endpoints.
