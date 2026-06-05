// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / IAadProvider.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   This interface defines the contract for constructing the
//   **Additional Authenticated Data** (AAD) used by AES‑GCM.
//   AAD is unencrypted data that is cryptographically bound to the
//   ciphertext: if the AAD changes even by a single byte, decryption
//   fails.  We use AAD to tie every ciphertext to a specific tenant,
//   entity type, purpose, and ciphertext version – preventing
//   cross‑tenant replay, entity swapping, and format downgrades.
//
//   By abstracting AAD construction behind an interface, the
//   encryption service is decoupled from *how* the binding is
//   computed.  The current implementation uses length‑prefixed
//   binary encoding + SHA256, but a future version could use a
//   different hashing strategy without touching the encryption
//   logic.
//
//   This follows the **Dependency Inversion Principle**: the
//   encryption service depends on this abstraction, not on a
//   concrete AAD builder.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide the declared method.
//    - Enables polymorphism: different AAD strategies can be
//      swapped without changing the encryption service.
//
// 2. **public** access modifier:
//    - The interface and its member are accessible from any project
//      that references this assembly.
//
// 3. **ReadOnlyMemory<byte>** return type:
//    - Represents a **read‑only view** over a region of memory.
//      The consumer can read the bytes but cannot modify the
//      underlying buffer through this view.  It does NOT guarantee
//      that the underlying memory is immutable – that is the
//      responsibility of the implementation.
//
// 4. **EncryptionContext** parameter:
//    - An immutable `record` (already `sealed record`) that carries
//      the binding metadata (TenantId, EntityType, Purpose).  The
//      type itself enforces immutability, so no extra documentation
//      is needed.
//
// 5. **byte version** parameter:
//    - The ciphertext format version byte (currently 0x01).  Including
//      the version in the AAD prevents an attacker from taking a
//      valid ciphertext and re‑wrapping it with a different version
//      header.
//
// 6. **namespace** declaration:
//    - Organises types into logical groups.  The namespace
//      `BuildingBlocks.Infrastructure.Encryption` indicates this
//      is part of the shared infrastructure, encryption component.
// ====================================================================

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// Builds the Additional Authenticated Data (AAD) for AES‑GCM
/// encryption.  The AAD binds a ciphertext to a specific tenant,
/// entity type, and purpose, preventing cross‑context replay.
/// </summary>
public interface IAadProvider
{
    /// <summary>
    /// Creates a deterministic AAD byte sequence for the given
    /// encryption context and ciphertext format version.
    /// </summary>
    /// <param name="context">
    /// The binding context (tenant ID, entity type, purpose).
    /// Must not be null.  This is an immutable record; the same
    /// logical context must be used for both encryption and
    /// decryption.
    /// </param>
    /// <param name="version">
    /// The ciphertext format version byte (currently 0x01).
    /// Including this prevents downgrade attacks.
    /// </param>
    /// <returns>
    /// A read‑only view of the AAD bytes.  The same
    /// <paramref name="context"/> and <paramref name="version"/>
    /// will always produce the same byte sequence.
    /// </returns>
    ReadOnlyMemory<byte> BuildAad(EncryptionContext context, byte version);
}



// Assume an IAadProvider instance is injected via the constructor:
//// private readonly IAadProvider _aadProvider;

//// ================================================================
//// Encryption path
//// ================================================================
//var context = new EncryptionContext("tenant-1", "User", "Email");
//    byte version = 0x01;

//    // Build the AAD – this binds the ciphertext to tenant‑1's User Email field.
//    ReadOnlyMemory<byte> aad = _aadProvider.BuildAad(context, version);

//    // The AAD is read‑only; we can pass its Span to AesGcm.
//    // aadMemory.Span gives a ReadOnlySpan<byte> for the crypto call.
//    // Example: aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aad.Span);

//    // ================================================================
//    // Decryption path
//    // ================================================================
//    // The encryption service extracts the version from the ciphertext header
//    // and recreates the exact same context that was used during encryption.
//    ReadOnlyMemory<byte> aad = _aadProvider.BuildAad(context, version);

//// AesGcm.Decrypt will verify the tag using this AAD.  If the context
//// or version has been tampered with, decryption throws.
