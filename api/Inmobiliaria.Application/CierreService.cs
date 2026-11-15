using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application;
public sealed class CierreService(ICierreRepository repo,AccesoService acceso)
{
    private async Task<int> Participante(ClaimsPrincipal principal) {
        var usuario=await acceso.Usuario(principal);
        if(usuario.Rol is not ("Comprador" or "Vendedor")) throw new AccesoDenegadoException();
        if(usuario.Rol=="Vendedor") await acceso.Vendedor(principal);
        return usuario.IdUsuario;
    }
    public async Task<List<ContratoDto>?> Contratos(ClaimsPrincipal p,int solicitud)=>await repo.Contratos(await Participante(p),solicitud);
    public async Task<List<PagoDto>?> Pagos(ClaimsPrincipal p,int solicitud)=>await repo.Pagos(await Participante(p),solicitud);
    public async Task<string?> Cambiar(ClaimsPrincipal p,int solicitud,string accion,PagoSimuladoRequest? pago=null) {
        var usuario=await Participante(p);
        if(pago is not null && (pago.Monto<=0 || pago.Monto>999999999 || decimal.Round(pago.Monto,2)!=pago.Monto
            || pago.Fecha>DateTimeOffset.UtcNow || pago.Fecha.Year<2000 || string.IsNullOrWhiteSpace(pago.Concepto)
            || pago.Concepto.Length>200 || string.IsNullOrWhiteSpace(pago.Referencia) || pago.Referencia.Length>200)) return "DATOS_INVALIDOS";
        return await repo.Cambiar(usuario,solicitud,accion,pago);
    }
    public async Task<List<NotificacionDto>> Notificaciones(ClaimsPrincipal p)=>await repo.Notificaciones((await acceso.Usuario(p)).IdUsuario);
    public async Task<bool> Leer(ClaimsPrincipal p,int id)=>await repo.LeerNotificacion((await acceso.Usuario(p)).IdUsuario,id);
    public async Task<List<ReporteAdminDto>> Reportes(ClaimsPrincipal p) {await acceso.Administrador(p);return await repo.Reportes();}
}
