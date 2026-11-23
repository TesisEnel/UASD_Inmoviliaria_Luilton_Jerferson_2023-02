package com.inmobiliaria.android

import androidx.compose.material3.*
import androidx.compose.runtime.*
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.time.OffsetDateTime
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

internal val dominicanOffset = ZoneOffset.ofHours(-4)
internal val dateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm")
internal fun now() = OffsetDateTime.now(dominicanOffset).withSecond(0).withNano(0)
internal fun date(value: String, offset: ZoneOffset = dominicanOffset): String =
    if (value.isBlank()) "" else runCatching { utc(value).atOffset(offset).format(dateFormat) }.getOrDefault(value)

@Composable
internal fun RequestsPage(api: Api, role: String?, open: (String, Int) -> Unit, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var requests by remember { mutableStateOf<List<JSONObject>?>(null) }
    var reply by remember { mutableStateOf<Int?>(null) }
    var reason by remember { mutableStateOf("") }
    val load: suspend () -> Unit = { requests = api.list("solicitudes") }
    LaunchedEffect(Unit) { op.run(load) }
    val list = requests.orEmpty()
    fun respond(id: Int, accept: Boolean) {
        scope.launch {
            op.run {
                require(accept || reason.isNotBlank()) { "Indica el motivo del rechazo." }
                api.send("solicitudes/$id/responder", json("aceptar" to accept, "motivo" to reason))
                reply = null
                notify("Respuesta guardada.")
                load()
            }
        }
    }
    Page(list.size, {
        Heading(if (role == "Vendedor") "Solicitudes recibidas" else "Mis solicitudes")
        Refresh(op, load)
        if (requests?.isEmpty() == true) Text("Todavía no hay solicitudes.")
    }) { index ->
        val item = list[index]
        val id = item.getInt("idSolicitud")
        val state = item.text("estado")
        Heading(item.text("titulo"))
        Text("Solicitud #$id · $state · ${date(item.text("fechaSolicitud"), ZoneOffset.UTC)} UTC")
        if (role == "Vendedor") Text("Comprador · ${item.text("comprador")}")
        if (item.text("mensaje").isNotBlank()) Text(item.text("mensaje"))
        if (!item.isNull("fechaRespuesta")) {
            Text("Respuesta · ${item.text("respuesta")} · ${date(item.text("fechaRespuesta"), ZoneOffset.UTC)} UTC")
        }
        if (item.optBoolean("publicacionRetirada")) Text("Publicación retirada. La solicitud se conserva en el historial.")
        OutlinedButton(onClick = { open("visitas", id) }) { Text("Ver visitas") }
        OutlinedButton(onClick = { open("reserva", id) }) { Text("Ver reserva") }
        OutlinedButton(onClick = { open("contrato", id) }) { Text("Contrato y pagos") }
        if (role == "Vendedor" && state == "Pendiente" && !item.optBoolean("publicacionRetirada")) {
            if (reply == id) {
                TextBox("Mensaje (obligatorio al rechazar)", reason, { reason = it }, 1000, multiline = true)
                Button(enabled = !op.busy, onClick = { respond(id, true) }) { Text("Aceptar solicitud") }
                OutlinedButton(enabled = !op.busy, onClick = { respond(id, false) }) { Text("Rechazar solicitud") }
                TextButton(onClick = { reply = null }) { Text("Volver") }
            } else {
                Button(enabled = !op.busy, onClick = { reply = id; reason = "" }) { Text("Responder") }
            }
        }
    }
}

private data class RequestAction(val title: String, val explanation: String, val path: String)

@Composable
internal fun RequestDetailPage(api: Api, screen: Screen, role: String?, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    val vendor = role == "Vendedor"
    val buyer = role == "Comprador"
    val path = "solicitudes/${screen.id}"
    val visits = "$path/visitas"
    val reservation = "$path/reserva"
    val contracts = "$path/contratos"
    val payments = "$path/pagos"
    val section = when (screen.route) {
        "visitas" -> visits
        "reserva" -> reservation
        else -> contracts
    }
    var entries by remember { mutableStateOf<List<JSONObject>?>(null) }
    var history by remember { mutableStateOf(emptyList<JSONObject>()) }
    var action by remember { mutableStateOf<RequestAction?>(null) }
    val load: suspend () -> Unit = {
        val items = api.list(section)
        if (section == contracts) history = api.list(payments)
        entries = items
    }
    LaunchedEffect(Unit) { op.run(load) }
    fun submit(endpoint: String, success: String, data: () -> JSONObject? = { null }, after: () -> Unit = {}) {
        scope.launch {
            op.run {
                api.send(endpoint, data())
                after()
                notify(success)
                load()
            }
        }
    }
    fun prepare(title: String, explanation: String, endpoint: String) {
        action = RequestAction(title, explanation, endpoint)
    }

    val list = entries.orEmpty()
    val hasActiveContract = list.any { it.text("estado") == "Activo" }
    val paymentRows = if (section == contracts && entries != null) 1 + history.size else 0
    Page(list.size + paymentRows, {
        Heading("Solicitud #${screen.id}")
        Text("Horarios de República Dominicana (UTC−04:00)")
        if (section == contracts) Text("Acuerdos y pagos simulados para fines académicos.")
        if (section == reservation) Text("Reserva académica. No se efectúan cobros.")
        Refresh(op, load)
        if (entries?.isEmpty() == true) Text(when (section) {
            visits -> "Todavía no hay visitas."
            reservation -> "Todavía no hay reserva."
            else -> "Todavía no hay contrato."
        })
        if (vendor && entries != null) {
            when (section) {
                visits -> if (list.none { it.text("estado") in listOf("Solicitada", "Confirmada") }) {
                    VisitForm(op.busy) { data -> submit(visits, "Visita propuesta.", data) }
                }
                reservation -> if (list.isEmpty()) {
                    ReservationForm(op.busy) { data -> submit(reservation, "Reserva propuesta.", data) }
                }
                contracts -> if (list.isEmpty()) {
                    Button(enabled = !op.busy, onClick = {
                        prepare("Crear propuesta académica",
                            "Se usarán las fechas de la reserva confirmada y el precio mensual de la publicación.", "$contracts/crear")
                    }) { Text("Crear propuesta académica") }
                }
            }
        }
    }) { index ->
        if (index >= list.size) {
            val paymentIndex = index - list.size - 1
            if (paymentIndex < 0) {
                Heading("Pagos simulados")
                if (history.isEmpty()) Text("No hay pagos registrados.")
                if (vendor && hasActiveContract) {
                    PaymentForm(op.busy) { data, reset -> submit(payments, "Pago simulado registrado.", data, reset) }
                }
            } else {
                val payment = history[paymentIndex]
                Text("${payment.money("monto")} · ${payment.text("estado")}")
                Text(payment.text("concepto"))
                Text("Referencia · ${payment.text("referencia")}")
                Text(date(payment.text("fecha")))
            }
        } else {
            val item = list[index]
            val state = item.text("estado")
            when (section) {
                visits -> {
                    Heading(date(item.text("fechaUtc")))
                    Text(state)
                    if (item.text("notas").isNotBlank()) Text(item.text("notas"))
                    val endpoint = "$visits/${item.getInt("idVisita")}"
                    if (buyer && state == "Solicitada") Button(enabled = !op.busy, onClick = {
                        submit("$endpoint/confirmar", "Asistencia confirmada.")
                    }) { Text("Confirmar asistencia") }
                    if (state in listOf("Solicitada", "Confirmada")) OutlinedButton(enabled = !op.busy, onClick = {
                        prepare("Cancelar visita", "La visita quedará cancelada en el historial.", "$endpoint/cancelar")
                    }) { Text("Cancelar visita") }
                }
                reservation -> {
                    Heading(state)
                    Text("${date(item.text("inicioUtc"))} — ${date(item.text("finUtc"))}")
                    Text(item.money("monto"))
                    if (buyer && state == "Pendiente") Button(enabled = !op.busy, onClick = {
                        submit("$reservation/confirmar", "Reserva confirmada.")
                    }) { Text("Confirmar reserva") }
                    if (state in listOf("Pendiente", "Confirmada")) OutlinedButton(enabled = !op.busy, onClick = {
                        prepare("Cancelar reserva", "La reserva quedará cancelada en el historial.", "$reservation/cancelar")
                    }) { Text("Cancelar reserva") }
                }
                contracts -> {
                    Text(state)
                    Heading(item.text("numero"))
                    Text("${date(item.text("inicio"))} — ${date(item.text("fin")).ifBlank { "Sin fecha final" }}")
                    Text("${item.money("monto")} / mes")
                    if (state == "Borrador") {
                        if (buyer) Button(enabled = !op.busy, onClick = {
                            prepare("Confirmar acuerdo académico",
                                "Se activará el acuerdo, se convertirá la reserva y la propiedad quedará alquilada. Se retirarán sus publicaciones.",
                                "$contracts/activar")
                        }) { Text("Confirmar acuerdo académico") }
                        OutlinedButton(enabled = !op.busy, onClick = {
                            prepare("Cancelar propuesta", "Se cancelarán la propuesta y su reserva. Se conserva el historial.",
                                "$contracts/cancelar")
                        }) { Text("Cancelar propuesta") }
                    }
                    if (state == "Activo" && vendor) OutlinedButton(enabled = !op.busy, onClick = {
                        prepare("Finalizar alquiler académico",
                            "La propiedad quedará disponible. Se conservarán los anuncios retirados y los pagos simulados.",
                            "$contracts/finalizar")
                    }) { Text("Finalizar alquiler académico") }
                }
            }
        }
    }
    action?.let { change ->
        Confirm(change.title, change.explanation, close = { action = null }) {
            action = null
            submit(change.path, "Operación guardada.")
        }
    }
}

