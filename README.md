# Smart Tourist Safety Monitoring & Incident Response System

An **ASP.NET Core 8 MVC + Web API** project (same architectural pattern as a typical
Hospital Management System build: Models → Data (EF Core) → Services → MVC Controllers/Views
+ a parallel Web API layer) implementing:

- **AI-based risk scoring** — a transparent, weighted heuristic ("explainable AI") that scores
  every location ping 0–100 based on zone danger level, geo-fence breaches, inactivity/silence,
  abnormal movement speed, and time of day.
- **Geo-fencing** — Haversine-distance zone containment checks against Safe / Caution /
  High-Risk / Restricted zones, with automatic incident creation on a Restricted-zone breach.
- **Real place names, not just coordinates** — `IReverseGeocodingService` (OpenStreetMap
  Nominatim, live and keyless, same pattern as the police-station lookup) turns any lat/lng into
  a real place name, so the tourist portal says "You're near Connaught Place, New Delhi" instead
  of a generic message when there's no declared zone.
- **Dynamic, report-driven risk areas** — an area does **not** need an officer/admin to draw a
  zone on it first. `IDynamicRiskZoneService` computes a live risk level for *any* coordinate
  automatically from real reports near it: tourist/officer-filed incidents already in the app,
  plus reports auto-ingested from real external feeds (a live, keyless GDELT news feed out of
  the box, and a pluggable adapter for any police/government open-data JSON portal). See
  "Dynamic Risk Areas" below.
- **Blockchain-based Digital ID** — each tourist's ID is one SHA-256 hash-chained block
  (`PreviousHash → CurrentHash`), so tampering with any past record breaks every hash that
  follows it. A `/api/digitalid/verify-chain` endpoint re-validates the whole ledger on demand.
- **Incident response workflow** — Reported → Acknowledged → Responder Dispatched → Resolved →
  Closed, with an alert log for every automated or manual trigger (geo-fence breach, AI anomaly,
  panic button, manual report).

## Project Structure

```
SmartTouristSafety/
├── SmartTouristSafety.sln  # Open this in Visual Studio / Rider
├── SmartTouristSafety.csproj
├── Models/                # Entities: Tourist, DigitalIdentity, GeoFenceZone,
│                           # LocationLog, Incident, AlertLog, AppUser, enums, DTOs
├── Data/                   # ApplicationDbContext (EF Core, SQLite) + SeedData
├── Services/                # IBlockchainService, IGeoFencingService, IAiRiskService,
│                             # IIncidentResponseService, IPasswordHasher
├── Controllers/             # MVC controllers (Home, Dashboard, Tourists, Zones,
│                             # Incidents, Account) — server-rendered Razor views
├── Controllers/Api/          # Web API controllers (TouristsApi, ZonesApi, IncidentsApi,
│                              # LocationApi, DigitalIdApi) — JSON REST endpoints
├── Views/                    # Razor views (Bootstrap 5 UI)
├── wwwroot/                  # Static assets (site.css)
└── Program.cs                 # DI wiring, auth, routing (MVC + Web API together)
```

## Running the project

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

The solution file is **`SmartTouristSafety.sln`** at the project root — open that in Visual
Studio / Rider, or use the CLI from the same folder:

```bash
cd SmartTouristSafety
dotnet restore
dotnet run
```

The app seeds a SQLite file (`SmartTouristSafety.db`) on first run with:
- Staff logins: `admin / Admin@123` (Admin) and `officer / Officer@123` (Authority) — land on the
  command-center Dashboard.
- Tourist self-service login: `emma / Tourist@123` — lands on **My Safety Dashboard**, the
  tourist-facing portal.
- 5 sample geo-fence zones (Safe → Restricted) and a small list of Delhi police stations kept
  purely as an **offline fallback** (see below).
- 2 sample tourists, each with a blockchain-issued Digital ID.

Then open the URL shown in the console (e.g. `http://localhost:5080`).

> If you use Visual Studio / Rider, just open `SmartTouristSafety.csproj` and press Run —
> `dotnet-ef` migrations aren't required since the demo uses `Database.EnsureCreated()`.
> For a production build, switch to `dotnet ef migrations add InitialCreate` +
> `Database.Migrate()` instead.

## User roles

| Role | How they sign in | What they can do |
|---|---|---|
| **Admin** | Login page | Full command-center: manage tourists, zones, incidents; create tourist portal logins |
| **Authority / Operator** | Login page | Same command-center dashboard, incident response workflow |
| **Tourist** | Login page (self-service account created by an Admin from a tourist's Details page) | **My Safety Dashboard**: live "which zone am I in" status via browser geolocation, AI risk score, nearest police station + distance, one-tap SOS button, their own incident history, and a rule-based safety chatbot. A tourist account can only ever ping/SOS on its own record — the API rejects any mismatched `touristId`. |

To give a registered tourist portal access: open **Tourists → (select tourist) → Create Portal
Login**. This generates a username/temporary password shown once — hand these to the tourist so
they can log in themselves.

## Web API quick reference

All endpoints return/accept JSON.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/tourists` | List all tourists |
| GET | `/api/tourists/{id}` | Get one tourist + recent location logs |
| POST | `/api/tourists` | Register a tourist |
| PUT/DELETE | `/api/tourists/{id}` | Update / remove a tourist |
| GET | `/api/zones` | List active geo-fence zones |
| POST/PUT/DELETE | `/api/zones[/{id}]` | Manage zones |
| POST | `/api/location/ping` | **Core endpoint** — submit a GPS ping; runs geo-fencing + AI risk scoring and auto-raises alerts/incidents |
| GET | `/api/location/history/{touristId}` | Location ping history |
| GET | `/api/incidents?status=Reported` | List incidents, optionally filtered |
| POST | `/api/incidents` | Manually report an incident |
| PUT | `/api/incidents/status` | Update incident workflow status |
| POST | `/api/incidents/panic` | Tourist app SOS / panic button |
| GET | `/api/digitalid/verify/{touristId}` | Verify one tourist's Digital ID |
| GET | `/api/digitalid/verify-chain` | Verify the integrity of the entire blockchain ledger |
| POST | `/api/digitalid/issue/{touristId}?days=30` | Issue/renew a Digital ID |
| POST | `/api/digitalid/revoke/{touristId}` | Revoke a Digital ID |
| GET | `/api/police-stations` | List admin-added fallback police stations |
| GET | `/api/police-stations/nearest?lat=&lng=` | **Live, worldwide** nearest-police-station lookup for any coordinate |
| POST | `/api/chatbot/ask` | Ask the rule-based safety assistant a question |

#### How the nearest-police-station lookup works

`GET /api/police-stations/nearest?lat={lat}&lng={lng}` does **not** use a hardcoded list. On every
call it queries the free [OpenStreetMap Overpass API](https://overpass-api.de/) live, asking for
real `amenity=police` records around the exact coordinates it's given, starting with a 3 km radius
and automatically widening (8 km → 20 km → 50 km) until it finds one. This works anywhere in the
world — not just Delhi — because it's driven entirely by the coordinates your browser/device
reports, not by anything stored in this project's database.

It only falls back to the small locally-seeded list (Delhi, for the demo) if the live API can't be
reached at all — e.g. this machine has no internet access, or both public Overpass mirrors are
temporarily rate-limited. In that fallback case only, results will be inaccurate for locations far
from Delhi; add your own local police stations via `POST /api/police-stations` (Admin/Authority
only) if you need better offline coverage for a specific region.

> **Note:** the public Overpass API is a shared, rate-limited community resource — fine for a
> demo/project, but for production traffic you should self-host an Overpass instance or switch to
> a paid places API (e.g. Google Places "police" type search) inside `PoliceStationService`.

## Dynamic Risk Areas — no manual zone required

Historically, an area only showed a risk level if an admin had drawn a `GeoFenceZone` on the
map for it — anywhere else showed up as "Unmonitored area". `IDynamicRiskZoneService` fixes
that: it scores **any** lat/lng automatically from real reports near it, no zone required.

**Where the reports come from:**
1. **In-app incidents** — anything already in `Incidents` (tourist SOS, officer-filed reports,
   AI anomaly detections).
2. **External feeds**, auto-synced in the background every `DynamicRisk:SyncIntervalMinutes`
   (default 30 min) by `ExternalReportSyncBackgroundService`:
   - **GDELT News Feed** (`GdeltNewsReportSource`) — live and **keyless**, the same
     "public API, no key needed" pattern this project already uses for police-station lookups.
     It searches GDELT's worldwide news monitoring for safety-related keywords (configurable in
     `ExternalReportSources:Gdelt:Query`) and turns matches into reports. This is genuinely live,
     real-world data, but it's news-driven — it surfaces protests, disasters, terror, large crime
     waves, not routine street-level crime.
     > This integration is written against GDELT's documented GEO 2.0 API contract but wasn't
     > exercised against the live endpoint in the environment this was built in (no outbound
     > network access there) — sanity-check the response shape once you run it for real, and
     > tune the query/timespan to your area.
   - **Generic police/open-data adapter** (`GenericJsonReportSource`) — point it at any real
     JSON open-data API (many city/police portals, e.g. Chicago's and NYC's crime datasets, run
     on Socrata and need no key for read access) by adding an entry under
     `ExternalReportSources:GenericSources` in `appsettings.json` — no code changes needed. This
     is the one to wire up for granular, hyper-local crime data for your actual jurisdiction.

**How the score is computed:** every report within `DynamicRisk:RadiusMeters` (default 2 km) and
`DynamicRisk:LookbackDays` (default 30 days) of a coordinate contributes a weight based on its
severity, how recent it is (roughly a 14-day half-life), and how close it is; the weights are
summed and squashed into a 0–100 score, then bucketed into Safe / Caution / High-Risk /
Restricted — the same scale as manually-declared zones.

**Where it shows up:**
- `GET /api/risk/at?lat=&lng=` — live risk for any coordinate, zone or no zone.
- `POST /api/risk/sync-reports` (Admin/Authority) — trigger an external sync immediately instead
  of waiting for the next background cycle.
- The tourist portal's "Where Am I Right Now?" card now always shows a real computed status
  instead of "Unmonitored area" when there's no declared zone.
- `IAiRiskService` blends the dynamic score into the overall AI risk score, and
  `IIncidentResponseService` can auto-raise an incident purely from a cluster of real nearby
  reports, even with no zone involved.
- The Dashboard shows how many external reports have been auto-ingested and when they last
  synced.

To add another real source, implement `IExternalReportSource` and register it in `Program.cs` —
the scoring logic never needs to change.

> After pulling this change, delete the existing `SmartTouristSafety.db` file (or point
> `DefaultConnection` at a fresh one) so `Database.EnsureCreated()` regenerates the schema with
> the new `AreaReport` table and the new `LocationLog` columns.

## Example: simulate a tourist entering a restricted zone

```bash
curl -X POST http://localhost:5080/api/location/ping \
  -H "Content-Type: application/json" \
  -d '{"touristId": 1, "latitude": 28.7041, "longitude": 77.1025}'
```

This coordinate matches the seeded "Border Security Zone" (Restricted) — the response will show
`isGeoFenceBreach: true`, a high `aiRiskScore`, and `incidentAutoCreated: true`, and a new row will
appear immediately on the Dashboard and Incidents pages.

## Notes on design choices

- **Why SQLite?** Zero-setup, file-based — matches the "just clone and run" simplicity of a
  typical student/demo hospital-management-style project, while still being real EF Core + SQL.
- **Why a heuristic instead of a trained ML model for "AI"?** For a safety-critical alerting
  system, an auditable, explainable scoring function is preferable to a black-box model for a
  demo/project scope; the `IAiRiskService` interface can be swapped for a real ML.NET or external
  model service without touching any controller code.
- **Why hash-chaining instead of a real blockchain node?** Demonstrates the core blockchain
  property that matters for a Digital ID use case — tamper-evidence via linked hashes — without
  requiring a running blockchain network, wallet, or gas fees for a project/demo environment.
