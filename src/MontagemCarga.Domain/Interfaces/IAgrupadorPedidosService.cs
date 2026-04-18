using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Domain.Interfaces;

/// <summary>
/// Servico de agrupamento de pedidos em carregamentos (logica de ObterGruposPedidos).
/// </summary>
public interface IAgrupadorPedidosService
{
    /// <summary>
    /// Agrupa pedidos conforme parametros e retorna sugestoes de carregamento.
    /// </summary>
    Task<ResultadoAgrupamentoOutput> Agrupar(
        IReadOnlyList<PedidoAgrupamentoInput> pedidos,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken = default);
}
