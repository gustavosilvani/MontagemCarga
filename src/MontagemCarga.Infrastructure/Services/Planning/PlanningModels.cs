using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Infrastructure.Services.Planning;

internal sealed record GroupKey(
    Guid FilialId,
    Guid? TipoOperacaoId,
    Guid? TipoDeCargaId,
    Guid? RotaFreteId,
    string? DestinatarioCnpj,
    string? RecebedorCnpj,
    string? RemetenteCnpj,
    string? ProdutoKey)
{
    public string SortValue =>
        string.Join("|", FilialId, TipoOperacaoId, TipoDeCargaId, RotaFreteId, DestinatarioCnpj, RecebedorCnpj, RemetenteCnpj, ProdutoKey);
}

internal sealed record PlanningUnit(
    string CodigoPedido,
    string UnidadeCodigo,
    string? ProdutoKey,
    Guid FilialId,
    Guid? TipoOperacaoId,
    Guid? TipoDeCargaId,
    Guid? RotaFreteId,
    decimal PesoReal,
    decimal PesoConsideradoCapacidade,
    decimal CubagemTotal,
    int NumeroPaletes,
    DateTime DataCarregamentoPedido,
    string? DestinatarioCnpj,
    string? RemetenteCnpj,
    string? RecebedorCnpj,
    double? Latitude,
    double? Longitude,
    int? CanalEntregaPrioridade,
    int? CanalEntregaLimitePedidos,
    DateTime? PrevisaoEntrega,
    DateTime? JanelaEntregaInicioUtc,
    DateTime? JanelaEntregaFimUtc,
    int TempoServicoMinutos);

public sealed record RouteStopCandidate(
    string PedidoCodigo,
    double? Latitude,
    double? Longitude,
    int? CanalEntregaPrioridade,
    int? CanalEntregaLimitePedidos,
    DateTime? PrevisaoEntrega,
    DateTime? JanelaEntregaInicioUtc,
    DateTime? JanelaEntregaFimUtc,
    int TempoServicoMinutos);

public sealed record RouteStopPlan(
    string PedidoCodigo,
    double Latitude,
    double Longitude,
    int OrdemEntrega,
    DateTime? ChegadaEstimadaUtc,
    DateTime? SaidaEstimadaUtc,
    decimal DistanciaDesdeAnteriorKm,
    decimal DuracaoDesdeAnteriorMin);

public sealed record RouteBuildResult(
    decimal DistanciaEstimadaKm,
    decimal DuracaoEstimadaMin,
    string? RouteGeometry,
    IReadOnlyList<RouteStopPlan> Paradas);

internal sealed record RoutePlan(
    decimal PesoTotal,
    decimal PesoConsideradoCapacidade,
    decimal CubagemTotal,
    int NumeroPaletesTotal,
    decimal OcupacaoPesoPercentual,
    decimal? OcupacaoCubagemPercentual,
    decimal? OcupacaoPaletesPercentual,
    decimal DistanciaEstimadaKm,
    decimal DuracaoEstimadaMin,
    decimal? CustoSimulado,
    string? RouteGeometry,
    IReadOnlyList<RouteStopPlan> Paradas);

internal sealed record CandidateEvaluation(
    GroupState? Group,
    ModeloVeicularInput Model,
    IReadOnlyList<PlanningUnit> Units,
    RoutePlan Plan,
    decimal PrimarySlack,
    decimal CostRank);

internal sealed class GroupState
{
    public GroupState(
        GroupKey key,
        int creationIndex,
        ModeloVeicularInput model,
        List<PlanningUnit> units,
        RoutePlan plan)
    {
        Key = key;
        CreationIndex = creationIndex;
        Model = model;
        Units = units;
        Plan = plan;
    }

    public GroupKey Key { get; }
    public int CreationIndex { get; }
    public ModeloVeicularInput Model { get; }
    public List<PlanningUnit> Units { get; private set; }
    public RoutePlan Plan { get; private set; }

    public void Apply(List<PlanningUnit> units, RoutePlan plan)
    {
        Units = units;
        Plan = plan;
    }

    public GrupoAgrupamentoOutput ToOutput(ParametrosAgrupamentoInput parametros)
    {
        return new GrupoAgrupamentoOutput(
            Units.Select(u => u.CodigoPedido).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Model.Id,
            parametros.CentroCarregamentoId,
            parametros.LatitudeCentro,
            parametros.LongitudeCentro,
            Key.FilialId,
            parametros.DataPrevistaCarregamento,
            Plan.PesoTotal,
            Plan.PesoConsideradoCapacidade,
            Plan.CubagemTotal,
            Plan.NumeroPaletesTotal,
            Plan.OcupacaoPesoPercentual,
            Plan.OcupacaoCubagemPercentual,
            Plan.OcupacaoPaletesPercentual,
            Plan.Paradas.Count,
            Key.TipoOperacaoId,
            Key.TipoDeCargaId,
            parametros.TipoMontagemCarregamentoVRP,
            parametros.TipoOcupacaoMontagemCarregamentoVRP,
            Plan.DistanciaEstimadaKm,
            Plan.DuracaoEstimadaMin,
            Plan.CustoSimulado,
            Plan.RouteGeometry,
            Plan.Paradas.Select(p => new ParadaAgrupamentoOutput(
                p.PedidoCodigo,
                p.Latitude,
                p.Longitude,
                p.OrdemEntrega,
                p.ChegadaEstimadaUtc,
                p.SaidaEstimadaUtc,
                p.DistanciaDesdeAnteriorKm,
                p.DuracaoDesdeAnteriorMin)).ToList());
    }
}
