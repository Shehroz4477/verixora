# FINAL VERIXORA VALIDATION SCRIPT (CLEAN VERSION)

---

# PURPOSE

The VERIXORA Validation Script enforces architectural integrity across the entire solution.

It ensures:

* module isolation
* dependency correctness
* SharedKernel purity
* contract safety
* structural consistency

---

# EXECUTION RULES

* Script must run in CI pipeline
* Script failure blocks build
* Script output must be deterministic

---

# SEVERITY LEVELS

Each violation must be categorized:

* CRITICAL -> build fails immediately
* HIGH -> build fails
* MEDIUM -> warning only
* INFO -> reporting only

---

# REQUIRED CHECKS

## 1. PROJECT STRUCTURE VALIDATION

* All expected module projects exist
* Each module contains:

  * Domain
  * Application
  * Infrastructure
  * Presentation
  * Contracts

---

## 2. DEPENDENCY RULE VALIDATION

### Domain Layer Rules:

* Domain can only reference SharedKernel.Domain
* Domain must NOT reference any infrastructure or application layer

### Application Layer Rules:

* Can reference:

  * its own Domain
  * SharedKernel.Application

### Infrastructure Layer Rules:

* Can reference:

  * its own Application
  * its own Domain
  * BuildingBlocks.Infrastructure

### Presentation Layer Rules:

* Can reference:

  * its own Application
  * Infrastructure (only via DI composition)

### Contracts Rules:

* Must have ZERO project references

---

## 3. SHARED KERNEL PROTECTION

* No external project can reference SharedKernel.Domain internals
* SharedKernel must not depend on any module
* No NuGet dependencies allowed in SharedKernel.Domain

---

## 4. API HOST VALIDATION

* ApiHost must reference all Presentation projects
* ApiHost must reference BuildingBlocks.Infrastructure
* ApiHost must NOT contain business logic

---

## 5. CIRCULAR DEPENDENCY DETECTION

Script must detect:

* direct cycles
* indirect cycles
* transitive dependency loops

---

## 6. DOMAIN INTEGRITY VALIDATION

* Domain layer must contain ONLY:

  * entities
  * value objects
  * domain services
  * domain events

RULE:
No infrastructure code allowed inside Domain.

---

## 7. MODULE COMPLETENESS VALIDATION

Each module must contain:

* Domain project
* Application project
* Infrastructure project
* Presentation project
* Contracts project

If missing -> CRITICAL failure

---

## 8. OUTPUT FORMAT

Script output must include:

* Summary (pass/fail)
* Table of violations
* Severity classification
* File/module location
* Expected vs actual result
* Fix suggestion

---

## 9. CI/CD BEHAVIOR

* CRITICAL or HIGH failure -> pipeline fails
* MEDIUM -> warning in logs
* INFO -> report only