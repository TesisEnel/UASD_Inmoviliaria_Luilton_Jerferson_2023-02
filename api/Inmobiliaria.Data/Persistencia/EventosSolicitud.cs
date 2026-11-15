using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;
internal static class EventosSolicitud
{
    public static async Task Agregar(ContextoDatos db,int solicitud,string titulo,string? ruta=null) {
        var usuarios=await (from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
            join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad join v in db.Vendedores on p.IdVendedor equals v.IdVendedor
            join c in db.Compradores on s.IdComprador equals c.IdComprador where s.IdSolicitud==solicitud
            select new {Vendedor=v.IdUsuario,Comprador=c.IdUsuario}).SingleAsync();
        foreach(var id in new[]{usuarios.Vendedor,usuarios.Comprador}.Distinct()) db.Notificaciones.Add(new Notificaciones {
            IdUsuarioDestino=id,TipoNotificacion="Solicitud",Titulo=titulo,Mensaje=$"Solicitud #{solicitud}: {titulo}.",
            UrlDestino=ruta??"/mis-solicitudes",FechaCreacion=DateTime.UtcNow,Leida=false
        });
    }
}
