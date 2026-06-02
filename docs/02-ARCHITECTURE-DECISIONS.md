# 1. ADR DOCUMENT 2 – ARCHITECTURAL REVIEW

Your ADR set is already strong, but it has **3 structural weaknesses**:

---

## ISSUE 1: No “System-wide Architectural Boundaries”

You define decisions per topic, but missing:

### Missing concept:

> What rules are ALWAYS true regardless of ADR?

Examples:

* module isolation rules
* domain ownership enforcement
* event usage rules

Without this, ADRs can conflict later.

---

## ISSUE 2: No conflict resolution strategy

You have many decisions:

* security first
* modular monolith
* backend authority
* device passive model

But missing:

### What happens if ADRs conflict?

Example:

* performance vs security
* automation vs safety
* usability vs validation chain

No resolution hierarchy exists.

---

## ISSUE 3: IoT + security decisions are split inconsistently

Some decisions:

* MQTT in one ADR
* device model in spec
* security in another doc

### Problem:

This creates:

> scattered architectural truth

We need ADR document to clearly unify:

* device trust model
* communication model
* failure model

---

## 2. MEDIUM ISSUES

### 1. No evolution strategy

What happens when:

* MQTT replaced
* face provider changes
* database changes

No ADR evolution policy defined.

---

### 2. No explicit “non-goals”

ADR should define what is NOT allowed.

Example:

* no microservices
* no direct module coupling
* no device-side authorization

---

### 3. No infrastructure decision boundaries

Missing:

* caching rules
* logging strategy
* retry policies
* message retry strategy

---

# 3. WHAT IS GOOD IN YOUR ADR

You already correctly defined:

* modular monolith choice
* clean architecture usage
* backend authority
* passive devices
* schema-per-module DB
* immutability for audit logs
* face verification abstraction

So we are not fixing logic — only formalizing structure.

---

# NOW WE FINALIZE ADR DOCUMENT

I will now generate:

* clean VS Code compatible version
* no Unicode symbols
* strict architectural format
* production-ready decision system

---

# VERIXORA ARCHITECTURE DECISIONS (FINAL)

---

# ADR SYSTEM OVERVIEW

This document defines all architectural decisions for VERIXORA.

All system behavior must follow these decisions.

If any conflict exists:
System must follow ADR priority rules defined below.

---

# ADR-001: MODULAR MONOLITH ARCHITECTURE

VERIXORA will be built as a Modular Monolith.

REASONS:

* single deployment unit
* strong module isolation
* reduced operational complexity
* easier testing and debugging

RULE:
Modules must never be split into independent services at this stage.

---

# ADR-002: CLEAN ARCHITECTURE INSIDE MODULES

Each module must follow:

* Domain Layer
* Application Layer
* Infrastructure Layer
* Presentation Layer

RULE:
Domain must never depend on infrastructure.

---

# ADR-003: BACKEND IS SYSTEM OF RECORD

Backend is the only authority for all decisions.

RULES:

* devices cannot decide access
* frontend cannot override backend decisions
* IoT devices are passive executors

---

# ADR-004: PASSIVE DEVICE MODEL

IoT devices (ESP32) must:

* execute backend commands only
* send telemetry only
* never make authorization decisions

FAILURE MODEL:

* devices may disconnect
* commands must be idempotent
* retry must be handled by backend

---

# ADR-005: MQTT IS PRIMARY IOT PROTOCOL

MQTT will be used for:

* device commands
* telemetry
* heartbeat signals

RULE:
MQTT messages must be stateless and replay-safe.

---

# ADR-006: BLE IS ONLY FOR PROVISIONING

BLE is strictly used for:

* device onboarding
* WiFi credential transfer

RULE:
BLE cannot be used for operational commands.

---

# ADR-007: SIGNALR IS FOR REAL-TIME UI ONLY

SignalR is used only for:

* dashboard updates
* live monitoring
* notifications to web/mobile

RULE:
SignalR must NOT control devices directly.

---

# ADR-008: SCHEMA-PER-MODULE DATABASE DESIGN

Each module owns its schema.

RULES:

* one database initially
* logical separation per module
* no cross-module table sharing

---

# ADR-009: EVENT-DRIVEN ARCHITECTURE

System uses:

* Domain Events (inside module)
* Integration Events (cross module)

RULE:
Modules communicate only via events or contracts.

---

# ADR-010: AUDIT LOG IMMUTABILITY

Audit logs are:

* append-only
* immutable
* never updated or deleted

RULE:
All security-sensitive actions must generate audit entries.

---

# ADR-011: FACE VERIFICATION IS PLUGGABLE

Face verification must be abstracted behind interface.

RULES:

* provider can be mocked or real
* no direct dependency on ML model
* must be optional per door policy

---

# ADR-012: SECURITY FIRST PRIORITY MODEL

Security overrides:

* usability
* performance
* automation convenience

RULE:
If conflict exists, security decision wins.

---

# ADR-013: SYSTEM BEHAVIOR INVARIANTS

The system must always guarantee:

* no device acts without backend approval
* no cross-home data access
* no bypass of authentication
* all actions are traceable

---

# ADR-014: NO DISTRIBUTED MICROSERVICES AT THIS STAGE

Microservices are explicitly NOT allowed.

REASON:

* unnecessary complexity
* premature scaling risk

Future migration allowed only after stable MVP.

---

# ADR PRIORITY RULE (IMPORTANT)

If ADRs conflict:

1. Security ADRs win
2. Data integrity ADRs win
3. Domain correctness ADRs win
4. Performance ADRs are last priority
