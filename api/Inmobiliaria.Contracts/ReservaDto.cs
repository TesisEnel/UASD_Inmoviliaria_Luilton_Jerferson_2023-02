namespace Inmobiliaria.Contracts;
public sealed record CrearReservaRequest(DateTimeOffset Inicio, DateTimeOffset Fin, decimal Monto);
public sealed record ReservaDto(int IdReservacion, DateTime InicioUtc, DateTime FinUtc, decimal Monto, string Moneda, string Estado);
