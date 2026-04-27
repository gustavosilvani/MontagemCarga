using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Exceptions;
using Xunit;

namespace MontagemCarga.Tests;

public class CarregamentoLifecycleEntityTests
{
    private static Carregamento Criar() => new(
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

    [Fact]
    public void IniciarTransito_FromMontado_DeveAvancar()
    {
        var c = Criar();
        c.IniciarTransito();
        Assert.Equal(SituacaoCarregamento.EmTransito, c.SituacaoCarregamento);
    }

    [Fact]
    public void Finalizar_FromMontado_DeveLancar()
    {
        var c = Criar();
        Assert.Throws<BusinessRuleException>(() => c.Finalizar());
    }

    [Fact]
    public void Finalizar_FromEmTransito_DeveAvancar()
    {
        var c = Criar();
        c.IniciarTransito();
        c.Finalizar();
        Assert.Equal(SituacaoCarregamento.Finalizado, c.SituacaoCarregamento);
    }

    [Fact]
    public void Cancelar_DePoisDeFinalizar_DeveLancar()
    {
        var c = Criar();
        c.IniciarTransito();
        c.Finalizar();
        Assert.Throws<BusinessRuleException>(() => c.Cancelar());
    }

    [Fact]
    public void Cancelar_FromMontado_DeveAlterarSituacao()
    {
        var c = Criar();
        c.Cancelar();
        Assert.Equal(SituacaoCarregamento.Cancelado, c.SituacaoCarregamento);
    }

    [Fact]
    public void IniciarTransito_FromEmTransito_DeveLancar()
    {
        var c = Criar();
        c.IniciarTransito();
        Assert.Throws<BusinessRuleException>(() => c.IniciarTransito());
    }

    [Fact]
    public void AdicionarPedido_DeveSomarPesoCubagemPaletes()
    {
        var c = Criar();
        c.AdicionarPedido("PED-1", 1, 100m, 2, 5m);
        c.AdicionarPedido("PED-2", 2, 50m, 1, 2m);

        Assert.Equal(150m, c.PesoCarregamento);
        Assert.Equal(7m, c.CubagemCarregamento);
        Assert.Equal(3, c.NumeroPaletesCarregamento);
        Assert.Equal(2, c.Pedidos.Count);
    }

    [Fact]
    public void AdicionarBloco_DeveAcumularBlocos()
    {
        var c = Criar();
        c.AdicionarBloco("PED-1", "B1", 1, 1, -23.5, -46.6, null, null, 1m, 5m);
        c.AdicionarBloco("PED-2", "B2", 2, 2, -23.6, -46.7, null, null, 2m, 8m);

        Assert.Equal(2, c.Blocos.Count);
    }

    [Fact]
    public void Construtor_DataCarregamentoLocal_DeveConverterParaUtc()
    {
        var local = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Local);
        var c = new Carregamento(
            Guid.NewGuid(),
            "X",
            TipoMontagemCarga.Manual,
            null,
            local,
            10m,
            null,
            null,
            Guid.NewGuid(),
            null);

        Assert.Equal(DateTimeKind.Utc, c.DataCarregamentoCarga.Kind);
    }
}
