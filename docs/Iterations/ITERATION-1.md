# VERIXORA ITERATION 1 – IDENTITY & HOME MANAGEMENT

---

## ITERATION OBJECTIVE

Implement the Identity and Home (tenant) system so users can register, log in, manage sessions, create and manage Homes, and assign roles.

By the end of Iteration 1:

- Users can register with email verification.
- Users can log in and receive JWT access + refresh tokens.
- JWT expires in 15 minutes; refresh tokens rotate on use.
- Sessions store device fingerprint; unknown devices require OTP.
- Max 5 trusted devices per user enforced.
- Users can create Homes and assign roles.
- Column-level encryption active for all PII.
- Mobile and Web Portal have full registration, login, OTP, and home management flows.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                    |
| ----------------- | ---------------------------------------- |
| Backend Developer | Identity module (all layers), Home aggregate |
| Mobile Developer  | Registration, login, OTP, home list      |
| Web Developer     | Admin user management, home CRUD         |

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

**Domain Events:**
- `UserRegistered`
- `EmailVerified`
- `UserLoggedIn`
- `SessionCreated`
- `DeviceTrusted`
- `HomeCreated`
- `MemberAdded`
- `RoleChanged`

### 1.2 Backend – Application Layer

**Commands:**
- `RegisterUserCommand` → `RegisterUserHandler`
- `VerifyEmailCommand` → `VerifyEmailHandler`
- `LoginCommand` → `LoginHandler`
- `RefreshTokenCommand` → `RefreshTokenHandler`
- `LogoutCommand` → `LogoutHandler`
- `ChangePasswordCommand` → `ChangePasswordHandler`
- `ResetPasswordCommand` → `ResetPasswordHandler`
- `TrustDeviceCommand` → `TrustDeviceHandler`
- `CreateHomeCommand` → `CreateHomeHandler`
- `AddMemberCommand` → `AddMemberHandler`
- `ChangeRoleCommand` → `ChangeRoleHandler`
- `RemoveMemberCommand` → `RemoveMemberHandler`

**Queries:**
- `GetUserByIdQuery` → `GetUserByIdHandler`
- `GetUserHomesQuery` → `GetUserHomesHandler`
- `GetHomeMembersQuery` → `GetHomeMembersHandler`
- `GetSessionsQuery` → `GetSessionsHandler`

**Validators (FluentValidation):**
- `RegisterUserCommandValidator`
- `LoginCommandValidator`
- `CreateHomeCommandValidator`
- `AddMemberCommandValidator`

### 1.3 Backend – Infrastructure Layer

**Persistence:**
- `IdentityDbContext` with `DbSet<User>`, `DbSet<Session>`, `DbSet<TrustedDevice>`, `DbSet<Home>`, `DbSet<HomeMembership>`, `DbSet<RefreshToken>`.
- EF Core value converters for encrypted columns (Email, PhoneNumber).
- Entity configurations with indexes on Email (unique), HomeId, UserId.

**Services:**
- `JwtTokenService` – generates and validates JWTs (15 min expiry).
- `RefreshTokenService` – generates, rotates, revokes refresh tokens (30 day rolling).
- `PasswordHasher` – BCrypt or Argon2 hashing.
- `DeviceFingerprintService` – computes fingerprint from request metadata.
- `OtpService` – generates and validates OTP for unknown devices.

**Repositories:**
- `UserRepository`
- `SessionRepository`
- `HomeRepository`
- `RefreshTokenRepository`

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

### 1.5 Mobile App

- **Registration Screen:** email, password, confirm password fields.
- **Login Screen:** email, password fields, OTP prompt for unknown devices.
- **Home List Screen:** shows user's homes.
- **Home Detail Screen:** shows members, roles.
- **Session Management:** view and revoke active sessions.
- **Trusted Devices:** manage trusted device list.

### 1.6 Web Portal

- **User Management:** list users, view details, disable accounts.
- **Home Management:** CRUD homes, manage members, assign roles.
- **Session Overview:** admin view of active sessions.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Backend – Identity.Domain

**New Files:**
```
Identity.Domain/
+-- Entities/
|   +-- User.cs
|   +-- Session.cs
|   +-- TrustedDevice.cs
|   +-- Home.cs
|   +-- HomeMembership.cs
|   +-- RefreshToken.cs
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
+-- ValueObjects/
|   +-- DeviceFingerprint.cs
+-- Services/
    +-- IPasswordHasher.cs
    +-- IJwtTokenService.cs
```

### 2.2 Backend – Identity.Application

**New Files:**
```
Identity.Application/
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
|   +-- AddMember/
|   |   +-- AddMemberCommand.cs
|   |   +-- AddMemberHandler.cs
|   +-- ChangeRole/
|   |   +-- ChangeRoleCommand.cs
|   |   +-- ChangeRoleHandler.cs
|   +-- RemoveMember/
|       +-- RemoveMemberCommand.cs
|       +-- RemoveMemberHandler.cs
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
|       +-- GetSessionsQuery.cs
|       +-- GetSessionsHandler.cs
+-- DTOs/
|   +-- AuthResponseDto.cs
|   +-- UserDto.cs
|   +-- HomeDto.cs
|   +-- MemberDto.cs
|   +-- SessionDto.cs
+-- Interfaces/
    +-- IUserRepository.cs
    +-- ISessionRepository.cs
    +-- IHomeRepository.cs
    +-- IRefreshTokenRepository.cs
```

### 2.3 Backend – Identity.Infrastructure

**New Files:**
```
Identity.Infrastructure/
+-- Persistence/
|   +-- IdentityDbContext.cs (updated)
|   +-- Configurations/
|   |   +-- UserConfiguration.cs
|   |   +-- SessionConfiguration.cs
|   |   +-- TrustedDeviceConfiguration.cs
|   |   +-- HomeConfiguration.cs
|   |   +-- HomeMembershipConfiguration.cs
|   |   +-- RefreshTokenConfiguration.cs
|   +-- Repositories/
|   |   +-- UserRepository.cs
|   |   +-- SessionRepository.cs
|   |   +-- HomeRepository.cs
|   |   +-- RefreshTokenRepository.cs
|   +-- Converters/
|       +-- EncryptionConverter.cs
+-- Services/
|   +-- JwtTokenService.cs
|   +-- RefreshTokenService.cs
|   +-- PasswordHasher.cs
|   +-- DeviceFingerprintService.cs
|   +-- OtpService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 Backend – Identity.Presentation

**New Files:**
```
Identity.Presentation/
+-- Controllers/
    +-- AuthController.cs
    +-- HomeController.cs
    +-- SessionController.cs
```

### 2.5 Backend – Identity.Contracts

**New Files:**
```
Identity.Contracts/
+-- Requests/
|   +-- RegisterRequest.cs
|   +-- LoginRequest.cs
|   +-- VerifyEmailRequest.cs
|   +-- RefreshTokenRequest.cs
|   +-- ChangePasswordRequest.cs
|   +-- ResetPasswordRequest.cs
|   +-- CreateHomeRequest.cs
|   +-- AddMemberRequest.cs
|   +-- ChangeRoleRequest.cs
+-- Responses/
    +-- AuthResponse.cs
    +-- UserResponse.cs
    +-- HomeResponse.cs
    +-- MemberResponse.cs
    +-- SessionResponse.cs
```

### 2.6 Mobile App

**New Pages/Files:**
```
src/app/
+-- pages/
|   +-- register/
|   |   +-- register.page.ts
|   |   +-- register.page.html
|   |   +-- register.page.scss
|   +-- login/
|   |   +-- login.page.ts
|   |   +-- login.page.html
|   |   +-- login.page.scss
|   +-- otp-verify/
|   |   +-- otp-verify.page.ts
|   |   +-- otp-verify.page.html
|   |   +-- otp-verify.page.scss
|   +-- home-list/
|   |   +-- home-list.page.ts
|   |   +-- home-list.page.html
|   |   +-- home-list.page.scss
|   +-- home-detail/
|   |   +-- home-detail.page.ts
|   |   +-- home-detail.page.html
|   |   +-- home-detail.page.scss
|   +-- sessions/
|   |   +-- sessions.page.ts
|   |   +-- sessions.page.html
|   |   +-- sessions.page.scss
|   +-- trusted-devices/
|       +-- trusted-devices.page.ts
|       +-- trusted-devices.page.html
|       +-- trusted-devices.page.scss
+-- services/
|   +-- auth.service.ts
|   +-- home.service.ts
|   +-- session.service.ts
|   +-- token.interceptor.ts
+-- models/
    +-- user.model.ts
    +-- home.model.ts
    +-- session.model.ts
```

### 2.7 Web Portal

**New Components/Files:**
```
src/app/
+-- features/
|   +-- auth/
|   |   +-- login/
|   |   |   +-- login.component.ts
|   |   |   +-- login.component.html
|   |   |   +-- login.component.scss
|   +-- homes/
|   |   +-- home-list/
|   |   |   +-- home-list.component.ts
|   |   |   +-- home-list.component.html
|   |   |   +-- home-list.component.scss
|   |   +-- home-detail/
|   |   |   +-- home-detail.component.ts
|   |   |   +-- home-detail.component.html
|   |   |   +-- home-detail.component.scss
|   |   +-- home-create/
|   |       +-- home-create.component.ts
|   |       +-- home-create.component.html
|   |       +-- home-create.component.scss
|   +-- users/
|       +-- user-list/
|       |   +-- user-list.component.ts
|       |   +-- user-list.component.html
|       |   +-- user-list.component.scss
|       +-- user-detail/
|           +-- user-detail.component.ts
|           +-- user-detail.component.html
|           +-- user-detail.component.scss
+-- services/
|   +-- auth.service.ts
|   +-- home.service.ts
|   +-- user.service.ts
|   +-- auth.interceptor.ts
+-- models/
    +-- user.model.ts
    +-- home.model.ts
    +-- session.model.ts
```

---

## PHASE 3: INTEGRATION

- Wire Identity module DI into ApiHost (`services.AddIdentityModule()`).
- Run EF Core migrations for Identity schema.
- Mobile and Web connect to auth and home endpoints.
- Email verification flow (mock email service initially).
- OTP flow for unknown devices (mock SMS/email initially).
- CI pipeline runs all new tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Identity.Domain)
- `User_Create_ShouldHashPassword`
- `Home_Create_ShouldSetDefaults`
- `HomeMembership_DuplicateRole_ShouldFail`
- `RefreshToken_Rotate_ShouldRevokeOld`
- `Session_Expired_ShouldBeInvalid`
- `TrustedDevice_MaxFive_ShouldFail`

### 4.2 Application Tests (Identity.Application)
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

### 4.3 Integration Tests
- `AuthController_Register_ShouldReturn201`
- `AuthController_Login_ShouldReturnTokens`
- `HomeController_CRUD_ShouldWork`
- `SessionController_List_ShouldReturnSessions`
- `PII_ShouldBeEncryptedInDatabase`

### 4.4 E2E Tests (Mobile & Web)
- Register → Login → OTP → Home created → Member added.
- Session revocation triggers forced re-login.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Build mobile app for test devices.
- Build web portal for staging.
- Run full validation script.

---

## ACCEPTANCE CRITERIA

- [ ] Users can register with email and password.
- [ ] Users can log in and receive JWT + refresh tokens.
- [ ] JWT expires in 15 minutes; refresh tokens rotate.
- [ ] Unknown device triggers OTP challenge.
- [ ] Max 5 trusted devices enforced.
- [ ] Users can create Homes.
- [ ] Owners can add members and assign roles.
- [ ] All PII encrypted at column level.
- [ ] All auth and home tests pass.
- [ ] Mobile app completes registration → login → home flow.
- [ ] Web portal completes user and home management flow.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement       | Iteration 1 Coverage              |
| ----------------------------- | --------------------------------- |
| User registration & login     | Full implementation               |
| JWT + refresh tokens          | 15min expiry, rotation            |
| Session management            | Device fingerprint, OTP flow      |
| Max 5 trusted devices         | Enforced in TrustDeviceHandler    |
| Home creation                 | Full CRUD                         |
| Role assignment               | Owner/Admin/Member/Guest          |
| ADR-017 (Column encryption)   | Email, phone encrypted            |
| Audit logging                 | Events raised for all actions     |

---

## FULL BACKEND HIERARCHY (ITERATION 1 ADDITIONS)

Only new and modified files shown. All Iteration 0 files remain.

```
src/Modules/Identity/
|
+-- Identity.Domain/
|   +-- (existing files from Iteration 0)
|   +-- Entities/
|   |   +-- User.cs
|   |   +-- Session.cs
|   |   +-- TrustedDevice.cs
|   |   +-- Home.cs
|   |   +-- HomeMembership.cs
|   |   +-- RefreshToken.cs
|   +-- Enums/
|   |   +-- HomeRole.cs
|   +-- Events/
|   |   +-- UserRegistered.cs
|   |   +-- EmailVerified.cs
|   |   +-- UserLoggedIn.cs
|   |   +-- SessionCreated.cs
|   |   +-- DeviceTrusted.cs
|   |   +-- HomeCreated.cs
|   |   +-- MemberAdded.cs
|   |   +-- RoleChanged.cs
|   +-- ValueObjects/
|   |   +-- DeviceFingerprint.cs
|   +-- Services/
|       +-- IPasswordHasher.cs
|       +-- IJwtTokenService.cs
|
+-- Identity.Application/
|   +-- (existing files from Iteration 0)
|   +-- Commands/
|   |   +-- RegisterUser/
|   |   |   +-- RegisterUserCommand.cs
|   |   |   +-- RegisterUserHandler.cs
|   |   |   +-- RegisterUserValidator.cs
|   |   +-- Login/
|   |   |   +-- LoginCommand.cs
|   |   |   +-- LoginHandler.cs
|   |   |   +-- LoginValidator.cs
|   |   +-- VerifyEmail/
|   |   |   +-- VerifyEmailCommand.cs
|   |   |   +-- VerifyEmailHandler.cs
|   |   +-- RefreshToken/
|   |   |   +-- RefreshTokenCommand.cs
|   |   |   +-- RefreshTokenHandler.cs
|   |   +-- Logout/
|   |   |   +-- LogoutCommand.cs
|   |   |   +-- LogoutHandler.cs
|   |   +-- ChangePassword/
|   |   |   +-- ChangePasswordCommand.cs
|   |   |   +-- ChangePasswordHandler.cs
|   |   +-- ResetPassword/
|   |   |   +-- ResetPasswordCommand.cs
|   |   |   +-- ResetPasswordHandler.cs
|   |   +-- TrustDevice/
|   |   |   +-- TrustDeviceCommand.cs
|   |   |   +-- TrustDeviceHandler.cs
|   |   +-- CreateHome/
|   |   |   +-- CreateHomeCommand.cs
|   |   |   +-- CreateHomeHandler.cs
|   |   |   +-- CreateHomeValidator.cs
|   |   +-- AddMember/
|   |   |   +-- AddMemberCommand.cs
|   |   |   +-- AddMemberHandler.cs
|   |   +-- ChangeRole/
|   |   |   +-- ChangeRoleCommand.cs
|   |   |   +-- ChangeRoleHandler.cs
|   |   +-- RemoveMember/
|   |       +-- RemoveMemberCommand.cs
|   |       +-- RemoveMemberHandler.cs
|   +-- Queries/
|   |   +-- GetUserById/
|   |   |   +-- GetUserByIdQuery.cs
|   |   |   +-- GetUserByIdHandler.cs
|   |   +-- GetUserHomes/
|   |   |   +-- GetUserHomesQuery.cs
|   |   |   +-- GetUserHomesHandler.cs
|   |   +-- GetHomeMembers/
|   |   |   +-- GetHomeMembersQuery.cs
|   |   |   +-- GetHomeMembersHandler.cs
|   |   +-- GetSessions/
|   |       +-- GetSessionsQuery.cs
|   |       +-- GetSessionsHandler.cs
|   +-- DTOs/
|   |   +-- AuthResponseDto.cs
|   |   +-- UserDto.cs
|   |   +-- HomeDto.cs
|   |   +-- MemberDto.cs
|   |   +-- SessionDto.cs
|   +-- Interfaces/
|       +-- IUserRepository.cs
|       +-- ISessionRepository.cs
|       +-- IHomeRepository.cs
|       +-- IRefreshTokenRepository.cs
|
+-- Identity.Infrastructure/
|   +-- (existing files from Iteration 0)
|   +-- Persistence/
|   |   +-- IdentityDbContext.cs (updated)
|   |   +-- Configurations/
|   |   |   +-- UserConfiguration.cs
|   |   |   +-- SessionConfiguration.cs
|   |   |   +-- TrustedDeviceConfiguration.cs
|   |   |   +-- HomeConfiguration.cs
|   |   |   +-- HomeMembershipConfiguration.cs
|   |   |   +-- RefreshTokenConfiguration.cs
|   |   +-- Repositories/
|   |   |   +-- UserRepository.cs
|   |   |   +-- SessionRepository.cs
|   |   |   +-- HomeRepository.cs
|   |   |   +-- RefreshTokenRepository.cs
|   |   +-- Converters/
|   |       +-- EncryptionConverter.cs
|   +-- Services/
|   |   +-- JwtTokenService.cs
|   |   +-- RefreshTokenService.cs
|   |   +-- PasswordHasher.cs
|   |   +-- DeviceFingerprintService.cs
|   |   +-- OtpService.cs
|   +-- Migrations/
|       +-- (EF Core generated)
|
+-- Identity.Presentation/
|   +-- (existing files from Iteration 0)
|   +-- Controllers/
|       +-- AuthController.cs
|       +-- HomeController.cs
|       +-- SessionController.cs
|
+-- Identity.Contracts/
    +-- (existing files from Iteration 0)
    +-- Requests/
    |   +-- RegisterRequest.cs
    |   +-- LoginRequest.cs
    |   +-- VerifyEmailRequest.cs
    |   +-- RefreshTokenRequest.cs
    |   +-- ChangePasswordRequest.cs
    |   +-- ResetPasswordRequest.cs
    |   +-- CreateHomeRequest.cs
    |   +-- AddMemberRequest.cs
    |   +-- ChangeRoleRequest.cs
    +-- Responses/
        +-- AuthResponse.cs
        +-- UserResponse.cs
        +-- HomeResponse.cs
        +-- MemberResponse.cs
        +-- SessionResponse.cs
```

---

## IDENTITY MODULE FILE COUNT (NEW)

| Layer | New Files |
|-------|-----------|
| Domain | 16 |
| Application | 38 |
| Infrastructure | 18 |
| Presentation | 3 |
| Contracts | 12 |
| **Total New** | **87** |

---

**ITERATION 1 DOCUMENT COMPLETE**
