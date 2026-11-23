package com.inmobiliaria.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.lifecycle.ViewModel
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import org.json.JSONObject

private const val defaultBase = "http://10.0.2.2:5180/"

class MainActivity : ComponentActivity() {
    private val state: AppState by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val settings = getSharedPreferences("settings", MODE_PRIVATE)
        setContent {
            MaterialTheme(colorScheme = lightColorScheme(
                primary = Color(0xFF175648), secondary = Color(0xFF4C635B),
                background = Color(0xFFF6F7F2), surface = Color(0xFFF6F7F2),
                surfaceVariant = Color(0xFFE6EDE6)
            )) {
                var base by remember { mutableStateOf(settings.getString("api", if (BuildConfig.DEBUG) defaultBase else "").orEmpty()) }
                Inmobiliaria(base, state) {
                    settings.edit().putString("api", it).apply()
                    base = it
                }
            }
        }
    }
}

internal data class Session(val token: String, val name: String, val role: String)
internal data class Screen(val route: String, val id: Int = 0)
internal class AppState : ViewModel() {
    var session by mutableStateOf<Session?>(null)
    val stack = mutableStateListOf<Screen>()
    var pendingLogin: Screen? = null
}

private data class Menu(val route: String, val title: String)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun Inmobiliaria(base: String, state: AppState, changeBase: (String) -> Unit) {
    val session = state.session
    val stack = state.stack
    val screen = stack.lastOrNull() ?: Screen(if (base.isBlank()) "api" else "catalogo")
    val api = remember(base, session) { Api(base, session?.token) }
    val publicApi = remember(base) { Api(base, null) }
    var menu by rememberSaveable { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()
    val notify: (String) -> Unit = { scope.launch { snackbar.showSnackbar(it) } }
    fun root(route: String) { stack.clear(); stack.add(Screen(route)) }
    fun open(route: String, id: Int = 0) {
        val target = Screen(route, id)
        if (target == screen) return
        if (stack.isEmpty()) stack.add(screen)
        stack.add(target)
    }
    fun back() { if (stack.size > 1) stack.removeAt(stack.lastIndex) }
    fun signIn(returnTo: Screen? = null) { state.pendingLogin = returnTo; open("login") }
    BackHandler(stack.size > 1) { back() }

    val destinations = buildList {
        add(Menu("catalogo", "Catálogo"))
        if (session?.role == "Vendedor") {
            add(Menu("propiedades", "Mis propiedades"))
            add(Menu("archivo", "Administrar"))
        }
        if (session?.role in listOf("Comprador", "Vendedor")) add(Menu("solicitudes", "Solicitudes"))
        if (session != null) add(Menu("notificaciones", "Notificaciones"))
        add(Menu("api", "Conexión"))
        add(if (session == null) Menu("login", "Iniciar sesión") else Menu("salir", "Cerrar sesión"))
    }
    val title = when (screen.route) {
        "detalle" -> "Propiedad en alquiler"
        "compradores", "vendedores" -> "Crear cuenta"
        "editar" -> if (screen.id == 0) "Nueva propiedad" else "Editar propiedad"
        "publicar" -> "Publicar alquiler"
        "imagenes" -> "Imágenes de la propiedad"
        "visitas" -> "Visitas"
        "reserva" -> "Reserva"
        "contrato" -> "Contrato y pagos"
        else -> destinations.find { it.route == screen.route }?.title ?: "Inmobiliaria"
    }
    Scaffold(
        topBar = {
            TopAppBar(title = { Text(title) }, navigationIcon = {
                if (stack.size > 1) TextButton(onClick = { back() }) { Text("Atrás") }
            }, actions = {
                TextButton(onClick = { menu = true }) { Text("Menú") }
                DropdownMenu(expanded = menu, onDismissRequest = { menu = false }) {
                    session?.let { DropdownMenuItem(text = { Text(it.name) }, enabled = false, onClick = {}) }
                    destinations.forEach { item ->
                        DropdownMenuItem(text = { Text(item.title) }, onClick = {
                            menu = false
                            when (item.route) {
                                "salir" -> { state.session = null; state.pendingLogin = null; root("catalogo") }
                                "login" -> signIn()
                                "api" -> if (screen.route != "api") open("api")
                                else -> root(item.route)
                            }
                        })
                    }
                }
            })
        },
        snackbarHost = { SnackbarHost(snackbar) }
    ) { padding ->
        Box(Modifier.fillMaxSize().padding(padding)) {
            key(screen, base, session) {
                when (screen.route) {
                    "api" -> ConnectionPage(base) {
                        if (base != it) state.session = null
                        changeBase(it)
                        state.pendingLogin = null
                        root("catalogo")
                    }
                    "login" -> LoginPage(publicApi, { open(it) }) { newSession ->
                        state.session = newSession
                        val target = state.pendingLogin
                        state.pendingLogin = null
                        if (target != null && newSession.role == "Comprador") {
                            root("catalogo"); stack.add(target)
                        } else root(if (newSession.role == "Vendedor") "propiedades" else "catalogo")
                    }
                    "compradores", "vendedores" -> RegisterPage(publicApi, screen.route) { back() }
                    "catalogo" -> CatalogPage(publicApi, { open("detalle", it) })
                    "detalle" -> ListingPage(publicApi, api, screen.id, session?.role, { signIn(screen) }, notify)
                    "propiedades" -> PropertiesPage(api, ::open)
                    "editar" -> EditPropertyPage(api, screen.id) { back(); notify("Propiedad guardada.") }
                    "publicar" -> PublishPage(api, screen.id, { open("imagenes", screen.id) }) { back(); notify("Publicación creada.") }
                    "imagenes" -> PropertyImagesPage(api, screen.id, { if (stack.getOrNull(stack.lastIndex - 1)?.route == "publicar") back() else open("publicar", screen.id) }, notify)
                    "archivo" -> ArchivePage(api, notify)
                    "solicitudes" -> RequestsPage(api, session?.role, ::open, notify)
                    "visitas", "reserva", "contrato" -> RequestDetailPage(api, screen, session?.role, notify)
                    "notificaciones" -> NotificationsPage(api, ::open, notify)
                }
            }
        }
    }
}

internal class Operation {
    var busy by mutableStateOf(false)
        private set
    var error by mutableStateOf<String?>(null)
        private set

    suspend fun run(action: suspend () -> Unit) {
        if (busy) return
        busy = true
        error = null
        try {
            action()
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (failure: Exception) {
            error = when (failure) {
                is ApiError, is IllegalArgumentException -> failure.message
                is java.net.SocketTimeoutException -> "La API tardó demasiado. Actualiza antes de volver a intentar."
                is java.io.IOException -> "No se pudo conectar con la API. Verifica la conexión."
                is org.json.JSONException -> "La API devolvió un formato inesperado."
                else -> "No se pudo completar la operación."
            }
        } finally {
            busy = false
        }
    }
}

@Composable
internal fun Feedback(operation: Operation) {
    if (operation.busy) LinearProgressIndicator(Modifier.fillMaxWidth())
    operation.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
}

@Composable
internal fun Page(
    count: Int = 0,
    header: @Composable ColumnScope.() -> Unit,
    entry: @Composable ColumnScope.(Int) -> Unit = {}
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize().imePadding(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
    ) {
        item { Column(verticalArrangement = Arrangement.spacedBy(12.dp), content = header) }
        items(count) { i -> CardBlock { entry(i) } }
    }
}

@Composable
internal fun CardBlock(content: @Composable ColumnScope.() -> Unit) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp), content = content)
    }
}

@Composable
internal fun Heading(text: String) = Text(text, style = MaterialTheme.typography.headlineSmall)

@Composable
internal fun Refresh(operation: Operation, load: suspend () -> Unit) {
    val scope = rememberCoroutineScope()
    OutlinedButton(enabled = !operation.busy, onClick = { scope.launch { operation.run(load) } }) {
        Text("Actualizar")
    }
    Feedback(operation)
}

@Composable
internal fun Confirm(title: String, description: String, close: () -> Unit, execute: () -> Unit) {
    AlertDialog(onDismissRequest = close, title = { Text(title) }, text = { Text(description) },
        confirmButton = { Button(onClick = execute) { Text("Confirmar") } },
        dismissButton = { TextButton(onClick = close) { Text("Volver") } })
}

@Composable
internal fun TextBox(
    title: String,
    value: String,
    change: (String) -> Unit,
    limit: Int = 4000,
    multiline: Boolean = false,
    password: Boolean = false,
    keyboard: androidx.compose.ui.text.input.KeyboardType = androidx.compose.ui.text.input.KeyboardType.Text
) {
    OutlinedTextField(
        value, { if (it.length <= limit) change(it) }, Modifier.fillMaxWidth(),
        label = { Text(title) }, singleLine = !multiline, minLines = if (multiline) 3 else 1,
        visualTransformation = if (password) androidx.compose.ui.text.input.PasswordVisualTransformation()
            else androidx.compose.ui.text.input.VisualTransformation.None,
        keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(keyboardType = keyboard)
    )
}
