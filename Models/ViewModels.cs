using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models.ViewModels
{
    /// <summary>Payload sent by the tourist mobile app / IoT tracker for a GPS ping.</summary>
    public class LocationPingDto
    {
        public int TouristId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>Result returned to the caller after processing a location ping.</summary>
    public class LocationPingResultDto
    {
        public bool IsInsideKnownZone { get; set; }
        public string? ZoneName { get; set; }
        public RiskLevel? ZoneRiskLevel { get; set; }
        public bool IsGeoFenceBreach { get; set; }
        public int AiRiskScore { get; set; }
        public string AiRiskCategory { get; set; } = "Normal";
        public bool IncidentAutoCreated { get; set; }
        public int? IncidentId { get; set; }

        // Real place name for the exact coordinate (live reverse-geocoding), so the UI can show
        // "You're near Connaught Place, New Delhi" instead of a generic message when there's no zone.
        public string? LocationName { get; set; }

        // Live risk computed automatically from real nearby reports (incidents + external
        // feeds) — always populated, even when the tourist is standing in an area no
        // administrator has ever manually marked as a zone.
        public RiskLevel DynamicRiskLevel { get; set; }
        public int DynamicRiskScore { get; set; }
        public int ContributingReportCount { get; set; }
        public double? NearestReportDistanceMeters { get; set; }

        // Nearest police station, resolved automatically from the tourist's coordinates.
        public string? NearestPoliceStationName { get; set; }
        public double? NearestPoliceStationDistanceMeters { get; set; }
        public string? NearestPoliceStationPhone { get; set; }
        public string? NearestPoliceStationAddress { get; set; }
        public double? NearestPoliceStationLatitude { get; set; }
        public double? NearestPoliceStationLongitude { get; set; }

        // Echoed back so the client can render a map without keeping its own copy
        // of the coordinates it just sent.
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class NearestPoliceStationDto
    {
        public int? PoliceStationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public double DistanceMeters { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ChatRequestDto
    {
        public int? TouristId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public List<string> QuickReplies { get; set; } = new();
    }

    /// <summary>View model for the tourist self-service safety portal.</summary>
    public class TouristPortalViewModel
    {
        public Tourist Tourist { get; set; } = null!;
        public List<GeoFenceZone> Zones { get; set; } = new();
        public List<Incident> MyIncidents { get; set; } = new();
        public DigitalIdVerificationResultDto? DigitalIdStatus { get; set; }
    }

    /// <summary>Payload for the tourist's SOS / panic button.</summary>
    public class PanicButtonDto
    {
        public int TouristId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Message { get; set; }
    }

    public class DigitalIdVerificationResultDto
    {
        public bool IsValid { get; set; }
        public bool IsChainIntact { get; set; }
        public bool IsExpired { get; set; }
        public bool IsRevoked { get; set; }
        public string CurrentHash { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalTourists { get; set; }
        public int ActiveTourists { get; set; }
        public int TotalZones { get; set; }
        public int RestrictedOrHighRiskZones { get; set; }
        public int OpenIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public List<Incident> RecentIncidents { get; set; } = new();
        public List<AlertLog> RecentAlerts { get; set; } = new();
        public List<LocationLog> RecentBreaches { get; set; } = new();

        // Automatic, real-world report ingestion feeding the dynamic risk engine.
        public int TotalExternalReports { get; set; }
        public DateTime? LastExternalReportIngestedAt { get; set; }
    }

    public class LoginViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Live, automatically-computed risk for an arbitrary coordinate — no zone required.</summary>
    public class RiskAtDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? LocationName { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public int Score { get; set; }
        public int ContributingReportCount { get; set; }
        public double RadiusMeters { get; set; }
        public double? NearestReportDistanceMeters { get; set; }

        // If this point also happens to fall inside an administrator-declared zone,
        // that's surfaced too — but it's never required for a risk status to appear.
        public bool IsInsideDeclaredZone { get; set; }
        public string? DeclaredZoneName { get; set; }
        public RiskLevel? DeclaredZoneRiskLevel { get; set; }
    }

    public class UpdateIncidentStatusDto
    {
        public int IncidentId { get; set; }
        public IncidentStatus Status { get; set; }
        public string? AssignedResponder { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}
