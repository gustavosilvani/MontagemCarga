using System.Text.Json;

namespace MontagemCarga.Application.Common;

internal static class SessaoMontagemJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T DeserializeOrDefault<T>(string? json, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(json))
            return defaultValue;

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
