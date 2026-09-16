using System.Text.Json;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Services
{
    /// <summary>A safety-relevant report normalized from whatever shape the origin source uses.</summary>
    public record NormalizedAreaReport(
        double Latitude,
        double Longitude,
        IncidentSeverity Severity,
        string Description,
        DateTime OccurredAt,
        string SourceName,
        ReportSource Source,
        string ExternalReference,
        string? Url = null);

    /// <summary>
    /// Something that can be polled for real-world safety reports near any location —
    /// a news feed, a police open-data portal, a government disaster feed, etc. Add a
    /// new implementation and register it in Program.cs to plug in another real source
    /// without touching the risk-scoring logic at all.
    /// </summary>
    public interface IExternalReportSource
    {
        string SourceName { get; }
        bool Enabled { get; }
        Task<IReadOnlyList<NormalizedAreaReport>> FetchRecentReportsAsync(CancellationToken ct = default);
    }

    /// <summary>Options bound from appsettings.json ("ExternalReportSources:Gdelt").</summary>
    public class GdeltSourceOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Keyword/theme query. GDELT full-text-searches worldwide news coverage for this
        /// phrase and returns every place mentioned near it. Combine safety-relevant terms
        /// with OR. See https://blog.gdeltproject.org for query syntax.
        /// </summary>
        public string Query { get; set; } =
            "crime OR robbery OR assault OR kidnapping OR terrorist OR riot OR unrest OR \"tourist scam\" OR landslide OR flood";

        /// <summary>How far back GDELT should look ("15m".."7d" per the live API's own limits).</summary>
        public string Timespan { get; set; } = "1d";

        public int MaxPoints { get; set; } = 250;
    }

    /// <summary>
    /// Real, keyless integration with GDELT's public GEO 2.0 API
    /// (https://api.gdeltproject.org/api/v2/geo/geo) — the same "live public API, no key
    /// required" pattern this project already uses for police-station lookups. It returns
    /// GeoJSON points of everywhere GDELT's worldwide news monitoring has recently mentioned
    /// the configured safety keywords, which we then normalize into AreaReports.
    ///
    /// This captures the kind of event that makes the news (protests, disasters, terror,
    /// large-scale crime waves) — it is a genuinely live, real-world signal, but it is
    /// necessarily macro/news-driven rather than hyper-local. For granular street-level
    /// crime data, pair it with <see cref="GenericJsonReportSource"/> pointed at a real
    /// police/city open-data portal.
    ///
    /// NOTE: this integration is written against GDELT's documented, key-free GEO 2.0 API
    /// contract, but it was not exercised against the live endpoint in this environment
    /// (no outbound network access here) — verify the exact field names still match once
    /// deployed, and adjust the query/timespan to taste.
    /// </summary>
    public class GdeltNewsReportSource : IExternalReportSource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GdeltSourceOptions _options;
        private readonly ILogger<GdeltNewsReportSource> _logger;

        private const string Endpoint = "https://api.gdeltproject.org/api/v2/geo/geo";

        public GdeltNewsReportSource(IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<GdeltSourceOptions> options, ILogger<GdeltNewsReportSource> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public string SourceName => "GDELT News Feed";
        public bool Enabled => _options.Enabled;

        public async Task<IReadOnlyList<NormalizedAreaReport>> FetchRecentReportsAsync(CancellationToken ct = default)
        {
            var results = new List<NormalizedAreaReport>();
            if (!Enabled) return results;

            try
            {
                var url = $"{Endpoint}?query={Uri.EscapeDataString(_options.Query)}" +
                           $"&format=GeoJSON&mode=PointData" +
                           $"&timespan={Uri.EscapeDataString(_options.Timespan)}" +
                           $"&maxpoints={_options.MaxPoints}";

                var client = _httpClientFactory.CreateClient("Gdelt");
                using var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GDELT GEO API returned {Status} for query '{Query}'.", response.StatusCode, _options.Query);
                    return results;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                if (!doc.RootElement.TryGetProperty("features", out var features)) return results;

                foreach (var feature in features.EnumerateArray())
                {
                    var report = ParseFeature(feature);
                    if (report is not null) results.Add(report);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GDELT news source fetch failed; skipping this sync cycle.");
            }

            return results;
        }

        private NormalizedAreaReport? ParseFeature(JsonElement feature)
        {
            try
            {
                if (!feature.TryGetProperty("geometry", out var geometry)) return null;
                if (!geometry.TryGetProperty("coordinates", out var coords) || coords.GetArrayLength() < 2) return null;

                // GeoJSON order is [lng, lat]
                var lng = coords[0].GetDouble();
                var lat = coords[1].GetDouble();

                var props = feature.TryGetProperty("properties", out var p) ? p : default;
                var name = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("name", out var n)
                    ? n.GetString() ?? "Unnamed location"
                    : "Unnamed location";
                var count = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("count", out var c) && c.TryGetInt32(out var ci)
                    ? ci : 1;

                var severity = count switch
                {
                    >= 10 => IncidentSeverity.High,
                    >= 4 => IncidentSeverity.Medium,
                    _ => IncidentSeverity.Low
                };

                var externalRef = $"gdelt:{lat:F4},{lng:F4}:{DateTime.UtcNow:yyyyMMddHH}:{name}";

                return new NormalizedAreaReport(
                    Latitude: lat,
                    Longitude: lng,
                    Severity: severity,
                    Description: $"{count} recent news mention(s) near {name} matching safety-related keywords.",
                    OccurredAt: DateTime.UtcNow,
                    SourceName: SourceName,
                    Source: ReportSource.NewsFeed,
                    ExternalReference: externalRef);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Config for one generically-configured open-data JSON source (appsettings.json array entry).</summary>
    public class JsonReportSourceConfig
    {
        public bool Enabled { get; set; } = false;
        public string Name { get; set; } = string.Empty;

        /// <summary>Full URL of a JSON API that returns an array of incident/crime records — e.g. a
        /// Socrata-style city/police open-data endpoint (many real police open-data portals, such as
        /// Chicago's and NYC's, use exactly this format and need no API key for read access).</summary>
        public string Url { get; set; } = string.Empty;

        public string? ApiKeyHeaderName { get; set; }
        public string? ApiKeyHeaderValue { get; set; }

        /// <summary>Dotted path to the array of records within the JSON payload, empty if the payload root is the array.</summary>
        public string ArrayPath { get; set; } = string.Empty;

        public string LatitudeField { get; set; } = "latitude";
        public string LongitudeField { get; set; } = "longitude";
        public string DescriptionField { get; set; } = "description";
        public string? DateField { get; set; }
        public string? IdField { get; set; }
        public string DefaultSeverity { get; set; } = "Medium";
    }

    /// <summary>
    /// Generic adapter for the many real government/police open-data portals that already publish
    /// plain JSON (most run on Socrata or a similar platform, e.g. city crime-incident datasets).
    /// Point it at any such endpoint via appsettings.json — no code changes needed to plug in a new
    /// real jurisdiction's feed.
    /// </summary>
    public class GenericJsonReportSource : IExternalReportSource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonReportSourceConfig _config;
        private readonly ILogger<GenericJsonReportSource> _logger;

        public GenericJsonReportSource(IHttpClientFactory httpClientFactory, JsonReportSourceConfig config, ILogger<GenericJsonReportSource> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public string SourceName => string.IsNullOrWhiteSpace(_config.Name) ? "External Open Data" : _config.Name;
        public bool Enabled => _config.Enabled && !string.IsNullOrWhiteSpace(_config.Url);

        public async Task<IReadOnlyList<NormalizedAreaReport>> FetchRecentReportsAsync(CancellationToken ct = default)
        {
            var results = new List<NormalizedAreaReport>();
            if (!Enabled) return results;

            try
            {
                var client = _httpClientFactory.CreateClient("ExternalReports");
                using var request = new HttpRequestMessage(HttpMethod.Get, _config.Url);
                if (!string.IsNullOrWhiteSpace(_config.ApiKeyHeaderName))
                    request.Headers.TryAddWithoutValidation(_config.ApiKeyHeaderName, _config.ApiKeyHeaderValue);

                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("External report source '{Name}' returned {Status}.", SourceName, response.StatusCode);
                    return results;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var array = ResolveArray(doc.RootElement, _config.ArrayPath);
                if (array is null) return results;

                var defaultSeverity = Enum.TryParse<IncidentSeverity>(_config.DefaultSeverity, true, out var sev)
                    ? sev : IncidentSeverity.Medium;

                foreach (var item in array.Value.EnumerateArray())
                {
                    var report = ParseItem(item, defaultSeverity);
                    if (report is not null) results.Add(report);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External report source '{Name}' fetch failed; skipping this sync cycle.", SourceName);
            }

            return results;
        }

        private static JsonElement? ResolveArray(JsonElement root, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return root.ValueKind == JsonValueKind.Array ? root : null;

            var current = root;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next)) return null;
                current = next;
            }
            return current.ValueKind == JsonValueKind.Array ? current : null;
        }

        private NormalizedAreaReport? ParseItem(JsonElement item, IncidentSeverity defaultSeverity)
        {
            if (!TryGetDouble(item, _config.LatitudeField, out var lat)) return null;
            if (!TryGetDouble(item, _config.LongitudeField, out var lng)) return null;

            var description = TryGetString(item, _config.DescriptionField) ?? $"Reported incident ({SourceName}).";
            var occurredAt = _config.DateField is not null && TryGetString(item, _config.DateField) is { } dateStr
                && DateTime.TryParse(dateStr, out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;

            var id = _config.IdField is not null ? TryGetString(item, _config.IdField) : null;
            var externalRef = $"{SourceName}:{id ?? $"{lat:F5},{lng:F5}:{occurredAt:O}"}";

            return new NormalizedAreaReport(
                Latitude: lat,
                Longitude: lng,
                Severity: defaultSeverity,
                Description: description,
                OccurredAt: occurredAt,
                SourceName: SourceName,
                Source: ReportSource.PoliceOpenData,
                ExternalReference: externalRef);
        }

        private static bool TryGetDouble(JsonElement obj, string field, out double value)
        {
            value = 0;
            if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(field, out var el)) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.TryGetDouble(out value);
            if (el.ValueKind == JsonValueKind.String) return double.TryParse(el.GetString(), out value);
            return false;
        }

        private static string? TryGetString(JsonElement obj, string field)
        {
            if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(field, out var el)) return null;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        }
    }
}
