using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Services
{
    public interface IChatbotService
    {
        Task<ChatResponseDto> GetResponseAsync(ChatRequestDto request);
    }

    /// <summary>
    /// A lightweight, explainable keyword-matching safety assistant. Rather than a
    /// single fixed sentence per topic, each branch pulls the tourist's actual
    /// current data (last known zone/risk score, real Digital ID status, live
    /// nearest police station) so answers reflect their real situation, not a
    /// canned script. Branches are ordered from most-specific phrase to most-generic
    /// keyword, because a later, more specific branch is unreachable if an earlier,
    /// broader branch already matches the same substring (e.g. "risk").
    /// </summary>
    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPoliceStationService _policeStationService;

        public ChatbotService(ApplicationDbContext db, IPoliceStationService policeStationService)
        {
            _db = db;
            _policeStationService = policeStationService;
        }

        public async Task<ChatResponseDto> GetResponseAsync(ChatRequestDto request)
        {
            var text = (request.Message ?? string.Empty).Trim().ToLowerInvariant();
            var tourist = request.TouristId.HasValue ? _db.Tourists.Find(request.TouristId.Value) : null;

            // 1. Emergency phrases always take priority over everything else.
            if (Contains(text, "sos", "emergency", "help me", "in danger", "im in danger", "i'm in danger", "attacked", "kidnap"))
            {
                return Reply(
                    "If you're in immediate danger, tap the red SOS button on your dashboard right now — " +
                    "it sends your live location straight to the tourist police. You can also call the " +
                    "nearest police station directly.",
                    "Find nearest police station", "Report a non-emergency incident");
            }

            // 2. Definition/explanation questions — checked BEFORE the generic status
            //    branch below, because both can contain the word "risk".
            if (Contains(text, "what do the risk levels mean", "what does caution mean", "what does restricted mean",
                              "what does high-risk mean", "what does high risk mean", "meaning of", "difference between",
                              "risk level mean", "levels mean"))
            {
                return Reply(
                    "Safe = normal tourist areas, no known concerns. Caution = limited lighting/patrols or " +
                    "occasional incidents — stay alert, especially at night. High-Risk = a pattern of incidents " +
                    "reported there — avoid if possible. Restricted = entry is prohibited entirely; stepping into " +
                    "one automatically raises a critical alert to the tourist police.",
                    "Show my current zone status", "Find nearest police station");
            }

            // 3. "Am I safe / what zone am I in" — answered with the tourist's ACTUAL last
            //    known location, not a generic sentence.
            if (Contains(text, "zone", "area", "am i safe", "is it safe", "how risky", "risk score",
                              "where am i", "current status", "my status", "is this safe"))
            {
                var lastLog = tourist is not null
                    ? _db.LocationLogs.Include(l => l.Zone).Where(l => l.TouristId == tourist.Id).OrderByDescending(l => l.Timestamp).FirstOrDefault()
                    : null;

                if (lastLog is null)
                {
                    return Reply(
                        "I don't have a location reading for you yet — allow location access on your dashboard " +
                        "(or tap Refresh there) and I'll be able to tell you exactly which zone you're in and how risky it is.",
                        "How do I share my location?");
                }

                var zoneName = lastLog.Zone?.Name;
                var riskLevel = lastLog.Zone?.RiskLevel;
                var minutesAgo = (int)(DateTime.UtcNow - lastLog.Timestamp).TotalMinutes;
                var ageNote = minutesAgo <= 1 ? "just now" : $"about {minutesAgo} minute(s) ago";

                if (lastLog.IsGeoFenceBreach)
                {
                    return Reply(
                        $"⚠️ Your last reading ({ageNote}) was inside '{zoneName}', which is a RESTRICTED zone. " +
                        $"This already triggered an automatic alert to the tourist police. Leave the area now if you can, " +
                        $"and use the SOS button if you need immediate help.",
                        "Use SOS button", "Find nearest police station");
                }

                if (zoneName is not null && riskLevel is not null)
                {
                    return Reply(
                        $"Your last reading ({ageNote}) put you in '{zoneName}', marked {riskLevel}. " +
                        $"Your AI safety score at that time was {lastLog.AiRiskScore}/100 ({lastLog.AiRiskCategory}).",
                        riskLevel is RiskLevel.Caution or RiskLevel.HighRisk ? "What should I do in a Caution/High-Risk area?" : "Find nearest police station",
                        "What do the risk levels mean?");
                }

                return Reply(
                    $"Your last reading ({ageNote}) was outside every zone we actively monitor — an unmonitored area. " +
                    $"Your AI safety score at that time was {lastLog.AiRiskScore}/100 ({lastLog.AiRiskCategory}). Stay alert and keep location sharing on.",
                    "Find nearest police station", "What do the risk levels mean?");
            }

            if (Contains(text, "what should i do", "caution area", "high-risk area", "high risk area"))
            {
                return Reply(
                    "In a Caution or High-Risk area: stay in well-lit, populated places, avoid isolated shortcuts " +
                    "(especially after dark), keep your phone charged with location sharing on, and know the nearest " +
                    "police station before you need it.",
                    "Find nearest police station", "Use SOS button");
            }

            // 4. Nearest police station — live lookup from the tourist's real last coordinates.
            if (Contains(text, "police", "station", "cops", "nearest help"))
            {
                if (tourist is not null)
                {
                    var lastLog = _db.LocationLogs
                        .Where(l => l.TouristId == tourist.Id)
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefault();

                    if (lastLog is not null)
                    {
                        var nearest = await _policeStationService.FindNearestAsync(lastLog.Latitude, lastLog.Longitude);
                        if (nearest is not null)
                        {
                            var km = (nearest.DistanceMeters / 1000.0).ToString("F1");
                            return Reply(
                                $"The nearest police station to your last known location is '{nearest.Name}', " +
                                $"about {km} km away.{(string.IsNullOrWhiteSpace(nearest.Phone) ? "" : $" Phone: {nearest.Phone}.")}",
                                "Use SOS button", "Show my current zone status");
                        }
                        return Reply("I couldn't find a police station near your last location right now — try the SOS button for an urgent situation.",
                            "Use SOS button");
                    }
                }
                return Reply("Share your location on the dashboard first (allow location access) and I can find the real nearest police station for you.",
                    "How do I share my location?");
            }

            // 5. Digital ID — real record lookup, not a canned sentence.
            if (Contains(text, "digital id", "blockchain", "verify my id", "my identity", "id status"))
            {
                if (tourist is not null)
                {
                    var digitalId = _db.DigitalIdentities
                        .Where(d => d.TouristId == tourist.Id)
                        .OrderByDescending(d => d.BlockIndex)
                        .FirstOrDefault();

                    if (digitalId is not null)
                    {
                        var status = digitalId.IsRevoked ? "revoked" :
                            DateTime.UtcNow > digitalId.ExpiresAt ? "expired" : "active and valid";
                        return Reply($"Your blockchain Digital ID (block #{digitalId.BlockIndex}) is currently {status}. " +
                                     $"It expires on {digitalId.ExpiresAt.ToLocalTime():d}.", "Contact support");
                    }
                }
                return Reply("I don't see a Digital ID on record for you yet — this is normally issued automatically when you register. Contact tourist support.");
            }

            if (Contains(text, "lost passport", "lost document", "lost my documents", "stolen passport", "lost my id"))
            {
                return Reply(
                    "Report this as an incident right away, choose type 'Other', and contact your embassy/consulate " +
                    "as soon as possible. The nearest police station can also issue a loss report you may need for a replacement.",
                    "Find nearest police station", "Report a non-emergency incident");
            }

            if (Contains(text, "medical", "doctor", "hospital", "sick", "injured", "injury"))
            {
                return Reply(
                    "For a medical emergency, use the SOS button — it flags your case as critical and shares your " +
                    "location with responders immediately. For non-urgent care, ask your hotel or the nearest " +
                    "police station for the closest hospital.",
                    "Use SOS button", "Find nearest police station");
            }

            if (Contains(text, "thank", "thanks", "thank you"))
            {
                return Reply("You're welcome — stay safe! Let me know if you need anything else.");
            }

            if (Contains(text, "hello", "hi", "hey", "namaste"))
            {
                return Reply("Hello! I'm your tourist safety assistant. Ask me about your current area's safety, " +
                             "the nearest police station, your Digital ID, or how to raise an SOS alert.",
                             "Show my current zone status", "Find nearest police station", "Use SOS button");
            }

            return Reply(
                "I didn't quite catch that. I can tell you: your real current zone/risk score, the nearest police " +
                "station, your Digital ID status, what to do about lost documents, medical help, or how SOS works. " +
                "Try one of the buttons below, or rephrase your question.",
                "Show my current zone status", "Find nearest police station", "Use SOS button");
        }

        private static bool Contains(string text, params string[] keywords)
            => keywords.Any(k => k.Length <= 3
                // Short keywords (e.g. "hi", "sos") are matched as whole words only —
                // otherwise they'd falsely match as a substring of unrelated words
                // like "this" or "history".
                ? Regex.IsMatch(text, $@"(?<![a-z]){Regex.Escape(k)}(?![a-z])", RegexOptions.IgnoreCase)
                : text.Contains(k, StringComparison.OrdinalIgnoreCase));

        private static ChatResponseDto Reply(string message, params string[] quickReplies)
            => new() { Reply = message, QuickReplies = quickReplies.ToList() };
    }
}
