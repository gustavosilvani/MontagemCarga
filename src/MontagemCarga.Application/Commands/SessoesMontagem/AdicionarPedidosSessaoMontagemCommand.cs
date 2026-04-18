using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class AdicionarPedidosSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }
    public List<PedidoParaMontagemDto> Pedidos { get; }

    public AdicionarPedidosSessaoMontagemCommand(Guid sessaoId, List<PedidoParaMontagemDto> pedidos)
    {
        SessaoId = sessaoId;
        Pedidos = pedidos ?? new List<PedidoParaMontagemDto>();
    }
}
