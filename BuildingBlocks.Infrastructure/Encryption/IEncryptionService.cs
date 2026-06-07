// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / IEncryptionService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   This interface defines the **contract** for the AES‑256‑GCM
//   encryption service.  It is the public API used by the rest of the
//   application to encrypt and decrypt data.  The implementation
//   orchestrates three components:
//     1. IKeyProvider   – provides the correct encryption key
//     2. IAadProvider   – builds the AAD (tenant/entity/purpose binding)
//     3. AES‑GCM        – performs the actual authenticated encryption
//
//   This follows the **Facade Pattern**: a single interface hides the
//   complexity of key management, AAD construction, binary packet
//   formatting, and AES‑GCM operations.  The consumer only needs to
//   provide the plaintext/ciphertext and the encryption context.
//
//   DEPENDENCY INVERSION:
//     Application code depends only on this interface, not on any
//     concrete implementation.  This allows the entire encryption
//     pipeline to be swapped (e.g., for a hardware‑backed version)
//     without changing any application logic.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide the declared methods.
//    - Enables polymorphism and dependency inversion.
//
// 2. **public** access modifier:
//    - The interface and its members are accessible from any project
//      that references this assembly.
//
// 3. **string** return type:
//    - Encrypt returns a Base64‑encoded string (safe for storage/transport).
//    - Decrypt accepts a Base64‑encoded string and returns the original plaintext.
//
// 4. **EncryptionContext** parameter:
//    - An immutable record carrying TenantId, EntityType, and Purpose.
//      This binds the ciphertext to a specific tenant/entity/purpose.
//
// 5. **namespace** declaration:
//    - Keeps the interface organised within the encryption infrastructure.
// ====================================================================

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// Provides AES‑256‑GCM authenticated encryption with tenant‑bound AAD.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a UTF‑8 plaintext string and returns a Base64‑encoded
    /// ciphertext packet.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <param name="context">
    /// The encryption context (tenant ID, entity type, purpose).
    /// The same context must be used for decryption.
    /// </param>
    /// <returns>A Base64‑encoded binary ciphertext packet.</returns>
    string Encrypt(string plainText, EncryptionContext context);

    /// <summary>
    /// Decrypts a Base64‑encoded ciphertext packet back to the original
    /// UTF‑8 string.
    /// </summary>
    /// <param name="cipherText">The Base64‑encoded ciphertext.</param>
    /// <param name="context">
    /// The exact same encryption context that was used during encryption.
    /// </param>
    /// <returns>The original plaintext string.</returns>
    string Decrypt(string cipherText, EncryptionContext context);

    /// <summary>
    /// Encrypts the contents of a file and returns a Base64‑encoded
    /// ciphertext string.  The file is streamed so that large files
    /// do not need to be loaded entirely into memory.
    /// </summary>
    /// <param name="filePath">The path to the file to encrypt.</param>
    /// <param name="context">The encryption context for AAD binding.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Base64‑encoded ciphertext string.</returns>
    Task<string> EncryptAsync(string filePath, EncryptionContext context, CancellationToken cancellationToken);
}





//// Assume an IEncryptionService instance is injected via the constructor:
//// private readonly IEncryptionService _encryptionService;

//// ================================================================
//// ENCRYPTION
//// ================================================================
//var context = new EncryptionContext("tenant-1", "User", "Email");
//    string ciphertext = _encryptionService.Encrypt("user@example.com", context);
//    // ciphertext is a Base64 string like "AQphM2YxYjJjNGQ1Z... (long string)".
//    // Store it in the database.

//    // ================================================================
//    // DECRYPTION
//    // ================================================================
//    string plaintext = _encryptionService.Decrypt(ciphertext, context);
//// plaintext is "user@example.com".
//// If the context or ciphertext is tampered with, a CryptographicException is thrown.
