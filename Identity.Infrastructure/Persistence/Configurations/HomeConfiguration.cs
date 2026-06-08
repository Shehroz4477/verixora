// ====================================================================
// VERIXORA – Identity.Infrastructure / Persistence / Configurations / HomeConfiguration.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IEntityTypeConfiguration{Home}"/> to tell
//   EF Core exactly how the <see cref="Home"/> aggregate and its
//   child <see cref="HomeMembership"/> entities map to database
//   tables, columns, indexes, and relationships.
//
//   WHY A SEPARATE CONFIGURATION CLASS:
//     - Keeps the domain entity (Home.cs) completely free of
//       persistence concerns (no [Table], [Column], or [Key]
//       attributes).
//     - All EF Core mapping is in one place, making it easy to
//       review, test, and modify without touching domain logic.
//     - Automatically discovered by EF Core via
//       `ApplyConfigurationsFromAssembly` in IdentityDbContext.
//
//   WHAT THIS CONFIGURATION DOES:
//     1. Maps Home to the "identity"."Homes" table.
//     2. Configures Id as the primary key (ULID stored as byte[]).
//     3. Configures the Name and MaxDevices columns.
//     4. Maps the Members collection as an owned navigation:
//        - HomeMemberships → "identity"."HomeMemberships" table.
//        - Uses the membership's own Id as the primary key.
//        - Cascade delete is automatic for owned types.
//        - Converts the HomeRole enumeration to its integer Id.
//
//   KEY DESIGN DECISIONS:
//     - The `Members` collection is configured as **owned** because
//       a HomeMembership has no meaning without its parent Home.
//       Deleting a Home automatically removes all its memberships
//       (cascade delete is implicit for owned types).
//     - The `Role` property is stored as an integer (the `Id` from
//       the `HomeRole` enumeration) rather than a string, making
//       queries and storage more efficient.
//     - No `OnDelete` call is needed — EF Core automatically applies
//       cascade delete to owned types.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing **IEntityTypeConfiguration<T>**:
//    - A generic interface from EF Core.  The type parameter `T` is
//      the entity to configure (here, `Home`).
//    - The single method `Configure(EntityTypeBuilder<T> builder)`
//      is called by EF Core during model building.
//
// 2. **EntityTypeBuilder<T>**:
//    - A fluent API object that provides methods for configuring
//      the table, columns, keys, indexes, and relationships for
//      a specific entity.
//
// 3. **ToTable(string, string)**:
//    - Specifies the database table name and schema.
//
// 4. **HasKey()**:
//    - Configures the primary key.  `x => x.Id` selects the `Id`
//      property as the primary key column.
//
// 5. **Property()**:
//    - Selects a single property for column‑level configuration.
//    - Chain methods like `.HasColumnName()`, `.IsRequired()`,
//      `.HasMaxLength()`, `.HasConversion()` to fine‑tune the
//      column mapping.
//
// 6. **HasConversion()**:
//    - Applies a value converter.  For ULIDs, we convert to/from
//      a byte array.  For HomeRole, we convert to its integer Id.
//
// 7. **OwnsMany()**:
//    - Configures a one‑to‑many relationship where the child entity
//      is "owned" by the parent.  Owned entities are always loaded
//      with the parent and are deleted when the parent is deleted.
//      Cascade delete is automatic.
//
// 8. **HasForeignKey()**:
//    - Specifies the foreign key column that links the child
//      entity back to its parent.
//
// 9. **sealed** modifier:
//    - Prevents inheritance.  Entity configurations should not
//       be overridden.
// ====================================================================

using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the <see cref="Home"/> aggregate.
/// </summary>
public sealed class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        // ------------------------------------------------------------
        // Table mapping
        // ------------------------------------------------------------
        builder.ToTable("Homes", "identity");

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
        // Name – required, max 200 characters
        // ------------------------------------------------------------
        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        // ------------------------------------------------------------
        // MaxDevices – integer, default 20
        // ------------------------------------------------------------
        builder.Property(x => x.MaxDevices)
            .HasColumnName("MaxDevices")
            .IsRequired()
            .HasDefaultValue(20);

        // ------------------------------------------------------------
        // CreatedAt – UTC timestamp
        // ------------------------------------------------------------
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // ------------------------------------------------------------
        // Members (HomeMembership) – owned collection
        // ------------------------------------------------------------
        builder.OwnsMany(
            x => x.Members,
            (OwnedNavigationBuilder<Home, HomeMembership> memberships) =>
            {
                memberships.ToTable("HomeMemberships", "identity");

                // Primary key – just the membership's own Id
                memberships.HasKey(m => m.Id);

                // Foreign key back to the parent Home
                memberships.WithOwner().HasForeignKey(m => m.HomeId);
                // Cascade delete is AUTOMATIC for owned types – no OnDelete() call needed.

                // Id (ULID → byte[])
                memberships.Property(m => m.Id)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                // HomeId (foreign key)
                memberships.Property(m => m.HomeId)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                // UserId (reference to a User in the Users table)
                memberships.Property(m => m.UserId)
                    .HasConversion(
                        ulid => ulid.ToByteArray(),
                        bytes => Ulid.FromBytes(bytes));

                // Role – HomeRole enumeration, stored as integer
                memberships.Property(m => m.Role)
                    .HasColumnName("Role")
                    .IsRequired()
                    .HasConversion(
                        role => role.Id,                          // HomeRole → int
                        id => HomeRole.FromId<HomeRole>(id)!      // int → HomeRole
                    );

                // JoinedAt – UTC timestamp
                memberships.Property(m => m.JoinedAt)
                    .HasColumnName("JoinedAt")
                    .IsRequired();
            });
    }
}
