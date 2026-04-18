using MediatR;

namespace MontagemCarga.Application.Common;

/// <summary>
/// Marca um request como command com resposta tipada.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }
