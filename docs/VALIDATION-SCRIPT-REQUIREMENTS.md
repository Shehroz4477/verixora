# FINAL VERIXORA VALIDATION SCRIPT (FINAL VERSION)

---

# PURPOSE

The VERIXORA Validation Script enforces architectural integrity across the entire solution.

It ensures:
- module isolation
- dependency correctness
- SharedKernel purity
- contract safety
- structural consistency
- adherence to enhanced security and operational ADRs
- integration event contract validity
- API documentation completeness

---

# EXECUTION RULES

- Script must run in CI pipeline
- Script failure blocks build
- Script output must be deterministic

---

# SEVERITY LEVELS

Each violation must be categorized:
- CRITICAL -> build fails immediately
- HIGH -> build fails
- MEDIUM -> warning only
- INFO -> reporting only

---

# REQUIRED CHECKS

## 1. PROJECT STRUCTURE VALIDATION

- All expected module projects exist.
- Each module contains:
  - Domain
  - Application
  - Infrastructure
  - Presentation
  - Contracts
- Sessions module must NOT exist as a separate module (merged into Identity).

---

## 2. DEPENDENCY RULE VALIDATION

### Domain Layer Rules:
- Domain can only reference SharedKernel.Domain.
- Domain must NOT reference any infrastructure or application layer.

### Application Layer Rules:
- Can reference:
  - its own Domain
  - SharedKernel.Application

### Infrastructure Layer Rules:
- Can reference:
  - its own Application
  - its own Domain
  - BuildingBlocks.Infrastructure

### Presentation Layer Rules:
- Can reference:
  - its own Application
  - Infrastructure (only via DI composition)

### Contracts Rules:
- Must have ZERO project references.
- Must contain IntegrationEvents folder.

---

## 3. SHARED KERNEL PROTECTION

- No external project can reference SharedKernel.Domain internals.
- SharedKernel must not depend on any module.
- No NuGet dependencies allowed in SharedKernel.Domain.

---

## 4. API HOST VALIDATION

- ApiHost must reference all Presentation projects.
- ApiHost must reference BuildingBlocks.Infrastructure.
- ApiHost must NOT contain business logic.
- Graceful shutdown must be configured in Program.cs.

---

## 5. CIRCULAR DEPENDENCY DETECTION

Script must detect:
- direct cycles
- indirect cycles
- transitive dependency loops

---

## 6. DOMAIN INTEGRITY VALIDATION

- Domain layer must contain ONLY:
  - entities
  - value objects
  - domain services
  - domain events

RULE:
No infrastructure code allowed inside Domain.

---

## 7. MODULE COMPLETENESS VALIDATION

Each module must contain:
- Domain project
- Application project
- Infrastructure project
- Presentation project
- Contracts project (with IntegrationEvents folder)

If missing -> CRITICAL failure.

---

## 8. ENHANCED ARCHITECTURAL CHECKS

### 8.1 Idempotency Decorator Validation
- All SmartLock command handlers (Unlock, Lock, EmergencyLock) must be decorated with an idempotency wrapper.
- Check for Idempotency-Key header handling in the presentation layer endpoints.
- SEVERITY: HIGH

### 8.2 Health Check Registration
- /health endpoint must be registered in ApiHost startup.
- It must report database, MQTT broker, and per-module health.
- SEVERITY: CRITICAL

### 8.3 Rate Limiting Middleware
- Rate-limiting middleware must be configured in ApiHost.
- Unlock-specific burst policy must be present (5 requests per 10 seconds).
- API key rate limit policy must be present (500 requests/min).
- SEVERITY: HIGH

### 8.4 Column-Level Encryption
- PII entities (User email, phone, face embeddings) and audit logs must use EF Core value converters with AES-256 encryption.
- Verify that converters are applied in module DbContext configurations.
- SEVERITY: HIGH

### 8.5 MQTT Token Service Registration
- MQTT token generation service must be registered in the Devices module infrastructure layer.
- Token lifetime must be 1 hour.
- SEVERITY: HIGH

### 8.6 Authorization Caching Decorator
- Authorization service in the Authorization module must be wrapped with a caching decorator.
- Cache invalidation must be triggered on RolePermissionChanged domain event.
- API key evaluations must NOT be cached.
- SEVERITY: HIGH

### 8.7 Signed Firmware Update Handling
- Firmware update handler in Devices.Application must verify signature before queuing the update.
- Check for cryptographic signature validation logic.
- SEVERITY: CRITICAL

### 8.8 Observability Infrastructure
- Serilog must be configured in ApiHost with TenantId, UserId, CorrelationId enrichment.
- Prometheus metrics endpoint must be exposed.
- OpenTelemetry tracing must be enabled for the unlock pipeline.
- SEVERITY: MEDIUM

### 8.9 API Versioning
- All controllers must be prefixed with /api/v1/.
- Versioning middleware must be active.
- SEVERITY: MEDIUM

### 8.10 Configurable Device Limit
- Home entity in Identity.Domain must have a configurable MaxDevices field (default 20).
- Validation must be applied on device registration.
- SEVERITY: MEDIUM

### 8.11 API Key Authentication
- ApiKey entity must exist in Identity.Domain.
- API keys must be stored hashed (SHA-256).
- ApiKeyAuthenticationHandler must be registered.
- SEVERITY: HIGH

### 8.12 Audit Log Retention
- AuditLogRetentionJob must be registered as a background service.
- Retention period must be 90 days.
- Archive destination must be configured.
- SEVERITY: HIGH

### 8.13 Secrets Management
- No connection strings or keys in appsettings.Production.json.
- Secrets must be referenced via key name from secrets manager.
- SEVERITY: CRITICAL

### 8.14 Graceful Shutdown
- All background services must implement IHostedService.
- CancellationToken must be respected.
- SEVERITY: HIGH

### 8.15 Database Connection Resilience
- EnableRetryOnFailure must be configured in BaseDbContext.
- Retry count: minimum 3.
- SEVERITY: HIGH

### 8.16 Feature Flag Service
- IFeatureFlagService must be registered in DI.
- Feature flag checks must be present on all post-MVP features.
- SEVERITY: MEDIUM

### 8.17 API Documentation
- XML documentation generation enabled on all Presentation projects.
- Swagger configured to include XML comments.
- SEVERITY: MEDIUM

### 8.18 Containerization
- Dockerfile must exist in solution root.
- docker-compose.yml must exist for local development.
- SEVERITY: MEDIUM

### 8.19 Environment Configuration
- appsettings files must exist for Development, Testing, Staging, Production.
- No secrets in any appsettings file.
- SEVERITY: HIGH

### 8.20 Contract Tests
- Contract test project must exist.
- Integration event contracts must match published events.
- Breaking changes to contracts must fail the build.
- SEVERITY: HIGH

### 8.21 Device Decommissioning
- DecommissionDevice command must exist in Devices.Application.
- Must revoke MQTT tokens.
- Must mark device status as Decommissioned.
- SEVERITY: HIGH

### 8.22 Offline Unlock Prohibition
- No offline unlock logic present in mobile app or backend.
- Unlock always requires backend connectivity.
- SEVERITY: CRITICAL

### 8.23 Suspicious Activity Detection
- SuspiciousActivityDetector service must be registered.
- Detection rules must be configurable.
- Alerts must be raised for threshold breaches.
- SEVERITY: MEDIUM

---

## 9. OUTPUT FORMAT

Script output must include:
- Summary (pass/fail)
- Table of violations
- Severity classification
- File/module location
- Expected vs actual result
- Fix suggestion

---

## 10. CI/CD BEHAVIOR

- CRITICAL or HIGH failure -> pipeline fails
- MEDIUM -> warning in logs
- INFO -> report only

---

**DOCUMENT VERSION: Final**
**LAST UPDATED: 2026-06-03**

---

**FINAL VERIXORA VALIDATION SCRIPT UPDATED.**

New checks added:
- 8.11: API Key Authentication
- 8.12: Audit Log Retention
- 8.13: Secrets Management
- 8.14: Graceful Shutdown
- 8.15: Database Connection Resilience
- 8.16: Feature Flag Service
- 8.17: API Documentation
- 8.18: Containerization
- 8.19: Environment Configuration
- 8.20: Contract Tests
- 8.21: Device Decommissioning
- 8.22: Offline Unlock Prohibition
- 8.23: Suspicious Activity Detection
- IntegrationEvents folder check in Contracts
- Sessions module absence check

Total checks: 10 -> 23
