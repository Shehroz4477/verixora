// ====================================================================
// VERIXORA – Identity.Infrastructure / Persistence / Configurations / UserConfiguration.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IEntityTypeConfiguration{User}"/> to tell
//   EF Core exactly how the <see cref="User"/> aggregate and its
//   child entities map to database tables, columns, indexes, and
//   relationships.  This is the Fluent API approach – all mapping
//   is in code, not in attributes on the domain classes.
//
//   WHY A SEPARATE CONFIGURATION CLASS:
//     - Keeps the domain entity (User.cs) completely free of
//       persistence concerns (no [Table], [Column], or [Key]
//       attributes).
//     - All EF Core mapping is in one place, making it easy to
//       review, test, and modify without touching domain logic.
//     - Automatically discovered by EF Core via
//       `ApplyConfigurationsFromAssembly` in IdentityDbContext.
//
//   WHAT THIS CONFIGURATION DOES:
//     1. Maps User to the "identity"."Users" table.
//     2. Configures Id as the primary key (ULID stored as byte[]).
//     3. Creates a **unique** index on EmailHash for fast lookups
//        and uniqueness enforcement.
//     4. Configures column‑level AES‑256 encryption for the Email
//        column via <see cref="EncryptionConverter"/>.
//     5. Adds a RowVersion column for optimistic concurrency
//        control.
//     6. Maps child entities as owned navigation properties:
//        - Sessions → "identity"."Sessions" table
//        - TrustedDevices → "identity"."TrustedDevices" table
//        - RefreshTokens → "identity"."RefreshTokens" table
//     7. All owned tables use composite primary keys (Id, UserId)
//        and have indexes on the foreign key UserId.
//
//   KEY DESIGN DECISIONS:
//     - **EmailHash** is stored as plain text with a unique index.
//       This allows fast, deterministic lookups and duplicate
//       detection, even though the Email column itself is encrypted
//       with a random IV.
//     - **Owned entities** use composite keys (Id + UserId) because
//       EF Core's OwnsMany requires the parent's key to be part of
//       the child's primary key for proper identity resolution.
//     - **Indexes on UserId** in each owned table ensure that
//       queries like "find all sessions for a user" are fast.
//     - **RowVersion** is a database‑managed concurrency token
//       that prevents lost updates without application locks.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing **IEntityTypeConfiguration<T>**:
//    - A generic interface from EF Core.  The type parameter `T` is
//      the entity to configure (here, `User`).
//
// 2. **EntityTypeBuilder<T>**:
//    - A fluent API object for configuring the table, columns,
//      keys, indexes, and relationships for a specific entity.
//
// 3. **ToTable(string, string)**:
//    - Specifies the database table name and schema.
//
// 4. **HasKey()**:
//    - Configures the primary key column.
//
// 5. **Property()**:
//    - Selects a single property for column‑level configuration.
//    - Chain methods like `.HasColumnName()`, `.IsRequired()`,
//      `.HasMaxLength()`, `.HasConversion()` to fine‑tune the
//      column mapping.
//
// 6. **HasConversion<TConverter>()**:
//    - Applies a value converter.  For ULIDs, we convert to/from
//      a byte array.  For Email and PhoneNumber, we use the
//      `EncryptionConverter` to encrypt at rest.
//
// 7. **HasIndex()**:
//    - Creates a database index.  `.IsUnique()` enforces uniqueness.
//
// 8. **IsRowVersion()**:
//    - Configures a property as a concurrency token that the
//      database automatically updates on every INSERT or UPDATE.
//
// 9. **OwnsMany()**:
//    - Configures a one‑to‑many relationship where the child entity
//      is "owned" by the parent.  Owned entities are always loaded
//      with the parent and are deleted when the parent is deleted.
//
// 10. **sealed** modifier:
//     - Prevents inheritance of the configuration.
// ====================================================================

using Identity.Domain.Entities;
using BuildingBlocks.Infrastructure.Encryption;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using static System.Net.Mime.MediaTypeNames;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the <see cref="User"/> aggregate.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ------------------------------------------------------------
        // Table mapping
        // ------------------------------------------------------------
        builder.ToTable("Users", "identity");

        // ------------------------------------------------------------
        // Primary key
        // ------------------------------------------------------------
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .IsRequired()
            .HasConversion(
                ulid => ulid.ToByteArray(),       // ULID → byte[] for storage
                bytes => Ulid.FromBytes(bytes)     // byte[] → ULID for reading
            );

        // ------------------------------------------------------------
        // Concurrency token (RowVersion)
        // ------------------------------------------------------------
        // The database automatically generates a new value on every
        // INSERT or UPDATE.  EF Core includes the original value in
        // the WHERE clause of UPDATE/DELETE statements and throws
        // DbUpdateConcurrencyException if it has changed.
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .HasColumnName("RowVersion");

        // ------------------------------------------------------------
        // Email – encrypted at column level (ADR‑017)
        // ------------------------------------------------------------
        builder.Property(x => x.Email)
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(320)                     // max email length per RFC 5321
            .HasConversion<EncryptionConverter>(); // AES‑256 encryption

        // Optional non‑unique index on Email for direct scans.
        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_Users_Email");

        // ------------------------------------------------------------
        // EmailHash – plain text, UNIQUE, for fast lookups
        // ------------------------------------------------------------
        builder.Property(x => x.EmailHash)
            .HasColumnName("EmailHash")
            .IsRequired()
            .HasMaxLength(44);   // SHA‑256 → 32 bytes → 44 base64 chars

        builder.HasIndex(x => x.EmailHash)
            .IsUnique()
            .HasDatabaseName("IX_Users_EmailHash");

        // ------------------------------------------------------------
        // PasswordHash – stored as plain text (already Argon2id hashed)
        // ------------------------------------------------------------
        builder.Property(x => x.PasswordHash)
            .HasColumnName("PasswordHash")
            .IsRequired()
            .HasMaxLength(1024);   // Argon2id hashes can be long

        // ------------------------------------------------------------
        // EmailVerified – a simple boolean
        // ------------------------------------------------------------
        builder.Property(x => x.EmailVerified)
            .HasColumnName("EmailVerified")
            .IsRequired()
            .HasDefaultValue(false);

        // ------------------------------------------------------------
        // PhoneNumber – optional, encrypted
        // ------------------------------------------------------------
        builder.Property(x => x.PhoneNumber)
            .HasColumnName("PhoneNumber")
            .HasMaxLength(30)
            .HasConversion<EncryptionConverter>();

        // ------------------------------------------------------------
        // CreatedAt – UTC timestamp
        // ------------------------------------------------------------
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // ------------------------------------------------------------
        // Sessions – owned collection, mapped to its own table
        // ------------------------------------------------------------
        builder.OwnsMany(
            x => x.Sessions,
            sessions =>
            {
                sessions.ToTable("Sessions", "identity");

                // Composite primary key: Id + UserId
                sessions.HasKey(s => new { s.Id, s.UserId });
                sessions.WithOwner().HasForeignKey(s => s.UserId);

                // Index on UserId for fast queries
                sessions.HasIndex(s => s.UserId)
                    .HasDatabaseName("IX_Sessions_UserId");

                sessions.Property(s => s.Id)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                sessions.Property(s => s.UserId)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                sessions.Property(s => s.DeviceFingerprint)
                    .HasMaxLength(256)
                    .IsRequired();

                sessions.Property(s => s.IpAddress)
                    .HasMaxLength(45)   // IPv6 max length
                    .IsRequired();

                sessions.Property(s => s.UserAgent)
                    .HasMaxLength(512);

                sessions.Property(s => s.IsTrusted)
                    .IsRequired();

                sessions.Property(s => s.CreatedAt)
                    .IsRequired();

                sessions.Property(s => s.ExpiresAt)
                    .IsRequired();

                sessions.Property(s => s.RevokedAt)
                    .IsRequired(false);
            });

        // ------------------------------------------------------------
        // TrustedDevices – owned collection
        // ------------------------------------------------------------
        builder.OwnsMany(
            x => x.TrustedDevices,
            devices =>
            {
                devices.ToTable("TrustedDevices", "identity");
                devices.HasKey(d => new { d.Id, d.UserId });
                devices.WithOwner().HasForeignKey(d => d.UserId);
                devices.HasIndex(d => d.UserId)
                    .HasDatabaseName("IX_TrustedDevices_UserId");

                devices.Property(d => d.Id)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                devices.Property(d => d.UserId)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                devices.Property(d => d.DeviceFingerprint)
                    .HasMaxLength(256)
                    .IsRequired();

                devices.Property(d => d.AddedAt)
                    .IsRequired();
            });

        // ------------------------------------------------------------
        // RefreshTokens – owned collection
        // ------------------------------------------------------------
        builder.OwnsMany(
            x => x.RefreshTokens,
            tokens =>
            {
                tokens.ToTable("RefreshTokens", "identity");
                tokens.HasKey(t => new { t.Id, t.UserId });
                tokens.WithOwner().HasForeignKey(t => t.UserId);
                tokens.HasIndex(t => t.UserId)
                    .HasDatabaseName("IX_RefreshTokens_UserId");

                tokens.Property(t => t.Id)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                tokens.Property(t => t.UserId)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                tokens.Property(t => t.TokenHash)
                    .HasMaxLength(128)
                    .IsRequired();

                tokens.Property(t => t.ExpiresAt)
                    .IsRequired();

                tokens.Property(t => t.RevokedAt)
                    .IsRequired(false);
            });
    }
}





//Dry‑run — what SQL tables and columns this configuration generates:
//-- =========================================================================
//-- TABLE: identity."Users"
//-- =========================================================================
//CREATE TABLE identity."Users" (
//    "Id"            UUID PRIMARY KEY,
//    "Email"         TEXT NOT NULL,              -- AES‑256 encrypted
//    "EmailHash"     VARCHAR(44) NOT NULL,       -- plain SHA‑256 hash
//    "PasswordHash"  VARCHAR(1024) NOT NULL,
//    "EmailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
//    "PhoneNumber"   TEXT,                       -- AES‑256 encrypted, nullable
//    "CreatedAt"     TIMESTAMPTZ NOT NULL,
//    "RowVersion"    BYTEA NOT NULL              -- concurrency token
//);

//CREATE UNIQUE INDEX "IX_Users_EmailHash" ON identity."Users" ("EmailHash");
//CREATE INDEX "IX_Users_Email" ON identity."Users" ("Email");

//-- =========================================================================
//-- TABLE: identity."Sessions"   (owned by User)
//-- =========================================================================
//CREATE TABLE identity."Sessions" (
//    "Id"                UUID NOT NULL,
//    "UserId"            UUID NOT NULL,
//    "DeviceFingerprint" VARCHAR(256) NOT NULL,
//    "IpAddress"         VARCHAR(45) NOT NULL,
//    "UserAgent"         VARCHAR(512),
//    "IsTrusted"         BOOLEAN NOT NULL,
//    "CreatedAt"         TIMESTAMPTZ NOT NULL,
//    "ExpiresAt"         TIMESTAMPTZ NOT NULL,
//    "RevokedAt"         TIMESTAMPTZ,
//    PRIMARY KEY("Id", "UserId"),
//    FOREIGN KEY("UserId") REFERENCES identity."Users"("Id") ON DELETE CASCADE
//);

//CREATE INDEX "IX_Sessions_UserId" ON identity."Sessions" ("UserId");

//-- =========================================================================
//-- TABLE: identity."TrustedDevices"   (owned by User)
//-- =========================================================================
//CREATE TABLE identity."TrustedDevices" (
//    "Id"                UUID NOT NULL,
//    "UserId"            UUID NOT NULL,
//    "DeviceFingerprint" VARCHAR(256) NOT NULL,
//    "AddedAt"           TIMESTAMPTZ NOT NULL,
//    PRIMARY KEY("Id", "UserId"),
//    FOREIGN KEY("UserId") REFERENCES identity."Users"("Id") ON DELETE CASCADE
//);

//CREATE INDEX "IX_TrustedDevices_UserId" ON identity."TrustedDevices" ("UserId");

//-- =========================================================================
//-- TABLE: identity."RefreshTokens"   (owned by User)
//-- =========================================================================
//CREATE TABLE identity."RefreshTokens" (
//    "Id"         UUID NOT NULL,
//    "UserId"     UUID NOT NULL,
//    "TokenHash"  VARCHAR(128) NOT NULL,
//    "ExpiresAt"  TIMESTAMPTZ NOT NULL,
//    "RevokedAt"  TIMESTAMPTZ,
//    PRIMARY KEY("Id", "UserId"),
//    FOREIGN KEY("UserId") REFERENCES identity."Users"("Id") ON DELETE CASCADE
//);

//CREATE INDEX "IX_RefreshTokens_UserId" ON identity."RefreshTokens" ("UserId");
