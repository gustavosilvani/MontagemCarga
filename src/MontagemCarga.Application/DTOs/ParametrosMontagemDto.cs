using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Parametros efetivos do agrupamento.
/// </summary>
public class ParametrosMontagemDto
{
    /// <summary>
    /// Data base do agrupamento.
    /// Ativa no motor atual.
    /// </summary>
    public DateTime DataPrevistaCarregamento { get; set; }

    /// <summary>
    /// Centro de carregamento selecionado.
    /// Ativo no motor atual.
    /// </summary>
    public Guid CentroCarregamentoId { get; set; }

    /// <summary>
    /// Latitude do centro/origem.
    /// Obrigatoria para modos VRP.
    /// </summary>
    public double? LatitudeCentro { get; set; }

    /// <summary>
    /// Longitude do centro/origem.
    /// Obrigatoria para modos VRP.
    /// </summary>
    public double? LongitudeCentro { get; set; }

    /// <summary>
    /// Tipo de montagem solicitado.
    /// Define o modo de montagem/roteirizacao.
    /// </summary>
    public TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP { get; set; }

    /// <summary>
    /// Tipo de ocupacao solicitado.
    /// Define a metrica primaria de otimizacao.
    /// </summary>
    public TipoOcupacaoMontagemCarregamentoVRP TipoOcupacaoMontagemCarregamentoVRP { get; set; }

    /// <summary>
    /// Limite maximo de entregas por grupo.
    /// Ativo no motor atual.
    /// </summary>
    public int QuantidadeMaximaEntregasRoteirizar { get; set; }

    /// <summary>
    /// Nivel de quebra para montagem por produto.
    /// Ativo quando a montagem por pedido-produto estiver habilitada.
    /// </summary>
    public NivelQuebraProdutoRoteirizar NivelQuebraProdutoRoteirizar { get; set; }

    /// <summary>
    /// Quando verdadeiro, separa grupos por destinatario.
    /// Ativo no motor atual.
    /// </summary>
    public bool AgruparPedidosMesmoDestinatario { get; set; }

    /// <summary>
    /// Quando verdadeiro, desconsidera RotaFreteId na chave do agrupamento.
    /// Ativo no motor atual.
    /// </summary>
    public bool IgnorarRotaFrete { get; set; }

    /// <summary>
    /// Quando verdadeiro, pedidos bloqueados podem participar do agrupamento.
    /// Ativo no motor atual.
    /// </summary>
    public bool PermitirPedidoBloqueado { get; set; }

    /// <summary>
    /// Solicita montagem por pedido-produto.
    /// Quando verdadeiro, o agrupamento opera sobre itens explodidos do pedido no preview e na sessao.
    /// </summary>
    public bool MontagemCarregamentoPedidoProduto { get; set; }

    /// <summary>
    /// Permite seguir sem disponibilidade explicita do centro.
    /// Ativo no motor atual.
    /// </summary>
    public bool UtilizarDispFrotaCentroDescCliente { get; set; }

    /// <summary>
    /// Disponibilidades efetivas do centro para a data.
    /// Ativas no motor atual.
    /// </summary>
    public List<DisponibilidadeEfetivaDto> Disponibilidades { get; set; } = new();

    /// <summary>
    /// Catalogo de modelos elegiveis.
    /// Catalogo de modelos elegiveis.
    /// </summary>
    public List<ModeloVeicularDto> ModelosVeiculares { get; set; } = new();

    /// <summary>
    /// Configuracao explicita de roteirizacao do embarcador.
    /// </summary>
    public ConfiguracaoRoteirizacaoDto? ConfiguracaoRoteirizacao { get; set; }

    /// <summary>
    /// Configuracao explicita para simulacao de frete.
    /// </summary>
    public ConfiguracaoSimulacaoFreteDto? ConfiguracaoSimulacaoFrete { get; set; }
}

/// <summary>
/// Modelo veicular elegivel para o agrupamento.
/// </summary>
public class ModeloVeicularDto
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Capacidade de peso ativa no motor atual.
    /// </summary>
    public decimal CapacidadePesoTransporte { get; set; }

    /// <summary>
    /// Tolerancia adicional de peso ativa no motor atual.
    /// </summary>
    public decimal ToleranciaPesoExtra { get; set; }

    /// <summary>
    /// Cubagem do modelo.
    /// Ativa na fase 2.
    /// </summary>
    public decimal? Cubagem { get; set; }

    /// <summary>
    /// Quantidade de pallets suportada.
    /// Ativa na fase 2.
    /// </summary>
    public int? NumeroPaletes { get; set; }
}

/// <summary>
/// Disponibilidade efetiva de um modelo no centro para a data consultada.
/// </summary>
public class DisponibilidadeEfetivaDto
{
    public Guid ModeloVeicularId { get; set; }
    public int Quantidade { get; set; }
}

public class ConfiguracaoRoteirizacaoDto
{
    public decimal VelocidadeMediaKmH { get; set; }
    public int TempoParadaPadraoMin { get; set; }
    public int ToleranciaJanelaMin { get; set; }
}

public class ConfiguracaoSimulacaoFreteDto
{
    public decimal CustoBase { get; set; }
    public decimal CustoPorKm { get; set; }
    public decimal CustoPorKg { get; set; }
    public decimal CustoPorMetroCubico { get; set; }
    public decimal CustoPorPallet { get; set; }
}
