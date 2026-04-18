using MediatR;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Queries.ObterCarregamento;

public class ObterCarregamentoQueryHandler : IRequestHandler<ObterCarregamentoQuery, CarregamentoResponseDto?>
{
    private readonly ICarregamentoRepository _repository;
    private readonly ITenantService _tenantService;

    public ObterCarregamentoQueryHandler(ICarregamentoRepository repository, ITenantService tenantService)
    {
        _repository = repository;
        _tenantService = tenantService;
    }

    public async Task<CarregamentoResponseDto?> Handle(ObterCarregamentoQuery request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual();
        if (!embarcadorId.HasValue)
            return null;

        var c = await _repository.ObterPorIdAsync(embarcadorId.Value, request.Id, cancellationToken);
        return c == null ? null : CarregamentoResponseMapper.Map(c);
    }
}
