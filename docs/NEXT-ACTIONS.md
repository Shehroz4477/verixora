# VERIXORA Next Actions

This is the active task list. Keep it short and update it as work completes.

## Now

1. Clean and finalize `SharedKernel.Domain`.
2. Add architecture validation script.
3. Add test projects.
4. Add first architecture tests for project references.
5. Start Identity domain model.

## Phase 1: SharedKernel Foundation

### Goals

- Establish reliable domain primitives.
- Keep domain code framework-independent.
- Prepare architecture enforcement before module implementation grows.

### Tasks

- Review `Entity<TId>` equality and domain event behavior.
- Review `ValueObject` equality and null-component behavior.
- Review `DomainException` constructors and error-code support.
- Add guard clauses.
- Add strongly typed domain event base if useful.
- Reduce excessive tutorial comments in source files.
- Keep educational explanations in docs, not inside every code line.
- Verify `SharedKernel.Domain` has no project references.
- Verify `SharedKernel.Domain` has no NuGet package references.
- Keep all backend projects on `.NET 8`.

## Phase 2 Preview: Identity

Identity should start only after SharedKernel is stable.

Initial Identity domain candidates:

- User
- Email
- PhoneNumber
- PasswordHash
- TrustedDevice
- UserSession
- UserRegisteredDomainEvent
- EmailVerificationOtpRequestedDomainEvent

## Rules While Working

- Update docs when a requirement or decision changes.
- Keep changes small and buildable.
- Add tests for business rules before moving to another module.
- Do not start frontend work until backend APIs are stable.
