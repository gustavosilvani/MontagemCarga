using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class RemoverPedidoSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }
    public string CodigoPedido { get; }

    public RemoverPedidoSessaoMontagemCommand(Guid sessaoId, string codigoPedido)
    {
        SessaoId = sessaoId;
        CodigoPedido = codigoPedido ?? string.Empty;
    }
}
