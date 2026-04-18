using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Domain.ValueObjects;

/// <summary>
/// Parâmetros para o agrupamento (centros, modelos, regras).
/// </summary>
public partial record ParametrosAgrupamentoInput(
    DateTime DataPrevistaCarregamento,
    Guid CentroCarregamentoId,
    double? LatitudeCentro,
    double? LongitudeCentro,
    TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP,
    TipoOcupacaoMontagemCarregamentoVRP TipoOcupacaoMontagemCarregamentoVRP,
    int QuantidadeMaximaEntregasRoteirizar,
    NivelQuebraProdutoRoteirizar NivelQuebraProdutoRoteirizar,
    bool AgruparPedidosMesmoDestinatario,
    bool IgnorarRotaFrete,
    bool PermitirPedidoBloqueado,
    bool MontagemCarregamentoPedidoProduto,
    bool UtilizarDispFrotaCentroDescCliente,
    IReadOnlyList<DisponibilidadeFrotaInput> Disponibilidades,
    IReadOnlyList<ModeloVeicularInput> ModelosVeiculares,
    ConfiguracaoRoteirizacaoInput? ConfiguracaoRoteirizacao,
    ConfiguracaoSimulacaoFreteInput? ConfiguracaoSimulacaoFrete);

public partial record ParametrosAgrupamentoInput
{
    public ParametrosAgrupamentoInput(
        DateTime DataPrevistaCarregamento,
        Guid CentroCarregamentoId,
        TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP,
        TipoOcupacaoMontagemCarregamentoVRP TipoOcupacaoMontagemCarregamentoVRP,
        int QuantidadeMaximaEntregasRoteirizar,
        NivelQuebraProdutoRoteirizar NivelQuebraProdutoRoteirizar,
        bool AgruparPedidosMesmoDestinatario,
        bool IgnorarRotaFrete,
        bool PermitirPedidoBloqueado,
        bool MontagemCarregamentoPedidoProduto,
        bool UtilizarDispFrotaCentroDescCliente,
        IReadOnlyList<DisponibilidadeFrotaInput> Disponibilidades,
        IReadOnlyList<ModeloVeicularInput> ModelosVeiculares)
        : this(
            DataPrevistaCarregamento,
            CentroCarregamentoId,
            null,
            null,
            TipoMontagemCarregamentoVRP,
            TipoOcupacaoMontagemCarregamentoVRP,
            QuantidadeMaximaEntregasRoteirizar,
            NivelQuebraProdutoRoteirizar,
            AgruparPedidosMesmoDestinatario,
            IgnorarRotaFrete,
            PermitirPedidoBloqueado,
            MontagemCarregamentoPedidoProduto,
            UtilizarDispFrotaCentroDescCliente,
            Disponibilidades,
            ModelosVeiculares,
            null,
            null)
    {
    }
}

/// <summary>
/// Capacidade do modelo veicular para agrupamento.
/// </summary>
public record ModeloVeicularInput(
    Guid Id,
    string Descricao,
    decimal CapacidadePesoTransporte,
    decimal ToleranciaPesoExtra,
    decimal? Cubagem,
    int? NumeroPaletes);

public record DisponibilidadeFrotaInput(
    Guid ModeloVeicularId,
    int Quantidade);

public record ConfiguracaoRoteirizacaoInput(
    decimal VelocidadeMediaKmH,
    int TempoParadaPadraoMin,
    int ToleranciaJanelaMin);

public record ConfiguracaoSimulacaoFreteInput(
    decimal CustoBase,
    decimal CustoPorKm,
    decimal CustoPorKg,
    decimal CustoPorMetroCubico,
    decimal CustoPorPallet);
