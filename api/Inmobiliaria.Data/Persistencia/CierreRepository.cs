using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;
internal sealed class CierreRepository(ContextoDatos db) : ICierreRepository
{
    private IQueryable<Solicitudes> Propias(int usuario,int solicitud)=>
        from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
        join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad join v in db.Vendedores on p.IdVendedor equals v.IdVendedor
        join c in db.Compradores on s.IdComprador equals c.IdComprador
        where s.IdSolicitud==solicitud && (v.IdUsuario==usuario || c.IdUsuario==usuario) select s;
    public async Task<List<ContratoDto>?> Contratos(int usuario,int solicitud) {
        if(!await Propias(usuario,solicitud).AnyAsync()) return null;
        return await db.Contratos.AsNoTracking().Where(c=>c.IdSolicitud==solicitud).OrderByDescending(c=>c.IdContrato)
            .Select(c=>new ContratoDto(c.IdContrato,c.NumeroContrato,c.FechaInicio,c.FechaFin,c.MontoAcordado,c.Moneda,c.EstadoContrato)).ToListAsync();
    }
    public async Task<List<PagoDto>?> Pagos(int usuario,int solicitud) {
        if(!await Propias(usuario,solicitud).AnyAsync()) return null;
        return await db.Pagos.AsNoTracking().Where(p=>p.IdSolicitud==solicitud).OrderByDescending(p=>p.IdPago)
            .Select(p=>new PagoDto(p.IdPago,p.Monto,p.Moneda,p.Concepto,p.ReferenciaExterna,p.FechaPago,p.EstadoPago)).ToListAsync();
    }
    public async Task<string?> Cambiar(int usuario,int solicitud,string accion,PagoSimuladoRequest? pago) {
        if(!await Propias(usuario,solicitud).AnyAsync()) return "NO_ENCONTRADO";
        var propiedadId=await (from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion where s.IdSolicitud==solicitud select a.IdPropiedad).SingleAsync();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedadId}");
        var actual=await Propias(usuario,solicitud).SingleAsync();
        var propiedad=await db.Propiedades.SingleAsync(p=>p.IdPropiedad==propiedadId);
        var vendedor=await db.Vendedores.SingleAsync(v=>v.IdVendedor==propiedad.IdVendedor);
        var anuncio=await db.Publicaciones.SingleAsync(a=>a.IdPublicacion==actual.IdPublicacion);
        var reserva=await db.Reservaciones.SingleOrDefaultAsync(r=>r.IdSolicitud==solicitud);
        var contratos=await db.Contratos.Where(c=>c.IdSolicitud==solicitud).ToListAsync();
        var contrato=contratos.SingleOrDefault(c=>c.EstadoContrato is "Borrador" or "Activo");
        var ahora=DateTime.UtcNow;
        if(accion is "crear" or "activar") {
            if(propiedad.EliminadoEn!=null || propiedad.EstadoPropiedad!="Disponible" || anuncio.EstadoPublicacion!="Publicada"
                || actual.EstadoSolicitud!="Aceptada" || reserva is null || reserva.EstadoReservacion!="Confirmada" || reserva.FechaFin<=ahora) return "CONFLICTO";
        }
        if(accion=="crear") {
            if(vendedor.IdUsuario!=usuario) return "PROHIBIDO";
            if(contratos.Count!=0) return "CONFLICTO";
            db.Contratos.Add(new Contratos {IdSolicitud=solicitud,TipoContrato="Alquiler",NumeroContrato="ACA-"+Guid.NewGuid().ToString("N"),
                FechaInicio=reserva!.FechaInicio,FechaFin=reserva.FechaFin,MontoAcordado=anuncio.Precio,Moneda=anuncio.Moneda,EstadoContrato="Borrador",FechaCreacion=ahora});
        } else if(accion=="activar") {
            if(vendedor.IdUsuario==usuario) return "PROHIBIDO";
            if(contrato?.EstadoContrato!="Borrador") return "CONFLICTO";
            if(await db.Contratos.AnyAsync(c=>c.IdSolicitud!=solicitud && c.EstadoContrato=="Activo" && db.Solicitudes.Any(s=>s.IdSolicitud==c.IdSolicitud && db.Publicaciones.Any(a=>a.IdPublicacion==s.IdPublicacion && a.IdPropiedad==propiedadId)))) return "CONFLICTO";
            contrato.EstadoContrato="Activo";reserva!.EstadoReservacion="Convertida";actual.EstadoSolicitud="Completada";
            propiedad.EstadoPropiedad="Alquilada";propiedad.ActualizadoEn=ahora;
            var anuncios=await db.Publicaciones.Where(a=>a.IdPropiedad==propiedadId && a.EstadoPublicacion=="Publicada").ToListAsync();
            foreach(var a in anuncios) a.EstadoPublicacion="Pausada";
            var visitas=await (from visita in db.Visitas join s in db.Solicitudes on visita.IdSolicitud equals s.IdSolicitud
                join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
                where a.IdPropiedad==propiedadId && visita.FechaVisita>ahora && (visita.EstadoVisita=="Solicitada" || visita.EstadoVisita=="Confirmada") select visita).ToListAsync();
            foreach(var visita in visitas)visita.EstadoVisita="Cancelada";
        } else if(accion is "finalizar" or "cancelar") {
            if(contrato is null) return "CONFLICTO";
            if(accion=="finalizar") {
                if(vendedor.IdUsuario!=usuario) return "PROHIBIDO";
                if(contrato.EstadoContrato!="Activo") return "CONFLICTO";
                contrato.EstadoContrato="Finalizado";
                if(propiedad.EstadoPropiedad=="Alquilada") {propiedad.EstadoPropiedad="Disponible";propiedad.ActualizadoEn=ahora;}
            } else {
                if(contrato.EstadoContrato!="Borrador") return "CONFLICTO";
                contrato.EstadoContrato="Cancelado";
                if(reserva?.EstadoReservacion=="Confirmada") reserva.EstadoReservacion="Cancelada";
            }
        } else if(accion=="pago") {
            if(vendedor.IdUsuario!=usuario) return "PROHIBIDO";
            if(contrato?.EstadoContrato!="Activo" || pago is null) return "CONFLICTO";
            var referencia=pago.Referencia.Trim();
            if(await db.Pagos.AnyAsync(p=>p.IdSolicitud==solicitud && p.ReferenciaExterna==referencia)) return "DUPLICADO";
            db.Pagos.Add(new Pagos {IdComprador=actual.IdComprador,IdSolicitud=solicitud,IdReservacion=reserva?.IdReservacion,
                Monto=pago.Monto,Moneda=contrato.Moneda,Concepto=pago.Concepto.Trim(),MetodoPago="Simulado",ReferenciaExterna=referencia,
                EstadoPago="Pagado",FechaPago=pago.Fecha.UtcDateTime,FechaRegistro=ahora});
        } else return "CONFLICTO";
        await EventosSolicitud.Agregar(db,solicitud,accion switch {"crear"=>"Contrato académico propuesto","activar"=>"Alquiler académico activado","pago"=>"Pago simulado registrado","finalizar"=>"Alquiler finalizado",_=>"Contrato cancelado"},$"/solicitudes/{solicitud}/contrato");
        db.HistorialAcciones.Add(new HistorialAcciones {IdUsuario=usuario,Accion="Academico:"+accion,TablaAfectada="Solicitudes",IdRegistro=solicitud,FechaAccion=ahora});
        await db.SaveChangesAsync();await tx.CommitAsync();return null;
    }
    public Task<List<NotificacionDto>> Notificaciones(int usuario)=>db.Notificaciones.AsNoTracking().Where(n=>n.IdUsuarioDestino==usuario)
        .OrderByDescending(n=>n.IdNotificacion).Take(100).Select(n=>new NotificacionDto(n.IdNotificacion,n.Titulo,n.Mensaje,n.UrlDestino,n.Leida,n.FechaCreacion)).ToListAsync();
    public async Task<bool> LeerNotificacion(int usuario,int id) {
        var n=await db.Notificaciones.SingleOrDefaultAsync(n=>n.IdNotificacion==id && n.IdUsuarioDestino==usuario);
        if(n is null)return false;n.Leida=true;n.FechaLectura??=DateTime.UtcNow;await db.SaveChangesAsync();return true;
    }
    public Task<List<ReporteAdminDto>> Reportes()=>db.Reportes.AsNoTracking().OrderByDescending(r=>r.IdReporte).Take(200)
        .Select(r=>new ReporteAdminDto(r.IdReporte,r.Categoria,r.Descripcion,r.EstadoReporte,r.FechaReporte)).ToListAsync();
}
