# VERIXORA MASTER SPECIFICATION (FINAL - WITH ROADMAP - ASCII SAFE)

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

---

# CORE DOMAIN SCOPE

VERIXORA manages:
- Identity and authentication
- Role and permission based authorization
- Physical device management (ESP32)
- Smart lock control
- Secure provisioning
- Real time monitoring
- Audit logging
- Notifications
- Automation rules
- Security policies
- Optional face verification

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
- Contracts Layer

EVENT BUS IMPLEMENTATION:
- Domain Events: in-memory mediator (MediatR) inside each module.
- Integration Events: lightweight message channel (PostgreSQL LISTEN/NOTIFY initially; RabbitMQ optional later). Each integration event carries a unique EventId for deduplication.

---

# MODULE CATALOG

Core modules:
- Identity
- Authorization
- Sessions
- Devices
- Provisioning
- SmartLocks
- Monitoring
- AuditLogs
- Notifications
- Reports (read-only, deferred to post-MVP)
- Automation
- Security
- FaceVerification

---

# DOMAIN OWNERSHIP RULES

| Entity         | Owner Module           |
| -------------- | ---------------------- |
| User           | Identity               |
| Home           | Identity / Tenant Core |
| Device         | Devices                |
| SmartLock      | SmartLocks             |
| Session        | Sessions               |
| AuditLog       | AuditLogs              |
| Permission     | Authorization          |
| AutomationRule | Automation             |

---

# SECURITY PHILOSOPHY

Security is higher priority than usability.

RULES:
- Every request must be authenticated unless public.
- Every action must be authorized.
- Devices are NOT trusted.
- Backend enforces all decisions.
- All critical actions must be audited.
- Data at rest encryption: AES-256 column-level encryption for all PII (emails, phone numbers, face embeddings) and audit logs. Implemented via EF Core value converters.

---

# AUTHORIZATION MODEL

VERIXORA uses hybrid authorization:
- RBAC (Role Based Access Control)
- PBAC (Policy Based Access Control)
- Device level constraints

EVALUATION ORDER (STRICT):
1. Explicit DENY policies
2. Device level restrictions
3. Policy based rules (PBAC)
4. Role based permissions (RBAC)
5. Explicit allow rules

DENY ALWAYS WINS.

AUTHORIZATION CACHING:
- Successful permission evaluations are cached per session for 1 minute.
- Cache invalidated on role/permission change via domain event.
- Implemented as a decorator around the authorization service.

---

# UNLOCK DECISION ENGINE (OPTIMISED)

All door unlock requests must pass through this fixed evaluation order.  
Steps are ordered cheap-to-expensive, with time-based checks early.

1. JWT validation
2. Session validation
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
- Execution depth limits
- All actions are audited

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

---

# DATABASE STRATEGY

- EF Core ORM
- One DbContext per module
- Schema per module (single database)
- PostgreSQL production recommended
- SQLite for testing
- Read-only replicas allowed for Reports module (post-MVP)

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

---

# SYSTEM INVARIANTS

- Every device belongs to exactly one Home.
- Every action is scoped to Home.
- No cross-home data leakage.
- Every unlock attempt is logged.
- No device can bypass backend validation.

---

# RATE LIMITING & SECURITY HARDENING

Global Rate Limiting:
- Per user: 100 requests/min
- Per IP: 200 requests/min

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

---

# BACKUP & DISASTER RECOVERY

- SHOULD HAVE (post-MVP): Automated daily encrypted database backups, stored off-site with 30-day retention.
- For MVP: Manual backup procedure documented and verified.

---

# CI/CD PIPELINE (DESIGNED NOW, IMPLEMENTED POST-MVP)

- GitHub Actions workflow:
  - Build
  - Run unit + integration tests
  - Deploy to staging
  - Smoke tests (unlock flow)
  - Manual approval for production deployment

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

---

# SYSTEM BOUNDARIES

VERIXORA does NOT include:
- hardware manufacturing
- cloud identity provider dependency
- external biometric system dependency (optional only)
- distributed microservices architecture (not in MVP phase)

---

# DEVELOPMENT ROADMAP & ITERATION PLAN

The system is delivered in six iterations, each covering backend, mobile app, and web portal phases.  
All MUST HAVE requirements are complete by the end of Iteration 4.  
All ADRs are implemented within the iterations listed below.

## ITERATION 0: PROJECT FOUNDATION & SETUP (1-2 weeks)

| Phase          | Backend                                                                                         | Mobile App                         | Web Portal                      |
| -------------- | ----------------------------------------------------------------------------------------------- | ---------------------------------- | ------------------------------- |
| Design         | Modular monolith structure, NuGet packages, ADR review                                          | Ionic project, Capacitor config, theme | Angular workspace, Material theming |
| Implementation | ApiHost scaffold, module empty projects, SharedKernel, BuildingBlocks, Serilog, OpenTelemetry, **global rate limiting middleware** | Login UI shell, routing            | Login UI shell, routing         |
| Integration    | Database provisioning (PostgreSQL), EF Core migrations, CI/CD pipeline                         | –                                  | –                               |
| Testing        | Architecture validation script passes, health check endpoint active                            | –                                  | –                               |
| Deployment     | CI/CD pipeline (GitHub Actions), /health endpoint                                               | –                                  | –                               |

**Rate Limiting active from start** (ADR-020): global limits 100 req/min per user, 200 per IP.

## ITERATION 1: IDENTITY & HOME MANAGEMENT (2-3 weeks)

| Phase          | Backend                                                                                         | Mobile App                      | Web Portal                        |
| -------------- | ----------------------------------------------------------------------------------------------- | ------------------------------- | --------------------------------- |
| Design         | User, Home, Session aggregates, role model                                                     | Registration/login wireframes   | Admin user management wireframes  |
| Implementation | Identity module (Domain, App, Infra, Presentation), JWT auth, refresh tokens, session fingerprint, column encryption (ADR-017) | Registration, login, OTP, home list | Home CRUD, user/role assignment |
| Integration    | API v1 endpoints, Swagger, unit/integration tests                                               | Connect to backend              | Connect to backend                |
| Testing        | Identity use case tests                                                                         | E2E smoke tests                 | E2E smoke tests                   |
| Deployment     | Deploy to staging, validation script                                                            | Build for test devices          | Build for staging                 |

## ITERATION 2: DEVICE REGISTRATION & PROVISIONING (2-3 weeks)

| Phase          | Backend                                                                                         | Mobile App                   | Web Portal                      |
| -------------- | ----------------------------------------------------------------------------------------------- | ---------------------------- | ------------------------------- |
| Design         | Device aggregate, provisioning token, MQTT token service (ADR-018), configurable device limit  | Device registration form, BLE flow | Device list, status view     |
| Implementation | Devices + Provisioning modules, short-lived MQTT tokens, device health                         | BLE provisioning screens, scanner | Device management dashboard  |
| Integration    | MQTT broker setup, device simulator                                                             | BLE plugin integration       | –                               |
| Testing        | Provisioning & device limit tests                                                               | Simulated provisioning       | Device CRUD tests               |
| Deployment     | Update staging                                                                                  | –                            | –                               |

## ITERATION 3: SMART LOCK CONTROL & UNLOCK PIPELINE (3-4 weeks)

| Phase          | Backend                                                                                         | Mobile App                       | Web Portal                |
| -------------- | ----------------------------------------------------------------------------------------------- | -------------------------------- | ------------------------- |
| Design         | SmartLock aggregate, unlock pipeline handler, idempotency decorator (ADR-016), authorization caching (ADR-019) | Lock/unlock UI, status indicator | Lock management page      |
| Implementation | SmartLocks module, command handlers, MQTT publishing, idempotency store, **unlock burst rate limit** | Lock/unlock interactions, SignalR status | Admin override controls |
| Integration    | MQTT device integration, SignalR hub                                                            | Connect to SignalR               | –                         |
| Testing        | Full pipeline tests, idempotency, rate limit, SLA measurement                                   | E2E unlock flow                  | –                         |
| Deployment     | Performance monitoring enabled                                                                  | –                                | –                         |

## ITERATION 4: AUTHORIZATION, AUDIT & SECURITY HARDENING (2-3 weeks)

| Phase          | Backend                                                                                         | Mobile App | Web Portal       |
| -------------- | ----------------------------------------------------------------------------------------------- | ---------- | ---------------- |
| Design         | PBAC evaluation engine, audit log schema, firmware signing service (ADR-022)                   | –          | Audit log viewer |
| Implementation | Authorization module cache invalidation, immutable audit logs, encrypted columns, signed firmware handling | –          | Audit dashboard  |
| Integration    | Firmware signing validation, email notifications                                               | –          | –                |
| Testing        | Auth cache, audit immutability, firmware signature                                              | –          | –                |
| Deployment     | –                                                                                               | –          | –                |

## ITERATION 5: MONITORING, NOTIFICATIONS & AUTOMATION (2-3 weeks)

| Phase          | Backend                                                                                         | Mobile App         | Web Portal               |
| -------------- | ----------------------------------------------------------------------------------------------- | ------------------ | ------------------------ |
| Design         | Monitoring aggregations, notification templates, IF-THEN rule engine                           | Notification UI    | Real-time dashboard      |
| Implementation | Monitoring module, Notifications (email), Automation module (depth limits)                     | Push via SignalR   | Charts, alert management |
| Integration    | Prometheus, Grafana                                                                            | –                  | –                        |
| Testing        | Loop detection, notification delivery                                                           | –                  | –                        |
| Deployment     | –                                                                                               | –                  | –                        |

## ITERATION 6: FACE VERIFICATION & PRODUCTION HARDENING (2-3 weeks)

| Phase          | Backend                                                                                         | Mobile App            | Web Portal          |
| -------------- | ----------------------------------------------------------------------------------------------- | --------------------- | ------------------- |
| Design         | Pluggable face provider, embedding encryption, caching                                         | Face capture screen   | Settings page       |
| Implementation | FaceVerification module, mock/real provider                                                    | Camera integration, face submission | –          |
| Integration    | Face recognition endpoint, session cache                                                       | –                     | –                   |
| Testing        | Spoof detection, face match, final pipeline SLA                                                | –                     | –                   |
| Deployment     | Production release, automated backups (ADR backup), app store submission                       | App store submission  | Production deploy   |

---

## TRACEABILITY MATRIX

| Document / Requirement          | Iteration(s)   |
| ------------------------------- | -------------- |
| Identity, JWT, sessions         | 1              |
| Home, roles                     | 1              |
| Devices, provisioning, MQTT tokens | 2           |
| Smart lock, pipeline, idempotency, auth cache | 3      |
| Rate limiting (global)          | 0              |
| Rate limiting (unlock burst)    | 3              |
| Authorization (RBAC/PBAC)       | 4              |
| Audit logs, encryption          | 4              |
| Signed firmware                 | 4              |
| Monitoring, notifications       | 5              |
| Automation                      | 5              |
| Face verification               | 6              |
| Observability, health checks    | 0, 5           |
| API versioning                  | 0              |
| Backup strategy                 | 6              |
| Validation script               | 0 (runs every iteration) |

---

DOCUMENT VERSION: Final - With Roadmap  
LAST UPDATED: 2026-06-02
