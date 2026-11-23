package com.inmobiliaria.android

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.text.NumberFormat
import java.util.Locale

@Composable
internal fun ConnectionPage(base: String, save: (String) -> Unit) {
    var value by remember { mutableStateOf(base) }
    var error by remember { mutableStateOf(false) }
    Page(header = {
        Heading("Servidor de la inmobiliaria")
        Text("Introduce la URL base de la API.")
        TextBox("URL de la API", value, { value = it; error = false }, keyboard = KeyboardType.Uri)
        if (error) Text("Introduce una URL HTTPS válida.", color = MaterialTheme.colorScheme.error)
        Button(onClick = { runCatching { apiBase(value) }.onSuccess(save).onFailure { error = true } }) {
            Text("Guardar conexión")
        }
    })
}

@Composable
internal fun LoginPage(api: Api, register: (String) -> Unit, onLogin: (Session) -> Unit) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    Page(header = {
        Heading("Accede a tu cuenta")
        TextBox("Correo", email, { email = it }, 254, keyboard = KeyboardType.Email)
        TextBox("Contraseña", password, { password = it }, 128, password = true, keyboard = KeyboardType.Password)
        Feedback(op)
        Button(enabled = !op.busy, onClick = {
            scope.launch {
                op.run {
                    require(email.isNotBlank() && password.isNotBlank()) { "Introduce el correo y la contraseña." }
                    val body = JSONObject(api.send("auth/login", json("correo" to email.trim(), "password" to password)))
                    val user = body.getJSONObject("usuario")
                    onLogin(Session(body.getString("accessToken"), user.getString("nombre"), user.getString("rol")))
                }
            }
        }) { Text("Iniciar sesión") }
        HorizontalDivider()
        TextButton(enabled = !op.busy, onClick = { register("compradores") }) { Text("Registrarme como comprador") }
        TextButton(enabled = !op.busy, onClick = { register("vendedores") }) { Text("Registrarme como vendedor") }
    })
}

@Composable
internal fun RegisterPage(api: Api, kind: String, onDone: () -> Unit) {
    val vendor = kind == "vendedores"
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var done by remember { mutableStateOf(false) }
    var name by remember { mutableStateOf("") }
    var surname by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var cedula by remember { mutableStateOf("") }
    Page(header = {
        Heading(if (vendor) "Registro de vendedor" else "Registro de comprador")
        if (done) {
            Text(if (vendor) "Tu cuenta está pendiente de aprobación por un administrador."
                else "Tu cuenta fue creada.")
            Button(onClick = onDone) { Text("Ir a iniciar sesión") }
        } else {
            TextBox("Nombre", name, { name = it }, 100)
            TextBox("Apellido", surname, { surname = it }, 100)
            TextBox("Correo", email, { email = it }, 254, keyboard = KeyboardType.Email)
            TextBox("Contraseña (mínimo 12 caracteres)", password, { password = it }, 128,
                password = true, keyboard = KeyboardType.Password)
            TextBox("Teléfono", phone, { phone = it }, 30, keyboard = KeyboardType.Phone)
            TextBox("Cédula (11 dígitos)", cedula, { cedula = it }, 11, keyboard = KeyboardType.Number)
            Feedback(op)
            Button(enabled = !op.busy, onClick = {
                scope.launch {
                    op.run {
                        require(name.isNotBlank() && surname.isNotBlank() && phone.isNotBlank() &&
                            android.util.Patterns.EMAIL_ADDRESS.matcher(email.trim()).matches() &&
                            password.length >= 12 && cedula.matches(Regex("[0-9]{11}"))) {
                            "Completa todos los campos. Revisa el correo, la contraseña y la cédula."
                        }
                        api.send("auth/$kind", json("nombre" to name.trim(), "apellido" to surname.trim(),
                            "correo" to email.trim(), "password" to password, "telefono" to phone.trim(), "cedula" to cedula))
                        password = ""
                        done = true
                    }
                }
            }) { Text("Registrarme") }
        }
    })
}

internal fun JSONObject.money(key: String): String {
    val amount = text(key).toBigDecimalOrNull() ?: return ""
    val format = NumberFormat.getNumberInstance(Locale.forLanguageTag("es-DO")).apply {
        minimumFractionDigits = 2
        maximumFractionDigits = 2
    }
    return text("moneda") + " " + format.format(amount)
}

@Composable
internal fun CatalogPage(api: Api, detail: (Int) -> Unit) {
    val op = remember { Operation() }
    var listings by remember { mutableStateOf<List<JSONObject>?>(null) }
    val load: suspend () -> Unit = { listings = api.list("publicaciones") }
    LaunchedEffect(Unit) { op.run(load) }
    val list = listings.orEmpty()
    Page(list.size, {
        Heading("Propiedades en alquiler")
        Text("Casas y apartamentos en República Dominicana")
        Refresh(op, load)
        if (listings?.isEmpty() == true) Text("Todavía no hay propiedades publicadas.")
    }) { index ->
        val item = list[index]
        val id = item.getInt("idPublicacion")
        val title = item.text("titulo")
        Gallery(api, "publicaciones/$id/imagenes", title, cover = true)
        Text(title, style = MaterialTheme.typography.titleLarge)
        Text("${item.text("sector")}, ${item.text("ciudad")}")
        Text("${item.money("precio")} / mes")
        Button(onClick = { detail(id) }) { Text("Ver propiedad") }
    }
}

@Composable
internal fun ListingPage(publicApi: Api, api: Api, id: Int, role: String?, signIn: () -> Unit, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var item by remember { mutableStateOf<JSONObject?>(null) }
    var message by remember { mutableStateOf("") }
    var requesting by remember { mutableStateOf(false) }
    val load: suspend () -> Unit = {
        item = publicApi.list("publicaciones").find { it.getInt("idPublicacion") == id }
        require(item != null) { "Esta propiedad ya no está publicada." }
    }
    LaunchedEffect(Unit) { op.run(load) }
    Page(header = {
        Refresh(op, load)
        item?.let { listing ->
            val title = listing.text("titulo")
            Gallery(publicApi, "publicaciones/$id/imagenes", title)
            Heading(title)
            Text("${listing.text("sector")}, ${listing.text("ciudad")}")
            Text("${listing.text("habitaciones")} habitaciones · ${listing.text("banos")} baños")
            Text("${listing.money("precio")} / mes", style = MaterialTheme.typography.titleLarge)
            if (!listing.isNull("deposito")) Text("Depósito · ${listing.money("deposito")}")
            if (!listing.isNull("mantenimientoMensual")) Text("Mantenimiento · ${listing.money("mantenimientoMensual")}")
            if (role == null) Button(onClick = signIn) { Text("Inicia sesión para solicitar") }
            if (role == "Comprador" && !requesting) Button(onClick = { requesting = true }) { Text("Solicitar alquiler") }
            if (role == "Comprador" && requesting) {
                TextBox("Mensaje al vendedor (opcional)", message, { message = it }, 2000, multiline = true)
                Button(enabled = !op.busy, onClick = {
                    scope.launch {
                        op.run {
                            api.send("solicitudes", json("idPublicacion" to id, "mensaje" to message))
                            requesting = false
                            message = ""
                            notify("Solicitud enviada. Puedes seguir su estado en Mis solicitudes.")
                        }
                    }
                }) { Text("Enviar solicitud") }
                TextButton(enabled = !op.busy, onClick = { requesting = false }) { Text("Cancelar") }
            }
        }
    })
}

@Composable
internal fun Gallery(api: Api, path: String, title: String, cover: Boolean = false) {
    var photos by remember(path) { mutableStateOf<List<JSONObject>?>(null) }
    var index by remember(path) { mutableIntStateOf(0) }
    LaunchedEffect(api, path) {
        photos = try { api.list(path).sortedBy { it.optInt("orden") } }
            catch (ex: CancellationException) { throw ex }
            catch (_: Exception) { emptyList() }
    }
    val images = photos ?: return
    if (images.isEmpty()) {
        Text("Sin fotografías", color = MaterialTheme.colorScheme.onSurfaceVariant)
    } else {
        RemoteImage(api, images[index].text("url"), "$title, imagen ${index + 1}")
        if (!cover && images.size > 1) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                TextButton(onClick = { index = (index + images.size - 1) % images.size }) { Text("Anterior") }
                Text("${index + 1} / ${images.size}", modifier = Modifier.padding(top = 12.dp))
                TextButton(onClick = { index = (index + 1) % images.size }) { Text("Siguiente") }
            }
        }
    }
}

@Composable
internal fun RemoteImage(api: Api, path: String, description: String) {
    var bitmap by remember(api, path) { mutableStateOf<ImageBitmap?>(null) }
    var failed by remember(api, path) { mutableStateOf(false) }
    LaunchedEffect(api, path) {
        try {
            bitmap = api.image(path)?.asImageBitmap()
            failed = bitmap == null
        } catch (ex: CancellationException) {
            throw ex
        } catch (_: Exception) {
            failed = true
        }
    }
    val photo = bitmap
    when {
        photo != null -> Image(photo, description, Modifier.fillMaxWidth().height(200.dp), contentScale = ContentScale.Crop)
        failed -> Text("Imagen no disponible")
        else -> LinearProgressIndicator(Modifier.fillMaxWidth())
    }
}
