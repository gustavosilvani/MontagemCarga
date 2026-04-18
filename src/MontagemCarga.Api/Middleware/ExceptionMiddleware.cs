using System.Net;
using System.Text.Json;
using FluentValidation;
using MontagemCarga.Domain.Exceptions;

namespace MontagemCarga.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro não tratado no MontagemCarga.Api: {Message}", exception.Message);
            await WriteErrorAsync(context, exception);
        }
    }

    private Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ApiException apiException => apiException.StatusCode,
            ValidationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var body = exception switch
        {
            ValidationException validationException => new
            {
                success = false,
                message = "Erro de validação",
                errors = validationException.Errors.Select(x => x.ErrorMessage).ToArray()
            },
            ApiException apiException => new
            {
                success = false,
                message = apiException.Message,
                errors = new[] { apiException.Message }
            },
            UnauthorizedAccessException => new
            {
                success = false,
                message = "Acesso não autorizado",
                errors = new[] { "Acesso não autorizado" }
            },
            _ => new
            {
                success = false,
                message = "Ocorreu um erro interno no servidor",
                errors = new[]
                {
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "Ocorreu um erro interno no servidor. Consulte os logs."
                }
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
