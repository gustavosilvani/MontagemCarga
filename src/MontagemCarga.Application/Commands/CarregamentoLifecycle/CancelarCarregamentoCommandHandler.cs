using MediatR;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Commands.CarregamentoLifecycle;

public class CancelarCarregamentoCommandHandler
    : IRequestHandler<CancelarCarregamentoCommand, Unit>
{
    private readonly ICarregamentoRepository _repository;
    private readonly ITenantService _tenantService;

    public CancelarCarregamentoCommandHandler(
        ICarregamentoRepository repository,
        ITenantService tenantService)
    {
        _repository = repository;
        _tenantService = tenantService;
    }

    public async Task<Unit> Handle(CancelarCarregamentoCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual()
            ?? throw new BusinessRuleException("Embarcador nao identificado.");

        var carregamento = await _repository.ObterPorIdAsync(embarcadorId, request.CarregamentoId, cancellationToken)
            ?? throw new NotFoundException($"Carregamento '{request.CarregamentoId}' nao encontrado.");

        carregamento.Cancelar();
        await _repository.AtualizarAsync(carregamento, cancellationToken);

        return Unit.Value;
    }
}
