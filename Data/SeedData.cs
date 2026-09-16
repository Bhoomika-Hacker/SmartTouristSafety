using SmartTouristSafety.Models;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext db, IBlockchainService blockchainService, IPasswordHasher hasher)
        {
            db.Database.EnsureCreated();

            if (!db.AppUsers.Any())
            {
                db.AppUsers.Add(new AppUser
                {
                    Username = "admin",
                    DisplayName = "System Administrator",
                    PasswordHash = hasher.Hash("Admin@123"),
                    Role = UserRole.Admin
                });
                db.AppUsers.Add(new AppUser
                {
                    Username = "officer",
                    DisplayName = "Tourist Police Officer",
                    PasswordHash = hasher.Hash("Officer@123"),
                    Role = UserRole.Authority
                });
                db.SaveChanges();
            }

            if (!db.GeoFenceZones.Any())
            {
                db.GeoFenceZones.AddRange(
                    new GeoFenceZone { Name = "Connaught Place (Safe Zone)", Description = "Central tourist & shopping hub, well patrolled.", Latitude = 28.6315, Longitude = 77.2167, RadiusMeters = 1200, RiskLevel = RiskLevel.Safe },
                    new GeoFenceZone { Name = "India Gate Precinct", Description = "Monument area, high foot traffic.", Latitude = 28.6129, Longitude = 77.2295, RadiusMeters = 1000, RiskLevel = RiskLevel.Safe },
                    new GeoFenceZone { Name = "Old City Backstreets", Description = "Narrow lanes, limited lighting/CCTV at night.", Latitude = 28.6507, Longitude = 77.2334, RadiusMeters = 800, RiskLevel = RiskLevel.Caution },
                    new GeoFenceZone { Name = "Riverfront Flood Plain", Description = "Seasonal flooding risk, unstable banks.", Latitude = 28.6139, Longitude = 77.2500, RadiusMeters = 1500, RiskLevel = RiskLevel.HighRisk },
                    new GeoFenceZone { Name = "Border Security Zone", Description = "Restricted military/border area - entry prohibited.", Latitude = 28.7041, Longitude = 77.1025, RadiusMeters = 3000, RiskLevel = RiskLevel.Restricted }
                );
                db.SaveChanges();
            }

            if (!db.Tourists.Any())
            {
                var t1 = new Tourist
                {
                    FullName = "Emma Johnson",
                    PassportOrIdNumber = "US4471829",
                    Nationality = "United States",
                    Phone = "+1-202-555-0143",
                    Email = "emma.johnson@example.com",
                    EmergencyContactName = "Mark Johnson",
                    EmergencyContactPhone = "+1-202-555-0199",
                    TripStartDate = DateTime.UtcNow.Date.AddDays(-2),
                    TripEndDate = DateTime.UtcNow.Date.AddDays(10),
                    LastCheckInAt = DateTime.UtcNow.AddHours(-1)
                };
                var t2 = new Tourist
                {
                    FullName = "Hiroshi Tanaka",
                    PassportOrIdNumber = "JP2209981",
                    Nationality = "Japan",
                    Phone = "+81-90-1234-5678",
                    Email = "h.tanaka@example.com",
                    EmergencyContactName = "Yuki Tanaka",
                    EmergencyContactPhone = "+81-90-8765-4321",
                    TripStartDate = DateTime.UtcNow.Date.AddDays(-1),
                    TripEndDate = DateTime.UtcNow.Date.AddDays(6),
                    LastCheckInAt = DateTime.UtcNow.AddMinutes(-20)
                };

                db.Tourists.AddRange(t1, t2);
                db.SaveChanges();

                // Issue blockchain-based digital IDs for seeded tourists
                blockchainService.IssueDigitalId(t1.Id, TimeSpan.FromDays(30));
                blockchainService.IssueDigitalId(t2.Id, TimeSpan.FromDays(30));

                // Give Emma a demo self-service portal login (SOS button, live zone status, chatbot)
                db.AppUsers.Add(new AppUser
                {
                    Username = "emma",
                    DisplayName = t1.FullName,
                    PasswordHash = hasher.Hash("Tourist@123"),
                    Role = UserRole.Tourist,
                    TouristId = t1.Id
                });
                db.SaveChanges();
            }

            if (!db.PoliceStations.Any())
            {
                // These are only an OFFLINE FALLBACK, used solely if the live worldwide lookup
                // (OpenStreetMap Overpass API, see IPoliceStationService) can't be reached — e.g.
                // no internet access. In normal operation, nearest-station results come from the
                // live API for whatever real coordinates the tourist is actually at, anywhere in
                // the world, not from this fixed list.
                db.PoliceStations.AddRange(
                    new PoliceStation { Name = "Connaught Place Police Station", Address = "Connaught Place, New Delhi", Phone = "+91-11-2334-1234", Latitude = 28.6330, Longitude = 77.2190 },
                    new PoliceStation { Name = "India Gate Tourist Police Post", Address = "Rajpath, New Delhi", Phone = "+91-11-2338-5566", Latitude = 28.6120, Longitude = 77.2280 },
                    new PoliceStation { Name = "Old City Police Station", Address = "Chandni Chowk, Old Delhi", Phone = "+91-11-2327-7890", Latitude = 28.6500, Longitude = 77.2310 },
                    new PoliceStation { Name = "Riverfront Police Outpost", Address = "Yamuna Riverfront, Delhi", Phone = "+91-11-2223-4455", Latitude = 28.6155, Longitude = 77.2480 },
                    new PoliceStation { Name = "Border Security Checkpoint", Address = "NH-1 Border Road", Phone = "+91-11-2999-1122", Latitude = 28.7000, Longitude = 77.1100 }
                );
                db.SaveChanges();
            }
        }
    }
}
