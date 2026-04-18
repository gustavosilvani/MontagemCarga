using Microsoft.EntityFrameworkCore;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Interfaces;
using MontagemCarga.Infrastructure.Persistence;

namespace MontagemCarga.Infrastructure.Repositories;

public class SessaoMontagemRepository : ISessaoMontagemRepository
{
    private readonly MontagemCargaDbContext _db;

    public SessaoMontagemRepository(MontagemCargaDbContext db)
    {
        _db = db;
    }

    public async Task<SessaoMontagem?> ObterPorIdAsync(Guid embarcadorId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SessoesMontagem
            .FirstOrDefaultAsync(x => x.EmbarcadorId == embarcadorId && x.Id == id, cancellationToken);
    }

    public async Task<SessaoMontagem> InserirAsync(SessaoMontagem sessao, CancellationToken cancellationToken = default)
    {
        _db.SessoesMontagem.Add(sessao);
        await _db.SaveChangesAsync(cancellationToken);
        return sessao;
    }

    public async Task AtualizarAsync(SessaoMontagem sessao, CancellationToken cancellationToken = default)
    {
        _db.SessoesMontagem.Update(sessao);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
