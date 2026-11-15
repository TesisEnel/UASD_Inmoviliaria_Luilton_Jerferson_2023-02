using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application;
public sealed class ReservasService(IReservasRepository repositorio,AccesoService acceso)
{
    private async Task<int> Usuario(ClaimsPrincipal principal) {
        var u=await acceso.Usuario(principal);
        if(u.Rol is not ("Comprador" or "Vendedor")) throw new AccesoDenegadoException();
        if(u.Rol=="Vendedor") await acceso.Vendedor(principal);
        return u.IdUsuario;
    }
    public async Task<List<ReservaDto>?> Listar(ClaimsPrincipal p,int solicitud) => await repositorio.Listar(await Usuario(p),solicitud);
    public async Task<string?> Cambiar(ClaimsPrincipal p,int solicitud,CrearReservaRequest? nueva,bool confirmar) {
        var usuario=await Usuario(p);
        if(nueva is not null && (nueva.Inicio<=DateTimeOffset.UtcNow || nueva.Fin<=nueva.Inicio || nueva.Monto<0 || nueva.Monto>999999999m || decimal.Round(nueva.Monto,2)!=nueva.Monto)) return "DATOS_INVALIDOS";
        return await repositorio.Cambiar(usuario,solicitud,nueva,confirmar);
    }
}
