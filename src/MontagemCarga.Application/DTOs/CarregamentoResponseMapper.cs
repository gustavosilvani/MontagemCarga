using MontagemCarga.Domain.Entities;
using MontagemCarga.Application.Common;

namespace MontagemCarga.Application.DTOs;

internal static class CarregamentoResponseMapper
{
    public static CarregamentoResponseDto Map(Carregamento c)
    {
        var dto = new CarregamentoResponseDto
        {
            Id = c.Id,
            NumeroCarregamento = c.NumeroCarregamento,
            SituacaoCarregamento = c.SituacaoCarregamento,
            TipoMontagemCarga = c.TipoMontagemCarga,
            TipoMontagemCarregamentoVRP = c.TipoMontagemCarregamentoVRP,
            ModeloVeicularId = c.ModeloVeicularId,
            CentroCarregamentoId = c.CentroCarregamentoId,
            LatitudeCentro = c.LatitudeCentro,
            LongitudeCentro = c.LongitudeCentro,
            DataCarregamentoCarga = c.DataCarregamentoCarga,
            PesoCarregamento = c.PesoCarregamento,
            CubagemCarregamento = c.CubagemCarregamento,
            NumeroPaletesCarregamento = c.NumeroPaletesCarregamento,
            OcupacaoPesoPercentual = c.OcupacaoPesoPercentual,
            OcupacaoCubagemPercentual = c.OcupacaoCubagemPercentual,
            OcupacaoPaletesPercentual = c.OcupacaoPaletesPercentual,
            DistanciaEstimadaKm = c.DistanciaEstimadaKm,
            DuracaoEstimadaMin = c.DuracaoEstimadaMin,
            CustoSimulado = c.CustoSimulado,
            RouteGeometry = c.RouteGeometry,
            TipoDeCargaId = c.TipoDeCargaId,
            TipoOperacaoId = c.TipoOperacaoId,
            FilialId = c.FilialId,
            EmpresaId = c.EmpresaId,
            Pedidos = c.Pedidos
                .OrderBy(p => p.Ordem)
                .Select(p => new CarregamentoPedidoItemDto
                {
                    PedidoIdExterno = p.PedidoIdExterno,
                    Ordem = p.Ordem,
                    Peso = p.Peso,
                    Pallet = p.Pallet,
                    VolumeTotal = p.VolumeTotal
                })
                .ToList(),
            Blocos = c.Blocos
                .OrderBy(b => b.OrdemCarregamento)
                .Select(b => new BlocoCarregamentoItemDto
                {
                    PedidoIdExterno = b.PedidoIdExterno,
                    Bloco = b.Bloco,
                    OrdemCarregamento = b.OrdemCarregamento,
                    OrdemEntrega = b.OrdemEntrega
                })
                .ToList(),
            Paradas = c.Blocos
                .OrderBy(b => b.OrdemEntrega)
                .Select(b => new ParadaCarregamentoItemDto
                {
                    PedidoCodigo = b.PedidoIdExterno,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    OrdemEntrega = b.OrdemEntrega,
                    ChegadaEstimadaUtc = b.ChegadaEstimadaUtc,
                    SaidaEstimadaUtc = b.SaidaEstimadaUtc,
                    DistanciaDesdeAnteriorKm = b.DistanciaDesdeAnteriorKm,
                    DuracaoDesdeAnteriorMin = b.DuracaoDesdeAnteriorMin
                })
                .ToList()
        };

        dto.AlertasOperacionais = MontagemCargaProjection.BuildAlertasCarregamento(dto);
        dto.IndicadoresOperacionais = MontagemCargaProjection.BuildIndicadoresCarregamento(dto);
        return dto;
    }
}
