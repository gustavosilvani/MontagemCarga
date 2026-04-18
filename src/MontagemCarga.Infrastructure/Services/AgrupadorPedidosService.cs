using Microsoft.Extensions.Logging;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;
using MontagemCarga.Domain.ValueObjects;
using MontagemCarga.Infrastructure.Services.Planning;

namespace MontagemCarga.Infrastructure.Services;

public class AgrupadorPedidosService : IAgrupadorPedidosService
{
    private readonly IRoutingProvider _routingProvider;
    private readonly ILogger<AgrupadorPedidosService> _logger;

    public AgrupadorPedidosService(
        IRoutingProvider routingProvider,
        ILogger<AgrupadorPedidosService> logger)
    {
        _routingProvider = routingProvider;
        _logger = logger;
    }

    public async Task<ResultadoAgrupamentoOutput> Agrupar(
        IReadOnlyList<PedidoAgrupamentoInput> pedidos,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureModelosDisponiveis(parametros);
        EnsureModePrerequisites(parametros);

        var rejeitados = new List<PedidoNaoAgrupadoOutput>();
        var unidades = ExpandirPedidosElegiveis(pedidos, parametros, rejeitados);
        if (unidades.Count == 0)
            return BuildResultado(Array.Empty<GrupoAgrupamentoOutput>(), rejeitados);

        var modelos = parametros.ModelosVeiculares
            .OrderBy(m => m.CapacidadePesoTransporte + m.ToleranciaPesoExtra)
            .ThenBy(m => m.Cubagem ?? decimal.MaxValue)
            .ThenBy(m => m.NumeroPaletes ?? int.MaxValue)
            .ThenBy(m => m.Descricao, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)
            .ToList();

        var disponibilidadePorModelo = BuildDisponibilidade(modelos, parametros);
        var grupos = new List<GroupState>();
        var creationIndex = 0;

        foreach (var grupoBase in unidades
                     .GroupBy(u => BuildGroupKey(u, parametros))
                     .OrderBy(g => g.Key.SortValue, StringComparer.Ordinal))
        {
            foreach (var unidade in grupoBase
                         .OrderBy(u => u.CanalEntregaPrioridade ?? 999)
                         .ThenBy(u => u.DataCarregamentoPedido)
                         .ThenBy(u => u.PrevisaoEntrega ?? DateTime.MaxValue)
                         .ThenByDescending(u => u.PesoReal)
                         .ThenBy(u => u.CodigoPedido, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(u => u.UnidadeCodigo, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidato = await SelectBestCandidateAsync(
                    unidade,
                    grupoBase.Key,
                    grupos.Where(g => g.Key == grupoBase.Key).ToList(),
                    modelos,
                    disponibilidadePorModelo,
                    parametros,
                    creationIndex,
                    cancellationToken);

                if (candidato is null)
                {
                    rejeitados.Add(new PedidoNaoAgrupadoOutput(
                        unidade.CodigoPedido,
                        DetermineRejectReason(modelos, disponibilidadePorModelo, unidade, parametros)));
                    continue;
                }

                if (candidato.Group is null)
                {
                    if (disponibilidadePorModelo.TryGetValue(candidato.Model.Id, out var disponibilidade) && disponibilidade != int.MaxValue)
                        disponibilidadePorModelo[candidato.Model.Id] = disponibilidade - 1;

                    grupos.Add(new GroupState(
                        grupoBase.Key,
                        creationIndex++,
                        candidato.Model,
                        candidato.Units.ToList(),
                        candidato.Plan));
                }
                else
                {
                    candidato.Group.Apply(candidato.Units.ToList(), candidato.Plan);
                }
            }
        }

        var saida = grupos
            .OrderBy(g => g.Key.SortValue, StringComparer.Ordinal)
            .ThenBy(g => g.CreationIndex)
            .Select(g => g.ToOutput(parametros))
            .ToList();

        _logger.LogInformation(
            "Agrupamento concluido com {Grupos} grupos e {Rejeitados} pedidos rejeitados.",
            saida.Count,
            rejeitados.Count);

        return BuildResultado(saida, rejeitados);
    }

    private static void EnsureModelosDisponiveis(ParametrosAgrupamentoInput parametros)
    {
        if (parametros.ModelosVeiculares.Count == 0)
            throw new BusinessRuleException("E necessario informar ao menos um modelo veicular elegivel.");
    }

    private static void EnsureModePrerequisites(ParametrosAgrupamentoInput parametros)
    {
        if (parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum)
            return;

        if (!parametros.LatitudeCentro.HasValue || !parametros.LongitudeCentro.HasValue)
            throw new BusinessRuleException("LatitudeCentro e LongitudeCentro sao obrigatorias para modos VRP.");

        if (parametros.ConfiguracaoRoteirizacao is null)
            throw new BusinessRuleException("ConfiguracaoRoteirizacao e obrigatoria para modos VRP.");

        if (parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.SimuladorFrete &&
            parametros.ConfiguracaoSimulacaoFrete is null)
            throw new BusinessRuleException("ConfiguracaoSimulacaoFrete e obrigatoria para o modo SimuladorFrete.");
    }

    private async Task<CandidateEvaluation?> SelectBestCandidateAsync(
        PlanningUnit unidade,
        GroupKey key,
        IReadOnlyList<GroupState> gruposExistentes,
        IReadOnlyList<ModeloVeicularInput> modelos,
        IReadOnlyDictionary<Guid, int> disponibilidadePorModelo,
        ParametrosAgrupamentoInput parametros,
        int creationIndex,
        CancellationToken cancellationToken)
    {
        var candidatos = new List<CandidateEvaluation>();

        foreach (var grupo in gruposExistentes)
        {
            var avaliacao = await EvaluateCandidateAsync(grupo, unidade, parametros, cancellationToken);
            if (avaliacao is not null)
                candidatos.Add(avaliacao);
        }

        foreach (var modelo in modelos.Where(modelo => disponibilidadePorModelo.GetValueOrDefault(modelo.Id, 0) > 0))
        {
            var avaliacao = await EvaluateNewGroupCandidateAsync(unidade, modelo, parametros, cancellationToken);
            if (avaliacao is not null)
                candidatos.Add(avaliacao);
        }

        return candidatos.Count == 0
            ? null
            : candidatos
                .OrderBy(c => c.CostRank)
                .ThenBy(c => c.PrimarySlack)
                .ThenBy(c => c.Plan.DistanciaEstimadaKm)
                .ThenBy(c => c.Group?.CreationIndex ?? creationIndex)
                .First();
    }

    private Task<CandidateEvaluation?> EvaluateCandidateAsync(GroupState grupo, PlanningUnit unidade, ParametrosAgrupamentoInput parametros, CancellationToken cancellationToken)
    {
        var unidades = grupo.Units.Concat(new[] { unidade }).ToList();
        return TryBuildEvaluationAsync(grupo, grupo.Model, unidades, parametros, cancellationToken);
    }

    private Task<CandidateEvaluation?> EvaluateNewGroupCandidateAsync(PlanningUnit unidade, ModeloVeicularInput modelo, ParametrosAgrupamentoInput parametros, CancellationToken cancellationToken)
    {
        var unidades = new List<PlanningUnit> { unidade };
        return TryBuildEvaluationAsync(null, modelo, unidades, parametros, cancellationToken);
    }

    private async Task<CandidateEvaluation?> TryBuildEvaluationAsync(
        GroupState? group,
        ModeloVeicularInput modelo,
        List<PlanningUnit> unidades,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken)
    {
        if (!RespectDeliveryLimit(unidades, parametros.QuantidadeMaximaEntregasRoteirizar) || !RespectCanalLimit(unidades))
            return null;

        var plan = await BuildPlanAsync(unidades, modelo, parametros, cancellationToken);
        if (plan is null)
            return null;

        return new CandidateEvaluation(
            group,
            modelo,
            unidades,
            plan,
            GetPrimarySlack(plan, modelo, parametros.TipoOcupacaoMontagemCarregamentoVRP),
            GetCostRank(plan, parametros.TipoMontagemCarregamentoVRP));
    }

    private async Task<RoutePlan?> BuildPlanAsync(
        IReadOnlyList<PlanningUnit> unidades,
        ModeloVeicularInput modelo,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken)
    {
        var pesoReal = decimal.Round(unidades.Sum(u => u.PesoReal), 4, MidpointRounding.AwayFromZero);
        var pesoCapacidade = decimal.Round(unidades.Sum(u => u.PesoConsideradoCapacidade), 4, MidpointRounding.AwayFromZero);
        var cubagem = decimal.Round(unidades.Sum(u => u.CubagemTotal), 4, MidpointRounding.AwayFromZero);
        var paletes = unidades.Sum(u => u.NumeroPaletes);

        if (!CanModelFitTotals(modelo, parametros.TipoOcupacaoMontagemCarregamentoVRP, pesoCapacidade, cubagem, paletes))
            return null;

        var stops = RoutePlanner.AggregateStops(unidades);
        var rota = parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum
            ? RoutePlanner.BuildFallbackRoute(stops, parametros)
            : await _routingProvider.BuildRouteAsync(stops, parametros, cancellationToken);

        if (rota is null)
            return null;

        var ocupacaoPeso = RoutePlanner.ComputePercent(pesoCapacidade, modelo.CapacidadePesoTransporte + modelo.ToleranciaPesoExtra);
        var ocupacaoCubagem = modelo.Cubagem.HasValue ? RoutePlanner.ComputePercent(cubagem, modelo.Cubagem.Value) : (decimal?)null;
        var ocupacaoPaletes = modelo.NumeroPaletes.HasValue && modelo.NumeroPaletes.Value > 0
            ? RoutePlanner.ComputePercent(paletes, modelo.NumeroPaletes.Value)
            : (decimal?)null;

        var custo = parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.SimuladorFrete &&
                    parametros.ConfiguracaoSimulacaoFrete is not null
            ? decimal.Round(
                parametros.ConfiguracaoSimulacaoFrete.CustoBase +
                (parametros.ConfiguracaoSimulacaoFrete.CustoPorKm * rota.DistanciaEstimadaKm) +
                (parametros.ConfiguracaoSimulacaoFrete.CustoPorKg * pesoReal) +
                (parametros.ConfiguracaoSimulacaoFrete.CustoPorMetroCubico * cubagem) +
                (parametros.ConfiguracaoSimulacaoFrete.CustoPorPallet * paletes),
                4,
                MidpointRounding.AwayFromZero)
            : (decimal?)null;

        return new RoutePlan(
            pesoReal,
            pesoCapacidade,
            cubagem,
            paletes,
            ocupacaoPeso,
            ocupacaoCubagem,
            ocupacaoPaletes,
            rota.DistanciaEstimadaKm,
            rota.DuracaoEstimadaMin,
            custo,
            rota.RouteGeometry,
            rota.Paradas);
    }

    private static bool CanModelFitTotals(ModeloVeicularInput modelo, TipoOcupacaoMontagemCarregamentoVRP tipoOcupacao, decimal pesoCapacidade, decimal cubagem, int paletes)
    {
        if (pesoCapacidade > (modelo.CapacidadePesoTransporte + modelo.ToleranciaPesoExtra))
            return false;
        if (modelo.Cubagem.HasValue && cubagem > modelo.Cubagem.Value)
            return false;
        if (modelo.NumeroPaletes.HasValue && paletes > modelo.NumeroPaletes.Value)
            return false;
        if (tipoOcupacao == TipoOcupacaoMontagemCarregamentoVRP.MetroCubico && !modelo.Cubagem.HasValue)
            return false;
        if (tipoOcupacao == TipoOcupacaoMontagemCarregamentoVRP.Pallet && !modelo.NumeroPaletes.HasValue)
            return false;
        return true;
    }

    private static decimal GetPrimarySlack(RoutePlan plan, ModeloVeicularInput modelo, TipoOcupacaoMontagemCarregamentoVRP tipoOcupacao) =>
        tipoOcupacao switch
        {
            TipoOcupacaoMontagemCarregamentoVRP.MetroCubico => modelo.Cubagem.HasValue ? decimal.Round(modelo.Cubagem.Value - plan.CubagemTotal, 4, MidpointRounding.AwayFromZero) : decimal.MaxValue,
            TipoOcupacaoMontagemCarregamentoVRP.Pallet => modelo.NumeroPaletes.HasValue ? modelo.NumeroPaletes.Value - plan.NumeroPaletesTotal : decimal.MaxValue,
            _ => decimal.Round((modelo.CapacidadePesoTransporte + modelo.ToleranciaPesoExtra) - plan.PesoConsideradoCapacidade, 4, MidpointRounding.AwayFromZero)
        };

    private static decimal GetCostRank(RoutePlan plan, TipoMontagemCarregamentoVRP tipoMontagem) =>
        tipoMontagem == TipoMontagemCarregamentoVRP.SimuladorFrete
            ? plan.CustoSimulado ?? decimal.MaxValue
            : decimal.Zero;

    private static bool RespectDeliveryLimit(IReadOnlyCollection<PlanningUnit> unidades, int quantidadeMaximaEntregas) =>
        quantidadeMaximaEntregas <= 0 ||
        unidades.Select(x => x.CodigoPedido).Distinct(StringComparer.OrdinalIgnoreCase).Count() <= quantidadeMaximaEntregas;

    private static bool RespectCanalLimit(IReadOnlyCollection<PlanningUnit> unidades)
    {
        foreach (var grupoCanal in unidades.Where(u => u.CanalEntregaPrioridade.HasValue && u.CanalEntregaLimitePedidos.HasValue).GroupBy(u => u.CanalEntregaPrioridade!.Value))
        {
            var limite = grupoCanal.Min(x => x.CanalEntregaLimitePedidos!.Value);
            var quantidade = grupoCanal.Select(x => x.CodigoPedido).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (quantidade > limite)
                return false;
        }
        return true;
    }

    private static Dictionary<Guid, int> BuildDisponibilidade(IReadOnlyList<ModeloVeicularInput> modelos, ParametrosAgrupamentoInput parametros)
    {
        if (parametros.Disponibilidades.Count == 0)
        {
            if (!parametros.UtilizarDispFrotaCentroDescCliente)
                throw new BusinessRuleException("Nao ha disponibilidade efetiva informada para o centro selecionado.");

            return modelos.ToDictionary(m => m.Id, _ => int.MaxValue);
        }

        var disponibilidade = parametros.Disponibilidades.GroupBy(d => d.ModeloVeicularId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        foreach (var modelo in modelos.Where(modelo => !disponibilidade.ContainsKey(modelo.Id)))
            disponibilidade[modelo.Id] = 0;
        return disponibilidade;
    }

    private static ResultadoAgrupamentoOutput BuildResultado(IReadOnlyList<GrupoAgrupamentoOutput> grupos, IReadOnlyList<PedidoNaoAgrupadoOutput> rejeitados)
    {
        var avisos = new List<string>();
        if (rejeitados.Count > 0)
        {
            avisos.Add($"{rejeitados.Count} pedido(s) nao foram agrupados.");
            avisos.AddRange(rejeitados.Select(x => x.Motivo).Distinct(StringComparer.OrdinalIgnoreCase));
        }
        return new ResultadoAgrupamentoOutput(grupos, rejeitados, avisos);
    }

    private static string DetermineRejectReason(IReadOnlyList<ModeloVeicularInput> modelos, IReadOnlyDictionary<Guid, int> disponibilidadePorModelo, PlanningUnit unidade, ParametrosAgrupamentoInput parametros)
    {
        var existeModeloComCapacidade = modelos.Any(modelo => CanModelFitTotals(modelo, parametros.TipoOcupacaoMontagemCarregamentoVRP, unidade.PesoConsideradoCapacidade, unidade.CubagemTotal, unidade.NumeroPaletes));
        if (!existeModeloComCapacidade)
            return "Nenhum modelo veicular elegivel comporta peso, cubagem e pallet da unidade.";
        if (!modelos.Any(modelo => disponibilidadePorModelo.GetValueOrDefault(modelo.Id, 0) > 0))
            return "Nao ha disponibilidade de frota restante para os modelos elegiveis.";
        return parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.VrpTimeWindows
            ? "Janela de atendimento inviavel para a combinacao atual de rota e capacidade."
            : "Pedido nao pode ser alocado em nenhum grupo disponivel.";
    }

    private static GroupKey BuildGroupKey(PlanningUnit pedido, ParametrosAgrupamentoInput parametros) =>
        new(
            pedido.FilialId,
            pedido.TipoOperacaoId,
            pedido.TipoDeCargaId,
            parametros.IgnorarRotaFrete ? null : pedido.RotaFreteId,
            parametros.AgruparPedidosMesmoDestinatario ? NormalizeToken(pedido.DestinatarioCnpj) : null,
            NormalizeToken(pedido.RecebedorCnpj),
            NormalizeToken(pedido.RemetenteCnpj),
            parametros.MontagemCarregamentoPedidoProduto ? pedido.ProdutoKey : null);

    private static string? NormalizeToken(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static List<PlanningUnit> ExpandirPedidosElegiveis(IReadOnlyList<PedidoAgrupamentoInput> pedidos, ParametrosAgrupamentoInput parametros, ICollection<PedidoNaoAgrupadoOutput> rejeitados)
    {
        var elegiveis = new List<PlanningUnit>(pedidos.Count);
        var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pedido in pedidos)
        {
            if (!codigos.Add(pedido.Codigo)) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido duplicado no payload.")); continue; }
            if (pedido.FilialId == Guid.Empty) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Filial invalida.")); continue; }
            if (pedido.PesoSaldoRestante <= 0) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "PesoSaldoRestante deve ser maior que zero.")); continue; }
            if (pedido.DataCarregamentoPedido == default) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "DataCarregamentoPedido e obrigatoria.")); continue; }
            if (pedido.DataCarregamentoPedido.Date > parametros.DataPrevistaCarregamento.Date) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido com data de carregamento posterior a data prevista informada.")); continue; }
            if (pedido.PedidoBloqueado && !parametros.PermitirPedidoBloqueado) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido bloqueado.")); continue; }
            if (!pedido.LiberadoMontagemCarga) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido nao liberado para montagem de carga.")); continue; }
            if (parametros.TipoMontagemCarregamentoVRP != TipoMontagemCarregamentoVRP.Nenhum && (!pedido.Latitude.HasValue || !pedido.Longitude.HasValue)) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido sem coordenadas para o modo VRP selecionado.")); continue; }
            if (parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.VrpTimeWindows && (!pedido.JanelaEntregaInicioUtc.HasValue || !pedido.JanelaEntregaFimUtc.HasValue)) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido sem janela de entrega para VrpTimeWindows.")); continue; }

            if (parametros.MontagemCarregamentoPedidoProduto)
            {
                if (pedido.Itens.Count == 0) { rejeitados.Add(new PedidoNaoAgrupadoOutput(pedido.Codigo, "Pedido sem itens para montagem por pedido-produto.")); continue; }
                elegiveis.AddRange(ExplodirPedidoPorProduto(pedido, parametros));
            }
            else
            {
                elegiveis.Add(CriarUnidadeDoPedido(pedido));
            }
        }
        return elegiveis;
    }

    private static PlanningUnit CriarUnidadeDoPedido(PedidoAgrupamentoInput pedido) =>
        new(
            pedido.Codigo,
            pedido.Codigo,
            null,
            pedido.FilialId,
            pedido.TipoOperacaoId,
            pedido.TipoDeCargaId,
            pedido.RotaFreteId,
            pedido.PesoSaldoRestante,
            pedido.NaoUtilizarCapacidadeVeiculo ? 0m : pedido.PesoSaldoRestante,
            pedido.CubagemTotal ?? 0m,
            pedido.NumeroPaletes ?? 0,
            pedido.DataCarregamentoPedido,
            pedido.DestinatarioCnpj,
            pedido.RemetenteCnpj,
            pedido.RecebedorCnpj,
            pedido.Latitude,
            pedido.Longitude,
            pedido.CanalEntregaPrioridade,
            pedido.CanalEntregaLimitePedidos,
            pedido.PrevisaoEntrega,
            pedido.JanelaEntregaInicioUtc,
            pedido.JanelaEntregaFimUtc,
            pedido.TempoServicoMinutos ?? 0);

    private static List<PlanningUnit> ExplodirPedidoPorProduto(PedidoAgrupamentoInput pedido, ParametrosAgrupamentoInput parametros)
    {
        var itens = pedido.Itens.Where(item => !string.IsNullOrWhiteSpace(item.Codigo) && (item.Peso > 0 || item.Saldo > 0 || item.Quantidade > 0)).ToList();
        if (itens.Count == 0) return new List<PlanningUnit>();
        var pesoBase = itens.Sum(item => item.Peso > 0 ? item.Peso : Math.Max(item.Saldo, item.Quantidade));
        if (pesoBase <= 0) pesoBase = pedido.PesoSaldoRestante;

        return itens.Select((item, index) =>
        {
            var pesoItem = item.Peso > 0 ? item.Peso : pedido.PesoSaldoRestante * (Math.Max(item.Saldo, item.Quantidade) / pesoBase);
            var proporcao = pedido.PesoSaldoRestante > 0 ? decimal.Round(pesoItem / pedido.PesoSaldoRestante, 8, MidpointRounding.AwayFromZero) : 0m;
            var cubagemItem = pedido.CubagemTotal.HasValue ? decimal.Round(pedido.CubagemTotal.Value * proporcao, 4, MidpointRounding.AwayFromZero) : 0m;
            var palletItem = pedido.NumeroPaletes.HasValue ? Math.Max(0, (int)Math.Round(pedido.NumeroPaletes.Value * (double)proporcao, MidpointRounding.AwayFromZero)) : 0;

            return new PlanningUnit(
                pedido.Codigo,
                $"{pedido.Codigo}::{item.Codigo}::{index + 1}",
                ResolveProdutoKey(item.Codigo, parametros.NivelQuebraProdutoRoteirizar),
                pedido.FilialId,
                pedido.TipoOperacaoId,
                pedido.TipoDeCargaId,
                pedido.RotaFreteId,
                pesoItem,
                pedido.NaoUtilizarCapacidadeVeiculo ? 0m : pesoItem,
                cubagemItem,
                palletItem,
                pedido.DataCarregamentoPedido,
                pedido.DestinatarioCnpj,
                pedido.RemetenteCnpj,
                pedido.RecebedorCnpj,
                pedido.Latitude,
                pedido.Longitude,
                pedido.CanalEntregaPrioridade,
                pedido.CanalEntregaLimitePedidos,
                pedido.PrevisaoEntrega,
                pedido.JanelaEntregaInicioUtc,
                pedido.JanelaEntregaFimUtc,
                pedido.TempoServicoMinutos ?? 0);
        }).ToList();
    }

    private static string? ResolveProdutoKey(string codigo, NivelQuebraProdutoRoteirizar nivel)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (nivel == NivelQuebraProdutoRoteirizar.Item) return normalizado;
        var partes = normalizado.Split(new[] { '-', '/', '.', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return normalizado;
        return nivel == NivelQuebraProdutoRoteirizar.Caixa ? partes[0] : "PALLET";
    }
}
