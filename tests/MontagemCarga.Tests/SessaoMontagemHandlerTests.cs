using Moq;
using MontagemCarga.Application.Commands.SessoesMontagem;
using MontagemCarga.Application.Common;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;
using Xunit;

namespace MontagemCarga.Tests;

public class SessaoMontagemHandlerTests
{
    private const string OperadorId = "op-1";
    private static readonly Guid EmbarcadorId = Guid.NewGuid();

    private static SessaoMontagem NovaSessao(string operadorId = OperadorId, Guid? embarcadorId = null) =>
        new(
            embarcadorId ?? EmbarcadorId,
            operadorId,
            "Operador 1",
            Guid.NewGuid(),
            null,
            "{}",
            "[]",
            "{}",
            "[]");

    private static (Mock<ISessaoMontagemRepository> Repo, Mock<ITenantService> Tenant, Mock<ISessaoMontagemWorkflow> Workflow) Mocks(
        SessaoMontagem? sessao = null,
        Guid? embarcador = null,
        string? operadorId = OperadorId)
    {
        var repo = new Mock<ISessaoMontagemRepository>();
        var tenant = new Mock<ITenantService>();
        var workflow = new Mock<ISessaoMontagemWorkflow>();

        tenant.Setup(t => t.ObterEmbarcadorIdAtual()).Returns(embarcador ?? EmbarcadorId);
        tenant.Setup(t => t.ObterOperadorIdAtual()).Returns(operadorId);
        tenant.Setup(t => t.ObterNomeOperadorAtual()).Returns("Operador 1");

        repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessao);

        workflow.Setup(w => w.ProcessarAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<PedidoParaMontagemDto>>(),
                It.IsAny<ParametrosMontagemDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessaoMontagemProcessamentoResult(new AgruparResponseDto(), Array.Empty<string>()));

        workflow.Setup(w => w.MapSessao(It.IsAny<SessaoMontagem>()))
            .Returns(new SessaoMontagemResponseDto
            {
                Pedidos = new List<PedidoParaMontagemDto>(),
                Parametros = new ParametrosMontagemDto(),
                Agrupamento = new AgruparResponseDto()
            });

        return (repo, tenant, workflow);
    }

    [Fact]
    public async Task Criar_Handle_DevePersistirEMaperResposta()
    {
        var (repo, tenant, workflow) = Mocks();
        var handler = new CriarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new CriarSessaoMontagemCommand(Guid.NewGuid(), null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        var resp = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(resp);
        repo.Verify(r => r.InserirAsync(It.IsAny<SessaoMontagem>(), It.IsAny<CancellationToken>()), Times.Once);
        workflow.Verify(w => w.MapSessao(It.IsAny<SessaoMontagem>()), Times.Once);
    }

    [Fact]
    public async Task Criar_Handle_SemEmbarcador_DeveLancarUnauthorized()
    {
        var (repo, tenant, workflow) = Mocks();
        tenant.Setup(t => t.ObterEmbarcadorIdAtual()).Returns((Guid?)null);
        var handler = new CriarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new CriarSessaoMontagemCommand(Guid.NewGuid(), null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Criar_Handle_SemOperador_DeveLancar()
    {
        var (repo, tenant, workflow) = Mocks();
        tenant.Setup(t => t.ObterOperadorIdAtual()).Returns((string?)null);
        var handler = new CriarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new CriarSessaoMontagemCommand(Guid.NewGuid(), null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_Handle_DeveAplicarProcessamento()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new AtualizarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AtualizarSessaoMontagemCommand(sessao.Id, null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        var resp = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(resp);
        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Atualizar_Handle_SessaoNaoEncontrada_DeveLancar()
    {
        var (repo, tenant, workflow) = Mocks(sessao: null);
        var handler = new AtualizarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AtualizarSessaoMontagemCommand(Guid.NewGuid(), null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_Handle_OutroOperador_DeveLancarUnauthorized()
    {
        var sessao = NovaSessao(operadorId: "outro-op");
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new AtualizarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AtualizarSessaoMontagemCommand(sessao.Id, null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_Handle_SessaoCancelada_DeveLancarConflict()
    {
        var sessao = NovaSessao();
        sessao.Cancelar();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new AtualizarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AtualizarSessaoMontagemCommand(sessao.Id, null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_Handle_SessaoPersistida_DeveLancarConflict()
    {
        var sessao = NovaSessao();
        sessao.MarcarPersistida("[]");
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new AtualizarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AtualizarSessaoMontagemCommand(sessao.Id, null, new List<PedidoParaMontagemDto>(), new ParametrosMontagemDto());

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task AdicionarPedidos_Handle_DeveProcessarEAtualizar()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new AdicionarPedidosSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new AdicionarPedidosSessaoMontagemCommand(sessao.Id, new List<PedidoParaMontagemDto>());

        await handler.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoverPedido_Handle_DeveProcessarEAtualizar()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new RemoverPedidoSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new RemoverPedidoSessaoMontagemCommand(sessao.Id, "PED-1");

        await handler.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reprocessar_Handle_DeveProcessarEAtualizar()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new ReprocessarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);
        var cmd = new ReprocessarSessaoMontagemCommand(sessao.Id);

        await handler.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_Handle_DeveCancelarSessao()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new CancelarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await handler.Handle(new CancelarSessaoMontagemCommand(sessao.Id), CancellationToken.None);

        Assert.Equal(SituacaoSessaoMontagem.Cancelada, sessao.Situacao);
        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_Handle_SessaoJaCancelada_NaoDeveAtualizar()
    {
        var sessao = NovaSessao();
        sessao.Cancelar();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new CancelarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await handler.Handle(new CancelarSessaoMontagemCommand(sessao.Id), CancellationToken.None);

        repo.Verify(r => r.AtualizarAsync(It.IsAny<SessaoMontagem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_Handle_SessaoPersistida_DeveLancarConflict()
    {
        var sessao = NovaSessao();
        sessao.MarcarPersistida("[]");
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new CancelarSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CancelarSessaoMontagemCommand(sessao.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Persistir_Handle_DevePersistirEMarcar()
    {
        var sessao = NovaSessao();
        var (repo, tenant, workflow) = Mocks(sessao);
        workflow.Setup(w => w.PersistirAsync(It.IsAny<Guid>(), sessao, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CarregamentoResponseDto>());
        var handler = new PersistirSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await handler.Handle(new PersistirSessaoMontagemCommand(sessao.Id, null), CancellationToken.None);

        Assert.Equal(SituacaoSessaoMontagem.Persistida, sessao.Situacao);
        repo.Verify(r => r.AtualizarAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Persistir_Handle_SessaoJaPersistida_DeveRetornarSemPersistir()
    {
        var sessao = NovaSessao();
        sessao.MarcarPersistida("[]");
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new PersistirSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await handler.Handle(new PersistirSessaoMontagemCommand(sessao.Id, null), CancellationToken.None);

        workflow.Verify(w => w.PersistirAsync(It.IsAny<Guid>(), It.IsAny<SessaoMontagem>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Persistir_Handle_SessaoCancelada_DeveLancarConflict()
    {
        var sessao = NovaSessao();
        sessao.Cancelar();
        var (repo, tenant, workflow) = Mocks(sessao);
        var handler = new PersistirSessaoMontagemCommandHandler(repo.Object, tenant.Object, workflow.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new PersistirSessaoMontagemCommand(sessao.Id, null), CancellationToken.None));
    }
}
