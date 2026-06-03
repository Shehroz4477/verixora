# VERIXORA ITERATION 6 – FACE VERIFICATION & PRODUCTION HARDENING (FINAL VERSION)

---

## ITERATION OBJECTIVE

Implement the pluggable face verification system, complete performance tuning, activate automated backups, configure production environments with secrets management, containerization, alerting, SLA dashboard, load testing, and prepare for production release.

By the end of Iteration 6:

- Pluggable face verification provider (mock + real AI integration point).
- Face verification runs last in the unlock pipeline (step 10).
- Face embeddings stored encrypted at column level.
- Face verification results cached per session for a short time.
- Spoof detection capability.
- Integration event contracts defined for FaceVerification module.
- Automated daily encrypted database backups with 30-day retention.
- Production environment configured with secrets management.
- Containerization with Docker and Kubernetes manifests.
- AlertManager rules active and tested.
- Grafana SLA dashboard deployed.
- Load testing validates 200ms p95 SLA.
- Final performance SLA validation.
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
| DevOps            | Production infrastructure, secrets, Docker, K8s, alerting, Grafana |
| QA                | Full system E2E testing, load testing          |

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

### 1.5 Backend – Contracts (including Integration Events)

**FaceVerification.Contracts:**
- `Requests/`: `EnrollFaceRequest`, `VerifyFaceRequest`.
- `Responses/`: `FaceProfileResponse`, `FaceVerificationResponse`, `VerificationHistoryResponse`.
- `IntegrationEvents/`:
  - `FaceVerificationPassedIntegrationEvent`
  - `FaceVerificationFailedIntegrationEvent`
  - `SpoofDetectedIntegrationEvent`

### 1.6 Production Hardening

**Backup Automation:**
- Scheduled job (Quartz.NET) for daily database backups.
- Backups encrypted with AES-256.
- Stored off-site (Azure Blob / AWS S3) with 30-day retention.
- Backup success/failure alerts via AlertManager.

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
- Secrets verified in production environment.

**Production Infrastructure:**
- Production PostgreSQL instance (managed service).
- Production MQTT broker (EMQX or VerneMQ cluster).
- Load balancer configuration.
- SSL/TLS certificates.
- DNS configuration.
- Secrets manager (Azure Key Vault / AWS Secrets Manager).
- Docker image built and pushed to container registry.
- Kubernetes manifests for production deployment.

**Load Testing:**
- k6 or NBomber test scripts for concurrent unlock requests.
- Validates 200ms p95 SLA under load.
- Tests rate limiting behavior under load.

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
+-- (existing files from Iteration 0)
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
+-- (existing files from Iteration 0)
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
+-- (existing files from Iteration 0)
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
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- FaceProfileController.cs
    +-- FaceVerificationController.cs
```

### 2.5 FaceVerification.Contracts (New Files)
```
FaceVerification.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- EnrollFaceRequest.cs
|   +-- VerifyFaceRequest.cs
+-- Responses/
|   +-- FaceProfileResponse.cs
|   +-- FaceVerificationResponse.cs
|   +-- VerificationHistoryResponse.cs
+-- IntegrationEvents/
    +-- FaceVerificationPassedIntegrationEvent.cs
    +-- FaceVerificationFailedIntegrationEvent.cs
    +-- SpoofDetectedIntegrationEvent.cs
```

### 2.6 BuildingBlocks Updates

**Backup Infrastructure:**
```
BuildingBlocks.Infrastructure/
+-- (existing files from Iteration 0)
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
+-- appsettings.Production.json (updated, no secrets)
```

### 2.8 Infrastructure (New Files)
```
infrastructure/
+-- kubernetes/
|   +-- namespace.yaml
|   +-- deployment.yaml
|   +-- service.yaml
|   +-- ingress.yaml
|   +-- configmap.yaml
|   +-- secrets-provider.yaml
+-- docker/
|   +-- Dockerfile (updated for production)
|   +-- .dockerignore
+-- load-tests/
    +-- k6/
        +-- unlock-pipeline-load.js
        +-- rate-limiting-load.js
```

### 2.9 Incident Response Runbook (New Document)
```
docs/
+-- incident-response-runbook.md
```

Contents:
- Force-lock all doors procedure.
- Revoke all sessions procedure.
- Rotate signing keys procedure.
- Isolate compromised device procedure.
- Notify affected users procedure.
- Escalation contacts.

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
- Secrets manager configured and verified.
- Docker image built and pushed to container registry.
- Kubernetes manifests applied to production cluster.
- AlertManager connected to production.
- Grafana dashboards imported to production.

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
- `Production_Secrets_ShouldNotBeInConfigFiles`
- `Kubernetes_Deployment_ShouldBeHealthy`

### 4.4 Contract Tests
- `FaceVerification_Contracts_ShouldMatchPublishedEvents`

### 4.5 Load Tests
- `UnlockPipeline_Concurrent100_ShouldMeetSLA`
- `UnlockPipeline_Concurrent500_ShouldMeetSLA`
- `UnlockPipeline_SustainedLoad_ShouldNotDegrade`
- `RateLimiting_UnderLoad_ShouldProtectSystem`
- `FaceVerification_UnderLoad_ShouldScale`

### 4.6 E2E Tests
- Enroll face → request unlock → face verified → door unlocks.
- Spoof attempt → face verification fails → unlock denied.
- Backup job runs → backup stored off-site → alert on success.

---

## PHASE 5: DEPLOYMENT

- Provision production infrastructure.
- Run all database migrations against production.
- Configure secrets manager with production secrets.
- Deploy backend to production (Docker/Kubernetes).
- Deploy MQTT broker to production.
- Configure DNS and SSL.
- Enable automated backups.
- Deploy Prometheus, Grafana, AlertManager to production.
- Import SLA dashboard.
- Run load tests against production.
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
- [ ] Integration event contracts defined.
- [ ] Contract tests pass.
- [ ] Automated daily backups running.
- [ ] Backups encrypted and stored off-site.
- [ ] 30-day backup retention configured.
- [ ] Production environment fully operational.
- [ ] Secrets managed externally, not in files.
- [ ] Docker image in container registry.
- [ ] Kubernetes deployment healthy.
- [ ] AlertManager rules active.
- [ ] Grafana SLA dashboard deployed.
- [ ] Load tests pass (200ms p95 SLA).
- [ ] Incident response runbook documented.
- [ ] Mobile app submitted to app stores.
- [ ] Web portal deployed to production.
- [ ] All tests pass including load tests.
- [ ] Validation script passes all 23 checks.

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
| Performance SLA (200ms p95)        | Load testing validation                 |
| Production deployment              | Full production infrastructure          |
| Secrets management                 | Azure Key Vault / AWS Secrets Manager   |
| Containerization                   | Docker + Kubernetes manifests           |
| Alerting rules                     | AlertManager deployed                   |
| SLA dashboard                      | Grafana dashboards deployed             |
| Load testing                       | k6 scripts, SLA validation              |
| Incident response                  | runbook documented                      |
| App store submission               | iOS and Android                         |
| Integration event contracts        | FaceVerification.Contracts              |

---

**ITERATION 6 COMPLETE.**

All improvements integrated:
- Integration event contracts (FaceVerification)
- Load testing strategy
- Environment configuration (production)
- Secrets management (production)
- Containerization (Kubernetes manifests)
- Alerting rules (deployed)
- SLA dashboard (deployed)
- Incident response runbook

---

## ALL ITERATIONS SUMMARY

| Iteration | Focus | Key Improvements |
|-----------|-------|-----------------|
| 0 | Foundation & Setup | 10 improvements |
| 1 | Identity & Home | API keys, Sessions merged, Integration events |
| 2 | Devices & Provisioning | Decommissioning, Simulator, Integration events |
| 3 | Smart Locks & Pipeline | API key bypass, No cache, No offline, Integration events |
| 4 | Authorization & Audit | Retention policy, API key audit, Integration events |
| 5 | Monitoring & Automation | Suspicious activity, Alerting rules, SLA dashboard, Integration events |
| 6 | Face Verify & Production | Load tests, Secrets, K8s, Incident runbook, Integration events |
