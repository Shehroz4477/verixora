// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionConverter.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   An EF Core value converter that transparently encrypts/decrypts
//   string properties at the column level.  It uses the application's
//   registered <see cref="IEncryptionService"/> (which must be
//   assigned to the static <see cref="EncryptionService"/> property
//   at application startup).
//
//   WHY A PARAMETERLESS CONSTRUCTOR:
//     The original design relied on EF Core injecting an
//     IServiceProvider into the converter's constructor.  That works
//     at runtime but fails during `dotnet ef migrations add` because
//     there is no DI container available at design time.
//     The solution is a parameterless constructor combined with a
//     static property that holds the encryption service.  The static
//     property is set once in Program.cs after the DI container is
//     built, and the converter uses it for all encryption/decryption.
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
// 2. **Static property** (`EncryptionService`):
//    - A single, application‑wide reference to the encryption
//      service.  Set at startup from the DI container.
//    - Avoids the need for constructor injection, which EF Core
//      cannot provide during design‑time operations.
//
// 3. **Parameterless constructor**:
//    - Required by EF Core when the converter is used in
//      `HasConversion<EncryptionConverter>()`.
//    - Calls the base constructor with lambdas that reference the
//      static `EncryptionService` property.
//
// 4. **sealed** modifier:
//    - Prevents inheritance.  Encryption logic should not be
//      overridden.
// ====================================================================

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// EF Core value converter that encrypts/decrypts string properties
/// using the application's registered <see cref="IEncryptionService"/>.
/// </summary>
public sealed class EncryptionConverter : ValueConverter<string, string>
{
    // ----------------------------------------------------------------
    // Static service reference – must be set at application startup.
    // ----------------------------------------------------------------
    /// <summary>
    /// Set this at startup (e.g., in Program.cs) to the resolved
    /// <see cref="IEncryptionService"/> from the DI container.
    /// </summary>
    public static IEncryptionService? EncryptionService { get; set; }

    // ----------------------------------------------------------------
    // Parameterless constructor (required by EF Core)
    // ----------------------------------------------------------------
    /// <summary>
    /// Creates the converter.  EF Core calls this constructor
    /// automatically when the converter is registered via
    /// <c>.HasConversion&lt;EncryptionConverter&gt;()</c>.
    /// </summary>
    public EncryptionConverter()
        : base(
            // ---- Convert TO provider (encrypt for storage) ----
            plainText => Encrypt(plainText),
            // ---- Convert FROM provider (decrypt for reading) ----
            cipherText => Decrypt(cipherText))
    {
    }

    // ----------------------------------------------------------------
    // Static helpers (called by the lambdas)
    // ----------------------------------------------------------------

    /// <summary>
    /// Encrypts a plaintext string using the static encryption service.
    /// Returns the original value unchanged if it is null or empty,
    /// or if the static service has not been initialised.
    /// </summary>
    private static string Encrypt(string plainText)
    {
        // If the value is null or empty, pass it through unchanged.
        // This prevents encrypting empty strings, which could cause
        // unnecessary overhead and ciphertext bloat.
        if (string.IsNullOrEmpty(plainText) || EncryptionService is null)
            return plainText;

        // Use a fixed context for all column‑level encryption.
        var context = new EncryptionContext(
            "SYSTEM",                // TenantId – not tenant‑specific
            "ColumnEncryption",      // EntityType – identifies this usage
            "EFCore");               // Purpose – distinguishes from other encryption

        return EncryptionService.Encrypt(plainText, context);
    }

    /// <summary>
    /// Decrypts a ciphertext string back to the original plaintext.
    /// Returns the original value unchanged if it is null or empty,
    /// or if the static service has not been initialised.
    /// </summary>
    private static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || EncryptionService is null)
            return cipherText;

        var context = new EncryptionContext("SYSTEM", "ColumnEncryption", "EFCore");

        return EncryptionService.Decrypt(cipherText, context);
    }
}

// Dry‑run — how the converter is used and what happens at runtime:
// ====================================================================
// 1. STARTUP (in Program.cs):
//    BuildingBlocks.Infrastructure.Encryption.EncryptionConverter.EncryptionService
//        = app.Services.GetRequiredService<IEncryptionService>();
//
//    The static EncryptionService field is now set to the AES‑256‑GCM
//    implementation registered in DI.
//
// ====================================================================
// 2. REGISTRATION (in UserConfiguration.cs):
//    builder.Property(x => x.Email)
//        .HasConversion<EncryptionConverter>();
//
//    EF Core sees that EncryptionConverter has a parameterless
//    constructor.  It creates an instance and caches it for all
//    Email conversions.
//
// ====================================================================
// 3. SAVE (INSERT or UPDATE):
//    The handler calls:
//      user = User.Register("alice@example.com", hash, now);
//      repo.AddAsync(user);
//      await uow.SaveChangesAsync();
//
//    EF Core processes the User entity.  For the Email property,
//    it calls the converter's "convert to provider" lambda:
//      input:  "alice@example.com"
//      output: "AQphM2YxYjJjNGQ1Z..." (Base64‑encoded AES‑GCM ciphertext)
//
//    SQL generated:
//      INSERT INTO identity."Users" ("Email", ...)
//      VALUES ('AQphM2YxYjJjNGQ1Z...', ...);
//
// ====================================================================
// 4. READ (SELECT):
//    The handler calls:
//      var user = await repo.GetByIdAsync(id, ct);
//
//    EF Core reads the row.  For the Email column, it calls the
//    converter's "convert from provider" lambda:
//      input:  "AQphM2YxYjJjNGQ1Z..."
//      output: "alice@example.com"
//
//    The handler sees the decrypted email without any manual decryption.
//
// ====================================================================
// 5. NULL / EMPTY VALUES:
//    If the Email property is null or empty:
//      input:  "" or null
//      output: "" or null (passed through unchanged)
//
//    No encryption/decryption overhead for empty values.
