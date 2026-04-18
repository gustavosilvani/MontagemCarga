using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Domain.ValueObjects;

public partial record PedidoCarregamentoPlanejadoInput(
    string CodigoPedido,
    decimal Peso,
    decimal? CubagemTotal,
    int? NumeroPaletes,
    int OrdemCarregamento,
    int OrdemEntrega,
    string Bloco);

public partial record PedidoCarregamentoPlanejadoInput
{
    public PedidoCarregamentoPlanejadoInput(
        string CodigoPedido,
        decimal Peso,
        int OrdemCarregamento,
        int OrdemEntrega,
        string Bloco)
        : this(CodigoPedido, Peso, null, null, OrdemCarregamento, OrdemEntrega, Bloco)
    {
    }
}

public record ParadaCarregamentoPlanejadaInput(
    string CodigoPedido,
    double Latitude,
    double Longitude,
    int OrdemEntrega,
    DateTime? ChegadaEstimadaUtc,
    DateTime? SaidaEstimadaUtc,
    decimal DistanciaDesdeAnteriorKm,
    decimal DuracaoDesdeAnteriorMin);

public partial record CarregamentoPlanejadoInput(
    Guid FilialId,
    Guid? ModeloVeicularId,
    Guid CentroCarregamentoId,
    double? LatitudeCentro,
    double? LongitudeCentro,
    DateTime DataCarregamento,
    decimal PesoTotal,
    decimal CubagemTotal,
    int NumeroPaletesTotal,
    decimal OcupacaoPesoPercentual,
    decimal? OcupacaoCubagemPercentual,
    decimal? OcupacaoPaletesPercentual,
    decimal DistanciaEstimadaKm,
    decimal DuracaoEstimadaMin,
    decimal? CustoSimulado,
    string? RouteGeometry,
    TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP,
    Guid? TipoDeCargaId,
    Guid? TipoOperacaoId,
    IReadOnlyList<PedidoCarregamentoPlanejadoInput> Pedidos,
    IReadOnlyList<ParadaCarregamentoPlanejadaInput> Paradas);

public partial record CarregamentoPlanejadoInput
{
    public CarregamentoPlanejadoInput(
        Guid FilialId,
        Guid? ModeloVeicularId,
        DateTime DataCarregamento,
        decimal PesoTotal,
        Guid? TipoDeCargaId,
        Guid? TipoOperacaoId,
        IReadOnlyList<PedidoCarregamentoPlanejadoInput> Pedidos)
        : this(
            FilialId,
            ModeloVeicularId,
            Guid.Empty,
            null,
            null,
            DataCarregamento,
            PesoTotal,
            0m,
            0,
            0m,
            null,
            null,
            0m,
            0m,
            null,
            null,
            TipoMontagemCarregamentoVRP.Nenhum,
            TipoDeCargaId,
            TipoOperacaoId,
            Pedidos,
            Array.Empty<ParadaCarregamentoPlanejadaInput>())
    {
    }
}
