using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;
[Authorize,ApiController,Route("api/v1")]
public sealed class CierreController(CierreService cierre):ControllerBase
{
    [HttpGet("solicitudes/{id:int}/contratos")] public async Task<IActionResult> Contratos(int id) {var datos=await cierre.Contratos(User,id);return datos is null?NotFound():Ok(datos);}
    [HttpGet("solicitudes/{id:int}/pagos")] public async Task<IActionResult> Pagos(int id) {var datos=await cierre.Pagos(User,id);return datos is null?NotFound():Ok(datos);}
    [HttpPost("solicitudes/{id:int}/contratos/crear")] public async Task<IActionResult> Crear(int id)=>Resultado(await cierre.Cambiar(User,id,"crear"));
    [HttpPost("solicitudes/{id:int}/contratos/activar")] public async Task<IActionResult> Activar(int id)=>Resultado(await cierre.Cambiar(User,id,"activar"));
    [HttpPost("solicitudes/{id:int}/contratos/cancelar")] public async Task<IActionResult> Cancelar(int id)=>Resultado(await cierre.Cambiar(User,id,"cancelar"));
    [HttpPost("solicitudes/{id:int}/contratos/finalizar")] public async Task<IActionResult> Finalizar(int id)=>Resultado(await cierre.Cambiar(User,id,"finalizar"));
    [HttpPost("solicitudes/{id:int}/pagos")] public async Task<IActionResult> Pago(int id,PagoSimuladoRequest pago)=>Resultado(await cierre.Cambiar(User,id,"pago",pago));
    [HttpGet("notificaciones")] public async Task<IActionResult> Notificaciones()=>Ok(await cierre.Notificaciones(User));
    [HttpPost("notificaciones/{id:int}/leer")] public async Task<IActionResult> Leer(int id)=>await cierre.Leer(User,id)?NoContent():NotFound();
    [HttpGet("admin/reportes")] public async Task<IActionResult> Reportes()=>Ok(await cierre.Reportes(User));
    private IActionResult Resultado(string? error)=>error switch {
        null=>NoContent(),"NO_ENCONTRADO"=>NotFound(),"PROHIBIDO"=>Forbid(),"DATOS_INVALIDOS"=>BadRequest(new{mensaje="Revisa el importe, fecha, concepto y referencia del pago simulado."}),
        "DUPLICADO"=>Conflict(new{mensaje="La referencia ya fue registrada para esta solicitud."}),
        _=>Conflict(new{mensaje="Operación no permitida para el estado actual. Actualiza la información."})
    };
}
