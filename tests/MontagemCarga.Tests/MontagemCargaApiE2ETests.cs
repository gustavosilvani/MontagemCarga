using System.Net;
using System.Net.Http.Json;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Application.Queries.ListarCarregamentos;
using MontagemCarga.Domain.Enums;
using Xunit;

namespace MontagemCarga.Tests;

public class MontagemCargaApiE2ETests : IClassFixture<MontagemCargaApiFactory>
{
    private readonly MontagemCargaApiFactory _factory;

    public MontagemCargaApiE2ETests(MontagemCargaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FluxoAutenticado_DeveAgruparCriarListarEDetalharComTenantNoJwt()
    {
        await _factory.ResetDatabaseAsync();

        var embarcadorId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var tipoOperacaoId = Guid.NewGuid();
        var tipoDeCargaId = Guid.NewGuid();
        var rotaFreteId = Guid.NewGuid();
        var centroCarregamentoId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(embarcadorId);

        var agruparRequest = new AgruparRequestDto
        {
            Pedidos =
            [
                new PedidoParaMontagemDto
                {
                    Codigo = "PED-E2E-1",
                    FilialId = filialId,
                    TipoOperacaoId = tipoOperacaoId,
                    TipoDeCargaId = tipoDeCargaId,
                    RotaFreteId = rotaFreteId,
                    PesoSaldoRestante = 450m,
                    DataCarregamentoPedido = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                    DestinatarioCnpj = "12345678000190",
                    RemetenteCnpj = "11111111000191",
                    RecebedorCnpj = "22222222000191",
                    Latitude = -23.5505,
                    Longitude = -46.6333,
                    CanalEntregaPrioridade = 1,
                    CanalEntregaLimitePedidos = 5,
                    NaoUtilizarCapacidadeVeiculo = false,
                    PrevisaoEntrega = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
                    PedidoBloqueado = false,
                    LiberadoMontagemCarga = true,
                    Itens =
                    [
                        new PedidoProdutoDto
                        {
                            Codigo = "ITEM-1",
                            Peso = 450m,
                            Quantidade = 1m,
                            Saldo = 1m
                        }
                    ]
                }
            ],
            Parametros = BuildParametros(centroCarregamentoId, modeloId)
        };

        var agruparResponse = await client.PostAsJsonAsync("/api/v1/montagem-carga/agrupar", agruparRequest);
        Assert.Equal(HttpStatusCode.OK, agruparResponse.StatusCode);

        var preview = await agruparResponse.Content.ReadFromJsonAsync<AgruparResponseDto>();
        Assert.NotNull(preview);
        Assert.Single(preview!.Grupos);
        Assert.Empty(preview.PedidosNaoAgrupados);

        var criarResponse = await client.PostAsJsonAsync("/api/v1/montagem-carga/carregamentos", new CriarCarregamentosRequestDto
        {
            Grupos = preview.Grupos,
            Pedidos = agruparRequest.Pedidos,
            Parametros = agruparRequest.Parametros,
            FilialId = filialId
        });

        Assert.Equal(HttpStatusCode.OK, criarResponse.StatusCode);

        var carregamentos = await criarResponse.Content.ReadFromJsonAsync<List<CarregamentoResponseDto>>();
        Assert.NotNull(carregamentos);
        Assert.Single(carregamentos!);

        var criado = carregamentos[0];
        Assert.Equal(TipoMontagemCarga.Automatica, criado.TipoMontagemCarga);
        Assert.Equal(450m, criado.PesoCarregamento);
        Assert.Single(criado.Pedidos);

        var listarResponse = await client.GetAsync("/api/v1/montagem-carga/carregamentos?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var listarResultado = await listarResponse.Content.ReadFromJsonAsync<ListarCarregamentosResult>();
        Assert.NotNull(listarResultado);
        Assert.Equal(1, listarResultado!.Total);
        Assert.Single(listarResultado.Items);

        var detalheResponse = await client.GetAsync($"/api/v1/montagem-carga/carregamentos/{criado.Id}");
        Assert.Equal(HttpStatusCode.OK, detalheResponse.StatusCode);

        var detalhe = await detalheResponse.Content.ReadFromJsonAsync<CarregamentoResponseDto>();
        Assert.NotNull(detalhe);
        Assert.Equal(criado.Id, detalhe!.Id);
        Assert.Equal(criado.NumeroCarregamento, detalhe.NumeroCarregamento);
        Assert.Single(detalhe.Blocos);
    }

    [Fact]
    public async Task RequisicaoAutenticada_SemTenant_DeveRetornarUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/montagem-carga/carregamentos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Contexto do embarcador", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwaggerJson_DeveRefletirContratoAtual_SemMencionarStub()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var swaggerJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stub", swaggerJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pedidos + parametros", swaggerJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fase 2", swaggerJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FluxoSessao_DeveCriarConsultarEPersistirSemDivergencia()
    {
        await _factory.ResetDatabaseAsync();

        var embarcadorId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var centroCarregamentoId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(embarcadorId);

        var pedido = new PedidoParaMontagemDto
        {
            Codigo = "PED-SESSION-1",
            FilialId = filialId,
            PesoSaldoRestante = 320m,
            CubagemTotal = 1.4m,
            NumeroPaletes = 1,
            DataCarregamentoPedido = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            DestinatarioCnpj = "12345678000190",
            RemetenteCnpj = "11111111000191",
            RecebedorCnpj = "22222222000191",
            Latitude = -23.5505,
            Longitude = -46.6333,
            PedidoBloqueado = false,
            LiberadoMontagemCarga = true,
            Itens =
            [
                new PedidoProdutoDto
                {
                    Codigo = "ITEM-SESSION-1",
                    Peso = 320m,
                    Quantidade = 1m,
                    Saldo = 1m
                }
            ]
        };

        var criarSessaoResponse = await client.PostAsJsonAsync("/api/v1/montagem-carga/sessoes", new CriarSessaoMontagemRequestDto
        {
            FilialId = filialId,
            Pedidos = [pedido],
            Parametros = BuildParametros(centroCarregamentoId, modeloId)
        });

        Assert.Equal(HttpStatusCode.OK, criarSessaoResponse.StatusCode);

        var sessao = await criarSessaoResponse.Content.ReadFromJsonAsync<SessaoMontagemResponseDto>();
        Assert.NotNull(sessao);
        Assert.NotEqual(Guid.Empty, sessao!.Id);
        Assert.Equal("Operador Teste", sessao.OperadorNome);
        Assert.Single(sessao.Agrupamento.Grupos);
        Assert.Single(sessao.NumerosCarregamentoReservados);

        var obterSessaoResponse = await client.GetAsync($"/api/v1/montagem-carga/sessoes/{sessao.Id}");
        Assert.Equal(HttpStatusCode.OK, obterSessaoResponse.StatusCode);

        var sessaoCarregada = await obterSessaoResponse.Content.ReadFromJsonAsync<SessaoMontagemResponseDto>();
        Assert.NotNull(sessaoCarregada);
        Assert.Equal(sessao.Id, sessaoCarregada!.Id);
        Assert.Single(sessaoCarregada.Agrupamento.Grupos);

        var persistirResponse = await client.PostAsJsonAsync($"/api/v1/montagem-carga/sessoes/{sessao.Id}/persistir", new PersistirSessaoRequestDto());
        Assert.Equal(HttpStatusCode.OK, persistirResponse.StatusCode);

        var sessaoPersistida = await persistirResponse.Content.ReadFromJsonAsync<SessaoMontagemResponseDto>();
        Assert.NotNull(sessaoPersistida);
        Assert.Equal(2, sessaoPersistida!.Situacao);
        Assert.Single(sessaoPersistida.CarregamentosCriados);
        Assert.Equal(sessao.NumerosCarregamentoReservados[0], sessaoPersistida.CarregamentosCriados[0].NumeroCarregamento);
    }

    [Fact]
    public async Task SessaoDeOutroOperador_DeveRetornarUnauthorizedMesmoNoMesmoTenant()
    {
        await _factory.ResetDatabaseAsync();

        var embarcadorId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var centroCarregamentoId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();

        using var clientCriacao = _factory.CreateAuthenticatedClient(embarcadorId, operadorId: "operador-a", operadorNome: "Operador A");
        using var clientOutroOperador = _factory.CreateAuthenticatedClient(embarcadorId, operadorId: "operador-b", operadorNome: "Operador B");

        var criarSessaoResponse = await clientCriacao.PostAsJsonAsync("/api/v1/montagem-carga/sessoes", new CriarSessaoMontagemRequestDto
        {
            FilialId = filialId,
            Pedidos =
            [
                new PedidoParaMontagemDto
                {
                    Codigo = "PED-OP-1",
                    FilialId = filialId,
                    PesoSaldoRestante = 320m,
                    DataCarregamentoPedido = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                    Latitude = -23.5505,
                    Longitude = -46.6333,
                    PedidoBloqueado = false,
                    LiberadoMontagemCarga = true
                }
            ],
            Parametros = BuildParametros(centroCarregamentoId, modeloId)
        });

        var sessao = await criarSessaoResponse.Content.ReadFromJsonAsync<SessaoMontagemResponseDto>();
        Assert.NotNull(sessao);

        var obterResponse = await clientOutroOperador.GetAsync($"/api/v1/montagem-carga/sessoes/{sessao!.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, obterResponse.StatusCode);
    }

    [Fact]
    public async Task SessaoDeveExporInconsistenciasOperacionaisNoWorkspace()
    {
        await _factory.ResetDatabaseAsync();

        var embarcadorId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var centroCarregamentoId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(embarcadorId);

        var criarSessaoResponse = await client.PostAsJsonAsync("/api/v1/montagem-carga/sessoes", new CriarSessaoMontagemRequestDto
        {
            FilialId = filialId,
            Pedidos =
            [
                new PedidoParaMontagemDto
                {
                    Codigo = "PED-VALIDO-1",
                    FilialId = filialId,
                    PesoSaldoRestante = 320m,
                    DataCarregamentoPedido = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                    Latitude = -23.5505,
                    Longitude = -46.6333,
                    PedidoBloqueado = false,
                    LiberadoMontagemCarga = true
                },
                new PedidoParaMontagemDto
                {
                    Codigo = "PED-BLOQUEADO-1",
                    FilialId = filialId,
                    PesoSaldoRestante = 220m,
                    DataCarregamentoPedido = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                    PedidoBloqueado = true,
                    LiberadoMontagemCarga = true
                }
            ],
            Parametros = BuildParametros(centroCarregamentoId, modeloId)
        });

        Assert.Equal(HttpStatusCode.OK, criarSessaoResponse.StatusCode);

        var sessao = await criarSessaoResponse.Content.ReadFromJsonAsync<SessaoMontagemResponseDto>();
        Assert.NotNull(sessao);
        Assert.NotEmpty(sessao!.InconsistenciasOperacionais);
        Assert.Contains(sessao.InconsistenciasOperacionais, item => item.Referencia == "PED-BLOQUEADO-1");
        Assert.NotEmpty(sessao.Agrupamento.InconsistenciasOperacionais);
    }

    private static ParametrosMontagemDto BuildParametros(Guid centroCarregamentoId, Guid modeloId)
    {
        return new ParametrosMontagemDto
        {
            DataPrevistaCarregamento = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            CentroCarregamentoId = centroCarregamentoId,
            TipoMontagemCarregamentoVRP = TipoMontagemCarregamentoVRP.Nenhum,
            TipoOcupacaoMontagemCarregamentoVRP = TipoOcupacaoMontagemCarregamentoVRP.Peso,
            QuantidadeMaximaEntregasRoteirizar = 10,
            NivelQuebraProdutoRoteirizar = NivelQuebraProdutoRoteirizar.Pallet,
            AgruparPedidosMesmoDestinatario = false,
            IgnorarRotaFrete = false,
            PermitirPedidoBloqueado = false,
            MontagemCarregamentoPedidoProduto = false,
            UtilizarDispFrotaCentroDescCliente = false,
            Disponibilidades =
            [
                new DisponibilidadeEfetivaDto
                {
                    ModeloVeicularId = modeloId,
                    Quantidade = 1
                }
            ],
            ModelosVeiculares =
            [
                new ModeloVeicularDto
                {
                    Id = modeloId,
                    Descricao = "Truck",
                    CapacidadePesoTransporte = 1000m,
                    ToleranciaPesoExtra = 50m,
                    Cubagem = 20m,
                    NumeroPaletes = 12
                }
            ]
        };
    }
}
