using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Commands.AgruparPedidos;

public class AgruparPedidosCommandHandler : IRequestHandler<AgruparPedidosCommand, AgruparResponseDto>
{
    private readonly IAgrupadorPedidosService _agrupador;

    public AgruparPedidosCommandHandler(IAgrupadorPedidosService agrupador)
    {
        _agrupador = agrupador;
    }

    public async Task<AgruparResponseDto> Handle(AgruparPedidosCommand request, CancellationToken cancellationToken)
    {
        var pedidos = request.Pedidos.Select(MontagemCargaProjection.MapPedido).ToList();
        var parametros = MontagemCargaProjection.MapParametros(request.Parametros);

        var resultado = await _agrupador.Agrupar(pedidos, parametros, cancellationToken);
        return MontagemCargaProjection.MapAgrupamentoResultado(resultado);
    }
}
