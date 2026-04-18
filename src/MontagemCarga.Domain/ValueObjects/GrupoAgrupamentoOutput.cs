using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Domain.ValueObjects;

/// <summary>
/// Um grupo de pedidos sugerido pelo agrupador (saida).
/// </summary>
public record GrupoAgrupamentoOutput(
    IReadOnlyList<string> CodigosPedido,
    Guid? ModeloVeicularSugeridoId,
    Guid CentroCarregamentoId,
    double? LatitudeCentro,
    double? LongitudeCentro,
    Guid CodigoFilial,
    DateTime DataCarregamento,
    decimal PesoTotal,
    decimal PesoConsideradoCapacidade,
    decimal CubagemTotal,
    int NumeroPaletesTotal,
    decimal OcupacaoPesoPercentual,
    decimal? OcupacaoCubagemPercentual,
    decimal? OcupacaoPaletesPercentual,
    int QtdeEntregas,
    Guid? TipoOperacaoId,
    Guid? TipoDeCargaId,
    TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP,
    TipoOcupacaoMontagemCarregamentoVRP TipoOcupacaoMontagemCarregamentoVRP,
    decimal DistanciaEstimadaKm,
    decimal DuracaoEstimadaMin,
    decimal? CustoSimulado,
    string? RouteGeometry,
    IReadOnlyList<ParadaAgrupamentoOutput> Paradas);

public record ParadaAgrupamentoOutput(
    string PedidoCodigo,
    double Latitude,
    double Longitude,
    int OrdemEntrega,
    DateTime? ChegadaEstimadaUtc,
    DateTime? SaidaEstimadaUtc,
    decimal DistanciaDesdeAnteriorKm,
    decimal DuracaoDesdeAnteriorMin);
