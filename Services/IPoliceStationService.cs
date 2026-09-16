using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Services
{
    public interface IPoliceStationService
    {
        Task<NearestPoliceStationDto?> FindNearestAsync(double lat, double lng);
    }

    /// <summary>
    /// Finds the closest real police station to a given coordinate, ANYWHERE in
    /// the world — not from a hardcoded list. It queries the free OpenStreetMap
    /// Overpass API live, at request time, for actual amenity=police records
    /// around the tourist's real coordinates, expanding the search radius until
    /// something is found. Only if the live lookup can't be reached at all (e.g.
    /// no internet) does it fall back to any stations an admin has manually
    /// added to the local database, purely as an offline safety net.
    /// </summary>
    public class PoliceStationService : IPoliceStationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeoFencingService _geoFencingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PoliceStationService> _logger;

        // Two public Overpass mirrors are tried in order, in case one is rate-limited or down.
        private static readonly string[] OverpassEndpoints =
        {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter"
        };

        // Expanding search radius (meters): start tight, widen if nothing is found nearby.
        private static readonly int[] SearchRadiiMeters = { 3000, 8000, 20000, 50000 };

        public PoliceStationService(
            ApplicationDbContext db,
            IGeoFencingService geoFencingService,
            IHttpClientFactory httpClientFactory,
            ILogger<PoliceStationService> logger)
        {
            _db = db;
            _geoFencingService = geoFencingService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<NearestPoliceStationDto?> FindNearestAsync(double lat, double lng)
        {
            foreach (var radius in SearchRadiiMeters)
            {
                var live = await TryLiveLookupAsync(lat, lng, radius);
                if (live is not null) return live;
            }

            _logger.LogWarning("Live police-station lookup returned nothing for ({Lat}, {Lng}); falling back to local list.", lat, lng);
            return FindNearestLocal(lat, lng);
        }

        private NearestPoliceStationDto? FindNearestLocal(double lat, double lng)
        {
            var stations = _db.PoliceStations.Where(p => p.IsActive).ToList();
            if (stations.Count == 0) return null;

            var nearest = stations
                .Select(s => new { Station = s, Distance = _geoFencingService.DistanceMeters(lat, lng, s.Latitude, s.Longitude) })
                .OrderBy(x => x.Distance)
                .First();

            return new NearestPoliceStationDto
            {
                PoliceStationId = nearest.Station.Id,
                Name = nearest.Station.Name,
                Address = nearest.Station.Address,
                Phone = nearest.Station.Phone,
                DistanceMeters = nearest.Distance,
                Latitude = nearest.Station.Latitude,
                Longitude = nearest.Station.Longitude
            };
        }

        private async Task<NearestPoliceStationDto?> TryLiveLookupAsync(double lat, double lng, int radiusMeters)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(CultureInfo.InvariantCulture);

            var query =
                $"[out:json][timeout:15];" +
                $"(" +
                $"node[\"amenity\"=\"police\"](around:{radiusMeters},{latStr},{lngStr});" +
                $"way[\"amenity\"=\"police\"](around:{radiusMeters},{latStr},{lngStr});" +
                $"relation[\"amenity\"=\"police\"](around:{radiusMeters},{latStr},{lngStr});" +
                $");" +
                $"out center tags;";

            foreach (var endpoint in OverpassEndpoints)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("Overpass");
                    using var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
                    using var response = await client.PostAsync(endpoint, content);
                    if (!response.IsSuccessStatusCode) continue;

                    var json = await response.Content.ReadAsStringAsync();
                    var parsed = JsonSerializer.Deserialize<OverpassResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed?.Elements is null || parsed.Elements.Count == 0) continue;

                    var nearest = parsed.Elements
                        .Select(ToDto)
                        .Where(dto => dto is not null)
                        .Select(dto => dto!)
                        .Select(dto => new { Dto = dto, Distance = _geoFencingService.DistanceMeters(lat, lng, dto.Lat, dto.Lng) })
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault();

                    if (nearest is null) continue;

                    return new NearestPoliceStationDto
                    {
                        Name = nearest.Dto.Name,
                        Address = nearest.Dto.Address,
                        Phone = nearest.Dto.Phone,
                        DistanceMeters = nearest.Distance,
                        Latitude = nearest.Dto.Lat,
                        Longitude = nearest.Dto.Lng
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Overpass lookup failed for endpoint {Endpoint} (radius {Radius}m).", endpoint, radiusMeters);
                }
            }

            return null;
        }

        private static ResolvedStation? ToDto(OverpassElement el)
        {
            var lat = el.Lat ?? el.Center?.Lat;
            var lng = el.Lon ?? el.Center?.Lon;
            if (lat is null || lng is null) return null;

            var tags = el.Tags ?? new Dictionary<string, string>();
            var name = tags.TryGetValue("name", out var n) && !string.IsNullOrWhiteSpace(n) ? n : "Police Station";
            var phone = tags.TryGetValue("phone", out var p) ? p
                : tags.TryGetValue("contact:phone", out var p2) ? p2 : null;

            var addressParts = new List<string>();
            if (tags.TryGetValue("addr:housenumber", out var hn)) addressParts.Add(hn);
            if (tags.TryGetValue("addr:street", out var st)) addressParts.Add(st);
            if (tags.TryGetValue("addr:city", out var city)) addressParts.Add(city);
            var address = addressParts.Count > 0 ? string.Join(", ", addressParts) : null;

            return new ResolvedStation(name, address, phone, lat.Value, lng.Value);
        }

        private record ResolvedStation(string Name, string? Address, string? Phone, double Lat, double Lng);

        private class OverpassResponse
        {
            [JsonPropertyName("elements")]
            public List<OverpassElement>? Elements { get; set; }
        }

        private class OverpassElement
        {
            [JsonPropertyName("lat")]
            public double? Lat { get; set; }

            [JsonPropertyName("lon")]
            public double? Lon { get; set; }

            [JsonPropertyName("center")]
            public OverpassCenter? Center { get; set; }

            [JsonPropertyName("tags")]
            public Dictionary<string, string>? Tags { get; set; }
        }

        private class OverpassCenter
        {
            [JsonPropertyName("lat")]
            public double Lat { get; set; }

            [JsonPropertyName("lon")]
            public double Lon { get; set; }
        }
    }
}
