namespace MontagemCarga.Domain.Enums;

/// <summary>
/// Tipo de roteirização VRP aplicada ao carregamento.
/// </summary>
public enum TipoMontagemCarregamentoVRP
{
    Nenhum = 0,
    VrpCapacidade = 1,
    VrpTimeWindows = 2,
    SimuladorFrete = 3
}
