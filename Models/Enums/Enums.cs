namespace SmartTouristSafety.Models.Enums
{
    public enum RiskLevel { Safe = 0, Caution = 1, HighRisk = 2, Restricted = 3 }

    public enum IncidentType
    {
        GeoFenceBreach = 0, PanicButton = 1, MissingPerson = 2, Medical = 3,
        Harassment = 4, Accident = 5, AnomalyDetected = 6, Other = 7
    }

    public enum IncidentSeverity { Low = 0, Medium = 1, High = 2, Critical = 3 }

    public enum IncidentStatus { Reported = 0, Acknowledged = 1, ResponderDispatched = 2, Resolved = 3, Closed = 4 }

    public enum AlertType { GeoFenceBreach = 0, AnomalyDetected = 1, PanicButton = 2, InactivityTimeout = 3, DigitalIdExpiring = 4 }

    public enum UserRole { Admin = 0, Authority = 1, Operator = 2, Tourist = 3 }

    /// <summary>Where an automatically-ingested <see cref="Models.AreaReport"/> came from.</summary>
    public enum ReportSource { NewsFeed = 0, PoliceOpenData = 1, CommunityTip = 2, Other = 3 }
}
