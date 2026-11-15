# Reservaciones academicas

En Solicitudes, abrir Ver reserva. El vendedor propone un periodo futuro y monto no negativo (hasta dos decimales), el comprador confirma y ambos pueden cancelar. El importe utiliza la moneda de la publicacion. No se procesan pagos ni se firma ningun contrato.

Requiere solicitud aceptada, propiedad disponible sin archivar y publicacion activa. Solo los participantes ven la reserva. Solo el vendedor propone y solo el comprador confirma. Se utiliza la tabla Reservaciones existente, sin migracion nueva.

Las reservas Pendientes y Confirmadas bloquean intervalos solapados de la misma propiedad, incluso en publicaciones y solicitudes distintas. Los intervalos son [inicio, fin): se permite comenzar exactamente cuando termina otra reserva. Se serializan las escrituras por propiedad para evitar conflictos concurrentes. Las fechas se guardan UTC y se muestran en horario dominicano UTC-04:00.

Cada solicitud admite una sola reserva, conforme al indice unico existente. Cancelar conserva ese registro y no permite reemplazarlo dentro de la misma solicitud. Las reservas vencidas se muestran Expiradas y dejan de bloquear disponibilidad y archivado. No se modifica el estado fisico de la propiedad ni se oculta permanentemente su anuncio por confirmar una reserva futura. La cancelacion libera el periodo.

Las reservas no se convierten aun en contratos ni generan notificaciones o cobros; son los proximos pasos. Para bases existentes, siguen siendo necesarios 004_archivado.sql y 005_respuesta_solicitudes.sql, aunque este avance no agrega columnas.

Verificacion: validaciones de fechas/importes y roles en pruebas unitarias; prueba SQL de dos propuestas concurrentes para solicitudes distintas de una misma propiedad (solo una se guarda), confirmacion por comprador y liberacion tras cancelar. Las pruebas usan SQLEXPRESS temporal, no Azure compartido.
