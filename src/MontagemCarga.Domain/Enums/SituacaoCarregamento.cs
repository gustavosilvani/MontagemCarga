namespace MontagemCarga.Domain.Enums;

/// <summary>
/// Situação do carregamento (viagem/slot).
/// </summary>
public enum SituacaoCarregamento
{
    Pendente = 0,
    EmMontagem = 1,
    Montado = 2,
    Roteirizado = 3,
    EmTransito = 4,
    Finalizado = 5,
    Cancelado = 6
}
