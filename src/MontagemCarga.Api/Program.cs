using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using MontagemCarga.Api.HealthChecks;
using MontagemCarga.Api.Middleware;
using MontagemCarga.Application;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Infrastructure;
using MontagemCarga.Infrastructure.Services.Planning;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service.name", builder.Environment.ApplicationName)
        .WriteTo.Console();
});

var serviceName = builder.Environment.ApplicationName;
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"]
    ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurado.");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret nao configurado.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MontagemCarga.Api", Version = "v1" });

    var xmlDocPaths = new[]
    {
        Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"),
        Path.Combine(AppContext.BaseDirectory, $"{typeof(AgruparRequestDto).Assembly.GetName().Name}.xml")
    };

    foreach (var xmlDocPath in xmlDocPaths.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(xmlDocPath))
            c.IncludeXmlComments(xmlDocPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<OsrmOptions>(builder.Configuration.GetSection(OsrmOptions.SectionName));
builder.Services.AddHttpClient<IRoutingProvider, OsrmRoutingProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OsrmOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
builder.Services.AddHttpClient<OsrmHealthCheck>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OsrmOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
builder.Services.AddHealthChecks()
    .AddNpgSql(defaultConnection, name: "database", tags: new[] { "db", "ready" })
    .AddCheck<OsrmHealthCheck>("osrm", tags: new[] { "http", "ready" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: serviceName,
        serviceVersion: serviceVersion,
        serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "CoreCteApi",
        ValidAudience = jwtSettings["Audience"] ?? "CoreCteApp",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var configuredCorsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var allowedCorsOrigins = new[]
    {
        "http://localhost:4200",
        "http://localhost:4201",
        "http://localhost:4202",
        "http://localhost:7420",
        "http://84.247.182.141:7420",
        "https://84.247.182.141:7420"
    }
    .Concat(configuredCorsOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MontagemCarga.Api v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.UseOpenTelemetryPrometheusScrapingEndpoint("/metrics");
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();

public partial class Program
{
}
