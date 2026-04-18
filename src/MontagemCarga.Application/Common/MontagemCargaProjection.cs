using System.Globalization;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Domain.Entities;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Application.Common;

internal static class MontagemCargaProjection
{
    public static PedidoAgrupamentoInput MapPedido(PedidoParaMontagemDto p)
    {
        return new PedidoAgrupamentoInput(
            p.Codigo,
            p.FilialId,
            p.TipoOperacaoId,
            p.TipoDeCargaId,
            p.RotaFreteId,
            p.PesoSaldoRestante,
            p.CubagemTotal,
            p.NumeroPaletes,
            p.DataCarregamentoPedido,
            p.DestinatarioCnpj,
            p.RemetenteCnpj,
            p.RecebedorCnpj,
            p.Latitude,
            p.Longitude,
            p.CanalEntregaPrioridade,
            p.CanalEntregaLimitePedidos,
            p.NaoUtilizarCapacidadeVeiculo,
            p.PrevisaoEntrega,
            p.JanelaEntregaInicioUtc,
            p.JanelaEntregaFimUtc,
            p.TempoServicoMinutos,
            p.PedidoBloqueado,
            p.LiberadoMontagemCarga,
            (p.Itens ?? new List<PedidoProdutoDto>())
                .Select(item => new PedidoItemAgrupamentoInput(item.Codigo, item.Peso, item.Quantidade, item.Saldo, item.Descricao))
                .ToList());
    }

    public static ParametrosAgrupamentoInput MapParametros(ParametrosMontagemDto parametros)
    {
        return new ParametrosAgrupamentoInput(
            parametros.DataPrevistaCarregamento,
            parametros.CentroCarregamentoId,
            parametros.LatitudeCentro,
            parametros.LongitudeCentro,
            parametros.TipoMontagemCarregamentoVRP,
            parametros.TipoOcupacaoMontagemCarregamentoVRP,
            parametros.QuantidadeMaximaEntregasRoteirizar,
            parametros.NivelQuebraProdutoRoteirizar,
            parametros.AgruparPedidosMesmoDestinatario,
            parametros.IgnorarRotaFrete,
            parametros.PermitirPedidoBloqueado,
            parametros.MontagemCarregamentoPedidoProduto,
            parametros.UtilizarDispFrotaCentroDescCliente,
            parametros.Disponibilidades
                .Select(d => new DisponibilidadeFrotaInput(d.ModeloVeicularId, d.Quantidade))
                .ToList(),
            parametros.ModelosVeiculares
                .Select(m => new ModeloVeicularInput(m.Id, m.Descricao, m.CapacidadePesoTransporte, m.ToleranciaPesoExtra, m.Cubagem, m.NumeroPaletes))
                .ToList(),
            parametros.ConfiguracaoRoteirizacao is null
                ? null
                : new ConfiguracaoRoteirizacaoInput(
                    parametros.ConfiguracaoRoteirizacao.VelocidadeMediaKmH,
                    parametros.ConfiguracaoRoteirizacao.TempoParadaPadraoMin,
                    parametros.ConfiguracaoRoteirizacao.ToleranciaJanelaMin),
            parametros.ConfiguracaoSimulacaoFrete is null
                ? null
                : new ConfiguracaoSimulacaoFreteInput(
                    parametros.ConfiguracaoSimulacaoFrete.CustoBase,
                    parametros.ConfiguracaoSimulacaoFrete.CustoPorKm,
                    parametros.ConfiguracaoSimulacaoFrete.CustoPorKg,
                    parametros.ConfiguracaoSimulacaoFrete.CustoPorMetroCubico,
                    parametros.ConfiguracaoSimulacaoFrete.CustoPorPallet));
    }

    public static AgruparResponseDto MapAgrupamentoResultado(ResultadoAgrupamentoOutput resultado, IReadOnlyList<string>? numerosReservados = null)
    {
        var response = new AgruparResponseDto
        {
            Grupos = resultado.Grupos.Select(MapGrupo).ToList(),
            PedidosNaoAgrupados = resultado.PedidosNaoAgrupados
                .Select(p => new PedidoNaoAgrupadoResponseDto
                {
                    Codigo = p.Codigo,
                    Motivo = p.Motivo
                })
                .ToList(),
            Aviso = resultado.Avisos.Count > 0 ? string.Join(" | ", resultado.Avisos) : null
        };

        response.AlertasOperacionais = BuildAlertasAgrupamento(response.Grupos, response.PedidosNaoAgrupados);
        response.InconsistenciasOperacionais = BuildInconsistenciasAgrupamento(response.Grupos, response.PedidosNaoAgrupados);
        response.Resumo = BuildResumo(response.Grupos, response.PedidosNaoAgrupados, numerosReservados);
        return response;
    }

    public static IReadOnlyList<CarregamentoPlanejadoInput> BuildPlanos(
        IReadOnlyList<GrupoPedidoResponseDto> grupos,
        IReadOnlyDictionary<string, PedidoParaMontagemDto> pedidosLookup,
        bool gerarUnicoBlocoPorRecebedor)
    {
        return grupos.Select(grupo => BuildPlano(grupo, pedidosLookup, gerarUnicoBlocoPorRecebedor)).ToList();
    }

    public static ResumoOperacionalDto BuildResumo(
        IReadOnlyList<GrupoPedidoResponseDto> grupos,
        IReadOnlyList<PedidoNaoAgrupadoResponseDto> rejeitados,
        IReadOnlyList<string>? numerosReservados = null)
    {
        return new ResumoOperacionalDto
        {
            TotalPedidosSelecionados = grupos.SelectMany(g => g.CodigosPedido).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalGrupos = grupos.Count,
            TotalPedidosNaoAgrupados = rejeitados.Count,
            TotalEntregas = grupos.Sum(g => g.QtdeEntregas),
            PesoTotal = grupos.Sum(g => g.PesoTotal),
            CubagemTotal = grupos.Sum(g => g.CubagemTotal),
            NumeroPaletesTotal = grupos.Sum(g => g.NumeroPaletesTotal),
            DistanciaTotalKm = grupos.Sum(g => g.DistanciaEstimadaKm),
            DuracaoTotalMin = groupsDuration(grupos),
            CustoTotal = grupos.Any(g => g.CustoSimulado.HasValue) ? grupos.Sum(g => g.CustoSimulado ?? 0m) : null,
            NumeroCarregamentoReservadoInicial = numerosReservados?.FirstOrDefault()
        };

        static decimal groupsDuration(IReadOnlyList<GrupoPedidoResponseDto> groups) => groups.Sum(g => g.DuracaoEstimadaMin);
    }

    public static List<AlertaOperacionalDto> BuildAlertasGrupo(GrupoPedidoResponseDto grupo)
    {
        var alertas = new List<AlertaOperacionalDto>();
        if (grupo.OcupacaoPesoPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-peso", Titulo = "Peso no limite", Descricao = "O grupo opera com ocupacao de peso acima de 90%.", Severidade = "warning" });
        if (grupo.OcupacaoCubagemPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-cubagem", Titulo = "Cubagem no limite", Descricao = "A cubagem esta acima de 90% da capacidade.", Severidade = "warning" });
        if (grupo.OcupacaoPaletesPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-paletes", Titulo = "Pallet no limite", Descricao = "Os pallets estao acima de 90% da capacidade.", Severidade = "warning" });
        if (grupo.CustoSimulado.HasValue)
            alertas.Add(new AlertaOperacionalDto { Codigo = "frete-simulado", Titulo = "Frete simulado", Descricao = "O grupo foi calculado com custo simulado de frete.", Severidade = "info" });
        if (grupo.QtdeEntregas > 1)
            alertas.Add(new AlertaOperacionalDto { Codigo = "multipla-entrega", Titulo = "Multiplas entregas", Descricao = $"O grupo contem {grupo.QtdeEntregas} entregas.", Severidade = "info" });
        return alertas;
    }

    public static List<AlertaOperacionalDto> BuildAlertasAgrupamento(
        IReadOnlyList<GrupoPedidoResponseDto> grupos,
        IReadOnlyList<PedidoNaoAgrupadoResponseDto> rejeitados)
    {
        var alertas = grupos
            .SelectMany(g => g.AlertasOperacionais)
            .GroupBy(alerta => $"{alerta.Codigo}|{alerta.Severidade}|{alerta.Titulo}|{alerta.Descricao}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (rejeitados.Count > 0)
        {
            alertas.Add(new AlertaOperacionalDto
            {
                Codigo = "pedidos-pendentes",
                Titulo = "Pedidos pendentes de tratamento",
                Descricao = $"{rejeitados.Count} pedido(s) ficaram fora do agrupamento e exigem decisao operacional.",
                Severidade = "warning"
            });
        }

        return alertas;
    }

    public static List<InconsistenciaOperacionalDto> BuildInconsistenciasAgrupamento(
        IReadOnlyList<GrupoPedidoResponseDto> grupos,
        IReadOnlyList<PedidoNaoAgrupadoResponseDto> rejeitados)
    {
        var inconsistencias = rejeitados
            .Select(pedido => new InconsistenciaOperacionalDto
            {
                Codigo = "pedido-nao-agrupado",
                Titulo = "Pedido fora do agrupamento",
                Descricao = pedido.Motivo,
                Severidade = "warning",
                Origem = "pedido",
                Referencia = pedido.Codigo
            })
            .ToList();

        foreach (var grupo in grupos)
        {
            var referenciaGrupo = string.Join(", ", grupo.CodigosPedido.Take(3));
            foreach (var alerta in grupo.AlertasOperacionais.Where(alerta => string.Equals(alerta.Severidade, "warning", StringComparison.OrdinalIgnoreCase)))
            {
                inconsistencias.Add(new InconsistenciaOperacionalDto
                {
                    Codigo = $"grupo-{alerta.Codigo}",
                    Titulo = alerta.Titulo,
                    Descricao = alerta.Descricao,
                    Severidade = alerta.Severidade,
                    Origem = "grupo",
                    Referencia = referenciaGrupo
                });
            }
        }

        return inconsistencias
            .GroupBy(item => $"{item.Codigo}|{item.Origem}|{item.Referencia}|{item.Descricao}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public static List<IndicadorOperacionalDto> BuildIndicadoresGrupo(GrupoPedidoResponseDto grupo)
    {
        return
        [
            new IndicadorOperacionalDto { Codigo = "peso", Titulo = "Peso", Valor = $"{grupo.PesoTotal.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))} kg", Destaque = $"{grupo.OcupacaoPesoPercentual.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" },
            new IndicadorOperacionalDto { Codigo = "cubagem", Titulo = "Cubagem", Valor = $"{grupo.CubagemTotal.ToString("N4", CultureInfo.GetCultureInfo("pt-BR"))} m3", Destaque = grupo.OcupacaoCubagemPercentual.HasValue ? $"{grupo.OcupacaoCubagemPercentual.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" : null },
            new IndicadorOperacionalDto { Codigo = "paletes", Titulo = "Paletes", Valor = grupo.NumeroPaletesTotal.ToString(CultureInfo.InvariantCulture), Destaque = grupo.OcupacaoPaletesPercentual.HasValue ? $"{grupo.OcupacaoPaletesPercentual.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" : null },
            new IndicadorOperacionalDto { Codigo = "rota", Titulo = "Rota", Valor = $"{grupo.DistanciaEstimadaKm.ToString("N1", CultureInfo.GetCultureInfo("pt-BR"))} km", Destaque = $"{grupo.DuracaoEstimadaMin.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"))} min" }
        ];
    }

    public static List<AlertaOperacionalDto> BuildAlertasCarregamento(CarregamentoResponseDto carregamento)
    {
        var alertas = new List<AlertaOperacionalDto>();
        if (carregamento.OcupacaoPesoPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-peso", Titulo = "Peso no limite", Descricao = "O carregamento opera com ocupacao de peso acima de 90%.", Severidade = "warning" });
        if (carregamento.OcupacaoCubagemPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-cubagem", Titulo = "Cubagem no limite", Descricao = "A cubagem esta acima de 90% da capacidade.", Severidade = "warning" });
        if (carregamento.OcupacaoPaletesPercentual >= 90m)
            alertas.Add(new AlertaOperacionalDto { Codigo = "alta-ocupacao-paletes", Titulo = "Pallet no limite", Descricao = "Os pallets estao acima de 90% da capacidade.", Severidade = "warning" });
        if (carregamento.CustoSimulado.HasValue)
            alertas.Add(new AlertaOperacionalDto { Codigo = "frete-simulado", Titulo = "Frete simulado", Descricao = "O carregamento preserva o valor de frete simulado da sessao.", Severidade = "info" });
        if (carregamento.Paradas.Count > 1)
            alertas.Add(new AlertaOperacionalDto { Codigo = "multipla-entrega", Titulo = "Multiplas entregas", Descricao = $"O carregamento contem {carregamento.Paradas.Count} paradas previstas.", Severidade = "info" });
        return alertas;
    }

    public static List<IndicadorOperacionalDto> BuildIndicadoresCarregamento(CarregamentoResponseDto carregamento)
    {
        return
        [
            new IndicadorOperacionalDto { Codigo = "peso", Titulo = "Peso", Valor = $"{carregamento.PesoCarregamento.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))} kg", Destaque = $"{carregamento.OcupacaoPesoPercentual.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" },
            new IndicadorOperacionalDto { Codigo = "cubagem", Titulo = "Cubagem", Valor = $"{carregamento.CubagemCarregamento.ToString("N4", CultureInfo.GetCultureInfo("pt-BR"))} m3", Destaque = carregamento.OcupacaoCubagemPercentual.HasValue ? $"{carregamento.OcupacaoCubagemPercentual.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" : null },
            new IndicadorOperacionalDto { Codigo = "paletes", Titulo = "Paletes", Valor = carregamento.NumeroPaletesCarregamento.ToString(CultureInfo.InvariantCulture), Destaque = carregamento.OcupacaoPaletesPercentual.HasValue ? $"{carregamento.OcupacaoPaletesPercentual.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%" : null },
            new IndicadorOperacionalDto { Codigo = "rota", Titulo = "Rota", Valor = $"{carregamento.DistanciaEstimadaKm.ToString("N1", CultureInfo.GetCultureInfo("pt-BR"))} km", Destaque = $"{carregamento.DuracaoEstimadaMin.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"))} min" }
        ];
    }

    private static GrupoPedidoResponseDto MapGrupo(GrupoAgrupamentoOutput g)
    {
        var grupo = new GrupoPedidoResponseDto
        {
            CodigosPedido = g.CodigosPedido.ToList(),
            ModeloVeicularSugeridoId = g.ModeloVeicularSugeridoId,
            CentroCarregamentoId = g.CentroCarregamentoId,
            LatitudeCentro = g.LatitudeCentro,
            LongitudeCentro = g.LongitudeCentro,
            CodigoFilial = g.CodigoFilial,
            DataCarregamento = g.DataCarregamento,
            PesoTotal = g.PesoTotal,
            PesoConsideradoCapacidade = g.PesoConsideradoCapacidade,
            CubagemTotal = g.CubagemTotal,
            NumeroPaletesTotal = g.NumeroPaletesTotal,
            OcupacaoPesoPercentual = g.OcupacaoPesoPercentual,
            OcupacaoCubagemPercentual = g.OcupacaoCubagemPercentual,
            OcupacaoPaletesPercentual = g.OcupacaoPaletesPercentual,
            QtdeEntregas = g.QtdeEntregas,
            TipoOperacaoId = g.TipoOperacaoId,
            TipoDeCargaId = g.TipoDeCargaId,
            TipoMontagemCarregamentoVRP = (int)g.TipoMontagemCarregamentoVRP,
            TipoOcupacaoMontagemCarregamentoVRP = (int)g.TipoOcupacaoMontagemCarregamentoVRP,
            DistanciaEstimadaKm = g.DistanciaEstimadaKm,
            DuracaoEstimadaMin = g.DuracaoEstimadaMin,
            CustoSimulado = g.CustoSimulado,
            RouteGeometry = g.RouteGeometry,
            Paradas = g.Paradas.Select(parada => new ParadaPedidoResponseDto
            {
                PedidoCodigo = parada.PedidoCodigo,
                Latitude = parada.Latitude,
                Longitude = parada.Longitude,
                OrdemEntrega = parada.OrdemEntrega,
                ChegadaEstimadaUtc = parada.ChegadaEstimadaUtc,
                SaidaEstimadaUtc = parada.SaidaEstimadaUtc,
                DistanciaDesdeAnteriorKm = parada.DistanciaDesdeAnteriorKm,
                DuracaoDesdeAnteriorMin = parada.DuracaoDesdeAnteriorMin
            }).ToList()
        };

        grupo.AlertasOperacionais = BuildAlertasGrupo(grupo);
        grupo.IndicadoresOperacionais = BuildIndicadoresGrupo(grupo);
        return grupo;
    }

    private static CarregamentoPlanejadoInput BuildPlano(
        GrupoPedidoResponseDto grupo,
        IReadOnlyDictionary<string, PedidoParaMontagemDto> pedidosLookup,
        bool gerarUnicoBlocoPorRecebedor)
    {
        var paradasOrdenadas = grupo.Paradas.OrderBy(p => p.OrdemEntrega).ToList();

        var pedidosPlanejados = paradasOrdenadas
            .Select((parada, index) =>
            {
                if (!pedidosLookup.TryGetValue(parada.PedidoCodigo, out var pedido))
                    throw new InvalidOperationException($"Pedido '{parada.PedidoCodigo}' nao encontrado para o carregamento.");

                return new PedidoCarregamentoPlanejadoInput(
                    parada.PedidoCodigo,
                    pedido.PesoSaldoRestante,
                    pedido.CubagemTotal,
                    pedido.NumeroPaletes,
                    paradasOrdenadas.Count - index,
                    parada.OrdemEntrega,
                    ResolveBloco(paradasOrdenadas, pedidosLookup, index, gerarUnicoBlocoPorRecebedor));
            })
            .ToList();

        return new CarregamentoPlanejadoInput(
            grupo.CodigoFilial,
            grupo.ModeloVeicularSugeridoId,
            grupo.CentroCarregamentoId,
            grupo.LatitudeCentro,
            grupo.LongitudeCentro,
            grupo.DataCarregamento,
            grupo.PesoTotal,
            grupo.CubagemTotal,
            grupo.NumeroPaletesTotal,
            grupo.OcupacaoPesoPercentual,
            grupo.OcupacaoCubagemPercentual,
            grupo.OcupacaoPaletesPercentual,
            grupo.DistanciaEstimadaKm,
            grupo.DuracaoEstimadaMin,
            grupo.CustoSimulado,
            grupo.RouteGeometry,
            (Domain.Enums.TipoMontagemCarregamentoVRP)grupo.TipoMontagemCarregamentoVRP,
            grupo.TipoDeCargaId,
            grupo.TipoOperacaoId,
            pedidosPlanejados,
            paradasOrdenadas.Select(parada => new ParadaCarregamentoPlanejadaInput(
                parada.PedidoCodigo,
                parada.Latitude,
                parada.Longitude,
                parada.OrdemEntrega,
                parada.ChegadaEstimadaUtc,
                parada.SaidaEstimadaUtc,
                parada.DistanciaDesdeAnteriorKm,
                parada.DuracaoDesdeAnteriorMin)).ToList());
    }

    private static string ResolveBloco(
        IReadOnlyList<ParadaPedidoResponseDto> paradasOrdenadas,
        IReadOnlyDictionary<string, PedidoParaMontagemDto> pedidosLookup,
        int indexAtual,
        bool gerarUnicoBlocoPorRecebedor)
    {
        var bloco = 1;
        for (var i = 1; i <= indexAtual; i++)
        {
            if (MudouCompatibilidadeLogistica(paradasOrdenadas[i - 1], paradasOrdenadas[i], pedidosLookup, gerarUnicoBlocoPorRecebedor))
                bloco++;
        }

        return $"B{bloco:D2}";
    }

    private static bool MudouCompatibilidadeLogistica(
        ParadaPedidoResponseDto anterior,
        ParadaPedidoResponseDto atual,
        IReadOnlyDictionary<string, PedidoParaMontagemDto> pedidosLookup,
        bool gerarUnicoBlocoPorRecebedor)
    {
        if (!pedidosLookup.TryGetValue(anterior.PedidoCodigo, out var pedidoAnterior) ||
            !pedidosLookup.TryGetValue(atual.PedidoCodigo, out var pedidoAtual))
            return true;

        if (!string.Equals(pedidoAnterior.RotaFreteId?.ToString(), pedidoAtual.RotaFreteId?.ToString(), StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.Equals(pedidoAnterior.DestinatarioCnpj, pedidoAtual.DestinatarioCnpj, StringComparison.OrdinalIgnoreCase))
            return true;
        if (gerarUnicoBlocoPorRecebedor &&
            !string.Equals(pedidoAnterior.RecebedorCnpj, pedidoAtual.RecebedorCnpj, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.Equals(pedidoAnterior.RemetenteCnpj, pedidoAtual.RemetenteCnpj, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
