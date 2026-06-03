# VERIXORA BACKEND – COMPLETE FILE HIERARCHY (PART 4 OF 6)

---

## MODULE: MONITORING

```
src/Modules/Monitoring/
+-- Monitoring.Domain/
|   +-- Monitoring.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- DashboardSnapshot.cs
|   |   +-- Alert.cs
|   |   +-- SuspiciousActivityRule.cs
|   |   +-- SystemMetric.cs
|   +-- Enums/
|   |   +-- AlertSeverity.cs
|   |   +-- AlertType.cs
|   +-- Events/
|   |   +-- AlertCreated.cs
|   |   +-- AlertAcknowledged.cs
|   |   +-- DashboardSnapshotGenerated.cs
|   |   +-- SuspiciousActivityDetected.cs
|   +-- Services/
|       +-- IAlertService.cs
|       +-- ISuspiciousActivityDetector.cs
+-- Monitoring.Application/
|   +-- Monitoring.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- GenerateDashboardSnapshot/
|   |   |   +-- GenerateDashboardSnapshotCommand.cs
|   |   |   +-- GenerateDashboardSnapshotHandler.cs
|   |   +-- CreateAlert/
|   |   |   +-- CreateAlertCommand.cs
|   |   |   +-- CreateAlertHandler.cs
|   |   +-- AcknowledgeAlert/
|   |   |   +-- AcknowledgeAlertCommand.cs
|   |   |   +-- AcknowledgeAlertHandler.cs
|   |   +-- RecordSystemMetric/
|   |   |   +-- RecordSystemMetricCommand.cs
|   |   |   +-- RecordSystemMetricHandler.cs
|   |   +-- ConfigureSuspiciousActivityRule/
|   |       +-- ConfigureSuspiciousActivityRuleCommand.cs
|   |       +-- ConfigureSuspiciousActivityRuleHandler.cs
|   +-- Queries/
|   |   +-- GetDashboard/
|   |   |   +-- GetDashboardQuery.cs
|   |   |   +-- GetDashboardHandler.cs
|   |   +-- GetAlerts/
|   |   |   +-- GetAlertsQuery.cs
|   |   |   +-- GetAlertsHandler.cs
|   |   +-- GetAlertsBySeverity/
|   |   |   +-- GetAlertsBySeverityQuery.cs
|   |   |   +-- GetAlertsBySeverityHandler.cs
|   |   +-- GetSystemMetrics/
|   |   |   +-- GetSystemMetricsQuery.cs
|   |   |   +-- GetSystemMetricsHandler.cs
|   |   +-- GetLiveEvents/
|   |   |   +-- GetLiveEventsQuery.cs
|   |   |   +-- GetLiveEventsHandler.cs
|   |   +-- GetSuspiciousActivityRules/
|   |       +-- GetSuspiciousActivityRulesQuery.cs
|   |       +-- GetSuspiciousActivityRulesHandler.cs
|   +-- DTOs/
|   |   +-- DashboardDto.cs
|   |   +-- AlertDto.cs
|   |   +-- SystemMetricDto.cs
|   |   +-- SuspiciousActivityRuleDto.cs
|   +-- Interfaces/
|       +-- IDashboardRepository.cs
|       +-- IAlertRepository.cs
|       +-- ISystemMetricRepository.cs
|       +-- ISuspiciousActivityRuleRepository.cs
+-- Monitoring.Infrastructure/
|   +-- Monitoring.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- MonitoringDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- DashboardSnapshotConfiguration.cs
|   |   |   +-- AlertConfiguration.cs
|   |   |   +-- SuspiciousActivityRuleConfiguration.cs
|   |   |   +-- SystemMetricConfiguration.cs
|   |   +-- Repositories/
|   |       +-- DashboardRepository.cs
|   |       +-- AlertRepository.cs
|   |       +-- SystemMetricRepository.cs
|   |       +-- SuspiciousActivityRuleRepository.cs
|   +-- Services/
|   |   +-- AlertService.cs
|   |   +-- SuspiciousActivityDetector.cs
|   +-- Migrations/
+-- Monitoring.Presentation/
|   +-- Monitoring.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- DashboardController.cs
|       +-- AlertController.cs
|       +-- MetricsController.cs
|       +-- SuspiciousActivityController.cs
+-- Monitoring.Contracts/
    +-- Monitoring.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Responses/
    |   +-- DashboardResponse.cs
    |   +-- AlertResponse.cs
    |   +-- SystemMetricResponse.cs
    |   +-- SuspiciousActivityRuleResponse.cs
    +-- IntegrationEvents/
    |   +-- AlertCreatedIntegrationEvent.cs
    |   +-- SuspiciousActivityDetectedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: AUDITLOGS

```
src/Modules/AuditLogs/
+-- AuditLogs.Domain/
|   +-- AuditLogs.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- AuditLog.cs
|   +-- Enums/
|   |   +-- AuditResult.cs
|   +-- ValueObjects/
|   |   +-- AuditLogEntry.cs
|   +-- Events/
|   |   +-- AuditLogCreated.cs
|   |   +-- AuditLogArchived.cs
|   +-- Services/
|       +-- IAuditService.cs
+-- AuditLogs.Application/
|   +-- AuditLogs.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- CreateAuditLog/
|   |   |   +-- CreateAuditLogCommand.cs
|   |   |   +-- CreateAuditLogHandler.cs
|   |   +-- BatchCreateAuditLog/
|   |   |   +-- BatchCreateAuditLogCommand.cs
|   |   |   +-- BatchCreateAuditLogHandler.cs
|   |   +-- ArchiveAuditLogs/
|   |       +-- ArchiveAuditLogsCommand.cs
|   |       +-- ArchiveAuditLogsHandler.cs
|   +-- Queries/
|   |   +-- GetAuditLogsByHome/
|   |   |   +-- GetAuditLogsByHomeQuery.cs
|   |   |   +-- GetAuditLogsByHomeHandler.cs
|   |   +-- GetAuditLogsByUser/
|   |   |   +-- GetAuditLogsByUserQuery.cs
|   |   |   +-- GetAuditLogsByUserHandler.cs
|   |   +-- GetAuditLogsByDevice/
|   |   |   +-- GetAuditLogsByDeviceQuery.cs
|   |   |   +-- GetAuditLogsByDeviceHandler.cs
|   |   +-- GetAuditLogsByDateRange/
|   |   |   +-- GetAuditLogsByDateRangeQuery.cs
|   |   |   +-- GetAuditLogsByDateRangeHandler.cs
|   |   +-- GetArchivedAuditLogs/
|   |       +-- GetArchivedAuditLogsQuery.cs
|   |       +-- GetArchivedAuditLogsHandler.cs
|   +-- DTOs/
|   |   +-- AuditLogDto.cs
|   |   +-- AuditLogFilterDto.cs
|   |   +-- ArchivedAuditLogDto.cs
|   +-- Interfaces/
|       +-- IAuditLogRepository.cs
+-- AuditLogs.Infrastructure/
|   +-- AuditLogs.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- AuditLogsDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- AuditLogConfiguration.cs
|   |   +-- Repositories/
|   |   |   +-- AuditLogRepository.cs
|   |   +-- Converters/
|   |       +-- EncryptionConverter.cs
|   +-- Services/
|   |   +-- AuditService.cs
|   |   +-- AuditLogRetentionJob.cs
|   +-- Migrations/
+-- AuditLogs.Presentation/
|   +-- AuditLogs.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- AuditLogController.cs
+-- AuditLogs.Contracts/
    +-- AuditLogs.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- AuditLogFilterRequest.cs
    +-- Responses/
    |   +-- AuditLogResponse.cs
    |   +-- AuditLogBatchResponse.cs
    |   +-- ArchivedAuditLogResponse.cs
    +-- IntegrationEvents/
    |   +-- AuditLogCreatedIntegrationEvent.cs
    |   +-- AuditLogArchivedIntegrationEvent.cs
    +-- CHANGELOG.md
```

---

## MODULE: NOTIFICATIONS

```
src/Modules/Notifications/
+-- Notifications.Domain/
|   +-- Notifications.Domain.csproj
|   +-- GlobalUsings.cs
|   +-- Entities/
|   |   +-- NotificationTemplate.cs
|   |   +-- Notification.cs
|   |   +-- UserPreference.cs
|   +-- Enums/
|   |   +-- NotificationType.cs
|   |   +-- NotificationStatus.cs
|   +-- Events/
|   |   +-- NotificationSent.cs
|   |   +-- NotificationFailed.cs
|   +-- Services/
|       +-- INotificationSender.cs
+-- Notifications.Application/
|   +-- Notifications.Application.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Commands/
|   |   +-- SendNotification/
|   |   |   +-- SendNotificationCommand.cs
|   |   |   +-- SendNotificationHandler.cs
|   |   +-- CreateTemplate/
|   |   |   +-- CreateTemplateCommand.cs
|   |   |   +-- CreateTemplateHandler.cs
|   |   +-- UpdateTemplate/
|   |   |   +-- UpdateTemplateCommand.cs
|   |   |   +-- UpdateTemplateHandler.cs
|   |   +-- UpdatePreferences/
|   |       +-- UpdatePreferencesCommand.cs
|   |       +-- UpdatePreferencesHandler.cs
|   +-- Queries/
|   |   +-- GetNotifications/
|   |   |   +-- GetNotificationsQuery.cs
|   |   |   +-- GetNotificationsHandler.cs
|   |   +-- GetTemplates/
|   |   |   +-- GetTemplatesQuery.cs
|   |   |   +-- GetTemplatesHandler.cs
|   |   +-- GetPreferences/
|   |       +-- GetPreferencesQuery.cs
|   |       +-- GetPreferencesHandler.cs
|   +-- DTOs/
|   |   +-- NotificationDto.cs
|   |   +-- TemplateDto.cs
|   |   +-- PreferenceDto.cs
|   +-- Interfaces/
|       +-- INotificationRepository.cs
|       +-- ITemplateRepository.cs
|       +-- IPreferenceRepository.cs
+-- Notifications.Infrastructure/
|   +-- Notifications.Infrastructure.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Persistence/
|   |   +-- NotificationsDbContext.cs
|   |   +-- Configurations/
|   |   |   +-- NotificationTemplateConfiguration.cs
|   |   |   +-- NotificationConfiguration.cs
|   |   |   +-- UserPreferenceConfiguration.cs
|   |   +-- Repositories/
|   |       +-- NotificationRepository.cs
|   |       +-- TemplateRepository.cs
|   |       +-- PreferenceRepository.cs
|   +-- Services/
|   |   +-- EmailSender.cs
|   |   +-- PushNotificationSender.cs
|   +-- Migrations/
+-- Notifications.Presentation/
|   +-- Notifications.Presentation.csproj
|   +-- GlobalUsings.cs
|   +-- DependencyInjection.cs
|   +-- Controllers/
|       +-- NotificationController.cs
|       +-- TemplateController.cs
|       +-- PreferenceController.cs
+-- Notifications.Contracts/
    +-- Notifications.Contracts.csproj
    +-- GlobalUsings.cs
    +-- Requests/
    |   +-- SendNotificationRequest.cs
    |   +-- CreateTemplateRequest.cs
    |   +-- UpdateTemplateRequest.cs
    |   +-- UpdatePreferencesRequest.cs
    +-- Responses/
    |   +-- NotificationResponse.cs
    |   +-- TemplateResponse.cs
    |   +-- PreferenceResponse.cs
    +-- IntegrationEvents/
    |   +-- NotificationSentIntegrationEvent.cs
    +-- CHANGELOG.md
```