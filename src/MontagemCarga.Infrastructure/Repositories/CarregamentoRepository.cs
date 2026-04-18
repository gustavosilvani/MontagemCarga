using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.Interfaces;
using MontagemCarga.Domain.ValueObjects;
using MontagemCarga.Infrastructure.Persistence;

namespace MontagemCarga.Infrastructure.Repositories;

public class CarregamentoRepository : ICarregamentoRepository
{
    private readonly MontagemCargaDbContext _db;

    public CarregamentoRepository(MontagemCargaDbContext db)
    {
        _db = db;
    }

    public async Task<Carregamento?> ObterPorIdAsync(Guid embarcadorId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Carregamentos
            .Include(c => c.Pedidos)
            .Include(c => c.Blocos)
            .FirstOrDefaultAsync(c => c.EmbarcadorId == embarcadorId && c.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Carregamento> Items, int Total)> ListarAsync(Guid embarcadorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Carregamentos
            .Include(c => c.Pedidos)
            .Include(c => c.Blocos)
            .Where(c => c.EmbarcadorId == embarcadorId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Carregamento> InserirAsync(Carregamento carregamento, CancellationToken cancellationToken = default)
    {
        _db.Carregamentos.Add(carregamento);
        await _db.SaveChangesAsync(cancellationToken);
        return carregamento;
    }

    public async Task<IReadOnlyList<string>> ReservarNumerosAsync(Guid filialId, int quantidade, CancellationToken cancellationToken = default)
    {
        if (quantidade <= 0)
            return Array.Empty<string>();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var numeros = await ReservarNumerosCarregamentoAsync(filialId, quantidade, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return numeros;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<Carregamento>> CriarLoteAsync(
        Guid embarcadorId,
        Guid? empresaId,
        IReadOnlyList<CarregamentoPlanejadoInput> carregamentos,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>>? numerosReservadosPorFilial = null,
        CancellationToken cancellationToken = default)
    {
        if (carregamentos.Count == 0)
            return Array.Empty<Carregamento>();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var numerosPorFilial = new Dictionary<Guid, Queue<string>>();
            foreach (var grupoFilial in carregamentos.GroupBy(c => c.FilialId))
            {
                IReadOnlyList<string> numeros;
                if (numerosReservadosPorFilial != null &&
                    numerosReservadosPorFilial.TryGetValue(grupoFilial.Key, out var reservados) &&
                    reservados.Count >= grupoFilial.Count())
                {
                    numeros = reservados.Take(grupoFilial.Count()).ToList();
                }
                else
                {
                    var faltantes = grupoFilial.Count();
                    var numerosDinamicos = await ReservarNumerosCarregamentoAsync(grupoFilial.Key, faltantes, transaction, cancellationToken);
                    numeros = numerosDinamicos;
                }

                numerosPorFilial[grupoFilial.Key] = new Queue<string>(numeros);
            }

            var entidades = new List<Carregamento>(carregamentos.Count);
            foreach (var planejado in carregamentos)
            {
                var numero = numerosPorFilial[planejado.FilialId].Dequeue();
                var carregamento = new Carregamento(
                    embarcadorId,
                    numero,
                    TipoMontagemCarga.Automatica,
                    planejado.TipoMontagemCarregamentoVRP,
                    planejado.ModeloVeicularId,
                    planejado.CentroCarregamentoId,
                    planejado.LatitudeCentro,
                    planejado.LongitudeCentro,
                    planejado.DataCarregamento,
                    planejado.PesoTotal,
                    planejado.CubagemTotal,
                    planejado.NumeroPaletesTotal,
                    planejado.OcupacaoPesoPercentual,
                    planejado.OcupacaoCubagemPercentual,
                    planejado.OcupacaoPaletesPercentual,
                    planejado.DistanciaEstimadaKm,
                    planejado.DuracaoEstimadaMin,
                    planejado.CustoSimulado,
                    planejado.RouteGeometry,
                    planejado.TipoDeCargaId,
                    planejado.TipoOperacaoId,
                    planejado.FilialId,
                    empresaId);

                foreach (var pedido in planejado.Pedidos.OrderBy(p => p.OrdemCarregamento))
                {
                    carregamento.AdicionarPedido(
                        pedido.CodigoPedido,
                        pedido.OrdemCarregamento,
                        pedido.Peso,
                        pedido.NumeroPaletes,
                        pedido.CubagemTotal);
                }

                var paradasPersistidas = false;
                foreach (var parada in planejado.Paradas.OrderBy(p => p.OrdemEntrega))
                {
                    var pedidoPlanejado = planejado.Pedidos.First(p => string.Equals(p.CodigoPedido, parada.CodigoPedido, StringComparison.OrdinalIgnoreCase));
                    carregamento.AdicionarBloco(
                        parada.CodigoPedido,
                        pedidoPlanejado.Bloco,
                        pedidoPlanejado.OrdemCarregamento,
                        parada.OrdemEntrega,
                        parada.Latitude,
                        parada.Longitude,
                        parada.ChegadaEstimadaUtc,
                        parada.SaidaEstimadaUtc,
                        parada.DistanciaDesdeAnteriorKm,
                        parada.DuracaoDesdeAnteriorMin);
                    paradasPersistidas = true;
                }

                if (!paradasPersistidas)
                {
                    foreach (var pedidoPlanejado in planejado.Pedidos.OrderBy(p => p.OrdemEntrega))
                    {
                        carregamento.AdicionarBloco(
                            pedidoPlanejado.CodigoPedido,
                            pedidoPlanejado.Bloco,
                            pedidoPlanejado.OrdemCarregamento,
                            pedidoPlanejado.OrdemEntrega,
                            planejado.LatitudeCentro ?? 0d,
                            planejado.LongitudeCentro ?? 0d,
                            null,
                            null,
                            0m,
                            0m);
                    }
                }

                entidades.Add(carregamento);
            }

            _db.Carregamentos.AddRange(entidades);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return entidades;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<IReadOnlyList<string>> ReservarNumerosCarregamentoAsync(
        Guid filialId,
        int quantidade,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            INSERT INTO sequencia_carregamentos ("FilialId", "UltimoNumero")
            VALUES (@filialId, @incremento)
            ON CONFLICT ("FilialId")
            DO UPDATE SET "UltimoNumero" = sequencia_carregamentos."UltimoNumero" + EXCLUDED."UltimoNumero"
            RETURNING "UltimoNumero";
            """;

        AddParameter(command, "@filialId", filialId);
        AddParameter(command, "@incremento", quantidade);

        var ultimoNumeroObject = await command.ExecuteScalarAsync(cancellationToken);
        var ultimoNumero = Convert.ToInt32(ultimoNumeroObject, CultureInfo.InvariantCulture);
        var primeiroNumero = ultimoNumero - quantidade + 1;

        return Enumerable.Range(primeiroNumero, quantidade)
            .Select(numero => numero.ToString(CultureInfo.InvariantCulture))
            .ToList();
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
