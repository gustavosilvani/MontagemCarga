namespace MontagemCarga.Infrastructure.Services.Planning;

public sealed class OsrmOptions
{
    public const string SectionName = "LocationServices:Osrm";

    public string BaseUrl { get; set; } = string.Empty;
    public string Profile { get; set; } = "driving";
    public int TimeoutSeconds { get; set; } = 5;
}
