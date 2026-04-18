namespace MontagemCarga.Domain.ValueObjects;

public record PedidoNaoAgrupadoOutput(
    string Codigo,
    string Motivo);

public record ResultadoAgrupamentoOutput(
    IReadOnlyList<GrupoAgrupamentoOutput> Grupos,
    IReadOnlyList<PedidoNaoAgrupadoOutput> PedidosNaoAgrupados,
    IReadOnlyList<string> Avisos);
