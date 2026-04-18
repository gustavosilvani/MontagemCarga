using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.ValueObjects;
using MontagemCarga.Infrastructure.Persistence;
using MontagemCarga.Infrastructure.Repositories;
using Xunit;

namespace MontagemCarga.Tests;

public class CarregamentoRepositoryTests
{
    [Fact]
    public async Task ListarAsync_DeveRetornarTotalRealDaConsultaFiltrandoPorTenant()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MontagemCargaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new MontagemCargaDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var tenantId = Guid.NewGuid();
        var outraTenantId = Guid.NewGuid();
        var filialId = Guid.NewGuid();

        context.Carregamentos.AddRange(
            new Carregamento(tenantId, "1", TipoMontagemCarga.Automatica, null, new DateTime(2026, 4, 1), 100m, null, null, filialId, null),
            new Carregamento(tenantId, "2", TipoMontagemCarga.Automatica, null, new DateTime(2026, 4, 1), 200m, null, null, filialId, null),
            new Carregamento(tenantId, "3", TipoMontagemCarga.Automatica, null, new DateTime(2026, 4, 1), 300m, null, null, filialId, null),
            new Carregamento(outraTenantId, "1", TipoMontagemCarga.Automatica, null, new DateTime(2026, 4, 1), 999m, null, null, filialId, null));

        await context.SaveChangesAsync();

        var repository = new CarregamentoRepository(context);
        var resultado = await repository.ListarAsync(tenantId, page: 1, pageSize: 2);

        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Items.Count);
        Assert.All(resultado.Items, item => Assert.Equal(tenantId, item.EmbarcadorId));
    }

    [Fact]
    public async Task CriarLoteAsync_DeveReservarNumerosSemDuplicidadeEPersistirPesoReal()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MontagemCargaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new MontagemCargaDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var tenantId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();
        var repository = new CarregamentoRepository(context);

        var primeiroLote = await repository.CriarLoteAsync(
            tenantId,
            null,
            new[]
            {
                BuildPlano(filialId, modeloId, "PED-1", 300m, ordem: 1, bloco: "1"),
                BuildPlano(filialId, modeloId, "PED-2", 450m, ordem: 1, bloco: "1")
            });

        var segundoLote = await repository.CriarLoteAsync(
            tenantId,
            null,
            new[]
            {
                new CarregamentoPlanejadoInput(
                    filialId,
                    modeloId,
                    new DateTime(2026, 4, 1),
                    900m,
                    null,
                    null,
                    new[]
                    {
                        new PedidoCarregamentoPlanejadoInput("PED-3", 500m, 1, 1, "1"),
                        new PedidoCarregamentoPlanejadoInput("PED-4", 400m, 2, 2, "2")
                    })
            });

        Assert.Equal(new[] { "1", "2" }, primeiroLote.Select(item => item.NumeroCarregamento).ToArray());
        Assert.Single(segundoLote);
        Assert.Equal("3", segundoLote[0].NumeroCarregamento);
        Assert.Equal(900m, segundoLote[0].PesoCarregamento);
        Assert.Equal(new[] { "1", "2" }, segundoLote[0].Blocos.OrderBy(item => item.OrdemCarregamento).Select(item => item.Bloco).ToArray());
    }

    private static CarregamentoPlanejadoInput BuildPlano(Guid filialId, Guid modeloId, string codigoPedido, decimal peso, int ordem, string bloco)
    {
        return new CarregamentoPlanejadoInput(
            filialId,
            modeloId,
            new DateTime(2026, 4, 1),
            peso,
            null,
            null,
            new[]
            {
                new PedidoCarregamentoPlanejadoInput(codigoPedido, peso, ordem, ordem, bloco)
            });
    }
}
