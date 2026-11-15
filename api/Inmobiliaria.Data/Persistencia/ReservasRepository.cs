using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;
internal sealed class ReservasRepository(ContextoDatos db) : IReservasRepository
{
    private IQueryable<Solicitudes> Propias(int usuario,int solicitud) =>
        from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
        join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad
        join v in db.Vendedores on p.IdVendedor equals v.IdVendedor
        join c in db.Compradores on s.IdComprador equals c.IdComprador
        where s.IdSolicitud==solicitud && (v.IdUsuario==usuario || c.IdUsuario==usuario) select s;
    public async Task<List<ReservaDto>?> Listar(int usuario,int solicitud) {
        if(!await Propias(usuario,solicitud).AnyAsync()) return null;
        var ahora=DateTime.UtcNow;
        return await (from r in db.Reservaciones.AsNoTracking()
            join s in db.Solicitudes on r.IdSolicitud equals s.IdSolicitud
            join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
            where s.IdSolicitud==solicitud
            select new ReservaDto(r.IdReservacion,r.FechaInicio,r.FechaFin,r.MontoReserva,a.Moneda,
                (r.EstadoReservacion=="Pendiente" || r.EstadoReservacion=="Confirmada") && r.FechaFin<=ahora ? "Expirada" : r.EstadoReservacion)).ToListAsync();
    }
    public async Task<string?> Cambiar(int usuario,int solicitud,CrearReservaRequest? nueva,bool confirmar) {
        if(!await Propias(usuario,solicitud).AnyAsync()) return "NO_ENCONTRADA";
        var propiedadId=await (from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion where s.IdSolicitud==solicitud select a.IdPropiedad).SingleAsync();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedadId}");
        var actual=await Propias(usuario,solicitud).SingleAsync();
        var p=await db.Propiedades.SingleAsync(p=>p.IdPropiedad==propiedadId);
        var v=await db.Vendedores.SingleAsync(v=>v.IdVendedor==p.IdVendedor);
        var anuncio=await db.Publicaciones.SingleAsync(a=>a.IdPublicacion==actual.IdPublicacion);
        var ahora=DateTime.UtcNow;
        if(nueva is not null || confirmar) {
            if(actual.EstadoSolicitud!="Aceptada" || p.EliminadoEn!=null || p.EstadoPropiedad!="Disponible" || anuncio.EstadoPublicacion!="Publicada") return "CONFLICTO";
        }
        var reserva=await db.Reservaciones.SingleOrDefaultAsync(r=>r.IdSolicitud==solicitud);
        if(nueva is not null) {
            if(v.IdUsuario!=usuario) return "PROHIBIDO";
            if(reserva is not null) return "YA_EXISTE";
            if(nueva.Inicio.UtcDateTime<=ahora || nueva.Fin<=nueva.Inicio) return "DATOS_INVALIDOS";
            var inicio=nueva.Inicio.UtcDateTime;var fin=nueva.Fin.UtcDateTime;
            var solapada=await (from r in db.Reservaciones join s in db.Solicitudes on r.IdSolicitud equals s.IdSolicitud
                join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
                where a.IdPropiedad==propiedadId && (r.EstadoReservacion=="Pendiente" || r.EstadoReservacion=="Confirmada")
                    && r.FechaFin>ahora && r.FechaInicio<fin && inicio<r.FechaFin select r.IdReservacion).AnyAsync();
            if(solapada) return "SOLAPADA";
            db.Reservaciones.Add(new Reservaciones {IdSolicitud=solicitud,FechaInicio=inicio,FechaFin=fin,MontoReserva=nueva.Monto,EstadoReservacion="Pendiente",FechaCreacion=ahora});
        } else {
            if(reserva is null) return "NO_ENCONTRADA";
            if(reserva.FechaFin<=ahora || reserva.EstadoReservacion is not ("Pendiente" or "Confirmada")) return "CONFLICTO";
            if(confirmar && v.IdUsuario==usuario) return "PROHIBIDO";
            if(confirmar && reserva.EstadoReservacion!="Pendiente") return "CONFLICTO";
            reserva.EstadoReservacion=confirmar ? "Confirmada" : "Cancelada";
            if(!confirmar) {
                var borradores=await db.Contratos.Where(c=>c.IdSolicitud==solicitud && c.EstadoContrato=="Borrador").ToListAsync();
                foreach(var borrador in borradores)borrador.EstadoContrato="Cancelado";
            }
        }
        await EventosSolicitud.Agregar(db,solicitud,nueva is not null ? "Reserva propuesta" : confirmar ? "Reserva confirmada" : "Reserva cancelada",$"/solicitudes/{solicitud}/reserva");
        await db.SaveChangesAsync();await tx.CommitAsync();return null;
    }
}
