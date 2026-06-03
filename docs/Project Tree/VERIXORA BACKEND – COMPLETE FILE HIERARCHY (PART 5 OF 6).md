# VERIXORA BACKEND – COMPLETE FILE HIERARCHY (PART 5 OF 6)

---

## MODULE: AUTOMATION

```
src/Modules/Automation/
+-- Automation.Domain/
|   +-- Automation.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- AutomationRule.cs
|   |   +-- AutomationExecution.cs
|   +-- Enums/
|   |   +-- TriggerType.cs
|   |   +-- ConditionOperator.cs
|   |   +-- ActionType.cs
|   |   +-- ExecutionResult.cs
|   +-- ValueObjects/
|   |   +-- AutomationTrigger.cs
|   |   +-- AutomationCondition.cs
|   |   +-- AutomationAction.cs
|   +-- Events/
|   |   +-- AutomationRuleTriggered.cs
|   |   +-- AutomationRuleExecuted.cs
|   |   +-- AutomationRuleBlocked.cs
|   |   +-- AutomationLoopDetected.cs
|   +-- Services/
|       +-- IAutomationSafetyService.cs
+-- Automation.Application/
|   +-- Automation.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- CreateRule/
|   |   |   +-- CreateRuleCommand.cs
|   |   |   +-- CreateRuleHandler.cs
|   |   |   +-- CreateRuleValidator.cs
|   |   +-- UpdateRule/
|   |   |   +-- UpdateRuleCommand.cs
|   |   |   +-- UpdateRuleHandler.cs
|   |   +-- DeleteRule/
|   |   |   +-- DeleteRuleCommand.cs
|   |   |   +-- DeleteRuleHandler.cs
|   |   +-- ActivateRule/
|   |   |   +-- ActivateRuleCommand.cs
|   |   |   +-- ActivateRuleHandler.cs
|   |   +-- DeactivateRule/
|   |   |   +-- DeactivateRuleCommand.cs
|   |   |   +-- DeactivateRuleHandler.cs
|   |   +-- ExecuteRule/
|   |       +-- ExecuteRuleCommand.cs
|   |       +-- ExecuteRuleHandler.cs
|   +-- Queries/
|   |   +-- GetRulesByHome/
|   |   |   +-- GetRulesByHomeQuery.cs
|   |   |   +-- GetRulesByHomeHandler.cs
|   |   +-- GetRuleExecutions/
|   |       +-- GetRuleExecutionsQuery.cs
|   |       +-- GetRuleExecutionsHandler.cs
|   +-- Services/
|   |   +-- IAutomationScheduler.cs
|   |   +-- IAutomationEventHandler.cs
|   +-- DTOs/
|   |   +-- AutomationRuleDto.cs
|   |   +-- AutomationExecutionDto.cs
|   +-- Interfaces/
|       +-- IAutomationRuleRepository.cs
|       +-- IAutomationExecutionRepository.cs
+-- Automation.Infrastructure/
|   +-- Automation.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AutomationDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- AutomationRuleConfiguration.cs
|   |   |   +-- AutomationExecutionConfiguration.cs
|   |   +-- Repositories/
|   |       +-- AutomationRuleRepository.cs
|   |       +-- AutomationExecutionRepository.cs
|   +-- Services/
|   |   +-- AutomationSafetyService.cs
|   |   +-- AutomationScheduler.cs
|   |   +-- AutomationEventHandler.cs
|   +-- Migrations/
+-- Automation.Presentation/
|   +-- Automation.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- AutomationRuleController.cs
|       +-- AutomationExecutionController.cs
+-- Automation.Contracts/
    +-- Automation.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- CreateRuleRequest.cs
    |   +-- UpdateRuleRequest.cs
    +-- Responses/
    |   +-- AutomationRuleResponse.cs
    |   +-- AutomationExecutionResponse.cs
    +-- IntegrationEvents/
    |   +-- AutomationRuleExecutedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: SECURITY

```
src/Modules/Security/
+-- Security.Domain/
|   +-- Security.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- FirmwarePackage.cs
|   |   +-- FirmwareUpdateRecord.cs
|   +-- Enums/
|   |   +-- FirmwareUpdateStatus.cs
|   +-- Events/
|   |   +-- FirmwarePackageSigned.cs
|   |   +-- FirmwareUpdateInitiated.cs
|   |   +-- FirmwareUpdateCompleted.cs
|   |   +-- FirmwareSignatureVerified.cs
|   |   +-- FirmwareSignatureFailed.cs
|   +-- Services/
|       +-- IFirmwareSigningService.cs
+-- Security.Application/
|   +-- Security.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- CreateFirmwarePackage/
|   |   |   +-- CreateFirmwarePackageCommand.cs
|   |   |   +-- CreateFirmwarePackageHandler.cs
|   |   +-- SignFirmwarePackage/
|   |   |   +-- SignFirmwarePackageCommand.cs
|   |   |   +-- SignFirmwarePackageHandler.cs
|   |   +-- InitiateFirmwareUpdate/
|   |   |   +-- InitiateFirmwareUpdateCommand.cs
|   |   |   +-- InitiateFirmwareUpdateHandler.cs
|   |   +-- CompleteFirmwareUpdate/
|   |   |   +-- CompleteFirmwareUpdateCommand.cs
|   |   |   +-- CompleteFirmwareUpdateHandler.cs
|   |   +-- VerifyFirmwareSignature/
|   |       +-- VerifyFirmwareSignatureCommand.cs
|   |       +-- VerifyFirmwareSignatureHandler.cs
|   +-- Queries/
|   |   +-- GetFirmwarePackages/
|   |   |   +-- GetFirmwarePackagesQuery.cs
|   |   |   +-- GetFirmwarePackagesHandler.cs
|   |   +-- GetFirmwareUpdateStatus/
|   |       +-- GetFirmwareUpdateStatusQuery.cs
|   |       +-- GetFirmwareUpdateStatusHandler.cs
|   +-- DTOs/
|   |   +-- FirmwarePackageDto.cs
|   |   +-- FirmwareUpdateStatusDto.cs
|   |   +-- FirmwareVerificationDto.cs
|   +-- Interfaces/
|       +-- IFirmwarePackageRepository.cs
|       +-- IFirmwareUpdateRepository.cs
+-- Security.Infrastructure/
|   +-- Security.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- SecurityDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- FirmwarePackageConfiguration.cs
|   |   |   +-- FirmwareUpdateRecordConfiguration.cs
|   |   +-- Repositories/
|   |       +-- FirmwarePackageRepository.cs
|   |       +-- FirmwareUpdateRepository.cs
|   +-- Services/
|   |   +-- FirmwareSigningService.cs
|   |   +-- FirmwareVerificationService.cs
|   +-- Migrations/
+-- Security.Presentation/
|   +-- Security.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- FirmwareController.cs
+-- Security.Contracts/
    +-- Security.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- CreateFirmwarePackageRequest.cs
    |   +-- SignFirmwareRequest.cs
    |   +-- InitiateFirmwareUpdateRequest.cs
    |   +-- VerifyFirmwareRequest.cs
    +-- Responses/
    |   +-- FirmwarePackageResponse.cs
    |   +-- FirmwareUpdateStatusResponse.cs
    |   +-- FirmwareVerificationResponse.cs
    +-- IntegrationEvents/
    |   +-- FirmwarePackageSignedIntegrationEvent.cs
    |   +-- FirmwareUpdateCompletedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: FACEVERIFICATION

```
src/Modules/FaceVerification/
+-- FaceVerification.Domain/
|   +-- FaceVerification.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- FaceProfile.cs
|   |   +-- FaceVerificationAttempt.cs
|   +-- Enums/
|   |   +-- VerificationResult.cs
|   +-- ValueObjects/
|   |   +-- FaceVerificationResult.cs
|   +-- Events/
|   |   +-- FaceProfileCreated.cs
|   |   +-- FaceProfileUpdated.cs
|   |   +-- FaceVerificationPassed.cs
|   |   +-- FaceVerificationFailed.cs
|   |   +-- SpoofDetected.cs
|   |   +-- FaceEmbeddingEncrypted.cs
|   +-- Services/
|       +-- IFaceRecognitionProvider.cs
|       +-- ISpoofDetectionProvider.cs
|       +-- IFaceEmbeddingEncryptionService.cs
+-- FaceVerification.Application/
|   +-- FaceVerification.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- EnrollFace/
|   |   |   +-- EnrollFaceCommand.cs
|   |   |   +-- EnrollFaceHandler.cs
|   |   |   +-- EnrollFaceValidator.cs
|   |   +-- UpdateFaceProfile/
|   |   |   +-- UpdateFaceProfileCommand.cs
|   |   |   +-- UpdateFaceProfileHandler.cs
|   |   +-- DeleteFaceProfile/
|   |   |   +-- DeleteFaceProfileCommand.cs
|   |   |   +-- DeleteFaceProfileHandler.cs
|   |   +-- VerifyFace/
|   |       +-- VerifyFaceCommand.cs
|   |       +-- VerifyFaceHandler.cs
|   +-- Queries/
|   |   +-- GetFaceProfile/
|   |   |   +-- GetFaceProfileQuery.cs
|   |   |   +-- GetFaceProfileHandler.cs
|   |   +-- GetVerificationHistory/
|   |       +-- GetVerificationHistoryQuery.cs
|   |       +-- GetVerificationHistoryHandler.cs
|   +-- Services/
|   |   +-- IFaceVerificationCacheService.cs
|   +-- DTOs/
|   |   +-- FaceProfileDto.cs
|   |   +-- FaceVerificationResultDto.cs
|   |   +-- VerificationHistoryDto.cs
|   +-- Interfaces/
|       +-- IFaceProfileRepository.cs
|       +-- IFaceVerificationAttemptRepository.cs
+-- FaceVerification.Infrastructure/
|   +-- FaceVerification.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- FaceVerificationDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- FaceProfileConfiguration.cs
|   |   |   +-- FaceVerificationAttemptConfiguration.cs
|   |   +-- Repositories/
|   |   |   +-- FaceProfileRepository.cs
|   |   |   +-- FaceVerificationAttemptRepository.cs
|   |   +-- Converters/
|   |       +-- EmbeddingEncryptionConverter.cs
|   +-- Providers/
|   |   +-- MockFaceRecognitionProvider.cs
|   |   +-- RealFaceRecognitionProvider.cs
|   |   +-- MockSpoofDetectionProvider.cs
|   |   +-- RealSpoofDetectionProvider.cs
|   +-- Services/
|   |   +-- FaceEmbeddingEncryptionService.cs
|   |   +-- FaceVerificationCacheService.cs
|   +-- Migrations/
+-- FaceVerification.Presentation/
|   +-- FaceVerification.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- FaceProfileController.cs
|       +-- FaceVerificationController.cs
+-- FaceVerification.Contracts/
    +-- FaceVerification.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- EnrollFaceRequest.cs
    |   +-- VerifyFaceRequest.cs
    +-- Responses/
    |   +-- FaceProfileResponse.cs
    |   +-- FaceVerificationResponse.cs
    |   +-- VerificationHistoryResponse.cs
    +-- IntegrationEvents/
    |   +-- FaceVerificationPassedIntegrationEvent.cs
    |   +-- FaceVerificationFailedIntegrationEvent.cs
    |   +-- SpoofDetectedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: REPORTS

```
src/Modules/Reports/
+-- Reports.Domain/
|   +-- Reports.Domain.csproj
|   +-- GlobalUsings.cs
+-- Reports.Application/
|   +-- Reports.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Interfaces/
|       +-- IReportQuery.cs
+-- Reports.Infrastructure/
|   +-- Reports.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- ReportsDbContext.cs
|   +-- Migrations/
+-- Reports.Presentation/
|   +-- Reports.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
+-- Reports.Contracts/
    +-- Reports.Contracts.csproj
    +-- GlobalUsings.cs
    +-- IntegrationEvents/
    +-- Responses/
    |   +-- ReportResponse.cs
    +-- CHANGELOG.md
```