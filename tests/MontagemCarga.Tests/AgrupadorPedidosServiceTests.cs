using Microsoft.Extensions.Logging.Abstractions;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.ValueObjects;
using MontagemCarga.Infrastructure.Services;
using MontagemCarga.Infrastructure.Services.Planning;
using Xunit;

namespace MontagemCarga.Tests;

public class AgrupadorPedidosServiceTests
{
    [Fact]
    public async Task Agrupar_DeveQuebrarPedidosEmMultiplosGruposQuandoCapacidadeNaoComportaTudo()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var tipoOperacaoId = Guid.NewGuid();
        var tipoDeCargaId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 1000m);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 2) });
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-700", filialId, tipoOperacaoId, tipoDeCargaId, peso: 700m),
            TestDataFactory.Pedido("PED-500", filialId, tipoOperacaoId, tipoDeCargaId, peso: 500m),
            TestDataFactory.Pedido("PED-400", filialId, tipoOperacaoId, tipoDeCargaId, peso: 400m)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Equal(2, resultado.Grupos.Count);
        Assert.Empty(resultado.PedidosNaoAgrupados);
        Assert.Equal(new[] { "PED-700" }, resultado.Grupos[0].CodigosPedido);
        Assert.Equal(new[] { "PED-500", "PED-400" }, resultado.Grupos[1].CodigosPedido);
    }

    [Fact]
    public async Task Agrupar_DeveSepararPorRotaOuIgnorarConformeParametro()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var rotaA = Guid.NewGuid();
        var rotaB = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 1000m);
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-A", filialId, rotaFreteId: rotaA, peso: 300m),
            TestDataFactory.Pedido("PED-B", filialId, rotaFreteId: rotaB, peso: 300m)
        };

        var resultadoComRota = await service.Agrupar(
            pedidos,
            TestDataFactory.Parametros(
                centroId,
                new[] { modelo },
                new[] { new DisponibilidadeFrotaInput(modelo.Id, 2) },
                ignorarRotaFrete: false));

        var resultadoIgnorandoRota = await service.Agrupar(
            pedidos,
            TestDataFactory.Parametros(
                centroId,
                new[] { modelo },
                new[] { new DisponibilidadeFrotaInput(modelo.Id, 2) },
                ignorarRotaFrete: true));

        Assert.Equal(2, resultadoComRota.Grupos.Count);
        Assert.Single(resultadoIgnorandoRota.Grupos);
    }

    [Fact]
    public async Task Agrupar_DeveSepararPorDestinatarioQuandoConfigurado()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 1000m);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 2) },
            agruparMesmoDestinatario: true);
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-1", filialId, destinatario: "11111111000191", peso: 250m),
            TestDataFactory.Pedido("PED-2", filialId, destinatario: "22222222000191", peso: 250m)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Equal(2, resultado.Grupos.Count);
    }

    [Fact]
    public async Task Agrupar_DeveRejeitarPedidosBloqueadosOuNaoLiberados()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo();
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 2) });
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-BLOQ", bloqueado: true, liberado: true),
            TestDataFactory.Pedido("PED-NL", bloqueado: false, liberado: false)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Empty(resultado.Grupos);
        Assert.Equal(2, resultado.PedidosNaoAgrupados.Count);
        Assert.Contains(resultado.PedidosNaoAgrupados, pedido => pedido.Codigo == "PED-BLOQ" && pedido.Motivo.Contains("bloqueado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(resultado.PedidosNaoAgrupados, pedido => pedido.Codigo == "PED-NL" && pedido.Motivo.Contains("liberado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Agrupar_DeveRespeitarDisponibilidadeERetornarPedidoNaoAlocadoQuandoEsgotar()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 800m);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 1) });
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-1", filialId, peso: 700m),
            TestDataFactory.Pedido("PED-2", filialId, peso: 700m)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Single(resultado.Grupos);
        Assert.Single(resultado.PedidosNaoAgrupados);
        Assert.Contains("disponibilidade", resultado.PedidosNaoAgrupados[0].Motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Agrupar_DeveDesconsiderarCapacidadeQuandoPedidoSolicitaNaoConsumirVeiculo()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 1000m);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 1) });
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-PESO", filialId, peso: 900m),
            TestDataFactory.Pedido("PED-SEM-CAP", filialId, peso: 900m, naoUtilizarCapacidade: true)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Single(resultado.Grupos);
        Assert.Equal(1800m, resultado.Grupos[0].PesoTotal);
        Assert.Equal(900m, resultado.Grupos[0].PesoConsideradoCapacidade);
    }

    [Fact]
    public async Task Agrupar_DeveAceitarCamposReservadosSemAlterarResultadoDoMotorAtual()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var tipoOperacaoId = Guid.NewGuid();
        var tipoDeCargaId = Guid.NewGuid();
        var rotaFreteId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();

        var parametrosBase = new ParametrosAgrupamentoInput(
            new DateTime(2026, 4, 1),
            centroId,
            TipoMontagemCarregamentoVRP.Nenhum,
            TipoOcupacaoMontagemCarregamentoVRP.Peso,
            10,
            NivelQuebraProdutoRoteirizar.Item,
            false,
            false,
            false,
            false,
            false,
            new[] { new DisponibilidadeFrotaInput(modeloId, 1) },
            new[] { new ModeloVeicularInput(modeloId, "Truck", 1000m, 0m, null, null) });

        var parametrosComReservados = new ParametrosAgrupamentoInput(
            new DateTime(2026, 4, 1),
            centroId,
            TipoMontagemCarregamentoVRP.Nenhum,
            TipoOcupacaoMontagemCarregamentoVRP.Peso,
            10,
            NivelQuebraProdutoRoteirizar.Pallet,
            false,
            false,
            false,
            false,
            false,
            new[] { new DisponibilidadeFrotaInput(modeloId, 1) },
            new[] { new ModeloVeicularInput(modeloId, "Truck", 1000m, 0m, 12m, 24) });

        var pedidoBase = new PedidoAgrupamentoInput(
            "PED-RESERVADO",
            filialId,
            tipoOperacaoId,
            tipoDeCargaId,
            rotaFreteId,
            500m,
            new DateTime(2026, 4, 1),
            "12345678000190",
            null,
            null,
            null,
            null,
            1,
            null,
            false,
            new DateTime(2026, 4, 2),
            false,
            true);

        var pedidoComReservados = new PedidoAgrupamentoInput(
            "PED-RESERVADO",
            filialId,
            tipoOperacaoId,
            tipoDeCargaId,
            rotaFreteId,
            500m,
            new DateTime(2026, 4, 1),
            "12345678000190",
            "11111111000191",
            "22222222000191",
            -23.5505,
            -46.6333,
            1,
            99,
            false,
            new DateTime(2026, 4, 2),
            false,
            true);

        var resultadoBase = await service.Agrupar(new[] { pedidoBase }, parametrosBase);
        var resultadoComReservados = await service.Agrupar(new[] { pedidoComReservados }, parametrosComReservados);

        Assert.Equal(resultadoBase.Grupos.Count, resultadoComReservados.Grupos.Count);
        Assert.Equal(resultadoBase.Grupos[0].CodigosPedido, resultadoComReservados.Grupos[0].CodigosPedido);
        Assert.Equal(resultadoBase.Grupos[0].PesoTotal, resultadoComReservados.Grupos[0].PesoTotal);
        Assert.Equal(resultadoBase.Grupos[0].PesoConsideradoCapacidade, resultadoComReservados.Grupos[0].PesoConsideradoCapacidade);
        Assert.Equal(resultadoBase.Grupos[0].QtdeEntregas, resultadoComReservados.Grupos[0].QtdeEntregas);
    }

    [Fact]
    public async Task Agrupar_DeveExigirCoordenadasDoCentroNosModosVrp()
    {
        var service = CreateService();
        var centroId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo();
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 1) },
            tipoMontagem: TipoMontagemCarregamentoVRP.VrpCapacidade,
            configuracaoRoteirizacao: new ConfiguracaoRoteirizacaoInput(40m, 10, 15));
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-1", latitude: -23.56, longitude: -46.64)
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.Agrupar(pedidos, parametros));

        Assert.Contains("LatitudeCentro", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Agrupar_DeveUsarRoutingProviderNosModosVrp()
    {
        var routingProvider = new FakeRoutingProvider(new RouteBuildResult(
            12.5m,
            48m,
            "_p~iF~ps|U_ulLnnqC_mqNvxq`@",
            new[]
            {
                new RouteStopPlan("PED-1", -23.56, -46.64, 1, new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 9, 12, 0, DateTimeKind.Utc), 6m, 12m),
                new RouteStopPlan("PED-2", -23.58, -46.62, 2, new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 9, 42, 0, DateTimeKind.Utc), 6.5m, 18m)
            }));
        var service = CreateService(routingProvider);
        var centroId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 2000m, cubagem: 20m, numeroPaletes: 10);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 1) },
            tipoMontagem: TipoMontagemCarregamentoVRP.VrpCapacidade,
            latitudeCentro: -23.5505,
            longitudeCentro: -46.6333,
            configuracaoRoteirizacao: new ConfiguracaoRoteirizacaoInput(40m, 10, 15));
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-1", peso: 500m, cubagemTotal: 3m, numeroPaletes: 2, latitude: -23.56, longitude: -46.64),
            TestDataFactory.Pedido("PED-2", peso: 400m, cubagemTotal: 2m, numeroPaletes: 1, latitude: -23.58, longitude: -46.62)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Single(resultado.Grupos);
        Assert.Equal(12.5m, resultado.Grupos[0].DistanciaEstimadaKm);
        Assert.Equal(48m, resultado.Grupos[0].DuracaoEstimadaMin);
        Assert.Equal("_p~iF~ps|U_ulLnnqC_mqNvxq`@", resultado.Grupos[0].RouteGeometry);
        Assert.True(routingProvider.WasCalled);
    }

    [Fact]
    public async Task Agrupar_DeveCalcularCustoSimuladoComDistanciaDaRotaOsrm()
    {
        var routingProvider = new FakeRoutingProvider(new RouteBuildResult(
            10m,
            35m,
            "encoded-route",
            new[]
            {
                new RouteStopPlan("PED-CUSTO", -23.56, -46.64, 1, null, null, 10m, 20m)
            }));
        var service = CreateService(routingProvider);
        var centroId = Guid.NewGuid();
        var modelo = TestDataFactory.Modelo(capacidadePeso: 2000m, cubagem: 20m, numeroPaletes: 10);
        var parametros = TestDataFactory.Parametros(
            centroId,
            new[] { modelo },
            new[] { new DisponibilidadeFrotaInput(modelo.Id, 1) },
            tipoMontagem: TipoMontagemCarregamentoVRP.SimuladorFrete,
            latitudeCentro: -23.5505,
            longitudeCentro: -46.6333,
            configuracaoRoteirizacao: new ConfiguracaoRoteirizacaoInput(40m, 10, 15),
            configuracaoSimulacaoFrete: new ConfiguracaoSimulacaoFreteInput(100m, 2m, 0.5m, 10m, 15m));
        var pedidos = new[]
        {
            TestDataFactory.Pedido("PED-CUSTO", peso: 500m, cubagemTotal: 2m, numeroPaletes: 1, latitude: -23.56, longitude: -46.64)
        };

        var resultado = await service.Agrupar(pedidos, parametros);

        Assert.Single(resultado.Grupos);
        Assert.Equal(405m, resultado.Grupos[0].CustoSimulado);
        Assert.Equal("encoded-route", resultado.Grupos[0].RouteGeometry);
    }

    private static AgrupadorPedidosService CreateService(IRoutingProvider? routingProvider = null)
    {
        return new AgrupadorPedidosService(
            routingProvider ?? new FakeRoutingProvider(new RouteBuildResult(0m, 0m, null, Array.Empty<RouteStopPlan>())),
            NullLogger<AgrupadorPedidosService>.Instance);
    }

    private sealed class FakeRoutingProvider : IRoutingProvider
    {
        private readonly RouteBuildResult _result;

        public FakeRoutingProvider(RouteBuildResult result)
        {
            _result = result;
        }

        public bool WasCalled { get; private set; }

        public Task<RouteBuildResult?> BuildRouteAsync(
            IReadOnlyList<RouteStopCandidate> paradas,
            ParametrosAgrupamentoInput parametros,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult<RouteBuildResult?>(_result);
        }
    }
}
