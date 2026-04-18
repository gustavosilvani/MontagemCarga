namespace MontagemCarga.Domain.Exceptions;

public sealed class BusinessRuleException : ApiException
{
    public BusinessRuleException(string message)
        : base(message, 422)
    {
    }
}
