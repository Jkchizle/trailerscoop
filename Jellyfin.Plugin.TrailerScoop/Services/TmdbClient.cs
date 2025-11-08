using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Web.HttpUtility;
using Jellyfin.Plugin.TrailerScoop.Services;

namespace Jellyfin.Plugin.TrailerScoop.Services;

internal sealed class TmdbClient
{
    private readonly HttpClient _http;
    private readonly ILogger _log;
    private readonly string? _apiKey;

    public TmdbClient(HttpClient http, ILogger log, string? apiKey)
    {
        _http = http;
        _log = log;
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey!.Trim();
    }

    public bool Enabled => _apiKey != null;

    public async Task<string?> TryGetYoutubeKeyAsync(string title, int? year, string lang, string region, CancellationToken ct)
    {
        if (!Enabled) return null;

        try
        {
            // 1) search movie
            var query = UrlEncode(title);
            var url = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&query={query}&include_adult=false&language={lang}";
            if (year is int y) url += $"&year={y}";

            var resp = await _http.GetStringAsync(url, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(resp);
            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var id = results[0].GetProperty("id").GetInt32();

            // 2) videos for that movie id
            var url2 = $"https://api.themoviedb.org/3/movie/{id}/videos?api_key={_apiKey}&language={lang}";
            var resp2 = await _http.GetStringAsync(url2, ct).ConfigureAwait(false);
            using var doc2 = JsonDocument.Parse(resp2);

            if (!doc2.RootElement.TryGetProperty("results", out var vids) || vids.GetArrayLength() == 0)
                return null;

            foreach (var v in vids.EnumerateArray())
            {
                // We prefer official “Trailer” on YouTube
                var site = v.GetPropertyOrNull("site")?.GetString() ?? "";
                var type = v.GetPropertyOrNull("type")?.GetString() ?? "";
                var key  = v.GetPropertyOrNull("key")?.GetString();
                var off  = v.GetPropertyOrNull("official")?.GetBoolean() ?? false;

                if (string.Equals(site, "YouTube", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(type, "Trailer", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(key))
                {
                    if (off) return key; // first official match
                    // remember any non-official key as fallback
                    key ??= key;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "TMDb trailer lookup failed for {Title}", title);
            return null;
        }
    }
}
