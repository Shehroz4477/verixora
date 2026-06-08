// ====================================================================
// VERIXORA – Identity.Infrastructure / Encryption / EncryptionConverter.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   An EF Core value converter that transparently encrypts/decrypts
//   string properties at the column level.  It uses the application's
//   registered <see cref="IEncryptionService"/> (from BuildingBlocks)
//   to encrypt data before writing to the database and decrypt it
//   after reading.
//
//   WHY A VALUE CONVERTER:
//     - EF Core value converters run automatically on every read/write
//       for the configured property – no manual encryption calls
//       needed in repositories or domain entities.
//     - Keeps encryption logic out of business logic and persistence
//       code.  Simply add `.HasConversion<EncryptionConverter>()` in
//       the entity configuration.
//     - The converter receives the application's IServiceProvider
//       automatically via EF Core's dependency injection, so it can
//       resolve <see cref="IEncryptionService"/> lazily.
//
//   HOW IT WORKS:
//     1. On SAVE: EF Core calls the "convert to provider" lambda,
//        which encrypts the plaintext string into a Base64‑encoded
//        ciphertext.
//     2. On READ: EF Core calls the "convert from provider" lambda,
//        which decrypts the ciphertext back to the original string.
//
//   ENCRYPTION CONTEXT:
//     The converter uses a fixed <see cref="EncryptionContext"/>
//     with TenantId = "SYSTEM", EntityType = "ColumnEncryption",
//     and Purpose = "EFCore".  This ensures all column‑level
//     encryption shares the same AAD binding.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **ValueConverter<TModel, TProvider>**:
//    - Base class from EF Core for defining a bidirectional
//      conversion between a model type (string) and a database
//      provider type (string, but encrypted).
//    - The base constructor takes two lambdas: one for converting
//      TO the provider (encrypt) and one for converting FROM the
//      provider (decrypt).
//
// 2. **IServiceProvider**:
//    - The .NET dependency injection container.  EF Core
//      automatically passes the application's service provider
//      when creating value converters that have a constructor
//      accepting <see cref="IServiceProvider"/>.
//    - The converter stores the service provider and uses it to
//      lazily resolve <see cref="IEncryptionService"/>.
//
// 3. **Expression trees** (lambdas):
//    - EF Core uses expression trees to compile the conversion
//      logic into efficient delegates at model build time.
//
// 4. **sealed** modifier:
//    - Prevents inheritance.  Encryption logic should not be
//      overridden.
// ====================================================================

using BuildingBlocks.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Identity.Infrastructure.Encryption;

/// <summary>
/// EF Core value converter that encrypts/decrypts string properties
/// using the application's <see cref="IEncryptionService"/>.
/// </summary>
public sealed class EncryptionConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Creates the converter with a reference to the service provider.
    /// EF Core calls this constructor automatically when the converter
    /// is registered via <c>.HasConversion&lt;EncryptionConverter&gt;()</c>.
    /// </summary>
    /// <param name="serviceProvider">
    /// The application's DI container, used to resolve
    /// <see cref="IEncryptionService"/>.
    /// </param>
    public EncryptionConverter(IServiceProvider serviceProvider)
        : base(
            // ---- Convert TO provider (encrypt for storage) ----
            plainText => Encrypt(serviceProvider, plainText),
            // ---- Convert FROM provider (decrypt for reading) ----
            cipherText => Decrypt(serviceProvider, cipherText))
    {
    }

    // ----------------------------------------------------------------
    // Static helpers (called by the lambdas)
    // ----------------------------------------------------------------

    /// <summary>
    /// Encrypts a plaintext string using the resolved encryption service.
    /// Returns the original value unchanged if it is null or empty.
    /// </summary>
    private static string Encrypt(IServiceProvider sp, string plainText)
    {
        // If the value is null or empty, pass it through unchanged.
        // This prevents encrypting empty strings, which could cause
        // unnecessary overhead and ciphertext bloat.
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        // Lazily resolve the encryption service from DI.
        // EF Core caches the converter instance, so this lookup
        // happens only once per property, not per row.
        var encryptionService = sp.GetRequiredService<IEncryptionService>();

        // Use a fixed context for all column‑level encryption.
        var context = new EncryptionContext(
            "SYSTEM",                // TenantId – not tenant‑specific
            "ColumnEncryption",      // EntityType – identifies this usage
            "EFCore");               // Purpose – distinguishes from other encryption

        return encryptionService.Encrypt(plainText, context);
    }

    /// <summary>
    /// Decrypts a ciphertext string back to the original plaintext.
    /// Returns the original value unchanged if it is null or empty.
    /// </summary>
    private static string Decrypt(IServiceProvider sp, string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var encryptionService = sp.GetRequiredService<IEncryptionService>();
        var context = new EncryptionContext("SYSTEM", "ColumnEncryption", "EFCore");

        return encryptionService.Decrypt(cipherText, context);
    }
}



//Dry‑run — how the converter is used and what happens at runtime:
// ====================================================================
// 1. REGISTRATION (in UserConfiguration.cs):
// ====================================================================
// builder.Property(x => x.Email)
//     .HasConversion<EncryptionConverter>();
//
// EF Core sees that EncryptionConverter has a constructor accepting
// IServiceProvider.  At model build time, EF Core resolves the
// converter from DI and caches it.

// ====================================================================
// 2. SAVE (INSERT or UPDATE):
// ====================================================================
// The handler calls:
//   user = User.Register("alice@example.com", hash, now);
//   repo.AddAsync(user);
//   await uow.SaveChangesAsync();
//
// EF Core processes the User entity.  For the Email property,
// it calls the converter's "convert to provider" lambda:
//   input:  "alice@example.com"
//   output: "AQphM2YxYjJjNGQ1Z..." (Base64‑encoded AES‑GCM ciphertext)
//
// SQL generated:
//   INSERT INTO identity."Users" ("Email", ...)
//   VALUES ('AQphM2YxYjJjNGQ1Z...', ...);

// ====================================================================
// 3. READ (SELECT):
// ====================================================================
// The handler calls:
//   var user = await repo.GetByIdAsync(id, ct);
//
// EF Core reads the row.  For the Email column, it calls the
// converter's "convert from provider" lambda:
//   input:  "AQphM2YxYjJjNGQ1Z..."
//   output: "alice@example.com"
//
// The handler sees the decrypted email without any manual decryption.

// ====================================================================
// 4. NULL / EMPTY VALUES:
// ====================================================================
// If the Email property is null or empty:
//   input:  "" or null
//   output: "" or null (passed through unchanged)
//
// No encryption/decryption overhead for empty values.
