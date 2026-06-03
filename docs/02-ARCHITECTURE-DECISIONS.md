# VERIXORA ARCHITECTURE DECISIONS (ENHANCED - ASCII SAFE)

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

# NEW ADRS ADDED FOR ENHANCED SYSTEM

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

REASON:
Reduces repeated database lookups during frequent unlock requests while staying secure.

---

# ADR-020: RATE LIMITING STRATEGY

A global rate-limiting middleware protects all endpoints.

LIMITS:
- per user: 100 requests/min
- per IP: 200 requests/min
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

# ADR PRIORITY RULE (IMPORTANT)

If ADRs conflict:
1. Security ADRs win (012, 013, 016, 017, 018, 022)
2. Data integrity ADRs win (008, 010, 016)
3. Domain correctness ADRs win (001, 002, 009)
4. Performance ADRs win (019, 020) only after above are satisfied

---

DOCUMENT VERSION: Final - Enhanced
LAST UPDATED: 2026-06-02