using System.Text.Json;

namespace Jellyfin.Plugin.TrailerScoop.Services;

internal static class JsonElementExt
{
    public static JsonElement? GetPropertyOrNull(this JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var prop))
            return prop;
        return null;
    }

    public static string? GetStringOrNull(this JsonElement? el, string name)
    {
        if (el.HasValue && el.Value.ValueKind == JsonValueKind.Object &&
            el.Value.TryGetProperty(name, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }
}
