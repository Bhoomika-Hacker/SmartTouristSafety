using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// MVC + Web API controllers (both use the same [Controller] pipeline in ASP.NET Core)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Serialize enums (RiskLevel, IncidentSeverity, etc.) as their names ("Safe", "HighRisk")
        // instead of raw numbers, so API responses and the front-end badges stay readable.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// EF Core (SQLite file database)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient used for the live, worldwide police-station lookup (OpenStreetMap Overpass API)
builder.Services.AddHttpClient("Overpass", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartTouristSafety/1.0 (student project)");
});

// HttpClient for the live, keyless GDELT news feed used by the dynamic risk engine
builder.Services.AddHttpClient("Gdelt", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartTouristSafety/1.0 (student project)");
});

// HttpClient for any generically-configured police/government open-data JSON feeds
builder.Services.AddHttpClient("ExternalReports", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartTouristSafety/1.0 (student project)");
});

// HttpClient for the live, keyless OpenStreetMap Nominatim reverse-geocoding lookup
builder.Services.AddHttpClient("Nominatim", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartTouristSafety/1.0 (student project)");
});

// Domain services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IGeoFencingService, GeoFencingService>();
builder.Services.AddScoped<IDynamicRiskZoneService, DynamicRiskZoneService>();
builder.Services.AddScoped<IAiRiskService, AiRiskService>();
builder.Services.AddScoped<IIncidentResponseService, IncidentResponseService>();
builder.Services.AddScoped<IPoliceStationService, PoliceStationService>();
builder.Services.AddScoped<IReverseGeocodingService, NominatimReverseGeocodingService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// Automatic, real-world risk data ingestion — pluggable external report sources feeding the
// dynamic risk engine. Add more IExternalReportSource implementations to plug in another feed.
builder.Services.Configure<GdeltSourceOptions>(builder.Configuration.GetSection("ExternalReportSources:Gdelt"));
builder.Services.AddScoped<IExternalReportSource, GdeltNewsReportSource>();

var genericSourceConfigs = builder.Configuration
    .GetSection("ExternalReportSources:GenericSources")
    .Get<List<JsonReportSourceConfig>>() ?? new List<JsonReportSourceConfig>();
foreach (var sourceConfig in genericSourceConfigs)
{
    builder.Services.AddScoped<IExternalReportSource>(sp =>
        new GenericJsonReportSource(
            sp.GetRequiredService<IHttpClientFactory>(),
            sourceConfig,
            sp.GetRequiredService<ILogger<GenericJsonReportSource>>()));
}

builder.Services.AddScoped<IExternalReportIngestionService, ExternalReportIngestionService>();
builder.Services.AddHostedService<ExternalReportSyncBackgroundService>();

// Cookie authentication for the dashboard (Admin / Authority / Operator login)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed the database on startup (demo project — safe to EnsureCreated instead of migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var blockchain = scope.ServiceProvider.GetRequiredService<IBlockchainService>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    SeedData.Initialize(db, blockchain, hasher);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Web API routes (attribute routed, e.g. /api/tourists)
app.MapControllers();

// MVC default route (e.g. /Tourists/Index, /Dashboard)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
