# Domain Exception Usage Patterns (Finalized Rule Set)

This document serves as the official behavioral contract for how `DomainException` is used across all modules.

## 1. Core Principle

`DomainException` is ONLY for enforcing business invariants inside the Domain layer.

**It is NOT for:**
- validation in API layer
- infrastructure failures
- application workflow control
- user input formatting errors

**It IS for:**
- protecting domain rules
- enforcing invariants inside aggregates
- stopping invalid state transitions

## 2. Where You Must Throw DomainException

### 2.1 Inside Aggregate Roots (Primary Rule)

Any time a business rule is violated inside an entity:

```csharp
public void Activate()
{
    if (Status == UserStatus.Blocked)
        throw new DomainException("USER_STATE_BLOCKED", "Blocked user cannot be activated.");

    Status = UserStatus.Active;
}
```

**Allowed because:** business rule lives inside domain and state transition is invalid.

### 2.2 Inside Value Objects

When constructing an invalid domain value:

```csharp
public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("USER_EMAIL_EMPTY", "Email cannot be empty.");

        if (!value.Contains("@"))
            throw new DomainException("USER_EMAIL_INVALID", "Email format is invalid.");

        Value = value;
    }
}
```

**Allowed because:** value object must always be valid.

### 2.3 Inside Factory Methods

```csharp
public static User Create(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        throw new DomainException("USER_CREATE_EMAIL_REQUIRED", "Email is required.");

    return new User(email);
}
```

**Ensures:** no invalid aggregate creation.

### 2.4 Inside Domain Methods (Behavior Methods)

Any business behavior method: `ChangePassword()`, `AssignRole()`, `LockDevice()`, `EnableFeature()`

**Rule:** if a business rule is violated, throw `DomainException` immediately.

## 3. Where You Must Not Throw DomainException

### 3.1 Application Layer

**Do not do this:**
```csharp
throw new DomainException(...)
```

**Instead:**
- use validation
- use command validators (FluentValidation or custom)
- return Result pattern if needed

### 3.2 Infrastructure Layer

Never throw `DomainException` for:
- database errors
- HTTP failures
- network issues
- external API failures

**Use:** `InfrastructureException`, logging, and retry mechanisms.

### 3.3 Controllers / Presentation Layer

Do not use `DomainException` directly for:
- input validation
- model binding errors

**These should be handled before domain entry.**

## 4. When Not to Throw (Important Design Rule)

Sometimes you should not throw even if invalid:

**Case 1: Optional business checks in queries**

Instead of throwing, return `false`:

```csharp
public bool CanActivate()
{
    return Status != UserStatus.Blocked;
}
```

**Case 2: Multi-rule evaluation**

Instead of stopping at the first error, collect errors in the Application layer and return a validation result.

## 5. Domain Boundary Rule

**Golden Rule:** `DomainException` is only allowed when a state change is being prevented inside the Domain Model.

- Command → Domain → enforce rule → throw
- Query → do not throw
- API → do not throw

## 6. Aggregate Safety Rule

Every Aggregate Root must ensure:
- It can never enter an invalid state
- All invariants are enforced internally
- No external trust is assumed

## 7. Error Code Usage Rule (Important Alignment)

All `DomainException`s must follow the pattern:

`MODULE_CONTEXT_REASON`

**Examples:**
- `USER_STATE_BLOCKED`
- `DEVICE_LOCK_ALREADY_ACTIVE`
- `AUTH_PERMISSION_DENIED`

**Mandatory:** every `DomainException` must have a structured code.

## 8. Final Behavior Summary

| Layer | Can Throw DomainException? |
|-------|---------------------------|
| Domain (Entities / VO / Factory) | Yes |
| Application | No |
| Infrastructure | No |
| Presentation | No |