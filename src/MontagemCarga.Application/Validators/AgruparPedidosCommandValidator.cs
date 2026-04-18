using FluentValidation;
using MontagemCarga.Application.Commands.AgruparPedidos;
using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Application.Validators;

public class AgruparPedidosCommandValidator : AbstractValidator<AgruparPedidosCommand>
{
    public AgruparPedidosCommandValidator()
    {
        RuleFor(x => x.Parametros)
            .NotNull()
            .WithMessage("Parametros sao obrigatorios.");

        RuleFor(x => x.Parametros!.CentroCarregamentoId)
            .NotEmpty()
            .When(x => x.Parametros != null)
            .WithMessage("CentroCarregamentoId e obrigatorio.");

        RuleFor(x => x.Parametros!.ModelosVeiculares)
            .NotEmpty()
            .When(x => x.Parametros != null)
            .WithMessage("E necessario pelo menos um modelo veicular nos parametros.");

        RuleFor(x => x.Parametros!)
            .Must(p => p.UtilizarDispFrotaCentroDescCliente || p.Disponibilidades.Count > 0)
            .When(x => x.Parametros != null)
            .WithMessage("Informe disponibilidades efetivas do centro ou habilite UtilizarDispFrotaCentroDescCliente.");

        RuleFor(x => x.Parametros!)
            .Must(p => p.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum ||
                       (p.LatitudeCentro.HasValue && p.LongitudeCentro.HasValue))
            .When(x => x.Parametros != null)
            .WithMessage("LatitudeCentro e LongitudeCentro sao obrigatorias para modos VRP.");

        RuleFor(x => x.Parametros!)
            .Must(p => p.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum ||
                       p.ConfiguracaoRoteirizacao != null)
            .When(x => x.Parametros != null)
            .WithMessage("Informe ConfiguracaoRoteirizacao para modos VRP.");

        RuleFor(x => x.Parametros!)
            .Must(p => p.TipoMontagemCarregamentoVRP != TipoMontagemCarregamentoVRP.SimuladorFrete ||
                       p.ConfiguracaoSimulacaoFrete != null)
            .When(x => x.Parametros != null)
            .WithMessage("Informe ConfiguracaoSimulacaoFrete para o modo SimuladorFrete.");

        RuleFor(x => x.Pedidos)
            .NotEmpty()
            .WithMessage("Informe ao menos um pedido para agrupar.");

        RuleFor(x => x.Pedidos)
            .Must(pedidos => pedidos.Select(p => p.Codigo?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == pedidos.Count)
            .WithMessage("Nao envie pedidos duplicados no agrupamento.");

        RuleForEach(x => x.Pedidos)
            .ChildRules(p =>
            {
                p.RuleFor(x => x.Codigo).NotEmpty().WithMessage("Codigo do pedido e obrigatorio.");
                p.RuleFor(x => x.FilialId).NotEmpty().WithMessage("FilialId e obrigatorio.");
                p.RuleFor(x => x.PesoSaldoRestante).GreaterThan(0).WithMessage("PesoSaldoRestante deve ser maior que zero.");
                p.RuleFor(x => x.CubagemTotal).GreaterThanOrEqualTo(0).When(x => x.CubagemTotal.HasValue).WithMessage("CubagemTotal nao pode ser negativa.");
                p.RuleFor(x => x.NumeroPaletes).GreaterThanOrEqualTo(0).When(x => x.NumeroPaletes.HasValue).WithMessage("NumeroPaletes nao pode ser negativo.");
                p.RuleFor(x => x.DataCarregamentoPedido).NotEmpty().WithMessage("DataCarregamentoPedido e obrigatoria.");
                p.RuleFor(x => x.TempoServicoMinutos).GreaterThan(0).When(x => x.TempoServicoMinutos.HasValue).WithMessage("TempoServicoMinutos deve ser maior que zero.");
                p.RuleFor(x => x)
                    .Must(x => !x.JanelaEntregaInicioUtc.HasValue || !x.JanelaEntregaFimUtc.HasValue || x.JanelaEntregaInicioUtc <= x.JanelaEntregaFimUtc)
                    .WithMessage("JanelaEntregaInicioUtc deve ser menor ou igual a JanelaEntregaFimUtc.");
                p.RuleFor(x => x)
                    .Must(x => x.CanalEntregaLimitePedidos is null || x.CanalEntregaLimitePedidos > 0)
                    .WithMessage("CanalEntregaLimitePedidos deve ser maior que zero quando informado.");
                p.RuleFor(x => x)
                    .Must(x => x.JanelaEntregaInicioUtc.HasValue == x.JanelaEntregaFimUtc.HasValue)
                    .WithMessage("Informe JanelaEntregaInicioUtc e JanelaEntregaFimUtc em conjunto.");
            });

        RuleForEach(x => x.Pedidos)
            .Must((command, pedido) =>
                command.Parametros?.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum ||
                (pedido.Latitude.HasValue && pedido.Longitude.HasValue))
            .When(x => x.Parametros != null)
            .WithMessage("Latitude e Longitude sao obrigatorias para pedidos em modos VRP.");

        RuleForEach(x => x.Pedidos)
            .Must((command, pedido) =>
                command.Parametros?.TipoMontagemCarregamentoVRP != TipoMontagemCarregamentoVRP.VrpTimeWindows ||
                (pedido.JanelaEntregaInicioUtc.HasValue && pedido.JanelaEntregaFimUtc.HasValue))
            .When(x => x.Parametros != null)
            .WithMessage("JanelaEntregaInicioUtc e JanelaEntregaFimUtc sao obrigatorias para VrpTimeWindows.");

        RuleForEach(x => x.Pedidos)
            .Must((command, pedido) =>
                !command.Parametros!.MontagemCarregamentoPedidoProduto || (pedido.Itens != null && pedido.Itens.Count > 0))
            .When(x => x.Parametros != null)
            .WithMessage("Pedidos precisam ter itens quando MontagemCarregamentoPedidoProduto estiver habilitado.");
    }
}
