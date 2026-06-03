# VERIXORA ITERATION 3 – SMART LOCK CONTROL & UNLOCK PIPELINE (FINAL VERSION)

---

## ITERATION OBJECTIVE

Implement smart lock control (lock/unlock/emergency lock), the full 10-step unlock decision pipeline, idempotency for all device commands, authorization caching, unlock-specific rate limiting, MQTT command publishing, and integration event contracts.

By the end of Iteration 3:

- Lock, unlock, and emergency lock commands work via MQTT.
- Full 10-step unlock pipeline executes in fixed order (cheap to expensive).
- Schedule validation runs early (step 3).
- API key authentication bypasses session check (step 2).
- Idempotency keys prevent duplicate command execution.
- Authorization decisions cached per session for 1 minute.
- API key evaluations are NOT cached.
- Unlock burst rate limit active (5 req / 10 sec per user).
- Pipeline completes within 200ms p95 (excluding MQTT round-trip).
- Integration event contracts defined for SmartLocks module.
- Mobile app supports lock/unlock with real-time status via SignalR.
- Web Portal supports admin lock management.
- Offline unlock is explicitly NOT supported.

---

## DURATION

3–4 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                          |
| ----------------- | ---------------------------------------------- |
| Backend Developer | SmartLocks module, unlock pipeline, MQTT       |
| Mobile Developer  | Lock/unlock UI, SignalR integration            |
| Web Developer     | Lock management page, admin override           |
| IoT Developer     | ESP32 lock actuator firmware, MQTT client      |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**SmartLocks.Domain:**
- `SmartLock` aggregate root:
  - `Id` (Guid)
  - `DeviceId` (Guid)
  - `HomeId` (Guid)
  - `Name` (string)
  - `State` (enum: Locked, Unlocked, Jammed, Maintenance)
  - `AutoLockTimerSeconds` (int?, null = disabled)
  - `RequireFaceVerification` (bool)
  - `LastLockedAt` (DateTimeOffset?)
  - `LastUnlockedAt` (DateTimeOffset?)
- `LockCommand` value object:
  - `CommandId` (Guid, idempotency key)
  - `CommandType` (enum: Lock, Unlock, EmergencyLock)
  - `RequestedBy` (Guid, UserId)
  - `AuthType` (enum: JWT, ApiKey)
  - `RequestedAt` (DateTimeOffset)
  - `Status` (enum: Pending, Executed, Failed, Duplicate)
- `UnlockRequest` entity:
  - `Id` (Guid)
  - `SmartLockId` (Guid)
  - `UserId` (Guid?)
  - `ApiKeyId` (Guid?)
  - `PipelineResult` (string, JSON of step results)
  - `Passed` (bool)
  - `FailureReason` (string?)
  - `CompletedAt` (DateTimeOffset)
  - `DurationMs` (int)

**Domain Events:**
- `DoorLocked`
- `DoorUnlocked`
- `DoorEmergencyLocked`
- `UnlockAttempted`
- `UnlockDenied`
- `UnlockPipelineCompleted`
- `LockCommandExecuted`
- `LockCommandDuplicate`

**Domain Services:**
- `IUnlockPipeline` – orchestrates 10 steps.
- `IIdempotencyService` – checks and stores idempotency keys.

### 1.2 Backend – Unlock Pipeline Steps

Fixed order, cheap to expensive:

| Step | Name | Module | Description |
|------|------|--------|-------------|
| 1 | JwtOrApiKeyValidation | Identity | Validates JWT or API key |
| 2 | SessionValidation | Identity | Validates session (skipped for API keys) |
| 3 | ScheduleValidation | Authorization | Checks user schedule/time restrictions |
| 4 | UserStatusValidation | Identity | Checks user is active, not locked out |
| 5 | RoleValidation | Authorization | Checks user has a valid role |
| 6 | PermissionValidation | Authorization | Checks user has unlock permission |
| 7 | HomeLevelAccess | Identity | Checks user belongs to the lock's Home |
| 8 | DeviceLevelAccess | Devices | Checks device assigned to user/Home |
| 9 | DeviceHealthValidation | Devices | Checks battery, signal, status |
| 10 | FaceVerification | FaceVerification | If required by lock policy (mock in MVP) |

Each step returns `PipelineStepResult { Passed, FailureReason }`.

### 1.3 Backend – Application Layer

**SmartLocks.Application – Commands:**
- `UnlockDoorCommand` → `UnlockDoorHandler` + `UnlockDoorValidator`
- `LockDoorCommand` → `LockDoorHandler` + `LockDoorValidator`
- `EmergencyLockCommand` → `EmergencyLockHandler` + `EmergencyLockValidator`
- `UpdateAutoLockTimerCommand` → `UpdateAutoLockTimerHandler`

**SmartLocks.Application – Queries:**
- `GetSmartLockByIdQuery` → `GetSmartLockByIdHandler`
- `GetSmartLocksByHomeQuery` → `GetSmartLocksByHomeHandler`
- `GetSmartLockStatusQuery` → `GetSmartLockStatusHandler`
- `GetUnlockHistoryQuery` → `GetUnlockHistoryHandler`

**SmartLocks.Application – Services:**
- `IUnlockPipelineService` – orchestrates the 10 steps.
- `IMqttCommandPublisher` – publishes commands to MQTT broker.

**SmartLocks.Application – Decorators:**
- `IdempotencyCommandDecorator` – wraps command handlers, checks idempotency store.
- `AuthorizationCacheDecorator` – wraps authorization checks, caches results (skipped for API keys).

### 1.4 Backend – Infrastructure Layer

**SmartLocks.Infrastructure:**
- `SmartLocksDbContext` with `DbSet<SmartLock>`, `DbSet<LockCommand>`, `DbSet<UnlockRequest>`.
- Entity configurations:
  - `SmartLockConfiguration`
  - `LockCommandConfiguration`
  - `UnlockRequestConfiguration`
- Repositories:
  - `SmartLockRepository`
  - `UnlockRequestRepository`
- Services:
  - `MqttCommandPublisher` – publishes to MQTT topics.
  - `UnlockPipelineService` – executes 10 steps via injected step services.
- Idempotency:
  - `IdempotencyStore` – EF Core-backed store (24-hour retention).
  - Background job to clean expired idempotency keys.

### 1.5 Backend – Presentation Layer

**SmartLocks.Presentation – Controllers:**
- `SmartLockController`:
  - `POST /api/v1/smartlocks/{id}/unlock` – unlock door (with Idempotency-Key header).
  - `POST /api/v1/smartlocks/{id}/lock` – lock door.
  - `POST /api/v1/smartlocks/{id}/emergency-lock` – emergency lock (admin only).
  - `GET /api/v1/smartlocks` – list smart locks by home.
  - `GET /api/v1/smartlocks/{id}` – get smart lock details.
  - `GET /api/v1/smartlocks/{id}/status` – get current state.
  - `GET /api/v1/smartlocks/{id}/history` – get unlock history.
  - `PUT /api/v1/smartlocks/{id}/auto-lock` – update auto-lock timer.

### 1.6 Backend – Contracts (including Integration Events)

**SmartLocks.Contracts:**
- `Requests/`: `UnlockRequest`, `LockRequest`, `EmergencyLockRequest`, `UpdateAutoLockRequest`.
- `Responses/`: `SmartLockResponse`, `SmartLockStatusResponse`, `UnlockHistoryResponse`, `UnlockResultResponse`.
- `IntegrationEvents/`:
  - `DoorUnlockedIntegrationEvent`
  - `DoorLockedIntegrationEvent`
  - `UnlockDeniedIntegrationEvent`
  - `UnlockPipelineCompletedIntegrationEvent`

### 1.7 ApiHost Updates

- Add unlock burst rate limit policy: 5 requests per 10 seconds per user.
- Register `IdempotencyMiddleware` (checks `Idempotency-Key` header on state-changing endpoints).
- Wire SignalR hub for real-time lock status updates.
- Register `AuthorizationCacheDecorator` in DI (skip cache for API keys).
- Register `IdempotencyCommandDecorator` in DI.

### 1.8 Mobile App

- **Lock Control Screen:** lock/unlock button, status indicator.
- **Lock History Screen:** recent unlock/lock events.
- **SignalR Service:** real-time status updates.
- **Auto-Lock Settings:** configure timer.
- **No offline unlock:** UI disables lock controls when connectivity is lost.

### 1.9 Web Portal

- **Lock Management Page:** list all locks, current state.
- **Lock Detail View:** lock info, history, settings.
- **Admin Override Controls:** emergency lock.
- **Real-time Status:** SignalR integration.

---

## PHASE 2: IMPLEMENTATION

### 2.1 SmartLocks.Domain (New Files)
```
SmartLocks.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- SmartLock.cs
|   +-- UnlockRequest.cs
+-- Enums/
|   +-- LockState.cs
|   +-- LockCommandType.cs
|   +-- LockCommandStatus.cs
|   +-- AuthType.cs
+-- ValueObjects/
|   +-- LockCommand.cs
|   +-- PipelineStepResult.cs
+-- Events/
|   +-- DoorLocked.cs
|   +-- DoorUnlocked.cs
|   +-- DoorEmergencyLocked.cs
|   +-- UnlockAttempted.cs
|   +-- UnlockDenied.cs
|   +-- UnlockPipelineCompleted.cs
|   +-- LockCommandExecuted.cs
|   +-- LockCommandDuplicate.cs
+-- Services/
    +-- IUnlockPipeline.cs
    +-- IIdempotencyService.cs
```

### 2.2 SmartLocks.Application (New Files)
```
SmartLocks.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- UnlockDoor/
|   |   +-- UnlockDoorCommand.cs
|   |   +-- UnlockDoorHandler.cs
|   |   +-- UnlockDoorValidator.cs
|   +-- LockDoor/
|   |   +-- LockDoorCommand.cs
|   |   +-- LockDoorHandler.cs
|   |   +-- LockDoorValidator.cs
|   +-- EmergencyLock/
|   |   +-- EmergencyLockCommand.cs
|   |   +-- EmergencyLockHandler.cs
|   |   +-- EmergencyLockValidator.cs
|   +-- UpdateAutoLockTimer/
|       +-- UpdateAutoLockTimerCommand.cs
|       +-- UpdateAutoLockTimerHandler.cs
+-- Queries/
|   +-- GetSmartLockById/
|   |   +-- GetSmartLockByIdQuery.cs
|   |   +-- GetSmartLockByIdHandler.cs
|   +-- GetSmartLocksByHome/
|   |   +-- GetSmartLocksByHomeQuery.cs
|   |   +-- GetSmartLocksByHomeHandler.cs
|   +-- GetSmartLockStatus/
|   |   +-- GetSmartLockStatusQuery.cs
|   |   +-- GetSmartLockStatusHandler.cs
|   +-- GetUnlockHistory/
|       +-- GetUnlockHistoryQuery.cs
|       +-- GetUnlockHistoryHandler.cs
+-- Pipeline/
|   +-- IUnlockPipelineService.cs
|   +-- UnlockPipelineService.cs
|   +-- Steps/
|       +-- JwtOrApiKeyValidationStep.cs
|       +-- SessionValidationStep.cs
|       +-- ScheduleValidationStep.cs
|       +-- UserStatusValidationStep.cs
|       +-- RoleValidationStep.cs
|       +-- PermissionValidationStep.cs
|       +-- HomeLevelAccessStep.cs
|       +-- DeviceLevelAccessStep.cs
|       +-- DeviceHealthValidationStep.cs
|       +-- FaceVerificationStep.cs
+-- Decorators/
|   +-- IdempotencyCommandDecorator.cs
|   +-- AuthorizationCacheDecorator.cs
+-- Services/
|   +-- IMqttCommandPublisher.cs
+-- DTOs/
|   +-- SmartLockDto.cs
|   +-- SmartLockStatusDto.cs
|   +-- UnlockHistoryDto.cs
|   +-- UnlockResultDto.cs
|   +-- PipelineStepResultDto.cs
+-- Interfaces/
    +-- ISmartLockRepository.cs
    +-- IUnlockRequestRepository.cs
```

### 2.3 SmartLocks.Infrastructure (New Files)
```
SmartLocks.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- SmartLocksDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- SmartLockConfiguration.cs
|   |   +-- LockCommandConfiguration.cs
|   |   +-- UnlockRequestConfiguration.cs
|   +-- Repositories/
|   |   +-- SmartLockRepository.cs
|   |   +-- UnlockRequestRepository.cs
+-- Services/
|   +-- MqttCommandPublisher.cs
|   +-- UnlockPipelineService.cs
+-- Idempotency/
|   +-- IdempotencyStore.cs
|   +-- IdempotencyCleanupJob.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 SmartLocks.Presentation (New Files)
```
SmartLocks.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
|   +-- SmartLockController.cs
+-- Hubs/
    +-- LockStatusHub.cs
```

### 2.5 SmartLocks.Contracts (New Files)
```
SmartLocks.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- UnlockRequest.cs
|   +-- LockRequest.cs
|   +-- EmergencyLockRequest.cs
|   +-- UpdateAutoLockRequest.cs
+-- Responses/
|   +-- SmartLockResponse.cs
|   +-- SmartLockStatusResponse.cs
|   +-- UnlockHistoryResponse.cs
|   +-- UnlockResultResponse.cs
+-- IntegrationEvents/
    +-- DoorUnlockedIntegrationEvent.cs
    +-- DoorLockedIntegrationEvent.cs
    +-- UnlockDeniedIntegrationEvent.cs
    +-- UnlockPipelineCompletedIntegrationEvent.cs
```

### 2.6 ApiHost (Modified Files)
```
ApiHost/
+-- Program.cs (updated)
|   +-- Add unlock burst rate limit policy
|   +-- Register IdempotencyMiddleware
|   +-- Map SignalR LockStatusHub
|   +-- Register API key authentication handler
+-- Middleware/
|   +-- IdempotencyMiddleware.cs
+-- Extensions/
    +-- ServiceCollectionExtensions.cs (updated)
    +-- ApplicationBuilderExtensions.cs (updated)
```

---

## PHASE 3: INTEGRATION

- Wire SmartLocks module DI into ApiHost.
- Run EF Core migrations for SmartLocks schema.
- Configure unlock burst rate limit policy.
- Register idempotency decorator for all command handlers.
- Register authorization cache decorator (skip cache for API keys).
- Wire SignalR hub for real-time lock status.
- Connect MQTT publisher to broker.
- Mobile app connects to SignalR hub for status updates.
- Web portal connects to SignalR hub.
- CI pipeline runs all new tests including contract tests and performance SLA check.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `SmartLock_Unlock_ShouldChangeState`
- `SmartLock_Lock_ShouldChangeState`
- `SmartLock_EmergencyLock_AdminOnly`
- `LockCommand_Duplicate_ShouldBeDetected`
- `UnlockRequest_PipelinePassed_ShouldRecord`
- `UnlockRequest_PipelineFailed_ShouldRecordReason`
- `LockCommand_ApiKeyAuth_ShouldRecordAuthType`

### 4.2 Application Tests
- `Unlock_ValidRequest_ShouldSucceed`
- `Unlock_InvalidSession_ShouldFail`
- `Unlock_OutsideSchedule_ShouldFail`
- `Unlock_LowBattery_ShouldFail`
- `Unlock_ShouldGenerateAuditLog`
- `Unlock_DuplicateIdempotencyKey_ShouldReturnSameResult`
- `Unlock_RateLimitExceeded_ShouldReturn429`
- `Unlock_FaceVerificationRequired_ShouldRunLast`
- `Unlock_ApiKeyAuth_ShouldBypassSessionCheck`
- `Unlock_ApiKeyAuth_ShouldNotCacheAuthorization`
- `Lock_ShouldSucceedAndAudit`
- `EmergencyLock_AdminOnly_ShouldSucceed`
- `EmergencyLock_NonAdmin_ShouldFail`
- `Pipeline_AllSteps_ShouldExecuteInOrder`
- `OfflineUnlock_ShouldNotBeSupported`

### 4.3 Integration Tests
- `SmartLockController_Unlock_ShouldReturn200`
- `SmartLockController_DuplicateUnlock_ShouldReturn200_SameResult`
- `SmartLockController_RateLimit_ShouldReturn429`
- `SmartLockController_ApiKeyUnlock_ShouldBypassSession`
- `MqttPublisher_ShouldSendCommand`
- `IdempotencyStore_ShouldPersistAndRetrieve`
- `AuthorizationCache_ShouldCacheAndInvalidate`
- `AuthorizationCache_ApiKey_ShouldNotCache`

### 4.4 Contract Tests
- `SmartLocks_Contracts_ShouldMatchPublishedEvents`
- `SmartLocks_IntegrationEvents_ShouldContainRequiredFields`

### 4.5 Performance Tests
- `UnlockPipeline_ShouldCompleteWithin200ms`
- `UnlockPipeline_UnderConcurrentLoad_ShouldMeetSLA`
- `UnlockPipeline_ApiKeyAuth_ShouldMeetSLA`

### 4.6 E2E Tests (Mobile)
- Tap unlock → pipeline executes → door unlocks → status updates via SignalR.
- Tap unlock twice → duplicate response, door unlocks once.
- API key unlock → bypasses session → door unlocks → status updates.
- Connectivity lost → unlock button disabled.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Configure MQTT broker for staging.
- Deploy SignalR hub.
- Build mobile app with lock control.
- Build web portal with lock management.
- Run performance benchmarks.

---

## ACCEPTANCE CRITERIA

- [ ] Lock, unlock, and emergency lock commands work.
- [ ] 10-step pipeline executes in correct order.
- [ ] Schedule validation at step 3 (fails fast).
- [ ] API key authentication bypasses session check (step 2).
- [ ] Idempotency keys prevent duplicate execution.
- [ ] Authorization cached per session for 1 minute (not for API keys).
- [ ] Unlock burst rate limit enforced (5/10sec).
- [ ] Pipeline completes within 200ms p95.
- [ ] SignalR pushes real-time status updates.
- [ ] Offline unlock not supported.
- [ ] Integration event contracts defined.
- [ ] Contract tests pass.
- [ ] Mobile app completes unlock flow.
- [ ] Web portal supports admin lock management.
- [ ] All tests pass including performance SLA.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 3 Coverage                    |
| ---------------------------------- | --------------------------------------- |
| Smart lock control                 | Lock, unlock, emergency lock            |
| 10-step unlock pipeline            | Full implementation, fixed order        |
| API key auth bypass session        | Step 2 skipped for API keys             |
| Schedule validation early          | Step 3 in pipeline                      |
| Idempotency keys (ADR-016)         | Decorator + store, 24h retention        |
| Authorization caching (ADR-019)    | Decorator, 1min TTL, event invalidation |
| API key no cache                   | API key evaluations always fresh        |
| Rate limiting – unlock burst       | 5 req/10 sec per user                   |
| Performance SLA (200ms p95)        | Benchmarked and enforced                |
| SignalR real-time updates          | LockStatusHub                           |
| MQTT command execution             | MqttCommandPublisher                    |
| Offline unlock prohibition         | Enforced, no offline capability         |
| Integration event contracts        | SmartLocks.Contracts/IntegrationEvents  |

---

**ITERATION 3 COMPLETE.**

All improvements integrated:
- Integration event contracts (SmartLocks)
- API key auth bypass session
- API key auth no caching
- Offline unlock prohibition