namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Request do endpoint POST /agrupar.
/// O endpoint aceita o contrato operacional atual do motor, incluindo modos deterministico, VRP e simulacao de frete.
/// </summary>
public class AgruparRequestDto
{
    /// <summary>
    /// Pedidos candidatos ao agrupamento.
    /// Campos operacionais ativos influenciam diretamente a montagem; campos ainda futuros continuam aceitos por compatibilidade.
    /// </summary>
    public List<PedidoParaMontagemDto> Pedidos { get; set; } = new();

    /// <summary>
    /// Parametros efetivos do preview.
    /// O comportamento do motor varia conforme o modo de montagem, ocupacao, disponibilidade, roteirizacao e simulacao configurados.
    /// </summary>
    public ParametrosMontagemDto Parametros { get; set; } = new();
}
