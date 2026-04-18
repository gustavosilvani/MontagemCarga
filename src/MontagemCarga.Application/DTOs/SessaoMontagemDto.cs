namespace MontagemCarga.Application.DTOs;

public class CriarSessaoMontagemRequestDto
{
    public Guid FilialId { get; set; }
    public Guid? EmpresaId { get; set; }
    public List<PedidoParaMontagemDto> Pedidos { get; set; } = new();
    public ParametrosMontagemDto Parametros { get; set; } = new();
}

public class AtualizarSessaoMontagemRequestDto
{
    public Guid? EmpresaId { get; set; }
    public List<PedidoParaMontagemDto> Pedidos { get; set; } = new();
    public ParametrosMontagemDto Parametros { get; set; } = new();
}

public class AdicionarPedidosSessaoRequestDto
{
    public List<PedidoParaMontagemDto> Pedidos { get; set; } = new();
}

public class PersistirSessaoRequestDto
{
    public Guid? EmpresaId { get; set; }
}

public class SessaoMontagemResponseDto
{
    public Guid Id { get; set; }
    public int Situacao { get; set; }
    public string SituacaoDescricao { get; set; } = string.Empty;
    public Guid FilialId { get; set; }
    public Guid? EmpresaId { get; set; }
    public string OperadorId { get; set; } = string.Empty;
    public string OperadorNome { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ProcessadaEmUtc { get; set; }
    public DateTime? PersistidaEmUtc { get; set; }
    public DateTime? CanceladaEmUtc { get; set; }
    public List<string> NumerosCarregamentoReservados { get; set; } = new();
    public ParametrosMontagemDto Parametros { get; set; } = new();
    public List<PedidoParaMontagemDto> Pedidos { get; set; } = new();
    public AgruparResponseDto Agrupamento { get; set; } = new();
    public ResumoOperacionalDto? Resumo { get; set; }
    public List<AlertaOperacionalDto> AlertasOperacionais { get; set; } = new();
    public List<InconsistenciaOperacionalDto> InconsistenciasOperacionais { get; set; } = new();
    public List<CarregamentoResponseDto> CarregamentosCriados { get; set; } = new();
}
