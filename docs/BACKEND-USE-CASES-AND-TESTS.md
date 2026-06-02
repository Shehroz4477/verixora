# FINAL VERIXORA USE CASES & TESTS (CLEAN VERSION)

---

# TESTING STRATEGY OVERVIEW

VERIXORA testing is structured into:

* Unit Tests (Domain logic)
* Application Tests (Use cases)
* Integration Tests (Infrastructure + DB + MQTT)
* End-to-End Tests (Full system flows)

---

# 1. IDENTITY MODULE

## USE CASES

* Register User
* Login
* Verify Email
* Reset Password
* Change Password
* Logout
* Refresh Token
* Manage Sessions
* Manage Trusted Devices
* Get Profile Data

---

## BUSINESS RULES

* Email must be unique
* Email verification required for web access
* JWT expires after 15 minutes
* Refresh tokens rotate on use
* Max 5 trusted devices per user
* Unknown device requires OTP

---

## TEST RULES (STANDARDIZED FORMAT)

FORMAT:

* GIVEN / WHEN / THEN

### EXAMPLES

* RegisterUser_ValidData_ShouldCreateUser
* Login_InvalidPassword_ShouldReject
* Login_UnknownDevice_ShouldRequireOtp
* RefreshToken_ValidToken_ShouldRotate
* ResetPassword_ShouldRevokeSessions

---

# 2. AUTHORIZATION MODULE

## USE CASES

* Create Role
* Assign Role
* Assign Permission
* Set Device Access
* Set Schedule Restriction

---

## BUSINESS RULES

* DENY overrides all permissions
* Device restrictions override role access
* Schedule restrictions mandatory for guests
* SystemAdmin has full override but must be audited

---

## TESTS

* AssignRole_ShouldGrantAccess
* Guest_OutsideSchedule_ShouldFailAccess
* DeviceRestrictedAccess_ShouldBlock
* PermissionMissing_ShouldReturnDenied

---

# 3. DEVICES MODULE

## USE CASES

* Register Device
* Update Device
* Remove Device
* Get Device Health
* Firmware Update Trigger

---

## BUSINESS RULES

* Device must belong to a Home
* Device must be provisioned before activation
* Max 20 devices per Home
* Device can be offline

---

## TESTS

* RegisterDevice_ShouldSucceed
* DeviceWithoutHome_ShouldFail
* FirmwareUpdate_ShouldQueueJob
* GetHealth_ShouldReturnStatus

---

# 4. PROVISIONING MODULE

## USE CASES

* Generate Token
* Start Provisioning
* Complete Provisioning

---

## BUSINESS RULES

* Token expires in 2 minutes
* BLE required for setup
* Device must be assigned to Home

---

## TESTS

* Token_ShouldExpire
* Provisioning_ShouldCreateDevice
* InvalidToken_ShouldFail

---

# 5. SMARTLOCKS MODULE

## USE CASES

* Unlock Door
* Lock Door
* Emergency Lock
* Get Status

---

## BUSINESS RULES

* Unlock requires full validation pipeline
* Face verification runs last
* All actions audited
* MQTT used for execution

---

## TESTS (STANDARDIZED AS BEHAVIOR)

* Unlock_ValidRequest_ShouldSucceed
* Unlock_InvalidSession_ShouldFail
* Unlock_OutsideSchedule_ShouldFail
* Unlock_LowBattery_ShouldFail
* Unlock_ShouldGenerateAuditLog

---

# 6. MONITORING MODULE

## USE CASES

* Get Dashboard
* Get Alerts
* Get Live Events

---

## TESTS

* Dashboard_ShouldReturnCorrectState
* Alerts_ShouldFilterSeverity

---

# 7. NOTIFICATIONS MODULE

## USE CASES

* Send Notification
* Update Preferences

---

## TESTS

* SendEmail_ShouldSucceed
* UpdatePreferences_ShouldPersist

---

# 8. AUDIT MODULE

## RULES

* Audit logs are immutable
* Every security action must be logged

---

## TESTS

* Audit_ShouldPersist
* Audit_ShouldBeImmutable

---

# 9. AUTOMATION MODULE

## RULES

* No infinite loops allowed
* Rule execution must be bounded
* Actions must be audited

---

## TESTS

* Rule_ShouldTriggerEvent
* Rule_ShouldExecuteAction
* RuleLoop_ShouldBeBlocked

---

# 10. FACE VERIFICATION MODULE

## RULES

* Pluggable provider
* Optional per policy
* Runs last in unlock flow

---

## TESTS

* FaceMatch_ShouldPassCorrectUser
* FaceMatch_ShouldFailInvalidUser
* SpoofDetection_ShouldRejectFake

---

# CROSS-CUTTING TESTS (CRITICAL)

* JWT_Expired_ShouldReject
* InvalidSignature_ShouldFail
* MissingPermission_ShouldReturn403
* RateLimit_ShouldBlockRequests
* AuditLog_AllActions_ShouldExist
