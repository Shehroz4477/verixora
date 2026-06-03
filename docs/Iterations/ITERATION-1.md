# VERIXORA ITERATION 1 – IDENTITY & HOME MANAGEMENT (FINAL VERSION)

---

## ITERATION OBJECTIVE

Implement the Identity and Home (tenant) system so users can register, log in, manage sessions, create and manage Homes, assign roles, and manage API keys.

By the end of Iteration 1:

- Users can register with email verification.
- Users can log in and receive JWT access + refresh tokens.
- JWT expires in 15 minutes; refresh tokens rotate on use.
- Sessions store device fingerprint; unknown devices require OTP.
- Max 5 trusted devices per user enforced.
- Users can create Homes and assign roles.
- API keys can be created, revoked, and rotated.
- API keys stored hashed (SHA-256).
- Max 10 API keys per Home.
- Column-level encryption active for all PII.
- Session entities (Session, RefreshToken, TrustedDevice) owned by Identity module.
- Integration event contracts defined for cross-module communication.
- Mobile and Web Portal have full registration, login, OTP, home management, and API key management flows.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                    |
| ----------------- | ---------------------------------------- |
| Backend Developer | Identity module (all layers), Home aggregate, API keys |
| Mobile Developer  | Registration, login, OTP, home list, API key management |
| Web Developer     | Admin user management, home CRUD, API key management |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**Identity.Domain:**
- `User` aggregate root:
  - `Id` (Guid)
  - `Email` (string, encrypted)
  - `PasswordHash` (string)
  - `EmailVerified` (bool)
  - `PhoneNumber` (string, encrypted, optional)
  - `CreatedAt` (DateTimeOffset)
- `Session` entity:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `DeviceFingerprint` (string)
  - `IpAddress` (string)
  - `IsTrusted` (bool)
  - `CreatedAt` (DateTimeOffset)
  - `ExpiresAt` (DateTimeOffset)
- `TrustedDevice` entity:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `DeviceFingerprint` (string)
  - `AddedAt` (DateTimeOffset)
- `Home` aggregate root:
  - `Id` (Guid)
  - `Name` (string)
  - `MaxDevices` (int, default 20, configurable)
  - `CreatedAt` (DateTimeOffset)
- `HomeMembership` entity:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `UserId` (Guid)
  - `Role` (enum: Owner, Admin, Member, Guest)
  - `JoinedAt` (DateTimeOffset)
- `RefreshToken` entity:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `Token` (string, hashed)
  - `ExpiresAt` (DateTimeOffset)
  - `IsRevoked` (bool)
- `ApiKey` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `KeyHash` (string, SHA-256)
  - `Name` (string)
  - `Permissions` (string, JSON array)
  - `CreatedBy` (Guid)
  - `CreatedAt` (DateTimeOffset)
  - `ExpiresAt` (DateTimeOffset?)
  - `IsRevoked` (bool)
  - `LastUsedAt` (DateTimeOffset?)

**Domain Events:**
- `UserRegistered`
- `EmailVerified`
- `UserLoggedIn`
- `SessionCreated`
- `DeviceTrusted`
- `HomeCreated`
- `MemberAdded`
- `RoleChanged`
- `ApiKeyCreated`
- `ApiKeyRevoked`
- `ApiKeyUsed`

### 1.2 Backend – Application Layer

**Commands:**
- `RegisterUserCommand` → `RegisterUserHandler` + `RegisterUserValidator`
- `VerifyEmailCommand` → `VerifyEmailHandler`
- `LoginCommand` → `LoginHandler` + `LoginValidator`
- `RefreshTokenCommand` → `RefreshTokenHandler`
- `LogoutCommand` → `LogoutHandler`
- `ChangePasswordCommand` → `ChangePasswordHandler`
- `ResetPasswordCommand` → `ResetPasswordHandler`
- `TrustDeviceCommand` → `TrustDeviceHandler`
- `CreateHomeCommand` → `CreateHomeHandler` + `CreateHomeValidator`
- `UpdateHomeCommand` → `UpdateHomeHandler`
- `AddMemberCommand` → `AddMemberHandler`
- `ChangeRoleCommand` → `ChangeRoleHandler`
- `RemoveMemberCommand` → `RemoveMemberHandler`
- `CreateApiKeyCommand` → `CreateApiKeyHandler` + `CreateApiKeyValidator`
- `RevokeApiKeyCommand` → `RevokeApiKeyHandler`
- `RotateApiKeyCommand` → `RotateApiKeyHandler`

**Queries:**
- `GetUserByIdQuery` → `GetUserByIdHandler`
- `GetUserHomesQuery` → `GetUserHomesHandler`
- `GetHomeMembersQuery` → `GetHomeMembersHandler`
- `GetSessionsQuery` → `GetSessionsHandler`
- `GetApiKeysQuery` → `GetApiKeysHandler`

### 1.3 Backend – Infrastructure Layer

**Persistence:**
- `IdentityDbContext` with `DbSet<User>`, `DbSet<Session>`, `DbSet<TrustedDevice>`, `DbSet<Home>`, `DbSet<HomeMembership>`, `DbSet<RefreshToken>`, `DbSet<ApiKey>`.
- EF Core value converters for encrypted columns (Email, PhoneNumber).
- Entity configurations with indexes on Email (unique), HomeId, UserId, KeyHash.

**Services:**
- `JwtTokenService` – generates and validates JWTs (15 min expiry).
- `RefreshTokenService` – generates, rotates, revokes refresh tokens (30 day rolling).
- `PasswordHasher` – BCrypt or Argon2 hashing.
- `DeviceFingerprintService` – computes fingerprint from request metadata.
- `OtpService` – generates and validates OTP for unknown devices.
- `ApiKeyService` – generates, hashes, validates API keys.

**Repositories:**
- `UserRepository`
- `SessionRepository`
- `HomeRepository`
- `RefreshTokenRepository`
- `ApiKeyRepository`

### 1.4 Backend – Presentation Layer

**Controllers:**
- `AuthController`:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/verify-email`
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
  - `POST /api/v1/auth/change-password`
  - `POST /api/v1/auth/reset-password`
  - `POST /api/v1/auth/trust-device`
- `HomeController`:
  - `POST /api/v1/homes`
  - `GET /api/v1/homes`
  - `GET /api/v1/homes/{id}`
  - `PUT /api/v1/homes/{id}`
  - `DELETE /api/v1/homes/{id}`
  - `POST /api/v1/homes/{id}/members`
  - `PUT /api/v1/homes/{id}/members/{userId}/role`
  - `DELETE /api/v1/homes/{id}/members/{userId}`
- `SessionController`:
  - `GET /api/v1/sessions`
  - `DELETE /api/v1/sessions/{id}`
- `ApiKeyController`:
  - `POST /api/v1/api-keys`
  - `GET /api/v1/api-keys`
  - `DELETE /api/v1/api-keys/{id}`
  - `POST /api/v1/api-keys/{id}/rotate`

### 1.5 Backend – Contracts (including Integration Events)

**Identity.Contracts:**
- `Requests/`: `RegisterRequest`, `LoginRequest`, `VerifyEmailRequest`, `RefreshTokenRequest`, `ChangePasswordRequest`, `ResetPasswordRequest`, `CreateHomeRequest`, `UpdateHomeRequest`, `AddMemberRequest`, `ChangeRoleRequest`, `CreateApiKeyRequest`.
- `Responses/`: `AuthResponse`, `UserResponse`, `HomeResponse`, `MemberResponse`, `SessionResponse`, `ApiKeyResponse`.
- `IntegrationEvents/`:
  - `UserRegisteredIntegrationEvent`
  - `UserLoggedInIntegrationEvent`
  - `HomeCreatedIntegrationEvent`
  - `MemberAddedIntegrationEvent`
  - `RoleChangedIntegrationEvent`
  - `ApiKeyCreatedIntegrationEvent`
  - `ApiKeyRevokedIntegrationEvent`

### 1.6 Mobile App

- **Registration Screen:** email, password, confirm password fields.
- **Login Screen:** email, password fields, OTP prompt for unknown devices.
- **Home List Screen:** shows user's homes.
- **Home Detail Screen:** shows members, roles.
- **API Key Management Screen:** create, view, revoke API keys.
- **Session Management:** view and revoke active sessions.
- **Trusted Devices:** manage trusted device list.

### 1.7 Web Portal

- **User Management:** list users, view details, disable accounts.
- **Home Management:** CRUD homes, manage members, assign roles.
- **API Key Management:** create, view, revoke, rotate API keys.
- **Session Overview:** admin view of active sessions.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Identity.Domain (New Files)
```
Identity.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- User.cs
|   +-- Session.cs
|   +-- TrustedDevice.cs
|   +-- Home.cs
|   +-- HomeMembership.cs
|   +-- RefreshToken.cs
|   +-- ApiKey.cs
+-- Enums/
|   +-- HomeRole.cs
+-- Events/
|   +-- UserRegistered.cs
|   +-- EmailVerified.cs
|   +-- UserLoggedIn.cs
|   +-- SessionCreated.cs
|   +-- DeviceTrusted.cs
|   +-- HomeCreated.cs
|   +-- MemberAdded.cs
|   +-- RoleChanged.cs
|   +-- ApiKeyCreated.cs
|   +-- ApiKeyRevoked.cs
|   +-- ApiKeyUsed.cs
+-- ValueObjects/
|   +-- DeviceFingerprint.cs
+-- Services/
    +-- IPasswordHasher.cs
    +-- IJwtTokenService.cs
    +-- IApiKeyService.cs
```

### 2.2 Identity.Application (New Files)
```
Identity.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- RegisterUser/
|   |   +-- RegisterUserCommand.cs
|   |   +-- RegisterUserHandler.cs
|   |   +-- RegisterUserValidator.cs
|   +-- Login/
|   |   +-- LoginCommand.cs
|   |   +-- LoginHandler.cs
|   |   +-- LoginValidator.cs
|   +-- VerifyEmail/
|   |   +-- VerifyEmailCommand.cs
|   |   +-- VerifyEmailHandler.cs
|   +-- RefreshToken/
|   |   +-- RefreshTokenCommand.cs
|   |   +-- RefreshTokenHandler.cs
|   +-- Logout/
|   |   +-- LogoutCommand.cs
|   |   +-- LogoutHandler.cs
|   +-- ChangePassword/
|   |   +-- ChangePasswordCommand.cs
|   |   +-- ChangePasswordHandler.cs
|   +-- ResetPassword/
|   |   +-- ResetPasswordCommand.cs
|   |   +-- ResetPasswordHandler.cs
|   +-- TrustDevice/
|   |   +-- TrustDeviceCommand.cs
|   |   +-- TrustDeviceHandler.cs
|   +-- CreateHome/
|   |   +-- CreateHomeCommand.cs
|   |   +-- CreateHomeHandler.cs
|   |   +-- CreateHomeValidator.cs
|   +-- UpdateHome/
|   |   +-- UpdateHomeCommand.cs
|   |   +-- UpdateHomeHandler.cs
|   +-- AddMember/
|   |   +-- AddMemberCommand.cs
|   |   +-- AddMemberHandler.cs
|   +-- ChangeRole/
|   |   +-- ChangeRoleCommand.cs
|   |   +-- ChangeRoleHandler.cs
|   +-- RemoveMember/
|   |   +-- RemoveMemberCommand.cs
|   |   +-- RemoveMemberHandler.cs
|   +-- CreateApiKey/
|   |   +-- CreateApiKeyCommand.cs
|   |   +-- CreateApiKeyHandler.cs
|   |   +-- CreateApiKeyValidator.cs
|   +-- RevokeApiKey/
|   |   +-- RevokeApiKeyCommand.cs
|   |   +-- RevokeApiKeyHandler.cs
|   +-- RotateApiKey/
|       +-- RotateApiKeyCommand.cs
|       +-- RotateApiKeyHandler.cs
+-- Queries/
|   +-- GetUserById/
|   |   +-- GetUserByIdQuery.cs
|   |   +-- GetUserByIdHandler.cs
|   +-- GetUserHomes/
|   |   +-- GetUserHomesQuery.cs
|   |   +-- GetUserHomesHandler.cs
|   +-- GetHomeMembers/
|   |   +-- GetHomeMembersQuery.cs
|   |   +-- GetHomeMembersHandler.cs
|   +-- GetSessions/
|   |   +-- GetSessionsQuery.cs
|   |   +-- GetSessionsHandler.cs
|   +-- GetApiKeys/
|       +-- GetApiKeysQuery.cs
|       +-- GetApiKeysHandler.cs
+-- DTOs/
|   +-- AuthResponseDto.cs
|   +-- UserDto.cs
|   +-- HomeDto.cs
|   +-- MemberDto.cs
|   +-- SessionDto.cs
|   +-- ApiKeyDto.cs
+-- Interfaces/
    +-- IUserRepository.cs
    +-- ISessionRepository.cs
    +-- IHomeRepository.cs
    +-- IRefreshTokenRepository.cs
    +-- IApiKeyRepository.cs
```

### 2.3 Identity.Infrastructure (New Files)
```
Identity.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- IdentityDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- UserConfiguration.cs
|   |   +-- SessionConfiguration.cs
|   |   +-- TrustedDeviceConfiguration.cs
|   |   +-- HomeConfiguration.cs
|   |   +-- HomeMembershipConfiguration.cs
|   |   +-- RefreshTokenConfiguration.cs
|   |   +-- ApiKeyConfiguration.cs
|   +-- Repositories/
|   |   +-- UserRepository.cs
|   |   +-- SessionRepository.cs
|   |   +-- HomeRepository.cs
|   |   +-- RefreshTokenRepository.cs
|   |   +-- ApiKeyRepository.cs
|   +-- Converters/
|       +-- EncryptionConverter.cs
+-- Services/
|   +-- JwtTokenService.cs
|   +-- RefreshTokenService.cs
|   +-- PasswordHasher.cs
|   +-- DeviceFingerprintService.cs
|   +-- OtpService.cs
|   +-- ApiKeyService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 Identity.Presentation (New Files)
```
Identity.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- AuthController.cs
    +-- HomeController.cs
    +-- SessionController.cs
    +-- ApiKeyController.cs
```

### 2.5 Identity.Contracts (New Files)
```
Identity.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- RegisterRequest.cs
|   +-- LoginRequest.cs
|   +-- VerifyEmailRequest.cs
|   +-- RefreshTokenRequest.cs
|   +-- ChangePasswordRequest.cs
|   +-- ResetPasswordRequest.cs
|   +-- CreateHomeRequest.cs
|   +-- UpdateHomeRequest.cs
|   +-- AddMemberRequest.cs
|   +-- ChangeRoleRequest.cs
|   +-- CreateApiKeyRequest.cs
+-- Responses/
|   +-- AuthResponse.cs
|   +-- UserResponse.cs
|   +-- HomeResponse.cs
|   +-- MemberResponse.cs
|   +-- SessionResponse.cs
|   +-- ApiKeyResponse.cs
+-- IntegrationEvents/
    +-- UserRegisteredIntegrationEvent.cs
    +-- UserLoggedInIntegrationEvent.cs
    +-- HomeCreatedIntegrationEvent.cs
    +-- MemberAddedIntegrationEvent.cs
    +-- RoleChangedIntegrationEvent.cs
    +-- ApiKeyCreatedIntegrationEvent.cs
    +-- ApiKeyRevokedIntegrationEvent.cs
```

---

## PHASE 3: INTEGRATION

- Wire Identity module DI into ApiHost.
- Run EF Core migrations for Identity schema.
- Register API key authentication handler.
- Mobile and Web connect to auth, home, and API key endpoints.
- Email verification flow (mock email service initially).
- OTP flow for unknown devices (mock SMS/email initially).
- CI pipeline runs all new tests including contract tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Identity.Domain)
- `User_Create_ShouldHashPassword`
- `Home_Create_ShouldSetDefaults`
- `HomeMembership_DuplicateRole_ShouldFail`
- `RefreshToken_Rotate_ShouldRevokeOld`
- `Session_Expired_ShouldBeInvalid`
- `TrustedDevice_MaxFive_ShouldFail`
- `ApiKey_Create_ShouldHashKey`
- `ApiKey_Revoke_ShouldInvalidate`
- `ApiKey_MaxTen_ShouldFail`

### 4.2 Application Tests
- `RegisterUser_ValidData_ShouldCreateUser`
- `RegisterUser_DuplicateEmail_ShouldFail`
- `Login_ValidCredentials_ShouldReturnTokens`
- `Login_InvalidPassword_ShouldReject`
- `Login_UnknownDevice_ShouldRequireOtp`
- `Login_FingerprintChanged_ShouldForceReLogin`
- `RefreshToken_Valid_ShouldRotate`
- `RefreshToken_Reuse_ShouldRevokeAll`
- `ResetPassword_ShouldRevokeSessions`
- `CreateHome_ValidData_ShouldCreate`
- `AddMember_ShouldSucceed`
- `AddMember_Duplicate_ShouldFail`
- `CreateApiKey_ValidData_ShouldCreate`
- `CreateApiKey_OverLimit_ShouldFail`
- `RevokeApiKey_ShouldInvalidate`
- `RotateApiKey_ShouldReplaceKey`

### 4.3 Integration Tests
- `AuthController_Register_ShouldReturn201`
- `AuthController_Login_ShouldReturnTokens`
- `ApiKeyController_Create_ShouldReturn201`
- `ApiKeyController_Revoke_ShouldReturn200`
- `HomeController_CRUD_ShouldWork`
- `SessionController_List_ShouldReturnSessions`
- `PII_ShouldBeEncryptedInDatabase`
- `ApiKey_ShouldBeHashedInDatabase`

### 4.4 Contract Tests
- `Identity_Contracts_ShouldMatchPublishedEvents`
- `Identity_IntegrationEvents_ShouldContainRequiredFields`

### 4.5 E2E Tests (Mobile & Web)
- Register → Login → OTP → Home created → Member added.
- API key created → used for authentication → API key revoked.
- Session revocation triggers forced re-login.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Build mobile app for test devices.
- Build web portal for staging.
- Run full validation script including contract tests.

---

## ACCEPTANCE CRITERIA

- [ ] Users can register with email and password.
- [ ] Users can log in and receive JWT + refresh tokens.
- [ ] JWT expires in 15 minutes; refresh tokens rotate.
- [ ] Unknown device triggers OTP challenge.
- [ ] Max 5 trusted devices enforced.
- [ ] Users can create Homes.
- [ ] Owners can add members and assign roles.
- [ ] API keys can be created, revoked, and rotated.
- [ ] API keys stored hashed (SHA-256).
- [ ] Max 10 API keys per Home.
- [ ] All PII encrypted at column level.
- [ ] Session entities owned by Identity module.
- [ ] Integration event contracts defined.
- [ ] Contract tests pass.
- [ ] All auth and home tests pass.
- [ ] Mobile app completes registration → login → home flow.
- [ ] Web portal completes user and home management flow.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 1 Coverage              |
| ---------------------------------- | --------------------------------- |
| User registration & login          | Full implementation               |
| JWT + refresh tokens               | 15min expiry, rotation            |
| Session management                 | Device fingerprint, OTP flow      |
| Max 5 trusted devices              | Enforced in TrustDeviceHandler    |
| Home creation                      | Full CRUD                         |
| Role assignment                    | Owner/Admin/Member/Guest          |
| API key authentication             | Full management lifecycle         |
| ADR-017 (Column encryption)        | Email, phone encrypted            |
| ADR-023 (Sessions merged)          | Session entities in Identity      |
| ADR-024 (API Key Auth)             | ApiKey entity, hashed storage     |
| Integration event contracts        | Identity.Contracts/IntegrationEvents |
| Audit logging                      | Events raised for all actions     |

---

## NEW FILE COUNT

| Layer | New Files |
|-------|-----------|
| Identity.Domain | 19 |
| Identity.Application | 44 |
| Identity.Infrastructure | 20 |
| Identity.Presentation | 4 |
| Identity.Contracts | 24 |
| **Total New** | **111** |

---

**ITERATION 1 COMPLETE.**

All improvements integrated:
- API key authentication
- Sessions merged into Identity
- Integration event contracts