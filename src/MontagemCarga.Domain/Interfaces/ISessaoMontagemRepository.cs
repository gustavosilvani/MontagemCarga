using MontagemCarga.Domain.Entities;

namespace MontagemCarga.Domain.Interfaces;

public interface ISessaoMontagemRepository
{
    Task<SessaoMontagem?> ObterPorIdAsync(Guid embarcadorId, Guid id, CancellationToken cancellationToken = default);
    Task<SessaoMontagem> InserirAsync(SessaoMontagem sessao, CancellationToken cancellationToken = default);
    Task AtualizarAsync(SessaoMontagem sessao, CancellationToken cancellationToken = default);
}
