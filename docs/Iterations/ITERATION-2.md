# VERIXORA ITERATION 2 – DEVICE REGISTRATION & PROVISIONING

---

## ITERATION OBJECTIVE

Implement device registration, provisioning via BLE, device-to-Home assignment, MQTT token lifecycle, device health tracking, and configurable device limits per Home.

By the end of Iteration 2:

- IoT devices can be registered to a Home.
- Devices must be provisioned before activation.
- BLE is used exclusively for provisioning (WiFi credential transfer).
- MQTT short-lived tokens (1 hour) are issued on device heartbeat.
- Device health status is tracked.
- Configurable device limit per Home is enforced (default 20).
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
| IoT Developer     | ESP32 firmware (provisioning + MQTT client)|

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

**Services (Devices.Application):**
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

**Provisioning.Presentation – Controllers:**
- `ProvisioningController`:
  - `POST /api/v1/provisioning/token` – generate provisioning token.
  - `POST /api/v1/provisioning/start` – start provisioning.
  - `POST /api/v1/provisioning/complete` – complete provisioning.
  - `GET /api/v1/provisioning/{id}` – get provisioning status.

### 1.5 Backend – Contracts

**Devices.Contracts:**
- `Requests/`: `RegisterDeviceRequest`, `UpdateDeviceRequest`, `HeartbeatRequest`.
- `Responses/`: `DeviceResponse`, `DeviceHealthResponse`, `MqttTokenResponse`.

**Provisioning.Contracts:**
- `Requests/`: `GenerateTokenRequest`, `StartProvisioningRequest`, `CompleteProvisioningRequest`.
- `Responses/`: `ProvisioningTokenResponse`, `ProvisioningStatusResponse`.

### 1.6 Mobile App

- **Device List Screen:** shows all devices in selected home.
- **Device Detail Screen:** shows device info, health, status.
- **Add Device Screen:** initiates registration and provisioning.
- **BLE Provisioning Screen:** scans for device, connects via BLE, transfers WiFi credentials.
- **Device Scanner Service:** BLE scanning and connection management.

### 1.7 Web Portal

- **Device Management Page:** table of all devices per home.
- **Device Detail View:** device info, health, provisioning status.
- **Device Registration Form:** manual device registration.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Devices.Domain (New Files)
```
Devices.Domain/
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
|   +-- MqttTokenIssued.cs
+-- Services/
    +-- IMqttTokenService.cs
    +-- IDeviceLimitService.cs
```

### 2.2 Devices.Application (New Files)
```
Devices.Application/
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
|       +-- UpdateDeviceHealthCommand.cs
|       +-- UpdateDeviceHealthHandler.cs
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
+-- Controllers/
    +-- DeviceController.cs
```

### 2.5 Devices.Contracts (New Files)
```
Devices.Contracts/
+-- Requests/
|   +-- RegisterDeviceRequest.cs
|   +-- UpdateDeviceRequest.cs
|   +-- HeartbeatRequest.cs
+-- Responses/
    +-- DeviceResponse.cs
    +-- DeviceHealthResponse.cs
    +-- MqttTokenResponse.cs
```

### 2.6 Provisioning.Domain (New Files)
```
Provisioning.Domain/
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
+-- Controllers/
    +-- ProvisioningController.cs
```

### 2.10 Provisioning.Contracts (New Files)
```
Provisioning.Contracts/
+-- Requests/
|   +-- GenerateTokenRequest.cs
|   +-- StartProvisioningRequest.cs
|   +-- CompleteProvisioningRequest.cs
+-- Responses/
    +-- ProvisioningTokenResponse.cs
    +-- ProvisioningStatusResponse.cs
```

### 2.11 Identity Module Update

**Identity.Domain – Home entity updated:**
- `MaxDevices` property added (default 20, configurable).
- `UpdateMaxDevices(int maxDevices)` method.

**Identity.Application:**
- `UpdateHomeCommand` updated to support MaxDevices.
- `UpdateHomeValidator` updated.

**Identity.Infrastructure – HomeConfiguration updated:**
- `MaxDevices` column mapped.

**Identity.Contracts:**
- `UpdateHomeRequest` updated with `MaxDevices` field.
- `HomeResponse` updated with `MaxDevices` field.

---

## PHASE 3: INTEGRATION

- Wire Devices and Provisioning modules DI into ApiHost (`services.AddDevicesModule()`, `services.AddProvisioningModule()`).
- Run EF Core migrations for Devices and Provisioning schemas.
- Set up MQTT broker (Mosquitto or EMQX) for development/testing.
- Configure MQTT connection in `appsettings.json`.
- Mobile app BLE plugin integration (Capacitor BLE plugin).
- Device simulator for testing (mock ESP32 responses).
- CI pipeline runs all new tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `Device_Register_ShouldSetPendingProvisioning`
- `Device_Provision_ShouldChangeStatus`
- `Device_Heartbeat_ShouldUpdateTimestamp`
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

### 4.3 Integration Tests
- `DeviceController_CRUD_ShouldWork`
- `ProvisioningController_FullFlow_ShouldSucceed`
- `MqttToken_ShouldConnectToBroker`
- `DeviceHealth_ShouldUpdateOnHeartbeat`

### 4.4 E2E Tests (Mobile)
- Scan BLE device → generate token → provision → device appears in list.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Deploy MQTT broker to staging.
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
- [ ] Mobile app completes BLE provisioning flow.
- [ ] Web portal shows device list and status.
- [ ] All tests pass.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement       | Iteration 2 Coverage                 |
| ----------------------------- | ------------------------------------ |
| Device registration           | Full implementation                  |
| Device provisioning (BLE)     | Token, BLE flow, activation          |
| Device-to-Home assignment     | Enforced in domain                   |
| MQTT token lifecycle          | 1-hour JWTs, scoped per device       |
| Configurable device limit     | Home.MaxDevices, DeviceLimitService  |
| Device health tracking        | Heartbeat, DeviceHealth value object |
| ADR-006 (BLE only provisioning)| Enforced, no operational BLE        |
| ADR-018 (Short-lived MQTT tokens)| 1-hour token service               |

---

## NEW FILE INVENTORY

| Module | Domain | Application | Infrastructure | Presentation | Contracts |
|--------|--------|-------------|----------------|--------------|------------|
| Devices | 10 | 18 | 7 | 1 | 5 |
| Provisioning | 7 | 13 | 7 | 1 | 5 |
| Identity (updates) | 1 | 2 | 1 | 0 | 2 |
| **Total New** | **18** | **33** | **15** | **2** | **12** |

**Grand Total New Files: 80**

---

**ITERATION 2 DOCUMENT COMPLETE**
