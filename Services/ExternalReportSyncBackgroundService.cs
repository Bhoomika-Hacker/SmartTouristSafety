namespace SmartTouristSafety.Services
{
    /// <summary>
    /// Runs the external report sync automatically in the background — once shortly after
    /// startup, then on a fixed interval — so real-world reports keep flowing in without
    /// anyone having to trigger it by hand.
    /// </summary>
    public class ExternalReportSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExternalReportSyncBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public ExternalReportSyncBackgroundService(IServiceProvider serviceProvider, ILogger<ExternalReportSyncBackgroundService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(configuration.GetValue<int>("DynamicRisk:SyncIntervalMinutes", 30));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Small initial delay so app startup / DB seeding finishes first.
            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); } catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var ingestion = scope.ServiceProvider.GetRequiredService<IExternalReportIngestionService>();
                    await ingestion.SyncAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "External report sync cycle failed; will retry on the next interval.");
                }

                try { await Task.Delay(_interval, stoppingToken); } catch (TaskCanceledException) { break; }
            }
        }
    }
}
