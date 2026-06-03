# FINAL VERIXORA REQUIREMENTS (ENHANCED - ASCII SAFE)

---

# SYSTEM OVERVIEW

VERIXORA is an IoT-based smart security and access control platform.

It provides secure management of:
- users
- homes
- devices
- smart locks
- access control decisions
- audit logging

Backend is the system of record for all decisions.

---

# REQUIREMENT CLASSIFICATION MODEL

All requirements are classified as:
- MUST HAVE (core system functionality, required for MVP)
- SHOULD HAVE (important but not critical for initial release)
- NICE TO HAVE (future enhancements)

---

# MUST HAVE REQUIREMENTS (MVP CORE)

## Identity System
- User registration
- Login with JWT authentication
- Session management
- Refresh token rotation
- Max 5 trusted devices per user
- Unknown device requires OTP verification

## Home System (Tenant Core)
- Create Home (tenant)
- Assign users to Home
- Assign roles per Home
- Configurable device limit per Home (default 20)

## Device System
- Register IoT devices (ESP32)
- Assign device to Home
- Device status tracking
- Device must be provisioned before activation
- Device cannot exist without Home

## Smart Lock Control
- Lock door
- Unlock door (via validation pipeline)
- Emergency lock (admin only)
- Device command execution via MQTT
- **Idempotency keys for all lock/unlock commands** (added)
- Commands must be idempotent and replay-safe

## Authorization System
- Role based access control (RBAC)
- Policy based access control (PBAC)
- Permission validation
- Device-level access rules
- **Authorization caching per session (1 minute)** (added)
- DENY always overrides ALLOW

## Unlock Decision Pipeline
- Fixed order evaluation (cheap to expensive)
- Schedule validation early (optimised)
- Face verification last (if required)
- **Performance SLA: 200ms p95** (added)

## Audit System
- Record all security actions
- Immutable, append-only audit logs
- Encrypted at column level
- Scoped to Home

## Security Hardening (added)
- Rate limiting: 100 req/min per user, 200 req/min per IP
- Unlock endpoint burst limit: 5 req / 10 sec per user
- Data at rest encryption: AES-256 for PII and audit logs
- JWT expiry: 15 minutes
- Refresh token rotation on use
- Session device fingerprinting (force re-login on change)

## IoT Communication
- MQTT for device commands and telemetry
- Short-lived MQTT tokens (1 hour) per device (added)
- BLE for provisioning only

## API & Operations (added)
- API versioning: /api/v1/
- Health checks: /health endpoint
- Structured logging (Serilog) with TenantId, UserId, CorrelationId

## Observability (added)
- Prometheus metrics for latency, error rates
- OpenTelemetry tracing for unlock pipeline

---

# SHOULD HAVE REQUIREMENTS

- Monitoring dashboard
- Real-time updates via SignalR
- Notification system (email only initially)
- Basic scheduling rules for guest access
- Automated daily encrypted backups (post-MVP)
- Manual backup procedure documented (MVP)

---

# NICE TO HAVE REQUIREMENTS

- Face verification system (with pluggable provider)
- Automation engine (IF-THEN rules)
- Advanced reporting (read-only replicas)
- SMS and push notifications
- PDF export
- Advanced security analytics
- CI/CD pipeline (GitHub Actions)
- Production face recognition
- Multi-region deployment

---

# SYSTEM MVP VALUE LOOP

A valid MVP must support:
1. User registers and logs in
2. User belongs to a Home
3. Device is registered to Home
4. Owner assigns access to user
5. User requests unlock (with Idempotency-Key)
6. Backend validates request through full pipeline
7. MQTT command sent to device (short-lived token)
8. Device executes action (idempotently)
9. Audit log is recorded (immutable, encrypted)

---

# SECURITY REQUIREMENTS

- JWT must expire in 15 minutes
- Refresh tokens must rotate on use
- Sessions must be tracked per device with fingerprint
- Unknown devices require verification (OTP)
- All actions must be authorized
- All security actions must be logged
- Rate limiting enforced globally and per endpoint
- PII and audit logs encrypted at column level
- MQTT tokens short-lived and scoped

---

# DEVICE REQUIREMENTS

- Devices must be provisioned before use
- Devices cannot act independently
- Devices may be offline
- Commands must be idempotent
- Firmware updates must be signed and verified (added)
- Device limit per Home configurable (added)

---

# PERFORMANCE EXPECTATIONS

- Unlock decision pipeline must complete within 200ms p95
- MQTT command delivery must be async and retry-safe
- System must handle concurrent access requests safely
- Authorization cache must reduce repeated permission checks

---

# SYSTEM BOUNDARIES

VERIXORA does NOT include:
- hardware manufacturing
- cloud identity provider dependency
- external biometric system dependency (optional only)
- distributed microservices architecture (not in MVP phase)

---

# USER JOURNEYS

## Owner Journey
- creates Home
- adds devices
- assigns users
- controls access

## Guest Journey
- receives access
- uses door within schedule
- has limited permissions

## Technician Journey
- manages device health
- performs maintenance only

---

# DEFERRED REQUIREMENTS

- SMS integration
- push notifications
- full automation engine
- production face recognition
- multi-region deployment
- microservices migration
- advanced reporting suite

---

DOCUMENT VERSION: Final - Enhanced
LAST UPDATED: 2026-06-02