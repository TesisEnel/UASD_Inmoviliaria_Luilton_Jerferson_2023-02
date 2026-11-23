package com.inmobiliaria.android

import android.app.DatePickerDialog
import android.app.TimePickerDialog
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

@Composable
private fun DateField(label: String, value: OffsetDateTime, update: (OffsetDateTime) -> Unit) {
    val context = LocalContext.current
    Text(label)
    OutlinedButton(onClick = {
        DatePickerDialog(context, { _, year, month, day ->
            TimePickerDialog(context, { _, hour, minute ->
                update(LocalDateTime.of(year, month + 1, day, hour, minute).atOffset(dominicanOffset))
            }, value.hour, value.minute, true).show()
        }, value.year, value.monthValue - 1, value.dayOfMonth).show()
    }) { Text(value.format(dateFormat)) }
}

@Composable
internal fun VisitForm(busy: Boolean, submit: (() -> JSONObject) -> Unit) {
    var whenAt by remember { mutableStateOf(now().plusDays(1)) }
    var notes by remember { mutableStateOf("") }
    CardBlock {
        Heading("Proponer visita")
        Text("Requiere una solicitud aceptada.")
        DateField("Fecha y hora", whenAt) { whenAt = it }
        TextBox("Notas (opcional)", notes, { notes = it }, 1000, multiline = true)
        Button(enabled = !busy, onClick = {
            submit {
                require(whenAt.isAfter(OffsetDateTime.now())) { "Selecciona una fecha futura." }
                json("fecha" to whenAt.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME), "notas" to notes)
            }
        }) { Text("Proponer visita") }
    }
}

@Composable
internal fun ReservationForm(busy: Boolean, submit: (() -> JSONObject) -> Unit) {
    var start by remember { mutableStateOf(now().plusDays(1)) }
    var end by remember { mutableStateOf(now().plusDays(3)) }
    var amount by remember { mutableStateOf("0") }
    CardBlock {
        Heading("Proponer reserva")
        Text("Requiere una solicitud aceptada. Se usa la moneda de la publicación.")
        DateField("Inicio del periodo", start) { start = it }
        DateField("Fin del periodo", end) { end = it }
        TextBox("Monto acordado", amount, { amount = it }, 16, keyboard = KeyboardType.Decimal)
        Button(enabled = !busy, onClick = {
            submit {
                require(start.isAfter(OffsetDateTime.now()) && end.isAfter(start)) {
                    "Selecciona fechas futuras y un fin posterior al inicio."
                }
                json("inicio" to start.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME), "fin" to end.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME), "monto" to decimal(amount, "monto", allowZero = true))
            }
        }) { Text("Proponer reserva") }
    }
}

@Composable
internal fun PaymentForm(busy: Boolean, submit: (() -> JSONObject, () -> Unit) -> Unit) {
    var amount by remember { mutableStateOf("") }
    var paidAt by remember { mutableStateOf(now().minusMinutes(1)) }
    var concept by remember { mutableStateOf("Alquiler simulado") }
    var reference by remember { mutableStateOf("") }
    Heading("Registrar pago simulado")
    TextBox("Monto", amount, { amount = it }, 16, keyboard = KeyboardType.Decimal)
    DateField("Fecha y hora del registro", paidAt) { paidAt = it }
    TextBox("Concepto", concept, { concept = it }, 200)
    TextBox("Referencia única", reference, { reference = it }, 200)
    Button(enabled = !busy, onClick = {
        submit({
            require(concept.isNotBlank() && reference.isNotBlank()) { "Completa el concepto y la referencia." }
            json("monto" to decimal(amount, "monto", allowZero = false), "fecha" to paidAt.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME),
                "concepto" to concept.trim(), "referencia" to reference.trim())
        }, { reference = ""; amount = "" })
    }) { Text("Registrar simulación") }
}

@Composable
internal fun NotificationsPage(api: Api, open: (String, Int) -> Unit, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var notifications by remember { mutableStateOf<List<JSONObject>?>(null) }
    val load: suspend () -> Unit = { notifications = api.list("notificaciones") }
    LaunchedEffect(Unit) { op.run(load) }
    val list = notifications.orEmpty()
    Page(list.size, {
        Heading("Tu actividad")
        Refresh(op, load)
        if (notifications?.isEmpty() == true) Text("No tienes notificaciones.")
    }) { index ->
        val item = list[index]
        val read = item.optBoolean("leida")
        Text((if (read) "Leída" else "Nueva") + " · " + date(item.text("fecha"), ZoneOffset.UTC) + " UTC")
        Heading(item.text("titulo"))
        Text(item.text("mensaje"))
        if (!read) OutlinedButton(enabled = !op.busy, onClick = {
            scope.launch {
                op.run {
                    api.send("notificaciones/${item.getInt("idNotificacion")}/leer")
                    notify("Notificación marcada como leída.")
                    load()
                }
            }
        }) { Text("Marcar como leída") }
        val url = item.text("url")
        val match = Regex("/solicitudes/([1-9][0-9]*)/(visitas|reserva|contrato)").matchEntire(url)
        val id = match?.groupValues?.get(1)?.toIntOrNull()
        if (url == "/mis-solicitudes") TextButton(onClick = { open("solicitudes", 0) }) { Text("Ver solicitudes") }
        if (match != null && id != null) TextButton(onClick = { open(match.groupValues[2], id) }) { Text("Ver detalle") }
    }
}

