# VERIXORA MASTER SPECIFICATION (FINAL VERSION)

---

# SYSTEM IDENTITY

VERIXORA is an enterprise IoT smart security and physical access control platform for homes, rentals, and small businesses.

The system controls physical access using smart locks and IoT devices with backend-driven decision making.

CORE PRINCIPLE:
The backend is the single source of truth for all security decisions.
Devices are passive executors only.

---

# TENANT MODEL (CRITICAL FOUNDATION)

VERIXORA is a multi-tenant system.

TENANT ROOT ENTITY: Home

A Home is the primary isolation boundary.

RULES:
- Every user belongs to one or more Homes.
- Every device belongs to exactly one Home.
- Every smart lock belongs to exactly one Home.
- All authorization, audit logs, and events are scoped to Home.

OWNERSHIP MODEL:
- A Home has one or more Owners.
- Owners manage users, devices, and policies within their Home.
- SystemAdmin can access all Homes but must remain tenant-aware.

---

# PLATFORM COMPONENTS

| Component       | Technology                             |
| --------------- | -------------------------------------- |
| Mobile App      | Ionic 7 + Angular 20 + Capacitor       |
| Web Portal      | Angular 20 + Angular Material          |
| Backend API     | ASP.NET Core 8 Modular Monolith        |
| IoT Devices     | ESP32 DevKit V1 + Arduino + MQTT + BLE |

OPERATIONAL COMPONENTS:
- Structured logging: Serilog
- Metrics: Prometheus endpoint
- Distributed tracing: OpenTelemetry
- CI/CD: GitHub Actions
- Secrets management: Azure Key Vault / AWS Secrets Manager
- Containerization: Docker + docker-compose (dev), Kubernetes (prod)

---

# CORE DOMAIN SCOPE

VERIXORA manages:
- Identity and authentication
- Role and permission based authorization
- Physical device management (ESP32)
- Smart lock control
- Secure provisioning
- Audit logging
- Notifications
- Automation rules
- Security policies
- Optional face verification
- API key authentication for service accounts
- Device decommissioning
- Offline mode (documented limitation)

---

# ARCHITECTURE STYLE

VERIXORA uses:
- Modular Monolith Architecture
- Clean Architecture inside modules
- Vertical Slice Architecture in Application layer
- CQRS pattern
- Domain events and integration events

MODULE ISOLATION RULE:
No module may directly reference another module.

Cross-module communication must use:
- Domain Events
- Integration Events
- Contracts Layer (including IntegrationEvents folder)

EVENT BUS IMPLEMENTATION:
- Domain Events: in-memory mediator (MediatR) inside each module.
- Integration Events: lightweight message channel (PostgreSQL LISTEN/NOTIFY initially; RabbitMQ optional later). Each integration event carries a unique EventId for deduplication.

---

# MODULE CATALOG

Core modules:
- Identity (includes Session management)
- Authorization
- Devices
- Provisioning
- SmartLocks
- Monitoring
- AuditLogs
- Notifications
- Reports (read-only queries, deferred to post-MVP)
- Automation
- Security
- FaceVerification

MODULE NOTE:
The Sessions module has been merged into Identity. Session entities (Session, RefreshToken, TrustedDevice) are owned by Identity.Domain. This eliminates a thin module with no independent domain logic.

---

# DOMAIN OWNERSHIP RULES

| Entity         | Owner Module           |
| -------------- | ---------------------- |
| User           | Identity               |
| Home           | Identity               |
| Session        | Identity               |
| RefreshToken   | Identity               |
| TrustedDevice  | Identity               |
| ApiKey         | Identity               |
| Device         | Devices                |
| SmartLock      | SmartLocks             |
| AuditLog       | AuditLogs              |
| Permission     | Authorization          |
| AutomationRule | Automation             |
| FirmwarePackage| Security               |

---

# SECURITY PHILOSOPHY

Security is higher priority than usability.

RULES:
- Every request must be authenticated unless public.
- Every action must be authorized.
- Devices are NOT trusted.
- Backend enforces all decisions.
- All critical actions must be audited.
- Data at rest encryption: AES-256 column-level encryption for all PII (emails, phone numbers, face embeddings) and audit logs.
- API keys supported for machine-to-machine authentication with scoped permissions.
- Secrets never stored in configuration files; use secrets manager.

---

# AUTHORIZATION MODEL

VERIXORA uses hybrid authorization:
- RBAC (Role Based Access Control)
- PBAC (Policy Based Access Control)
- Device level constraints
- API key scoped access (service accounts)

EVALUATION ORDER (STRICT):
1. Explicit DENY policies
2. Device level restrictions
3. API key scope restrictions
4. Policy based rules (PBAC)
5. Role based permissions (RBAC)
6. Explicit allow rules

DENY ALWAYS WINS.

AUTHORIZATION CACHING:
- Successful permission evaluations are cached per session for 1 minute.
- Cache invalidated on role/permission change via domain event.
- API key evaluations are not cached (always re-evaluated).

---

# UNLOCK DECISION ENGINE (OPTIMISED)

All door unlock requests must pass through this fixed evaluation order.
Steps are ordered cheap-to-expensive, with time-based checks early.

1. JWT validation (or API key validation)
2. Session validation (skipped for API keys)
3. Schedule validation (moved early to fail fast)
4. User status validation
5. Role validation
6. Permission validation
7. Home level access validation
8. Device level access validation
9. Device health validation
10. Face verification (if required)

RULE: Any failure stops execution immediately.

PERFORMANCE SLA: The entire pipeline (excluding MQTT round-trip) must complete within 200ms p95.

---

# IOT COMMUNICATION MODEL

PROTOCOLS:
- MQTT = device commands and telemetry
- BLE = provisioning only
- SignalR = real time UI updates

MQTT TOKEN LIFECYCLE:
- Each device receives a short-lived JWT (1 hour) scoped to its own topic.
- Token issued by backend on device heartbeat; stolen tokens expire quickly.
- MQTT broker enforces topic ACLs based on JWT claims.

---

# IOT DEVICE RULES

DEVICE BEHAVIOR:
- Devices are passive executors.
- Devices never make security decisions.
- Devices only execute backend approved commands.

DEVICE IDENTITY:
- Unique Device ID
- Home association
- MQTT token as defined above

FAILURE HANDLING:
- Devices may be offline.
- Commands must be idempotent.
- Duplicate commands must be handled safely via Idempotency-Key header.

IDEMPOTENCY MECHANISM:
- All state-changing device commands (lock, unlock, firmware update) require an Idempotency-Key header.
- Backend stores key + response for 24 hours. Replayed commands return original result without re-execution.

OTA FIRMWARE UPDATES:
- Firmware binary must be digitally signed.
- ESP32 verifies signature before flashing.
- Backend stores signed image and distributes only verified binaries.

DEVICE DECOMMISSIONING:
- DecommissionDevice command revokes MQTT tokens, removes WiFi credentials, marks device as decommissioned.
- Decommissioned devices cannot be re-activated without re-provisioning.

---

# SMART LOCK RULES

SUPPORTED:
- Lock / Unlock
- Auto lock timer
- Emergency lock (admin only)

RULES:
- Unlock requires full validation pipeline.
- Sensitive locks require face verification.
- All actions are audited.
- Unlock requests are rate-limited (see Rate Limiting section).
- Offline mode is NOT supported. The mobile device must have connectivity to the backend to perform any lock operation. This is a security decision.

---

# OFFLINE MODE STRATEGY

VERIXORA does NOT support offline unlock operations.

RATIONALE:
- The backend is the single source of truth for all security decisions.
- Offline unlock would require caching authorization policies on the mobile device, violating the passive device model.
- Cached policies could be stale, allowing unauthorized access if permissions were revoked.

FUTURE CONSIDERATION:
- If offline support is required in the future, it must use short-lived cached tokens with mandatory sync-before-unlock and full audit reconciliation on reconnect.
- This would be a major architectural change requiring a new ADR.

---

# EVENT SYSTEM (FORMAL)

DOMAIN EVENTS EXAMPLES:
- UserCreated
- DeviceRegistered
- DoorUnlocked
- DoorLocked
- FaceVerificationFailed
- DeviceOffline
- AccessDenied
- RolePermissionChanged (triggers auth cache invalidation)

INTEGRATION EVENTS:
Each module's Contracts project includes an IntegrationEvents folder with cross-module event DTOs. Events are delivered via PostgreSQL LISTEN/NOTIFY (or RabbitMQ in future).

RULE: Events are immutable and used for monitoring, automation, and audit history.

---

# AUTOMATION ENGINE

MODEL: IF-THEN rule system

TRIGGERS:
- Time based
- Event based
- Condition based

ACTIONS:
- Lock door
- Unlock door (restricted)
- Send notification
- Trigger alarm

SAFETY RULES:
- No infinite loops
- Execution depth limits (max 10)
- All actions are audited
- Suspicious patterns trigger alerts (see Suspicious Activity Detection)

---

# FACE VERIFICATION

- Pluggable provider (mock or real AI).
- Runs only when required.
- Executed last in unlock pipeline.

RULES:
- Cached per session for short time.
- Stored embeddings are encrypted (column-level AES-256).

---

# AUDIT SYSTEM

AUDIT LOGS ARE:
- Immutable
- Append only
- Scoped to Home
- Encrypted at column level

REQUIRED DATA:
- User ID
- Device ID
- Action
- Timestamp
- Result
- Metadata

AUDIT LOG RETENTION:
- Operational database retains audit logs for 90 days.
- AuditLogRetentionJob archives logs older than 90 days to cold storage (Azure Blob / AWS S3).
- Archived logs are retained for 7 years for compliance.
- Archived logs remain encrypted and immutable.

---

# SUSPICIOUS ACTIVITY DETECTION

The Monitoring module includes a SuspiciousActivityDetector that analyzes audit patterns and raises alerts:

DETECTION RULES:
- X failed unlock attempts in Y minutes from same user (default: 5 in 10 minutes)
- X failed login attempts in Y minutes from same IP (default: 10 in 5 minutes)
- Device offline for more than X minutes (default: 30 minutes)
- Multiple devices in same Home going offline simultaneously
- Unlock attempts outside scheduled hours
- API key usage from new IP address

Alert severity: Warning for thresholds, Critical for repeated patterns.

---

# DATABASE STRATEGY

- EF Core ORM
- One DbContext per module
- Schema per module (single database)
- PostgreSQL production recommended
- SQLite for testing
- Read-only replicas allowed for Reports module (post-MVP)
- EnableRetryOnFailure configured for transient fault handling
- Migration strategy: backward-compatible only, expand-contract pattern for breaking changes

---

# DEVICE CONSTRAINTS

- Configurable device limit per Home (default 20; Owner may request increase).
- Device must be provisioned before activation.
- Device cannot exist without Home.
- Devices cannot act independently.

---

# SESSION AND TOKEN RULES

- JWT expiry: 15 minutes
- Refresh token: 30 day rolling
- Max trusted devices per user: 5
- Session stores device fingerprint (User-Agent, OS, screen resolution hash). If fingerprint changes dramatically mid-session, force re-login.
- Token rotation required.
- API keys: long-lived, scoped by permissions, stored hashed, can be revoked individually.

---

# API KEY AUTHENTICATION

VERIXORA supports machine-to-machine authentication via API keys.

RULES:
- API keys are scoped to a Home and have specific permissions.
- API keys are stored hashed (SHA-256) in the database.
- API key authentication bypasses session checks in the pipeline (step 2 skipped).
- API key usage is audited with the key ID.
- API keys can be created, revoked, and rotated by Home Owners.
- Max 10 API keys per Home.

---

# SYSTEM INVARIANTS

- Every device belongs to exactly one Home.
- Every action is scoped to Home.
- No cross-home data leakage.
- Every unlock attempt is logged.
- No device can bypass backend validation.
- All PII and audit logs are encrypted at rest.
- Secrets are never stored in configuration files.
- No offline unlock capability.

---

# RATE LIMITING & SECURITY HARDENING

Global Rate Limiting:
- Per user: 100 requests/min
- Per IP: 200 requests/min
- Per API key: 500 requests/min

Unlock Endpoint Specific:
- Burst limit: 5 requests per 10 seconds per user.
- Implemented via ASP.NET Core rate-limiting middleware.

---

# API VERSIONING

- All API routes prefixed with /api/v1/.
- Future breaking changes introduce /api/v2/ while v1 remains supported for at least one release cycle.
- Enforced via controller attributes.

---

# OBSERVABILITY

Health Checks:
- /health endpoint reports database connectivity, MQTT broker status, and per-module health.

Logging:
- Structured logging via Serilog. All log entries include TenantId, UserId, and CorrelationId.

Metrics:
- Prometheus metrics exposed for request duration, unlock pipeline latency, error rates.

Tracing:
- OpenTelemetry distributed tracing. Every unlock request creates a trace spanning the full pipeline.

Alerting:
- AlertManager rules defined for critical thresholds (latency, error rate, device offline, failed attempts).
- Pre-built Grafana dashboard for SLA monitoring.

---

# BACKUP & DISASTER RECOVERY

- SHOULD HAVE (post-MVP): Automated daily encrypted database backups, stored off-site with 30-day retention.
- For MVP: Manual backup procedure documented and verified.

---

# CI/CD PIPELINE (DESIGNED NOW, IMPLEMENTED POST-MVP)

- GitHub Actions workflow:
  - Build
  - Run unit + integration tests
  - Run contract tests
  - Run architecture validation script
  - Deploy to staging
  - Smoke tests (unlock flow)
  - Manual approval for production deployment

---

# ENVIRONMENTS

| Environment | Purpose | Configuration Source |
|-------------|---------|---------------------|
| Development | Local development | appsettings.Development.json + secrets manager dev |
| Testing | CI/CD test runs | appsettings.Testing.json + secrets manager test |
| Staging | Pre-production validation | appsettings.Staging.json + secrets manager staging |
| Production | Live system | appsettings.Production.json + secrets manager prod |

Secrets (connection strings, JWT keys, MQTT credentials) are never stored in appsettings files. They are retrieved from a secrets manager (Azure Key Vault or AWS Secrets Manager).

---

# CONTAINERIZATION

- Dockerfile provided for ApiHost.
- docker-compose.yml for local development includes: ApiHost, PostgreSQL, MQTT broker (Mosquitto).
- Production deployment via Kubernetes manifests (post-MVP).

---

# MVP DEFINITION

FIRST WORKING SYSTEM MUST SUPPORT:
1. User registration and login
2. Home creation
3. Device registration or simulator
4. Access assignment
5. Unlock request flow (with idempotency keys)
6. MQTT command execution
7. Audit logging
8. Basic monitoring (health checks + structured logs)
9. Rate limiting on unlock endpoints
10. API key authentication for service accounts
11. Audit log retention (90-day operational + archival)

---

# SYSTEM BOUNDARIES

VERIXORA does NOT include:
- hardware manufacturing
- cloud identity provider dependency
- external biometric system dependency (optional only)
- distributed microservices architecture (not in MVP phase)
- offline unlock capability (explicitly excluded)

---

# DEVELOPMENT ROADMAP & ITERATION PLAN

| Iteration | Focus | Duration |
|-----------|-------|----------|
| 0 | Project Foundation & Setup | 1-2 weeks |
| 1 | Identity & Home Management | 2-3 weeks |
| 2 | Device Registration & Provisioning | 2-3 weeks |
| 3 | Smart Lock Control & Unlock Pipeline | 3-4 weeks |
| 4 | Authorization, Audit & Security Hardening | 2-3 weeks |
| 5 | Monitoring, Notifications & Automation | 2-3 weeks |
| 6 | Face Verification & Production Hardening | 2-3 weeks |

---

## TRACEABILITY MATRIX

| Document / Requirement          | Iteration(s)   |
| ------------------------------- | -------------- |
| Identity, JWT, sessions         | 1              |
| Home, roles                     | 1              |
| API key authentication          | 1              |
| Devices, provisioning, MQTT tokens | 2           |
| Device decommissioning          | 2              |
| Smart lock, pipeline, idempotency, auth cache | 3      |
| Offline mode exclusion          | 3              |
| Rate limiting (global)          | 0              |
| Rate limiting (unlock burst)    | 3              |
| Authorization (RBAC/PBAC)       | 4              |
| Audit logs, encryption          | 4              |
| Audit log retention             | 4              |
| Signed firmware                 | 4              |
| Monitoring, notifications       | 5              |
| Automation                      | 5              |
| Suspicious activity detection   | 5              |
| Face verification               | 6              |
| Observability, health checks    | 0, 5           |
| API versioning                  | 0              |
| Backup strategy                 | 6              |
| Validation script               | 0 (runs every iteration) |
| Contract tests                  | 0 (runs every iteration) |
| Environments & secrets          | 0, 6           |
| Containerization                | 0, 6           |

---

**DOCUMENT VERSION: Final**
**LAST UPDATED: 2026-06-03**

---

**VERIXORA MASTER SPECIFICATION UPDATED.**

All 6 improvements integrated:
- Sessions module merged into Identity
- Reports module contracts minimum defined
- API key authentication added
- Audit log retention policy added
- Suspicious activity detection added
- Offline mode strategy documented