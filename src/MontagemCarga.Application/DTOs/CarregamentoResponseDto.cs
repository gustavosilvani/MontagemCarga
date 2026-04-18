using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Resposta de carregamento (persistido).
/// </summary>
public class CarregamentoResponseDto
{
    public Guid Id { get; set; }
    public string NumeroCarregamento { get; set; } = string.Empty;
    public SituacaoCarregamento SituacaoCarregamento { get; set; }
    public TipoMontagemCarga TipoMontagemCarga { get; set; }
    public TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP { get; set; }
    public Guid? ModeloVeicularId { get; set; }
    public Guid CentroCarregamentoId { get; set; }
    public double? LatitudeCentro { get; set; }
    public double? LongitudeCentro { get; set; }
    public DateTime DataCarregamentoCarga { get; set; }
    public decimal PesoCarregamento { get; set; }
    public decimal CubagemCarregamento { get; set; }
    public int NumeroPaletesCarregamento { get; set; }
    public decimal OcupacaoPesoPercentual { get; set; }
    public decimal? OcupacaoCubagemPercentual { get; set; }
    public decimal? OcupacaoPaletesPercentual { get; set; }
    public decimal DistanciaEstimadaKm { get; set; }
    public decimal DuracaoEstimadaMin { get; set; }
    public decimal? CustoSimulado { get; set; }
    public string? RouteGeometry { get; set; }
    public Guid? TipoDeCargaId { get; set; }
    public Guid? TipoOperacaoId { get; set; }
    public Guid FilialId { get; set; }
    public Guid? EmpresaId { get; set; }
    public List<AlertaOperacionalDto> AlertasOperacionais { get; set; } = new();
    public List<IndicadorOperacionalDto> IndicadoresOperacionais { get; set; } = new();
    public List<CarregamentoPedidoItemDto> Pedidos { get; set; } = new();
    public List<BlocoCarregamentoItemDto> Blocos { get; set; } = new();
    public List<ParadaCarregamentoItemDto> Paradas { get; set; } = new();
}

public class CarregamentoPedidoItemDto
{
    public string PedidoIdExterno { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public decimal Peso { get; set; }
    public int? Pallet { get; set; }
    public decimal? VolumeTotal { get; set; }
}

public class BlocoCarregamentoItemDto
{
    public string PedidoIdExterno { get; set; } = string.Empty;
    public string Bloco { get; set; } = string.Empty;
    public int OrdemCarregamento { get; set; }
    public int OrdemEntrega { get; set; }
}

public class ParadaCarregamentoItemDto
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
