using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

public sealed class CriarSessaoMontagemCommand : ICommand<SessaoMontagemResponseDto>
{
    public Guid FilialId { get; }
    public Guid? EmpresaId { get; }
    public List<PedidoParaMontagemDto> Pedidos { get; }
    public ParametrosMontagemDto Parametros { get; }

    public CriarSessaoMontagemCommand(Guid filialId, Guid? empresaId, List<PedidoParaMontagemDto> pedidos, ParametrosMontagemDto parametros)
    {
        FilialId = filialId;
        EmpresaId = empresaId;
        Pedidos = pedidos ?? new List<PedidoParaMontagemDto>();
        Parametros = parametros ?? new ParametrosMontagemDto();
    }
}
