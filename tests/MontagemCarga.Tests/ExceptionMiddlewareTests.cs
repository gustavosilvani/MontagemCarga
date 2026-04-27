using System.Net;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MontagemCarga.Api.Middleware;
using MontagemCarga.Domain.Exceptions;
using Xunit;

namespace MontagemCarga.Tests;

public class ExceptionMiddlewareTests
{
    private static (HttpContext Context, MemoryStream Body) NewContext()
    {
        var ctx = new DefaultHttpContext();
        var body = new MemoryStream();
        ctx.Response.Body = body;
        return (ctx, body);
    }

    private static ExceptionMiddleware Build(RequestDelegate next, bool development = false)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(development ? "Development" : "Production");
        return new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance, env.Object);
    }

    private static string ReadBody(MemoryStream s)
    {
        s.Position = 0;
        return Encoding.UTF8.GetString(s.ToArray());
    }

    [Fact]
    public async Task Invoke_SemErro_DeveChamarNext()
    {
        var (ctx, _) = NewContext();
        var called = false;
        var middleware = Build(_ => { called = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(ctx);

        Assert.True(called);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_BusinessRuleException_Deve422()
    {
        var (ctx, body) = NewContext();
        var middleware = Build(_ => throw new BusinessRuleException("regra X"));

        await middleware.InvokeAsync(ctx);

        Assert.Equal(422, ctx.Response.StatusCode);
        Assert.Contains("regra X", ReadBody(body));
    }

    [Fact]
    public async Task Invoke_NotFoundException_Deve404()
    {
        var (ctx, _) = NewContext();
        var middleware = Build(_ => throw new NotFoundException("nao achou"));

        await middleware.InvokeAsync(ctx);

        Assert.Equal(404, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ConflictException_Deve409()
    {
        var (ctx, _) = NewContext();
        var middleware = Build(_ => throw new ConflictException("conflito"));

        await middleware.InvokeAsync(ctx);

        Assert.Equal(409, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ValidationException_Deve400()
    {
        var (ctx, body) = NewContext();
        var failures = new[] { new ValidationFailure("Campo", "Erro de validacao do campo") };
        var middleware = Build(_ => throw new ValidationException(failures));

        await middleware.InvokeAsync(ctx);

        Assert.Equal((int)HttpStatusCode.BadRequest, ctx.Response.StatusCode);
        Assert.Contains("Erro de validacao", ReadBody(body));
    }

    [Fact]
    public async Task Invoke_Unauthorized_Deve401()
    {
        var (ctx, _) = NewContext();
        var middleware = Build(_ => throw new UnauthorizedAccessException());

        await middleware.InvokeAsync(ctx);

        Assert.Equal((int)HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ExcecaoGenerica_Deve500()
    {
        var (ctx, body) = NewContext();
        var middleware = Build(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(ctx);

        Assert.Equal((int)HttpStatusCode.InternalServerError, ctx.Response.StatusCode);
        Assert.Contains("erro interno", ReadBody(body), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoke_ExcecaoGenerica_EmDev_DeveExporMensagem()
    {
        var (ctx, body) = NewContext();
        var middleware = Build(_ => throw new InvalidOperationException("detalhe-debug"), development: true);

        await middleware.InvokeAsync(ctx);

        Assert.Equal((int)HttpStatusCode.InternalServerError, ctx.Response.StatusCode);
        var text = ReadBody(body);
        // Em dev expomos a mensagem original
        Assert.Contains("detalhe-debug", text);
    }
}
