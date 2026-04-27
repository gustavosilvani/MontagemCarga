using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Domain.Interfaces;

/// <summary>
/// Repositório de carregamentos.
/// </summary>
public interface ICarregamentoRepository
{
    Task<Carregamento?> ObterPorIdAsync(Guid embarcadorId, Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Carregamento> Items, int Total)> ListarAsync(Guid embarcadorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Carregamento> InserirAsync(Carregamento carregamento, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ReservarNumerosAsync(Guid filialId, int quantidade, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carregamento>> CriarLoteAsync(
        Guid embarcadorId,
        Guid? empresaId,
        IReadOnlyList<CarregamentoPlanejadoInput> carregamentos,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>>? numerosReservadosPorFilial = null,
        CancellationToken cancellationToken = default);
    Task AtualizarAsync(Carregamento carregamento, CancellationToken cancellationToken = default);
}
