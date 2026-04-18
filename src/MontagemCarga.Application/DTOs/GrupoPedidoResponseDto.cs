namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Um grupo de pedidos sugerido pelo agrupamento (resposta do POST /agrupar).
/// </summary>
public class GrupoPedidoResponseDto
{
    public List<string> CodigosPedido { get; set; } = new();
    public Guid? ModeloVeicularSugeridoId { get; set; }
    public Guid CentroCarregamentoId { get; set; }
    public double? LatitudeCentro { get; set; }
    public double? LongitudeCentro { get; set; }
    public Guid CodigoFilial { get; set; }
    public DateTime DataCarregamento { get; set; }
    public decimal PesoTotal { get; set; }
    public decimal PesoConsideradoCapacidade { get; set; }
    public decimal CubagemTotal { get; set; }
    public int NumeroPaletesTotal { get; set; }
    public decimal OcupacaoPesoPercentual { get; set; }
    public decimal? OcupacaoCubagemPercentual { get; set; }
    public decimal? OcupacaoPaletesPercentual { get; set; }
    public int QtdeEntregas { get; set; }
    public Guid? TipoOperacaoId { get; set; }
    public Guid? TipoDeCargaId { get; set; }
    public int TipoMontagemCarregamentoVRP { get; set; }
    public int TipoOcupacaoMontagemCarregamentoVRP { get; set; }
    public decimal DistanciaEstimadaKm { get; set; }
    public decimal DuracaoEstimadaMin { get; set; }
    public decimal? CustoSimulado { get; set; }
    public string? RouteGeometry { get; set; }
    public List<AlertaOperacionalDto> AlertasOperacionais { get; set; } = new();
    public List<IndicadorOperacionalDto> IndicadoresOperacionais { get; set; } = new();
    public List<ParadaPedidoResponseDto> Paradas { get; set; } = new();
}

public class ParadaPedidoResponseDto
{
    public string PedidoCodigo { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int OrdemEntrega { get; set; }
    public DateTime? ChegadaEstimadaUtc { get; set; }
    public DateTime? SaidaEstimadaUtc { get; set; }
    public decimal DistanciaDesdeAnteriorKm { get; set; }
    public decimal DuracaoDesdeAnteriorMin { get; set; }
}
