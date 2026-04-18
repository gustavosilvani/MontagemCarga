using Microsoft.EntityFrameworkCore;
using MontagemCarga.Domain.Entities;

namespace MontagemCarga.Infrastructure.Persistence;

public class MontagemCargaDbContext : DbContext
{
    public MontagemCargaDbContext(DbContextOptions<MontagemCargaDbContext> options) : base(options)
    {
    }

    public DbSet<Carregamento> Carregamentos { get; set; } = null!;
    public DbSet<CarregamentoPedido> CarregamentoPedidos { get; set; } = null!;
    public DbSet<BlocoCarregamento> BlocoCarregamentos { get; set; } = null!;
    public DbSet<SequenciaCarregamento> SequenciasCarregamento { get; set; } = null!;
    public DbSet<SessaoMontagem> SessoesMontagem { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Carregamento>(e =>
        {
            e.ToTable("carregamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmbarcadorId).IsRequired();
            e.Property(x => x.NumeroCarregamento).HasMaxLength(50).IsRequired();
            e.Property(x => x.PesoCarregamento).HasPrecision(18, 4);
            e.Property(x => x.CubagemCarregamento).HasPrecision(18, 4);
            e.Property(x => x.OcupacaoPesoPercentual).HasPrecision(10, 2);
            e.Property(x => x.OcupacaoCubagemPercentual).HasPrecision(10, 2);
            e.Property(x => x.OcupacaoPaletesPercentual).HasPrecision(10, 2);
            e.Property(x => x.DistanciaEstimadaKm).HasPrecision(18, 4);
            e.Property(x => x.DuracaoEstimadaMin).HasPrecision(18, 2);
            e.Property(x => x.CustoSimulado).HasPrecision(18, 4);
            e.Property(x => x.RouteGeometry).HasColumnName("route_geometry");
            e.HasIndex(x => new { x.EmbarcadorId, x.FilialId, x.NumeroCarregamento }).IsUnique();
            e.HasIndex(x => new { x.EmbarcadorId, x.CreatedAt });
            e.HasMany(x => x.Pedidos)
                .WithOne(x => x.Carregamento)
                .HasForeignKey(x => x.CarregamentoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Blocos)
                .WithOne(x => x.Carregamento)
                .HasForeignKey(x => x.CarregamentoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CarregamentoPedido>(e =>
        {
            e.ToTable("carregamento_pedidos");
            e.HasKey(x => x.Id);
            e.Property(x => x.PedidoIdExterno).HasMaxLength(100).IsRequired();
            e.Property(x => x.Peso).HasPrecision(18, 4);
        });

        modelBuilder.Entity<BlocoCarregamento>(e =>
        {
            e.ToTable("bloco_carregamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.PedidoIdExterno).HasMaxLength(100).IsRequired();
            e.Property(x => x.Bloco).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanciaDesdeAnteriorKm).HasPrecision(18, 4);
            e.Property(x => x.DuracaoDesdeAnteriorMin).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SequenciaCarregamento>(e =>
        {
            e.ToTable("sequencia_carregamentos");
            e.HasKey(x => x.FilialId);
            e.Property(x => x.UltimoNumero).IsRequired();
        });

        modelBuilder.Entity<SessaoMontagem>(e =>
        {
            e.ToTable("sessao_montagem");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmbarcadorId).IsRequired();
            e.Property(x => x.OperadorId).HasMaxLength(200).IsRequired();
            e.Property(x => x.OperadorNome).HasMaxLength(300).IsRequired();
            e.Property(x => x.ParametrosJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.PedidosJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.ResultadoJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.NumerosCarregamentoReservadosJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.CarregamentosCriadosJson).HasColumnType("jsonb").IsRequired();
            e.HasIndex(x => new { x.EmbarcadorId, x.CreatedAt });
            e.HasIndex(x => new { x.EmbarcadorId, x.FilialId, x.Situacao });
        });
    }
}
