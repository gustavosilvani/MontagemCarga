namespace MontagemCarga.Domain.ValueObjects;

/// <summary>
/// Dados mínimos de um pedido para agrupamento (entrada do serviço de agrupamento).
/// </summary>
public partial record PedidoAgrupamentoInput(
    string Codigo,
    Guid FilialId,
    Guid? TipoOperacaoId,
    Guid? TipoDeCargaId,
    Guid? RotaFreteId,
    decimal PesoSaldoRestante,
    decimal? CubagemTotal,
    int? NumeroPaletes,
    DateTime DataCarregamentoPedido,
    string? DestinatarioCnpj,
    string? RemetenteCnpj,
    string? RecebedorCnpj,
    double? Latitude,
    double? Longitude,
    int? CanalEntregaPrioridade,
    int? CanalEntregaLimitePedidos,
    bool NaoUtilizarCapacidadeVeiculo,
    DateTime? PrevisaoEntrega,
    DateTime? JanelaEntregaInicioUtc,
    DateTime? JanelaEntregaFimUtc,
    int? TempoServicoMinutos,
    bool PedidoBloqueado,
    bool LiberadoMontagemCarga,
    IReadOnlyList<PedidoItemAgrupamentoInput> Itens);

public partial record PedidoAgrupamentoInput
{
    public PedidoAgrupamentoInput(
        string Codigo,
        Guid FilialId,
        Guid? TipoOperacaoId,
        Guid? TipoDeCargaId,
        Guid? RotaFreteId,
        decimal PesoSaldoRestante,
        DateTime DataCarregamentoPedido,
        string? DestinatarioCnpj,
        string? RemetenteCnpj,
        string? RecebedorCnpj,
        double? Latitude,
        double? Longitude,
        int? CanalEntregaPrioridade,
        int? CanalEntregaLimitePedidos,
        bool NaoUtilizarCapacidadeVeiculo,
        DateTime? PrevisaoEntrega,
        bool PedidoBloqueado,
        bool LiberadoMontagemCarga)
        : this(
            Codigo,
            FilialId,
            TipoOperacaoId,
            TipoDeCargaId,
            RotaFreteId,
            PesoSaldoRestante,
            null,
            null,
            DataCarregamentoPedido,
            DestinatarioCnpj,
            RemetenteCnpj,
            RecebedorCnpj,
            Latitude,
            Longitude,
            CanalEntregaPrioridade,
            CanalEntregaLimitePedidos,
            NaoUtilizarCapacidadeVeiculo,
            PrevisaoEntrega,
            null,
            null,
            null,
            PedidoBloqueado,
            LiberadoMontagemCarga,
            Array.Empty<PedidoItemAgrupamentoInput>())
    {
    }
}

public record PedidoItemAgrupamentoInput(
    string Codigo,
    decimal Peso,
    decimal Quantidade,
    decimal Saldo,
    string? Descricao);
