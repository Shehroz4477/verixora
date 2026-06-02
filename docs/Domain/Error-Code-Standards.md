# 🚨 VERIXORA — ERROR CODE STANDARD SYSTEM (DESIGN)

This becomes the official rulebook for ALL domain errors in your system.

---

## 🧠 1. CORE IDEA

Every business error in VERIXORA must have:

- A unique machine-readable code  
- A clear module identity  
- A consistent structure  

### 👉 Why?

Because later you will have:

- API responses  
- Logs  
- Mobile app messages  
- IoT device errors  
- Monitoring dashboards  

All must speak the same language.

---

## 📌 2. FINAL ERROR CODE FORMAT (VERIXORA STANDARD)


### ✔ Example

- `USER_EMAIL_ALREADY_EXISTS`
- `DEVICE_ALREADY_REGISTERED`
- `AUTH_INVALID_CREDENTIALS`
- `LOCK_ACCESS_DENIED`

---

## 🧱 3. MODULE PREFIX RULES

Each bounded context MUST use its own prefix:

| Module         | Prefix        |
|----------------|--------------|
| Identity       | USER / AUTH  |
| Devices        | DEVICE       |
| SmartLocks     | LOCK         |
| Provisioning   | PROV         |
| Monitoring     | MON          |
| Notifications  | NOTIF        |
| Security       | SEC          |

---

## 🧠 4. NAMING RULES (STRICT)

### ✔ Must follow:

- UPPER_CASE  
- Underscore `_` separation  
- No spaces  
- No special characters  
- Must be self-explanatory  

### ❌ NOT allowed:

- `UserError1`  
- `error_device`  
- `DeviceFailed`  

---

## 🧠 5. ERROR CATEGORIES (IMPORTANT DESIGN LEVEL)

We classify errors into 3 types:

---

### 1. BUSINESS RULE VIOLATION

👉 Domain logic broken

**Examples:**
- `USER_EMAIL_NOT_VERIFIED`
- `DEVICE_ALREADY_LOCKED`

---

### 2. STATE VIOLATION

👉 Object is in wrong state

**Examples:**
- `LOCK_ALREADY_OPEN`
- `DEVICE_NOT_ACTIVE`

---

### 3. AUTHORIZATION / ACCESS

👉 Permission-based domain rules

**Examples:**
- `LOCK_ACCESS_DENIED`
- `USER_NOT_AUTHORIZED`

---

## 🧱 6. HOW IT CONNECTS TO DOMAIN EXCEPTION

Your existing class:

```csharp
throw new DomainException("USER_EMAIL_ALREADY_EXISTS", "User already exists.");