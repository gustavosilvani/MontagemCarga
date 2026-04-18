using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class ReprocessarSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }

    public ReprocessarSessaoMontagemCommand(Guid sessaoId)
    {
        SessaoId = sessaoId;
    }
}
