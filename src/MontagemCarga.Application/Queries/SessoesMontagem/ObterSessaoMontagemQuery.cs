using MediatR;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Queries.SessoesMontagem;

public sealed class ObterSessaoMontagemQuery : IRequest<SessaoMontagemResponseDto?>
{
    public Guid SessaoId { get; }

    public ObterSessaoMontagemQuery(Guid sessaoId)
    {
        SessaoId = sessaoId;
    }
}
