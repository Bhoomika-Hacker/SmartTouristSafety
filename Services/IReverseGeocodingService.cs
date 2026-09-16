using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartTouristSafety.Services
{
    public interface IReverseGeocodingService
    {
        /// <summary>Live lookup of a short, human-readable place name for a coordinate (e.g. "Connaught Place, New Delhi").</summary>
        Task<string?> GetPlaceNameAsync(double lat, double lng, CancellationToken ct = default);
    }

    /// <summary>
    /// Turns a raw lat/lng into a real place name using OpenStreetMap's free, keyless Nominatim
    /// reverse-geocoding API — the same "live public API, no key required" pattern this project
    /// already uses for police-station lookups. This is what lets the tourist portal say
    /// "You're near Connaught Place, New Delhi" instead of just printing coordinates or a
    /// generic "unmarked area" message.
    ///
    /// Nominatim's usage policy caps free/shared usage at roughly one request per second and
    /// requires identifying the application via User-Agent (set below) — fine for a demo/small
    /// deployment, but for production traffic you should self-host Nominatim or switch to a
    /// paid geocoding API, exactly like the existing Overpass police-station lookup's own note.
    /// </summary>
    public class NominatimReverseGeocodingService : IReverseGeocodingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NominatimReverseGeocodingService> _logger;

        private const string Endpoint = "https://nominatim.openstreetmap.org/reverse";

        public NominatimReverseGeocodingService(IHttpClientFactory httpClientFactory, ILogger<NominatimReverseGeocodingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string?> GetPlaceNameAsync(double lat, double lng, CancellationToken ct = default)
        {
            try
            {
                var latStr = lat.ToString(CultureInfo.InvariantCulture);
                var lngStr = lng.ToString(CultureInfo.InvariantCulture);
                var url = $"{Endpoint}?format=jsonv2&lat={latStr}&lon={lngStr}&zoom=16&addressdetails=1";

                var client = _httpClientFactory.CreateClient("Nominatim");
                using var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim reverse geocode returned {Status} for ({Lat}, {Lng}).", response.StatusCode, lat, lng);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                var parsed = await JsonSerializer.DeserializeAsync<NominatimResponse>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, ct);

                return BuildShortName(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reverse geocoding failed for ({Lat}, {Lng}).", lat, lng);
                return null;
            }
        }

        /// <summary>Prefers a neighbourhood/suburb + city combo over Nominatim's long, cluttered full display_name.</summary>
        private static string? BuildShortName(NominatimResponse? parsed)
        {
            if (parsed is null) return null;

            var addr = parsed.Address;
            if (addr is not null)
            {
                var locality = addr.Suburb ?? addr.Neighbourhood ?? addr.Road ?? addr.Village ?? addr.Town;
                var city = addr.City ?? addr.Town ?? addr.County;

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(locality)) parts.Add(locality);
                if (!string.IsNullOrWhiteSpace(city) && city != locality) parts.Add(city!);
                if (parts.Count > 0) return string.Join(", ", parts);
            }

            return parsed.DisplayName;
        }

        private class NominatimResponse
        {
            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("address")]
            public NominatimAddress? Address { get; set; }
        }

        private class NominatimAddress
        {
            [JsonPropertyName("suburb")] public string? Suburb { get; set; }
            [JsonPropertyName("neighbourhood")] public string? Neighbourhood { get; set; }
            [JsonPropertyName("road")] public string? Road { get; set; }
            [JsonPropertyName("village")] public string? Village { get; set; }
            [JsonPropertyName("town")] public string? Town { get; set; }
            [JsonPropertyName("city")] public string? City { get; set; }
            [JsonPropertyName("county")] public string? County { get; set; }
        }
    }
}
