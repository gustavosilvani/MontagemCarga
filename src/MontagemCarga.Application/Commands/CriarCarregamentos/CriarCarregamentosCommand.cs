using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.CriarCarregamentos;

/// <summary>
/// Command para criar carregamentos (agrupar + persistir).
/// </summary>
public class CriarCarregamentosCommand : ICommand<List<CarregamentoResponseDto>>
{
    public List<GrupoPedidoResponseDto>? Grupos { get; }
    public List<PedidoParaMontagemDto>? Pedidos { get; }
    public ParametrosMontagemDto? Parametros { get; }
    public Guid? FilialId { get; }
    public Guid? EmpresaId { get; }

    public CriarCarregamentosCommand(
        List<GrupoPedidoResponseDto>? grupos,
        List<PedidoParaMontagemDto>? pedidos,
        ParametrosMontagemDto? parametros,
        Guid? filialId,
        Guid? empresaId)
    {
        Grupos = grupos;
        Pedidos = pedidos;
        Parametros = parametros;
        FilialId = filialId;
        EmpresaId = empresaId;
    }
}
