using FluentValidation;
using MontagemCarga.Application.Commands.SessoesMontagem;

namespace MontagemCarga.Application.Validators;

public class CriarSessaoMontagemCommandValidator : AbstractValidator<CriarSessaoMontagemCommand>
{
    public CriarSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEmpty()
            .WithMessage("FilialId e obrigatorio para criar a sessao.");

        RuleFor(x => x.Pedidos)
            .NotEmpty()
            .WithMessage("Informe ao menos um pedido para criar a sessao.");

        RuleFor(x => x.Parametros)
            .NotNull()
            .WithMessage("Parametros sao obrigatorios para criar a sessao.");
    }
}

public class AtualizarSessaoMontagemCommandValidator : AbstractValidator<AtualizarSessaoMontagemCommand>
{
    public AtualizarSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");

        RuleFor(x => x.Pedidos)
            .NotNull()
            .WithMessage("Pedidos sao obrigatorios para atualizar a sessao.");

        RuleFor(x => x.Parametros)
            .NotNull()
            .WithMessage("Parametros sao obrigatorios para atualizar a sessao.");
    }
}

public class AdicionarPedidosSessaoMontagemCommandValidator : AbstractValidator<AdicionarPedidosSessaoMontagemCommand>
{
    public AdicionarPedidosSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");

        RuleFor(x => x.Pedidos)
            .NotEmpty()
            .WithMessage("Informe ao menos um pedido para adicionar na sessao.");
    }
}

public class RemoverPedidoSessaoMontagemCommandValidator : AbstractValidator<RemoverPedidoSessaoMontagemCommand>
{
    public RemoverPedidoSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");

        RuleFor(x => x.CodigoPedido)
            .NotEmpty()
            .WithMessage("CodigoPedido e obrigatorio.");
    }
}

public class ReprocessarSessaoMontagemCommandValidator : AbstractValidator<ReprocessarSessaoMontagemCommand>
{
    public ReprocessarSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");
    }
}

public class PersistirSessaoMontagemCommandValidator : AbstractValidator<PersistirSessaoMontagemCommand>
{
    public PersistirSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");
    }
}

public class CancelarSessaoMontagemCommandValidator : AbstractValidator<CancelarSessaoMontagemCommand>
{
    public CancelarSessaoMontagemCommandValidator()
    {
        RuleFor(x => x.SessaoId)
            .NotEmpty()
            .WithMessage("SessaoId e obrigatorio.");
    }
}
