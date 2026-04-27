using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.ValueObjects;
using MontagemCarga.Infrastructure.Services.Planning;
using Xunit;

namespace MontagemCarga.Tests;

public class RoutePlannerTests
{
    private static ParametrosAgrupamentoInput Parametros(double? lat = null, double? lon = null, ConfiguracaoRoteirizacaoInput? cfg = null) =>
        new(
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            lat,
            lon,
            TipoMontagemCarregamentoVRP.Nenhum,
            TipoOcupacaoMontagemCarregamentoVRP.Peso,
            10,
            NivelQuebraProdutoRoteirizar.Item,
            false,
            false,
            false,
            false,
            false,
            Array.Empty<DisponibilidadeFrotaInput>(),
            Array.Empty<ModeloVeicularInput>(),
            cfg,
            null);

    [Fact]
    public void ToDecimal_DeveArredondarPara4Casas()
    {
        Assert.Equal(1.2346m, RoutePlanner.ToDecimal(1.23456789));
        Assert.Equal(0m, RoutePlanner.ToDecimal(0d));
        Assert.Equal(-1.5m, RoutePlanner.ToDecimal(-1.5d));
    }

    [Fact]
    public void ComputePercent_Decimal_CapacidadeZero_DeveRetornarZero()
    {
        Assert.Equal(0m, RoutePlanner.ComputePercent(100m, 0m));
        Assert.Equal(0m, RoutePlanner.ComputePercent(100m, -1m));
    }

    [Fact]
    public void ComputePercent_Decimal_DeveCalcularPercentual()
    {
        Assert.Equal(50m, RoutePlanner.ComputePercent(50m, 100m));
        Assert.Equal(150m, RoutePlanner.ComputePercent(150m, 100m));
    }

    [Fact]
    public void ComputePercent_Int_CapacidadeZero_DeveRetornarZero()
    {
        Assert.Equal(0m, RoutePlanner.ComputePercent(5, 0));
    }

    [Fact]
    public void ComputePercent_Int_DeveCalcular()
    {
        Assert.Equal(25m, RoutePlanner.ComputePercent(1, 4));
    }

    [Fact]
    public void NormalizeUtc_FromUnspecified_DeveAplicarUtcKind()
    {
        var dt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var result = RoutePlanner.NormalizeUtc(dt);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(dt.Ticks, result.Ticks);
    }

    [Fact]
    public void NormalizeUtc_FromUtc_DeveManter()
    {
        var dt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var result = RoutePlanner.NormalizeUtc(dt);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void NormalizeUtc_FromLocal_DeveConverter()
    {
        var dt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Local);
        var result = RoutePlanner.NormalizeUtc(dt);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void BuildFallbackRoute_SemParadas_DeveRetornarRotaVazia()
    {
        var result = RoutePlanner.BuildFallbackRoute(Array.Empty<RouteStopCandidate>(), Parametros());

        Assert.NotNull(result);
        Assert.Equal(0m, result!.DistanciaEstimadaKm);
        Assert.Empty(result.Paradas);
    }

    [Fact]
    public void BuildFallbackRoute_SemCoordenadas_DeveRetornarRotaSintetica()
    {
        var paradas = new[]
        {
            new RouteStopCandidate("PED-1", null, null, null, null, null, null, null, 0)
        };

        var result = RoutePlanner.BuildFallbackRoute(paradas, Parametros());

        Assert.NotNull(result);
        Assert.Single(result!.Paradas);
        Assert.Equal(0m, result.DistanciaEstimadaKm);
        Assert.Equal("PED-1", result.Paradas[0].PedidoCodigo);
        Assert.Equal(1, result.Paradas[0].OrdemEntrega);
    }

    [Fact]
    public void BuildFallbackRoute_ComCoordenadas_DeveCalcularDistanciaERetornar()
    {
        var paradas = new[]
        {
            new RouteStopCandidate("PED-1", -23.56, -46.64, null, null, null, null, null, 10),
            new RouteStopCandidate("PED-2", -23.58, -46.62, null, null, null, null, null, 5)
        };
        var cfg = new ConfiguracaoRoteirizacaoInput(45m, 5, 15);
        var result = RoutePlanner.BuildFallbackRoute(paradas, Parametros(-23.55, -46.65, cfg));

        Assert.NotNull(result);
        Assert.Equal(2, result!.Paradas.Count);
        Assert.True(result.DistanciaEstimadaKm > 0m);
        Assert.True(result.DuracaoEstimadaMin > 0m);
        Assert.NotNull(result.RouteGeometry);
    }

    [Fact]
    public void BuildFallbackRoute_RespeitaPrioridadeQuandoSintetico()
    {
        var paradas = new[]
        {
            new RouteStopCandidate("PED-A", null, null, 99, null, null, null, null, 0),
            new RouteStopCandidate("PED-B", null, null, 1, null, null, null, null, 0)
        };

        var result = RoutePlanner.BuildFallbackRoute(paradas, Parametros());

        Assert.NotNull(result);
        Assert.Equal("PED-B", result!.Paradas[0].PedidoCodigo);
        Assert.Equal("PED-A", result.Paradas[1].PedidoCodigo);
    }

    [Fact]
    public void BuildGeometry_SemCentro_DeveRetornarNull()
    {
        var paradas = new[] { new RouteStopPlan("X", -23.5, -46.6, 1, null, null, 0m, 0m) };
        Assert.Null(RoutePlanner.BuildGeometry(Parametros(), paradas));
    }

    [Fact]
    public void BuildGeometry_ComCentro_DeveRetornarPolylineNaoVazia()
    {
        var paradas = new[] { new RouteStopPlan("X", -23.56, -46.64, 1, null, null, 0m, 0m) };
        var encoded = RoutePlanner.BuildGeometry(Parametros(-23.55, -46.65), paradas);
        Assert.False(string.IsNullOrEmpty(encoded));
    }

    [Fact]
    public void BuildGeometry_SemParadas_ComCentro_DeveCodificarSomenteCentro()
    {
        var encoded = RoutePlanner.BuildGeometry(Parametros(-23.55, -46.65), Array.Empty<RouteStopPlan>());
        Assert.False(string.IsNullOrEmpty(encoded));
    }
}
