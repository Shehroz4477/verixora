# VERIXORA — User Aggregate Root Design (Identity Module)

This is the first real Domain Entity in VERIXORA.

We are still in the Domain Layer only.

No CQRS, no Repository, no EF Core, no Infrastructure, no Application Layer.

## 1. Why User Is an Aggregate Root

The User controls:
- Authentication identity
- Roles
- Permissions
- Account status
- Security state
- MFA configuration
- Face verification enrollment
- Device ownership relationships

Many objects depend on User. Therefore, User becomes the Aggregate Root of the Identity Module.

## 2. User Aggregate Responsibilities

The User aggregate must protect:

**Identity Rules**
- Username uniqueness (validated externally)
- Email uniqueness (validated externally)

**Account Rules**
- User cannot activate if deleted
- User cannot login if blocked
- User cannot change password if deleted

**Security Rules**
- Password must exist
- Password history maintained
- MFA state controlled

**Lifecycle Rules**
- Create
- Activate
- Deactivate
- Block
- Unblock
- Delete

## 3. User Aggregate Root Structure

```
User
│
├── Id
├── Username
├── Email
├── PasswordHash
├── Status
├── IsDeleted
├── IsMfaEnabled
├── CreatedAtUtc
├── UpdatedAtUtc
│
├── Activate()
├── Deactivate()
├── Block()
├── Unblock()
├── ChangePassword()
├── EnableMfa()
├── DisableMfa()
├── SoftDelete()
│
└── Domain Events
```

## 4. User Status Enum

| Status | Purpose |
|--------|---------|
| Pending | Newly created account |
| Active | Can use system |
| Inactive | Temporarily disabled |
| Blocked | Security action |
| Deleted | Soft deleted account |

## 5. User Invariants (Very Important)

The aggregate must NEVER allow:

| Rule | Description |
|------|-------------|
| 1 | Deleted user cannot become active (Deleted → Active is forbidden) |
| 2 | Deleted user cannot login |
| 3 | Blocked user cannot be activated directly. Must: Blocked → Unblock → Activate |
| 4 | Password cannot be empty |
| 5 | Username cannot be empty |
| 6 | Email cannot be empty |
| 7 | Deleted account cannot change password |

## 6. User Behaviors

### Activate()

**Purpose:** Pending → Active, Inactive → Active

**Allowed:** Pending, Inactive

**Forbidden:** Deleted, Blocked

### Deactivate()

**Purpose:** Active → Inactive

### Block()

**Purpose:** Active → Blocked, Inactive → Blocked

### Unblock()

**Purpose:** Blocked → Inactive

**Important:** Never directly return to Active. Requires explicit activation.

### ChangePassword()

**Checks:**
- User not deleted
- New password hash exists
- Password history validation

### EnableMfa()

**Checks:** User active, user not deleted

### DisableMfa()

**Checks:** User active

### SoftDelete()

**Purpose:** Any state → Deleted

**Effects:** Login disabled, password changes disabled, account operations disabled

## 7. Domain Events

The User aggregate will raise events. Examples:

- `UserCreatedDomainEvent` – User registered
- `UserActivatedDomainEvent` – User activated
- `UserBlockedDomainEvent` – Security block occurred
- `UserPasswordChangedDomainEvent` – Password updated
- `UserDeletedDomainEvent` – Account deleted

## 8. Aggregate Boundary

**User Aggregate owns:**
- User Identity
- Security State
- MFA State

**User Aggregate does NOT own:**
- Roles (separate aggregate)
- Permissions (separate aggregate)
- Devices (Device Module aggregate)
- Smart Locks (SmartLock aggregate)

References are by Id only.

## 9. Domain Exception Examples

Examples that must throw:

| Error Code | Description |
|------------|-------------|
| `USER_STATE_DELETED` | Deleted user attempting activation |
| `USER_STATE_BLOCKED` | Blocked user attempting activation |
| `USER_PASSWORD_EMPTY` | Password missing |
| `USER_EMAIL_EMPTY` | Email missing |

## 10. Aggregate Root Checklist

Before implementation:

- [x] Aggregate boundary defined
- [x] State model defined
- [x] Invariants defined
- [x] Behaviors defined
- [x] Domain events identified
- [x] Exception rules identified