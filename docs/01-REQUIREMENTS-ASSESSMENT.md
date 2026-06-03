# FINAL VERIXORA REQUIREMENTS (FINAL VERSION)

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
- API keys for service accounts

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
- API key generation, management, and revocation for service accounts
- API keys scoped to Home with specific permissions

## Home System (Tenant Core)
- Create Home (tenant)
- Assign users to Home
- Assign roles per Home
- Configurable device limit per Home (default 20)
- Max 10 API keys per Home

## Device System
- Register IoT devices (ESP32)
- Assign device to Home
- Device status tracking
- Device must be provisioned before activation
- Device cannot exist without Home
- Device decommissioning process (revoke credentials, mark status)

## Smart Lock Control
- Lock door
- Unlock door (via validation pipeline)
- Emergency lock (admin only)
- Device command execution via MQTT
- Idempotency keys for all lock/unlock commands
- Commands must be idempotent and replay-safe
- Offline unlock is NOT supported (security decision)

## Authorization System
- Role based access control (RBAC)
- Policy based access control (PBAC)
- Permission validation
- Device-level access rules
- API key scope validation
- Authorization caching per session (1 minute)
- DENY always overrides ALLOW

## Unlock Decision Pipeline
- Fixed order evaluation (cheap to expensive)
- Schedule validation early (optimised)
- Face verification last (if required)
- API key authentication bypasses session check (step 2)
- Performance SLA: 200ms p95

## Audit System
- Record all security actions
- Immutable, append-only audit logs
- Encrypted at column level
- Scoped to Home
- 90-day operational retention
- Archival to cold storage after 90 days
- Archived logs retained for 7 years

## Security Hardening
- Rate limiting: 100 req/min per user, 200 req/min per IP, 500 req/min per API key
- Unlock endpoint burst limit: 5 req / 10 sec per user
- Data at rest encryption: AES-256 for PII and audit logs
- JWT expiry: 15 minutes
- Refresh token rotation on use
- Session device fingerprinting (force re-login on change)
- API keys stored hashed (SHA-256)
- Secrets never stored in configuration files

## IoT Communication
- MQTT for device commands and telemetry
- Short-lived MQTT tokens (1 hour) per device
- BLE for provisioning only

## API & Operations
- API versioning: /api/v1/
- Health checks: /health endpoint
- Structured logging (Serilog) with TenantId, UserId, CorrelationId
- Environment strategy: Development, Testing, Staging, Production
- Secrets management via Azure Key Vault or AWS Secrets Manager

## Observability
- Prometheus metrics for latency, error rates
- OpenTelemetry tracing for unlock pipeline
- AlertManager rules for critical thresholds
- Pre-built Grafana SLA dashboard

---

# SHOULD HAVE REQUIREMENTS

- Monitoring dashboard
- Real-time updates via SignalR
- Notification system (email only initially)
- Basic scheduling rules for guest access
- Automated daily encrypted backups (post-MVP)
- Manual backup procedure documented (MVP)
- Suspicious activity detection (failed attempts, device offline patterns)
- Docker containerization for development
- TypeScript contract generation from API specs

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
- Device simulator for development/testing
- Kubernetes deployment
- Load testing framework
- Offline unlock capability (requires major architectural change)

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
10. Audit log retained for 90 days, then archived

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
- API keys hashed, scoped, and auditable
- Secrets managed externally, never in code
- Offline unlock explicitly not supported

---

# DEVICE REQUIREMENTS

- Devices must be provisioned before use
- Devices cannot act independently
- Devices may be offline
- Commands must be idempotent
- Firmware updates must be signed and verified
- Device limit per Home configurable
- Device decommissioning must revoke all credentials

---

# PERFORMANCE EXPECTATIONS

- Unlock decision pipeline must complete within 200ms p95
- MQTT command delivery must be async and retry-safe
- System must handle concurrent access requests safely
- Authorization cache must reduce repeated permission checks
- Database connections use transient fault retry handling

---

# SYSTEM BOUNDARIES

VERIXORA does NOT include:
- hardware manufacturing
- cloud identity provider dependency
- external biometric system dependency (optional only)
- distributed microservices architecture (not in MVP phase)
- offline unlock capability (explicitly excluded for security)

---

# USER JOURNEYS

## Owner Journey
- creates Home
- adds devices
- assigns users
- controls access
- creates and manages API keys

## Guest Journey
- receives access
- uses door within schedule
- has limited permissions

## Technician Journey
- manages device health
- performs maintenance only

## Service Account Journey
- authenticates via API key
- performs automated operations within scoped permissions
- actions audited with key ID

---

# DEFERRED REQUIREMENTS

- SMS integration
- push notifications
- full automation engine
- production face recognition
- multi-region deployment
- microservices migration
- advanced reporting suite
- offline unlock capability
- device simulator

---

**DOCUMENT VERSION: Final**
**LAST UPDATED: 2026-06-03**

---

**FINAL VERIXORA REQUIREMENTS UPDATED.**

All improvements integrated:
- API key authentication (MUST HAVE)
- Offline mode exclusion (MUST HAVE - explicitly not supported)
- Audit log retention policy (MUST HAVE)
- Secrets management (MUST HAVE)
- Environment strategy (MUST HAVE)
- Suspicious activity detection (SHOULD HAVE)
- Docker containerization (SHOULD HAVE)
- TypeScript generation (SHOULD HAVE)