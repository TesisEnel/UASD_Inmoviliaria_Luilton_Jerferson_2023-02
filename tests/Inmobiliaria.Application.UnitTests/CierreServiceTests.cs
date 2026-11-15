using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;
namespace Inmobiliaria.Application.UnitTests;
public class CierreServiceTests
{
    [Theory]
    [InlineData(0,"Concepto","Ref")]
    [InlineData(-1,"Concepto","Ref")]
    [InlineData(1.001,"Concepto","Ref")]
    [InlineData(1," ","Ref")]
    [InlineData(1,"Concepto"," ")]
    public async Task PagoInvalidoNoPersiste(double monto,string concepto,string referencia) {
        var e=new Escenario();var repo=new Mock<ICierreRepository>();
        var result=await new CierreService(repo.Object,e.Acceso).Cambiar(e.Principal,1,"pago",new((decimal)monto,DateTimeOffset.UtcNow.AddMinutes(-1),concepto,referencia));
        Assert.Equal("DATOS_INVALIDOS",result);repo.VerifyNoOtherCalls();
    }
    [Fact] public async Task PagoFuturoNoPersiste() {
        var e=new Escenario();var repo=new Mock<ICierreRepository>();
        Assert.Equal("DATOS_INVALIDOS",await new CierreService(repo.Object,e.Acceso).Cambiar(e.Principal,1,"pago",new(10,DateTimeOffset.UtcNow.AddDays(1),"Alquiler","Ref")));repo.VerifyNoOtherCalls();
    }
    [Fact] public async Task AdministradorNoAccedeAContratosDeClientes() {
        var e=new Escenario("Administrador");var repo=new Mock<ICierreRepository>();
        await Assert.ThrowsAsync<AccesoDenegadoException>(()=>new CierreService(repo.Object,e.Acceso).Contratos(e.Principal,1));repo.VerifyNoOtherCalls();
    }
    [Fact] public async Task CompradorNoConsultaReportesAdministrativos() {
        var e=new Escenario("Comprador");var repo=new Mock<ICierreRepository>();
        await Assert.ThrowsAsync<AccesoDenegadoException>(()=>new CierreService(repo.Object,e.Acceso).Reportes(e.Principal));repo.VerifyNoOtherCalls();
    }
}
