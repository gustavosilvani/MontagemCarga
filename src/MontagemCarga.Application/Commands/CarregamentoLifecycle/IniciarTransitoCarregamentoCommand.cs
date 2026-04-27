using MediatR;

namespace MontagemCarga.Application.Commands.CarregamentoLifecycle;

public record IniciarTransitoCarregamentoCommand(Guid CarregamentoId) : IRequest<Unit>;
