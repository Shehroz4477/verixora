// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / IKeyProvider.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   This interface defines the **contract** for providing AES‑256
//   encryption keys.  It is the **abstraction** that the encryption
//   service depends on, following the **Dependency Inversion
//   Principle** (the "D" in SOLID).  High‑level encryption logic
//   depends only on this interface, not on any concrete key store.
//   This makes it possible to swap key storage (in‑memory, Azure
//   Key Vault, AWS KMS, HSM) without modifying the encryption
//   service itself.
//
//   In VERIXORA, the concrete implementation (`KeyProvider`) holds a
//   dictionary of keys loaded from configuration (which itself comes
//   from a secrets manager).  The encryption service always uses
//   `CurrentKey` for new encryptions and calls `TryGetKey` to find
//   the right key for decryption.
//
//   Key rotation is supported because multiple keys can be stored;
//   old data carries the key ID in its ciphertext header, and
//   `TryGetKey` resolves it.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide the declared members.
//    - Enables polymorphism: different implementations can be
//      swapped at runtime or in unit tests (e.g., real provider vs.
//      mock provider).
//
// 2. **public** access modifier:
//    - The interface and its members are accessible from any project
//      that references this assembly.
//
// 3. **ReadOnlyMemory<byte>** (return type / out parameter):
//    - Represents a read‑only view of a contiguous region of memory.
//    - Unlike `byte[]`, the underlying bytes cannot be modified
//      **through** a `ReadOnlyMemory<byte>`.  This prevents accidental
//      mutation of cryptographic key material by the consumer.
//    - **Important**: It does NOT guarantee that the underlying
//      memory is immutable; the provider implementation could still
//      modify the original array internally.  It is a view, not a
//      secure enclave.
//    - It is a **value type** (struct) and can be used in
//      interfaces and async methods (unlike `Span<byte>`, which is
//      a ref struct and cannot be a generic type argument or used
//      in async methods).
//
// 4. **IReadOnlyCollection<string>**:
//    - An interface that represents a read‑only collection of
//      elements.  It provides a `Count` property and an enumerator.
//    - Used here to expose a snapshot of all known key IDs.
//
// 5. **out** parameter modifier:
//    - Allows a method to return more than one value.  The caller
//      provides a variable, and the method assigns a value to it.
//    - In `TryGetKey`, we return a boolean (success/failure) and,
//      when successful, the actual key bytes via `out`.
//
// 6. **bool TryGetKey(...)** pattern:
//    - Known as the "Try‑Parse" pattern.  It avoids throwing
//      exceptions for expected failures (like a rotated‑out key).
//    - This is more efficient and clearer than catching
//      `KeyNotFoundException`.
//
// 7. **Read‑only property** (`CurrentKey { get; }`):
//    - A property with only a getter is immutable from the outside.
//    - The implementing class can still set the value internally
//      (e.g., in the constructor).
//
// 8. **namespace** declaration:
//    - Organises types into logical groups.  The namespace
//      `BuildingBlocks.Infrastructure.Encryption` clearly indicates
//      that this is part of the shared infrastructure layer,
//      specifically the encryption sub‑component.
// ====================================================================

using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Infrastructure.Encryption;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// Provides access to AES‑256 encryption keys by their unique
/// identifier.  This abstraction allows key storage to be swapped
/// without changing the encryption logic (memory, vault, HSM,
/// cloud provider).
/// </summary>
/// <remarks>
/// <para>
/// All implementations MUST be <b>thread‑safe</b> and safe for
/// concurrent reads during key rotation.  The returned
/// <see cref="ReadOnlyMemory{T}"/> views are read‑only for the
/// consumer, but the provider must take care not to mutate the
/// underlying byte arrays while they are in use.
/// </para>
/// <para>
/// This interface does NOT provide key isolation or secure memory
/// protection.  Key material is stored in managed memory and is not
/// encrypted at rest within the process.  For hardware‑backed keys,
/// implement a custom provider that interfaces with an HSM or KMS.
/// </para>
/// </remarks>
public interface IKeyProvider
{
    /// <summary>
    /// Gets the currently active AES‑256 encryption key, used for
    /// all NEW encryption operations.  This key is guaranteed to
    /// be a valid 32‑byte AES‑256 key.
    /// </summary>
    ReadOnlyMemory<byte> CurrentKey { get; }

    /// <summary>
    /// Attempts to retrieve the AES‑256 key material for the
    /// specified key ID.  Used during DECRYPTION to find the key
    /// that was originally used to encrypt the data.
    /// </summary>
    /// <param name="keyIdHex">
    /// The normalised 32‑character lowercase hex string that
    /// identifies the key.  This is a GUID in "N" format (e.g.,
    /// <c>"a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6"</c>).
    /// The caller is responsible for providing the identifier
    /// in this canonical form.
    /// </param>
    /// <param name="key">
    /// When this method returns <c>true</c>, this parameter will
    /// contain a read‑only view of the 32‑byte AES‑256 key.
    /// When it returns <c>false</c>, the value is undefined.
    /// </param>
    /// <returns>
    /// <c>true</c> if a key with the given ID was found;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryGetKey(string keyIdHex, out ReadOnlyMemory<byte> key);

    /// <summary>
    /// Returns a snapshot of all key identifiers currently
    /// available in the system.  All returned IDs are in 32‑character
    /// lowercase hex (Guid "N" format).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned collection is a point‑in‑time copy; subsequent
    /// changes to the provider's internal state will not affect it.
    /// This method may allocate a new collection on every call.
    /// </para>
    /// <para>
    /// The order of the returned keys is <b>not guaranteed</b> and
    /// may differ between calls.
    /// </para>
    /// </remarks>
    IReadOnlyCollection<string> GetKeyIds();
}


//// Assume an IKeyProvider instance is injected via the constructor:
//// private readonly IKeyProvider _keyProvider;

//// ================================================================
//// SCENARIO 1: Encrypting data (uses the CURRENT key)
//// ================================================================
//byte[] plaintext = Encoding.UTF8.GetBytes("user@example.com");
//    ReadOnlyMemory<byte> keyMemory = _keyProvider.CurrentKey;
//    // keyMemory.Span gives a ReadOnlySpan<byte> for use with AesGcm.
//    // IMPORTANT: Span is stack‑only; do not capture or store it.

//    // ================================================================
//    // SCENARIO 2: Decrypting old data (may use ANY key)
//    // ================================================================
//    string keyIdHex = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6";
//if (_keyProvider.TryGetKey(keyIdHex, out ReadOnlyMemory<byte> decryptionKey))
//{
//    // decryptionKey is a read‑only view of the correct key.
//}
//else
//{
//    throw new CryptographicException($"Unknown key ID: {keyIdHex}");
//}

//// ================================================================
//// SCENARIO 3: Key rotation health check
//// ================================================================
//IReadOnlyCollection<string> allKeys = _keyProvider.GetKeyIds();
//Console.WriteLine($"Currently managing {allKeys.Count} keys.");
//foreach (string id in allKeys)
//{
//    // do something with each key ID, e.g., verify it exists
//}
