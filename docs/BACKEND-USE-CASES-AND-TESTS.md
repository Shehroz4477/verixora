# FINAL VERIXORA USE CASES & TESTS (ENHANCED - ASCII SAFE)

---

# TESTING STRATEGY OVERVIEW

VERIXORA testing is structured into:
- Unit Tests (Domain logic)
- Application Tests (Use cases)
- Integration Tests (Infrastructure + DB + MQTT)
- End-to-End Tests (Full system flows)

All tests follow the format: GIVEN / WHEN / THEN

---

# 1. IDENTITY MODULE

## USE CASES
- Register User
- Login
- Verify Email
- Reset Password
- Change Password
- Logout
- Refresh Token
- Manage Sessions
- Manage Trusted Devices
- Get Profile Data

## BUSINESS RULES
- Email must be unique
- Email verification required for web access
- JWT expires after 15 minutes
- Refresh tokens rotate on use
- Max 5 trusted devices per user
- Unknown device requires OTP
- Session stores device fingerprint; dramatic fingerprint change forces re-login

## TESTS

- RegisterUser_ValidData_ShouldCreateUser
- RegisterUser_DuplicateEmail_ShouldFail
- Login_ValidCredentials_ShouldReturnToken
- Login_InvalidPassword_ShouldReject
- Login_UnknownDevice_ShouldRequireOtp
- Login_FingerprintChanged_ShouldForceReLogin (new)
- RefreshToken_ValidToken_ShouldRotate
- RefreshToken_ExpiredToken_ShouldFail
- RefreshToken_ReuseDetected_ShouldRevokeAllTokens
- ResetPassword_ShouldRevokeSessions
- AddTrustedDevice_OverLimit_ShouldFail

---

# 2. AUTHORIZATION MODULE

## USE CASES
- Create Role
- Assign Role
- Assign Permission
- Set Device Access
- Set Schedule Restriction
- Evaluate Permission (internal)

## BUSINESS RULES
- DENY overrides all permissions
- Device restrictions override role access
- Schedule restrictions mandatory for guests
- SystemAdmin has full override but must be audited
- Successful permission evaluation cached per session for 1 minute (new)
- Cache invalidated on RolePermissionChanged event (new)

## TESTS

- AssignRole_ShouldGrantAccess
- Guest_OutsideSchedule_ShouldFailAccess
- DeviceRestrictedAccess_ShouldBlock
- PermissionMissing_ShouldReturnDenied
- DenyPolicy_ShouldOverrideAllow
- Authorization_CachedResult_ShouldSkipReevaluation (new)
- Authorization_CacheInvalidatedOnRoleChange_ShouldReevaluate (new)

---

# 3. DEVICES MODULE

## USE CASES
- Register Device
- Update Device
- Remove Device
- Get Device Health
- Firmware Update Trigger (new: signed)

## BUSINESS RULES
- Device must belong to a Home
- Device must be provisioned before activation
- Max devices per Home configurable (default 20)
- Device can be offline
- Firmware update binary must be digitally signed; device verifies before flashing (new)

## TESTS

- RegisterDevice_ShouldSucceed
- DeviceWithoutHome_ShouldFail
- FirmwareUpdate_ShouldQueueJob
- FirmwareUpdate_UnsignedBinary_ShouldReject (new)
- GetHealth_ShouldReturnStatus
- DeviceOverLimit_ShouldFail

---

# 4. PROVISIONING MODULE

## USE CASES
- Generate Token
- Start Provisioning
- Complete Provisioning

## BUSINESS RULES
- Token expires in 2 minutes
- BLE required for setup
- Device must be assigned to Home

## TESTS

- Token_ShouldExpire
- Provisioning_ShouldCreateDevice
- InvalidToken_ShouldFail
- Provisioning_WithoutHome_ShouldFail

---

# 5. SMARTLOCKS MODULE

## USE CASES
- Unlock Door
- Lock Door
- Emergency Lock
- Get Status

## BUSINESS RULES
- Unlock requires full validation pipeline (10 steps, schedule check early)
- Face verification runs last
- All actions audited
- MQTT used for execution
- Commands idempotent via Idempotency-Key (new)
- Unlock burst limited: 5 req / 10 sec per user (new)
- Performance SLA: pipeline completes within 200ms p95 (new)

## TESTS

- Unlock_ValidRequest_ShouldSucceed
- Unlock_InvalidSession_ShouldFail
- Unlock_OutsideSchedule_ShouldFail
- Unlock_LowBattery_ShouldFail
- Unlock_ShouldGenerateAuditLog
- Unlock_DuplicateIdempotencyKey_ShouldReturnSameResult (new)
- Unlock_RateLimitExceeded_ShouldReturn429 (new)
- Unlock_FaceVerificationRequired_ShouldRunLast
- Unlock_PipelineLatency_ShouldMeetSLA (new, performance test)
- Lock_ShouldSucceedAndAudit
- EmergencyLock_AdminOnly

---

# 6. MONITORING MODULE

## USE CASES
- Get Dashboard
- Get Alerts
- Get Live Events

## TESTS

- Dashboard_ShouldReturnCorrectState
- Alerts_ShouldFilterSeverity
- LiveEvents_ShouldBeReceivedViaSignalR

---

# 7. NOTIFICATIONS MODULE

## USE CASES
- Send Notification
- Update Preferences

## TESTS

- SendEmail_ShouldSucceed
- UpdatePreferences_ShouldPersist

---

# 8. AUDIT MODULE

## RULES
- Audit logs are immutable
- Every security action must be logged
- Logs encrypted at column level (new)

## TESTS

- Audit_ShouldPersist
- Audit_ShouldBeImmutable
- Audit_EncryptedData_ShouldBeUnreadableDirectly (new)

---

# 9. AUTOMATION MODULE

## RULES
- No infinite loops allowed
- Rule execution must be bounded
- Actions must be audited

## TESTS

- Rule_ShouldTriggerEvent
- Rule_ShouldExecuteAction
- RuleLoop_ShouldBeBlocked

---

# 10. FACE VERIFICATION MODULE

## RULES
- Pluggable provider
- Optional per policy
- Runs last in unlock flow
- Embeddings encrypted at column level (new)

## TESTS

- FaceMatch_ShouldPassCorrectUser
- FaceMatch_ShouldFailInvalidUser
- SpoofDetection_ShouldRejectFake
- Embeddings_ShouldBeEncrypted (new)

---

# CROSS-CUTTING TESTS (CRITICAL, ENHANCED)

- JWT_Expired_ShouldReject
- InvalidSignature_ShouldFail
- MissingPermission_ShouldReturn403
- RateLimit_ShouldBlockRequests (new)
- Unlock_BurstLimit_ShouldEnforce (new)
- IdempotencyKey_ShouldPreventDuplicateExecution (new)
- AuditLog_AllActions_ShouldExist
- HealthCheck_ShouldReturnAllComponents (new)
- MQTT_TokenExpired_ShouldReconnect (new)
- Observability_TracesGeneratedForUnlock (new)

---

# VERTICAL SLICE TEST NOTES (NEW)

Each use case handler in the Application layer must be tested in isolation:
- The handler receives a request DTO.
- Mocked domain services verify business logic.
- Idempotency decorator prevents handler re-execution.
- Authorization caching decorator reduces service calls.

Tests for infrastructure (EF Core, MQTT broker, SignalR) run in integration suites with containers where possible.

---

DOCUMENT VERSION: Final - Enhanced
LAST UPDATED: 2026-06-02