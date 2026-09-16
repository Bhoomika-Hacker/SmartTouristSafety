using SmartTouristSafety.Data;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Services
{
    public interface IGeoFencingService
    {
        (GeoFenceZone? zone, double distanceMeters) FindContainingZone(double lat, double lng);
        double DistanceMeters(double lat1, double lng1, double lat2, double lng2);
    }

    /// <summary>
    /// Determines which registered geo-fence zone (if any) a given lat/lng
    /// coordinate falls inside, using the Haversine great-circle distance
    /// formula against each zone's center point and radius.
    /// </summary>
    public class GeoFencingService : IGeoFencingService
    {
        private readonly ApplicationDbContext _db;
        private const double EarthRadiusMeters = 6371000;

        public GeoFencingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public (GeoFenceZone? zone, double distanceMeters) FindContainingZone(double lat, double lng)
        {
            GeoFenceZone? best = null;
            var bestDistance = double.MaxValue;

            foreach (var zone in _db.GeoFenceZones.Where(z => z.IsActive))
            {
                var distance = DistanceMeters(lat, lng, zone.Latitude, zone.Longitude);
                if (distance <= zone.RadiusMeters && distance < bestDistance)
                {
                    best = zone;
                    bestDistance = distance;
                }
            }

            return (best, best is null ? -1 : bestDistance);
        }

        public double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            double ToRad(double deg) => deg * Math.PI / 180.0;

            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMeters * c;
        }
    }
}
