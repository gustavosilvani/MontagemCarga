using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class PersistirSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }
    public Guid? EmpresaId { get; }

    public PersistirSessaoMontagemCommand(Guid sessaoId, Guid? empresaId)
    {
        SessaoId = sessaoId;
        EmpresaId = empresaId;
    }
}
