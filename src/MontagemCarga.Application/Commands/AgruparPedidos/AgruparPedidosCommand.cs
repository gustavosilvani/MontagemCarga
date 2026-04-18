using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.AgruparPedidos;

/// <summary>
/// Command para agrupar pedidos (sem persistência).
/// </summary>
public class AgruparPedidosCommand : ICommand<AgruparResponseDto>
{
    public List<PedidoParaMontagemDto> Pedidos { get; }
    public ParametrosMontagemDto Parametros { get; }

    public AgruparPedidosCommand(List<PedidoParaMontagemDto> pedidos, ParametrosMontagemDto parametros)
    {
        Pedidos = pedidos ?? new List<PedidoParaMontagemDto>();
        Parametros = parametros ?? new ParametrosMontagemDto();
    }
}
