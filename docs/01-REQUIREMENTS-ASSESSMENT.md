# VERIXORA Requirements Assessment

This document analyzes the requirements gathered so far and separates enterprise-ready decisions from items that need refinement or should be deferred.

## Accepted Core Requirements

These requirements are strong enough to guide implementation now.

### Product Domain

VERIXORA is an IoT smart security and access control platform for homes, rentals, and small businesses. The core domain is physical access control: deciding who can access which protected resource, under what conditions, and recording every important security event.

### Backend Authority

The backend is the only source of truth for access decisions. IoT devices are passive executors. Devices must not decide whether a door can unlock.

### Architecture

The backend will use:

- ASP.NET Core 8
- Modular monolith
- Clean Architecture
- Vertical Slice Architecture
- CQRS
- Domain events and integration events

The modular monolith is the correct choice for this stage. It gives enterprise structure without the operational cost of microservices.

### Module Boundaries

The 13-module catalog is accepted:

- Identity
- Authorization
- Sessions
- Devices
- Provisioning
- SmartLocks
- Monitoring
- AuditLogs
- Notifications
- Reports
- Automation
- Security
- FaceVerification

No module-to-module direct dependency is allowed. Cross-module communication should happen through contracts, domain events, integration events, or application-level orchestration.

### Security Model

Accepted security requirements:

- JWT access tokens
- Refresh token rotation
- Session tracking
- Trusted devices
- RBAC and permission-based authorization
- Device-level access restrictions
- Schedule-based restrictions
- Email OTP
- Optional face verification
- Immutable audit logs

### Unlock Decision Pipeline

The 10-layer unlock pipeline is accepted as the key domain workflow:

1. JWT validation
2. Session validation
3. User active check
4. Role check
5. Permission check
6. Door-specific access check
7. Schedule restriction check
8. Trusted device check
9. Door health check
10. Face verification when required

The ordering is good because cheap checks run before expensive checks.

### IoT Strategy

MQTT is accepted for device commands and telemetry. BLE provisioning is accepted for mobile-to-device setup. Device simulation is accepted as a first delivery path so development is not blocked by hardware.

### Database Strategy

Accepted:

- SQL Server for development
- SQLite in-memory for tests
- PostgreSQL as a production option
- EF Core for persistence
- One DbContext per module
- Schema-per-module in one physical database at first

## Requirements That Need Refinement

These ideas are useful but not yet detailed enough for implementation.

### Homes, Properties, Doors, And Ownership

The requirements mention homes, properties, doors, and owners, but the aggregate model is not fully defined.

Need decisions:

- Is a Home the top-level tenant boundary?
- Can one user own multiple homes?
- Can a door exist without a smart lock?
- Can one smart lock control more than one door?
- How are guests invited to a home?

### Multi-Tenancy

The project implies property-level isolation but does not fully define tenancy.

Need decisions:

- Tenant model: Home, Organization, Property, or Account?
- Can a SystemAdmin see all tenants?
- Can an Owner invite users across multiple properties?

### Face Verification Provider

Face verification is accepted as a business capability, but implementation provider is not chosen.

Need decisions:

- Local model, cloud provider, or mocked provider for phase 1?
- Where embeddings are generated?
- What anti-spoofing level is realistic for the academic prototype?
- What confidence threshold default should be used?

### SMS And Push Notifications

Notification types include email, SMS, and push, but providers are not selected.

Need decisions:

- Email provider first: SMTP via MailKit is enough.
- SMS provider can be deferred.
- Push notifications can be deferred until mobile app integration.

### Reporting And PDF Export

Reports and PDF export are useful, but not essential for the first working access-control path.

Need decisions:

- Which reports are required for MVP?
- Is CSV export enough for phase 1?
- PDF export can be deferred unless required by supervisor.

### Automation Engine Safety

Automation can trigger sensitive actions like unlock. This is risky.

Need decisions:

- Should automation be allowed to unlock doors, or only lock and notify?
- Does automation require owner confirmation for unlock?
- Are automation executions always audited?

## Deferred Requirements

These should not be built first because they add cost before the core system is proven.

- Full SMS integration
- Full push notification integration
- Production-grade face recognition
- Advanced anti-spoofing
- Multi-database deployment
- Cloud-managed infrastructure
- PDF report generation
- Firmware update delivery
- IP whitelist management
- API key management
- Complex custom role designer UI

## Rejected Or Changed Requirements

These requirements should be changed to reduce risk.

### SystemAdmin Bypasses Everything

Original idea: SystemAdmin can do everything and bypass all checks.

Decision: SystemAdmin may bypass normal permission checks, but must not bypass audit logging, tenant safety checks, or critical confirmation flows. Every SystemAdmin action must be traceable.

### Devices Execute Commands Independently

Rejected. Devices never decide access. Devices only execute backend-approved commands.

### Web Portal Before Core Backend

Rejected as an implementation priority. The backend domain and API must come first. Web Portal should consume stable APIs after the core workflows exist.

## MVP Definition

The first real MVP should prove one secure end-to-end flow:

1. User registers and logs in.
2. User receives JWT and refresh token.
3. Device or smart lock is registered or simulated.
4. Owner grants user door access.
5. User requests door unlock.
6. Backend runs the unlock pipeline.
7. MQTT command is published to simulator.
8. Audit log is written.
9. Monitoring event is produced.

If this works cleanly, the project has a strong enterprise foundation.
