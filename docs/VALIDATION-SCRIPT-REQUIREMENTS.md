# FINAL VERIXORA VALIDATION SCRIPT (ENHANCED - ASCII SAFE)

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
- Contracts project

If missing -> CRITICAL failure.

---

## 8. ENHANCED ARCHITECTURAL CHECKS (NEW)

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

DOCUMENT VERSION: Final - Enhanced
LAST UPDATED: 2026-06-02