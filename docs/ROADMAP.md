# VERIXORA Roadmap

## Phase 1: Shared Kernel Foundation

Goal: establish reliable domain primitives used across all modules.

Deliverables:

- Entity base class
- ValueObject base class
- Domain event abstraction
- Domain exception model
- Guard clause helpers
- Error code standard
- Initial architecture tests where practical
- Architecture validation script requirements and first implementation

## Phase 2: Domain Modeling

Goal: define the core business model before infrastructure details.

Priority modules:

1. Identity
2. Authorization
3. Sessions
4. Devices
5. SmartLocks
6. Provisioning
7. AuditLogs
8. Monitoring
9. Security
10. FaceVerification
11. Notifications
12. Automation
13. Reports

Domain modeling should include entities, value objects, aggregates, invariants, and domain events.

The initial module behavior and test targets are tracked in [BACKEND-USE-CASES-AND-TESTS.md](BACKEND-USE-CASES-AND-TESTS.md).

## Phase 3: Application Layer

Goal: expose use cases through commands and queries.

Deliverables:

- Command and query contracts
- Handlers for core workflows
- Validation around application requests
- Module-level service abstractions
- Authorization checks at use-case boundaries

## Phase 4: Infrastructure

Goal: connect application behavior to storage and external systems.

Deliverables:

- EF Core persistence
- SQL Server development database
- SQLite in-memory testing provider
- Central migration strategy
- Repository or persistence abstractions where needed
- Redis caching and session-state foundations
- MQTT integration contracts
- Device command dispatching
- Audit log persistence

## Phase 5: API And Frontend Integration

Goal: provide usable system workflows through APIs and later a frontend.

Deliverables:

- API endpoints per module
- Authentication endpoints
- Access control endpoints
- Device and smart lock endpoints
- Monitoring endpoints
- Audit log endpoints
- Frontend integration contracts

## Phase 6: IoT Integration And Testing

Goal: validate real device behavior and production readiness.

Deliverables:

- ESP32 provisioning flow
- MQTT topic contracts
- Device heartbeat handling
- Lock/unlock command workflow
- Unauthorized access simulation
- Integration and end-to-end tests
- Security testing
- Performance testing
- Production checklist
