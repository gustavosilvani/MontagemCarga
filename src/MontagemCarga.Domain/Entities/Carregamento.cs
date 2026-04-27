using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;

namespace MontagemCarga.Domain.Entities;

/// <summary>
/// Agregado raiz: carregamento (viagem/slot de carga).
/// </summary>
public class Carregamento
{
    public Guid Id { get; protected set; }
    public Guid EmbarcadorId { get; protected set; }
    public string NumeroCarregamento { get; protected set; } = string.Empty;
    public SituacaoCarregamento SituacaoCarregamento { get; protected set; }
    public TipoMontagemCarga TipoMontagemCarga { get; protected set; }
    public TipoMontagemCarregamentoVRP TipoMontagemCarregamentoVRP { get; protected set; }
    public Guid? ModeloVeicularId { get; protected set; }
    public Guid CentroCarregamentoId { get; protected set; }
    public double? LatitudeCentro { get; protected set; }
    public double? LongitudeCentro { get; protected set; }
    public DateTime DataCarregamentoCarga { get; protected set; }
    public decimal PesoCarregamento { get; protected set; }
    public decimal CubagemCarregamento { get; protected set; }
    public int NumeroPaletesCarregamento { get; protected set; }
    public decimal OcupacaoPesoPercentual { get; protected set; }
    public decimal? OcupacaoCubagemPercentual { get; protected set; }
    public decimal? OcupacaoPaletesPercentual { get; protected set; }
    public decimal DistanciaEstimadaKm { get; protected set; }
    public decimal DuracaoEstimadaMin { get; protected set; }
    public decimal? CustoSimulado { get; protected set; }
    public string? RouteGeometry { get; protected set; }
    public Guid? TipoDeCargaId { get; protected set; }
    public Guid? TipoOperacaoId { get; protected set; }
    public Guid FilialId { get; protected set; }
    public Guid? EmpresaId { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    private readonly List<CarregamentoPedido> _pedidos = new();
    public IReadOnlyCollection<CarregamentoPedido> Pedidos => _pedidos.AsReadOnly();

    private readonly List<BlocoCarregamento> _blocos = new();
    public IReadOnlyCollection<BlocoCarregamento> Blocos => _blocos.AsReadOnly();

    protected Carregamento() { }

    public Carregamento(
        Guid embarcadorId,
        string numeroCarregamento,
        TipoMontagemCarga tipoMontagemCarga,
        TipoMontagemCarregamentoVRP tipoMontagemCarregamentoVRP,
        Guid? modeloVeicularId,
        Guid centroCarregamentoId,
        double? latitudeCentro,
        double? longitudeCentro,
        DateTime dataCarregamentoCarga,
        decimal pesoCarregamento,
        decimal cubagemCarregamento,
        int numeroPaletesCarregamento,
        decimal ocupacaoPesoPercentual,
        decimal? ocupacaoCubagemPercentual,
        decimal? ocupacaoPaletesPercentual,
        decimal distanciaEstimadaKm,
        decimal duracaoEstimadaMin,
        decimal? custoSimulado,
        string? routeGeometry,
        Guid? tipoDeCargaId,
        Guid? tipoOperacaoId,
        Guid filialId,
        Guid? empresaId)
    {
        Id = Guid.NewGuid();
        EmbarcadorId = embarcadorId;
        NumeroCarregamento = numeroCarregamento;
        SituacaoCarregamento = SituacaoCarregamento.Montado;
        TipoMontagemCarga = tipoMontagemCarga;
        TipoMontagemCarregamentoVRP = tipoMontagemCarregamentoVRP;
        ModeloVeicularId = modeloVeicularId;
        CentroCarregamentoId = centroCarregamentoId;
        LatitudeCentro = latitudeCentro;
        LongitudeCentro = longitudeCentro;
        DataCarregamentoCarga = NormalizeDateTime(dataCarregamentoCarga);
        PesoCarregamento = pesoCarregamento;
        CubagemCarregamento = cubagemCarregamento;
        NumeroPaletesCarregamento = numeroPaletesCarregamento;
        OcupacaoPesoPercentual = ocupacaoPesoPercentual;
        OcupacaoCubagemPercentual = ocupacaoCubagemPercentual;
        OcupacaoPaletesPercentual = ocupacaoPaletesPercentual;
        DistanciaEstimadaKm = distanciaEstimadaKm;
        DuracaoEstimadaMin = duracaoEstimadaMin;
        CustoSimulado = custoSimulado;
        RouteGeometry = routeGeometry;
        TipoDeCargaId = tipoDeCargaId;
        TipoOperacaoId = tipoOperacaoId;
        FilialId = filialId;
        EmpresaId = empresaId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Carregamento(
        Guid embarcadorId,
        string numeroCarregamento,
        TipoMontagemCarga tipoMontagemCarga,
        Guid? modeloVeicularId,
        DateTime dataCarregamentoCarga,
        decimal pesoCarregamento,
        Guid? tipoDeCargaId,
        Guid? tipoOperacaoId,
        Guid filialId,
        Guid? empresaId)
        : this(
            embarcadorId,
            numeroCarregamento,
            tipoMontagemCarga,
            TipoMontagemCarregamentoVRP.Nenhum,
            modeloVeicularId,
            Guid.Empty,
            null,
            null,
            dataCarregamentoCarga,
            pesoCarregamento,
            0m,
            0,
            0m,
            null,
            null,
            0m,
            0m,
            null,
            null,
            tipoDeCargaId,
            tipoOperacaoId,
            filialId,
            empresaId)
    {
    }

    public void AdicionarPedido(string pedidoIdExterno, int ordem, decimal peso, int? pallet, decimal? volumeTotal)
    {
        _pedidos.Add(new CarregamentoPedido(Id, pedidoIdExterno, ordem, peso, pallet, volumeTotal));
        PesoCarregamento = _pedidos.Sum(p => p.Peso);
        CubagemCarregamento = _pedidos.Sum(p => p.VolumeTotal ?? 0m);
        NumeroPaletesCarregamento = _pedidos.Sum(p => p.Pallet ?? 0);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdicionarBloco(
        string pedidoIdExterno,
        string bloco,
        int ordemCarregamento,
        int ordemEntrega,
        double latitude,
        double longitude,
        DateTime? chegadaEstimadaUtc,
        DateTime? saidaEstimadaUtc,
        decimal distanciaDesdeAnteriorKm,
        decimal duracaoDesdeAnteriorMin)
    {
        _blocos.Add(new BlocoCarregamento(
            Id,
            pedidoIdExterno,
            bloco,
            ordemCarregamento,
            ordemEntrega,
            latitude,
            longitude,
            chegadaEstimadaUtc,
            saidaEstimadaUtc,
            distanciaDesdeAnteriorKm,
            duracaoDesdeAnteriorMin));
        UpdatedAt = DateTime.UtcNow;
    }

    public void IniciarTransito()
    {
        if (SituacaoCarregamento != SituacaoCarregamento.Montado &&
            SituacaoCarregamento != SituacaoCarregamento.Roteirizado)
            throw new BusinessRuleException(
                $"Carregamento em '{SituacaoCarregamento}' não pode iniciar trânsito. Estado requerido: Montado ou Roteirizado.");

        SituacaoCarregamento = SituacaoCarregamento.EmTransito;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Finalizar()
    {
        if (SituacaoCarregamento != SituacaoCarregamento.EmTransito)
            throw new BusinessRuleException(
                $"Carregamento em '{SituacaoCarregamento}' não pode ser finalizado. Estado requerido: EmTransito.");

        SituacaoCarregamento = SituacaoCarregamento.Finalizado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        if (SituacaoCarregamento == SituacaoCarregamento.Finalizado)
            throw new BusinessRuleException("Carregamento finalizado não pode ser cancelado.");

        SituacaoCarregamento = SituacaoCarregamento.Cancelado;
        UpdatedAt = DateTime.UtcNow;
    }

    private static DateTime NormalizeDateTime(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
