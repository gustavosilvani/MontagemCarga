using MediatR;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Application.Commands.SessoesMontagem;

internal static class SessaoMontagemCommandSupport
{
    public static Guid RequireEmbarcadorId(ITenantService tenantService)
    {
        var embarcadorId = tenantService.ObterEmbarcadorIdAtual();
        if (!embarcadorId.HasValue)
            throw new UnauthorizedAccessException("Contexto do embarcador nao encontrado.");

        return embarcadorId.Value;
    }

    public static string RequireOperadorId(ITenantService tenantService)
    {
        var operadorId = tenantService.ObterOperadorIdAtual();
        if (string.IsNullOrWhiteSpace(operadorId))
            throw new UnauthorizedAccessException("Contexto do operador nao encontrado.");

        return operadorId;
    }

    public static async Task<SessaoMontagem> ObterSessaoAsync(
        ISessaoMontagemRepository repository,
        Guid embarcadorId,
        Guid sessaoId,
        CancellationToken cancellationToken)
    {
        var sessao = await repository.ObterPorIdAsync(embarcadorId, sessaoId, cancellationToken);
        if (sessao is null)
            throw new BusinessRuleException("Sessao de montagem nao encontrada.");

        return sessao;
    }

    public static void EnsureOperadorPodeAcessar(SessaoMontagem sessao, string operadorId)
    {
        if (!string.Equals(sessao.OperadorId, operadorId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Sessao pertence a outro operador.");
    }

    public static void EnsureMutavel(SessaoMontagem sessao)
    {
        if (sessao.Situacao == SituacaoSessaoMontagem.Cancelada)
            throw new ConflictException("Sessao cancelada nao pode ser alterada.");

        if (sessao.Situacao == SituacaoSessaoMontagem.Persistida)
            throw new ConflictException("Sessao persistida nao pode ser alterada.");
    }

    public static void ApplyProcessamento(
        SessaoMontagem sessao,
        Guid filialId,
        Guid? empresaId,
        IReadOnlyList<PedidoParaMontagemDto> pedidos,
        ParametrosMontagemDto parametros,
        SessaoMontagemProcessamentoResult processamento)
    {
        sessao.AtualizarEstado(
            filialId,
            empresaId,
            SessaoMontagemJson.Serialize(parametros),
            SessaoMontagemJson.Serialize(pedidos),
            SessaoMontagemJson.Serialize(processamento.Agrupamento),
            SessaoMontagemJson.Serialize(processamento.NumerosCarregamentoReservados));
    }

    public static IReadOnlyList<PedidoParaMontagemDto> MergePedidos(
        IReadOnlyList<PedidoParaMontagemDto> atuais,
        IReadOnlyList<PedidoParaMontagemDto> novos)
    {
        var merged = atuais
            .Concat(novos)
            .GroupBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        return merged;
    }
}

public sealed class CriarSessaoMontagemCommandHandler : IRequestHandler<CriarSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public CriarSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(CriarSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var operadorNome = _tenantService.ObterNomeOperadorAtual() ?? operadorId;

        var processamento = await _workflow.ProcessarAsync(
            request.FilialId,
            request.EmpresaId,
            request.Pedidos,
            request.Parametros,
            cancellationToken);

        var sessao = new SessaoMontagem(
            embarcadorId,
            operadorId,
            operadorNome,
            request.FilialId,
            request.EmpresaId,
            SessaoMontagemJson.Serialize(request.Parametros),
            SessaoMontagemJson.Serialize(request.Pedidos),
            SessaoMontagemJson.Serialize(processamento.Agrupamento),
            SessaoMontagemJson.Serialize(processamento.NumerosCarregamentoReservados));

        await _repository.InserirAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class AtualizarSessaoMontagemCommandHandler : IRequestHandler<AtualizarSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public AtualizarSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(AtualizarSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);
        SessaoMontagemCommandSupport.EnsureMutavel(sessao);

        var processamento = await _workflow.ProcessarAsync(
            sessao.FilialId,
            request.EmpresaId ?? sessao.EmpresaId,
            request.Pedidos,
            request.Parametros,
            cancellationToken);

        SessaoMontagemCommandSupport.ApplyProcessamento(
            sessao,
            sessao.FilialId,
            request.EmpresaId ?? sessao.EmpresaId,
            request.Pedidos,
            request.Parametros,
            processamento);

        await _repository.AtualizarAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class AdicionarPedidosSessaoMontagemCommandHandler : IRequestHandler<AdicionarPedidosSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public AdicionarPedidosSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(AdicionarPedidosSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);
        SessaoMontagemCommandSupport.EnsureMutavel(sessao);

        var sessaoDto = _workflow.MapSessao(sessao);
        var pedidos = SessaoMontagemCommandSupport.MergePedidos(sessaoDto.Pedidos, request.Pedidos).ToList();
        var processamento = await _workflow.ProcessarAsync(
            sessao.FilialId,
            sessao.EmpresaId,
            pedidos,
            sessaoDto.Parametros,
            cancellationToken);

        SessaoMontagemCommandSupport.ApplyProcessamento(
            sessao,
            sessao.FilialId,
            sessao.EmpresaId,
            pedidos,
            sessaoDto.Parametros,
            processamento);

        await _repository.AtualizarAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class RemoverPedidoSessaoMontagemCommandHandler : IRequestHandler<RemoverPedidoSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public RemoverPedidoSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(RemoverPedidoSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);
        SessaoMontagemCommandSupport.EnsureMutavel(sessao);

        var sessaoDto = _workflow.MapSessao(sessao);
        var pedidos = sessaoDto.Pedidos
            .Where(p => !string.Equals(p.Codigo, request.CodigoPedido, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var processamento = await _workflow.ProcessarAsync(
            sessao.FilialId,
            sessao.EmpresaId,
            pedidos,
            sessaoDto.Parametros,
            cancellationToken);

        SessaoMontagemCommandSupport.ApplyProcessamento(
            sessao,
            sessao.FilialId,
            sessao.EmpresaId,
            pedidos,
            sessaoDto.Parametros,
            processamento);

        await _repository.AtualizarAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class ReprocessarSessaoMontagemCommandHandler : IRequestHandler<ReprocessarSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public ReprocessarSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(ReprocessarSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);
        SessaoMontagemCommandSupport.EnsureMutavel(sessao);

        var sessaoDto = _workflow.MapSessao(sessao);
        var processamento = await _workflow.ProcessarAsync(
            sessao.FilialId,
            sessao.EmpresaId,
            sessaoDto.Pedidos,
            sessaoDto.Parametros,
            cancellationToken);

        SessaoMontagemCommandSupport.ApplyProcessamento(
            sessao,
            sessao.FilialId,
            sessao.EmpresaId,
            sessaoDto.Pedidos,
            sessaoDto.Parametros,
            processamento);

        await _repository.AtualizarAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class PersistirSessaoMontagemCommandHandler : IRequestHandler<PersistirSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public PersistirSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(PersistirSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);

        if (sessao.Situacao == SituacaoSessaoMontagem.Cancelada)
            throw new ConflictException("Sessao cancelada nao pode ser persistida.");

        if (sessao.Situacao == SituacaoSessaoMontagem.Persistida)
            return _workflow.MapSessao(sessao);

        var carregamentos = await _workflow.PersistirAsync(embarcadorId, sessao, request.EmpresaId, cancellationToken);
        sessao.MarcarPersistida(SessaoMontagemJson.Serialize(carregamentos));

        await _repository.AtualizarAsync(sessao, cancellationToken);
        return _workflow.MapSessao(sessao);
    }
}

public sealed class CancelarSessaoMontagemCommandHandler : IRequestHandler<CancelarSessaoMontagemCommand, SessaoMontagemResponseDto>
{
    private readonly ISessaoMontagemRepository _repository;
    private readonly ITenantService _tenantService;
    private readonly ISessaoMontagemWorkflow _workflow;

    public CancelarSessaoMontagemCommandHandler(
        ISessaoMontagemRepository repository,
        ITenantService tenantService,
        ISessaoMontagemWorkflow workflow)
    {
        _repository = repository;
        _tenantService = tenantService;
        _workflow = workflow;
    }

    public async Task<SessaoMontagemResponseDto> Handle(CancelarSessaoMontagemCommand request, CancellationToken cancellationToken)
    {
        var embarcadorId = SessaoMontagemCommandSupport.RequireEmbarcadorId(_tenantService);
        var operadorId = SessaoMontagemCommandSupport.RequireOperadorId(_tenantService);
        var sessao = await SessaoMontagemCommandSupport.ObterSessaoAsync(_repository, embarcadorId, request.SessaoId, cancellationToken);
        SessaoMontagemCommandSupport.EnsureOperadorPodeAcessar(sessao, operadorId);

        if (sessao.Situacao == SituacaoSessaoMontagem.Persistida)
            throw new ConflictException("Sessao persistida nao pode ser cancelada.");

        if (sessao.Situacao != SituacaoSessaoMontagem.Cancelada)
        {
            sessao.Cancelar();
            await _repository.AtualizarAsync(sessao, cancellationToken);
        }

        return _workflow.MapSessao(sessao);
    }
}
