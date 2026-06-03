# VERIXORA ITERATION 4 – AUTHORIZATION, AUDIT & SECURITY HARDENING

---

## ITERATION OBJECTIVE

Implement RBAC/PBAC enforcement engine, immutable encrypted audit logs, signed firmware update handling, and complete the security hardening of the system.

By the end of Iteration 4:

- Full RBAC and PBAC policy evaluation engine.
- DENY always overrides ALLOW.
- Immutable, append-only audit logs encrypted at column level.
- All security-sensitive actions generate audit entries.
- Firmware update binaries are digitally signed and verified.
- Authorization cache invalidation on role/permission changes.
- Web Portal shows audit log viewer.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                          |
| ----------------- | ---------------------------------------------- |
| Backend Developer | Authorization module, AuditLogs module, Security module |
| Mobile Developer  | No major changes (minor audit visibility if needed) |
| Web Developer     | Audit log viewer, dashboard                    |
| IoT Developer     | ESP32 firmware signature verification          |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**Authorization.Domain:**
- `Role` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `Name` (string)
  - `Description` (string)
  - `IsSystemRole` (bool)
  - `CreatedAt` (DateTimeOffset)
- `Permission` entity:
  - `Id` (Guid)
  - `Name` (string)
  - `Resource` (string)
  - `Action` (string)
  - `Description` (string)
- `RolePermission` entity:
  - `Id` (Guid)
  - `RoleId` (Guid)
  - `PermissionId` (Guid)
- `Policy` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `Name` (string)
  - `Type` (enum: Allow, Deny)
  - `Resource` (string)
  - `Action` (string)
  - `Conditions` (string, JSON)
  - `Priority` (int)
  - `IsActive` (bool)
- `DeviceAccess` entity:
  - `Id` (Guid)
  - `DeviceId` (Guid)
  - `UserId` (Guid)
  - `CanUnlock` (bool)
  - `CanLock` (bool)
  - `ScheduleStart` (TimeOnly?)
  - `ScheduleEnd` (TimeOnly?)
  - `DaysOfWeek` (string, JSON array)
- `ScheduleRestriction` value object:
  - `StartTime` (TimeOnly)
  - `EndTime` (TimeOnly)
  - `DaysOfWeek` (DayOfWeek[])

**AuditLogs.Domain:**
- `AuditLog` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `UserId` (Guid?)
  - `DeviceId` (Guid?)
  - `Action` (string)
  - `Resource` (string)
  - `Result` (enum: Success, Failure, Denied)
  - `Metadata` (string, JSON, encrypted)
  - `IpAddress` (string)
  - `UserAgent` (string)
  - `Timestamp` (DateTimeOffset)
- `AuditLogEntry` value object (for batch operations):
  - `Action` (string)
  - `Timestamp` (DateTimeOffset)

**Security.Domain:**
- `FirmwarePackage` aggregate root:
  - `Id` (Guid)
  - `Version` (string)
  - `FileName` (string)
  - `FileHash` (string, SHA256)
  - `DigitalSignature` (string)
  - `SignedBy` (string)
  - `SignedAt` (DateTimeOffset)
  - `IsActive` (bool)
  - `CreatedAt` (DateTimeOffset)
- `FirmwareUpdateRecord` entity:
  - `Id` (Guid)
  - `DeviceId` (Guid)
  - `FirmwarePackageId` (Guid)
  - `Status` (enum: Queued, Downloading, Verifying, Installing, Completed, Failed)
  - `StartedAt` (DateTimeOffset?)
  - `CompletedAt` (DateTimeOffset?)

**Domain Events:**
- `RoleCreated`
- `RolePermissionChanged` (triggers auth cache invalidation)
- `PolicyCreated`
- `PolicyUpdated`
- `PolicyDeleted`
- `AccessEvaluated`
- `AccessDenied`
- `AuditLogCreated`
- `FirmwarePackageSigned`
- `FirmwareUpdateInitiated`
- `FirmwareUpdateCompleted`
- `FirmwareSignatureVerified`
- `FirmwareSignatureFailed`

**Domain Services:**
- `IAccessEvaluationEngine` – evaluates RBAC + PBAC + device constraints.
- `IFirmwareSigningService` – signs firmware binaries.

### 1.2 Backend – Application Layer

**Authorization.Application – Commands:**
- `CreateRoleCommand` → `CreateRoleHandler` + `CreateRoleValidator`
- `UpdateRoleCommand` → `UpdateRoleHandler`
- `DeleteRoleCommand` → `DeleteRoleHandler`
- `AssignPermissionCommand` → `AssignPermissionHandler`
- `RemovePermissionCommand` → `RemovePermissionHandler`
- `CreatePolicyCommand` → `CreatePolicyHandler` + `CreatePolicyValidator`
- `UpdatePolicyCommand` → `UpdatePolicyHandler`
- `DeletePolicyCommand` → `DeletePolicyHandler`
- `SetDeviceAccessCommand` → `SetDeviceAccessHandler` + `SetDeviceAccessValidator`
- `EvaluateAccessQuery` → `EvaluateAccessHandler`

**Authorization.Application – Queries:**
- `GetRolesByHomeQuery` → `GetRolesByHomeHandler`
- `GetPoliciesByHomeQuery` → `GetPoliciesByHomeHandler`
- `GetDeviceAccessQuery` → `GetDeviceAccessHandler`
- `GetUserPermissionsQuery` → `GetUserPermissionsHandler`

**Authorization.Application – Services:**
- `IAccessEvaluationEngine` (implemented in Infrastructure)
- `IAuthorizationCacheService` – manages the 1-minute cache.

**AuditLogs.Application – Commands:**
- `CreateAuditLogCommand` → `CreateAuditLogHandler`
- `BatchCreateAuditLogCommand` → `BatchCreateAuditLogHandler`

**AuditLogs.Application – Queries:**
- `GetAuditLogsByHomeQuery` → `GetAuditLogsByHomeHandler`
- `GetAuditLogsByUserQuery` → `GetAuditLogsByUserHandler`
- `GetAuditLogsByDeviceQuery` → `GetAuditLogsByDeviceHandler`
- `GetAuditLogsByDateRangeQuery` → `GetAuditLogsByDateRangeHandler`

**Security.Application – Commands:**
- `CreateFirmwarePackageCommand` → `CreateFirmwarePackageHandler`
- `SignFirmwarePackageCommand` → `SignFirmwarePackageHandler`
- `InitiateFirmwareUpdateCommand` → `InitiateFirmwareUpdateHandler`
- `CompleteFirmwareUpdateCommand` → `CompleteFirmwareUpdateHandler`
- `VerifyFirmwareSignatureCommand` → `VerifyFirmwareSignatureHandler`

**Security.Application – Queries:**
- `GetFirmwarePackagesQuery` → `GetFirmwarePackagesHandler`
- `GetFirmwareUpdateStatusQuery` → `GetFirmwareUpdateStatusHandler`

### 1.3 Backend – Infrastructure Layer

**Authorization.Infrastructure:**
- `AuthorizationDbContext` with `DbSet<Role>`, `DbSet<Permission>`, `DbSet<RolePermission>`, `DbSet<Policy>`, `DbSet<DeviceAccess>`.
- Entity configurations for all entities.
- Repositories: `RoleRepository`, `PolicyRepository`, `DeviceAccessRepository`, `PermissionRepository`.
- `AccessEvaluationEngine` – implements the strict evaluation order.
- `AuthorizationCacheService` – in-memory cache with event invalidation.

**AuditLogs.Infrastructure:**
- `AuditLogsDbContext` with `DbSet<AuditLog>`.
- `AuditLogConfiguration` – configures encrypted metadata column.
- `AuditLogRepository` – append-only, no update/delete methods.
- `EncryptionConverter` for metadata column.

**Security.Infrastructure:**
- `SecurityDbContext` with `DbSet<FirmwarePackage>`, `DbSet<FirmwareUpdateRecord>`.
- Entity configurations.
- Repositories: `FirmwarePackageRepository`, `FirmwareUpdateRepository`.
- `FirmwareSigningService` – RSA/ECDSA signing.
- `FirmwareVerificationService` – verifies signature.

### 1.4 Backend – Presentation Layer

**Authorization.Presentation – Controllers:**
- `RoleController`:
  - `POST /api/v1/roles`
  - `GET /api/v1/roles`
  - `GET /api/v1/roles/{id}`
  - `PUT /api/v1/roles/{id}`
  - `DELETE /api/v1/roles/{id}`
  - `POST /api/v1/roles/{id}/permissions`
  - `DELETE /api/v1/roles/{id}/permissions/{permissionId}`
- `PolicyController`:
  - `POST /api/v1/policies`
  - `GET /api/v1/policies`
  - `GET /api/v1/policies/{id}`
  - `PUT /api/v1/policies/{id}`
  - `DELETE /api/v1/policies/{id}`
- `DeviceAccessController`:
  - `POST /api/v1/device-access`
  - `GET /api/v1/device-access`
  - `PUT /api/v1/device-access/{id}`
  - `DELETE /api/v1/device-access/{id}`

**AuditLogs.Presentation – Controllers:**
- `AuditLogController`:
  - `GET /api/v1/audit-logs`
  - `GET /api/v1/audit-logs/user/{userId}`
  - `GET /api/v1/audit-logs/device/{deviceId}`
  - `GET /api/v1/audit-logs/date-range`

**Security.Presentation – Controllers:**
- `FirmwareController`:
  - `POST /api/v1/firmware/packages`
  - `GET /api/v1/firmware/packages`
  - `POST /api/v1/firmware/packages/{id}/sign`
  - `POST /api/v1/firmware/updates`
  - `GET /api/v1/firmware/updates/{id}`
  - `POST /api/v1/firmware/updates/{id}/verify`

### 1.5 Backend – Contracts

**Authorization.Contracts:**
- `Requests/`: `CreateRoleRequest`, `UpdateRoleRequest`, `AssignPermissionRequest`, `CreatePolicyRequest`, `UpdatePolicyRequest`, `SetDeviceAccessRequest`, `EvaluateAccessRequest`.
- `Responses/`: `RoleResponse`, `PolicyResponse`, `DeviceAccessResponse`, `AccessEvaluationResponse`, `PermissionResponse`.

**AuditLogs.Contracts:**
- `Requests/`: `AuditLogFilterRequest`.
- `Responses/`: `AuditLogResponse`, `AuditLogBatchResponse`.

**Security.Contracts:**
- `Requests/`: `CreateFirmwarePackageRequest`, `SignFirmwareRequest`, `InitiateFirmwareUpdateRequest`, `VerifyFirmwareRequest`.
- `Responses/`: `FirmwarePackageResponse`, `FirmwareUpdateStatusResponse`, `FirmwareVerificationResponse`.

### 1.6 Web Portal

- **Audit Log Viewer:** filterable table by user, device, date range, action, result.
- **Role Management Page:** CRUD roles, assign permissions.
- **Policy Management Page:** CRUD policies, set priorities.
- **Device Access Page:** per-user per-device access settings.
- **Firmware Management Page:** upload, sign, view update status.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Authorization.Domain (New Files)
```
Authorization.Domain/
+-- Entities/
|   +-- Role.cs
|   +-- Permission.cs
|   +-- RolePermission.cs
|   +-- Policy.cs
|   +-- DeviceAccess.cs
+-- Enums/
|   +-- PolicyType.cs
|   +-- AccessResult.cs
+-- ValueObjects/
|   +-- ScheduleRestriction.cs
+-- Events/
|   +-- RoleCreated.cs
|   +-- RolePermissionChanged.cs
|   +-- PolicyCreated.cs
|   +-- PolicyUpdated.cs
|   +-- PolicyDeleted.cs
|   +-- AccessEvaluated.cs
|   +-- AccessDenied.cs
+-- Services/
    +-- IAccessEvaluationEngine.cs
```

### 2.2 Authorization.Application (New Files)
```
Authorization.Application/
+-- Commands/
|   +-- CreateRole/
|   |   +-- CreateRoleCommand.cs
|   |   +-- CreateRoleHandler.cs
|   |   +-- CreateRoleValidator.cs
|   +-- UpdateRole/
|   |   +-- UpdateRoleCommand.cs
|   |   +-- UpdateRoleHandler.cs
|   +-- DeleteRole/
|   |   +-- DeleteRoleCommand.cs
|   |   +-- DeleteRoleHandler.cs
|   +-- AssignPermission/
|   |   +-- AssignPermissionCommand.cs
|   |   +-- AssignPermissionHandler.cs
|   +-- RemovePermission/
|   |   +-- RemovePermissionCommand.cs
|   |   +-- RemovePermissionHandler.cs
|   +-- CreatePolicy/
|   |   +-- CreatePolicyCommand.cs
|   |   +-- CreatePolicyHandler.cs
|   |   +-- CreatePolicyValidator.cs
|   +-- UpdatePolicy/
|   |   +-- UpdatePolicyCommand.cs
|   |   +-- UpdatePolicyHandler.cs
|   +-- DeletePolicy/
|   |   +-- DeletePolicyCommand.cs
|   |   +-- DeletePolicyHandler.cs
|   +-- SetDeviceAccess/
|   |   +-- SetDeviceAccessCommand.cs
|   |   +-- SetDeviceAccessHandler.cs
|   |   +-- SetDeviceAccessValidator.cs
|   +-- EvaluateAccess/
|       +-- EvaluateAccessQuery.cs
|       +-- EvaluateAccessHandler.cs
+-- Queries/
|   +-- GetRolesByHome/
|   |   +-- GetRolesByHomeQuery.cs
|   |   +-- GetRolesByHomeHandler.cs
|   +-- GetPoliciesByHome/
|   |   +-- GetPoliciesByHomeQuery.cs
|   |   +-- GetPoliciesByHomeHandler.cs
|   +-- GetDeviceAccess/
|   |   +-- GetDeviceAccessQuery.cs
|   |   +-- GetDeviceAccessHandler.cs
|   +-- GetUserPermissions/
|       +-- GetUserPermissionsQuery.cs
|       +-- GetUserPermissionsHandler.cs
+-- Services/
|   +-- IAuthorizationCacheService.cs
+-- DTOs/
|   +-- RoleDto.cs
|   +-- PolicyDto.cs
|   +-- DeviceAccessDto.cs
|   +-- AccessEvaluationDto.cs
|   +-- PermissionDto.cs
+-- Interfaces/
    +-- IRoleRepository.cs
    +-- IPolicyRepository.cs
    +-- IDeviceAccessRepository.cs
    +-- IPermissionRepository.cs
```

### 2.3 Authorization.Infrastructure (New Files)
```
Authorization.Infrastructure/
+-- Persistence/
|   +-- AuthorizationDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- RoleConfiguration.cs
|   |   +-- PermissionConfiguration.cs
|   |   +-- RolePermissionConfiguration.cs
|   |   +-- PolicyConfiguration.cs
|   |   +-- DeviceAccessConfiguration.cs
|   +-- Repositories/
|   |   +-- RoleRepository.cs
|   |   +-- PolicyRepository.cs
|   |   +-- DeviceAccessRepository.cs
|   |   +-- PermissionRepository.cs
+-- Services/
|   +-- AccessEvaluationEngine.cs
|   +-- AuthorizationCacheService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 Authorization.Presentation (New Files)
```
Authorization.Presentation/
+-- Controllers/
    +-- RoleController.cs
    +-- PolicyController.cs
    +-- DeviceAccessController.cs
```

### 2.5 Authorization.Contracts (New Files)
```
Authorization.Contracts/
+-- Requests/
|   +-- CreateRoleRequest.cs
|   +-- UpdateRoleRequest.cs
|   +-- AssignPermissionRequest.cs
|   +-- CreatePolicyRequest.cs
|   +-- UpdatePolicyRequest.cs
|   +-- SetDeviceAccessRequest.cs
|   +-- EvaluateAccessRequest.cs
+-- Responses/
    +-- RoleResponse.cs
    +-- PolicyResponse.cs
    +-- DeviceAccessResponse.cs
    +-- AccessEvaluationResponse.cs
    +-- PermissionResponse.cs
```

### 2.6 AuditLogs.Domain (New Files)
```
AuditLogs.Domain/
+-- Entities/
|   +-- AuditLog.cs
+-- Enums/
|   +-- AuditResult.cs
+-- ValueObjects/
|   +-- AuditLogEntry.cs
+-- Events/
|   +-- AuditLogCreated.cs
+-- Services/
    +-- IAuditService.cs
```

### 2.7 AuditLogs.Application (New Files)
```
AuditLogs.Application/
+-- Commands/
|   +-- CreateAuditLog/
|   |   +-- CreateAuditLogCommand.cs
|   |   +-- CreateAuditLogHandler.cs
|   +-- BatchCreateAuditLog/
|       +-- BatchCreateAuditLogCommand.cs
|       +-- BatchCreateAuditLogHandler.cs
+-- Queries/
|   +-- GetAuditLogsByHome/
|   |   +-- GetAuditLogsByHomeQuery.cs
|   |   +-- GetAuditLogsByHomeHandler.cs
|   +-- GetAuditLogsByUser/
|   |   +-- GetAuditLogsByUserQuery.cs
|   |   +-- GetAuditLogsByUserHandler.cs
|   +-- GetAuditLogsByDevice/
|   |   +-- GetAuditLogsByDeviceQuery.cs
|   |   +-- GetAuditLogsByDeviceHandler.cs
|   +-- GetAuditLogsByDateRange/
|       +-- GetAuditLogsByDateRangeQuery.cs
|       +-- GetAuditLogsByDateRangeHandler.cs
+-- DTOs/
|   +-- AuditLogDto.cs
|   +-- AuditLogFilterDto.cs
+-- Interfaces/
    +-- IAuditLogRepository.cs
```

### 2.8 AuditLogs.Infrastructure (New Files)
```
AuditLogs.Infrastructure/
+-- Persistence/
|   +-- AuditLogsDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- AuditLogConfiguration.cs
|   +-- Repositories/
|   |   +-- AuditLogRepository.cs
|   +-- Converters/
|       +-- EncryptionConverter.cs
+-- Services/
|   +-- AuditService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.9 AuditLogs.Presentation (New Files)
```
AuditLogs.Presentation/
+-- Controllers/
    +-- AuditLogController.cs
```

### 2.10 AuditLogs.Contracts (New Files)
```
AuditLogs.Contracts/
+-- Requests/
|   +-- AuditLogFilterRequest.cs
+-- Responses/
    +-- AuditLogResponse.cs
    +-- AuditLogBatchResponse.cs
```

### 2.11 Security.Domain (New Files)
```
Security.Domain/
+-- Entities/
|   +-- FirmwarePackage.cs
|   +-- FirmwareUpdateRecord.cs
+-- Enums/
|   +-- FirmwareUpdateStatus.cs
+-- Events/
|   +-- FirmwarePackageSigned.cs
|   +-- FirmwareUpdateInitiated.cs
|   +-- FirmwareUpdateCompleted.cs
|   +-- FirmwareSignatureVerified.cs
|   +-- FirmwareSignatureFailed.cs
+-- Services/
    +-- IFirmwareSigningService.cs
```

### 2.12 Security.Application (New Files)
```
Security.Application/
+-- Commands/
|   +-- CreateFirmwarePackage/
|   |   +-- CreateFirmwarePackageCommand.cs
|   |   +-- CreateFirmwarePackageHandler.cs
|   +-- SignFirmwarePackage/
|   |   +-- SignFirmwarePackageCommand.cs
|   |   +-- SignFirmwarePackageHandler.cs
|   +-- InitiateFirmwareUpdate/
|   |   +-- InitiateFirmwareUpdateCommand.cs
|   |   +-- InitiateFirmwareUpdateHandler.cs
|   +-- CompleteFirmwareUpdate/
|   |   +-- CompleteFirmwareUpdateCommand.cs
|   |   +-- CompleteFirmwareUpdateHandler.cs
|   +-- VerifyFirmwareSignature/
|       +-- VerifyFirmwareSignatureCommand.cs
|       +-- VerifyFirmwareSignatureHandler.cs
+-- Queries/
|   +-- GetFirmwarePackages/
|   |   +-- GetFirmwarePackagesQuery.cs
|   |   +-- GetFirmwarePackagesHandler.cs
|   +-- GetFirmwareUpdateStatus/
|       +-- GetFirmwareUpdateStatusQuery.cs
|       +-- GetFirmwareUpdateStatusHandler.cs
+-- DTOs/
|   +-- FirmwarePackageDto.cs
|   +-- FirmwareUpdateStatusDto.cs
|   +-- FirmwareVerificationDto.cs
+-- Interfaces/
    +-- IFirmwarePackageRepository.cs
    +-- IFirmwareUpdateRepository.cs
```

### 2.13 Security.Infrastructure (New Files)
```
Security.Infrastructure/
+-- Persistence/
|   +-- SecurityDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- FirmwarePackageConfiguration.cs
|   |   +-- FirmwareUpdateRecordConfiguration.cs
|   +-- Repositories/
|   |   +-- FirmwarePackageRepository.cs
|   |   +-- FirmwareUpdateRepository.cs
+-- Services/
|   +-- FirmwareSigningService.cs
|   +-- FirmwareVerificationService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.14 Security.Presentation (New Files)
```
Security.Presentation/
+-- Controllers/
    +-- FirmwareController.cs
```

### 2.15 Security.Contracts (New Files)
```
Security.Contracts/
+-- Requests/
|   +-- CreateFirmwarePackageRequest.cs
|   +-- SignFirmwareRequest.cs
|   +-- InitiateFirmwareUpdateRequest.cs
|   +-- VerifyFirmwareRequest.cs
+-- Responses/
    +-- FirmwarePackageResponse.cs
    +-- FirmwareUpdateStatusResponse.cs
    +-- FirmwareVerificationResponse.cs
```

---

## PHASE 3: INTEGRATION

- Wire Authorization, AuditLogs, and Security modules DI into ApiHost.
- Run EF Core migrations for all three schemas.
- Connect audit logging to all existing modules (Identity, Devices, SmartLocks).
- Authorization cache invalidation wired to `RolePermissionChanged` events.
- Firmware signing integrated with device update flow.
- CI pipeline runs all new tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `Policy_Deny_ShouldOverrideAllow`
- `Role_AddPermission_ShouldRaiseEvent`
- `AuditLog_ShouldBeImmutable`
- `FirmwarePackage_Sign_ShouldSetSignature`

### 4.2 Application Tests
- `AssignRole_ShouldGrantAccess`
- `Guest_OutsideSchedule_ShouldFailAccess`
- `DeviceRestrictedAccess_ShouldBlock`
- `PermissionMissing_ShouldReturnDenied`
- `DenyPolicy_ShouldOverrideAllow`
- `Authorization_CachedResult_ShouldSkipReevaluation`
- `Authorization_CacheInvalidatedOnRoleChange_ShouldReevaluate`
- `AuditLog_Create_ShouldPersist`
- `AuditLog_ShouldBeImmutable_NoUpdate`
- `AuditLog_EncryptedData_ShouldBeUnreadableDirectly`
- `FirmwareUpdate_UnsignedBinary_ShouldReject`
- `FirmwareUpdate_SignedBinary_ShouldVerify`
- `FirmwareUpdate_SignatureMismatch_ShouldFail`

### 4.3 Integration Tests
- `RoleController_CRUD_ShouldWork`
- `PolicyController_CRUD_ShouldWork`
- `AuditLogController_Filter_ShouldReturnResults`
- `AuditLog_Encryption_ShouldBeTransparent`
- `FirmwareController_Sign_ShouldSucceed`

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Run all migrations.
- Verify audit logs from previous iterations are captured.
- Build web portal with audit log viewer.
- Run full validation script.

---

## ACCEPTANCE CRITERIA

- [ ] RBAC and PBAC evaluation engine works.
- [ ] DENY always overrides ALLOW.
- [ ] Authorization cache invalidates on role/permission change.
- [ ] Audit logs are immutable and append-only.
- [ ] Audit logs encrypted at column level.
- [ ] All security-sensitive actions generate audit entries.
- [ ] Firmware packages can be uploaded and signed.
- [ ] Firmware signature verification works.
- [ ] Unsigned firmware updates are rejected.
- [ ] Web portal shows audit log viewer.
- [ ] All tests pass.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 4 Coverage                    |
| ---------------------------------- | --------------------------------------- |
| RBAC/PBAC enforcement              | AccessEvaluationEngine                  |
| DENY priority (ADR-012)            | Strict evaluation order                 |
| Immutable audit logs (ADR-010)     | Append-only repository                  |
| Audit encryption (ADR-017)         | Column-level encrypted metadata         |
| Signed firmware (ADR-022)          | FirmwareSigningService                  |
| Authorization caching invalidation | RolePermissionChanged event             |
| Audit all security actions         | Integrated across all modules           |

---

## NEW FILE INVENTORY

| Module | Domain | Application | Infrastructure | Presentation | Contracts |
|--------|--------|-------------|----------------|--------------|------------|
| Authorization | 16 | 32 | 15 | 3 | 12 |
| AuditLogs | 6 | 14 | 8 | 1 | 3 |
| Security | 9 | 17 | 10 | 1 | 7 |
| **Total New** | **31** | **63** | **33** | **5** | **22** |

**Grand Total New Files: 154**

---

**ITERATION 4 DOCUMENT COMPLETE**