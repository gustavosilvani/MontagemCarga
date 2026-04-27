using MediatR;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Commands.CarregamentoLifecycle;

public class FinalizarCarregamentoCommandHandler
    : IRequestHandler<FinalizarCarregamentoCommand, Unit>
{
    private readonly ICarregamentoRepository _repository;
    private readonly ITenantService _tenantService;

    public FinalizarCarregamentoCommandHandler(
        ICarregamentoRepository repository,
        ITenantService tenantService)
    {
        _repository = repository;
        _tenantService = tenantService;
    }

    public async Task<Unit> Handle(FinalizarCarregamentoCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual()
            ?? throw new BusinessRuleException("Embarcador nao identificado.");

        var carregamento = await _repository.ObterPorIdAsync(embarcadorId, request.CarregamentoId, cancellationToken)
            ?? throw new NotFoundException($"Carregamento '{request.CarregamentoId}' nao encontrado.");

        carregamento.Finalizar();
        await _repository.AtualizarAsync(carregamento, cancellationToken);

        return Unit.Value;
    }
}
