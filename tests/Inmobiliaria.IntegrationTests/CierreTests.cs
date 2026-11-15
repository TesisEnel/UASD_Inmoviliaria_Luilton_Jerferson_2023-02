using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Inmobiliaria.Application;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
namespace Inmobiliaria.IntegrationTests;
[CollectionDefinition("Cierre",DisableParallelization=true)]
public sealed class CierreCollection:ICollectionFixture<ApiSqlFixture>{}
[Collection("Cierre")]
public sealed class CierreTests(ApiSqlFixture app)
{
    private static ClaimsPrincipal Principal(int id)=>new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id.ToString())],"Test"));
    private async Task<T> Servicio<T>(Func<IServiceProvider,Task<T>> accion){using var scope=app.Services.CreateScope();return await accion(scope.ServiceProvider);}
    [Fact]
    public async Task ContratoPagosCierreYNotificaciones_PermisosConcurrenciaEHistorial()
    {
        var vendedor=await Servicio(async sp=>await sp.GetRequiredService<CuentasService>().Registrar(new("Vendedor","Cierre","vendedor@cierre.example",ApiSqlFixture.Password,"8095550000","20000000001"),"Vendedor"));
        var comprador=await Servicio(async sp=>await sp.GetRequiredService<CuentasService>().Registrar(new("Comprador","Cierre","comprador@cierre.example",ApiSqlFixture.Password,"8095550000","20000000002"),"Comprador"));
        var ajeno=await Servicio(async sp=>await sp.GetRequiredService<CuentasService>().Registrar(new("Ajeno","Cierre","ajeno@cierre.example",ApiSqlFixture.Password,"8095550000","20000000003"),"Comprador"));
        Assert.True(vendedor.IsSuccess);Assert.True(comprador.IsSuccess);Assert.True(ajeno.IsSuccess);
        var idVendedor=await app.Scalar("SELECT IdVendedor FROM Vendedores WHERE IdUsuario=@id",new SqlParameter("@id",vendedor.Value.IdUsuario));
        var aprobado=await Servicio(sp=>sp.GetRequiredService<CuentasService>().Decidir(Principal(1),idVendedor,true,null));Assert.True(aprobado.IsSuccess);
        var tipos=await Servicio(sp=>sp.GetRequiredService<IPropiedadRepository>().TiposDeAlquiler());
        var provincias=await Servicio(sp=>sp.GetRequiredService<IPropiedadRepository>().Provincias());
        var idPropiedad=(await Servicio(sp=>sp.GetRequiredService<IPropiedadRepository>().Guardar(idVendedor,new PropiedadDto {IdTipoPropiedad=tipos[0].IdTipoPropiedad,IdProvincia=provincias[0].IdProvincia,Titulo="Casa cierre",Descripcion="Prueba",Ciudad="Ciudad",Sector="Sector",Direccion="Direccion",MetrosCuadrados=80},DateTime.UtcNow)))!.Value;
        var publicacion=await Servicio(sp=>sp.GetRequiredService<PublicacionesService>().Publicar(Principal(vendedor.Value.IdUsuario),new(idPropiedad,"Alquiler",20000,"DOP",null,null)));Assert.True(publicacion.IsSuccess);
        var solicitud=await Servicio(sp=>sp.GetRequiredService<SolicitudesService>().Crear(Principal(comprador.Value.IdUsuario),new(publicacion.Value,"Interesado")));Assert.True(solicitud.IsSuccess);
        Assert.Null(await Servicio(sp=>sp.GetRequiredService<SolicitudesService>().Responder(Principal(vendedor.Value.IdUsuario),solicitud.Value,new(true,null))));
        var reserva=new CrearReservaRequest(DateTimeOffset.UtcNow.AddDays(1),DateTimeOffset.UtcNow.AddDays(30),1000);
        Assert.Null(await Servicio(sp=>sp.GetRequiredService<ReservasService>().Cambiar(Principal(vendedor.Value.IdUsuario),solicitud.Value,reserva,false)));
        Assert.Null(await Servicio(sp=>sp.GetRequiredService<ReservasService>().Cambiar(Principal(comprador.Value.IdUsuario),solicitud.Value,null,true)));
        Task<string?> Cambiar(int usuario,string accion,PagoSimuladoRequest? pago=null)=>Servicio(sp=>sp.GetRequiredService<CierreService>().Cambiar(Principal(usuario),solicitud.Value,accion,pago));
        Assert.Equal("NO_ENCONTRADO",await Cambiar(ajeno.Value.IdUsuario,"crear"));
        Assert.Equal("PROHIBIDO",await Cambiar(comprador.Value.IdUsuario,"crear"));
        var creaciones=await Task.WhenAll(Cambiar(vendedor.Value.IdUsuario,"crear"),Cambiar(vendedor.Value.IdUsuario,"crear"));Assert.Single(creaciones,x=>x is null);Assert.Single(creaciones,x=>x=="CONFLICTO");
        Assert.Equal("PROHIBIDO",await Cambiar(vendedor.Value.IdUsuario,"activar"));
        var activaciones=await Task.WhenAll(Cambiar(comprador.Value.IdUsuario,"activar"),Cambiar(comprador.Value.IdUsuario,"activar"));Assert.Single(activaciones,x=>x is null);Assert.Single(activaciones,x=>x=="CONFLICTO");
        Assert.Equal(1,await app.Scalar("SELECT COUNT(*) FROM Propiedades WHERE IdPropiedad=@id AND EstadoPropiedad='Alquilada'",new SqlParameter("@id",idPropiedad)));
        Assert.Equal(1,await app.Scalar("SELECT COUNT(*) FROM Reservaciones WHERE IdSolicitud=@id AND EstadoReservacion='Convertida'",new SqlParameter("@id",solicitud.Value)));
        Assert.Equal(1,await app.Scalar("SELECT COUNT(*) FROM Publicaciones WHERE IdPublicacion=@id AND EstadoPublicacion='Pausada'",new SqlParameter("@id",publicacion.Value)));
        Assert.Equal(EstadoEliminacion.ConCompromisosActivos,await Servicio(sp=>sp.GetRequiredService<IPropiedadRepository>().Eliminar(idVendedor,idPropiedad)));
        var pago=new PagoSimuladoRequest(20000.25m,DateTimeOffset.UtcNow.AddMinutes(-1),"Primer mes","SIM-001");
        Assert.Equal("PROHIBIDO",await Cambiar(comprador.Value.IdUsuario,"pago",pago));
        var pagos=await Task.WhenAll(Cambiar(vendedor.Value.IdUsuario,"pago",pago),Cambiar(vendedor.Value.IdUsuario,"pago",pago));Assert.Single(pagos,x=>x is null);Assert.Single(pagos,x=>x=="DUPLICADO");
        var lista=await Servicio(sp=>sp.GetRequiredService<CierreService>().Pagos(Principal(comprador.Value.IdUsuario),solicitud.Value));Assert.Equal(20000.25m,Assert.Single(lista!).Monto);
        Assert.Null(await Servicio(sp=>sp.GetRequiredService<CierreService>().Pagos(Principal(ajeno.Value.IdUsuario),solicitud.Value)));
        Assert.Equal("PROHIBIDO",await Cambiar(comprador.Value.IdUsuario,"finalizar"));
        Assert.Null(await Cambiar(vendedor.Value.IdUsuario,"finalizar"));
        Assert.Equal("CONFLICTO",await Cambiar(vendedor.Value.IdUsuario,"finalizar"));
        Assert.Equal(1,await app.Scalar("SELECT COUNT(*) FROM Propiedades WHERE IdPropiedad=@id AND EstadoPropiedad='Disponible'",new SqlParameter("@id",idPropiedad)));
        Assert.Equal(1,await app.Scalar("SELECT COUNT(*) FROM Pagos WHERE IdSolicitud=@id AND MetodoPago='Simulado'",new SqlParameter("@id",solicitud.Value)));
        using var client=app.CreateClient();using var login=await client.PostAsJsonAsync("/api/v1/auth/login",new LoginRequest("comprador@cierre.example",ApiSqlFixture.Password));Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        var sesion=(await login.Content.ReadFromJsonAsync<LoginResponse>())!;client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",sesion.AccessToken);
        var notificaciones=(await client.GetFromJsonAsync<List<NotificacionDto>>("/api/v1/notificaciones"))!;Assert.Contains(notificaciones,n=>n.Titulo=="Pago simulado registrado");
        var id=notificaciones[0].IdNotificacion;using var leer=await client.PostAsync($"/api/v1/notificaciones/{id}/leer",null);Assert.Equal(HttpStatusCode.NoContent,leer.StatusCode);
        Assert.False(await Servicio(sp=>sp.GetRequiredService<CierreService>().Leer(Principal(ajeno.Value.IdUsuario),id)));
        using var reportes=await client.GetAsync("/api/v1/admin/reportes");Assert.Equal(HttpStatusCode.Forbidden,reportes.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<ContratoDto>>($"/api/v1/solicitudes/{solicitud.Value}/contratos"))!);
        using var admin=app.CreateClient();using var loginAdmin=await admin.PostAsJsonAsync("/api/v1/auth/login",new LoginRequest(ApiSqlFixture.AdminEmail,ApiSqlFixture.Password));var sesionAdmin=(await loginAdmin.Content.ReadFromJsonAsync<LoginResponse>())!;admin.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",sesionAdmin.AccessToken);
        using var reporteAdmin=await admin.GetAsync("/api/v1/admin/reportes");Assert.Equal(HttpStatusCode.OK,reporteAdmin.StatusCode);
    }
}
