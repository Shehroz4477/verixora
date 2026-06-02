# VERIXORA MASTER SPECIFICATION

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

TENANT ROOT ENTITY: HOME

A Home is the primary isolation boundary.

RULES:

* Every user belongs to one or more Homes
* Every device belongs to exactly one Home
* Every smart lock belongs to exactly one Home
* All authorization, audit logs, and events are scoped to Home

OWNERSHIP MODEL:

* A Home has one or more Owners
* Owners manage users, devices, and policies within their Home
* SystemAdmin can access all Homes but must remain tenant-aware

---

# PLATFORM COMPONENTS

| Component   | Technology                             |
| ----------- | -------------------------------------- |
| Mobile App  | Ionic 7 + Angular 20 + Capacitor       |
| Web Portal  | Angular 20 + Angular Material          |
| Backend API | ASP.NET Core 8 Modular Monolith        |
| IoT Devices | ESP32 DevKit V1 + Arduino + MQTT + BLE |

---

# CORE DOMAIN SCOPE

VERIXORA manages:

* Identity and authentication
* Role and permission based authorization
* Physical device management (ESP32)
* Smart lock control
* Secure provisioning
* Real time monitoring
* Audit logging
* Notifications
* Automation rules
* Security policies
* Optional face verification

---

# ARCHITECTURE STYLE

VERIXORA uses:

* Modular Monolith Architecture
* Clean Architecture inside modules
* Vertical Slice Architecture in Application layer
* CQRS pattern
* Domain events and integration events

MODULE ISOLATION RULE:
No module may directly reference another module.

Cross module communication must use:

* Domain Events
* Integration Events
* Contracts Layer

---

# MODULE CATALOG

Core modules:

* Identity
* Authorization
* Sessions
* Devices
* Provisioning
* SmartLocks
* Monitoring
* AuditLogs
* Notifications
* Reports
* Automation
* Security
* FaceVerification

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

* Every request must be authenticated unless public
* Every action must be authorized
* Devices are NOT trusted
* Backend enforces all decisions
* All critical actions must be audited

---

# AUTHORIZATION MODEL

VERIXORA uses hybrid authorization:

* RBAC (Role Based Access Control)
* PBAC (Policy Based Access Control)
* Device level constraints

EVALUATION ORDER (STRICT):

1. Explicit DENY policies
2. Device level restrictions
3. Policy based rules (PBAC)
4. Role based permissions (RBAC)
5. Explicit allow rules

DENY ALWAYS WINS.

---

# UNLOCK DECISION ENGINE

All door unlock requests must pass:

1. JWT validation
2. Session validation
3. User status validation
4. Role validation
5. Permission validation
6. Home level access validation
7. Device level access validation
8. Schedule validation
9. Device health validation
10. Face verification (if required)

RULE:

* Steps are fixed order (cheap to expensive)
* Any failure stops execution immediately

---

# IOT COMMUNICATION MODEL

PROTOCOLS:

* MQTT = device commands and telemetry
* BLE = provisioning only
* SignalR = real time UI updates

---

# IOT DEVICE RULES

DEVICE BEHAVIOR:

* Devices are passive executors
* Devices never make security decisions
* Devices only execute backend approved commands

DEVICE IDENTITY:

* Unique Device ID
* Home association
* Authentication secret (MQTT token or equivalent)

FAILURE HANDLING:

* Devices may be offline
* Commands must be idempotent
* Duplicate commands must be handled safely

---

# SMART LOCK RULES

SUPPORTED:

* Lock / Unlock
* Auto lock timer
* Emergency lock (admin only)

RULES:

* Unlock requires full validation pipeline
* Sensitive locks require face verification
* All actions are audited

---

# EVENT SYSTEM (FORMAL)

DOMAIN EVENTS EXAMPLES:

* UserCreated
* DeviceRegistered
* DoorUnlocked
* DoorLocked
* FaceVerificationFailed
* DeviceOffline
* AccessDenied

RULE:
Events are immutable and used for:

* Monitoring
* Automation
* Audit history

---

# AUTOMATION ENGINE

MODEL:
IF-THEN rule system

TRIGGERS:

* Time based
* Event based
* Condition based

ACTIONS:

* Lock door
* Unlock door (restricted)
* Send notification
* Trigger alarm

SAFETY RULES:

* No infinite loops
* Execution depth limits
* All actions are audited

---

# FACE VERIFICATION

* Pluggable provider (mock or real AI)
* Runs only when required
* Executed last in unlock pipeline

RULES:

* Cached per session for short time
* Stored embeddings are encrypted

---

# AUDIT SYSTEM

AUDIT LOGS ARE:

* Immutable
* Append only
* Scoped to Home

REQUIRED DATA:

* User ID
* Device ID
* Action
* Timestamp
* Result
* Metadata

---

# DATABASE STRATEGY

* EF Core ORM
* One DbContext per module
* Schema per module (single database)
* PostgreSQL production recommended
* SQLite for testing

---

# DEVICE CONSTRAINTS

* Max 20 devices per Home (default)
* Device must be provisioned before activation
* Device cannot exist without Home
* Devices cannot act independently

---

# SESSION AND TOKEN RULES

* JWT expiry: 15 minutes
* Refresh token: 30 day rolling
* Max trusted devices per user: 5
* Session stores device + IP metadata
* Token rotation required

---

# SYSTEM INVARIANTS

* Every device belongs to exactly one Home
* Every action is scoped to Home
* No cross-home data leakage
* Every unlock attempt is logged
* No device can bypass backend validation

---

# MVP DEFINITION

FIRST WORKING SYSTEM MUST SUPPORT:

1. User registration and login
2. Home creation
3. Device registration or simulator
4. Access assignment
5. Unlock request flow
6. MQTT command execution
7. Audit logging
8. Basic monitoring

