namespace MontagemCarga.Domain.Entities;

/// <summary>
/// Ordem de carregamento/entrega por pedido no carregamento.
/// </summary>
public class BlocoCarregamento
{
    public Guid Id { get; protected set; }
    public Guid CarregamentoId { get; protected set; }
    public string PedidoIdExterno { get; protected set; } = string.Empty;
    public string Bloco { get; protected set; } = string.Empty;
    public int OrdemCarregamento { get; protected set; }
    public int OrdemEntrega { get; protected set; }
    public double Latitude { get; protected set; }
    public double Longitude { get; protected set; }
    public DateTime? ChegadaEstimadaUtc { get; protected set; }
    public DateTime? SaidaEstimadaUtc { get; protected set; }
    public decimal DistanciaDesdeAnteriorKm { get; protected set; }
    public decimal DuracaoDesdeAnteriorMin { get; protected set; }

    public Carregamento Carregamento { get; protected set; } = null!;

    protected BlocoCarregamento() { }

    public BlocoCarregamento(
        Guid carregamentoId,
        string pedidoIdExterno,
        string bloco,
        int ordemCarregamento,
        int ordemEntrega,
        double latitude,
        double longitude,
        DateTime? chegadaEstimadaUtc,
        DateTime? saidaEstimadaUtc,
        decimal distanciaDesdeAnteriorKm,
        decimal duracaoDesdeAnteriorMin)
    {
        Id = Guid.NewGuid();
        CarregamentoId = carregamentoId;
        PedidoIdExterno = pedidoIdExterno;
        Bloco = bloco;
        OrdemCarregamento = ordemCarregamento;
        OrdemEntrega = ordemEntrega;
        Latitude = latitude;
        Longitude = longitude;
        ChegadaEstimadaUtc = chegadaEstimadaUtc;
        SaidaEstimadaUtc = saidaEstimadaUtc;
        DistanciaDesdeAnteriorKm = distanciaDesdeAnteriorKm;
        DuracaoDesdeAnteriorMin = duracaoDesdeAnteriorMin;
    }
}
