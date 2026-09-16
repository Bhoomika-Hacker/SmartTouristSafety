using SmartTouristSafety.Data;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Services
{
    public interface IExternalReportIngestionService
    {
        /// <summary>Pulls from every registered external source and stores any new reports. Returns how many were added.</summary>
        Task<int> SyncAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Orchestrates every registered <see cref="IExternalReportSource"/>, normalizes what
    /// each one returns, de-duplicates against what's already stored, and persists the rest
    /// as <see cref="AreaReport"/> rows the dynamic risk engine can score against. One
    /// misbehaving source never blocks the others — each is isolated and logged.
    /// </summary>
    public class ExternalReportIngestionService : IExternalReportIngestionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEnumerable<IExternalReportSource> _sources;
        private readonly ILogger<ExternalReportIngestionService> _logger;

        public ExternalReportIngestionService(ApplicationDbContext db, IEnumerable<IExternalReportSource> sources, ILogger<ExternalReportIngestionService> logger)
        {
            _db = db;
            _sources = sources;
            _logger = logger;
        }

        public async Task<int> SyncAsync(CancellationToken ct = default)
        {
            var added = 0;

            foreach (var source in _sources.Where(s => s.Enabled))
            {
                IReadOnlyList<NormalizedAreaReport> fetched;
                try
                {
                    fetched = await source.FetchRecentReportsAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Sync failed for external report source '{Source}'.", source.SourceName);
                    continue;
                }

                foreach (var report in fetched)
                {
                    var exists = _db.AreaReports.Any(r =>
                        r.SourceName == report.SourceName && r.ExternalReference == report.ExternalReference);
                    if (exists) continue;

                    _db.AreaReports.Add(new AreaReport
                    {
                        Latitude = report.Latitude,
                        Longitude = report.Longitude,
                        Severity = report.Severity,
                        Description = report.Description,
                        Source = report.Source,
                        SourceName = report.SourceName,
                        ExternalReference = report.ExternalReference,
                        OccurredAt = report.OccurredAt,
                        Url = report.Url
                    });
                    added++;
                }
            }

            if (added > 0) await _db.SaveChangesAsync(ct);
            _logger.LogInformation("External report sync complete: {Added} new report(s) ingested.", added);
            return added;
        }
    }
}
