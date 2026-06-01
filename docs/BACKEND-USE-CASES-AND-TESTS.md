# VERIXORA Backend Use Cases And Tests

This document captures backend use cases, business logic, and test coverage targets by module.

## 1. Identity

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 1.1 | RegisterUser | Command | Mobile app registration. Email is required but not verified. Trusted device and session are created. JWT and refresh token are returned. |
| 1.2 | Login | Command | Email and password login. Unknown device requires OTP. Known device logs in directly. Session and tokens are created. |
| 1.3 | VerifyEmail Send OTP | Command | User triggers email verification from Settings. OTP is sent to email. |
| 1.4 | VerifyEmail Confirm OTP | Command | User submits OTP. Email is marked verified. Web Portal access is enabled. |
| 1.5 | ForgotPassword | Command | Sends reset OTP to email. |
| 1.6 | ResetPassword | Command | Validates OTP, sets new password, revokes all sessions. |
| 1.7 | ChangePassword | Command | Requires old password. New password must satisfy complexity rules. |
| 1.8 | Logout | Command | Revokes current session. |
| 1.9 | RefreshToken | Command | Rotates refresh token and invalidates old token. |
| 1.10 | RevokeSession | Command | Revokes a specific session. Authorized users may revoke others. |
| 1.11 | RemoveTrustedDevice | Command | Removes a trusted device. |
| 1.12 | GetProfile | Query | Returns user profile. |
| 1.13 | GetSessions | Query | Lists active sessions for user. |
| 1.14 | GetTrustedDevices | Query | Lists trusted devices for user. |

### Business Logic

- Email is required at registration but is not auto-verified.
- Email verification is triggered manually from Settings.
- Web Portal login is only allowed after email verification.
- Passwords are hashed with Argon2id.
- Access tokens expire after 15 minutes.
- Refresh tokens use 30-day rolling expiration.
- Users may have at most 5 trusted devices.
- Unknown devices require OTP.
- Sessions store device info, IP address, and refresh token hash.
- Concurrent sessions are allowed up to the configured maximum.

### Test Cases

- RegisterUser_WithValidData_ShouldCreateUser
- RegisterUser_WithExistingEmail_ShouldFail
- RegisterUser_WithExistingPhone_ShouldFail
- RegisterUser_ShouldCreateTrustedDevice
- RegisterUser_ShouldCreateSession
- RegisterUser_ShouldReturnTokens
- Login_WithValidCredentials_ShouldReturnTokens
- Login_WithUnknownDevice_ShouldRequireOtp
- Login_WithKnownDevice_ShouldNotRequireOtp
- Login_WithInvalidPassword_ShouldFail
- Login_WithUnverifiedEmail_ForWebPortal_ShouldFail
- VerifyEmail_SendOtp_ShouldSendEmail
- VerifyEmail_ConfirmOtp_ShouldMarkVerified
- VerifyEmail_AlreadyVerified_ShouldFail
- VerifyEmail_WrongOtp_ShouldFail
- ForgotPassword_ShouldSendResetEmail
- ResetPassword_WithValidOtp_ShouldChangePassword
- ResetPassword_ShouldRevokeAllSessions
- ChangePassword_WithCorrectOldPassword_ShouldChange
- Logout_ShouldRevokeCurrentSession
- RefreshToken_ShouldRotateTokens
- RefreshToken_WithInvalidToken_ShouldFail
- RevokeSession_OwnerRevokingOthers_ShouldSucceed
- RevokeSession_FamilyMemberRevokingOthers_ShouldFail
- RemoveTrustedDevice_ShouldSucceed
- AddTrustedDevice_WhenMaxReached_ShouldFail

## 2. Authorization

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 2.1 | CreateRole | Command | Creates a custom role with permissions. |
| 2.2 | UpdateRole | Command | Modifies role permissions. |
| 2.3 | DeleteRole | Command | Deletes a custom role. Default roles cannot be deleted. |
| 2.4 | AssignRole | Command | Assigns role to user. |
| 2.5 | AssignPermission | Command | Assigns specific permission to user. |
| 2.6 | SetDeviceAccess | Command | Defines which doors a user can access. |
| 2.7 | SetScheduleRestriction | Command | Defines time-based access for a user. |
| 2.8 | GetRoles | Query | Lists all roles. |
| 2.9 | GetPermissions | Query | Lists all permissions. |
| 2.10 | GetUserPermissions | Query | Gets effective permissions for a user. |

### Business Logic

- SystemAdmin can do everything.
- Owner can manage only their own property.
- Guest can only access assigned doors within schedule.
- FamilyMember has limited management.
- Technician can only manage devices.
- Permissions are cumulative from role and explicit grants.
- Device-level access overrides role permissions when it is more restrictive.
- Schedule restrictions are mandatory for guests.

### Test Cases

- CreateRole_WithValidData_ShouldSucceed
- UpdateRole_ShouldModifyPermissions
- DeleteRole_SystemRole_ShouldFail
- AssignRole_ToUser_ShouldSucceed
- AssignPermission_Directly_ShouldSucceed
- SetDeviceAccess_ShouldRestrictToSpecificDoors
- SetScheduleRestriction_ShouldEnforceTimeWindows
- SystemAdmin_ShouldBypassAllPermissionChecks
- Guest_ShouldOnlyAccessAssignedDoors
- Guest_OutsideSchedule_ShouldBeDenied

## 3. Devices

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 3.1 | RegisterDevice | Command | Registers a new ESP32 device. |
| 3.2 | UpdateDevice | Command | Updates device metadata. |
| 3.3 | DeleteDevice | Command | Decommissions a device. |
| 3.4 | UpdateFirmware | Command | Triggers asynchronous firmware update. |
| 3.5 | GetDevices | Query | Lists devices. |
| 3.6 | GetDeviceById | Query | Gets device details. |
| 3.7 | GetDeviceHealth | Query | Gets battery, signal, and online status. |

### Business Logic

- A home may have at most 20 devices by default.
- Device must be provisioned before registration.
- Firmware updates are asynchronous.
- Device health is polled via MQTT.

### Test Cases

- RegisterDevice_ShouldSucceed
- RegisterDevice_WhenMaxReached_ShouldFail
- DeleteDevice_ShouldRemoveFromGroups
- UpdateFirmware_ShouldPublishMqttCommand
- GetDeviceHealth_ShouldReturnCurrentStatus

## 4. Provisioning

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 4.1 | GenerateProvisioningToken | Command | Generates a time-limited token. |
| 4.2 | StartProvisioning | Command | Initiates BLE provisioning session. |
| 4.3 | CompleteProvisioning | Command | Finalizes device setup. |
| 4.4 | GetProvisioningStatus | Query | Checks provisioning progress. |

### Business Logic

- Provisioning token expires after 2 minutes.
- Provisioning requires BLE connection.
- Device Wi-Fi credentials are sent securely.

### Test Cases

- GenerateToken_ShouldReturnValidToken
- Token_ShouldExpireAfter2Minutes
- StartProvisioning_ShouldCreateSession
- CompleteProvisioning_ShouldRegisterDevice

## 5. SmartLocks

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 5.1 | UnlockDoor | Command | Runs 10-layer validation, then publishes MQTT command. |
| 5.2 | LockDoor | Command | Locks a specific door. |
| 5.3 | EmergencyLockAll | Command | Locks all doors immediately. |
| 5.4 | ConfigureLockSettings | Command | Sets auto-lock timer and sensitive-door flag. |
| 5.5 | GetLockStatus | Query | Gets current lock state. |
| 5.6 | GetLockHistory | Query | Gets lock/unlock event history. |

### Unlock Validation

| Layer | Check | Approximate Cost |
| --- | --- | --- |
| 1 | JWT valid | 1 ms |
| 2 | Session active | 2 ms |
| 3 | User active | 5 ms |
| 4 | Role allows unlock | 1 ms |
| 5 | Permission `door.unlock` | 1 ms |
| 6 | Device-level access for this door | 5 ms |
| 7 | Schedule restriction | 5 ms |
| 8 | Trusted device | 5 ms |
| 9 | Door online and battery above 5 percent | 2 ms |
| 10 | Face verification when required | 500 ms to 2 seconds |

### Business Logic

- Sensitive doors always require face verification.
- Face verification runs last because it is the most expensive check.
- If any layer fails, unlock is denied immediately.
- Unlock events are stored in AuditLogs.
- SignalR notifications are broadcast to the Web Portal.
- MQTT commands are sent to ESP32 devices only after validation succeeds.

### Test Cases

- UnlockDoor_WithInvalidJwt_ShouldFail
- UnlockDoor_WithExpiredSession_ShouldFail
- UnlockDoor_WithDeactivatedUser_ShouldFail
- UnlockDoor_WithWrongRole_ShouldFail
- UnlockDoor_WithoutPermission_ShouldFail
- UnlockDoor_WithoutDeviceAccess_ShouldFail
- UnlockDoor_OutsideSchedule_ShouldFail
- UnlockDoor_UntrustedDevice_ShouldRequireOtp
- UnlockDoor_DoorOffline_ShouldFail
- UnlockDoor_LowBattery_ShouldFail
- UnlockDoor_FaceVerificationRequired_ShouldTriggerFaceCheck
- UnlockDoor_FaceVerificationFailed_ShouldFail
- UnlockDoor_AllLayersPassed_ShouldPublishMqttCommand
- UnlockDoor_ShouldStoreAuditLog
- UnlockDoor_ShouldBroadcastSignalR

## 6. Monitoring

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 6.1 | GetDashboard | Query | Gets device status counts and recent events. |
| 6.2 | GetAlerts | Query | Gets active and historical alerts. |
| 6.3 | GetLiveEvents | Query | Gets real-time event stream. |

### Test Cases

- GetDashboard_ShouldReturnCorrectCounts
- GetAlerts_ShouldFilterBySeverity
- GetLiveEvents_ShouldReturnRecentActivity

## 7. Notifications

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 7.1 | SendNotification | Command | Sends email, SMS, or push notification. |
| 7.2 | UpdatePreferences | Command | Updates user notification preferences. |
| 7.3 | GetNotifications | Query | Gets notification history. |
| 7.4 | GetTemplates | Query | Lists notification templates. |

### Test Cases

- SendNotification_Email_ShouldSucceed
- SendNotification_Sms_ShouldSucceed
- UpdatePreferences_ShouldSaveSettings
- GetNotifications_ShouldReturnHistory

## 8. Sessions

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 8.1 | CreateSession | Command | Internal use during login. |
| 8.2 | RevokeSession | Command | Revokes a specific session. |
| 8.3 | GetActiveSessions | Query | Lists active sessions for user. |

### Test Cases

- CreateSession_ShouldStoreSession
- RevokeSession_ShouldInvalidateRefreshToken
- GetActiveSessions_ShouldReturnCorrectCount

## 9. AuditLogs

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 9.1 | WriteAuditLog | Command | Writes immutable audit entry. |
| 9.2 | GetAuditLogs | Query | Searches and filters logs. |
| 9.3 | ExportAuditLogs | Query | Exports logs to CSV or PDF. |

### Business Logic

- Audit logs are immutable.
- Audit storage is write-once for normal application paths.
- Sensitive operations must be logged.

### Test Cases

- WriteAuditLog_ShouldPersistEntry
- GetAuditLogs_ShouldFilterByDate
- ExportAuditLogs_ShouldReturnCsv
- AuditLogs_ShouldBeImmutable

## 10. Reports

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 10.1 | GenerateAccessReport | Query | Generates access pattern report. |
| 10.2 | GenerateUserReport | Query | Generates user activity report. |
| 10.3 | GenerateDeviceReport | Query | Generates device health trend report. |
| 10.4 | GenerateSecurityReport | Query | Generates security incident report. |

### Test Cases

- GenerateAccessReport_ShouldReturnData
- GenerateDeviceReport_ShouldIncludeBatteryTrends

## 11. Automation

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 11.1 | CreateRule | Command | Creates IF-THEN rule. |
| 11.2 | UpdateRule | Command | Modifies rule. |
| 11.3 | DeleteRule | Command | Deletes rule. |
| 11.4 | ToggleRule | Command | Enables or disables rule. |
| 11.5 | GetRules | Query | Lists rules. |
| 11.6 | GetRuleHistory | Query | Gets execution log. |

### Business Logic

- Rules are evaluated on triggers.
- Supported triggers include time, event, and condition.
- Supported actions include lock, unlock, notify, and activate siren.

### Test Cases

- CreateRule_TimeBased_ShouldSchedule
- CreateRule_EventBased_ShouldListenForEvent
- ToggleRule_ShouldEnableDisable
- RuleExecution_ShouldLogHistory

## 12. Security

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 12.1 | UpdateSecurityPolicy | Command | Updates lockout thresholds and password rules. |
| 12.2 | ManageIpWhitelist | Command | Adds or removes IP addresses. |
| 12.3 | GetSecuritySettings | Query | Gets current security configuration. |

### Test Cases

- UpdateSecurityPolicy_ShouldPersistSettings
- ManageIpWhitelist_ShouldAddEntry
- IpWhitelist_ShouldBlockUnauthorizedIp

## 13. FaceVerification

### Use Cases

| Id | Use Case | Type | Description |
| --- | --- | --- | --- |
| 13.1 | EnrollFace | Command | Uploads face images and generates embeddings. |
| 13.2 | DeleteFace | Command | Removes face data. |
| 13.3 | GetEnrollmentStatus | Query | Checks whether face is enrolled. |
| 13.4 | GetVerificationLogs | Query | Gets face verification history. |

### Business Logic

- Face embeddings are encrypted at rest.
- Anti-spoofing check runs during verification.
- Confidence threshold is configurable per door.

### Test Cases

- EnrollFace_ShouldGenerateEmbeddings
- EnrollFace_WithPoorQuality_ShouldReject
- DeleteFace_ShouldRemoveData
- FaceVerification_ShouldMatchCorrectFace
- FaceVerification_ShouldRejectWrongFace
- FaceVerification_AntiSpoofing_ShouldDetectPhoto

## Cross-Cutting Test Cases

| Id | Test Case | Description |
| --- | --- | --- |
| C1 | Authentication_ExpiredToken_ShouldReject | JWT expiry handling. |
| C2 | Authentication_InvalidSignature_ShouldReject | Token tampering. |
| C3 | Authorization_MissingPermission_ShouldReturn403 | Permission enforcement. |
| C4 | RateLimiting_TooManyRequests_ShouldReturn429 | Brute-force protection. |
| C5 | GlobalExceptionHandler_ShouldReturnProblemDetails | Consistent error responses. |
| C6 | AuditLog_EverySensitiveOperation_ShouldBeLogged | Audit trail completeness. |
