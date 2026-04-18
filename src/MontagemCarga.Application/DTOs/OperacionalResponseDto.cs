namespace MontagemCarga.Application.DTOs;

public class AlertaOperacionalDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Severidade { get; set; } = "info";
}

public class InconsistenciaOperacionalDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Severidade { get; set; } = "warning";
    public string Origem { get; set; } = "sessao";
    public string? Referencia { get; set; }
}

public class IndicadorOperacionalDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Destaque { get; set; }
}

public class ResumoOperacionalDto
{
    public int TotalPedidosSelecionados { get; set; }
    public int TotalGrupos { get; set; }
    public int TotalPedidosNaoAgrupados { get; set; }
    public int TotalEntregas { get; set; }
    public decimal PesoTotal { get; set; }
    public decimal CubagemTotal { get; set; }
    public int NumeroPaletesTotal { get; set; }
    public decimal DistanciaTotalKm { get; set; }
    public decimal DuracaoTotalMin { get; set; }
    public decimal? CustoTotal { get; set; }
    public string? NumeroCarregamentoReservadoInicial { get; set; }
}
