# Route Planner API

ASP.NET Core API for route planning, Prambanan runsheet creation, and TMS EasyGO integration.

## Requirements

- .NET 10 SDK
- Valid `JwtSettings` and connection strings in configuration (`appsettings.json` / environment)

## Run

```bash
cd RoutePlanner-Api
dotnet run --launch-profile https
```

Default URLs (see `Properties/launchSettings.json`):

- HTTPS: `https://localhost:7142`
- HTTP: `http://localhost:5217`

## API documentation (OpenAPI + Scalar)

In **Development**, interactive docs are available at:

| Resource | URL |
|---|---|
| Scalar UI | https://localhost:7142/scalar |
| OpenAPI JSON | https://localhost:7142/openapi/v1.json |

### Auth flow in Scalar

1. Call `POST /api/Auth/Login` with `user_id` and `password`.
2. Copy the `token` from the response.
3. Click **Authorize**, paste the token (with or without the `Bearer ` prefix depending on the UI), then call protected endpoints.

## Endpoints overview

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/Auth/Login` | No | Obtain JWT |
| `POST` | `/api/Planner/CreateRunsheets` | JWT | Create runsheets from pool/car/trip data |
| `POST` | `/api/Planner/IntegrateRunsheets` | JWT | Integrate runsheets to TMS EasyGO |
| `POST` | `/api/PrambananRoutePlan/CreateRunsheets` | JWT | Create Prambanan runsheets (manual if `car_plate` set, else automatic) |
| `POST` | `/api/PrambananRoutePlan/UpdatePS` | JWT | Update PL/PS for sales orders |
| `POST` | `/api/PrambananRoutePlan/IntegrateRunsheets` | JWT | Integrate Prambanan runsheets to TMS EasyGO |

## Notes

- Docs (`/scalar`, `/openapi/v1.json`) are mapped only when `ASPNETCORE_ENVIRONMENT=Development`.
- Prefer the Scalar UI as the source of truth for request/response schemas; this README covers setup and high-level flow only.
