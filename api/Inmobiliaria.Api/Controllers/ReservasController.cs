using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;
[Authorize,ApiController,Route("api/v1/solicitudes/{solicitud:int}/reserva")]
public sealed class ReservasController(ReservasService reservas) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(int solicitud) {var r=await reservas.Listar(User,solicitud);return r is null ? NotFound() : Ok(r);}
    [HttpPost] public async Task<IActionResult> Crear(int solicitud,CrearReservaRequest request) => Resultado(await reservas.Cambiar(User,solicitud,request,false));
    [HttpPost("confirmar")] public async Task<IActionResult> Confirmar(int solicitud) => Resultado(await reservas.Cambiar(User,solicitud,null,true));
    [HttpPost("cancelar")] public async Task<IActionResult> Cancelar(int solicitud) => Resultado(await reservas.Cambiar(User,solicitud,null,false));
    private IActionResult Resultado(string? error) => error switch {
        null=>NoContent(),"NO_ENCONTRADA"=>NotFound(),"PROHIBIDO"=>Forbid(),
        "DATOS_INVALIDOS"=>BadRequest(new {mensaje="Indica un periodo futuro válido y un importe no negativo con máximo dos decimales."}),
        "SOLAPADA"=>Conflict(new {mensaje="La propiedad ya tiene otra reserva para ese periodo."}),
        "YA_EXISTE"=>Conflict(new {mensaje="Esta solicitud ya tiene una reserva. Se conserva su historial."}),
        _=>Conflict(new {mensaje="No se puede cambiar la reserva con el estado actual de la solicitud o propiedad."})
    };
}
