using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Tests;

internal static class TestDataFactory
{
    public static ModeloVeicularInput Modelo(
        Guid? id = null,
        decimal capacidadePeso = 1000m,
        decimal tolerancia = 0m,
        string descricao = "Truck",
        decimal? cubagem = null,
        int? numeroPaletes = null)
    {
        return new ModeloVeicularInput(
            id ?? Guid.NewGuid(),
            descricao,
            capacidadePeso,
            tolerancia,
            cubagem,
            numeroPaletes);
    }

    public static PedidoAgrupamentoInput Pedido(
        string codigo,
        Guid? filialId = null,
        Guid? tipoOperacaoId = null,
        Guid? tipoDeCargaId = null,
        Guid? rotaFreteId = null,
        decimal peso = 100m,
        decimal? cubagemTotal = null,
        int? numeroPaletes = null,
        DateTime? dataCarregamento = null,
        string? destinatario = "12345678000190",
        string? remetente = null,
        string? recebedor = null,
        double? latitude = null,
        double? longitude = null,
        int? prioridade = null,
        int? limitePedidosCanal = null,
        bool naoUtilizarCapacidade = false,
        DateTime? previsaoEntrega = null,
        DateTime? janelaEntregaInicioUtc = null,
        DateTime? janelaEntregaFimUtc = null,
        int? tempoServicoMinutos = null,
        bool bloqueado = false,
        bool liberado = true,
        IReadOnlyList<PedidoItemAgrupamentoInput>? itens = null)
    {
        return new PedidoAgrupamentoInput(
            codigo,
            filialId ?? Guid.NewGuid(),
            tipoOperacaoId,
            tipoDeCargaId,
            rotaFreteId,
            peso,
            cubagemTotal,
            numeroPaletes,
            dataCarregamento ?? new DateTime(2026, 4, 1),
            destinatario,
            remetente,
            recebedor,
            latitude,
            longitude,
            prioridade,
            limitePedidosCanal,
            naoUtilizarCapacidade,
            previsaoEntrega,
            janelaEntregaInicioUtc,
            janelaEntregaFimUtc,
            tempoServicoMinutos,
            bloqueado,
            liberado,
            itens ?? Array.Empty<PedidoItemAgrupamentoInput>());
    }

    public static ParametrosAgrupamentoInput Parametros(
        Guid centroCarregamentoId,
        IReadOnlyList<ModeloVeicularInput> modelos,
        IReadOnlyList<DisponibilidadeFrotaInput>? disponibilidades = null,
        bool agruparMesmoDestinatario = false,
        bool ignorarRotaFrete = false,
        bool permitirBloqueado = false,
        bool montagemPedidoProduto = false,
        bool utilizarDispCentro = false,
        TipoMontagemCarregamentoVRP tipoMontagem = TipoMontagemCarregamentoVRP.Nenhum,
        TipoOcupacaoMontagemCarregamentoVRP tipoOcupacao = TipoOcupacaoMontagemCarregamentoVRP.Peso,
        int quantidadeMaximaEntregas = 10,
        DateTime? dataPrevista = null,
        double? latitudeCentro = null,
        double? longitudeCentro = null,
        ConfiguracaoRoteirizacaoInput? configuracaoRoteirizacao = null,
        ConfiguracaoSimulacaoFreteInput? configuracaoSimulacaoFrete = null)
    {
        return new ParametrosAgrupamentoInput(
            dataPrevista ?? new DateTime(2026, 4, 1),
            centroCarregamentoId,
            latitudeCentro,
            longitudeCentro,
            tipoMontagem,
            tipoOcupacao,
            quantidadeMaximaEntregas,
            NivelQuebraProdutoRoteirizar.Item,
            agruparMesmoDestinatario,
            ignorarRotaFrete,
            permitirBloqueado,
            montagemPedidoProduto,
            utilizarDispCentro,
            disponibilidades ?? Array.Empty<DisponibilidadeFrotaInput>(),
            modelos,
            configuracaoRoteirizacao,
            configuracaoSimulacaoFrete);
    }
}
