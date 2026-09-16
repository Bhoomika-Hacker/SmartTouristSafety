using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTouristSafety.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AreaReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoFenceZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    RadiusMeters = table.Column<double>(type: "REAL", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoFenceZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoliceStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliceStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tourists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PassportOrIdNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Nationality = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "TEXT", nullable: false),
                    TripStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TripEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastCheckInAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tourists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    TouristId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUsers_Tourists_TouristId",
                        column: x => x.TouristId,
                        principalTable: "Tourists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DigitalIdentities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TouristId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentHash = table.Column<string>(type: "TEXT", nullable: false),
                    Nonce = table.Column<long>(type: "INTEGER", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalIdentities_Tourists_TouristId",
                        column: x => x.TouristId,
                        principalTable: "Tourists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TouristId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedResponder = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Tourists_TouristId",
                        column: x => x.TouristId,
                        principalTable: "Tourists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TouristId = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsInsideKnownZone = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGeoFenceBreach = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiRiskScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AiRiskCategory = table.Column<string>(type: "TEXT", nullable: true),
                    DynamicRiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ContributingReportCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationLogs_GeoFenceZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "GeoFenceZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LocationLogs_Tourists_TouristId",
                        column: x => x.TouristId,
                        principalTable: "Tourists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TouristId = table.Column<int>(type: "INTEGER", nullable: false),
                    IncidentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlertType = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertLogs_GeoFenceZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "GeoFenceZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlertLogs_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlertLogs_Tourists_TouristId",
                        column: x => x.TouristId,
                        principalTable: "Tourists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_IncidentId",
                table: "AlertLogs",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_TouristId",
                table: "AlertLogs",
                column: "TouristId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_ZoneId",
                table: "AlertLogs",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_TouristId",
                table: "AppUsers",
                column: "TouristId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaReports_SourceName_ExternalReference",
                table: "AreaReports",
                columns: new[] { "SourceName", "ExternalReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalIdentities_TouristId",
                table: "DigitalIdentities",
                column: "TouristId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TouristId",
                table: "Incidents",
                column: "TouristId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationLogs_TouristId",
                table: "LocationLogs",
                column: "TouristId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationLogs_ZoneId",
                table: "LocationLogs",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Tourists_PassportOrIdNumber",
                table: "Tourists",
                column: "PassportOrIdNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertLogs");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "AreaReports");

            migrationBuilder.DropTable(
                name: "DigitalIdentities");

            migrationBuilder.DropTable(
                name: "LocationLogs");

            migrationBuilder.DropTable(
                name: "PoliceStations");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "GeoFenceZones");

            migrationBuilder.DropTable(
                name: "Tourists");
        }
    }
}
