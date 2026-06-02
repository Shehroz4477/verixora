# FINAL VERIXORA REQUIREMENTS ASSESSMENT (CLEAN VERSION)

---

# SYSTEM OVERVIEW

VERIXORA is an IoT-based smart security and access control platform.

It provides secure management of:

* users
* homes
* devices
* smart locks
* access control decisions
* audit logging

Backend is the system of record for all decisions.

---

# REQUIREMENT CLASSIFICATION MODEL

All requirements are classified as:

* MUST HAVE (core system functionality)
* SHOULD HAVE (important but not critical)
* NICE TO HAVE (future enhancements)

---

# MUST HAVE REQUIREMENTS (MVP CORE)

## Identity System

* User registration
* Login with JWT authentication
* Session management
* Refresh token rotation

## Home System (Tenant Core)

* Create Home (tenant)
* Assign users to Home
* Assign roles per Home

## Device System

* Register IoT devices (ESP32)
* Assign device to Home
* Device status tracking

## Smart Lock Control

* Lock door
* Unlock door (via validation pipeline)
* Device command execution via MQTT

## Authorization System

* Role based access control
* Permission validation
* Device-level access rules

## Audit System

* Record all security actions
* Immutable audit logs

---

# SHOULD HAVE REQUIREMENTS

* Monitoring dashboard
* Real-time updates via SignalR
* Notification system (email only initially)
* Basic scheduling rules for guest access

---

# NICE TO HAVE REQUIREMENTS

* Face verification system
* Automation engine
* Advanced reporting
* SMS and push notifications
* PDF export
* Advanced security analytics

---

# SYSTEM MVP VALUE LOOP

A valid MVP must support:

1. User registers and logs in
2. User belongs to a Home
3. Device is registered to Home
4. Owner assigns access to user
5. User requests unlock
6. Backend validates request
7. MQTT command sent to device
8. Device executes action
9. Audit log is recorded

---

# SECURITY REQUIREMENTS

* JWT must expire in 15 minutes
* Refresh tokens must rotate
* Sessions must be tracked per device
* Unknown devices require verification
* All actions must be authorized
* All security actions must be logged

---

# DEVICE REQUIREMENTS

* Devices must be provisioned before use
* Devices cannot act independently
* Devices may be offline
* Commands must be idempotent

---

# PERFORMANCE EXPECTATIONS

* Unlock decision pipeline must complete within acceptable real-time threshold
* MQTT command delivery must be async and retry-safe
* System must handle concurrent access requests safely

---

# SYSTEM BOUNDARIES

VERIXORA does NOT include:

* hardware manufacturing
* cloud identity provider dependency
* external biometric system dependency (optional only)
* distributed microservices architecture (not in MVP phase)

---

# USER JOURNEYS

## Owner Journey

* creates Home
* adds devices
* assigns users
* controls access

## Guest Journey

* receives access
* uses door within schedule
* has limited permissions

## Technician Journey

* manages device health
* performs maintenance only

---

# DEFERRED REQUIREMENTS

* SMS integration
* push notifications
* full automation engine
* production face recognition
* multi-region deployment
* microservices migration
* advanced reporting suite
