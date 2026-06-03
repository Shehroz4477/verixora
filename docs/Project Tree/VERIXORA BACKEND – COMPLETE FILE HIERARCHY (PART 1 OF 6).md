# VERIXORA BACKEND – COMPLETE FILE HIERARCHY (PART 1 OF 6)

---

## SOLUTION ROOT

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

---

## TESTS

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

---

## API HOST

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

---

## SHARED KERNEL

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

---

## BUILDING BLOCKS

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

---

## GENERATED TYPESCRIPT

```
generated/
+-- typescript/
    +-- verixora-models.ts
```

---

## INFRASTRUCTURE

```
infrastructure/
+-- alerting/
|   +-- alertmanager.yml
|   +-- rules/
|       +-- latency.yml
|       +-- errors.yml
|       +-- devices.yml
|       +-- security.yml
+-- grafana/
|   +-- dashboards/
|       +-- verixora-sla-dashboard.json
|       +-- verixora-device-dashboard.json
|       +-- verixora-security-dashboard.json
+-- kubernetes/
|   +-- namespace.yaml
|   +-- deployment.yaml
|   +-- service.yaml
|   +-- ingress.yaml
|   +-- configmap.yaml
|   +-- secrets-provider.yaml
+-- docker/
    +-- Dockerfile
    +-- .dockerignore
```

---

## LOAD TESTS

```
tests/LoadTests/
+-- k6/
    +-- unlock-pipeline-load.js
    +-- rate-limiting-load.js
```

---

## DEVICE SIMULATOR

```
tools/DeviceSimulator/
+-- DeviceSimulator.csproj
+-- Program.cs
+-- MqttClientService.cs
+-- ProvisioningSimulator.cs
+-- appsettings.json
+-- Dockerfile
```

---

## DOCUMENTATION

```
docs/
+-- incident-response-runbook.md
```