using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class AtualizarSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid SessaoId { get; }
    public Guid? EmpresaId { get; }
    public List<PedidoParaMontagemDto> Pedidos { get; }
    public ParametrosMontagemDto Parametros { get; }

    public AtualizarSessaoMontagemCommand(Guid sessaoId, Guid? empresaId, List<PedidoParaMontagemDto> pedidos, ParametrosMontagemDto parametros)
    {
        SessaoId = sessaoId;
        EmpresaId = empresaId;
        Pedidos = pedidos ?? new List<PedidoParaMontagemDto>();
        Parametros = parametros ?? new ParametrosMontagemDto();
    }
}
