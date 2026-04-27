using MediatR;

namespace MontagemCarga.Application.Commands.CarregamentoLifecycle;

public record CancelarCarregamentoCommand(Guid CarregamentoId) : IRequest<Unit>;
