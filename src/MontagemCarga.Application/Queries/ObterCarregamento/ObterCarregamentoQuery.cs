using MediatR;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Queries.ObterCarregamento;

public class ObterCarregamentoQuery : IRequest<CarregamentoResponseDto?>
{
    public Guid Id { get; }

    public ObterCarregamentoQuery(Guid id)
    {
        Id = id;
    }
}
