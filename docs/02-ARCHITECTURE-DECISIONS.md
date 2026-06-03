# ARCHITECTURAL DECISION (FINAL VERSION)

---

# ADR SYSTEM OVERVIEW

This document defines all architectural decisions for VERIXORA.

All system behavior must follow these decisions.

If any conflict exists:
System must follow ADR priority rules defined below.

---

# ADR-001: MODULAR MONOLITH ARCHITECTURE

VERIXORA will be built as a Modular Monolith.

REASONS:
- single deployment unit
- strong module isolation
- reduced operational complexity
- easier testing and debugging

RULE:
Modules must never be split into independent services at this stage.

---

# ADR-002: CLEAN ARCHITECTURE INSIDE MODULES

Each module must follow:
- Domain Layer
- Application Layer
- Infrastructure Layer
- Presentation Layer
- Contracts Layer (includes IntegrationEvents)

RULE:
Domain must never depend on infrastructure.

---

# ADR-003: BACKEND IS SYSTEM OF RECORD

Backend is the only authority for all decisions.

RULES:
- devices cannot decide access
- frontend cannot override backend decisions
- IoT devices are passive executors

---

# ADR-004: PASSIVE DEVICE MODEL

IoT devices (ESP32) must:
- execute backend commands only
- send telemetry only
- never make authorization decisions

FAILURE MODEL:
- devices may disconnect
- commands must be idempotent
- retry must be handled by backend

---

# ADR-005: MQTT IS PRIMARY IOT PROTOCOL

MQTT will be used for:
- device commands
- telemetry
- heartbeat signals

RULE:
MQTT messages must be stateless and replay-safe.

---

# ADR-006: BLE IS ONLY FOR PROVISIONING

BLE is strictly used for:
- device onboarding
- WiFi credential transfer

RULE:
BLE cannot be used for operational commands.

---

# ADR-007: SIGNALR IS FOR REAL-TIME UI ONLY

SignalR is used only for:
- dashboard updates
- live monitoring
- notifications to web/mobile

RULE:
SignalR must NOT control devices directly.

---

# ADR-008: SCHEMA-PER-MODULE DATABASE DESIGN

Each module owns its schema.

RULES:
- one database initially
- logical separation per module
- no cross-module table sharing

---

# ADR-009: EVENT-DRIVEN ARCHITECTURE

System uses:
- Domain Events (inside module)
- Integration Events (cross module)

IMPLEMENTATION:
- Domain Events: in-memory mediator (MediatR)
- Integration Events: PostgreSQL LISTEN/NOTIFY initially
- Each module's Contracts project includes IntegrationEvents folder

RULE:
Modules communicate only via events or contracts.

---

# ADR-010: AUDIT LOG IMMUTABILITY

Audit logs are:
- append-only
- immutable
- never updated or deleted

RULE:
All security-sensitive actions must generate audit entries.

---

# ADR-011: FACE VERIFICATION IS PLUGGABLE

Face verification must be abstracted behind interface.

RULES:
- provider can be mocked or real
- no direct dependency on ML model
- must be optional per door policy

---

# ADR-012: SECURITY FIRST PRIORITY MODEL

Security overrides:
- usability
- performance
- automation convenience

RULE:
If conflict exists, security decision wins.

---

# ADR-013: SYSTEM BEHAVIOR INVARIANTS

The system must always guarantee:
- no device acts without backend approval
- no cross-home data access
- no bypass of authentication
- all actions are traceable

---

# ADR-014: NO DISTRIBUTED MICROSERVICES AT THIS STAGE

Microservices are explicitly NOT allowed.

REASON:
- unnecessary complexity
- premature scaling risk

Future migration allowed only after stable MVP.

---

# ADR-015: API VERSIONING

All REST endpoints must be versioned via URL path (/api/v1/).

RULES:
- breaking changes require a new version (e.g., /api/v2/)
- old versions must coexist for at least one release cycle
- enforced via controller attributes

REASON:
Mobile apps cannot be force-updated; versioning prevents breaking end-user access.

---

# ADR-016: IDEMPOTENCY FOR DEVICE COMMANDS

All state-changing device commands require a client-supplied Idempotency-Key header.

RULES:
- backend stores key + response for 24 hours
- duplicate keys return the stored response without re-execution
- applies to lock, unlock, and firmware update commands

REASON:
MQTT is not transactional; idempotency eliminates duplicate door operations from client retries.

---

# ADR-017: COLUMN-LEVEL ENCRYPTION FOR PII

Any stored personally identifiable information must be encrypted at the column level using AES-256.

SCOPE:
- user emails
- phone numbers
- face embeddings
- audit log details

IMPLEMENTATION:
EF Core value converters.

REASON:
Even if database backups are compromised, sensitive data remains unreadable.

---

# ADR-018: SHORT-LIVED MQTT TOKENS

MQTT authentication uses short-lived JWTs (1 hour) issued per device.

RULES:
- token scoped to a single device topic
- backend issues new token on device heartbeat
- broker enforces topic ACLs based on JWT claims

REASON:
Stolen device credentials become useless within an hour.

---

# ADR-019: AUTHORIZATION CACHING

Successful RBAC/PBAC evaluations are cached per session for 1 minute.

MECHANISM:
- decorator around authorization service
- cache invalidated via RolePermissionChanged domain event
- API key evaluations are NOT cached

REASON:
Reduces repeated database lookups during frequent unlock requests while staying secure.

---

# ADR-020: RATE LIMITING STRATEGY

A global rate-limiting middleware protects all endpoints.

LIMITS:
- per user: 100 requests/min
- per IP: 200 requests/min
- per API key: 500 requests/min
- unlock endpoint burst: 5 requests per 10 seconds per user

IMPLEMENTATION:
ASP.NET Core rate-limiting middleware.

REASON:
Prevents brute-force attacks and accidental client-loop flooding.

---

# ADR-021: OBSERVABILITY REQUIREMENTS

The system must provide structured logging, metrics, and distributed tracing.

COMPONENTS:
- structured logging: Serilog (includes TenantId, UserId, CorrelationId)
- metrics: Prometheus endpoint (request duration, pipeline latency, error rates)
- tracing: OpenTelemetry (span across unlock pipeline)
- alerting: AlertManager rules for critical thresholds
- dashboard: Pre-built Grafana SLA dashboard template

REASON:
Essential for debugging access-denied issues and monitoring SLA compliance.

---

# ADR-022: SIGNED FIRMWARE UPDATES

All over-the-air firmware updates must be digitally signed.

RULES:
- backend stores signed binary
- ESP32 verifies signature before flashing

REASON:
Prevents malicious firmware injection into physical devices.

---

# ADR-023: SESSIONS MODULE MERGED INTO IDENTITY

The Sessions module has been merged into Identity.

REASON:
- Sessions module had no independent domain logic
- Session, RefreshToken, and TrustedDevice entities naturally belong to Identity domain
- Eliminates a thin module that violated modular monolith principles

RULE:
All session-related entities and services are owned by Identity.Domain and Identity.Infrastructure.

---

# ADR-024: API KEY AUTHENTICATION

VERIXORA supports machine-to-machine authentication via API keys.

RULES:
- API keys scoped to a Home with specific permissions
- API keys stored hashed (SHA-256)
- API key authentication bypasses session validation in unlock pipeline (step 2 skipped)
- API key usage audited with key ID
- Max 10 API keys per Home
- API keys can be created, revoked, and rotated by Home Owners

REASON:
Enables service account integration without exposing user credentials.

---

# ADR-025: AUDIT LOG RETENTION

Audit logs follow a retention and archival policy.

RULES:
- operational database retains audit logs for 90 days
- AuditLogRetentionJob archives logs older than 90 days to cold storage (Azure Blob / AWS S3)
- archived logs retained for 7 years
- archived logs remain encrypted and immutable
- archive job runs daily

REASON:
Balances database performance with compliance requirements.

---

# ADR-026: NO OFFLINE UNLOCK

VERIXORA does not support offline unlock operations.

REASON:
- backend is the single source of truth for all security decisions
- offline unlock would require caching authorization policies on mobile device
- cached policies could be stale, allowing unauthorized access
- violates the core security model

FUTURE:
If required, must use short-lived cached tokens with mandatory sync-before-unlock and full audit reconciliation. Requires a new ADR.

---

# ADR-027: GRACEFUL SHUTDOWN

All background services must implement graceful shutdown.

SERVICES AFFECTED:
- MQTT connection handler
- Backup job
- Idempotency cleanup job
- Automation scheduler
- Audit log retention job

IMPLEMENTATION:
All background services implement IHostedService with CancellationToken support. Graceful shutdown registered in Program.cs.

REASON:
Prevents data loss and incomplete operations during deployment or server restart.

---

# ADR-028: DATABASE MIGRATION STRATEGY

All EF Core migrations must be backward-compatible.

RULES:
- no rename columns in a single deployment
- no delete columns in a single deployment
- use expand-contract pattern: add new column -> deploy -> migrate data -> remove old column in next deployment
- all migrations tested against production-like dataset before deployment

REASON:
Enables zero-downtime deployments.

---

# ADR-029: FEATURE FLAGS

New features must be wrapped with feature flags for incremental rollout.

IMPLEMENTATION:
- IFeatureFlagService in BuildingBlocks
- reads from configuration (appsettings) or database
- feature flag checks in controllers or middleware

REASON:
Enables disabling problematic features in production without full rollback.

---

# ADR-030: ENVIRONMENT STRATEGY

Four environments defined with consistent configuration management.

ENVIRONMENTS:
- Development: local, appsettings.Development.json
- Testing: CI/CD, appsettings.Testing.json
- Staging: pre-production, appsettings.Staging.json
- Production: live, appsettings.Production.json

SECRETS:
Connection strings, JWT keys, MQTT credentials never stored in appsettings files. Retrieved from Azure Key Vault or AWS Secrets Manager.

---

# ADR-031: SECRETS MANAGEMENT

All secrets must be stored externally.

IMPLEMENTATION:
- Azure Key Vault (primary) or AWS Secrets Manager (alternative)
- secrets referenced by key name in configuration
- local development uses dotnet user-secrets or local secrets manager emulator
- CI/CD injects secrets at deployment time

REASON:
Secrets in source control are a critical security risk.

---

# ADR-032: CONTAINERIZATION

Application must be containerized for consistent deployment.

IMPLEMENTATION:
- Dockerfile for ApiHost
- docker-compose.yml for local development (ApiHost + PostgreSQL + Mosquitto)
- Kubernetes manifests for production (post-MVP)

REASON:
Ensures consistent runtime environments across development, testing, and production.

---

# ADR-033: DATABASE CONNECTION RESILIENCE

EF Core must be configured with transient fault retry handling.

IMPLEMENTATION:
- EnableRetryOnFailure() in BaseDbContext
- retry count: 3
- max delay: 30 seconds
- applies to all DbContext instances

REASON:
Handles temporary database connection failures without crashing the application.

---

# ADR-034: API DOCUMENTATION STANDARD

All API endpoints must be documented with XML comments.

IMPLEMENTATION:
- XML documentation generation enabled on all Presentation projects
- Swagger configured to include XML comments
- standard error response format defined in Contracts
- all endpoints include example request/response in documentation

REASON:
Ensures consistent API documentation for mobile and web developers.

---

# ADR-035: SHARED TYPESCRIPT GENERATION

TypeScript interfaces for mobile and web must be generated from API contracts.

IMPLEMENTATION:
- NSwag or Swagger Codegen in CI pipeline
- generates TypeScript models from Swagger JSON
- published as npm package or included in build output

REASON:
Prevents drift between backend contracts and frontend models.

---

# ADR-036: MODULE VERSIONING POLICY

Module contracts follow semantic versioning.

RULES:
- MAJOR: breaking API changes
- MINOR: new endpoints
- PATCH: non-breaking fixes
- each module maintains a CHANGELOG
- contract tests run in CI to detect breaking changes

REASON:
Provides clear communication of changes to dependent modules and frontend teams.

---

# ADR PRIORITY RULE

If ADRs conflict:
1. Security ADRs win (012, 013, 016, 017, 018, 022, 024, 026, 031)
2. Data integrity ADRs win (008, 010, 025, 028)
3. Domain correctness ADRs win (001, 002, 009, 023)
4. Operational ADRs win (027, 029, 030, 033)
5. Developer experience ADRs win (015, 034, 035, 036)
6. Performance ADRs win (019, 020) only after above are satisfied

---

**DOCUMENT VERSION: Final**
**LAST UPDATED: 2026-06-03**

---

**ARCHITECTURAL DECISION UPDATED.**

New ADRs added:
- ADR-023: Sessions merged into Identity
- ADR-024: API Key Authentication
- ADR-025: Audit Log Retention
- ADR-026: No Offline Unlock
- ADR-027: Graceful Shutdown
- ADR-028: Database Migration Strategy
- ADR-029: Feature Flags
- ADR-030: Environment Strategy
- ADR-031: Secrets Management
- ADR-032: Containerization
- ADR-033: Database Connection Resilience
- ADR-034: API Documentation Standard
- ADR-035: Shared TypeScript Generation
- ADR-036: Module Versioning Policy

Total ADRs: 22 -> 36