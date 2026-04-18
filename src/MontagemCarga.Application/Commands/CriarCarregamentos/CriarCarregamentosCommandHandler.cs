using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Application.Commands.CriarCarregamentos;

public class CriarCarregamentosCommandHandler : IRequestHandler<CriarCarregamentosCommand, List<CarregamentoResponseDto>>
{
    private readonly ICarregamentoRepository _repository;
    private readonly IAgrupadorPedidosService _agrupador;
    private readonly ITenantService _tenantService;

    public CriarCarregamentosCommandHandler(
        ICarregamentoRepository repository,
        IAgrupadorPedidosService agrupador,
        ITenantService tenantService)
    {
        _repository = repository;
        _agrupador = agrupador;
        _tenantService = tenantService;
    }

    public async Task<List<CarregamentoResponseDto>> Handle(CriarCarregamentosCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual();
        if (!embarcadorId.HasValue)
            throw new BusinessRuleException("Embarcador nao identificado.");

        if (request.Pedidos is null || request.Parametros is null)
            throw new BusinessRuleException("Pedidos e parametros sao obrigatorios para criar carregamentos.");

        var pedidosLookup = request.Pedidos
            .GroupBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var pedidos = request.Pedidos.Select(MontagemCargaProjection.MapPedido).ToList();
        var parametros = MontagemCargaProjection.MapParametros(request.Parametros);
        var resultado = await _agrupador.Agrupar(pedidos, parametros, cancellationToken);

        if (resultado.Grupos.Count == 0)
            throw new BusinessRuleException("Nenhum grupo elegivel foi gerado para criacao de carregamentos.");

        if (request.Grupos is { Count: > 0 })
            ValidatePreview(request.Grupos, resultado.Grupos);

        if (request.FilialId.HasValue && resultado.Grupos.Any(g => g.CodigoFilial != request.FilialId.Value))
            throw new ConflictException("A filial informada nao corresponde aos grupos calculados para criacao.");

        var agrupamento = MontagemCargaProjection.MapAgrupamentoResultado(resultado);
        var planejados = MontagemCargaProjection.BuildPlanos(agrupamento.Grupos, pedidosLookup, gerarUnicoBlocoPorRecebedor: false);

        var salvos = await _repository.CriarLoteAsync(embarcadorId.Value, request.EmpresaId, planejados, cancellationToken: cancellationToken);
        return salvos.Select(CarregamentoResponseMapper.Map).ToList();
    }

    private static void ValidatePreview(
        IReadOnlyList<GrupoPedidoResponseDto> previewInformado,
        IReadOnlyList<GrupoAgrupamentoOutput> previewAtual)
    {
        var esperado = previewAtual.Select(NormalizeGroup).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var informado = previewInformado.Select(NormalizeGroup).OrderBy(x => x, StringComparer.Ordinal).ToList();

        if (!esperado.SequenceEqual(informado, StringComparer.Ordinal))
            throw new ConflictException("O preview de agrupamento esta desatualizado. Gere o agrupamento novamente antes de criar carregamentos.");
    }

    private static string NormalizeGroup(GrupoAgrupamentoOutput grupo)
    {
        return string.Join("|",
            grupo.CentroCarregamentoId,
            grupo.CodigoFilial,
            grupo.ModeloVeicularSugeridoId,
            grupo.TipoOperacaoId,
            grupo.TipoDeCargaId,
            grupo.DataCarregamento.Date.ToString("yyyy-MM-dd"),
            string.Join(",", grupo.CodigosPedido.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
    }

    private static string NormalizeGroup(GrupoPedidoResponseDto grupo)
    {
        return string.Join("|",
            grupo.CentroCarregamentoId,
            grupo.CodigoFilial,
            grupo.ModeloVeicularSugeridoId,
            grupo.TipoOperacaoId,
            grupo.TipoDeCargaId,
            grupo.DataCarregamento.Date.ToString("yyyy-MM-dd"),
            string.Join(",", grupo.CodigosPedido.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
    }
}
