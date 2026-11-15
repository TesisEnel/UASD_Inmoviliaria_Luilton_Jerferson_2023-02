using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Contracts;
public sealed record ContratoDto(int IdContrato,string Numero,DateTime Inicio,DateTime? Fin,decimal Monto,string Moneda,string Estado);
public sealed record PagoDto(int IdPago,decimal Monto,string Moneda,string Concepto,string? Referencia,DateTime? Fecha,string Estado);
public sealed record PagoSimuladoRequest(decimal Monto,DateTimeOffset Fecha,[Required,MaxLength(200)] string Concepto,[Required,MaxLength(200)] string Referencia);
public sealed record NotificacionDto(int IdNotificacion,string Titulo,string Mensaje,string? Url,bool Leida,DateTime Fecha);
public sealed record ReporteAdminDto(int IdReporte,string Categoria,string Descripcion,string Estado,DateTime Fecha);
