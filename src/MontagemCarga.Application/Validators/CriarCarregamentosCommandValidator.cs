using FluentValidation;
using MontagemCarga.Application.Commands.CriarCarregamentos;
using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Application.Validators;

public class CriarCarregamentosCommandValidator : AbstractValidator<CriarCarregamentosCommand>
{
    public CriarCarregamentosCommandValidator()
    {
        RuleFor(x => x.Pedidos)
            .NotNull()
            .WithMessage("Pedidos sao obrigatorios para criar carregamentos com persistencia segura.");

        RuleFor(x => x.Pedidos)
            .NotEmpty()
            .When(x => x.Pedidos != null)
            .WithMessage("Informe ao menos um pedido para criar carregamentos.");

        RuleFor(x => x.Parametros)
            .NotNull()
            .WithMessage("Parametros sao obrigatorios para criar carregamentos com persistencia segura.");

        RuleFor(x => x.Grupos)
            .NotEmpty()
            .When(x => x.Grupos != null)
            .WithMessage("Grupos informados para criacao nao podem estar vazios.");

        RuleFor(x => x)
            .Must(x => x.Pedidos != null && x.Pedidos.Count > 0 && x.Parametros != null)
            .WithMessage("Criacao de carregamentos exige pedidos e parametros. Enviar apenas grupos nao e suportado nesta versao.");

        RuleFor(x => x.Parametros!)
            .Must(p => p == null || p.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.Nenhum ||
                       (p.LatitudeCentro.HasValue && p.LongitudeCentro.HasValue))
            .When(x => x.Parametros != null)
            .WithMessage("LatitudeCentro e LongitudeCentro sao obrigatorias para modos VRP.");
    }
}
