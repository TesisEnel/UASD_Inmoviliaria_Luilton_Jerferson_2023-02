using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;
public interface ICierreRepository
{
    Task<List<ContratoDto>?> Contratos(int usuario,int solicitud);
    Task<List<PagoDto>?> Pagos(int usuario,int solicitud);
    Task<string?> Cambiar(int usuario,int solicitud,string accion,PagoSimuladoRequest? pago);
    Task<List<NotificacionDto>> Notificaciones(int usuario);
    Task<bool> LeerNotificacion(int usuario,int id);
    Task<List<ReporteAdminDto>> Reportes();
}
