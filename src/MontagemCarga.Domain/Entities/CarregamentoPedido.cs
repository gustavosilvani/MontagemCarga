namespace MontagemCarga.Domain.Entities;

/// <summary>
/// Relação N:M entre Carregamento e Pedido (referência externa por código/ID).
/// </summary>
public class CarregamentoPedido
{
    public Guid Id { get; protected set; }
    public Guid CarregamentoId { get; protected set; }
    public string PedidoIdExterno { get; protected set; } = string.Empty;
    public int Ordem { get; protected set; }
    public decimal Peso { get; protected set; }
    public int? Pallet { get; protected set; }
    public decimal? VolumeTotal { get; protected set; }

    public Carregamento Carregamento { get; protected set; } = null!;

    protected CarregamentoPedido() { }

    public CarregamentoPedido(Guid carregamentoId, string pedidoIdExterno, int ordem, decimal peso, int? pallet, decimal? volumeTotal)
    {
        Id = Guid.NewGuid();
        CarregamentoId = carregamentoId;
        PedidoIdExterno = pedidoIdExterno;
        Ordem = ordem;
        Peso = peso;
        Pallet = pallet;
        VolumeTotal = volumeTotal;
    }
}
