using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using Xunit;

namespace MontagemCarga.Tests;

public class CarregamentoTests
{
    [Fact]
    public void Constructor_DeveNormalizarDataCarregamentoParaUtc()
    {
        var carregamento = new Carregamento(
            Guid.NewGuid(),
            "1",
            TipoMontagemCarga.Automatica,
            null,
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Unspecified),
            100m,
            null,
            null,
            Guid.NewGuid(),
            null);

        Assert.Equal(DateTimeKind.Utc, carregamento.DataCarregamentoCarga.Kind);
        Assert.Equal(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), carregamento.DataCarregamentoCarga);
    }
}
