using MediatR;
using Moq;
using MontagemCarga.Application.Commands.CarregamentoLifecycle;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using MontagemCarga.Domain.Interfaces;
using Xunit;

namespace MontagemCarga.Tests;

public class CarregamentoLifecycleHandlerTests
{
    private static Carregamento NovoCarregamento() => new(
        Guid.NewGuid(),
        "1",
        TipoMontagemCarga.Automatica,
        null,
        new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
        100m,
        null,
        null,
        Guid.NewGuid(),
        null);

    private static (Mock<ICarregamentoRepository> Repo, Mock<ITenantService> Tenant) Mocks(Guid? embarcadorId = null, Carregamento? carregamento = null)
    {
        var repo = new Mock<ICarregamentoRepository>();
        var tenant = new Mock<ITenantService>();
        tenant.Setup(t => t.ObterEmbarcadorIdAtual()).Returns(embarcadorId ?? Guid.NewGuid());
        repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(carregamento);
        return (repo, tenant);
    }

    [Fact]
    public async Task Cancelar_Handle_DeveCancelarECarregamentoAtualizado()
    {
        var c = NovoCarregamento();
        var (repo, tenant) = Mocks(carregamento: c);
        var handler = new CancelarCarregamentoCommandHandler(repo.Object, tenant.Object);

        var result = await handler.Handle(new CancelarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal(SituacaoCarregamento.Cancelado, c.SituacaoCarregamento);
        repo.Verify(r => r.AtualizarAsync(c, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_Handle_SemCarregamento_DeveLancarNotFound()
    {
        var (repo, tenant) = Mocks(carregamento: null);
        var handler = new CancelarCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_Handle_SemEmbarcador_DeveLancar()
    {
        var repo = new Mock<ICarregamentoRepository>();
        var tenant = new Mock<ITenantService>();
        tenant.Setup(t => t.ObterEmbarcadorIdAtual()).Returns((Guid?)null);
        var handler = new CancelarCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new CancelarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task IniciarTransito_Handle_DeveAlterarParaEmTransito()
    {
        var c = NovoCarregamento();
        var (repo, tenant) = Mocks(carregamento: c);
        var handler = new IniciarTransitoCarregamentoCommandHandler(repo.Object, tenant.Object);

        await handler.Handle(new IniciarTransitoCarregamentoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(SituacaoCarregamento.EmTransito, c.SituacaoCarregamento);
        repo.Verify(r => r.AtualizarAsync(c, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IniciarTransito_Handle_NotFound_DeveLancar()
    {
        var (repo, tenant) = Mocks(carregamento: null);
        var handler = new IniciarTransitoCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new IniciarTransitoCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task IniciarTransito_Handle_SemEmbarcador_DeveLancar()
    {
        var repo = new Mock<ICarregamentoRepository>();
        var tenant = new Mock<ITenantService>();
        tenant.Setup(t => t.ObterEmbarcadorIdAtual()).Returns((Guid?)null);
        var handler = new IniciarTransitoCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new IniciarTransitoCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Finalizar_Handle_FromEmTransito_DeveFinalizar()
    {
        var c = NovoCarregamento();
        c.IniciarTransito();
        var (repo, tenant) = Mocks(carregamento: c);
        var handler = new FinalizarCarregamentoCommandHandler(repo.Object, tenant.Object);

        await handler.Handle(new FinalizarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(SituacaoCarregamento.Finalizado, c.SituacaoCarregamento);
        repo.Verify(r => r.AtualizarAsync(c, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Finalizar_Handle_FromMontado_DeveLancarBusinessRule()
    {
        var c = NovoCarregamento();
        var (repo, tenant) = Mocks(carregamento: c);
        var handler = new FinalizarCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new FinalizarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Finalizar_Handle_NotFound_DeveLancar()
    {
        var (repo, tenant) = Mocks(carregamento: null);
        var handler = new FinalizarCarregamentoCommandHandler(repo.Object, tenant.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new FinalizarCarregamentoCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
