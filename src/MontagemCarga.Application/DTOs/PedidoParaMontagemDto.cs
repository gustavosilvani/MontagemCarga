namespace MontagemCarga.Application.DTOs;

/// <summary>
/// DTO de pedido usado como entrada do preview e da criacao de carregamentos.
/// O contrato aceita o recorte operacional atual do motor, incluindo capacidade, VRP, janelas e simulacao.
/// </summary>
public class PedidoParaMontagemDto
{
    /// <summary>
    /// Identificador unico do pedido no payload.
    /// Ativo no motor atual.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Filial do pedido.
    /// Ativo no motor atual.
    /// </summary>
    public Guid FilialId { get; set; }

    /// <summary>
    /// Tipo de operacao do pedido.
    /// Ativo no motor atual.
    /// </summary>
    public Guid? TipoOperacaoId { get; set; }

    /// <summary>
    /// Tipo de carga do pedido.
    /// Ativo no motor atual.
    /// </summary>
    public Guid? TipoDeCargaId { get; set; }

    /// <summary>
    /// Rota de frete do pedido.
    /// Ativo no motor atual quando IgnorarRotaFrete = false.
    /// </summary>
    public Guid? RotaFreteId { get; set; }

    /// <summary>
    /// Peso restante elegivel para montagem.
    /// Ativo no motor atual.
    /// </summary>
    public decimal PesoSaldoRestante { get; set; }

    /// <summary>
    /// Cubagem total do pedido.
    /// Ativa na fase 2 para validacao de capacidade e simulacao de frete.
    /// </summary>
    public decimal? CubagemTotal { get; set; }

    /// <summary>
    /// Quantidade de pallets do pedido.
    /// Ativa na fase 2 para validacao de capacidade e simulacao de frete.
    /// </summary>
    public int? NumeroPaletes { get; set; }

    /// <summary>
    /// Data de carregamento do pedido.
    /// Ativo no motor atual.
    /// </summary>
    public DateTime DataCarregamentoPedido { get; set; }

    /// <summary>
    /// Documento do destinatario.
    /// Ativo apenas quando AgruparPedidosMesmoDestinatario = true.
    /// </summary>
    public string? DestinatarioCnpj { get; set; }

    /// <summary>
    /// Documento do remetente.
    /// Ativo na compatibilidade logistica do grupo.
    /// </summary>
    public string? RemetenteCnpj { get; set; }

    /// <summary>
    /// Documento do recebedor.
    /// Ativo na compatibilidade logistica do grupo.
    /// </summary>
    public string? RecebedorCnpj { get; set; }

    /// <summary>
    /// Latitude do pedido.
    /// Ativa nos modos VRP e no workspace de mapa.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude do pedido.
    /// Ativa nos modos VRP e no workspace de mapa.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Prioridade do canal de entrega.
    /// Ativa no motor atual apenas como criterio de ordenacao.
    /// </summary>
    public int? CanalEntregaPrioridade { get; set; }

    /// <summary>
    /// Limite de pedidos por canal.
    /// Ativo quando informado para limitar o grupo por canal.
    /// </summary>
    public int? CanalEntregaLimitePedidos { get; set; }

    /// <summary>
    /// Quando verdadeiro, o pedido nao consome capacidade de peso do veiculo.
    /// Ativo no motor atual.
    /// </summary>
    public bool NaoUtilizarCapacidadeVeiculo { get; set; }

    /// <summary>
    /// Previsao de entrega do pedido.
    /// Ativa no motor atual apenas como criterio de ordenacao.
    /// </summary>
    public DateTime? PrevisaoEntrega { get; set; }

    /// <summary>
    /// Inicio da janela de entrega em UTC.
    /// Ativa nos modos VRP com janela.
    /// </summary>
    public DateTime? JanelaEntregaInicioUtc { get; set; }

    /// <summary>
    /// Fim da janela de entrega em UTC.
    /// Ativa nos modos VRP com janela.
    /// </summary>
    public DateTime? JanelaEntregaFimUtc { get; set; }

    /// <summary>
    /// Tempo de atendimento da parada em minutos.
    /// Ativo nos modos VRP.
    /// </summary>
    public int? TempoServicoMinutos { get; set; }

    /// <summary>
    /// Indica se o pedido esta bloqueado.
    /// Ativo no motor atual.
    /// </summary>
    public bool PedidoBloqueado { get; set; }

    /// <summary>
    /// Indica se o pedido esta liberado para montagem.
    /// Ativo no motor atual.
    /// </summary>
    public bool LiberadoMontagemCarga { get; set; }

    /// <summary>
    /// Itens do pedido.
    /// Usados quando a montagem por pedido-produto esta habilitada.
    /// </summary>
    public List<PedidoProdutoDto>? Itens { get; set; }
}

/// <summary>
/// Item do pedido mantido por compatibilidade com evolucoes futuras do motor.
/// </summary>
public class PedidoProdutoDto
{
    public string Codigo { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public decimal Quantidade { get; set; }
    public decimal Saldo { get; set; }
    public string? Descricao { get; set; }
}
