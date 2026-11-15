# Cierre academico del alquiler

## Recorrido

1. Vendedor acepta la solicitud, propone una reserva y el comprador la confirma.
2. En Contrato y pagos, el vendedor crea una propuesta academica. Se copian las fechas de la reserva y el precio mensual/moneda de la publicacion. Se permite un contrato por solicitud.
3. El comprador revisa los datos y confirma expresamente la simulacion. No constituye firma electronica ni documento legal.
4. En una transaccion se activa el contrato, convierte la reserva, completa la solicitud, marca la propiedad Alquilada y pausa sus anuncios. Las visitas futuras pendientes/confirmadas de esa propiedad se cancelan. La propiedad deja de estar disponible desde esta confirmacion, incluso si el periodo copiado empieza en el futuro.
5. El vendedor puede registrar pagos simulados para el contrato activo: monto positivo, fecha no futura, concepto y referencia. Se usa la moneda del contrato. Una referencia no se repite dentro de la misma solicitud, incluso con envios simultaneos. No existe conexion con bancos ni cobro de tarjetas. Se conserva el historial; no hay anulacion/reembolso de estos registros en esta etapa.
6. El vendedor finaliza expresamente el alquiler academico. Puede representar una terminacion anticipada; no hay finalizacion automatica. La propiedad vuelve a Disponible, conservando anuncios pausados, pagos y contrato Finalizado. El vendedor decide si reactiva los anuncios. No se firma ningun acuerdo real ni se transfieren fondos.

Cancelar un borrador conserva contrato y cancela su reserva. Cancelar la reserva antes de activacion tambien cancela su borrador. No se reemplaza el contrato de esa solicitud, para conservar trazabilidad. La finalizacion de un contrato activo es exclusiva del vendedor; no se permite que compradores ajenos o vendedores ajenos consulten o cambien el proceso.

## Notificaciones

Solicitudes, respuestas, visitas, reservas, contratos y pagos generan notificaciones para ambos participantes dentro de la misma transaccion. La cuenta puede consultar las ultimas 100 y marcarlas como leidas; no puede leer notificaciones ajenas. La pagina Notificaciones tiene actualizacion manual, sin correo, SMS ni avisos del navegador. Estas notificaciones no sustituyen el estado actual que aparece en cada detalle.

## Base de datos

Se utilizan Contratos, Pagos, Notificaciones e HistorialAcciones existentes, sin migracion nueva. Siguen siendo obligatorias 004_archivado.sql y 005_respuesta_solicitudes.sql para propiedades y solicitudes. La cuenta SQL de la aplicacion necesita lectura y escritura; los cambios de esquema deben ejecutarse con una cuenta con permisos DDL. Nunca ejecutar 001 contra la base existente.

## Validacion

CierreTests comprueba el ciclo real contra SQL temporal: permisos, propuestas/confirmaciones concurrentes, conversion de reserva, ocultacion de anuncios, bloqueo de archivado, pagos repetidos, finalizacion y notificaciones. Las pruebas bUnit verifican la confirmacion explicita del comprador y el marcado de notificaciones. Las pruebas no crean contratos o pagos en Azure compartido.
