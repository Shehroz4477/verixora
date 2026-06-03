# VERIXORA ITERATION 0 – PROJECT FOUNDATION & SETUP

---

## ITERATION OBJECTIVE

Establish all project repositories, tooling, continuous integration, and the foundational architecture so that subsequent iterations can focus solely on feature delivery.

By the end of Iteration 0:

- Backend solution compiles with all module projects, SharedKernel, and BuildingBlocks.
- The architecture validation script passes.
- CI/CD pipeline builds, tests, and deploys an empty but functional shell.
- `/health` endpoint responds with database and broker status.
- Global rate limiting is active.
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
| DevOps / Lead     | CI/CD, validation script, repo  |

---

## PHASE 1: ARCHITECTURE & DESIGN

### 1.1 Backend

- Finalise modular monolith solution structure.
- Confirm all ADRs (001–022) and review against master spec.
- Define NuGet packages centrally in `Directory.Packages.props`.
- Design `Program.cs` startup pipeline: versioning, rate limiting, observability, health checks, Swagger.
- Design `SharedKernel.Domain` base classes and interfaces.
- Design `SharedKernel.Application` CQRS abstractions and pipeline behaviours.
- Design `BuildingBlocks.Infrastructure` cross-cutting services.

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
  - Rate limiting middleware (global: 100 req/min/user, 200 req/min/IP).
  - Serilog structured logging.
  - OpenTelemetry tracing.
  - Prometheus metrics endpoint.
  - Health checks (database, MQTT broker placeholder).
  - Swagger/OpenAPI.
  - Global exception handling middleware.
  - Controller registration from all module Presentation projects.
- `appsettings.json` and `appsettings.Development.json` with connection strings and Serilog config.
- `Properties/launchSettings.json` for local development profiles.

### 2.2 Backend – SharedKernel

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

### 2.3 Backend – BuildingBlocks

- `BuildingBlocks.Infrastructure`:
  - `Persistence/BaseDbContext.cs` – base EF Core context with domain event dispatching.
  - `Persistence/UnitOfWork.cs` – unit of work pattern.
  - `Logging/SerilogEnricher.cs` – enriches logs with TenantId, UserId, CorrelationId.
  - `Tracing/OpenTelemetryExtensions.cs` – OpenTelemetry setup helpers.
  - `HealthChecks/HealthCheckExtensions.cs` – health check registration helpers.
  - `RateLimiting/RateLimitingExtensions.cs` – rate limiting policy registration.
  - `Encryption/EncryptionOptions.cs` – AES encryption configuration.
  - `Encryption/AesEncryptionService.cs` – AES-256 encryption/decryption service.
  - `Idempotency/IdempotencyStore.cs` – stores idempotency keys and responses.
  - `Idempotency/IdempotencyMiddleware.cs` – idempotency check middleware.

### 2.4 Backend – All 13 Modules

Every module gets the same structural files:
- `{Module}.Domain.csproj` – references `SharedKernel.Domain`.
- `{Module}.Application.csproj` – references own Domain, `SharedKernel.Application`.
- `{Module}.Infrastructure.csproj` – references own Application, own Domain, `BuildingBlocks.Infrastructure`.
- `{Module}.Presentation.csproj` – references own Application.
- `{Module}.Contracts.csproj` – ZERO references.
- `GlobalUsings.cs` in every project.
- `DependencyInjection.cs` in Application, Infrastructure, and Presentation (extension method on `IServiceCollection`).
- `Persistence/{Module}DbContext.cs` in Infrastructure (inherits `BaseDbContext`).
- `Migrations/` folder in Infrastructure.
- `Controllers/` folder in Presentation (empty).

### 2.5 Backend – Architecture Validation Script

- `tests/ArchitectureValidation/ArchitectureValidation.csproj`.
- `tests/ArchitectureValidation/ArchitectureValidationTests.cs`:
  - Validates all 13 modules have 5 projects each.
  - Validates dependency rules per ADR.
  - Validates SharedKernel has no external NuGet packages.
  - Validates Contracts have no project references.
  - Validates ApiHost references all Presentation projects.
  - Validates ApiHost contains no Domain/Application code.

### 2.6 Backend – Integration Tests

- `tests/ApiHost.IntegrationTests/ApiHost.IntegrationTests.csproj`.
- `tests/ApiHost.IntegrationTests/HealthCheckTests.cs` – verifies `/health` returns 200.
- `tests/ApiHost.IntegrationTests/RateLimitingTests.cs` – verifies 429 on exceeding limits.

### 2.7 Mobile App

- Scaffold Ionic 7 + Angular 20 + Capacitor project.
- Create `LoginPage` component with basic UI shell.
- Create `HomePage` component as placeholder.
- Configure routing: `/login`, `/home`.
- Add Capacitor for native builds (Android/iOS).
- Add HTTP client service configured for `/api/v1/`.
- Add a test call to `/health` endpoint.

### 2.8 Web Portal

- Scaffold Angular 20 workspace.
- Create `LoginComponent` with basic Material UI shell.
- Create `DashboardComponent` as placeholder.
- Configure lazy-loaded routing.
- Apply Angular Material theme (consistent with mobile).
- Add HTTP interceptor for `/api/v1/` base URL.
- Add a test call to `/health` endpoint.

---

## PHASE 3: INTEGRATION

- Provision PostgreSQL database (local dev and CI).
- Run initial EF Core migrations (empty schemas per module).
- Mobile and Web: configure environment files pointing to backend dev URL.
- GitHub Actions CI pipeline:
  - On push:
    - Build backend (all 71 projects).
    - Run architecture validation script (fail on CRITICAL/HIGH).
    - Run unit tests.
    - Run integration tests.
    - Deploy to staging (if main branch).
  - On pull request: build + validation + unit tests only.

---

## PHASE 4: TESTING

### 4.1 Backend

- Solution builds with zero errors (all 71 projects).
- Architecture validation script passes.
- `/health` endpoint returns HTTP 200 with database status.
- Rate limiting returns 429 when burst exceeds 100 requests in 1 minute from same user.
- OpenTelemetry exports traces to console.

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

- Backend: Deploy to staging environment.
- CI/CD: Pipeline confirmed working with automatic staging deploy on merge to main.
- Mobile/Web: No deployment yet; builds stored as CI artifacts.

---

## ACCEPTANCE CRITERIA

- [ ] Backend solution compiles (71 projects, 0 errors).
- [ ] `/health` returns 200 with database status.
- [ ] Global rate limiting enforced (429 on exceed).
- [ ] Architecture validation script passes in CI.
- [ ] CI/CD pipeline deploys to staging.
- [ ] Mobile app scaffold runs on simulator/device.
- [ ] Web portal scaffold runs in browser.
- [ ] All projects follow Clean Architecture dependency rules.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement       | Iteration 0 Coverage                     |
| ----------------------------- | ---------------------------------------- |
| Modular Monolith Structure    | All 71 projects created                  |
| ADR-001 to ADR-014            | Enforced by validation script            |
| ADR-015 (API Versioning)      | `/api/v1/` prefix configured             |
| ADR-016 (Idempotency)         | IdempotencyStore and Middleware in place |
| ADR-017 (Encryption)          | AesEncryptionService in place            |
| ADR-020 (Rate Limiting)       | Global middleware active                 |
| ADR-021 (Observability)       | Serilog, Prometheus, OpenTelemetry       |
| Health Checks                 | `/health` endpoint                       |
| CI/CD Pipeline                | GitHub Actions workflow                  |
| Validation Script             | Integrated into CI                       |

---

## FULL BACKEND HIERARCHY

```
Verixora.sln
|
+-- .gitignore
+-- .editorconfig
+-- README.md
+-- Directory.Build.props
+-- Directory.Packages.props
+-- global.json
+-- nuget.config
|
+-- tests/
|   +-- ArchitectureValidation/
|   |   +-- ArchitectureValidation.csproj
|   |   +-- ArchitectureValidationTests.cs
|   +-- ApiHost.IntegrationTests/
|       +-- ApiHost.IntegrationTests.csproj
|       +-- HealthCheckTests.cs
|       +-- RateLimitingTests.cs
|
+-- src/
    |
    +-- ApiHost/
    |   +-- ApiHost.csproj
    |   +-- Program.cs
    |   +-- appsettings.json
    |   +-- appsettings.Development.json
    |   +-- Properties/
    |   |   +-- launchSettings.json
    |   +-- Controllers/
    |   |   +-- HealthController.cs
    |   +-- Middleware/
    |   |   +-- GlobalExceptionHandler.cs
    |   |   +-- RateLimitingMiddleware.cs
    |   +-- Extensions/
    |   |   +-- ServiceCollectionExtensions.cs
    |   |   +-- ApplicationBuilderExtensions.cs
    |   +-- Filters/
    |       +-- ApiExceptionFilter.cs
    |
    +-- Modules/
    |   |
    |   +-- Identity/
    |   |   +-- Identity.Domain/
    |   |   |   +-- Identity.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Identity.Application/
    |   |   |   +-- Identity.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Identity.Infrastructure/
    |   |   |   +-- Identity.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- IdentityDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Identity.Presentation/
    |   |   |   +-- Identity.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Identity.Contracts/
    |   |       +-- Identity.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Authorization/
    |   |   +-- Authorization.Domain/
    |   |   |   +-- Authorization.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Authorization.Application/
    |   |   |   +-- Authorization.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Authorization.Infrastructure/
    |   |   |   +-- Authorization.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- AuthorizationDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Authorization.Presentation/
    |   |   |   +-- Authorization.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Authorization.Contracts/
    |   |       +-- Authorization.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Sessions/
    |   |   +-- Sessions.Domain/
    |   |   |   +-- Sessions.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Sessions.Application/
    |   |   |   +-- Sessions.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Sessions.Infrastructure/
    |   |   |   +-- Sessions.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- SessionsDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Sessions.Presentation/
    |   |   |   +-- Sessions.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Sessions.Contracts/
    |   |       +-- Sessions.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Devices/
    |   |   +-- Devices.Domain/
    |   |   |   +-- Devices.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Devices.Application/
    |   |   |   +-- Devices.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Devices.Infrastructure/
    |   |   |   +-- Devices.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- DevicesDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Devices.Presentation/
    |   |   |   +-- Devices.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Devices.Contracts/
    |   |       +-- Devices.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Provisioning/
    |   |   +-- Provisioning.Domain/
    |   |   |   +-- Provisioning.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Provisioning.Application/
    |   |   |   +-- Provisioning.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Provisioning.Infrastructure/
    |   |   |   +-- Provisioning.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- ProvisioningDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Provisioning.Presentation/
    |   |   |   +-- Provisioning.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Provisioning.Contracts/
    |   |       +-- Provisioning.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- SmartLocks/
    |   |   +-- SmartLocks.Domain/
    |   |   |   +-- SmartLocks.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- SmartLocks.Application/
    |   |   |   +-- SmartLocks.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- SmartLocks.Infrastructure/
    |   |   |   +-- SmartLocks.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- SmartLocksDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- SmartLocks.Presentation/
    |   |   |   +-- SmartLocks.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- SmartLocks.Contracts/
    |   |       +-- SmartLocks.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Monitoring/
    |   |   +-- Monitoring.Domain/
    |   |   |   +-- Monitoring.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Monitoring.Application/
    |   |   |   +-- Monitoring.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Monitoring.Infrastructure/
    |   |   |   +-- Monitoring.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- MonitoringDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Monitoring.Presentation/
    |   |   |   +-- Monitoring.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Monitoring.Contracts/
    |   |       +-- Monitoring.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- AuditLogs/
    |   |   +-- AuditLogs.Domain/
    |   |   |   +-- AuditLogs.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- AuditLogs.Application/
    |   |   |   +-- AuditLogs.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- AuditLogs.Infrastructure/
    |   |   |   +-- AuditLogs.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- AuditLogsDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- AuditLogs.Presentation/
    |   |   |   +-- AuditLogs.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- AuditLogs.Contracts/
    |   |       +-- AuditLogs.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Notifications/
    |   |   +-- Notifications.Domain/
    |   |   |   +-- Notifications.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Notifications.Application/
    |   |   |   +-- Notifications.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Notifications.Infrastructure/
    |   |   |   +-- Notifications.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- NotificationsDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Notifications.Presentation/
    |   |   |   +-- Notifications.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Notifications.Contracts/
    |   |       +-- Notifications.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Reports/
    |   |   +-- Reports.Domain/
    |   |   |   +-- Reports.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Reports.Application/
    |   |   |   +-- Reports.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Reports.Infrastructure/
    |   |   |   +-- Reports.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- ReportsDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Reports.Presentation/
    |   |   |   +-- Reports.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Reports.Contracts/
    |   |       +-- Reports.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Automation/
    |   |   +-- Automation.Domain/
    |   |   |   +-- Automation.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Automation.Application/
    |   |   |   +-- Automation.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Automation.Infrastructure/
    |   |   |   +-- Automation.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- AutomationDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Automation.Presentation/
    |   |   |   +-- Automation.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Automation.Contracts/
    |   |       +-- Automation.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- Security/
    |   |   +-- Security.Domain/
    |   |   |   +-- Security.Domain.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   +-- Security.Application/
    |   |   |   +-- Security.Application.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   +-- Security.Infrastructure/
    |   |   |   +-- Security.Infrastructure.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Persistence/
    |   |   |   |   +-- SecurityDbContext.cs
    |   |   |   +-- Migrations/
    |   |   +-- Security.Presentation/
    |   |   |   +-- Security.Presentation.csproj
    |   |   |   +-- GlobalUsings.cs
    |   |   |   +-- DependencyInjection.cs
    |   |   |   +-- Controllers/
    |   |   +-- Security.Contracts/
    |   |       +-- Security.Contracts.csproj
    |   |       +-- GlobalUsings.cs
    |   |
    |   +-- FaceVerification/
    |       +-- FaceVerification.Domain/
    |       |   +-- FaceVerification.Domain.csproj
    |       |   +-- GlobalUsings.cs
    |       +-- FaceVerification.Application/
    |       |   +-- FaceVerification.Application.csproj
    |       |   +-- GlobalUsings.cs
    |       |   +-- DependencyInjection.cs
    |       +-- FaceVerification.Infrastructure/
    |       |   +-- FaceVerification.Infrastructure.csproj
    |       |   +-- GlobalUsings.cs
    |       |   +-- DependencyInjection.cs
    |       |   +-- Persistence/
    |       |   |   +-- FaceVerificationDbContext.cs
    |       |   +-- Migrations/
    |       +-- FaceVerification.Presentation/
    |       |   +-- FaceVerification.Presentation.csproj
    |       |   +-- GlobalUsings.cs
    |       |   +-- DependencyInjection.cs
    |       |   +-- Controllers/
    |       +-- FaceVerification.Contracts/
    |           +-- FaceVerification.Contracts.csproj
    |           +-- GlobalUsings.cs
    |
    +-- SharedKernel/
    |   +-- SharedKernel.Domain/
    |   |   +-- SharedKernel.Domain.csproj
    |   |   +-- GlobalUsings.cs
    |   |   +-- Base/
    |   |   |   +-- Entity.cs
    |   |   |   +-- ValueObject.cs
    |   |   |   +-- IAggregateRoot.cs
    |   |   |   +-- Enumeration.cs
    |   |   +-- Events/
    |   |   |   +-- IDomainEvent.cs
    |   |   +-- Guard/
    |   |   |   +-- Guard.cs
    |   |   +-- Results/
    |   |       +-- Result.cs
    |   |       +-- ResultT.cs
    |   +-- SharedKernel.Application/
    |       +-- SharedKernel.Application.csproj
    |       +-- GlobalUsings.cs
    |       +-- Abstractions/
    |       |   +-- ICommand.cs
    |       |   +-- IQuery.cs
    |       |   +-- ICommandHandler.cs
    |       |   +-- IQueryHandler.cs
    |       +-- Behaviours/
    |       |   +-- ValidationBehaviour.cs
    |       |   +-- LoggingBehaviour.cs
    |       +-- Exceptions/
    |           +-- ValidationException.cs
    |           +-- NotFoundException.cs
    |
    +-- BuildingBlocks/
        +-- BuildingBlocks.Infrastructure/
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
                +-- IdempotencyStore.cs
                +-- IdempotencyMiddleware.cs
```

---

## PROJECT SUMMARY

| Layer | Projects |
|-------|----------|
| ApiHost | 1 |
| Modules (13 x 5) | 65 |
| SharedKernel | 2 |
| BuildingBlocks | 1 |
| Tests | 2 |
| **Total** | **71** |

---

**ITERATION 0 DOCUMENT COMPLETE**