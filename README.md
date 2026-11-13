# AgriStore: Smart Agricultural Storage and Post-Harvest Loss Prevention System

CSE253 (.NET Programming) project, section RB.
Team: Jinadev R P, S Pradeep, Mirza Ali Baaqar.
Theme: Agriculture, FoodTech and Rural Development.

**Live demo:** https://agristore-w0qv.onrender.com (free plan: the first load after idle takes about a minute)



A large share of India's fruit, vegetables and grain is lost after harvest because it is
kept too long, or kept at the wrong temperature and humidity. Storage operators find out
only when the produce has already started to rot.

## What AgriStore does

- Keeps a record of every **storage unit** (cold store, warehouse, silo, godown) and every
  **stock lot** (crop, farmer, quantity, date stored).
- Receives **temperature and humidity readings** from sensors through a REST API, or by hand.
- Compares each lot against its **crop profile** (safe temperature, safe humidity, shelf life).
- Gives every lot a **spoilage risk score** (0-100, Low / Medium / High / Critical), the
  reasons behind it, and a clear action ("Sell, process or move this lot today").
- The overview page tells the operator in one sentence how many lots need action, then lists
  them worst first, next to how full each storage unit is and its latest conditions.

### How the risk score works (`Services/RiskService.cs`)

| Factor | Points |
|---|---|
| Age: share of shelf life used | up to 50 (60 once past shelf life) |
| Temperature outside the crop's safe range | 8 per °C, max 30 |
| Humidity outside the crop's safe range | 1.5 per %, max 20 |
| No sensor reading in the last 24 hours | 10 |

Score 0-29 Low, 30-54 Medium, 55-79 High, 80-100 Critical.

## Run it

Needs the .NET 10 SDK. No database install needed: it uses SQLite and creates and fills
`app.db` with demo data on first run.

```bash
cd src/AgriStore.Web
dotnet run
```

Open http://localhost:5283

| Account | Email | Password | Can do |
|---|---|---|---|
| Admin (seeded) | admin@agristore.local | Admin@123 | Everything, including storage units and crops |
| Staff | Register any account | | Record lots and readings, dispatch lots |

API documentation (Swagger): http://localhost:5283/swagger

Run the tests:

```bash
dotnet test
```

### Using SQL Server instead of SQLite

In `appsettings.json` set `"DatabaseProvider": "SqlServer"` and a SQL Server connection
string, then recreate the migration for SQL Server:

```bash
rm -r src/AgriStore.Web/Data/Migrations
dotnet ef migrations add InitialCreate -p src/AgriStore.Web -o Data/Migrations
```

## Deploy (Render)

The `Dockerfile` builds and runs the app. On Render: New Web Service, connect this repo,
runtime Docker. The SQLite database lives in `/tmp` and is reseeded with demo data on every
deploy or restart.

## REST API

Reads are open. Writes need the header `X-Api-Key` (value in `appsettings.json`, `Api:Key`).

| Method | URL | Purpose |
|---|---|---|
| GET | `/api/units` | Storage units, stored tonnes, latest reading |
| GET | `/api/units/{id}/readings?take=50` | Recent readings for one unit |
| GET | `/api/lots/at-risk?minLevel=Medium` | Lots at or above a risk level, with reasons |
| POST | `/api/readings` | A sensor sends a reading |

```bash
curl -X POST http://localhost:5283/api/readings \
  -H "Content-Type: application/json" -H "X-Api-Key: agristore-demo-key" \
  -d '{"storageUnitId": 1, "temperatureC": 5.2, "humidityPercent": 90}'
```

## Project structure

```
AgriStore.sln
src/AgriStore.Web/
  Program.cs                 services, DI, middleware pipeline
  Models/Entities.cs         CropProfile, StorageUnit, StockLot, SensorReading
  Models/ViewModels.cs       dashboard and page models
  Models/ApiDtos.cs          API request/response records (DTOs)
  Data/ApplicationDbContext  EF Core DbContext (+ Identity tables)
  Data/DbSeeder.cs           migrations, admin user, demo data
  Data/Migrations/           code-first migration
  Services/RiskService.cs    spoilage risk engine (pure logic, unit tested)
  Services/InsightService.cs LINQ queries joining stock, readings and risk
  Middleware/ApiKeyMiddleware.cs  custom middleware guarding API writes
  Controllers/               MVC controllers + Api/StorageApiController
  Views/                     Razor views, layout, partials
tests/AgriStore.Tests/       xUnit tests for the risk engine
```

## Syllabus coverage

| Unit | Where it is used |
|---|---|
| I .NET and C# basics, CLI, NuGet, project structure | `dotnet new`/`dotnet ef` CLI, solution with two projects, NuGet packages |
| II C# fundamentals, OOP, collections, exceptions | Entity classes, enums, `List<>`/`Dictionary<>`, validation errors |
| III LINQ, delegates, generics, async/await, SOLID | `InsightService` LINQ, `async` everywhere, `IRiskService` interface (DIP), one job per class (SRP) |
| IV ASP.NET Core MVC, Razor, layouts, ViewBag/ViewData/TempData, model binding, tag helpers, validation | All controllers and views; client + server validation; TempData flash messages |
| V Middleware, DI, EF Core code-first, migrations, CRUD, authentication and authorization | `ApiKeyMiddleware`, DI in `Program.cs`, `Data/Migrations`, ASP.NET Core Identity with Admin role |
| VI SQL Server, Web API, HTTP methods and status codes, DTOs, routing, Swagger | `StorageApiController` (attribute routing, 200/201/400/401/404), DTO records, Swagger UI; SQL Server switch |
