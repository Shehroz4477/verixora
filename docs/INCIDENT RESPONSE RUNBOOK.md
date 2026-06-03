# VERIXORA INCIDENT RESPONSE RUNBOOK

---

## PURPOSE

This document defines procedures for responding to security incidents affecting the VERIXORA platform. All operations team members must be familiar with these procedures.

---

## INCIDENT SEVERITY LEVELS

| Level | Name | Description | Response Time |
|-------|------|-------------|---------------|
| 1 | Critical | Active breach, unauthorized access, multiple devices compromised | Immediate |
| 2 | High | Suspicious activity detected, single device compromised, failed access pattern | 15 minutes |
| 3 | Medium | Service degradation, increased error rates, MQTT broker issues | 30 minutes |
| 4 | Low | Non-critical alerts, backup failures, minor performance issues | 2 hours |

---

## PROCEDURE 1: FORCE-LOCK ALL DOORS

**When to use:** Suspected or confirmed unauthorized access, security breach, or emergency situation requiring immediate lockdown.

**Steps:**

1. Log in to VERIXORA admin portal as SystemAdmin.
2. Navigate to Emergency Controls → Force Lock All.
3. Select the affected Home(s) or select All Homes.
4. Confirm force-lock. System sends emergency lock command to all smart locks via MQTT.
5. Verify all devices show Locked state in the dashboard.
6. Document the incident:
   - Time of force-lock
   - Reason
   - Homes affected
   - Who authorized

**Rollback:** Only SystemAdmin can issue Unlock All after incident resolution and security verification.

---

## PROCEDURE 2: REVOKE ALL SESSIONS

**When to use:** Compromised user account, suspicious session activity, or forced logout required.

**Steps:**

1. Log in to VERIXORA admin portal as SystemAdmin.
2. Navigate to User Management → select affected user.
3. Click Revoke All Sessions.
4. Confirm. System invalidates all refresh tokens and active JWT tokens for the user.
5. All devices with active sessions for that user will be forced to re-authenticate.
6. Document the incident:
   - User affected
   - Time of revocation
   - Reason

**Post-action:** User must log in again on all devices. Unknown device OTP challenge will apply.

---

## PROCEDURE 3: ROTATE SIGNING KEYS

**When to use:** Suspected key compromise, scheduled rotation, or after a security incident.

**Steps:**

1. Log in to VERIXORA admin portal as SystemAdmin.
2. Navigate to Security → Key Management.
3. Click Rotate JWT Signing Key.
4. Confirm rotation. System generates new RSA/ECDSA key pair.
5. New key becomes active immediately for new token issuance.
6. Old key remains valid for token validation for 15 minutes (max JWT lifetime).
7. After 15 minutes, old key is archived.
8. Document:
   - Time of rotation
   - Reason (scheduled or emergency)
   - New key ID

**Post-action:** All existing tokens expire naturally within 15 minutes. No forced logout needed.

---

## PROCEDURE 4: ISOLATE COMPROMISED DEVICE

**When to use:** A specific IoT device shows suspicious behavior, unauthorized commands, or is reported lost/stolen.

**Steps:**

1. Log in to VERIXORA admin portal as SystemAdmin or Home Owner.
2. Navigate to Devices → select affected device.
3. Click Decommission Device.
4. Confirm. System:
   - Revokes MQTT credentials
   - Sends decommission command if device is online
   - Marks device status as Decommissioned
   - Logs the action in audit trail
5. Physically inspect the device if accessible.
6. Document:
   - Device ID
   - Home
   - Time of decommissioning
   - Reason

**Re-activation:** Decommissioned devices require full re-provisioning. Cannot be re-activated remotely.

---

## PROCEDURE 5: REVOKE COMPROMISED API KEY

**When to use:** API key exposed in code repository, unauthorized API key usage detected, or service account decommissioned.

**Steps:**

1. Log in to VERIXORA admin portal as Home Owner.
2. Navigate to API Keys → select affected key.
3. Click Revoke.
4. Confirm. System immediately invalidates the key.
5. All services using the key will receive 401 Unauthorized.
6. Create a new API key if needed.
7. Document:
   - Key ID
   - Home
   - Time of revocation
   - Reason
   - New key ID (if created)

---

## PROCEDURE 6: RESPOND TO SUSPICIOUS ACTIVITY ALERT

**When to use:** AlertManager triggers a suspicious activity alert (failed unlocks, failed logins, device offline patterns).

**Steps:**

1. Acknowledge alert in AlertManager or VERIXORA dashboard.
2. Investigate:
   - Review audit logs for the time window specified in the alert.
   - Check if the pattern matches a known user (forgot password, new device) or appears malicious.
   - Check IP addresses and device fingerprints.
3. If malicious:
   - Follow Procedure 2 (Revoke All Sessions) for affected user.
   - Follow Procedure 4 (Isolate Device) if a specific device is involved.
   - Escalate to Level 1 or 2 incident.
4. If non-malicious:
   - Contact user to verify activity.
   - Update alert threshold if needed.
5. Document findings and actions.

---

## PROCEDURE 7: DATABASE BREACH RESPONSE

**When to use:** Suspected or confirmed database compromise, data exfiltration, or unauthorized database access.

**Steps:**

1. Immediately isolate the database instance (restrict network access).
2. Rotate database credentials.
3. Follow Procedure 3 (Rotate Signing Keys).
4. Follow Procedure 2 (Revoke All Sessions) for all users.
5. Follow Procedure 5 (Revoke All API Keys).
6. Assess data exposure:
   - PII is encrypted at column level (AES-256). Assess if encryption keys were also compromised.
   - If encryption keys in secrets manager were not compromised, PII remains protected.
   - If encryption keys were compromised, initiate data breach notification process.
7. Restore database from last clean backup if needed.
8. Document full incident timeline and actions.

---

## PROCEDURE 8: MQTT BROKER FAILURE

**When to use:** MQTT broker becomes unavailable, devices cannot receive commands.

**Steps:**

1. Verify broker status via health checks or Prometheus metrics.
2. Attempt broker restart.
3. If restart fails:
   - Devices will continue operating in their current state.
   - Unlock/lock commands will fail gracefully with appropriate error.
   - Rate limiting continues to function.
4. Check broker logs for cause.
5. If hardware failure, failover to secondary broker if available.
6. Document:
   - Time of failure
   - Duration
   - Impact (commands failed)
   - Resolution

**Recovery:** Devices reconnect automatically when broker is available. Heartbeat resumes, new MQTT tokens issued.

---

## PROCEDURE 9: NOTIFY AFFECTED USERS

**When to use:** Security incident affects specific users or all users of a Home.

**Steps:**

1. Prepare notification with:
   - Brief description of incident (without sensitive details).
   - Actions taken (force-lock, session revocation, etc.).
   - Actions required from user (re-login, verify devices).
   - Contact for questions.
2. Send via:
   - Email (all affected users).
   - Push notification (users with mobile app).
3. Document notification sent including:
   - Recipients
   - Time sent
   - Content

---

## ESCALATION CONTACTS

| Role | Contact | When to Escalate |
|------|---------|-----------------|
| Security Lead | [security-lead@verixora.com] | Level 1, Level 2 incidents |
| DevOps Lead | [devops-lead@verixora.com] | Infrastructure failures |
| CTO | [cto@verixora.com] | Data breach, prolonged outage |
| Legal | [legal@verixora.com] | Data breach requiring regulatory notification |
| Support Lead | [support@verixora.com] | User-impacting incidents |

---

## INCIDENT LOG TEMPLATE

```
INCIDENT REPORT
---------------
Incident ID:
Date/Time:
Severity Level:
Reported By:
Affected Homes:
Affected Users:
Affected Devices:

Summary:

Timeline:
- [Time] - Incident detected
- [Time] - Response initiated
- [Time] - Actions taken
- [Time] - Incident resolved

Actions Taken:

Root Cause:

Follow-up Actions:

Signed Off By:
Date:
```

---

**DOCUMENT VERSION: 1.0**
**LAST UPDATED: 2026-06-03**

---

**INCIDENT RESPONSE RUNBOOK COMPLETE.**
