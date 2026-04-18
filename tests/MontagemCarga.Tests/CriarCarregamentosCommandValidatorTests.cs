using MontagemCarga.Application.Commands.CriarCarregamentos;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Application.Validators;
using Xunit;

namespace MontagemCarga.Tests;

public class CriarCarregamentosCommandValidatorTests
{
    [Fact]
    public void Validate_DeveFalharQuandoReceberApenasGrupos()
    {
        var validator = new CriarCarregamentosCommandValidator();
        var command = new CriarCarregamentosCommand(
            new List<GrupoPedidoResponseDto>
            {
                new()
                {
                    CentroCarregamentoId = Guid.NewGuid(),
                    CodigoFilial = Guid.NewGuid(),
                    CodigosPedido = new List<string> { "PED-1" },
                    DataCarregamento = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)
                }
            },
            pedidos: null,
            parametros: null,
            filialId: Guid.NewGuid(),
            empresaId: null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("apenas grupos", StringComparison.OrdinalIgnoreCase));
    }
}
