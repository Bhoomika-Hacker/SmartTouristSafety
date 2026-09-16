using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// MVC + Web API controllers
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Serialize enums as their names ("Safe", "HighRisk")
        // instead of raw numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// EF Core (SQLite file database)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// HttpClient used for the live, worldwide police-station lookup
// (OpenStreetMap Overpass API)
builder.Services.AddHttpClient("Overpass", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SmartTouristSafety/1.0 (student project)"
    );
});

// HttpClient for the live, keyless GDELT news feed
builder.Services.AddHttpClient("Gdelt", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SmartTouristSafety/1.0 (student project)"
    );
});

// HttpClient for external police/government JSON feeds
builder.Services.AddHttpClient("ExternalReports", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SmartTouristSafety/1.0 (student project)"
    );
});

// HttpClient for OpenStreetMap Nominatim reverse geocoding
builder.Services.AddHttpClient("Nominatim", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "SmartTouristSafety/1.0 (student project)"
    );
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

// External report sources
builder.Services.Configure<GdeltSourceOptions>(
    builder.Configuration.GetSection("ExternalReportSources:Gdelt")
);

builder.Services.AddScoped<IExternalReportSource, GdeltNewsReportSource>();

var genericSourceConfigs = builder.Configuration
    .GetSection("ExternalReportSources:GenericSources")
    .Get<List<JsonReportSourceConfig>>()
    ?? new List<JsonReportSourceConfig>();

foreach (var sourceConfig in genericSourceConfigs)
{
    builder.Services.AddScoped<IExternalReportSource>(sp =>
        new GenericJsonReportSource(
            sp.GetRequiredService<IHttpClientFactory>(),
            sourceConfig,
            sp.GetRequiredService<ILogger<GenericJsonReportSource>>()
        )
    );
}

builder.Services.AddScoped<IExternalReportIngestionService, ExternalReportIngestionService>();

builder.Services.AddHostedService<ExternalReportSyncBackgroundService>();

// Cookie authentication
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization();

var app = builder.Build();


// ============================================================
// DATABASE SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var blockchain = scope.ServiceProvider.GetRequiredService<IBlockchainService>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    SeedData.Initialize(db, blockchain, hasher);
}


// ============================================================
// TEMPORARY DATABASE MIGRATION FIX
// ============================================================
//
// Your existing SmartTouristSafety.db already contains the
// AreaReports table, but EF Core does not have
// InitialCreate recorded in __EFMigrationsHistory.
//
// This code records InitialCreate as already applied.
//
// IMPORTANT:
// Run the application ONCE with this code.
// Then REMOVE this entire block from Program.cs.
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.OpenConnection();

    using var command = db.Database.GetDbConnection().CreateCommand();

    command.CommandText = """
        CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
            "MigrationId" TEXT NOT NULL
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
            "ProductVersion" TEXT NOT NULL
        );

        INSERT OR IGNORE INTO "__EFMigrationsHistory"
        ("MigrationId", "ProductVersion")
        VALUES
        ('20260916034250_InitialCreate', '10.0.0');
        """;

    command.ExecuteNonQuery();

    db.Database.CloseConnection();
}


// ============================================================
// HTTP PIPELINE
// ============================================================

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


// Web API routes
app.MapControllers();


// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();