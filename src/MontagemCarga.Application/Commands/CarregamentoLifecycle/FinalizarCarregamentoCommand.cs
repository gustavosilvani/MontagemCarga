using MediatR;

namespace MontagemCarga.Application.Commands.CarregamentoLifecycle;

public record FinalizarCarregamentoCommand(Guid CarregamentoId) : IRequest<Unit>;
