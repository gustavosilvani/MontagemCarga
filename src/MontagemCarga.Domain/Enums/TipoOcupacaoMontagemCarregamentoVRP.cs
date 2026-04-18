namespace MontagemCarga.Domain.Enums;

/// <summary>
/// Critério de ocupação usado no VRP (peso, volume, pallet).
/// </summary>
public enum TipoOcupacaoMontagemCarregamentoVRP
{
    Peso = 0,
    MetroCubico = 1,
    Pallet = 2
}
