namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Request do endpoint POST /carregamentos.
/// A persistencia segura exige sempre pedidos + parametros.
/// Grupos sao opcionais e servem apenas para validar se o preview ainda e compativel.
/// </summary>
public class CriarCarregamentosRequestDto
{
    /// <summary>
    /// Grupos ja calculados (ex.: resultado de POST /agrupar) usados apenas como guarda de consistencia do preview.
    /// </summary>
    public List<GrupoPedidoResponseDto>? Grupos { get; set; }

    /// <summary>
    /// Pedidos elegiveis que serao persistidos no carregamento.
    /// </summary>
    public List<PedidoParaMontagemDto>? Pedidos { get; set; }

    /// <summary>
    /// Parametros efetivos usados no preview que antecede a criacao.
    /// </summary>
    public ParametrosMontagemDto? Parametros { get; set; }

    /// <summary>
    /// Filial para geracao do numero do carregamento e validacao opcional do preview.
    /// </summary>
    public Guid? FilialId { get; set; }

    /// <summary>
    /// Empresa opcional associada ao carregamento persistido.
    /// </summary>
    public Guid? EmpresaId { get; set; }
}
