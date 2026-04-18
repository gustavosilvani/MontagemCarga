using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.Commands.SessoesMontagem;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Queries.SessoesMontagem;

public sealed class ObterSessaoMontagemQueryHandler : IRequestHandler<ObterSessaoMontagemQuery, SessaoMontagemResponseDto?>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public ObterSessaoMontagemQueryHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto?> Handle(ObterSessaoMontagemQuery request, CancellationToken cancellationToken)
    {
        var embarcadorId = _tenantService.ObterEmbarcadorIdAtual();
        if (!embarcadorId.HasValue)
            return null;

        var operadorId = _tenantService.ObterOperadorIdAtual();
        if (string.IsNullOrWhiteSpace(operadorId))
            throw new UnauthorizedAccessException("Contexto do operador nao encontrado.");

        var sessao = await _repository.ObterPorIdAsync(embarcadorId.Value, request.SessaoId, cancellationToken);
        if (sessao is null)
            return null;

        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);
        return _workflow.MapSessao(sessao);
    }
}
