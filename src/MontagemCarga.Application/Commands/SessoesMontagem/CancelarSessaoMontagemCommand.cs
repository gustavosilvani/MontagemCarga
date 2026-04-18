using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class CancelarSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }

    public CancelarSessaoMontagemCommand(Guid sessaoId)
    {
        SessaoId = sessaoId;
    }
}
