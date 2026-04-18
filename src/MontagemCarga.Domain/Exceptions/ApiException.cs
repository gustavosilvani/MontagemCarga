namespace MontagemCarga.Domain.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
