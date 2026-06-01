# VERIXORA

VERIXORA is an IoT smart security and access control platform for managing users, roles, doors, smart locks, trusted devices, access decisions, monitoring, and audit logs.

The backend is the authority for every access decision. Smart lock devices are passive executors and only run backend-approved commands.

## Architecture

- ASP.NET Core 8
- Modular monolith
- Clean Architecture
- Vertical Slice Architecture
- CQRS
- Domain events and integration events
- One DbContext per module

## Core Modules

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

## Documentation

Start with [docs/README.md](docs/README.md).

## Status

Early architecture and domain foundation phase.
