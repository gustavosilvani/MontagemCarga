using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MontagemCarga.Domain.Interfaces;
using MontagemCarga.Infrastructure.Persistence;
using MontagemCarga.Infrastructure.Repositories;
using MontagemCarga.Infrastructure.Services;
using MontagemCarga.Infrastructure.Services.Planning;
using MontagemCarga.Infrastructure.Services.Security;

namespace MontagemCarga.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurado.");
        services.AddDbContext<MontagemCargaDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ICarregamentoRepository, CarregamentoRepository>();
        services.AddScoped<ISessaoMontagemRepository, SessaoMontagemRepository>();
        services.AddScoped<IAgrupadorPedidosService, AgrupadorPedidosService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddHttpContextAccessor();
        return services;
    }
}
