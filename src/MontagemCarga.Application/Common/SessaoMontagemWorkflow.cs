using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Common;

public interface ISessaoMontagemWorkflow
{
    Task<SessaoMontagemProcessamentoResult> ProcessarAsync(
        Guid filialId,
        Guid? empresaId,
        IReadOnlyList<PedidoParaMontagemDto> pedidos,
        ParametrosMontagemDto parametros,
        CancellationToken cancellationToken);

    Task<List<CarregamentoResponseDto>> PersistirAsync(
        Guid embarcadorId,
        SessaoMontagem sessao,
        Guid? empresaId,
        CancellationToken cancellationToken);

    SessaoMontagemResponseDto MapSessao(SessaoMontagem sessao);
}

public sealed record SessaoMontagemProcessamentoResult(
    AgruparResponseDto Agrupamento,
    IReadOnlyList<string> NumerosCarregamentoReservados);

internal sealed class SessaoMontagemWorkflow : ISessaoMontagemWorkflow
{
    private readonly IAgrupadorPedidosService _agrupador;
    private readonly ICarregamentoRepository _carregamentoRepository;

    public SessaoMontagemWorkflow(
        IAgrupadorPedidosService agrupador,
        ICarregamentoRepository carregamentoRepository)
    {
        _agrupador = agrupador;
        _carregamentoRepository = carregamentoRepository;
    }

    public async Task<SessaoMontagemProcessamentoResult> ProcessarAsync(
        Guid filialId,
        Guid? empresaId,
        IReadOnlyList<PedidoParaMontagemDto> pedidos,
        ParametrosMontagemDto parametros,
        CancellationToken cancellationToken)
    {
        if (pedidos.Count == 0)
        {
            var vazio = new AgruparResponseDto
            {
                Resumo = new ResumoOperacionalDto(),
                AlertasOperacionais = new List<AlertaOperacionalDto>(),
                InconsistenciasOperacionais = new List<InconsistenciaOperacionalDto>()
            };

            return new SessaoMontagemProcessamentoResult(vazio, Array.Empty<string>());
        }

        var entradas = pedidos.Select(MontagemCargaProjection.MapPedido).ToList();
        var parametrosInput = MontagemCargaProjection.MapParametros(parametros);
        var resultado = await _agrupador.Agrupar(entradas, parametrosInput, cancellationToken);
        var numerosReservados = resultado.Grupos.Count > 0
            ? await _carregamentoRepository.ReservarNumerosAsync(filialId, resultado.Grupos.Count, cancellationToken)
            : Array.Empty<string>();

        var agrupamento = MontagemCargaProjection.MapAgrupamentoResultado(resultado, numerosReservados);
        return new SessaoMontagemProcessamentoResult(agrupamento, numerosReservados);
    }

    public async Task<List<CarregamentoResponseDto>> PersistirAsync(
        Guid embarcadorId,
        SessaoMontagem sessao,
        Guid? empresaId,
        CancellationToken cancellationToken)
    {
        var sessaoDto = MapSessao(sessao);

        if (sessaoDto.Agrupamento.Grupos.Count == 0)
            throw new BusinessRuleException("A sessao nao possui grupos processados para persistencia.");

        var pedidosLookup = sessaoDto.Pedidos
            .GroupBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var planos = MontagemCargaProjection.BuildPlanos(
            sessaoDto.Agrupamento.Grupos,
            pedidosLookup,
            gerarUnicoBlocoPorRecebedor: false);

        var numerosReservadosPorFilial = sessaoDto.NumerosCarregamentoReservados.Count == 0
            ? null
            : new Dictionary<Guid, IReadOnlyList<string>>
            {
                [sessao.FilialId] = sessaoDto.NumerosCarregamentoReservados
            };

        var salvos = await _carregamentoRepository.CriarLoteAsync(
            embarcadorId,
            empresaId ?? sessao.EmpresaId,
            planos,
            numerosReservadosPorFilial,
            cancellationToken);

        return salvos.Select(CarregamentoResponseMapper.Map).ToList();
    }

    public SessaoMontagemResponseDto MapSessao(SessaoMontagem sessao)
    {
        var parametros = SessaoMontagemJson.DeserializeOrDefault(sessao.ParametrosJson, new ParametrosMontagemDto());
        var pedidos = SessaoMontagemJson.DeserializeOrDefault(sessao.PedidosJson, new List<PedidoParaMontagemDto>());
        var agrupamento = SessaoMontagemJson.DeserializeOrDefault(sessao.ResultadoJson, new AgruparResponseDto());
        var numeros = SessaoMontagemJson.DeserializeOrDefault(sessao.NumerosCarregamentoReservadosJson, new List<string>());
        var carregamentos = SessaoMontagemJson.DeserializeOrDefault(sessao.CarregamentosCriadosJson, new List<CarregamentoResponseDto>());

        if (agrupamento.AlertasOperacionais.Count == 0)
            agrupamento.AlertasOperacionais = MontagemCargaProjection.BuildAlertasAgrupamento(agrupamento.Grupos, agrupamento.PedidosNaoAgrupados);

        if (agrupamento.InconsistenciasOperacionais.Count == 0)
            agrupamento.InconsistenciasOperacionais = MontagemCargaProjection.BuildInconsistenciasAgrupamento(agrupamento.Grupos, agrupamento.PedidosNaoAgrupados);

        agrupamento.Resumo ??= MontagemCargaProjection.BuildResumo(agrupamento.Grupos, agrupamento.PedidosNaoAgrupados, numeros);

        return new SessaoMontagemResponseDto
        {
            Id = sessao.Id,
            Situacao = (int)sessao.Situacao,
            SituacaoDescricao = sessao.Situacao.ToString(),
            FilialId = sessao.FilialId,
            EmpresaId = sessao.EmpresaId,
            OperadorId = sessao.OperadorId,
            OperadorNome = sessao.OperadorNome,
            CreatedAt = sessao.CreatedAt,
            UpdatedAt = sessao.UpdatedAt,
            ProcessadaEmUtc = sessao.ProcessadaEmUtc,
            PersistidaEmUtc = sessao.PersistidaEmUtc,
            CanceladaEmUtc = sessao.CanceladaEmUtc,
            NumerosCarregamentoReservados = numeros,
            Parametros = parametros,
            Pedidos = pedidos,
            Agrupamento = agrupamento,
            Resumo = agrupamento.Resumo,
            AlertasOperacionais = agrupamento.AlertasOperacionais,
            InconsistenciasOperacionais = agrupamento.InconsistenciasOperacionais,
            CarregamentosCriados = carregamentos
        };
    }
}
