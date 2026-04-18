namespace MontagemCarga.Application.DTOs;

/// <summary>
/// Response do endpoint POST /agrupar.
/// </summary>
public class AgruparResponseDto
{
    public List<GrupoPedidoResponseDto> Grupos { get; set; } = new();
    public List<PedidoNaoAgrupadoResponseDto> PedidosNaoAgrupados { get; set; } = new();
    public List<AlertaOperacionalDto> AlertasOperacionais { get; set; } = new();
    public List<InconsistenciaOperacionalDto> InconsistenciasOperacionais { get; set; } = new();
    public ResumoOperacionalDto? Resumo { get; set; }
    public string? Erro { get; set; }
    public string? Aviso { get; set; }
}

public class PedidoNaoAgrupadoResponseDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}
