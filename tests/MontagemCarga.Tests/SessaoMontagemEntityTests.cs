using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using Xunit;

namespace MontagemCarga.Tests;

public class SessaoMontagemEntityTests
{
    private static SessaoMontagem CriarSessao() => new(
        Guid.NewGuid(),
        "op-1",
        "Operador 1",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "{}",
        "[]",
        "{}",
        "[]");

    [Fact]
    public void Construtor_DeveInicializarComoProcessada()
    {
        var sessao = CriarSessao();

        Assert.Equal(SituacaoSessaoMontagem.Processada, sessao.Situacao);
        Assert.NotNull(sessao.ProcessadaEmUtc);
        Assert.Null(sessao.PersistidaEmUtc);
        Assert.Null(sessao.CanceladaEmUtc);
        Assert.NotEqual(Guid.Empty, sessao.Id);
    }

    [Fact]
    public void AtualizarEstado_DeveSubstituirJsonsEReprocessar()
    {
        var sessao = CriarSessao();
        var novaFilial = Guid.NewGuid();

        sessao.AtualizarEstado(novaFilial, null, "{\"a\":1}", "[1]", "{\"r\":1}", "[\"N1\"]");

        Assert.Equal(novaFilial, sessao.FilialId);
        Assert.Null(sessao.EmpresaId);
        Assert.Equal("{\"a\":1}", sessao.ParametrosJson);
        Assert.Equal("[1]", sessao.PedidosJson);
        Assert.Equal("{\"r\":1}", sessao.ResultadoJson);
        Assert.Equal("[\"N1\"]", sessao.NumerosCarregamentoReservadosJson);
        Assert.Equal(SituacaoSessaoMontagem.Processada, sessao.Situacao);
    }

    [Fact]
    public void MarcarAtiva_DeveAlterarSituacao()
    {
        var sessao = CriarSessao();

        sessao.MarcarAtiva();

        Assert.Equal(SituacaoSessaoMontagem.Ativa, sessao.Situacao);
    }

    [Fact]
    public void MarcarPersistida_DevePopularJsonECarimboData()
    {
        var sessao = CriarSessao();

        sessao.MarcarPersistida("[{\"id\":1}]");

        Assert.Equal(SituacaoSessaoMontagem.Persistida, sessao.Situacao);
        Assert.Equal("[{\"id\":1}]", sessao.CarregamentosCriadosJson);
        Assert.NotNull(sessao.PersistidaEmUtc);
    }

    [Fact]
    public void Cancelar_DeveAlterarSituacao()
    {
        var sessao = CriarSessao();

        sessao.Cancelar();

        Assert.Equal(SituacaoSessaoMontagem.Cancelada, sessao.Situacao);
        Assert.NotNull(sessao.CanceladaEmUtc);
    }

    [Fact]
    public void AtualizarEstado_AposCancelar_DeveLancar()
    {
        var sessao = CriarSessao();
        sessao.Cancelar();

        Assert.Throws<InvalidOperationException>(() =>
            sessao.AtualizarEstado(Guid.NewGuid(), null, "{}", "[]", "{}", "[]"));
    }

    [Fact]
    public void AtualizarEstado_AposPersistir_DeveLancar()
    {
        var sessao = CriarSessao();
        sessao.MarcarPersistida("[]");

        Assert.Throws<InvalidOperationException>(() =>
            sessao.AtualizarEstado(Guid.NewGuid(), null, "{}", "[]", "{}", "[]"));
    }

    [Fact]
    public void MarcarAtiva_AposCancelar_DeveLancar()
    {
        var sessao = CriarSessao();
        sessao.Cancelar();

        Assert.Throws<InvalidOperationException>(() => sessao.MarcarAtiva());
    }

    [Fact]
    public void MarcarPersistida_AposCancelar_DeveLancar()
    {
        var sessao = CriarSessao();
        sessao.Cancelar();

        Assert.Throws<InvalidOperationException>(() => sessao.MarcarPersistida("[]"));
    }

    [Fact]
    public void Cancelar_AposPersistir_DeveLancar()
    {
        var sessao = CriarSessao();
        sessao.MarcarPersistida("[]");

        Assert.Throws<InvalidOperationException>(() => sessao.Cancelar());
    }
}
