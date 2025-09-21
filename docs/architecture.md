# Visual Phishing Training Backend

## High-Level Overview
- **API**: ASP.NET Core 8 minimal API hosting layered services.
- **Persistence**: PostgreSQL via EF Core with Identity for RBAC.
- **Caching & Locks**: Redis for distributed rate limiting and job coordination.
- **Object Storage**: Azure Blob Storage for media assets and signed URL delivery.
- **Background Processing**: Hosted worker + channel-backed queue for imports and thumbnails.
- **Telemetry**: Aggregates anonymised signals, never stores raw PII.

## Core Domain
- `ApplicationUser`/`ApplicationRole`: Identity entities with GUID keys.
- `TrainingScenario`: Draft/published phishing visuals with metadata and linked media.
- `MediaAsset`: Blob-backed file descriptors (original + generated thumbnail).
- `ScenarioAttempt`: Records learner choices, scoring, and timing metrics.
- `ImportBatch`: Tracks admin-driven content imports with processing status.
- `TelemetryEvent`: Stores privacy-safe JSON payloads hashed on UserId.

## Services
- Scenario service handles CRUD, publishing workflow, tagging, and attempt counts.
- Media service orchestrates Azure Blob upload tokens, verifies MIME, schedules thumbnail job.
- Attempt service validates answers, computes score, raises telemetry events.
- Import service ingests admin-provided manifests/json, enqueues background job to build scenarios.
- Telemetry service batches events and pushes to sink (db + optional Application Insights).

## Infrastructure
- `ApplicationDbContext` extends `IdentityDbContext` with schema config + concurrency tokens.
- Redis-backed rate limiter implements sliding window per-IP/user.
- Distributed lock helper for import/thumbnails to avoid duplicate processing.
- `BackgroundJobQueue` + hosted worker handle job execution with retry policies.
- Azure Blob SAS helper centralises signing plus stored policy management.

## API Surface (summary)
- `POST /auth/register`, `POST /auth/token`
- `GET|POST|PUT|DELETE /scenarios`
- `POST /scenarios/{id}/publish`
- `GET /scenarios/{id}/media-url`
- `POST /scenarios/{id}/attempts`
- `GET /attempts/mine`
- `POST /admin/imports`
- `GET /admin/imports`
- `POST /admin/imports/{id}/start`
- `POST /telemetry`

## Testing & Tooling
- xUnit + Moq for services and controllers.
- Test containers for Postgres/Redis (disabled by default, uses local connection override).
- Dockerfile publishes trimmed image; docker-compose includes api, postgres, redis, azurite.

## Non-Functional Requirements
- Secrets via environment variables; never stored in repo.
- Strict DTO validation with FluentValidation.
- Comprehensive OpenAPI doc + security scheme for JWT.
- GDPR-friendly telemetry with salted SHA256 user hashes.
