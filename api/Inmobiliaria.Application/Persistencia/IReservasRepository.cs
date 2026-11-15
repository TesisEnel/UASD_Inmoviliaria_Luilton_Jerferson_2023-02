using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;
public interface IReservasRepository
{
    Task<List<ReservaDto>?> Listar(int usuario,int solicitud);
    Task<string?> Cambiar(int usuario,int solicitud,CrearReservaRequest? nueva,bool confirmar);
}
