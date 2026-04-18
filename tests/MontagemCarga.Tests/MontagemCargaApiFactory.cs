using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MontagemCarga.Infrastructure.Persistence;
using Xunit;

namespace MontagemCarga.Tests;

public sealed class MontagemCargaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtSecret = "dev-secret-minimo-32-chars-para-jwt-funcionar";
    private const string JwtIssuer = "CoreCteApi";
    private const string JwtAudience = "CoreCteApp";

    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MontagemCargaDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateAuthenticatedClient(Guid? embarcadorId = null, Guid? tenantHeaderId = null, string? operadorId = null, string? operadorNome = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(embarcadorId, operadorId, operadorNome));

        if (tenantHeaderId.HasValue)
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantHeaderId.Value.ToString());

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=montagem_carga_tests;Username=test;Password=test",
                ["JwtSettings:Secret"] = JwtSecret,
                ["JwtSettings:Issuer"] = JwtIssuer,
                ["JwtSettings:Audience"] = JwtAudience
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<MontagemCargaDbContext>));
            services.RemoveAll(typeof(MontagemCargaDbContext));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<MontagemCargaDbContext>));

            services.AddSingleton(_connection);
            services.AddDbContext<MontagemCargaDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(serviceProvider.GetRequiredService<SqliteConnection>());
            });
        });
    }

    private static string CreateToken(Guid? embarcadorId, string? operadorId, string? operadorNome)
    {
        var operadorIdEfetivo = operadorId ?? Guid.NewGuid().ToString();
        var operadorNomeEfetivo = string.IsNullOrWhiteSpace(operadorNome) ? "Operador Teste" : operadorNome;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, operadorIdEfetivo),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, operadorIdEfetivo),
            new(ClaimTypes.Name, operadorNomeEfetivo)
        };

        if (embarcadorId.HasValue)
            claims.Add(new Claim("EmbarcadorId", embarcadorId.Value.ToString()));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();

        await base.DisposeAsync();
    }
}
