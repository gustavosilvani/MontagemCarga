using MediatR;
using MontagemCarga.Application.DTOs;

namespace MontagemCarga.Application.Queries.ListarCarregamentos;

public class ListarCarregamentosQuery : IRequest<ListarCarregamentosResult>
{
    public int Page { get; }
    public int PageSize { get; }

    public ListarCarregamentosQuery(int page = 1, int pageSize = 20)
    {
        Page = page;
        PageSize = pageSize;
    }
}

public class ListarCarregamentosResult
{
    public List<CarregamentoResponseDto> Items { get; set; } = new();
    public int Total { get; set; }
}
