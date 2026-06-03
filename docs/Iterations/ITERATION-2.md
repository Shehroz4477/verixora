# VERIXORA ITERATION 2 – DEVICE REGISTRATION & PROVISIONING (FINAL VERSION)

---

## ITERATION OBJECTIVE

Implement device registration, provisioning via BLE, device-to-Home assignment, MQTT token lifecycle, device health tracking, configurable device limits, device decommissioning, and a device simulator for development.

By the end of Iteration 2:

- IoT devices can be registered to a Home.
- Devices must be provisioned before activation.
- BLE is used exclusively for provisioning (WiFi credential transfer).
- MQTT short-lived tokens (1 hour) are issued on device heartbeat.
- Device health status is tracked.
- Configurable device limit per Home is enforced (default 20).
- Device decommissioning revokes credentials and marks device.
- Integration event contracts defined for Devices and Provisioning modules.
- Device simulator available for development and testing.
- Mobile app supports BLE provisioning flow.
- Web Portal shows device list and status.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                      |
| ----------------- | ------------------------------------------ |
| Backend Developer | Devices module + Provisioning module       |
| Mobile Developer  | BLE provisioning screens, device scanner   |
| Web Developer     | Device management dashboard                |
| IoT Developer     | ESP32 firmware (provisioning + MQTT client), Device Simulator |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**Devices.Domain:**
- `Device` aggregate root:
  - `Id` (Guid)
  - `DeviceUid` (string, unique hardware ID)
  - `HomeId` (Guid)
  - `Name` (string)
  - `DeviceType` (enum: SmartLock, Sensor, Gateway, Other)
  - `Status` (enum: PendingProvisioning, Online, Offline, Maintenance, Decommissioned)
  - `FirmwareVersion` (string)
  - `BatteryLevel` (int?, percentage)
  - `LastHeartbeat` (DateTimeOffset?)
  - `RegisteredAt` (DateTimeOffset)
  - `ProvisionedAt` (DateTimeOffset?)
  - `DecommissionedAt` (DateTimeOffset?)
- `DeviceHealth` value object:
  - `BatteryLevel` (int?)
  - `SignalStrength` (int?)
  - `UptimeMinutes` (int?)
  - `LastCheckedAt` (DateTimeOffset)

**Provisioning.Domain:**
- `ProvisioningSession` aggregate root:
  - `Id` (Guid)
  - `DeviceUid` (string)
  - `Token` (string, hashed)
  - `TokenExpiresAt` (DateTimeOffset)
  - `HomeId` (Guid)
  - `Status` (enum: Pending, InProgress, Completed, Expired, Failed)
  - `CreatedAt` (DateTimeOffset)
  - `CompletedAt` (DateTimeOffset?)
- `ProvisioningToken` value object:
  - `Token` (string)
  - `ExpiresAt` (DateTimeOffset, 2 minutes from creation)

**Domain Events:**
- `DeviceRegistered`
- `DeviceProvisioned`
- `DeviceOnline`
- `DeviceOffline`
- `DeviceHealthUpdated`
- `DeviceLimitReached`
- `DeviceDecommissioned`
- `ProvisioningStarted`
- `ProvisioningCompleted`
- `ProvisioningExpired`
- `MqttTokenIssued`

### 1.2 Backend – Application Layer

**Devices.Application – Commands:**
- `RegisterDeviceCommand` → `RegisterDeviceHandler` + `RegisterDeviceValidator`
- `UpdateDeviceCommand` → `UpdateDeviceHandler`
- `RemoveDeviceCommand` → `RemoveDeviceHandler`
- `UpdateDeviceHealthCommand` → `UpdateDeviceHealthHandler`
- `DecommissionDeviceCommand` → `DecommissionDeviceHandler`

**Devices.Application – Queries:**
- `GetDeviceByIdQuery` → `GetDeviceByIdHandler`
- `GetDevicesByHomeQuery` → `GetDevicesByHomeHandler`
- `GetDeviceHealthQuery` → `GetDeviceHealthHandler`

**Provisioning.Application – Commands:**
- `GenerateProvisioningTokenCommand` → `GenerateProvisioningTokenHandler`
- `StartProvisioningCommand` → `StartProvisioningHandler`
- `CompleteProvisioningCommand` → `CompleteProvisioningHandler`
- `ExpireProvisioningCommand` → `ExpireProvisioningHandler`

**Provisioning.Application – Queries:**
- `GetProvisioningStatusQuery` → `GetProvisioningStatusHandler`

**Services:**
- `IMqttTokenService` – generates short-lived MQTT JWTs.
- `IDeviceLimitService` – checks and enforces device limit per Home.

### 1.3 Backend – Infrastructure Layer

**Devices.Infrastructure:**
- `DevicesDbContext` with `DbSet<Device>`.
- `DeviceConfiguration` – indexes on `DeviceUid` (unique), `HomeId`.
- `DeviceRepository`.
- `MqttTokenService` – generates 1-hour JWTs scoped to device topic.
- `DeviceLimitService` – checks Home.MaxDevices against current count.

**Provisioning.Infrastructure:**
- `ProvisioningDbContext` with `DbSet<ProvisioningSession>`.
- `ProvisioningSessionConfiguration`.
- `ProvisioningRepository`.
- `ProvisioningTokenService` – generates 2-minute expiry tokens.

### 1.4 Backend – Presentation Layer

**Devices.Presentation – Controllers:**
- `DeviceController`:
  - `POST /api/v1/devices` – register device.
  - `GET /api/v1/devices` – list devices by home.
  - `GET /api/v1/devices/{id}` – get device details.
  - `PUT /api/v1/devices/{id}` – update device.
  - `DELETE /api/v1/devices/{id}` – remove device.
  - `GET /api/v1/devices/{id}/health` – get device health.
  - `POST /api/v1/devices/{id}/heartbeat` – device heartbeat (issues MQTT token).
  - `POST /api/v1/devices/{id}/decommission` – decommission device.

**Provisioning.Presentation – Controllers:**
- `ProvisioningController`:
  - `POST /api/v1/provisioning/token` – generate provisioning token.
  - `POST /api/v1/provisioning/start` – start provisioning.
  - `POST /api/v1/provisioning/complete` – complete provisioning.
  - `GET /api/v1/provisioning/{id}` – get provisioning status.

### 1.5 Backend – Contracts (including Integration Events)

**Devices.Contracts:**
- `Requests/`: `RegisterDeviceRequest`, `UpdateDeviceRequest`, `HeartbeatRequest`.
- `Responses/`: `DeviceResponse`, `DeviceHealthResponse`, `MqttTokenResponse`.
- `IntegrationEvents/`:
  - `DeviceRegisteredIntegrationEvent`
  - `DeviceOnlineIntegrationEvent`
  - `DeviceOfflineIntegrationEvent`
  - `DeviceHealthUpdatedIntegrationEvent`
  - `DeviceDecommissionedIntegrationEvent`

**Provisioning.Contracts:**
- `Requests/`: `GenerateTokenRequest`, `StartProvisioningRequest`, `CompleteProvisioningRequest`.
- `Responses/`: `ProvisioningTokenResponse`, `ProvisioningStatusResponse`.
- `IntegrationEvents/`:
  - `ProvisioningStartedIntegrationEvent`
  - `ProvisioningCompletedIntegrationEvent`

### 1.6 Device Simulator

A lightweight console application or Docker container that mimics ESP32 behavior:
- Responds to MQTT commands (lock/unlock acknowledgement).
- Sends periodic heartbeats.
- Supports provisioning flow.
- Configurable device ID and behavior.
- Used for integration tests and development.

```
tools/DeviceSimulator/
+-- DeviceSimulator.csproj
+-- Program.cs
+-- MqttClientService.cs
+-- ProvisioningSimulator.cs
+-- appsettings.json
+-- Dockerfile
```

### 1.7 Mobile App

- **Device List Screen:** shows all devices in selected home.
- **Device Detail Screen:** shows device info, health, status.
- **Add Device Screen:** initiates registration and provisioning.
- **BLE Provisioning Screen:** scans for device, connects via BLE, transfers WiFi credentials.
- **Device Scanner Service:** BLE scanning and connection management.

### 1.8 Web Portal

- **Device Management Page:** table of all devices per home.
- **Device Detail View:** device info, health, provisioning status.
- **Device Registration Form:** manual device registration.
- **Decommission Button:** decommission device with confirmation.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Devices.Domain (New Files)
```
Devices.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- Device.cs
+-- Enums/
|   +-- DeviceType.cs
|   +-- DeviceStatus.cs
+-- ValueObjects/
|   +-- DeviceHealth.cs
+-- Events/
|   +-- DeviceRegistered.cs
|   +-- DeviceProvisioned.cs
|   +-- DeviceOnline.cs
|   +-- DeviceOffline.cs
|   +-- DeviceHealthUpdated.cs
|   +-- DeviceLimitReached.cs
|   +-- DeviceDecommissioned.cs
|   +-- MqttTokenIssued.cs
+-- Services/
    +-- IMqttTokenService.cs
    +-- IDeviceLimitService.cs
```

### 2.2 Devices.Application (New Files)
```
Devices.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- RegisterDevice/
|   |   +-- RegisterDeviceCommand.cs
|   |   +-- RegisterDeviceHandler.cs
|   |   +-- RegisterDeviceValidator.cs
|   +-- UpdateDevice/
|   |   +-- UpdateDeviceCommand.cs
|   |   +-- UpdateDeviceHandler.cs
|   +-- RemoveDevice/
|   |   +-- RemoveDeviceCommand.cs
|   |   +-- RemoveDeviceHandler.cs
|   +-- UpdateDeviceHealth/
|   |   +-- UpdateDeviceHealthCommand.cs
|   |   +-- UpdateDeviceHealthHandler.cs
|   +-- DecommissionDevice/
|       +-- DecommissionDeviceCommand.cs
|       +-- DecommissionDeviceHandler.cs
+-- Queries/
|   +-- GetDeviceById/
|   |   +-- GetDeviceByIdQuery.cs
|   |   +-- GetDeviceByIdHandler.cs
|   +-- GetDevicesByHome/
|   |   +-- GetDevicesByHomeQuery.cs
|   |   +-- GetDevicesByHomeHandler.cs
|   +-- GetDeviceHealth/
|       +-- GetDeviceHealthQuery.cs
|       +-- GetDeviceHealthHandler.cs
+-- DTOs/
|   +-- DeviceDto.cs
|   +-- DeviceHealthDto.cs
|   +-- MqttTokenDto.cs
+-- Interfaces/
    +-- IDeviceRepository.cs
```

### 2.3 Devices.Infrastructure (New Files)
```
Devices.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- DevicesDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- DeviceConfiguration.cs
|   +-- Repositories/
|   |   +-- DeviceRepository.cs
+-- Services/
|   +-- MqttTokenService.cs
|   +-- DeviceLimitService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 Devices.Presentation (New Files)
```
Devices.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- DeviceController.cs
```

### 2.5 Devices.Contracts (New Files)
```
Devices.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- RegisterDeviceRequest.cs
|   +-- UpdateDeviceRequest.cs
|   +-- HeartbeatRequest.cs
+-- Responses/
|   +-- DeviceResponse.cs
|   +-- DeviceHealthResponse.cs
|   +-- MqttTokenResponse.cs
+-- IntegrationEvents/
    +-- DeviceRegisteredIntegrationEvent.cs
    +-- DeviceOnlineIntegrationEvent.cs
    +-- DeviceOfflineIntegrationEvent.cs
    +-- DeviceHealthUpdatedIntegrationEvent.cs
    +-- DeviceDecommissionedIntegrationEvent.cs
```

### 2.6 Provisioning.Domain (New Files)
```
Provisioning.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- ProvisioningSession.cs
+-- Enums/
|   +-- ProvisioningStatus.cs
+-- ValueObjects/
|   +-- ProvisioningToken.cs
+-- Events/
|   +-- ProvisioningStarted.cs
|   +-- ProvisioningCompleted.cs
|   +-- ProvisioningExpired.cs
+-- Services/
    +-- IProvisioningTokenService.cs
```

### 2.7 Provisioning.Application (New Files)
```
Provisioning.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- GenerateProvisioningToken/
|   |   +-- GenerateProvisioningTokenCommand.cs
|   |   +-- GenerateProvisioningTokenHandler.cs
|   +-- StartProvisioning/
|   |   +-- StartProvisioningCommand.cs
|   |   +-- StartProvisioningHandler.cs
|   +-- CompleteProvisioning/
|   |   +-- CompleteProvisioningCommand.cs
|   |   +-- CompleteProvisioningHandler.cs
|   +-- ExpireProvisioning/
|       +-- ExpireProvisioningCommand.cs
|       +-- ExpireProvisioningHandler.cs
+-- Queries/
|   +-- GetProvisioningStatus/
|       +-- GetProvisioningStatusQuery.cs
|       +-- GetProvisioningStatusHandler.cs
+-- DTOs/
|   +-- ProvisioningSessionDto.cs
+-- Interfaces/
    +-- IProvisioningRepository.cs
```

### 2.8 Provisioning.Infrastructure (New Files)
```
Provisioning.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- ProvisioningDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- ProvisioningSessionConfiguration.cs
|   +-- Repositories/
|   |   +-- ProvisioningRepository.cs
+-- Services/
|   +-- ProvisioningTokenService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.9 Provisioning.Presentation (New Files)
```
Provisioning.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- ProvisioningController.cs
```

### 2.10 Provisioning.Contracts (New Files)
```
Provisioning.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- GenerateTokenRequest.cs
|   +-- StartProvisioningRequest.cs
|   +-- CompleteProvisioningRequest.cs
+-- Responses/
|   +-- ProvisioningTokenResponse.cs
|   +-- ProvisioningStatusResponse.cs
+-- IntegrationEvents/
    +-- ProvisioningStartedIntegrationEvent.cs
    +-- ProvisioningCompletedIntegrationEvent.cs
```

### 2.11 Device Simulator
```
tools/DeviceSimulator/
+-- DeviceSimulator.csproj
+-- Program.cs
+-- MqttClientService.cs
+-- ProvisioningSimulator.cs
+-- appsettings.json
+-- Dockerfile
```

### 2.12 Identity Module Update

**Identity.Domain – Home entity updated:**
- `MaxDevices` property added (default 20, configurable).

**Identity.Infrastructure – HomeConfiguration updated:**
- `MaxDevices` column mapped.

**Identity.Contracts updated:**
- `UpdateHomeRequest` with `MaxDevices` field.
- `HomeResponse` with `MaxDevices` field.

---

## PHASE 3: INTEGRATION

- Wire Devices and Provisioning modules DI into ApiHost.
- Run EF Core migrations for Devices and Provisioning schemas.
- Set up MQTT broker (Mosquitto) via docker-compose.
- Configure MQTT connection in `appsettings.json`.
- Mobile app BLE plugin integration (Capacitor BLE plugin).
- Device simulator integrated into docker-compose for local development.
- CI pipeline runs all new tests including contract tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `Device_Register_ShouldSetPendingProvisioning`
- `Device_Provision_ShouldChangeStatus`
- `Device_Heartbeat_ShouldUpdateTimestamp`
- `Device_Decommission_ShouldRevokeTokens`
- `ProvisioningToken_ShouldExpireIn2Minutes`
- `ProvisioningSession_Complete_ShouldSetCompletedAt`
- `DeviceLimit_Exceeded_ShouldThrow`

### 4.2 Application Tests
- `RegisterDevice_ValidData_ShouldSucceed`
- `RegisterDevice_WithoutHome_ShouldFail`
- `RegisterDevice_OverLimit_ShouldFail`
- `GenerateToken_ShouldReturnToken`
- `Token_Expired_ShouldFailProvisioning`
- `CompleteProvisioning_ShouldActivateDevice`
- `Heartbeat_ShouldIssueMqttToken`
- `MqttToken_ShouldBeScopedToDevice`
- `DecommissionDevice_ShouldRevokeTokens`
- `DecommissionDevice_ShouldMarkStatus`
- `DecommissionedDevice_Reactivation_ShouldFail`

### 4.3 Integration Tests
- `DeviceController_CRUD_ShouldWork`
- `ProvisioningController_FullFlow_ShouldSucceed`
- `MqttToken_ShouldConnectToBroker`
- `DeviceHealth_ShouldUpdateOnHeartbeat`
- `DeviceSimulator_ShouldRespondToCommands`

### 4.4 Contract Tests
- `Devices_Contracts_ShouldMatchPublishedEvents`
- `Provisioning_Contracts_ShouldMatchPublishedEvents`

### 4.5 E2E Tests (Mobile)
- Scan BLE device → generate token → provision → device appears in list.
- Device simulator heartbeat → status updates in web portal.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Deploy MQTT broker to staging.
- Device simulator available for testing.
- Build mobile app with BLE support for test devices.
- Build web portal for staging.

---

## ACCEPTANCE CRITERIA

- [ ] Devices can be registered to a Home.
- [ ] Provisioning flow works end-to-end (token → BLE → activation).
- [ ] Provisioning token expires in 2 minutes.
- [ ] MQTT tokens issued on heartbeat (1 hour expiry).
- [ ] Device limit enforced per Home (default 20, configurable).
- [ ] Device health updated on heartbeat.
- [ ] Device decommissioning revokes MQTT tokens.
- [ ] Decommissioned devices cannot be re-activated.
- [ ] Integration event contracts defined.
- [ ] Contract tests pass.
- [ ] Device simulator available and functional.
- [ ] Mobile app completes BLE provisioning flow.
- [ ] Web portal shows device list and status.
- [ ] All tests pass.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 2 Coverage                 |
| ---------------------------------- | ------------------------------------ |
| Device registration                | Full implementation                  |
| Device provisioning (BLE)          | Token, BLE flow, activation          |
| Device-to-Home assignment          | Enforced in domain                   |
| MQTT token lifecycle               | 1-hour JWTs, scoped per device       |
| Configurable device limit          | Home.MaxDevices, DeviceLimitService  |
| Device health tracking             | Heartbeat, DeviceHealth value object |
| Device decommissioning             | Full flow with credential revocation |
| ADR-006 (BLE only provisioning)    | Enforced, no operational BLE         |
| ADR-018 (Short-lived MQTT tokens)  | 1-hour token service                 |
| Integration event contracts        | Devices + Provisioning Contracts     |
| Device simulator                   | tools/DeviceSimulator                |

---

**ITERATION 2 COMPLETE.**

All improvements integrated:
- Integration event contracts (Devices + Provisioning)
- Device decommissioning
- Device simulator