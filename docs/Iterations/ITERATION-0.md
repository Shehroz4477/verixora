# VERIXORA ITERATION 0 – PROJECT FOUNDATION & SETUP (FINAL VERSION)

---

## ITERATION OBJECTIVE

Establish all project repositories, tooling, continuous integration, and the foundational architecture so that subsequent iterations can focus solely on feature delivery.

By the end of Iteration 0:

- Backend solution compiles with all module projects, SharedKernel, and BuildingBlocks.
- Sessions module excluded (merged into Identity).
- Reports module contracts defined (minimum structure).
- Architecture validation script passes.
- CI/CD pipeline builds, tests, and deploys an empty but functional shell.
- `/health` endpoint responds with database and broker status.
- Global rate limiting is active.
- Graceful shutdown configured.
- Database retry resilience configured.
- Feature flag service registered.
- API documentation standard configured.
- Environment strategy defined with secrets management.
- Containerization with Docker and docker-compose.
- Shared TypeScript generation configured in CI.
- Mobile and Web Portal apps exist as scaffolded projects with login UI shells and routing.

---

## DURATION

1–2 weeks

---

## TEAM ALLOCATION

| Role              | Focus                           |
| ----------------- | ------------------------------- |
| Backend Developer | ApiHost, modules, CI/CD, infra  |
| Mobile Developer  | Ionic project, Capacitor, theme |
| Web Developer     | Angular workspace, Material     |
| DevOps / Lead     | CI/CD, validation script, Docker, secrets |

---

## PHASE 1: ARCHITECTURE & DESIGN

### 1.1 Backend

- Finalise modular monolith solution structure.
- Sessions module removed; session entities owned by Identity.
- Reports module contracts defined (IReportQuery interfaces, response DTOs).
- Confirm all ADRs (001–036).
- Define NuGet packages centrally in `Directory.Packages.props`.
- Design `Program.cs` startup pipeline: versioning, rate limiting, observability, health checks, Swagger, graceful shutdown, feature flags.
- Design environment strategy: Development, Testing, Staging, Production.
- Design secrets management: Azure Key Vault references, no secrets in files.
- Design Dockerfile and docker-compose.yml.
- Design TypeScript generation pipeline step.

### 1.2 Mobile App

- Ionic 7 + Angular 20 project structure with Capacitor.
- Define theme (Material Design), routing skeleton.
- Prepare for `/api/v1/` base URL configuration.
- Design `LoginPage` and `HomePage` shell components.

### 1.3 Web Portal

- Angular 20 workspace with Angular Material.
- Routing skeleton with lazy-loaded feature modules.
- Theming aligned with mobile app.
- Design `LoginComponent` and `DashboardComponent` shells.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Backend – ApiHost

- `Program.cs`:
  - API versioning with `/api/v1/` prefix.
  - Rate limiting middleware (global: 100 req/min/user, 200 req/min/IP, 500 req/min/API key).
  - Serilog structured logging.
  - OpenTelemetry tracing.
  - Prometheus metrics endpoint.
  - Health checks (database, MQTT broker placeholder).
  - Swagger/OpenAPI with XML comments.
  - Global exception handling middleware.
  - Graceful shutdown with CancellationToken support.
  - Feature flag service registration.
  - Secrets manager integration (Azure Key Vault).
  - Controller registration from all module Presentation projects.
- `appsettings.json` – non-secret configuration only.
- `appsettings.Development.json` – local dev settings.
- `appsettings.Testing.json` – CI test settings.
- `appsettings.Staging.json` – pre-production settings.
- `appsettings.Production.json` – production settings (no secrets).
- `Properties/launchSettings.json` for local development profiles.

### 2.2 Backend – Docker

- `Dockerfile` – multi-stage build for ApiHost.
- `docker-compose.yml` – ApiHost + PostgreSQL + Mosquitto MQTT broker.
- `.dockerignore` – exclude unnecessary files.

### 2.3 Backend – SharedKernel

- `SharedKernel.Domain`:
  - `Base/Entity.cs` – base entity with Id and domain events collection.
  - `Base/ValueObject.cs` – value object base with equality components.
  - `Base/IAggregateRoot.cs` – marker interface for aggregate roots.
  - `Base/Enumeration.cs` – enumeration base class.
  - `Events/IDomainEvent.cs` – domain event interface.
  - `Guard/Guard.cs` – argument validation helpers.
  - `Results/Result.cs` – operation result without value.
  - `Results/ResultT.cs` – operation result with value.
- `SharedKernel.Application`:
  - `Abstractions/ICommand.cs` – command marker.
  - `Abstractions/IQuery.cs` – query marker.
  - `Abstractions/ICommandHandler.cs` – command handler interface.
  - `Abstractions/IQueryHandler.cs` – query handler interface.
  - `Behaviours/ValidationBehaviour.cs` – MediatR validation pipeline.
  - `Behaviours/LoggingBehaviour.cs` – MediatR logging pipeline.
  - `Exceptions/ValidationException.cs` – custom validation exception.
  - `Exceptions/NotFoundException.cs` – custom not found exception.

### 2.4 Backend – BuildingBlocks

- `BuildingBlocks.Infrastructure`:
  - `Persistence/BaseDbContext.cs` – base EF Core context with domain event dispatching and EnableRetryOnFailure.
  - `Persistence/UnitOfWork.cs` – unit of work pattern.
  - `Logging/SerilogEnricher.cs` – enriches logs with TenantId, UserId, CorrelationId.
  - `Tracing/OpenTelemetryExtensions.cs` – OpenTelemetry setup helpers.
  - `HealthChecks/HealthCheckExtensions.cs` – health check registration helpers.
  - `RateLimiting/RateLimitingExtensions.cs` – rate limiting policy registration.
  - `Encryption/EncryptionOptions.cs` – AES encryption configuration.
  - `Encryption/AesEncryptionService.cs` – AES-256 encryption/decryption service.
  - `Idempotency/IdempotencyStore.cs` – stores idempotency keys and responses.
  - `Idempotency/IdempotencyMiddleware.cs` – idempotency check middleware.
  - `Backup/BackupService.cs` – backup service placeholder.
  - `Backup/BackupConfiguration.cs` – backup configuration.
  - `Backup/BackupJob.cs` – backup background job.
  - `FeatureFlags/FeatureFlagService.cs` – feature flag service.
  - `FeatureFlags/IFeatureFlagService.cs` – feature flag interface.
  - `Secrets/SecretsExtensions.cs` – secrets manager integration helpers.

### 2.5 Backend – All Modules

Every module gets the same structural files:
- `{Module}.Domain.csproj` – references `SharedKernel.Domain`.
- `{Module}.Application.csproj` – references own Domain, `SharedKernel.Application`.
- `{Module}.Infrastructure.csproj` – references own Application, own Domain, `BuildingBlocks.Infrastructure`.
- `{Module}.Presentation.csproj` – references own Application, XML documentation enabled.
- `{Module}.Contracts.csproj` – ZERO references, contains `IntegrationEvents/` folder.
- `GlobalUsings.cs` in every project.
- `DependencyInjection.cs` in Application, Infrastructure, and Presentation.
- `Persistence/{Module}DbContext.cs` in Infrastructure (inherits `BaseDbContext`).
- `Migrations/` folder in Infrastructure.
- `Controllers/` folder in Presentation (empty).

**Module list (13 modules):**
Identity, Authorization, Devices, Provisioning, SmartLocks, Monitoring, AuditLogs, Notifications, Reports, Automation, Security, FaceVerification
(Note: Sessions module removed, merged into Identity)

**Reports module minimum contracts:**
- `Reports.Contracts/IntegrationEvents/` (empty folder, placeholder)
- `Reports.Contracts/Responses/ReportResponse.cs` (placeholder)

### 2.6 Backend – Architecture Validation Script

- `tests/ArchitectureValidation/ArchitectureValidation.csproj`.
- `tests/ArchitectureValidation/ArchitectureValidationTests.cs`:
  - Validates all 13 modules have 5 projects each.
  - Validates Sessions module does NOT exist.
  - Validates dependency rules per ADR.
  - Validates SharedKernel has no external NuGet packages.
  - Validates Contracts have no project references and contain IntegrationEvents folder.
  - Validates ApiHost references all Presentation projects.
  - Validates ApiHost contains no Domain/Application code.
  - Validates XML documentation enabled on all Presentation projects.
  - Validates no secrets in appsettings files.
  - Validates Dockerfile exists.
  - Validates docker-compose.yml exists.

### 2.7 Backend – Integration Tests

- `tests/ApiHost.IntegrationTests/ApiHost.IntegrationTests.csproj`.
- `tests/ApiHost.IntegrationTests/HealthCheckTests.cs` – verifies `/health` returns 200.
- `tests/ApiHost.IntegrationTests/RateLimitingTests.cs` – verifies 429 on exceeding limits.
- `tests/ApiHost.IntegrationTests/GracefulShutdownTests.cs` – verifies pending operations complete.

### 2.8 Backend – Contract Tests

- `tests/ContractTests/ContractTests.csproj`.
- `tests/ContractTests/IntegrationEventContractTests.cs` – validates event schemas.

### 2.9 CI/CD – TypeScript Generation

- NSwag or Swagger Codegen step in CI pipeline.
- Generates TypeScript interfaces from Swagger JSON.
- Output placed in `generated/typescript/` folder.
- Mobile and Web projects reference generated types.

### 2.10 Mobile App

- Scaffold Ionic 7 + Angular 20 + Capacitor project.
- Create `LoginPage` component with basic UI shell.
- Create `HomePage` component as placeholder.
- Configure routing: `/login`, `/home`.
- Add Capacitor for native builds (Android/iOS).
- Add HTTP client service configured for `/api/v1/`.
- Add generated TypeScript models reference.
- Add a test call to `/health` endpoint.

### 2.11 Web Portal

- Scaffold Angular 20 workspace.
- Create `LoginComponent` with basic Material UI shell.
- Create `DashboardComponent` as placeholder.
- Configure lazy-loaded routing.
- Apply Angular Material theme (consistent with mobile).
- Add HTTP interceptor for `/api/v1/` base URL.
- Add generated TypeScript models reference.
- Add a test call to `/health` endpoint.

---

## PHASE 3: INTEGRATION

- Provision PostgreSQL database (local dev and CI).
- Run initial EF Core migrations (empty schemas per module).
- Set up Mosquitto MQTT broker via docker-compose.
- Mobile and Web: configure environment files pointing to backend dev URL.
- GitHub Actions CI pipeline:
  - On push:
    - Build backend (all projects).
    - Run architecture validation script (fail on CRITICAL/HIGH).
    - Run contract tests.
    - Run unit tests.
    - Run integration tests.
    - Generate TypeScript interfaces.
    - Build Docker image.
    - Deploy to staging (if main branch).
  - On pull request: build + validation + unit tests + contract tests only.
- Secrets injected at deployment time, never stored in repository.

---

## PHASE 4: TESTING

### 4.1 Backend

- Solution builds with zero errors.
- Architecture validation script passes (all 23 checks).
- Contract tests pass.
- `/health` endpoint returns HTTP 200 with database status.
- Rate limiting returns 429 when burst exceeds limits.
- Graceful shutdown completes pending operations.
- OpenTelemetry exports traces to console.
- Docker image builds and runs successfully.
- docker-compose up starts all services.

### 4.2 Mobile App

- App compiles for Android and iOS.
- Login page loads and calls `/health` successfully.
- Navigation between Login and Home works.

### 4.3 Web Portal

- App compiles.
- Login page loads and calls `/health` successfully.
- Navigation between Login and Dashboard works.

---

## PHASE 5: DEPLOYMENT

- Backend: Deploy Docker image to staging environment.
- CI/CD: Pipeline confirmed working with automatic staging deploy on merge to main.
- Mobile/Web: No deployment yet; builds stored as CI artifacts.

---

## ACCEPTANCE CRITERIA

- [ ] Backend solution compiles (0 errors).
- [ ] All 13 modules have 5 projects each.
- [ ] Sessions module does NOT exist.
- [ ] `/health` returns 200 with database status.
- [ ] Global rate limiting enforced (429 on exceed).
- [ ] Architecture validation script passes in CI (all 23 checks).
- [ ] Contract tests pass.
- [ ] CI/CD pipeline deploys to staging.
- [ ] Docker image builds successfully.
- [ ] docker-compose up runs all services locally.
- [ ] No secrets in any configuration files.
- [ ] TypeScript interfaces generated in CI.
- [ ] Mobile app scaffold runs on simulator/device.
- [ ] Web portal scaffold runs in browser.
- [ ] All projects follow Clean Architecture dependency rules.
- [ ] XML documentation enabled on all Presentation projects.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 0 Coverage                     |
| ---------------------------------- | ---------------------------------------- |
| Modular Monolith Structure         | 13 modules, 71 projects                  |
| Sessions merged into Identity      | Sessions module absent                   |
| Reports module contracts           | Minimum structure in place               |
| ADR-001 to ADR-036                 | Enforced by validation script            |
| ADR-015 (API Versioning)           | `/api/v1/` prefix configured             |
| ADR-016 (Idempotency)              | IdempotencyStore and Middleware in place |
| ADR-017 (Encryption)               | AesEncryptionService in place            |
| ADR-020 (Rate Limiting)            | Global + API key middleware active       |
| ADR-021 (Observability)            | Serilog, Prometheus, OpenTelemetry       |
| ADR-027 (Graceful Shutdown)        | IHostedService + CancellationToken       |
| ADR-028 (Migration Strategy)       | Expand-contract documented               |
| ADR-029 (Feature Flags)            | IFeatureFlagService registered           |
| ADR-030 (Environment Strategy)     | 4 appsettings files                      |
| ADR-031 (Secrets Management)       | Key Vault integration                    |
| ADR-032 (Containerization)         | Dockerfile + docker-compose              |
| ADR-033 (DB Resilience)            | EnableRetryOnFailure configured          |
| ADR-034 (API Documentation)        | XML comments + Swagger                   |
| ADR-035 (TypeScript Generation)    | NSwag in CI                              |
| ADR-036 (Module Versioning)        | CHANGELOG placeholder                    |
| Health Checks                      | `/health` endpoint                       |
| CI/CD Pipeline                     | GitHub Actions workflow                  |
| Validation Script                  | Integrated into CI (23 checks)           |
| Contract Tests                     | Integrated into CI                       |

---

## FULL BACKEND HIERARCHY (COMPLETE)

### Solution Root
```
Verixora.sln
.gitignore
.editorconfig
README.md
Directory.Build.props
Directory.Packages.props
global.json
nuget.config
Dockerfile
.dockerignore
docker-compose.yml
```

### Tests
```
tests/
+-- ArchitectureValidation/
|   +-- ArchitectureValidation.csproj
|   +-- ArchitectureValidationTests.cs
+-- ApiHost.IntegrationTests/
|   +-- ApiHost.IntegrationTests.csproj
|   +-- HealthCheckTests.cs
|   +-- RateLimitingTests.cs
|   +-- GracefulShutdownTests.cs
+-- ContractTests/
    +-- ContractTests.csproj
    +-- IntegrationEventContractTests.cs
```

### ApiHost
```
src/ApiHost/
+-- ApiHost.csproj
+-- Program.cs
+-- appsettings.json
+-- appsettings.Development.json
+-- appsettings.Testing.json
+-- appsettings.Staging.json
+-- appsettings.Production.json
+-- Properties/
|   +-- launchSettings.json
+-- Controllers/
|   +-- HealthController.cs
+-- Middleware/
|   +-- GlobalExceptionHandler.cs
|   +-- RateLimitingMiddleware.cs
|   +-- IdempotencyMiddleware.cs
+-- Extensions/
|   +-- ServiceCollectionExtensions.cs
|   +-- ApplicationBuilderExtensions.cs
+-- Filters/
    +-- ApiExceptionFilter.cs
```

### SharedKernel
```
src/SharedKernel/
+-- SharedKernel.Domain/
|   +-- SharedKernel.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Base/
|   |   +-- Entity.cs
|   |   +-- ValueObject.cs
|   |   +-- IAggregateRoot.cs
|   |   +-- Enumeration.cs
|   +-- Events/
|   |   +-- IDomainEvent.cs
|   +-- Guard/
|   |   +-- Guard.cs
|   +-- Results/
|       +-- Result.cs
|       +-- ResultT.cs
+-- SharedKernel.Application/
    +-- SharedKernel.Application.csproj
    +-- GlobalUsings.cs
    +-- Abstractions/
    |   +-- ICommand.cs
    |   +-- IQuery.cs
    |   +-- ICommandHandler.cs
    |   +-- IQueryHandler.cs
    +-- Behaviours/
    |   +-- ValidationBehaviour.cs
    |   +-- LoggingBehaviour.cs
    +-- Exceptions/
        +-- ValidationException.cs
        +-- NotFoundException.cs
```

### BuildingBlocks
```
src/BuildingBlocks/BuildingBlocks.Infrastructure/
+-- BuildingBlocks.Infrastructure.csproj
+-- GlobalUsings.cs
+-- Persistence/
|   +-- BaseDbContext.cs
|   +-- UnitOfWork.cs
+-- Logging/
|   +-- SerilogEnricher.cs
+-- Tracing/
|   +-- OpenTelemetryExtensions.cs
+-- HealthChecks/
|   +-- HealthCheckExtensions.cs
+-- RateLimiting/
|   +-- RateLimitingExtensions.cs
+-- Encryption/
|   +-- EncryptionOptions.cs
|   +-- AesEncryptionService.cs
+-- Idempotency/
|   +-- IdempotencyStore.cs
|   +-- IdempotencyMiddleware.cs
+-- Backup/
|   +-- BackupService.cs
|   +-- BackupConfiguration.cs
|   +-- BackupJob.cs
+-- FeatureFlags/
|   +-- IFeatureFlagService.cs
|   +-- FeatureFlagService.cs
+-- Secrets/
    +-- SecretsExtensions.cs
```

### Generated TypeScript
```
generated/
+-- typescript/
    +-- verixora-models.ts
```

### Module: Identity
```
src/Modules/Identity/
+-- Identity.Domain/
|   +-- Identity.Domain.csproj
|   +-- GlobalUsings.cs
+-- Identity.Application/
|   +-- Identity.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Identity.Infrastructure/
|   +-- Identity.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- IdentityDbContext.cs
|   +-- Migrations/
+-- Identity.Presentation/
|   +-- Identity.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Identity.Contracts/
    +-- Identity.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Authorization
```
src/Modules/Authorization/
+-- Authorization.Domain/
|   +-- Authorization.Domain.csproj
|   +-- GlobalUsings.cs
+-- Authorization.Application/
|   +-- Authorization.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Authorization.Infrastructure/
|   +-- Authorization.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AuthorizationDbContext.cs
|   +-- Migrations/
+-- Authorization.Presentation/
|   +-- Authorization.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Authorization.Contracts/
    +-- Authorization.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Devices
```
src/Modules/Devices/
+-- Devices.Domain/
|   +-- Devices.Domain.csproj
|   +-- GlobalUsings.cs
+-- Devices.Application/
|   +-- Devices.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Devices.Infrastructure/
|   +-- Devices.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- DevicesDbContext.cs
|   +-- Migrations/
+-- Devices.Presentation/
|   +-- Devices.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Devices.Contracts/
    +-- Devices.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Provisioning
```
src/Modules/Provisioning/
+-- Provisioning.Domain/
|   +-- Provisioning.Domain.csproj
|   +-- GlobalUsings.cs
+-- Provisioning.Application/
|   +-- Provisioning.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Provisioning.Infrastructure/
|   +-- Provisioning.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- ProvisioningDbContext.cs
|   +-- Migrations/
+-- Provisioning.Presentation/
|   +-- Provisioning.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Provisioning.Contracts/
    +-- Provisioning.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: SmartLocks
```
src/Modules/SmartLocks/
+-- SmartLocks.Domain/
|   +-- SmartLocks.Domain.csproj
|   +-- GlobalUsings.cs
+-- SmartLocks.Application/
|   +-- SmartLocks.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- SmartLocks.Infrastructure/
|   +-- SmartLocks.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- SmartLocksDbContext.cs
|   +-- Migrations/
+-- SmartLocks.Presentation/
|   +-- SmartLocks.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- SmartLocks.Contracts/
    +-- SmartLocks.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Monitoring
```
src/Modules/Monitoring/
+-- Monitoring.Domain/
|   +-- Monitoring.Domain.csproj
|   +-- GlobalUsings.cs
+-- Monitoring.Application/
|   +-- Monitoring.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Monitoring.Infrastructure/
|   +-- Monitoring.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- MonitoringDbContext.cs
|   +-- Migrations/
+-- Monitoring.Presentation/
|   +-- Monitoring.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Monitoring.Contracts/
    +-- Monitoring.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: AuditLogs
```
src/Modules/AuditLogs/
+-- AuditLogs.Domain/
|   +-- AuditLogs.Domain.csproj
|   +-- GlobalUsings.cs
+-- AuditLogs.Application/
|   +-- AuditLogs.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- AuditLogs.Infrastructure/
|   +-- AuditLogs.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AuditLogsDbContext.cs
|   +-- Migrations/
+-- AuditLogs.Presentation/
|   +-- AuditLogs.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- AuditLogs.Contracts/
    +-- AuditLogs.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Notifications
```
src/Modules/Notifications/
+-- Notifications.Domain/
|   +-- Notifications.Domain.csproj
|   +-- GlobalUsings.cs
+-- Notifications.Application/
|   +-- Notifications.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Notifications.Infrastructure/
|   +-- Notifications.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- NotificationsDbContext.cs
|   +-- Migrations/
+-- Notifications.Presentation/
|   +-- Notifications.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Notifications.Contracts/
    +-- Notifications.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Reports
```
src/Modules/Reports/
+-- Reports.Domain/
|   +-- Reports.Domain.csproj
|   +-- GlobalUsings.cs
+-- Reports.Application/
|   +-- Reports.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Interfaces/
|       +-- IReportQuery.cs
+-- Reports.Infrastructure/
|   +-- Reports.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- ReportsDbContext.cs
|   +-- Migrations/
+-- Reports.Presentation/
|   +-- Reports.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Reports.Contracts/
    +-- Reports.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- Responses/
    |   +-- ReportResponse.cs
    +-- CHANGELOG.md
```

### Module: Automation
```
src/Modules/Automation/
+-- Automation.Domain/
|   +-- Automation.Domain.csproj
|   +-- GlobalUsings.cs
+-- Automation.Application/
|   +-- Automation.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Automation.Infrastructure/
|   +-- Automation.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AutomationDbContext.cs
|   +-- Migrations/
+-- Automation.Presentation/
|   +-- Automation.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Automation.Contracts/
    +-- Automation.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: Security
```
src/Modules/Security/
+-- Security.Domain/
|   +-- Security.Domain.csproj
|   +-- GlobalUsings.cs
+-- Security.Application/
|   +-- Security.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- Security.Infrastructure/
|   +-- Security.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- SecurityDbContext.cs
|   +-- Migrations/
+-- Security.Presentation/
|   +-- Security.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Security.Contracts/
    +-- Security.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

### Module: FaceVerification
```
src/Modules/FaceVerification/
+-- FaceVerification.Domain/
|   +-- FaceVerification.Domain.csproj
|   +-- GlobalUsings.cs
+-- FaceVerification.Application/
|   +-- FaceVerification.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
+-- FaceVerification.Infrastructure/
|   +-- FaceVerification.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- FaceVerificationDbContext.cs
|   +-- Migrations/
+-- FaceVerification.Presentation/
|   +-- FaceVerification.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- FaceVerification.Contracts/
    +-- FaceVerification.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- CHANGELOG.md
```

---

## PROJECT COUNT

| Category | Count |
|----------|-------|
| Solution Files | 13 |
| Test Projects | 3 |
| ApiHost | 1 |
| SharedKernel | 2 |
| BuildingBlocks | 1 |
| Modules (13 x 5) | 65 |
| Generated | 1 |
| **Total** | **86** |

---

**ITERATION 0 UPDATED.**

All 10 improvements integrated:
- Reports module minimum contracts
- Graceful shutdown
- Database migration strategy
- Feature flags
- API documentation standard
- Environment strategy (4 configs)
- Secrets management
- Containerization (Docker + docker-compose)
- Database connection resilience
- TypeScript generation in CI
