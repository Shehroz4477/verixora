# VERIXORA BACKEND – COMPLETE FILE HIERARCHY (PART 2 OF 6)

---

## MODULE: IDENTITY

```
src/Modules/Identity/
+-- Identity.Domain/
|   +-- Identity.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- User.cs
|   |   +-- Session.cs
|   |   +-- TrustedDevice.cs
|   |   +-- Home.cs
|   |   +-- HomeMembership.cs
|   |   +-- RefreshToken.cs
|   |   +-- ApiKey.cs
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
|   |   +-- ApiKeyCreated.cs
|   |   +-- ApiKeyRevoked.cs
|   |   +-- ApiKeyUsed.cs
|   +-- ValueObjects/
|   |   +-- DeviceFingerprint.cs
|   +-- Services/
|       +-- IPasswordHasher.cs
|       +-- IJwtTokenService.cs
|       +-- IApiKeyService.cs
+-- Identity.Application/
|   +-- Identity.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
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
|   |   +-- UpdateHome/
|   |   |   +-- UpdateHomeCommand.cs
|   |   |   +-- UpdateHomeHandler.cs
|   |   +-- AddMember/
|   |   |   +-- AddMemberCommand.cs
|   |   |   +-- AddMemberHandler.cs
|   |   +-- ChangeRole/
|   |   |   +-- ChangeRoleCommand.cs
|   |   |   +-- ChangeRoleHandler.cs
|   |   +-- RemoveMember/
|   |   |   +-- RemoveMemberCommand.cs
|   |   |   +-- RemoveMemberHandler.cs
|   |   +-- CreateApiKey/
|   |   |   +-- CreateApiKeyCommand.cs
|   |   |   +-- CreateApiKeyHandler.cs
|   |   |   +-- CreateApiKeyValidator.cs
|   |   +-- RevokeApiKey/
|   |   |   +-- RevokeApiKeyCommand.cs
|   |   |   +-- RevokeApiKeyHandler.cs
|   |   +-- RotateApiKey/
|   |       +-- RotateApiKeyCommand.cs
|   |       +-- RotateApiKeyHandler.cs
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
|   |   |   +-- GetSessionsQuery.cs
|   |   |   +-- GetSessionsHandler.cs
|   |   +-- GetApiKeys/
|   |       +-- GetApiKeysQuery.cs
|   |       +-- GetApiKeysHandler.cs
|   +-- DTOs/
|   |   +-- AuthResponseDto.cs
|   |   +-- UserDto.cs
|   |   +-- HomeDto.cs
|   |   +-- MemberDto.cs
|   |   +-- SessionDto.cs
|   |   +-- ApiKeyDto.cs
|   +-- Interfaces/
|       +-- IUserRepository.cs
|       +-- ISessionRepository.cs
|       +-- IHomeRepository.cs
|       +-- IRefreshTokenRepository.cs
|       +-- IApiKeyRepository.cs
+-- Identity.Infrastructure/
|   +-- Identity.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- IdentityDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- UserConfiguration.cs
|   |   |   +-- SessionConfiguration.cs
|   |   |   +-- TrustedDeviceConfiguration.cs
|   |   |   +-- HomeConfiguration.cs
|   |   |   +-- HomeMembershipConfiguration.cs
|   |   |   +-- RefreshTokenConfiguration.cs
|   |   |   +-- ApiKeyConfiguration.cs
|   |   +-- Repositories/
|   |   |   +-- UserRepository.cs
|   |   |   +-- SessionRepository.cs
|   |   |   +-- HomeRepository.cs
|   |   |   +-- RefreshTokenRepository.cs
|   |   |   +-- ApiKeyRepository.cs
|   |   +-- Converters/
|   |       +-- EncryptionConverter.cs
|   +-- Services/
|   |   +-- JwtTokenService.cs
|   |   +-- RefreshTokenService.cs
|   |   +-- PasswordHasher.cs
|   |   +-- DeviceFingerprintService.cs
|   |   +-- OtpService.cs
|   |   +-- ApiKeyService.cs
|   +-- Migrations/
+-- Identity.Presentation/
|   +-- Identity.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- AuthController.cs
|       +-- HomeController.cs
|       +-- SessionController.cs
|       +-- ApiKeyController.cs
+-- Identity.Contracts/
    +-- Identity.Contracts.csproj
    +-- GlobalUsings.cs
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
    |   +-- UserRegisteredIntegrationEvent.cs
    |   +-- UserLoggedInIntegrationEvent.cs
    |   +-- HomeCreatedIntegrationEvent.cs
    |   +-- MemberAddedIntegrationEvent.cs
    |   +-- RoleChangedIntegrationEvent.cs
    |   +-- ApiKeyCreatedIntegrationEvent.cs
    |   +-- ApiKeyRevokedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: AUTHORIZATION

```
src/Modules/Authorization/
+-- Authorization.Domain/
|   +-- Authorization.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- Role.cs
|   |   +-- Permission.cs
|   |   +-- RolePermission.cs
|   |   +-- Policy.cs
|   |   +-- DeviceAccess.cs
|   +-- Enums/
|   |   +-- PolicyType.cs
|   |   +-- AccessResult.cs
|   +-- ValueObjects/
|   |   +-- ScheduleRestriction.cs
|   +-- Events/
|   |   +-- RoleCreated.cs
|   |   +-- RolePermissionChanged.cs
|   |   +-- PolicyCreated.cs
|   |   +-- PolicyUpdated.cs
|   |   +-- PolicyDeleted.cs
|   |   +-- AccessEvaluated.cs
|   |   +-- AccessDenied.cs
|   +-- Services/
|       +-- IAccessEvaluationEngine.cs
+-- Authorization.Application/
|   +-- Authorization.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- CreateRole/
|   |   |   +-- CreateRoleCommand.cs
|   |   |   +-- CreateRoleHandler.cs
|   |   |   +-- CreateRoleValidator.cs
|   |   +-- UpdateRole/
|   |   |   +-- UpdateRoleCommand.cs
|   |   |   +-- UpdateRoleHandler.cs
|   |   +-- DeleteRole/
|   |   |   +-- DeleteRoleCommand.cs
|   |   |   +-- DeleteRoleHandler.cs
|   |   +-- AssignPermission/
|   |   |   +-- AssignPermissionCommand.cs
|   |   |   +-- AssignPermissionHandler.cs
|   |   +-- RemovePermission/
|   |   |   +-- RemovePermissionCommand.cs
|   |   |   +-- RemovePermissionHandler.cs
|   |   +-- CreatePolicy/
|   |   |   +-- CreatePolicyCommand.cs
|   |   |   +-- CreatePolicyHandler.cs
|   |   |   +-- CreatePolicyValidator.cs
|   |   +-- UpdatePolicy/
|   |   |   +-- UpdatePolicyCommand.cs
|   |   |   +-- UpdatePolicyHandler.cs
|   |   +-- DeletePolicy/
|   |   |   +-- DeletePolicyCommand.cs
|   |   |   +-- DeletePolicyHandler.cs
|   |   +-- SetDeviceAccess/
|   |   |   +-- SetDeviceAccessCommand.cs
|   |   |   +-- SetDeviceAccessHandler.cs
|   |   |   +-- SetDeviceAccessValidator.cs
|   |   +-- EvaluateAccess/
|   |       +-- EvaluateAccessQuery.cs
|   |       +-- EvaluateAccessHandler.cs
|   +-- Queries/
|   |   +-- GetRolesByHome/
|   |   |   +-- GetRolesByHomeQuery.cs
|   |   |   +-- GetRolesByHomeHandler.cs
|   |   +-- GetPoliciesByHome/
|   |   |   +-- GetPoliciesByHomeQuery.cs
|   |   |   +-- GetPoliciesByHomeHandler.cs
|   |   +-- GetDeviceAccess/
|   |   |   +-- GetDeviceAccessQuery.cs
|   |   |   +-- GetDeviceAccessHandler.cs
|   |   +-- GetUserPermissions/
|   |       +-- GetUserPermissionsQuery.cs
|   |       +-- GetUserPermissionsHandler.cs
|   +-- Services/
|   |   +-- IAuthorizationCacheService.cs
|   +-- DTOs/
|   |   +-- RoleDto.cs
|   |   +-- PolicyDto.cs
|   |   +-- DeviceAccessDto.cs
|   |   +-- AccessEvaluationDto.cs
|   |   +-- PermissionDto.cs
|   +-- Interfaces/
|       +-- IRoleRepository.cs
|       +-- IPolicyRepository.cs
|       +-- IDeviceAccessRepository.cs
|       +-- IPermissionRepository.cs
+-- Authorization.Infrastructure/
|   +-- Authorization.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AuthorizationDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- RoleConfiguration.cs
|   |   |   +-- PermissionConfiguration.cs
|   |   |   +-- RolePermissionConfiguration.cs
|   |   |   +-- PolicyConfiguration.cs
|   |   |   +-- DeviceAccessConfiguration.cs
|   |   +-- Repositories/
|   |       +-- RoleRepository.cs
|   |       +-- PolicyRepository.cs
|   |       +-- DeviceAccessRepository.cs
|   |       +-- PermissionRepository.cs
|   +-- Services/
|   |   +-- AccessEvaluationEngine.cs
|   |   +-- AuthorizationCacheService.cs
|   +-- Migrations/
+-- Authorization.Presentation/
|   +-- Authorization.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- RoleController.cs
|       +-- PolicyController.cs
|       +-- DeviceAccessController.cs
+-- Authorization.Contracts/
    +-- Authorization.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- CreateRoleRequest.cs
    |   +-- UpdateRoleRequest.cs
    |   +-- AssignPermissionRequest.cs
    |   +-- CreatePolicyRequest.cs
    |   +-- UpdatePolicyRequest.cs
    |   +-- SetDeviceAccessRequest.cs
    |   +-- EvaluateAccessRequest.cs
    +-- Responses/
    |   +-- RoleResponse.cs
    |   +-- PolicyResponse.cs
    |   +-- DeviceAccessResponse.cs
    |   +-- AccessEvaluationResponse.cs
    |   +-- PermissionResponse.cs
    +-- IntegrationEvents/
    |   +-- RolePermissionChangedIntegrationEvent.cs
    |   +-- AccessEvaluatedIntegrationEvent.cs
    |   +-- AccessDeniedIntegrationEvent.cs
    +-- CHANGELOG.md
```
