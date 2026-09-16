<h1 align="center">🚀 Smart Tourist Safety</h1>

<p align="center"> <strong>Smart Tourist Safety Monitoring & Incident Response System</strong><br/> <em>with AI risk scoring, geo-fencing, dynamic risk detection, Digital ID and emergency response</em> </p>

<p align="center"> <img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10"/> <img src="https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge" alt="ASP.NET Core MVC"/> <img src="https://img.shields.io/badge/Web%20API-REST-0A66C2?style=for-the-badge" alt="Web API"/> <img src="https://img.shields.io/badge/Entity%20Framework%20Core-SQLite-003B57?style=for-the-badge" alt="EF Core SQLite"/> <img src="https://img.shields.io/badge/Security-SHA--256-111827?style=for-the-badge" alt="SHA-256"/> </p>

<p align="center"> <em>Tourist Safety • AI Risk • Geo-Fencing • SOS • Incidents • Digital ID • Police Lookup • Analytics</em> </p>

<p align="center"> <a href="#-about-the-project">About</a> • <a href="#-key-features">Features</a> • <a href="#-system-architecture">Architecture</a> • <a href="#-risk-scoring">Risk Scoring</a> • <a href="#-digital-id">Digital ID</a> • <a href="#-installation--setup">Setup</a> </p>

<h2>📸 Project Images</h2>

Tip: Add your application screenshots to docs/screenshots/ and replace the placeholders below.

Tourist Dashboard


Authority Dashboard


Recommended GitHub Screenshot Layout
Create this folder:

docs/
└── screenshots/
    ├── login.png
    ├── tourist-dashboard.png
    ├── authority-dashboard.png
    ├── location.png
    ├── risk-score.png
    ├── geo-fencing.png
    ├── incidents.png
    ├── sos.png
    ├── digital-id.png
    ├── police-stations.png
    ├── chatbot.png
    └── reports.png
Then add them here:

![Login](docs/screenshots/login.png)
![Tourist Dashboard](docs/screenshots/tourist-dashboard.png)
![Authority Dashboard](docs/screenshots/authority-dashboard.png)
![Risk Score](docs/screenshots/risk-score.png)
![Geo-Fencing](docs/screenshots/geo-fencing.png)
![Incidents](docs/screenshots/incidents.png)
![Digital ID](docs/screenshots/digital-id.png)
<h2>🚀 About the Project</h2>

Smart Tourist Safety is a web-based tourist safety and incident-response platform built with ASP.NET Core MVC + Web API and Entity Framework Core with SQLite.

The system connects the complete safety workflow:

Tourist Location → Geo-Fence Check → Risk Assessment → Alert / Incident → Authority Response

The project also includes a Digital ID module based on SHA-256 hash chaining, dynamic report-driven risk assessment, nearest police-station lookup, reverse geocoding and a safety chatbot.

<h2>✨ Key Features</h2>

<h3>🧭 Tourist Safety Monitoring</h3>

Tourist registration and account management

Browser/device-based location sharing

Current safety status

Current risk score

Geo-fence status

Dynamic risk information

Personal incident history

Nearest police-station information

<h3>🤖 AI-Based Risk Scoring</h3>

Explainable risk-scoring service

Risk score from 0–100

Zone-based risk contribution

Restricted-zone breach detection

Tourist inactivity/silence detection

Abnormal movement-speed detection

Night-time risk factor

Dynamic nearby-report risk contribution

Risk categories: Normal, Moderate, High and Critical

The current project uses an explainable weighted risk heuristic rather than a trained black-box machine-learning model.

<h3>📍 Geo-Fencing</h3>

Safe zones

Caution zones

High-Risk zones

Restricted zones

Haversine-distance based location checking

Automatic restricted-zone breach detection

Automatic incident generation for critical breaches

Location-aware tourist alerts

<h3>🚨 SOS & Incident Response</h3>

One-tap panic / SOS support

Manual incident reporting

Geo-fence breach incidents

Anomaly-related incidents

Automatic alert logging

Incident assignment

Incident status management

Resolution notes

<h3>🔗 Digital ID</h3>

Tourist Digital ID generation

SHA-256 hashing

Hash-chained identity blocks

Previous-hash linking

Digital ID verification

Digital ID revocation

Complete chain-integrity verification

Tamper detection

Note: The current implementation demonstrates the blockchain/hash-chain concept locally. It does not require a live Ethereum or Hyperledger network.

<h3>🌐 Dynamic Risk Areas</h3>

Risk calculation for any coordinate

Nearby incident/report detection

Severity-based risk contribution

Recency-based risk contribution

Distance-based risk weighting

External report integration

Dynamic risk even where no manually created geo-fence exists

<h3>👮 Authority Dashboard</h3>

The authority side can manage:

Tourists

Geo-fence zones

Incidents

Alerts

Digital IDs

Safety operations

Incident status and response

<h3>🚓 Police Station Lookup</h3>

Find nearby police stations

OpenStreetMap/Overpass integration

Coordinate-based search

Progressive search area where required

Local/demo fallback data

<h3>🗺️ Reverse Geocoding</h3>

The system can convert:

Latitude + Longitude
        ↓
Readable Place Name
using an OpenStreetMap-based reverse-geocoding service.

<h3>💬 Safety Chatbot</h3>

The project includes a safety-focused chatbot interface for tourist assistance and project demonstrations.

<h2>📊 Risk Scoring</h2>

The risk service produces a score between 0 and 100.

Score	Risk Category
0–39	Normal
40–69	Moderate
70–89	High
90–100	Critical
Risk Processing
flowchart TD
    A[Tourist GPS Location] --> B[Check Active Geo-Fence]
    B --> C[Check Restricted Breach]
    C --> D[Calculate Dynamic Area Risk]
    D --> E[AI Risk Scoring]
    E --> F{Risk Level}
    F -->|Normal| G[Continue Monitoring]
    F -->|Moderate| H[Safety Warning]
    F -->|High| I[Enhanced Monitoring]
    F -->|Critical| J[Alert / Incident Response]
Main Risk Factors
Zone Risk
    +
Restricted-Zone Breach
    +
Inactivity / Silence
    +
Abnormal Movement
    +
Night-Time Factor
    +
Dynamic Nearby Reports
    =
Final Risk Score (0–100)
<h2>📍 Geo-Fencing Flow</h2>

flowchart LR
    A[Tourist Location Ping] --> B[Find Active Zones]
    B --> C[Haversine Distance]
    C --> D{Inside Zone?}
    D -->|No| E[No Zone / Dynamic Risk]
    D -->|Yes| F[Read Zone Risk Level]
    F --> G{Restricted?}
    G -->|No| H[Safety Status]
    G -->|Yes| I[Create Incident]
    I --> J[Create Alert]
    J --> K[Authority Dashboard]
The Haversine formula is used to calculate the distance between two latitude/longitude points.

<h2>🚨 SOS / Incident Response Flow</h2>

sequenceDiagram
    participant T as Tourist
    participant API as Incident API
    participant DB as Database
    participant A as Authority

    T->>API: Press SOS
    API->>DB: Create Critical Incident
    API->>DB: Create Alert Log
    DB-->>A: Incident Available
    A->>DB: Acknowledge Incident
    A->>DB: Assign Responder
    A->>DB: Resolve Incident
    A->>DB: Close Incident
Incident Status
Reported
   ↓
Acknowledged
   ↓
Responder Dispatched
   ↓
Resolved
   ↓
Closed
<h2>🔗 Digital ID</h2>

The Digital ID module uses a hash-chain structure.

flowchart LR
    A[Tourist] --> B[Issue Digital ID]
    B --> C[Create Identity Block]
    C --> D[Generate SHA-256 Hash]
    D --> E[Link Previous Hash]
    E --> F[Store Digital ID]
    F --> G[Verify ID]
    G --> H{Hash Valid?}
    H -->|Yes| I[Identity Valid]
    H -->|No| J[Tampering Detected]
Digital ID Data
Each identity block can contain:

Tourist ID

Block index

Previous hash

Current hash

Issue time

Expiry time

Status

Hash relationship:

PreviousHash → CurrentHash
<h2>🌐 Dynamic Risk Area</h2>

Dynamic risk extends safety monitoring beyond manually created geo-fence zones.

flowchart TD
    A[Tourist Coordinate] --> B[Search Nearby Reports]
    B --> C[Filter Recent Reports]
    C --> D[Calculate Distance]
    D --> E[Apply Severity]
    E --> F[Apply Recency]
    F --> G[Calculate Dynamic Risk]
    G --> H[AI Risk Score]
Dynamic Risk Inputs
Incident severity

Report age / recency

Distance from tourist

Number of nearby reports

Report source

This allows the system to react to recently reported risk in locations that do not yet have a manually configured safety zone.

<h2>👮 Authority Workflow</h2>

Tourist / System Event
        ↓
Incident Created
        ↓
Alert Logged
        ↓
Authority Dashboard
        ↓
Incident Acknowledged
        ↓
Responder Dispatched
        ↓
Incident Resolved
        ↓
Incident Closed
Authorities can monitor and manage the safety state of tourists and incidents through the centralized dashboard.

<h2>🏗️ System Architecture</h2>

flowchart TD
    U[Tourist / Authority] --> UI[ASP.NET Core MVC Views]

    UI --> C[Controllers]
    C --> API[REST API Controllers]

    C --> S[Application Services]
    API --> S

    S --> GF[Geo-Fencing Service]
    S --> AI[AI Risk Service]
    S --> DR[Dynamic Risk Service]
    S --> IR[Incident Response Service]
    S --> DID[Digital ID / Hash Chain Service]
    S --> POL[Police Station Service]
    S --> GEO[Reverse Geocoding Service]

    S --> EF[Entity Framework Core]
    EF --> DB[(SQLite Database)]

    POL --> OSM[OpenStreetMap / Overpass]
    GEO --> NOM[Nominatim]
    DR --> EXT[GDELT / External Reports]
<h2>🗄️ Database / ER Diagram</h2>

erDiagram
    TOURIST ||--o{ LOCATION_LOG : generates
    TOURIST ||--o{ INCIDENT : reports
    TOURIST ||--o{ ALERT_LOG : receives
    TOURIST ||--o| DIGITAL_IDENTITY : owns
    GEO_FENCE_ZONE ||--o{ LOCATION_LOG : evaluates
    GEO_FENCE_ZONE ||--o{ INCIDENT : triggers
    INCIDENT ||--o{ ALERT_LOG : generates
    POLICE_STATION ||--o{ INCIDENT : supports
    AREA_REPORT ||--o{ INCIDENT : relates_to

    TOURIST {
        int TouristId PK
        string Name
        string Email
        string Phone
        string Status
    }

    LOCATION_LOG {
        int LocationLogId PK
        int TouristId FK
        double Latitude
        double Longitude
        datetime Timestamp
    }

    GEO_FENCE_ZONE {
        int ZoneId PK
        string Name
        double Latitude
        double Longitude
        double Radius
        string RiskLevel
    }

    INCIDENT {
        int IncidentId PK
        int TouristId FK
        int ZoneId FK
        string Type
        string Severity
        string Status
        datetime CreatedAt
    }

    ALERT_LOG {
        int AlertId PK
        int TouristId FK
        int IncidentId FK
        string AlertType
        datetime CreatedAt
    }

    DIGITAL_IDENTITY {
        int DigitalIdentityId PK
        int TouristId FK
        int BlockIndex
        string PreviousHash
        string CurrentHash
        string Status
    }

    POLICE_STATION {
        int PoliceStationId PK
        string Name
        double Latitude
        double Longitude
    }

    AREA_REPORT {
        int AreaReportId PK
        double Latitude
        double Longitude
        string Severity
        datetime ReportedAt
    }
<h2>🔌 REST API</h2>

Method	Endpoint	Purpose
GET	/api/tourists	List tourists
GET	/api/tourists/{id}	Get tourist details
POST	/api/tourists	Register tourist
GET	/api/zones	List active zones
POST	/api/zones	Create geo-fence zone
POST	/api/location/ping	Process tourist location
GET	/api/location/history/{touristId}	Location history
GET	/api/incidents	List incidents
POST	/api/incidents	Create incident
POST	/api/incidents/panic	Trigger SOS
PUT	/api/incidents/status	Update incident status
GET	/api/risk/at?lat=&lng=	Calculate risk at coordinate
POST	/api/risk/sync-reports	Synchronize reports
GET	/api/digitalid/verify/{touristId}	Verify Digital ID
GET	/api/digitalid/verify-chain	Verify Digital ID chain
POST	/api/digitalid/issue/{touristId}	Issue Digital ID
POST	/api/digitalid/revoke/{touristId}	Revoke Digital ID
GET	/api/police-stations/nearest	Find nearest police station
POST	/api/chatbot/ask	Ask safety chatbot
<h2>🛠️ Tech Stack</h2>

Technology	Purpose
C#	Application language
.NET 10	Runtime / framework
ASP.NET Core MVC	Web application
ASP.NET Core Web API	REST API layer
Entity Framework Core	ORM / database access
SQLite	Application database
Razor Views	Server-side UI
Bootstrap 5	UI styling
JavaScript	Client-side interaction
Browser Geolocation	Tourist location
Haversine Formula	Geo-fence distance calculation
SHA-256	Digital ID hashing
OpenStreetMap	Mapping / location services
Nominatim	Reverse geocoding
Overpass API	Nearby police lookup
GDELT	External report ingestion
<h2>📁 Project Structure</h2>

SmartTouristSafety/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── IncidentsController.cs
│   ├── TouristsController.cs
│   ├── TouristPortalController.cs
│   └── ZonesController.cs
│
├── Controllers/Api/
│   ├── ChatbotApiController.cs
│   ├── DigitalIdApiController.cs
│   ├── IncidentsApiController.cs
│   ├── LocationApiController.cs
│   ├── PoliceStationsApiController.cs
│   ├── RiskApiController.cs
│   ├── TouristsApiController.cs
│   └── ZonesApiController.cs
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── SeedData.cs
│
├── Models/
│   ├── Tourist.cs
│   ├── Incident.cs
│   ├── DigitalIdentity.cs
│   ├── GeoFenceZone.cs
│   ├── LocationLog.cs
│   ├── AlertLog.cs
│   ├── AreaReport.cs
│   ├── PoliceStation.cs
│   └── ViewModels.cs
│
├── Services/
│   ├── IAiRiskService.cs
│   ├── IBlockchainService.cs
│   ├── IGeoFencingService.cs
│   ├── IIncidentResponseService.cs
│   ├── IDynamicRiskZoneService.cs
│   ├── IPoliceStationService.cs
│   └── IReverseGeocodingService.cs
│
├── Views/
│   ├── Account/
│   ├── Dashboard/
│   ├── Incidents/
│   ├── Tourists/
│   ├── TouristPortal/
│   └── Zones/
│
├── wwwroot/
│   ├── css/
│   └── js/
│
├── Program.cs
├── SmartTouristSafety.csproj
└── SmartTouristSafety.sln
<h2>⚙️ Installation & Setup</h2>

<h3>1. Clone the repository</h3>

git clone https://github.com/Bhoomika-Hacker/SmartTouristSafety.git
cd SmartTouristSafety
<h3>2. Restore dependencies</h3>

dotnet restore
<h3>3. Build the project</h3>

dotnet build
<h3>4. Run the application</h3>

dotnet run
Then open the local URL displayed by ASP.NET Core in your browser.

<h2>🔐 Demo Accounts</h2>

Admin
Username: admin
Password: Admin@123
Authority / Officer
Username: officer
Password: Officer@123
Tourist
Username: emma
Password: Tourist@123
These credentials are intended for the academic/demo environment and should be changed before production deployment.

<h2>🧪 Example Location API</h2>

A tourist location can be processed using:

curl -X POST http://localhost:5080/api/location/ping ^
  -H "Content-Type: application/json" ^
  -d "{\"touristId\":1,\"latitude\":28.7041,\"longitude\":77.1025}"
Processing flow:

GPS Location
     ↓
Geo-Fence Check
     ↓
Dynamic Risk
     ↓
AI Risk Score
     ↓
Risk Classification
     ↓
Incident / Alert Decision
     ↓
Database Logging
<h2>🚨 SOS Example</h2>

Tourist presses SOS
        ↓
Panic API receives request
        ↓
Critical Incident Created
        ↓
Alert Log Created
        ↓
Authority Dashboard Updated
        ↓
Responder Assigned
        ↓
Incident Resolved
        ↓
Incident Closed
<h2>🗺️ External Services</h2>

OpenStreetMap Nominatim
Used for reverse geocoding and converting coordinates into readable place information.

OpenStreetMap Overpass
Used for nearby police-station lookup.

GDELT
Used as an external report source for dynamic risk-area calculations.

External services depend on network availability and public API limitations. Production deployments should use appropriate service policies, rate limits and infrastructure.

<h2>🔒 Security & Design</h2>

Role-based access for tourist and authority functionality.

Tourist operations are restricted to the appropriate tourist account.

SHA-256 hash verification provides tamper detection for Digital ID records.

Incident status follows a defined response workflow.

Service interfaces separate core logic from external integrations.

External service failures can use project-level fallback behaviour where implemented.

Do not commit passwords, API keys or production secrets to GitHub.

Recommended .gitignore entries:

bin/
obj/
.vs/
*.user
*.suo
appsettings.Production.json
secrets.json
<h2>📈 Project Workflow</h2>

flowchart LR
    A[Tourist Registration] --> B[Location Sharing]
    B --> C[Geo-Fence Check]
    C --> D[Risk Assessment]
    D --> E{Safety Event?}
    E -->|No| F[Continue Monitoring]
    E -->|Yes| G[Incident / Alert]
    G --> H[Authority Dashboard]
    H --> I[Responder Action]
    I --> J[Resolve Incident]
    J --> K[Close Incident]
<h2>🔮 Future Scope</h2>

Possible future extensions:

Android/iOS tourist application

Real-time push/SMS emergency notifications

Advanced ML models trained on larger incident datasets

Computer-vision based crowd monitoring

Multi-language emergency assistance

Production blockchain/DID infrastructure

Government emergency-system integration

Smart-city sensor integration

AR-based safety navigation

More detailed police/crime open-data integration

Cloud deployment

Real-time location streaming

Emergency contact integration

<h2>🖼️ Adding Screenshots to GitHub</h2>

For the best GitHub presentation, take screenshots of:

Login page

Tourist dashboard

Authority dashboard

Location monitoring

Risk score

Geo-fence zones

Incidents

SOS

Digital ID

Police station lookup

Safety chatbot

Reports / analytics

Save them under:

docs/screenshots/
Recommended naming:

01-login.png
02-tourist-dashboard.png
03-authority-dashboard.png
04-location.png
05-risk-score.png
06-geo-fencing.png
07-incidents.png
08-sos.png
09-digital-id.png
10-police-stations.png
11-chatbot.png
12-reports.png
<h2>📌 Important Notes</h2>

Risk scoring: The current implementation is an explainable weighted risk heuristic, not a trained ML prediction model.

Digital ID: The project uses SHA-256 hash chaining to demonstrate tamper-evident identity records.

Blockchain: A live public blockchain network is not required by the current implementation.

Location: Browser/device geolocation requires appropriate browser permission.

External services: OpenStreetMap/Nominatim/Overpass/GDELT functionality depends on network availability.

Police lookup: A local/demo fallback can be used when live lookup is unavailable.

Project purpose: The application is intended for academic/project demonstration and can be extended for production use.

<h2>👨‍💻 Project Team</h2>

Name	Roll Number	Branch
Student’s Name	Roll Number	Civil Engineering
Student’s Name	Roll Number	Civil Engineering
Guided By
Supervisor Name

<h2>📜 License</h2>

This project is developed for academic/project demonstration purposes.

If you plan to publish it as an open-source project, add an appropriate license such as MIT.

<h2>⭐ Project Summary</h2>

Smart Tourist Safety combines location intelligence, risk assessment, geo-fencing, digital identity and incident management into a single web application.

Core Modules
Tourist Monitoring
       +
Geo-Fencing
       +
AI Risk Scoring
       +
Dynamic Risk
       +
SOS & Incidents
       +
Digital ID
       +
Police Lookup
       +
Authority Dashboard
       =
SMART TOURIST SAFETY
<p align="center"> <strong>Smart Tourist Safety — Monitor. Assess. Alert. Respond.</strong> </p>

