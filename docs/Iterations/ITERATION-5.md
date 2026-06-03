# VERIXORA ITERATION 5 – MONITORING, NOTIFICATIONS & AUTOMATION (FINAL VERSION)

---

## ITERATION OBJECTIVE

Implement the monitoring dashboard, alert system, email notifications, suspicious activity detection, basic IF-THEN automation engine with safety limits, integration event contracts, alerting rules, and SLA dashboard.

By the end of Iteration 5:

- Monitoring dashboard shows system status, device health, recent events.
- Alerts generated for critical events (device offline, access denied, low battery).
- Suspicious activity detection analyzes audit patterns and raises alerts.
- Email notifications sent for important security events.
- Automation engine supports time-based, event-based, and condition-based triggers.
- Automation safety rules enforced (no infinite loops, execution depth limits).
- All automation actions are audited.
- Integration event contracts defined for Monitoring, Notifications, and Automation modules.
- AlertManager rules defined for critical thresholds.
- Pre-built Grafana SLA dashboard template.
- Mobile app receives push notifications via SignalR.
- Web Portal shows real-time monitoring dashboard.

---

## DURATION

2–3 weeks

---

## TEAM ALLOCATION

| Role              | Focus                                          |
| ----------------- | ---------------------------------------------- |
| Backend Developer | Monitoring module, Notifications module, Automation module |
| Mobile Developer  | Notification list, push notification handling  |
| Web Developer     | Real-time dashboard, charts, alert management  |
| DevOps            | Prometheus, Grafana, AlertManager setup        |

---

## PHASE 1: DESIGN

### 1.1 Backend – Domain Models

**Monitoring.Domain:**
- `DashboardSnapshot` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `TotalDevices` (int)
  - `OnlineDevices` (int)
  - `OfflineDevices` (int)
  - `RecentUnlocks` (int)
  - `RecentDenials` (int)
  - `ActiveAlerts` (int)
  - `GeneratedAt` (DateTimeOffset)
- `Alert` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `Severity` (enum: Info, Warning, Critical)
  - `Type` (enum: DeviceOffline, AccessDenied, LowBattery, FirmwareUpdate, SystemHealth, SuspiciousActivity)
  - `Title` (string)
  - `Message` (string)
  - `Source` (string)
  - `IsAcknowledged` (bool)
  - `AcknowledgedBy` (Guid?)
  - `AcknowledgedAt` (DateTimeOffset?)
  - `CreatedAt` (DateTimeOffset)
- `SuspiciousActivityRule` entity:
  - `Id` (Guid)
  - `Name` (string)
  - `Description` (string)
  - `Pattern` (string, JSON configuration)
  - `Threshold` (int)
  - `TimeWindowMinutes` (int)
  - `Severity` (enum: Warning, Critical)
  - `IsActive` (bool)
- `SystemMetric` entity:
  - `Id` (Guid)
  - `Name` (string)
  - `Value` (double)
  - `Unit` (string)
  - `RecordedAt` (DateTimeOffset)

**Notifications.Domain:**
- `NotificationTemplate` aggregate root:
  - `Id` (Guid)
  - `Name` (string)
  - `Type` (enum: Email, Push, Sms)
  - `Subject` (string)
  - `BodyTemplate` (string)
  - `IsActive` (bool)
- `Notification` entity:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `UserId` (Guid)
  - `TemplateId` (Guid?)
  - `Type` (enum: Email, Push, Sms)
  - `Subject` (string)
  - `Body` (string)
  - `Status` (enum: Pending, Sent, Failed, Read)
  - `CreatedAt` (DateTimeOffset)
  - `SentAt` (DateTimeOffset?)
- `UserPreference` entity:
  - `Id` (Guid)
  - `UserId` (Guid)
  - `EmailEnabled` (bool)
  - `PushEnabled` (bool)
  - `SmsEnabled` (bool)
  - `AlertSeverityMinimum` (enum: Info, Warning, Critical)

**Automation.Domain:**
- `AutomationRule` aggregate root:
  - `Id` (Guid)
  - `HomeId` (Guid)
  - `Name` (string)
  - `Description` (string)
  - `IsActive` (bool)
  - `Trigger` (AutomationTrigger value object)
  - `Conditions` (List of conditions)
  - `Actions` (List of actions)
  - `Priority` (int)
  - `ExecutionCount` (int)
  - `LastExecutedAt` (DateTimeOffset?)
  - `CreatedAt` (DateTimeOffset)
- `AutomationTrigger` value object:
  - `Type` (enum: TimeBased, EventBased, ConditionBased)
  - `CronExpression` (string?, for time-based)
  - `EventName` (string?, for event-based)
  - `ConditionExpression` (string?, for condition-based)
- `AutomationCondition` value object:
  - `Field` (string)
  - `Operator` (enum: Equals, NotEquals, GreaterThan, LessThan, Contains)
  - `Value` (string)
- `AutomationAction` value object:
  - `Type` (enum: LockDoor, UnlockDoor, SendNotification, TriggerAlarm)
  - `Parameters` (string, JSON)
- `AutomationExecution` entity:
  - `Id` (Guid)
  - `RuleId` (Guid)
  - `TriggeredBy` (string)
  - `Result` (enum: Success, Failed, Blocked)
  - `ExecutedActions` (string, JSON)
  - `Depth` (int)
  - `ExecutionTimeMs` (int)
  - `ExecutedAt` (DateTimeOffset)

**Domain Events:**
- `AlertCreated`
- `AlertAcknowledged`
- `DashboardSnapshotGenerated`
- `SuspiciousActivityDetected`
- `NotificationSent`
- `NotificationFailed`
- `AutomationRuleTriggered`
- `AutomationRuleExecuted`
- `AutomationRuleBlocked`
- `AutomationLoopDetected`

**Domain Services:**
- `IAutomationSafetyService` – checks depth limits, loop detection.
- `INotificationSender` – sends email/push notifications.
- `IAlertService` – generates alerts from domain events.
- `ISuspiciousActivityDetector` – analyzes audit patterns.

### 1.2 Backend – Application Layer

**Monitoring.Application – Commands:**
- `GenerateDashboardSnapshotCommand` → `GenerateDashboardSnapshotHandler`
- `CreateAlertCommand` → `CreateAlertHandler`
- `AcknowledgeAlertCommand` → `AcknowledgeAlertHandler`
- `RecordSystemMetricCommand` → `RecordSystemMetricHandler`
- `ConfigureSuspiciousActivityRuleCommand` → `ConfigureSuspiciousActivityRuleHandler`

**Monitoring.Application – Queries:**
- `GetDashboardQuery` → `GetDashboardHandler`
- `GetAlertsQuery` → `GetAlertsHandler`
- `GetAlertsBySeverityQuery` → `GetAlertsBySeverityHandler`
- `GetSystemMetricsQuery` → `GetSystemMetricsHandler`
- `GetLiveEventsQuery` → `GetLiveEventsHandler`
- `GetSuspiciousActivityRulesQuery` → `GetSuspiciousActivityRulesHandler`

**Notifications.Application – Commands:**
- `SendNotificationCommand` → `SendNotificationHandler`
- `CreateTemplateCommand` → `CreateTemplateHandler`
- `UpdateTemplateCommand` → `UpdateTemplateHandler`
- `UpdatePreferencesCommand` → `UpdatePreferencesHandler`

**Notifications.Application – Queries:**
- `GetNotificationsQuery` → `GetNotificationsHandler`
- `GetTemplatesQuery` → `GetTemplatesHandler`
- `GetPreferencesQuery` → `GetPreferencesHandler`

**Automation.Application – Commands:**
- `CreateRuleCommand` → `CreateRuleHandler` + `CreateRuleValidator`
- `UpdateRuleCommand` → `UpdateRuleHandler`
- `DeleteRuleCommand` → `DeleteRuleHandler`
- `ActivateRuleCommand` → `ActivateRuleHandler`
- `DeactivateRuleCommand` → `DeactivateRuleHandler`
- `ExecuteRuleCommand` → `ExecuteRuleHandler`

**Automation.Application – Queries:**
- `GetRulesByHomeQuery` → `GetRulesByHomeHandler`
- `GetRuleExecutionsQuery` → `GetRuleExecutionsHandler`

**Automation.Application – Services:**
- `IAutomationScheduler` – schedules time-based rules.
- `IAutomationEventHandler` – handles event-based triggers.

### 1.3 Backend – Infrastructure Layer

**Monitoring.Infrastructure:**
- `MonitoringDbContext` with `DbSet<DashboardSnapshot>`, `DbSet<Alert>`, `DbSet<SuspiciousActivityRule>`, `DbSet<SystemMetric>`.
- Entity configurations.
- Repositories: `DashboardRepository`, `AlertRepository`, `SystemMetricRepository`, `SuspiciousActivityRuleRepository`.
- `AlertService` – subscribes to domain events, creates alerts.
- `SuspiciousActivityDetector` – analyzes audit logs, raises alerts.

**Notifications.Infrastructure:**
- `NotificationsDbContext` with `DbSet<NotificationTemplate>`, `DbSet<Notification>`, `DbSet<UserPreference>`.
- Entity configurations.
- Repositories: `NotificationRepository`, `TemplateRepository`, `PreferenceRepository`.
- `EmailSender` – SMTP email service.
- `PushNotificationSender` – via SignalR.

**Automation.Infrastructure:**
- `AutomationDbContext` with `DbSet<AutomationRule>`, `DbSet<AutomationExecution>`.
- Entity configurations.
- Repositories: `AutomationRuleRepository`, `AutomationExecutionRepository`.
- `AutomationSafetyService` – enforces max depth (10), loop detection via execution history.
- `AutomationScheduler` – Quartz.NET for cron-based rules.
- `AutomationEventHandler` – subscribes to integration events.

### 1.4 Backend – Presentation Layer

**Monitoring.Presentation – Controllers:**
- `DashboardController`:
  - `GET /api/v1/dashboard` – get current dashboard.
  - `GET /api/v1/dashboard/history` – get dashboard history.
- `AlertController`:
  - `GET /api/v1/alerts` – list alerts.
  - `GET /api/v1/alerts/{id}` – get alert details.
  - `PUT /api/v1/alerts/{id}/acknowledge` – acknowledge alert.
- `MetricsController`:
  - `GET /api/v1/metrics` – get system metrics.
- `SuspiciousActivityController`:
  - `GET /api/v1/suspicious-activity/rules` – list detection rules.
  - `PUT /api/v1/suspicious-activity/rules/{id}` – update rule.

**Notifications.Presentation – Controllers:**
- `NotificationController`:
  - `GET /api/v1/notifications` – list user notifications.
  - `PUT /api/v1/notifications/{id}/read` – mark as read.
- `TemplateController`:
  - `POST /api/v1/notification-templates`
  - `GET /api/v1/notification-templates`
  - `PUT /api/v1/notification-templates/{id}`
- `PreferenceController`:
  - `GET /api/v1/notification-preferences`
  - `PUT /api/v1/notification-preferences`

**Automation.Presentation – Controllers:**
- `AutomationRuleController`:
  - `POST /api/v1/automation-rules`
  - `GET /api/v1/automation-rules`
  - `GET /api/v1/automation-rules/{id}`
  - `PUT /api/v1/automation-rules/{id}`
  - `DELETE /api/v1/automation-rules/{id}`
  - `POST /api/v1/automation-rules/{id}/activate`
  - `POST /api/v1/automation-rules/{id}/deactivate`
- `AutomationExecutionController`:
  - `GET /api/v1/automation-executions`
  - `GET /api/v1/automation-executions/{id}`

### 1.5 Backend – Contracts (including Integration Events)

**Monitoring.Contracts:**
- `Responses/`: `DashboardResponse`, `AlertResponse`, `SystemMetricResponse`, `SuspiciousActivityRuleResponse`.
- `IntegrationEvents/`:
  - `AlertCreatedIntegrationEvent`
  - `SuspiciousActivityDetectedIntegrationEvent`

**Notifications.Contracts:**
- `Requests/`: `SendNotificationRequest`, `CreateTemplateRequest`, `UpdateTemplateRequest`, `UpdatePreferencesRequest`.
- `Responses/`: `NotificationResponse`, `TemplateResponse`, `PreferenceResponse`.
- `IntegrationEvents/`:
  - `NotificationSentIntegrationEvent`

**Automation.Contracts:**
- `Requests/`: `CreateRuleRequest`, `UpdateRuleRequest`.
- `Responses/`: `AutomationRuleResponse`, `AutomationExecutionResponse`.
- `IntegrationEvents/`:
  - `AutomationRuleExecutedIntegrationEvent`

### 1.6 Alerting Rules (Prometheus AlertManager)

```
infrastructure/alerting/
+-- alertmanager.yml
+-- rules/
    +-- latency.yml
    +-- errors.yml
    +-- devices.yml
    +-- security.yml
```

Alert rules defined:
- Unlock pipeline latency > 500ms for 5 minutes (Critical)
- Error rate > 5% for 5 minutes (Warning)
- Device offline > 10 minutes (Warning)
- Failed login attempts > 50 in 5 minutes (Critical)
- MQTT broker disconnected (Critical)
- Database connection failures (Critical)

### 1.7 Grafana Dashboard

```
infrastructure/grafana/
+-- dashboards/
    +-- verixora-sla-dashboard.json
    +-- verixora-device-dashboard.json
    +-- verixora-security-dashboard.json
```

SLA dashboard includes:
- Unlock pipeline P50/P95/P99 latency
- Device online/offline count
- Active sessions
- Error rate
- Audit log ingestion rate
- Alert history

### 1.8 Mobile App

- **Notification List Screen:** displays notifications with read/unread status.
- **Push Notification Handling:** receives push via SignalR.
- **Notification Settings:** preferences for email/push.

### 1.9 Web Portal

- **Real-time Dashboard:** device count, online/offline, recent events, active alerts (charts).
- **Alert Management Page:** list, filter, acknowledge alerts.
- **Suspicious Activity Rules:** view and configure detection rules.
- **Notification Templates:** CRUD templates.
- **Automation Rules Page:** CRUD rules, view execution history.
- **System Metrics:** Prometheus/Grafana dashboards embedded.

---

## PHASE 2: IMPLEMENTATION

### 2.1 Monitoring.Domain (New Files)
```
Monitoring.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- DashboardSnapshot.cs
|   +-- Alert.cs
|   +-- SuspiciousActivityRule.cs
|   +-- SystemMetric.cs
+-- Enums/
|   +-- AlertSeverity.cs
|   +-- AlertType.cs
+-- Events/
|   +-- AlertCreated.cs
|   +-- AlertAcknowledged.cs
|   +-- DashboardSnapshotGenerated.cs
|   +-- SuspiciousActivityDetected.cs
+-- Services/
    +-- IAlertService.cs
    +-- ISuspiciousActivityDetector.cs
```

### 2.2 Monitoring.Application (New Files)
```
Monitoring.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- GenerateDashboardSnapshot/
|   |   +-- GenerateDashboardSnapshotCommand.cs
|   |   +-- GenerateDashboardSnapshotHandler.cs
|   +-- CreateAlert/
|   |   +-- CreateAlertCommand.cs
|   |   +-- CreateAlertHandler.cs
|   +-- AcknowledgeAlert/
|   |   +-- AcknowledgeAlertCommand.cs
|   |   +-- AcknowledgeAlertHandler.cs
|   +-- RecordSystemMetric/
|   |   +-- RecordSystemMetricCommand.cs
|   |   +-- RecordSystemMetricHandler.cs
|   +-- ConfigureSuspiciousActivityRule/
|       +-- ConfigureSuspiciousActivityRuleCommand.cs
|       +-- ConfigureSuspiciousActivityRuleHandler.cs
+-- Queries/
|   +-- GetDashboard/
|   |   +-- GetDashboardQuery.cs
|   |   +-- GetDashboardHandler.cs
|   +-- GetAlerts/
|   |   +-- GetAlertsQuery.cs
|   |   +-- GetAlertsHandler.cs
|   +-- GetAlertsBySeverity/
|   |   +-- GetAlertsBySeverityQuery.cs
|   |   +-- GetAlertsBySeverityHandler.cs
|   +-- GetSystemMetrics/
|   |   +-- GetSystemMetricsQuery.cs
|   |   +-- GetSystemMetricsHandler.cs
|   +-- GetLiveEvents/
|   |   +-- GetLiveEventsQuery.cs
|   |   +-- GetLiveEventsHandler.cs
|   +-- GetSuspiciousActivityRules/
|       +-- GetSuspiciousActivityRulesQuery.cs
|       +-- GetSuspiciousActivityRulesHandler.cs
+-- DTOs/
|   +-- DashboardDto.cs
|   +-- AlertDto.cs
|   +-- SystemMetricDto.cs
|   +-- SuspiciousActivityRuleDto.cs
+-- Interfaces/
    +-- IDashboardRepository.cs
    +-- IAlertRepository.cs
    +-- ISystemMetricRepository.cs
    +-- ISuspiciousActivityRuleRepository.cs
```

### 2.3 Monitoring.Infrastructure (New Files)
```
Monitoring.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- MonitoringDbContext.cs
|   +-- Configurations/
|   |   +-- DashboardSnapshotConfiguration.cs
|   |   +-- AlertConfiguration.cs
|   |   +-- SuspiciousActivityRuleConfiguration.cs
|   |   +-- SystemMetricConfiguration.cs
|   +-- Repositories/
|       +-- DashboardRepository.cs
|       +-- AlertRepository.cs
|       +-- SystemMetricRepository.cs
|       +-- SuspiciousActivityRuleRepository.cs
+-- Services/
|   +-- AlertService.cs
|   +-- SuspiciousActivityDetector.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.4 Monitoring.Presentation (New Files)
```
Monitoring.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- DashboardController.cs
    +-- AlertController.cs
    +-- MetricsController.cs
    +-- SuspiciousActivityController.cs
```

### 2.5 Monitoring.Contracts (New Files)
```
Monitoring.Contracts/
+-- (existing files from Iteration 0)
+-- Responses/
|   +-- DashboardResponse.cs
|   +-- AlertResponse.cs
|   +-- SystemMetricResponse.cs
|   +-- SuspiciousActivityRuleResponse.cs
+-- IntegrationEvents/
    +-- AlertCreatedIntegrationEvent.cs
    +-- SuspiciousActivityDetectedIntegrationEvent.cs
```

### 2.6 Notifications.Domain (New Files)
```
Notifications.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- NotificationTemplate.cs
|   +-- Notification.cs
|   +-- UserPreference.cs
+-- Enums/
|   +-- NotificationType.cs
|   +-- NotificationStatus.cs
+-- Events/
|   +-- NotificationSent.cs
|   +-- NotificationFailed.cs
+-- Services/
    +-- INotificationSender.cs
```

### 2.7 Notifications.Application (New Files)
```
Notifications.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- SendNotification/
|   |   +-- SendNotificationCommand.cs
|   |   +-- SendNotificationHandler.cs
|   +-- CreateTemplate/
|   |   +-- CreateTemplateCommand.cs
|   |   +-- CreateTemplateHandler.cs
|   +-- UpdateTemplate/
|   |   +-- UpdateTemplateCommand.cs
|   |   +-- UpdateTemplateHandler.cs
|   +-- UpdatePreferences/
|       +-- UpdatePreferencesCommand.cs
|       +-- UpdatePreferencesHandler.cs
+-- Queries/
|   +-- GetNotifications/
|   |   +-- GetNotificationsQuery.cs
|   |   +-- GetNotificationsHandler.cs
|   +-- GetTemplates/
|   |   +-- GetTemplatesQuery.cs
|   |   +-- GetTemplatesHandler.cs
|   +-- GetPreferences/
|       +-- GetPreferencesQuery.cs
|       +-- GetPreferencesHandler.cs
+-- DTOs/
|   +-- NotificationDto.cs
|   +-- TemplateDto.cs
|   +-- PreferenceDto.cs
+-- Interfaces/
    +-- INotificationRepository.cs
    +-- ITemplateRepository.cs
    +-- IPreferenceRepository.cs
```

### 2.8 Notifications.Infrastructure (New Files)
```
Notifications.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- NotificationsDbContext.cs
|   +-- Configurations/
|   |   +-- NotificationTemplateConfiguration.cs
|   |   +-- NotificationConfiguration.cs
|   |   +-- UserPreferenceConfiguration.cs
|   +-- Repositories/
|       +-- NotificationRepository.cs
|       +-- TemplateRepository.cs
|       +-- PreferenceRepository.cs
+-- Services/
|   +-- EmailSender.cs
|   +-- PushNotificationSender.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.9 Notifications.Presentation (New Files)
```
Notifications.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- NotificationController.cs
    +-- TemplateController.cs
    +-- PreferenceController.cs
```

### 2.10 Notifications.Contracts (New Files)
```
Notifications.Contracts/
+-- (existing files from Iteration 0)
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
    +-- NotificationSentIntegrationEvent.cs
```

### 2.11 Automation.Domain (New Files)
```
Automation.Domain/
+-- (existing files from Iteration 0)
+-- Entities/
|   +-- AutomationRule.cs
|   +-- AutomationExecution.cs
+-- Enums/
|   +-- TriggerType.cs
|   +-- ConditionOperator.cs
|   +-- ActionType.cs
|   +-- ExecutionResult.cs
+-- ValueObjects/
|   +-- AutomationTrigger.cs
|   +-- AutomationCondition.cs
|   +-- AutomationAction.cs
+-- Events/
|   +-- AutomationRuleTriggered.cs
|   +-- AutomationRuleExecuted.cs
|   +-- AutomationRuleBlocked.cs
|   +-- AutomationLoopDetected.cs
+-- Services/
    +-- IAutomationSafetyService.cs
```

### 2.12 Automation.Application (New Files)
```
Automation.Application/
+-- (existing files from Iteration 0)
+-- Commands/
|   +-- CreateRule/
|   |   +-- CreateRuleCommand.cs
|   |   +-- CreateRuleHandler.cs
|   |   +-- CreateRuleValidator.cs
|   +-- UpdateRule/
|   |   +-- UpdateRuleCommand.cs
|   |   +-- UpdateRuleHandler.cs
|   +-- DeleteRule/
|   |   +-- DeleteRuleCommand.cs
|   |   +-- DeleteRuleHandler.cs
|   +-- ActivateRule/
|   |   +-- ActivateRuleCommand.cs
|   |   +-- ActivateRuleHandler.cs
|   +-- DeactivateRule/
|   |   +-- DeactivateRuleCommand.cs
|   |   +-- DeactivateRuleHandler.cs
|   +-- ExecuteRule/
|       +-- ExecuteRuleCommand.cs
|       +-- ExecuteRuleHandler.cs
+-- Queries/
|   +-- GetRulesByHome/
|   |   +-- GetRulesByHomeQuery.cs
|   |   +-- GetRulesByHomeHandler.cs
|   +-- GetRuleExecutions/
|       +-- GetRuleExecutionsQuery.cs
|       +-- GetRuleExecutionsHandler.cs
+-- Services/
|   +-- IAutomationScheduler.cs
|   +-- IAutomationEventHandler.cs
+-- DTOs/
|   +-- AutomationRuleDto.cs
|   +-- AutomationExecutionDto.cs
+-- Interfaces/
    +-- IAutomationRuleRepository.cs
    +-- IAutomationExecutionRepository.cs
```

### 2.13 Automation.Infrastructure (New Files)
```
Automation.Infrastructure/
+-- (existing files from Iteration 0)
+-- Persistence/
|   +-- AutomationDbContext.cs
|   +-- Configurations/
|   |   +-- AutomationRuleConfiguration.cs
|   |   +-- AutomationExecutionConfiguration.cs
|   +-- Repositories/
|       +-- AutomationRuleRepository.cs
|       +-- AutomationExecutionRepository.cs
+-- Services/
|   +-- AutomationSafetyService.cs
|   +-- AutomationScheduler.cs
|   +-- AutomationEventHandler.cs
+-- Migrations/
    +-- (EF Core generated)
```

### 2.14 Automation.Presentation (New Files)
```
Automation.Presentation/
+-- (existing files from Iteration 0)
+-- Controllers/
    +-- AutomationRuleController.cs
    +-- AutomationExecutionController.cs
```

### 2.15 Automation.Contracts (New Files)
```
Automation.Contracts/
+-- (existing files from Iteration 0)
+-- Requests/
|   +-- CreateRuleRequest.cs
|   +-- UpdateRuleRequest.cs
+-- Responses/
|   +-- AutomationRuleResponse.cs
|   +-- AutomationExecutionResponse.cs
+-- IntegrationEvents/
    +-- AutomationRuleExecutedIntegrationEvent.cs
```

### 2.16 Infrastructure Config (New Files)
```
infrastructure/
+-- alerting/
|   +-- alertmanager.yml
|   +-- rules/
|       +-- latency.yml
|       +-- errors.yml
|       +-- devices.yml
|       +-- security.yml
+-- grafana/
    +-- dashboards/
        +-- verixora-sla-dashboard.json
        +-- verixora-device-dashboard.json
        +-- verixora-security-dashboard.json
```

---

## PHASE 3: INTEGRATION

- Wire Monitoring, Notifications, and Automation modules DI into ApiHost.
- Run EF Core migrations for all three schemas.
- Configure email provider (SMTP) in appsettings.
- Wire SignalR for push notifications.
- Set up Prometheus scraping endpoint.
- Deploy AlertManager with defined rules.
- Import Grafana dashboards.
- Automation event handler subscribes to domain events.
- Automation scheduler starts on application startup.
- Suspicious activity detector analyzes audit logs.
- CI pipeline runs all new tests including contract tests.

---

## PHASE 4: TESTING

### 4.1 Unit Tests (Domain)
- `Alert_Create_ShouldSetSeverity`
- `NotificationTemplate_Render_ShouldReplacePlaceholders`
- `AutomationTrigger_TimeBased_ShouldParseCron`
- `AutomationRule_DepthExceeded_ShouldBeBlocked`
- `AutomationSafety_LoopDetected_ShouldBlock`
- `SuspiciousActivityRule_ThresholdExceeded_ShouldDetect`

### 4.2 Application Tests
- `Dashboard_ShouldReturnCorrectState`
- `Alerts_ShouldFilterBySeverity`
- `SendEmail_ShouldSucceed`
- `SendPush_ShouldDeliverViaSignalR`
- `UpdatePreferences_ShouldPersist`
- `Rule_ShouldTriggerOnEvent`
- `Rule_ShouldExecuteActions`
- `RuleLoop_ShouldBeBlocked`
- `Rule_DepthLimit_ShouldBeEnforced`
- `Rule_Execution_ShouldBeAudited`
- `SuspiciousActivity_FailedUnlocks_ShouldTriggerAlert`
- `SuspiciousActivity_FailedLogins_ShouldTriggerAlert`
- `SuspiciousActivity_DeviceOffline_ShouldTriggerAlert`
- `SuspiciousActivity_MultipleDevicesOffline_ShouldTriggerCritical`
- `SuspiciousActivity_ApiKeyNewIp_ShouldTriggerAlert`

### 4.3 Integration Tests
- `DashboardController_ShouldReturnDashboard`
- `AlertController_Acknowledge_ShouldWork`
- `NotificationController_Send_ShouldDeliver`
- `AutomationRuleController_CRUD_ShouldWork`
- `AutomationScheduler_ShouldExecuteCronRule`
- `SuspiciousActivityDetector_ShouldAnalyzeAuditLogs`
- `AlertManager_Rules_ShouldTrigger`
- `Grafana_Dashboard_ShouldLoad`

### 4.4 Contract Tests
- `Monitoring_Contracts_ShouldMatchPublishedEvents`
- `Notifications_Contracts_ShouldMatchPublishedEvents`
- `Automation_Contracts_ShouldMatchPublishedEvents`

### 4.5 E2E Tests
- Alert generated on device offline → notification sent → appears in mobile app.
- Suspicious activity detected → alert raised → email sent.
- Automation rule created → event triggers → action executes → audit log recorded.

---

## PHASE 5: DEPLOYMENT

- Deploy updated backend to staging.
- Configure email service for staging.
- Deploy Prometheus, Grafana, and AlertManager.
- Import SLA dashboard.
- Build mobile app with notifications.
- Build web portal with dashboard.
- Run full validation script.

---

## ACCEPTANCE CRITERIA

- [ ] Dashboard shows accurate system status.
- [ ] Alerts generated for critical events.
- [ ] Suspicious activity detection analyzes audit patterns.
- [ ] Email notifications sent and delivered.
- [ ] Push notifications work via SignalR.
- [ ] Automation rules support time-based, event-based, and condition-based triggers.
- [ ] Automation safety rules enforced (depth limit, loop detection).
- [ ] All automation actions are audited.
- [ ] Integration event contracts defined.
- [ ] Contract tests pass.
- [ ] AlertManager rules configured.
- [ ] Grafana SLA dashboard operational.
- [ ] Prometheus metrics exposed.
- [ ] Mobile app receives push notifications.
- [ ] Web portal shows real-time dashboard.
- [ ] All tests pass.

---

## TRACEABILITY TO MASTER SPEC

| Master Spec Requirement            | Iteration 5 Coverage                    |
| ---------------------------------- | --------------------------------------- |
| Monitoring dashboard (SHOULD)      | DashboardSnapshot, real-time events     |
| Email notifications (SHOULD)       | EmailSender, templates                  |
| Push notifications (NICE)          | SignalR push notifications              |
| Automation engine (NICE)           | IF-THEN rules, triggers, actions        |
| Automation safety                  | Depth limits, loop detection            |
| Suspicious activity detection      | SuspiciousActivityDetector              |
| Alerting rules (ADR-021)           | AlertManager configuration              |
| SLA dashboard (ADR-021)            | Grafana dashboards                      |
| Integration event contracts        | Monitoring + Notifications + Automation |
| Audit automation actions           | AutomationExecution records             |

---

**ITERATION 5 COMPLETE.**

All improvements integrated:
- Integration event contracts (Monitoring, Notifications, Automation)
- Suspicious activity detection
- Alerting rules (AlertManager)
- SLA dashboard (Grafana)