# VibeCoding Visual Phishing Training Backend

This repository contains the .NET 9 backend for the Visual Phishing Training experience ("Real or Fake?"). It exposes secured APIs that power the React front-end, manages training content, records learner attempts, and orchestrates media/telemetry pipelines.

## Features
- ASP.NET Core 9 API with JWT auth + Identity (Admin/Trainer/Learner roles)
- EF Core/PostgreSQL persistence with Azure Blob Storage media integration
- Redis-backed rate limiting and distributed locks
- Scenario CRUD + publish workflow with background thumbnails
- Import batches for bulk scenario onboarding (with background jobs)
- Signed upload/download URLs for media assets
- Learner attempt recording, scoring, and privacy-safe telemetry events
- Background processing pipeline via hosted worker + channel queue
- Dockerfile and docker-compose for local orchestration (API + Postgres + Redis + Azurite)
- xUnit-based unit tests covering core services

## Getting Started

### Prerequisites
- .NET 9 SDK
- Docker (for containerised setup)
- Access to PostgreSQL, Redis, and Azure Blob Storage (or Azurite)

### Local Development
1. Restore and build:
   `bash
   dotnet build
   `
2. Apply database migrations (add new ones as needed):
   `bash
   dotnet ef migrations add InitialCreate -p VibeCoding.Api -s VibeCoding.Api
   dotnet ef database update -p VibeCoding.Api -s VibeCoding.Api
   `
3. Run the API:
   `bash
   dotnet run --project VibeCoding.Api
   `
4. OpenAPI docs are available at /openapi/v1.json (development only).

### Running with Docker Compose
`bash
docker compose up --build
`
This starts the API at http://localhost:8080 along with PostgreSQL, Redis, and Azurite. Default credentials:
- Admin user: dmin@local.test / Passw0rd!

### Configuration
All sensitive values are provided via environment variables. Key settings include:
- Postgres__ConnectionString
- Redis__ConnectionString
- AzureBlob__ConnectionString
- Jwt__SecretKey
- Telemetry__UserHashSalt
- Admin__Email / Admin__Password

Use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) during development to avoid committing secrets.

### Testing
`bash
dotnet test --no-build
`

## Project Structure
- VibeCoding.Api/ – primary API project (controllers, services, infrastructure)
- VibeCoding.Tests/ – unit tests for service layer
- docs/architecture.md – high-level architecture overview
- Dockerfile, docker-compose.yml – container build assets

## Security Notes
- Thumbnails leverage ImageSharp (currently emits vulnerability advisories; monitor and upgrade when patched).
- JWT secrets and admin credentials in configuration are placeholders—replace for any real deployment.

## Next Steps
- Add integration tests for the import workflow
- Wire Application Insights (or similar) when telemetry instrumentation key is provided
- Harden rate limiting rules per endpoint as usage patterns emerge
