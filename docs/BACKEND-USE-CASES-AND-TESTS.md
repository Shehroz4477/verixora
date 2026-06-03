# FINAL VERIXORA USE CASES & TESTS (FINAL VERSION)

---

# TESTING STRATEGY OVERVIEW

VERIXORA testing is structured into:
- Unit Tests (Domain logic)
- Application Tests (Use cases)
- Integration Tests (Infrastructure + DB + MQTT)
- Contract Tests (Module contracts and integration events)
- Load Tests (Performance SLA validation)
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
- Create API Key
- Revoke API Key
- Rotate API Key
- List API Keys

## BUSINESS RULES
- Email must be unique
- Email verification required for web access
- JWT expires after 15 minutes
- Refresh tokens rotate on use
- Max 5 trusted devices per user
- Unknown device requires OTP
- Session stores device fingerprint; dramatic fingerprint change forces re-login
- API keys scoped to Home with specific permissions
- API keys stored hashed (SHA-256)
- Max 10 API keys per Home
- Session entities owned by Identity module

## TESTS

- RegisterUser_ValidData_ShouldCreateUser
- RegisterUser_DuplicateEmail_ShouldFail
- Login_ValidCredentials_ShouldReturnToken
- Login_InvalidPassword_ShouldReject
- Login_UnknownDevice_ShouldRequireOtp
- Login_FingerprintChanged_ShouldForceReLogin
- RefreshToken_ValidToken_ShouldRotate
- RefreshToken_ExpiredToken_ShouldFail
- RefreshToken_ReuseDetected_ShouldRevokeAllTokens
- ResetPassword_ShouldRevokeSessions
- AddTrustedDevice_OverLimit_ShouldFail
- CreateApiKey_ValidData_ShouldCreate
- CreateApiKey_OverLimit_ShouldFail
- RevokeApiKey_ShouldInvalidate
- ApiKey_StoredHashed_ShouldNotExposeRaw
- ApiKey_Authenticate_ShouldBypassSessionCheck

---

# 2. AUTHORIZATION MODULE

## USE CASES
- Create Role
- Assign Role
- Assign Permission
- Set Device Access
- Set Schedule Restriction
- Evaluate Permission (internal)
- Evaluate API Key Access (internal)

## BUSINESS RULES
- DENY overrides all permissions
- Device restrictions override role access
- Schedule restrictions mandatory for guests
- SystemAdmin has full override but must be audited
- Successful permission evaluation cached per session for 1 minute
- API key evaluations are NOT cached
- Cache invalidated on RolePermissionChanged event

## TESTS

- AssignRole_ShouldGrantAccess
- Guest_OutsideSchedule_ShouldFailAccess
- DeviceRestrictedAccess_ShouldBlock
- PermissionMissing_ShouldReturnDenied
- DenyPolicy_ShouldOverrideAllow
- Authorization_CachedResult_ShouldSkipReevaluation
- Authorization_CacheInvalidatedOnRoleChange_ShouldReevaluate
- ApiKey_Authorization_ShouldNotBeCached
- ApiKey_ScopeRestriction_ShouldBlockUnauthorizedAction

---

# 3. DEVICES MODULE

## USE CASES
- Register Device
- Update Device
- Remove Device
- Get Device Health
- Firmware Update Trigger (signed)
- Decommission Device

## BUSINESS RULES
- Device must belong to a Home
- Device must be provisioned before activation
- Max devices per Home configurable (default 20)
- Device can be offline
- Firmware update binary must be digitally signed; device verifies before flashing
- Decommissioning revokes MQTT tokens and marks device as decommissioned
- Decommissioned devices cannot be re-activated without re-provisioning

## TESTS

- RegisterDevice_ShouldSucceed
- DeviceWithoutHome_ShouldFail
- FirmwareUpdate_ShouldQueueJob
- FirmwareUpdate_UnsignedBinary_ShouldReject
- GetHealth_ShouldReturnStatus
- DeviceOverLimit_ShouldFail
- DecommissionDevice_ShouldRevokeTokens
- DecommissionDevice_ShouldMarkStatus
- DecommissionedDevice_Reactivation_ShouldFail

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
- Commands idempotent via Idempotency-Key
- Unlock burst limited: 5 req / 10 sec per user
- Performance SLA: pipeline completes within 200ms p95
- Offline unlock NOT supported
- API key auth bypasses session check (step 2)

## TESTS

- Unlock_ValidRequest_ShouldSucceed
- Unlock_InvalidSession_ShouldFail
- Unlock_OutsideSchedule_ShouldFail
- Unlock_LowBattery_ShouldFail
- Unlock_ShouldGenerateAuditLog
- Unlock_DuplicateIdempotencyKey_ShouldReturnSameResult
- Unlock_RateLimitExceeded_ShouldReturn429
- Unlock_FaceVerificationRequired_ShouldRunLast
- Unlock_PipelineLatency_ShouldMeetSLA
- Lock_ShouldSucceedAndAudit
- EmergencyLock_AdminOnly
- Unlock_ApiKeyAuth_ShouldBypassSessionCheck
- OfflineUnlock_ShouldNotBeSupported

---

# 6. MONITORING MODULE

## USE CASES
- Get Dashboard
- Get Alerts
- Get Live Events
- Detect Suspicious Activity

## BUSINESS RULES
- Suspicious activity detection analyzes audit patterns
- X failed unlocks in Y minutes triggers alert
- X failed logins from same IP triggers alert
- Device offline > X minutes triggers alert
- Multiple devices offline simultaneously triggers alert

## TESTS

- Dashboard_ShouldReturnCorrectState
- Alerts_ShouldFilterSeverity
- LiveEvents_ShouldBeReceivedViaSignalR
- SuspiciousActivity_FailedUnlocks_ShouldTriggerAlert
- SuspiciousActivity_FailedLogins_ShouldTriggerAlert
- SuspiciousActivity_DeviceOffline_ShouldTriggerAlert
- SuspiciousActivity_MultipleDevicesOffline_ShouldTriggerCriticalAlert
- SuspiciousActivity_ApiKeyNewIp_ShouldTriggerAlert

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
- Logs encrypted at column level
- Operational retention: 90 days
- Archive to cold storage after 90 days
- Archived logs retained for 7 years

## TESTS

- Audit_ShouldPersist
- Audit_ShouldBeImmutable
- Audit_EncryptedData_ShouldBeUnreadableDirectly
- AuditLogRetention_ShouldArchiveOlderThan90Days
- AuditLogRetention_ShouldNotArchiveRecentLogs
- AuditLogArchive_ShouldRemainEncrypted
- AuditLogArchive_ShouldBeImmutable

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
- Embeddings encrypted at column level

## TESTS

- FaceMatch_ShouldPassCorrectUser
- FaceMatch_ShouldFailInvalidUser
- SpoofDetection_ShouldRejectFake
- Embeddings_ShouldBeEncrypted

---

# CONTRACT TESTS (NEW SECTION)

## PURPOSE
Ensure module contracts and integration events maintain compatibility.

## TESTS

- Identity_Contracts_ShouldMatchPublishedEvents
- Devices_Contracts_ShouldMatchPublishedEvents
- SmartLocks_Contracts_ShouldMatchPublishedEvents
- Authorization_Contracts_ShouldMatchPublishedEvents
- AuditLogs_Contracts_ShouldMatchPublishedEvents
- ApiKey_IntegrationEvent_ShouldContainRequiredFields
- DoorUnlocked_IntegrationEvent_ShouldContainAuditData
- Contract_BreakingChange_ShouldBeDetected

---

# LOAD TESTS (NEW SECTION)

## PURPOSE
Validate performance SLA under concurrent load.

## TESTS

- UnlockPipeline_Concurrent100_ShouldMeetSLA
- UnlockPipeline_Concurrent500_ShouldMeetSLA
- UnlockPipeline_SustainedLoad_ShouldNotDegrade
- RateLimiting_UnderLoad_ShouldProtectSystem
- Database_ConnectionPool_ShouldHandleLoad
- MQTT_CommandDelivery_UnderLoad_ShouldNotLoseMessages

---

# CROSS-CUTTING TESTS

- JWT_Expired_ShouldReject
- InvalidSignature_ShouldFail
- MissingPermission_ShouldReturn403
- RateLimit_ShouldBlockRequests
- Unlock_BurstLimit_ShouldEnforce
- IdempotencyKey_ShouldPreventDuplicateExecution
- AuditLog_AllActions_ShouldExist
- HealthCheck_ShouldReturnAllComponents
- MQTT_TokenExpired_ShouldReconnect
- Observability_TracesGeneratedForUnlock
- GracefulShutdown_ShouldCompletePendingOperations
- Database_TransientFailure_ShouldRetry
- FeatureFlag_Disabled_ShouldReturn404
- ApiKey_RateLimit_ShouldEnforce

---

# VERTICAL SLICE TEST NOTES

Each use case handler in the Application layer must be tested in isolation:
- The handler receives a request DTO.
- Mocked domain services verify business logic.
- Idempotency decorator prevents handler re-execution.
- Authorization caching decorator reduces service calls.
- API key authentication decorator bypasses session cache.

Tests for infrastructure (EF Core, MQTT broker, SignalR) run in integration suites with containers where possible.

Contract tests run in CI and fail the build if any contract is broken.

Load tests run nightly or on-demand, not in every CI run.

---

**DOCUMENT VERSION: Final**
**LAST UPDATED: 2026-06-03**

---

**FINAL VERIXORA USE CASES & TESTS UPDATED.**

New sections added:
- Contract Tests section
- Load Tests section
- API key test cases in Identity and Authorization
- Device decommissioning test cases
- Audit log retention test cases
- Suspicious activity detection test cases
- Offline unlock test case
- Graceful shutdown test case
- Database retry test case
- Feature flag test case