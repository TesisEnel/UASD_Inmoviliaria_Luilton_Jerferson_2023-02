using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;
namespace Inmobiliaria.Application.UnitTests;
public class ReservasServiceTests {
 [Theory]
 [InlineData(-1,2,100)] [InlineData(2,1,100)] [InlineData(1,2,-1)] [InlineData(1,2,1.001)]
 public async Task DatosInvalidos_NoPersisten(int inicio,int fin,double monto){var e=new Escenario();var repo=new Mock<IReservasRepository>();var result=await new ReservasService(repo.Object,e.Acceso).Cambiar(e.Principal,1,new(DateTimeOffset.UtcNow.AddDays(inicio),DateTimeOffset.UtcNow.AddDays(fin),(decimal)monto),false);Assert.Equal("DATOS_INVALIDOS",result);repo.VerifyNoOtherCalls();}
 [Fact] public async Task AdministradorSinAcceso(){var e=new Escenario("Administrador");var repo=new Mock<IReservasRepository>();await Assert.ThrowsAsync<AccesoDenegadoException>(()=>new ReservasService(repo.Object,e.Acceso).Listar(e.Principal,1));repo.VerifyNoOtherCalls();}
}
