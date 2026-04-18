namespace MontagemCarga.Domain.Exceptions;

public sealed class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
