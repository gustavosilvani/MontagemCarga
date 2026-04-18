using MediatR;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Queries.ListarCarregamentos;

public class ListarCarregamentosQueryHandler : IRequestHandler<ListarCarregamentosQuery, ListarCarregamentosResult>
{
    private readonly ICarregamentoRepository _repository;
    private readonly ITenantService _tenantService;

    public ListarCarregamentosQueryHandler(ICarregamentoRepository repository, ITenantService tenantService)
    {
        _repository = repository;
        _tenantService = tenantService;
    }

    public async Task<ListarCarregamentosResult> Handle(ListarCarregamentosQuery request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual();
        if (!embarcadorId.HasValue)
            return new ListarCarregamentosResult();

        var (items, total) = await _repository.ListarAsync(embarcadorId.Value, request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(CarregamentoResponseMapper.Map).ToList();
        return new ListarCarregamentosResult { Items = dtos, Total = total };
    }
}
