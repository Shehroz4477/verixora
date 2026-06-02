# VERIXORA Master Specification

## System Identity

VERIXORA is an enterprise-grade IoT smart security and access control platform for managing and securing physical environments.

The platform combines:

- Smart locks and ESP32-based IoT devices
- Identity and access control
- A centralized security decision engine
- Real-time monitoring
- Audit and compliance tracking
- Notifications and reporting
- Automation workflows
- Optional face verification for high-risk access decisions

Core principle: the backend is the only source of truth for all access decisions.

## Platform Components

VERIXORA has four major product components:

| Component | Technology |
| --- | --- |
| Mobile App | Ionic 7, Angular 20, Capacitor |
| Web Portal | Angular 20, Angular Material |
| Backend API | ASP.NET Core 8 modular monolith |
| IoT Devices | ESP32 DevKit V1, Arduino framework, MQTT, BLE |

The platform supports smart homes, smart locks, role and permission management, real-time monitoring, face-verified unlock, device provisioning, automation, audit logs, reporting, and multi-factor authentication.

## Technology Stack

Backend:

- ASP.NET Core 8
- SQL Server 2022 for development
- PostgreSQL, MySQL, and SQLite support through provider switching where practical
- Redis for caching and session state
- MQTT through EMQX or Mosquitto for ESP32 communication
- SignalR for real-time dashboard updates
- Hangfire for background jobs
- MediatR for CQRS
- FluentValidation for request validation
- Mapster for object mapping
- Serilog for structured logging
- Swashbuckle for Swagger/OpenAPI
- Hellang ProblemDetails for API error handling
- MailKit and MimeKit for email
- Argon2id for password hashing
- JWT bearer tokens with refresh token rotation

Mobile:

- Ionic 7
- Angular 20
- Capacitor
- Native biometric authentication
- BLE provisioning
- Push notifications

Web:

- Angular 20
- Angular Material
- SignalR client
- Chart.js

IoT:

- ESP32 DevKit V1
- Arduino framework
- AsyncMqttClient
- NimBLE-Arduino
- mbedTLS

## Architecture

VERIXORA uses a modular monolith architecture with Clean Architecture and Vertical Slice Architecture.

The system is deployed as one application, but internally split into isolated bounded-context modules. Each module owns its domain rules and follows the same four-layer structure:

- Domain: business rules, entities, value objects, aggregates, domain events
- Application: use cases, commands, queries, handlers, contracts
- Infrastructure: persistence and external integrations
- Presentation: API endpoints and request/response contracts

CQRS separates commands from queries. Feature folders should group vertical slices, including command/query contracts, handlers, validators, and mappings. Domain events and integration events are used for event-driven behavior across module boundaries without direct module references.

## Dependency Rules

SharedKernel contains only cross-cutting domain primitives:

- Entity base class
- ValueObject base class
- DomainException
- DomainEvent abstraction
- Guard clauses

SharedKernel must not depend on any module. Modules may depend on SharedKernel.

Layer dependency rules:

- Domain depends only on SharedKernel
- Application depends on Domain and SharedKernel
- Infrastructure depends on Application and Domain
- Presentation depends on Application
- ApiHost composes all Presentation and Infrastructure layers

No circular dependencies are allowed. No module may directly depend on another module.

Contracts projects are reference-free message/API contract projects used to avoid illegal module dependencies.

## Module Catalog

VERIXORA is organized around these modules:

- Identity
- Authorization
- Devices
- SmartLocks
- Provisioning
- Monitoring
- Notifications
- Sessions
- AuditLogs
- Reports
- Automation
- Security
- FaceVerification

Module responsibilities:

| Module | Responsibility |
| --- | --- |
| Identity | Registration, login, email verification, password reset, profile, trusted devices, sessions |
| Authorization | RBAC, PBAC, device-level access, schedule restrictions |
| Devices | ESP32 device management, health, firmware |
| Provisioning | BLE provisioning workflow, token generation, sessions, completion |
| SmartLocks | Door lock/unlock, 10-layer validation, emergency lock, configuration |
| Monitoring | Real-time dashboard, device status, alerts, SignalR |
| Notifications | Email, SMS, push notifications, templates, preferences |
| Sessions | Session lifecycle, revocation, active session limits |
| AuditLogs | Immutable write-only audit trail and export |
| Reports | Analytics, charts, CSV/PDF export |
| Automation | IF-THEN rules, time-based and event-based triggers |
| Security | Encryption, IP whitelist, policies, threat detection |
| FaceVerification | Face enrollment, matching, anti-spoofing, verification logs |

## Security Philosophy

Security wins over convenience. If a feature conflicts with security, the feature is rejected or redesigned.

Every request must be authenticated unless explicitly approved as a public system endpoint. Every protected action must pass authorization. Devices are never trusted by default and cannot independently decide access outcomes.

Critical actions must always be audited with:

- User identity
- Device identity
- Action type
- Timestamp
- Result
- Metadata

## Users And Roles

Users can:

- Register an account
- Log in and log out
- Manage their profile
- Request device access
- View assigned devices
- View their activity logs

Admins can:

- Manage users
- Assign roles
- Grant and revoke permissions
- View system-wide logs
- Perform approved emergency operations

Example roles:

- SystemAdmin
- Owner
- FamilyMember
- Guest
- Technician

Role intent:

- SystemAdmin: full platform administration
- Owner: full control over own property
- FamilyMember: trusted limited management
- Guest: temporary restricted access with schedule
- Technician: device maintenance only

Permission examples:

- door.unlock
- door.lock
- device.control
- device.configure
- reports.view
- users.manage

## Access Control

Every action is permission-based. Permissions may be granted through roles or policies.

Access rules include:

- No permission means no access
- Expired permission is denied
- Revoked permission is denied immediately
- Access decisions must satisfy the full validation chain

Policy examples:

- Time-based access
- Location-based access
- Device-specific access
- Face verification required for sensitive access

Guests can only access specific doors during specific time windows.

## Unlock Decision Engine

Door unlock requests must pass the full decision pipeline:

1. JWT validation
2. Session validation
3. User validation
4. Role validation
5. Permission validation
6. Door access validation
7. Schedule validation
8. Trusted device validation
9. Device health validation
10. Face verification when required

If any step fails, the request is denied, audited, and may generate a security alert.

## Device Rules

Supported device types include:

- Smart locks
- Sensors
- ESP32 IoT controllers

Devices are passive. A device never decides whether an action is allowed. It only executes commands issued by the backend after the backend has completed validation.

Device lifecycle:

1. Register device
2. Activate device
3. Assign to user, lock, or location
4. Receive backend commands
5. Execute commands
6. Send status updates
7. Emit heartbeat and telemetry
8. Participate in monitoring

Devices communicate through the backend only. Planned communication channels include MQTT for IoT messaging and SignalR for real-time application updates.

## Smart Lock Rules

Smart locks can:

- Lock a door
- Unlock a door
- Auto-lock after timeout
- Support emergency override for authorized administrators

Unlocking requires the full authorization chain. Emergency override requires elevated permission and must always be audited.

## Core Workflows

### Door Unlock Flow

1. User sends unlock request
2. API authenticates user
3. Authorization module validates permissions
4. Security module evaluates access rules
5. Device module resolves the target device
6. Command is sent to the device
7. Device executes the command
8. Device returns the result
9. Audit log is created
10. Monitoring status is updated

### Unauthorized Access Attempt

1. Request is received
2. Authentication fails or permission is missing
3. Request is rejected
4. Security alert is generated
5. Audit log is stored
6. Monitoring is notified

### Device Heartbeat Flow

1. Device sends heartbeat
2. Backend validates device identity and trust state
3. Device status is updated
4. Activity is logged
5. Monitoring dashboard receives the latest state

## Business Rules

Core rules:

- No action without authentication
- No protected action without authorization
- No device operation without registration
- No execution without the validation chain
- All critical actions must be traceable
- Security overrides usability
- Backend is authoritative for all decisions
- Email addresses must be unique
- Users cannot exceed active session limits
- Security-sensitive actions must be logged
- Access tokens expire after 15 minutes
- Refresh tokens use 30-day rolling expiration
- A user may have at most 5 trusted devices
- A home may have at most 20 devices by default
- Provisioning tokens expire after 2 minutes
- Sensitive doors always require face verification
- Email must be verified before Web Portal access
- Web Portal login requires email, password, and email OTP
- Door unlock face verification must run last because it is the most expensive validation step

The system must reject:

- Unauthorized access attempts
- Unknown devices
- Tampered requests
- Expired sessions
- Invalid tokens
- Unregistered device actions
- Illegal direct cross-module calls

## Data And Audit

Audit logs are immutable records for forensic analysis and compliance. Normal users cannot delete audit logs.

Audit data should preserve enough context to reconstruct security-sensitive actions, access decisions, and device command outcomes.

## Database Strategy

Development database: SQL Server Developer Edition.

Testing database: SQLite in-memory.

Production database: PostgreSQL is preferred for low-cost deployments. SQL Server Standard remains an option when needed.

Entity Framework Core is the persistence strategy. Migrations should be managed centrally and aligned with module boundaries.

Each module owns its own DbContext. Modules may share one physical database using schema-per-module. The architecture should allow separate databases later if scaling requires it.

## Deployment Strategy

Local development uses Docker Compose with:

- API
- SQL Server
- Redis
- EMQX

Production phase 1 targets a single Azure VM running containers and either SQL Server or PostgreSQL. The future architecture may scale into separate databases or managed cloud services.

## Web Portal Scope

The Web Portal includes:

- Authentication and OTP
- Real-time dashboard
- User management
- Role and permission management
- Device management
- Door control
- Monitoring and alerts
- Face verification management
- Automation rules
- Reports and exports
- Audit logs
- Notification preferences and templates
- Security settings
- System settings and health

## Mobile App Scope

The Mobile App includes:

- Registration
- Login with biometric support
- OTP for unknown devices
- Email verification from settings
- Door unlock with face capture
- Face enrollment
- Guest access management
- Notifications inbox
- Activity log
- BLE device provisioning
- Emergency features
- Profile, trusted devices, and sessions

## Testing Strategy

Testing priorities:

- Domain rule unit tests
- Application use-case tests
- Integration tests for persistence and APIs
- End-to-end access workflows
- Security tests
- Performance tests for high-frequency access and telemetry paths

Domain rules receive the highest coverage priority.

## Roadmap

1. Shared Kernel Foundation
2. Domain Modeling
3. Application Layer
4. Infrastructure
5. API and Frontend Integration
6. IoT Integration and Testing
