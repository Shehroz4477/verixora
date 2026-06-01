# VERIXORA Architecture Decisions

This document records current architecture decisions. These decisions should guide code and future documentation.

## ADR-001: Use Modular Monolith First

Decision: Build VERIXORA as a modular monolith.

Reason:

- One deployable backend is easier to develop, test, and deploy.
- Module boundaries can still be enforced.
- The project avoids premature distributed-system complexity.
- The architecture can evolve later if specific modules need independent scaling.

## ADR-002: Use Clean Architecture Inside Modules

Decision: Each module follows Domain, Application, Infrastructure, and Presentation layers.

Reason:

- Domain rules stay independent of frameworks.
- Application use cases can be tested without infrastructure.
- Infrastructure can change without rewriting business rules.
- Presentation remains thin.

## ADR-003: Backend Owns All Access Decisions

Decision: The backend is the only authority for unlock decisions.

Reason:

- Physical security must not depend on device-side trust.
- Compromised devices should not be able to grant access.
- Audit and monitoring require centralized decision records.

## ADR-004: Devices Are Passive Executors

Decision: ESP32 devices only execute backend-approved commands and report telemetry.

Reason:

- Keeps device firmware simpler.
- Reduces attack surface.
- Makes security policy changes backend-driven.

## ADR-005: Use MQTT For IoT Communication

Decision: MQTT is the device command and telemetry protocol.

Reason:

- MQTT is lightweight and common in IoT systems.
- ESP32 support is strong.
- It works well for lock commands, telemetry, and heartbeat events.

## ADR-006: Support Device Simulation

Decision: A simulator is a first-class development and demo target.

Reason:

- Development should not be blocked by hardware.
- Testing can run without physical devices.
- Viva or demo risk is reduced.

## ADR-007: Use Schema-Per-Module Initially

Decision: Each module owns its DbContext and schema inside one physical database.

Reason:

- Keeps module ownership visible.
- Avoids operational complexity of many databases.
- Allows future database separation if needed.

## ADR-008: Audit Logs Are Immutable

Decision: Audit logs are append-only through normal application paths.

Reason:

- Security investigations require trustable history.
- Access-control systems must preserve traceability.
- Deleting or editing audit records weakens the domain.

## ADR-009: Build Backend Before Frontends

Decision: Backend domain, application workflows, and APIs come before Web Portal and Mobile App implementation.

Reason:

- Frontends depend on stable API contracts.
- The core project value is the secure access decision engine.
- Testing is easier when backend workflows are proven first.

## ADR-010: Face Verification Is Pluggable

Decision: Face verification must be represented behind an application contract.

Reason:

- A mock provider can support early development.
- A real provider can be added later.
- The unlock pipeline should not depend directly on ML or vendor details.

## ADR-011: Target .NET 8

Decision: All backend projects target `.NET 8`.

Reason:

- The project specification requires ASP.NET Core 8.
- .NET 8 is a long-term support release.
- University and deployment environments are more likely to support .NET 8 consistently.
- Package choices should be aligned with the .NET 8 ecosystem.
