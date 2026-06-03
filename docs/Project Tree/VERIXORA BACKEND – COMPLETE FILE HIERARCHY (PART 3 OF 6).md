# VERIXORA BACKEND – COMPLETE FILE HIERARCHY (PART 3 OF 6)

---

## MODULE: DEVICES

```
src/Modules/Devices/
+-- Devices.Domain/
|   +-- Devices.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- Device.cs
|   +-- Enums/
|   |   +-- DeviceType.cs
|   |   +-- DeviceStatus.cs
|   +-- ValueObjects/
|   |   +-- DeviceHealth.cs
|   +-- Events/
|   |   +-- DeviceRegistered.cs
|   |   +-- DeviceProvisioned.cs
|   |   +-- DeviceOnline.cs
|   |   +-- DeviceOffline.cs
|   |   +-- DeviceHealthUpdated.cs
|   |   +-- DeviceLimitReached.cs
|   |   +-- DeviceDecommissioned.cs
|   |   +-- MqttTokenIssued.cs
|   +-- Services/
|       +-- IMqttTokenService.cs
|       +-- IDeviceLimitService.cs
+-- Devices.Application/
|   +-- Devices.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- RegisterDevice/
|   |   |   +-- RegisterDeviceCommand.cs
|   |   |   +-- RegisterDeviceHandler.cs
|   |   |   +-- RegisterDeviceValidator.cs
|   |   +-- UpdateDevice/
|   |   |   +-- UpdateDeviceCommand.cs
|   |   |   +-- UpdateDeviceHandler.cs
|   |   +-- RemoveDevice/
|   |   |   +-- RemoveDeviceCommand.cs
|   |   |   +-- RemoveDeviceHandler.cs
|   |   +-- UpdateDeviceHealth/
|   |   |   +-- UpdateDeviceHealthCommand.cs
|   |   |   +-- UpdateDeviceHealthHandler.cs
|   |   +-- DecommissionDevice/
|   |       +-- DecommissionDeviceCommand.cs
|   |       +-- DecommissionDeviceHandler.cs
|   +-- Queries/
|   |   +-- GetDeviceById/
|   |   |   +-- GetDeviceByIdQuery.cs
|   |   |   +-- GetDeviceByIdHandler.cs
|   |   +-- GetDevicesByHome/
|   |   |   +-- GetDevicesByHomeQuery.cs
|   |   |   +-- GetDevicesByHomeHandler.cs
|   |   +-- GetDeviceHealth/
|   |       +-- GetDeviceHealthQuery.cs
|   |       +-- GetDeviceHealthHandler.cs
|   +-- DTOs/
|   |   +-- DeviceDto.cs
|   |   +-- DeviceHealthDto.cs
|   |   +-- MqttTokenDto.cs
|   +-- Interfaces/
|       +-- IDeviceRepository.cs
+-- Devices.Infrastructure/
|   +-- Devices.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- DevicesDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- DeviceConfiguration.cs
|   |   +-- Repositories/
|   |       +-- DeviceRepository.cs
|   +-- Services/
|   |   +-- MqttTokenService.cs
|   |   +-- DeviceLimitService.cs
|   +-- Migrations/
+-- Devices.Presentation/
|   +-- Devices.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- DeviceController.cs
+-- Devices.Contracts/
    +-- Devices.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- RegisterDeviceRequest.cs
    |   +-- UpdateDeviceRequest.cs
    |   +-- HeartbeatRequest.cs
    +-- Responses/
    |   +-- DeviceResponse.cs
    |   +-- DeviceHealthResponse.cs
    |   +-- MqttTokenResponse.cs
    +-- IntegrationEvents/
    |   +-- DeviceRegisteredIntegrationEvent.cs
    |   +-- DeviceOnlineIntegrationEvent.cs
    |   +-- DeviceOfflineIntegrationEvent.cs
    |   +-- DeviceHealthUpdatedIntegrationEvent.cs
    |   +-- DeviceDecommissionedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: PROVISIONING

```
src/Modules/Provisioning/
+-- Provisioning.Domain/
|   +-- Provisioning.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- ProvisioningSession.cs
|   +-- Enums/
|   |   +-- ProvisioningStatus.cs
|   +-- ValueObjects/
|   |   +-- ProvisioningToken.cs
|   +-- Events/
|   |   +-- ProvisioningStarted.cs
|   |   +-- ProvisioningCompleted.cs
|   |   +-- ProvisioningExpired.cs
|   +-- Services/
|       +-- IProvisioningTokenService.cs
+-- Provisioning.Application/
|   +-- Provisioning.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- GenerateProvisioningToken/
|   |   |   +-- GenerateProvisioningTokenCommand.cs
|   |   |   +-- GenerateProvisioningTokenHandler.cs
|   |   +-- StartProvisioning/
|   |   |   +-- StartProvisioningCommand.cs
|   |   |   +-- StartProvisioningHandler.cs
|   |   +-- CompleteProvisioning/
|   |   |   +-- CompleteProvisioningCommand.cs
|   |   |   +-- CompleteProvisioningHandler.cs
|   |   +-- ExpireProvisioning/
|   |       +-- ExpireProvisioningCommand.cs
|   |       +-- ExpireProvisioningHandler.cs
|   +-- Queries/
|   |   +-- GetProvisioningStatus/
|   |       +-- GetProvisioningStatusQuery.cs
|   |       +-- GetProvisioningStatusHandler.cs
|   +-- DTOs/
|   |   +-- ProvisioningSessionDto.cs
|   +-- Interfaces/
|       +-- IProvisioningRepository.cs
+-- Provisioning.Infrastructure/
|   +-- Provisioning.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- ProvisioningDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- ProvisioningSessionConfiguration.cs
|   |   +-- Repositories/
|   |       +-- ProvisioningRepository.cs
|   +-- Services/
|   |   +-- ProvisioningTokenService.cs
|   +-- Migrations/
+-- Provisioning.Presentation/
|   +-- Provisioning.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- ProvisioningController.cs
+-- Provisioning.Contracts/
    +-- Provisioning.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- GenerateTokenRequest.cs
    |   +-- StartProvisioningRequest.cs
    |   +-- CompleteProvisioningRequest.cs
    +-- Responses/
    |   +-- ProvisioningTokenResponse.cs
    |   +-- ProvisioningStatusResponse.cs
    +-- IntegrationEvents/
    |   +-- ProvisioningStartedIntegrationEvent.cs
    |   +-- ProvisioningCompletedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: SMARTLOCKS

```
src/Modules/SmartLocks/
+-- SmartLocks.Domain/
|   +-- SmartLocks.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- SmartLock.cs
|   |   +-- UnlockRequest.cs
|   +-- Enums/
|   |   +-- LockState.cs
|   |   +-- LockCommandType.cs
|   |   +-- LockCommandStatus.cs
|   |   +-- AuthType.cs
|   +-- ValueObjects/
|   |   +-- LockCommand.cs
|   |   +-- PipelineStepResult.cs
|   +-- Events/
|   |   +-- DoorLocked.cs
|   |   +-- DoorUnlocked.cs
|   |   +-- DoorEmergencyLocked.cs
|   |   +-- UnlockAttempted.cs
|   |   +-- UnlockDenied.cs
|   |   +-- UnlockPipelineCompleted.cs
|   |   +-- LockCommandExecuted.cs
|   |   +-- LockCommandDuplicate.cs
|   +-- Services/
|       +-- IUnlockPipeline.cs
|       +-- IIdempotencyService.cs
+-- SmartLocks.Application/
|   +-- SmartLocks.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- UnlockDoor/
|   |   |   +-- UnlockDoorCommand.cs
|   |   |   +-- UnlockDoorHandler.cs
|   |   |   +-- UnlockDoorValidator.cs
|   |   +-- LockDoor/
|   |   |   +-- LockDoorCommand.cs
|   |   |   +-- LockDoorHandler.cs
|   |   |   +-- LockDoorValidator.cs
|   |   +-- EmergencyLock/
|   |   |   +-- EmergencyLockCommand.cs
|   |   |   +-- EmergencyLockHandler.cs
|   |   |   +-- EmergencyLockValidator.cs
|   |   +-- UpdateAutoLockTimer/
|   |       +-- UpdateAutoLockTimerCommand.cs
|   |       +-- UpdateAutoLockTimerHandler.cs
|   +-- Queries/
|   |   +-- GetSmartLockById/
|   |   |   +-- GetSmartLockByIdQuery.cs
|   |   |   +-- GetSmartLockByIdHandler.cs
|   |   +-- GetSmartLocksByHome/
|   |   |   +-- GetSmartLocksByHomeQuery.cs
|   |   |   +-- GetSmartLocksByHomeHandler.cs
|   |   +-- GetSmartLockStatus/
|   |   |   +-- GetSmartLockStatusQuery.cs
|   |   |   +-- GetSmartLockStatusHandler.cs
|   |   +-- GetUnlockHistory/
|   |       +-- GetUnlockHistoryQuery.cs
|   |       +-- GetUnlockHistoryHandler.cs
|   +-- Pipeline/
|   |   +-- IUnlockPipelineService.cs
|   |   +-- UnlockPipelineService.cs
|   |   +-- Steps/
|   |       +-- JwtOrApiKeyValidationStep.cs
|   |       +-- SessionValidationStep.cs
|   |       +-- ScheduleValidationStep.cs
|   |       +-- UserStatusValidationStep.cs
|   |       +-- RoleValidationStep.cs
|   |       +-- PermissionValidationStep.cs
|   |       +-- HomeLevelAccessStep.cs
|   |       +-- DeviceLevelAccessStep.cs
|   |       +-- DeviceHealthValidationStep.cs
|   |       +-- FaceVerificationStep.cs
|   +-- Decorators/
|   |   +-- IdempotencyCommandDecorator.cs
|   |   +-- AuthorizationCacheDecorator.cs
|   +-- Services/
|   |   +-- IMqttCommandPublisher.cs
|   +-- DTOs/
|   |   +-- SmartLockDto.cs
|   |   +-- SmartLockStatusDto.cs
|   |   +-- UnlockHistoryDto.cs
|   |   +-- UnlockResultDto.cs
|   |   +-- PipelineStepResultDto.cs
|   +-- Interfaces/
|       +-- ISmartLockRepository.cs
|       +-- IUnlockRequestRepository.cs
+-- SmartLocks.Infrastructure/
|   +-- SmartLocks.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- SmartLocksDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- SmartLockConfiguration.cs
|   |   |   +-- LockCommandConfiguration.cs
|   |   |   +-- UnlockRequestConfiguration.cs
|   |   +-- Repositories/
|   |       +-- SmartLockRepository.cs
|   |       +-- UnlockRequestRepository.cs
|   +-- Services/
|   |   +-- MqttCommandPublisher.cs
|   |   +-- UnlockPipelineService.cs
|   +-- Idempotency/
|   |   +-- IdempotencyStore.cs
|   |   +-- IdempotencyCleanupJob.cs
|   +-- Migrations/
+-- SmartLocks.Presentation/
|   +-- SmartLocks.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|   |   +-- SmartLockController.cs
|   +-- Hubs/
|       +-- LockStatusHub.cs
+-- SmartLocks.Contracts/
    +-- SmartLocks.Contracts.csproj
    +-- GlobalUsings.cs
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
    |   +-- DoorUnlockedIntegrationEvent.cs
    |   +-- DoorLockedIntegrationEvent.cs
    |   +-- UnlockDeniedIntegrationEvent.cs
    |   +-- UnlockPipelineCompletedIntegrationEvent.cs
    +-- CHANGELOG.md
```