# VERIXORA ITERATION 6 – FACE VERIFICATION & PRODUCTION HARDENING

---

## ITERATION OBJECTIVE

Implement the pluggable face verification system, complete performance tuning, activate automated backups, and prepare the system for production release.

By the end of Iteration 6:

- Pluggable face verification provider (mock + real AI integration point).
- Face verification runs last in the unlock pipeline (step 10).
- Face embeddings stored encrypted at column level.
- Face verification results cached per session for a short time.
- Spoof detection capability.
- Automated daily encrypted database backups with 30-day retention.
- Final performance SLA validation (200ms p95).
- Production deployment of backend, mobile app store submission, web portal go-live.
- Full validation script passes all checks.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                          |
| ----------------- | ---------------------------------------------- |
| Backend Developer | FaceVerification module, backup automation, final tuning |
| Mobile Developer  | Face capture screen, camera integration, app store prep |
| Web Developer     | Settings page, final polish                    |
| DevOps            | Production infrastructure, backup automation, monitoring |
| QA                | Full system E2E testing, performance testing   |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**FaceVerification.Domain:**
- `FaceProfile` aggregate root:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `Embedding` (byte[], encrypted)
  - `EmbeddingVersion` (string)
  - `ImageCount` (int)
  - `CreatedAt` (DateTimeOffset)
  - `UpdatedAt` (DateTimeOffset)
  - `IsActive` (bool)
- `FaceVerificationResult` value object:
  - `Passed` (bool)
  - `ConfidenceScore` (double)
  - `MatchedProfileId` (Guid?)
  - `IsSpoofDetected` (bool)
  - `ProcessingTimeMs` (int)
  - `VerifiedAt` (DateTimeOffset)
- `FaceVerificationAttempt` entity:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `SmartLockId` (Guid)
  - `Result` (enum: Passed, Failed, SpoofDetected, Error)
  - `ConfidenceScore` (double)
  - `AttemptedAt` (DateTimeOffset)

**Domain Events:**
- `FaceProfileCreated`
- `FaceProfileUpdated`
- `FaceVerificationPassed`
- `FaceVerificationFailed`
- `SpoofDetected`
- `FaceEmbeddingEncrypted`

**Domain Services:**
- `IFaceRecognitionProvider` – pluggable interface for face matching.
- `ISpoofDetectionProvider` – pluggable interface for liveness/spoof detection.
- `IFaceEmbeddingEncryptionService` – encrypts/decrypts embeddings.

### 1.2 Backend – Application Layer

**FaceVerification.Application – Commands:**
- `EnrollFaceCommand` → `EnrollFaceHandler` + `EnrollFaceValidator`
- `UpdateFaceProfileCommand` → `UpdateFaceProfileHandler`
- `DeleteFaceProfileCommand` → `DeleteFaceProfileHandler`
- `VerifyFaceCommand` → `VerifyFaceHandler`

**FaceVerification.Application – Queries:**
- `GetFaceProfileQuery` → `GetFaceProfileHandler`
- `GetVerificationHistoryQuery` → `GetVerificationHistoryHandler`

**FaceVerification.Application – Services:**
- `IFaceVerificationCacheService` – caches verification results per session (short TTL).

### 1.3 Backend – Infrastructure Layer

**FaceVerification.Infrastructure:**
- `FaceVerificationDbContext` with `DbSet<FaceProfile>`, `DbSet<FaceVerificationAttempt>`.
- Entity configurations with encrypted embedding column.
- Repositories: `FaceProfileRepository`, `FaceVerificationAttemptRepository`.
- `MockFaceRecognitionProvider` – for development/testing.
- `RealFaceRecognitionProvider` – integration point for production AI service.
- `MockSpoofDetectionProvider` – for development/testing.
- `RealSpoofDetectionProvider` – integration point for liveness detection.
- `FaceEmbeddingEncryptionService` – AES-256 encryption for embeddings.
- `FaceVerificationCacheService` – in-memory cache per session.

### 1.4 Backend – Presentation Layer

**FaceVerification.Presentation – Controllers:**
- `FaceProfileController`:
  - `POST /api/v1/face-profiles` – enroll face.
  - `GET /api/v1/face-profiles` – get user's face profiles.
  - `DELETE /api/v1/face-profiles/{id}` – delete face profile.
- `FaceVerificationController`:
  - `POST /api/v1/face-verification/verify` – verify face against profile.
  - `GET /api/v1/face-verification/history` – get verification history.

### 1.5 Backend – Contracts

**FaceVerification.Contracts:**
- `Requests/`: `EnrollFaceRequest`, `VerifyFaceRequest`.
- `Responses/`: `FaceProfileResponse`, `FaceVerificationResponse`, `VerificationHistoryResponse`.

### 1.6 Production Hardening

**Backup Automation:**
- Scheduled job (Quartz.NET or similar) for daily database backups.
- Backups encrypted with AES-256.
- Stored off-site (Azure Blob / AWS S3) with 30-day retention.
- Backup success/failure alerts.

**Performance Tuning:**
- Database query optimization (indexes, query plans).
- Unlock pipeline profiling and optimization.
- Caching review (authorization, face verification).
- Connection pool tuning.

**Security Hardening:**
- Final review of all encryption implementations.
- JWT signing key rotation policy.
- MQTT broker security review.
- API penetration testing preparation.

**Production Infrastructure:**
- Production PostgreSQL instance (managed service).
- Production MQTT broker (EMQX or VerneMQ cluster).
- Load balancer configuration.
- SSL/TLS certificates.
- DNS configuration.
- Monitoring and alerting (Prometheus + Grafana + AlertManager).

### 1.7 Mobile App

- **Face Capture Screen:** camera preview, capture button, submit for enrollment.
- **Face Verification Screen:** camera preview during unlock (if required).
- **App Store Preparation:** icons, screenshots, privacy policy, App Store / Google Play metadata.

### 1.8 Web Portal

- **Face Verification Settings:** enable/disable per lock.
- **Face Profile Management:** view enrolled profiles.
- **Verification History:** view attempts and results.
- **Backup Status:** view backup job status and history.

---

## PHASE 2: IMPLEMENTATION

### 2.1 FaceVerification.Domain (New Files)
```
FaceVerification.Domain/
+-- Entities/
|   +-- FaceProfile.cs
|   +-- FaceVerificationAttempt.cs
+-- Enums/
|   +-- VerificationResult.cs
+-- ValueObjects/
|   +-- FaceVerificationResult.cs
+-- Events/
|   +-- FaceProfileCreated.cs
|   +-- FaceProfileUpdated.cs
|   +-- FaceVerificationPassed.cs
|   +-- FaceVerificationFailed.cs
|   +-- SpoofDetected.cs
|   +-- FaceEmbeddingEncrypted.cs
+-- Services/
    +-- IFaceRecognitionProvider.cs
    +-- ISpoofDetectionProvider.cs
    +-- IFaceEmbeddingEncryptionService.cs
```

### 2.2 FaceVerification.Application (New Files)
```
FaceVerification.Application/
+-- Commands/
|   +-- EnrollFace/
|   |   +-- EnrollFaceCommand.cs
|   |   +-- EnrollFaceHandler.cs
|   |   +-- EnrollFaceValidator.cs
|   +-- UpdateFaceProfile/
|   |   +-- UpdateFaceProfileCommand.cs
|   |   +-- UpdateFaceProfileHandler.cs
|   +-- DeleteFaceProfile/
|   |   +-- DeleteFaceProfileCommand.cs
|   |   +-- DeleteFaceProfileHandler.cs
|   +-- VerifyFace/
|       +-- VerifyFaceCommand.cs
|       +-- VerifyFaceHandler.cs
+-- Queries/
|   +-- GetFaceProfile/
|   |   +-- GetFaceProfileQuery.cs
|   |   +-- GetFaceProfileHandler.cs
|   +-- GetVerificationHistory/
|       +-- GetVerificationHistoryQuery.cs
|       +-- GetVerificationHistoryHandler.cs
+-- Services/
|   +-- IFaceVerificationCacheService.cs
+-- DTOs/
|   +-- FaceProfileDto.cs
|   +-- FaceVerificationResultDto.cs
|   +-- VerificationHistoryDto.cs
+-- Interfaces/
    +-- IFaceProfileRepository.cs
    +-- IFaceVerificationAttemptRepository.cs
```

### 2.3 FaceVerification.Infrastructure (New Files)
```
FaceVerification.Infrastructure/
+-- Persistence/
|   +-- FaceVerificationDbContext.cs
|   +-- Configurations/
|   |   +-- FaceProfileConfiguration.cs
|   |   +-- FaceVerificationAttemptConfiguration.cs
|   +-- Repositories/
|   |   +-- FaceProfileRepository.cs
|   |   +-- FaceVerificationAttemptRepository.cs
|   +-- Converters/
|       +-- EmbeddingEncryptionConverter.cs
+-- Providers/
|   +-- MockFaceRecognitionProvider.cs
|   +-- RealFaceRecognitionProvider.cs
|   +-- MockSpoofDetectionProvider.cs
|   +-- RealSpoofDetectionProvider.cs
+-- Services/
|   +-- FaceEmbeddingEncryptionService.cs
|   +-- FaceVerificationCacheService.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 FaceVerification.Presentation (New Files)
```
FaceVerification.Presentation/
+-- Controllers/
    +-- FaceProfileController.cs
    +-- FaceVerificationController.cs
```

### 2.5 FaceVerification.Contracts (New Files)
```
FaceVerification.Contracts/
+-- Requests/
|   +-- EnrollFaceRequest.cs
|   +-- VerifyFaceRequest.cs
+-- Responses/
    +-- FaceProfileResponse.cs
    +-- FaceVerificationResponse.cs
    +-- VerificationHistoryResponse.cs
```

### 2.6 BuildingBlocks Updates

**Backup Infrastructure:**
```
BuildingBlocks.Infrastructure/
+-- Backup/
    +-- BackupService.cs
    +-- BackupConfiguration.cs
    +-- BackupJob.cs
```

### 2.7 ApiHost Updates
```
ApiHost/
+-- Program.cs (updated)
|   +-- Register backup job scheduler
|   +-- Configure CORS for production domains
|   +-- Configure production middleware
+-- appsettings.Production.json
```

---

## PHASE 3: INTEGRATION

- Wire FaceVerification module DI into ApiHost.
- Run EF Core migrations for FaceVerification schema.
- Register face recognition provider based on environment (mock for dev, real for prod).
- Face verification step (step 10) integrated into unlock pipeline.
- Face verification cache wired per session.
- Backup job scheduled and tested.
- Production infrastructure provisioned.
- SSL certificates installed.
- DNS configured.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `FaceProfile_Enroll_ShouldEncryptEmbedding`
- `FaceVerificationResult_Passed_ShouldSetConfidence`
- `FaceVerificationResult_SpoofDetected_ShouldFail`
- `FaceEmbedding_ShouldBeEncrypted`

### 4.2 Application Tests
- `EnrollFace_ValidImage_ShouldCreateProfile`
- `EnrollFace_InvalidImage_ShouldFail`
- `VerifyFace_Match_ShouldPass`
- `VerifyFace_NoMatch_ShouldFail`
- `VerifyFace_SpoofDetected_ShouldReject`
- `VerifyFace_CachedResult_ShouldSkipProvider`
- `FaceVerification_RunsLastInPipeline`

### 4.3 Integration Tests
- `FaceProfileController_Enroll_ShouldReturn201`
- `FaceVerificationController_Verify_ShouldReturnResult`
- `FaceEmbedding_EncryptedInDatabase`
- `BackupJob_ShouldCreateEncryptedBackup`
- `BackupJob_ShouldUploadToOffSite`

### 4.4 Performance Tests
- `UnlockPipeline_WithFaceVerification_ShouldMeetSLA`
- `FaceVerification_UnderLoad_ShouldScale`

### 4.5 E2E Tests
- Enroll face → request unlock → face verified → door unlocks.
- Spoof attempt → face verification fails → unlock denied.

---

## PHASE 5: DEPLOYMENT

- Provision production infrastructure.
- Run all database migrations against production.
- Deploy backend to production.
- Deploy MQTT broker to production.
- Configure DNS and SSL.
- Enable automated backups.
- Deploy Prometheus, Grafana, AlertManager.
- Submit mobile app to App Store and Google Play.
- Deploy web portal to production CDN.
- Run final validation script against production.
- Smoke test all critical flows.

---

## ACCEPTANCE CRITERIA

- [ ] Face profiles can be enrolled with image upload.
- [ ] Face verification works with mock provider.
- [ ] Real AI provider integration point ready.
- [ ] Spoof detection rejects fake attempts.
- [ ] Face embeddings encrypted at column level.
- [ ] Face verification cached per session.
- [ ] Face verification runs last in unlock pipeline (step 10).
- [ ] Automated daily backups running.
- [ ] Backups encrypted and stored off-site.
- [ ] 30-day backup retention configured.
- [ ] Final unlock pipeline meets 200ms p95 SLA.
- [ ] Production environment fully operational.
- [ ] Mobile app submitted to app stores.
- [ ] Web portal deployed to production.
- [ ] All tests pass including performance SLA.
- [ ] Validation script passes all checks.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 6 Coverage                    |
| ---------------------------------- | --------------------------------------- |
| Face verification (NICE)           | Pluggable provider, mock + real         |
| Face verification in pipeline      | Step 10, last in order                  |
| Embedding encryption (ADR-017)     | AES-256 column-level encryption         |
| Face cache per session             | FaceVerificationCacheService            |
| Spoof detection                    | ISpoofDetectionProvider                 |
| Backup strategy (SHOULD)           | Automated daily encrypted backups       |
| Backup retention (30 days)         | Off-site storage with retention policy  |
| Performance SLA (200ms p95)        | Final validation and tuning             |
| Production deployment              | Full production infrastructure          |
| App store submission               | iOS and Android                         |

---

## NEW FILE INVENTORY

| Module | Domain | Application | Infrastructure | Presentation | Contracts |
|--------|--------|-------------|----------------|--------------|------------|
| FaceVerification | 12 | 20 | 14 | 2 | 5 |
| BuildingBlocks (backup) | 0 | 0 | 3 | 0 | 0 |
| ApiHost (updates) | 0 | 0 | 0 | 2 | 0 |
| **Total New** | **12** | **20** | **17** | **4** | **5** |

**Grand Total New Files: 58**

---

## ALL ITERATIONS SUMMARY

| Iteration | Focus | New Files |
|-----------|-------|-----------|
| Iteration 0 | Foundation & Setup | 71 projects |
| Iteration 1 | Identity & Home | 87 |
| Iteration 2 | Devices & Provisioning | 80 |
| Iteration 3 | Smart Locks & Pipeline | 74 |
| Iteration 4 | Authorization & Audit | 154 |
| Iteration 5 | Monitoring & Automation | 140 |
| Iteration 6 | Face Verify & Production | 58 |
| **Total** | | **593 files + 71 projects** |

---

**ITERATION 6 DOCUMENT COMPLETE**

**ALL 7 ITERATION DOCUMENTS NOW COMPLETE**