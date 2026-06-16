// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / KeyProvider.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IKeyProvider"/> by storing AES‑256 keys in a
//   plain dictionary, loaded once at startup from configuration.  The
//   provider is **deeply immutable** after construction: every key
//   array is defensively copied, the dictionary is never modified, and
//   no reference to internal arrays is ever leaked.
//
//   This class is designed to be registered as a **Singleton**.
//   Because it is fully immutable after construction, all reads are
//   inherently thread‑safe with no locks required.
//
//   SIMPLICITY NOTE:
//     We deliberately use plain arrays and a dictionary – no
//     ImmutableArray or other fancy collections.  The immutability
//     guarantee comes from the ownership model: the arrays are created
//     and copied once, then never changed or shared.  This keeps the
//     code straightforward and avoids unnecessary abstraction.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** (reference type) implementing an interface:
//    - The `: IKeyProvider` part tells the compiler that this class
//      must provide all members declared in the interface.  The
//      compiler will error if we forget any method or property.
//
// 2. **private readonly fields**:
//    - `_currentKey`, `_currentKeyId`, `_keys`, `_keyIds` are all
//      `readonly`.  In C#, `readonly` means the variable can only be
//      assigned during declaration or in the constructor.  After
//      construction, it cannot be changed – this is how we guarantee
//      immutability.
//
// 3. **Dictionary<string, byte[]>** with **StringComparer.Ordinal**:
//    - A key‑value collection that maps a string (the hex key ID)
//      to a byte array (the raw AES key).  `StringComparer.Ordinal`
//      provides case‑sensitive, culture‑insensitive lookups, which
//      are fast and predictable for machine‑generated identifiers.
//
// 4. **IOptions<EncryptionOptions>**:
//    - The ASP.NET Core Options pattern.  `IOptions<T>` holds the
//      configuration values that were bound and validated at startup.
//      The values are effectively read‑only for the application's
//      lifetime.
//
// 5. **Convert.FromBase64String**:
//    - Decodes a base64‑encoded string into a raw byte array.  The
//      keys in our configuration are stored as base64 text, so we
//      must decode them before use.
//
// 6. **Defensive copying** (`.ToArray()`):
//    - When we load key bytes from configuration, we immediately
//      call `.ToArray()` to create a new, independent copy.  This
//      ensures that no other code (including the options binder)
//      can modify our key material through a shared reference.
//
// 7. **ReadOnlyMemory<byte>** (return type):
//    - The `CurrentKey` property and `TryGetKey` method return a
//      read‑only view of the internal key bytes.  The consumer can
//      read the bytes but cannot modify them through this wrapper.
//      Because we already own the only copy of the array, the data
//      is effectively immutable to the outside world.
//
// 8. **out** parameter in `TryGetKey`:
//    - `out ReadOnlyMemory<byte> key` is an output parameter.  The
//      method MUST assign a value to `key` before it returns.  If
//      the key is found, we assign the byte array; otherwise we
//      assign `default` (an empty view).
//
// 9. **IReadOnlyCollection<string>** returned by `GetKeyIds()`:
//    - The method returns a cached snapshot of all key IDs.  Because
//      the key set never changes, we compute this list once in the
//      constructor and return it on every call – zero allocation.
//
// 10. **sealed** modifier:
//     - `sealed class` prevents other classes from inheriting from
//       this one.  This is a defensive design choice: we don't want
//       anyone to accidentally break the immutability contract by
//       overriding methods in a subclass.
// ====================================================================

using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// In‑memory key provider backed by a plain dictionary.
/// Keys are loaded once at startup and are deeply immutable.
/// </summary>
public sealed class KeyProvider : IKeyProvider
{
    // The currently active key.  Points to an array stored in _keys.
    // Because that array was already a defensive copy, no extra copy
    // is needed.
    private readonly byte[] _currentKey;

    // The normalised hex ID of the current key.
    // This is embedded in the ciphertext header so decryption can
    // select the correct key.
    private readonly string _currentKeyId;

    // All known keys, indexed by normalised hex key ID.
    // This dictionary is never modified after the constructor finishes.
    private readonly Dictionary<string, byte[]> _keys;

    // Cached snapshot of all key IDs.  Computed once in the constructor
    // because the key set never changes.
    private readonly IReadOnlyCollection<string> _keyIds;

    /// <summary>
    /// Initialises the key provider from validated encryption options.
    /// All key material is defensively copied to guarantee deep
    /// immutability.
    /// </summary>
    /// <param name="options">
    /// The encryption options, already validated and normalised by
    /// <see cref="EncryptionOptions.Validate()"/>.
    /// </param>
    public KeyProvider(IOptions<EncryptionOptions> options)
    {
        var config = options.Value;
        _keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Load every key from the normalised configuration dictionary.
        foreach (var kvp in config.Keys)
        {
            // Decode the base64 string into raw bytes.
            var decoded = Convert.FromBase64String(kvp.Value);

            // AES‑256 requires exactly 32 bytes.  This check is already
            // done by EncryptionOptions, but we repeat it for defence‑in‑depth.
            if (decoded.Length != 32)
                throw new InvalidOperationException(
                    $"Key '{kvp.Key}' is {decoded.Length} bytes; AES‑256 requires 32.");

            // DEFENSIVE COPY: create a new array that no external code
            // can reference.  This is the only allocation of this key.
            _keys[kvp.Key] = decoded.ToArray();
        }

        // Retrieve the normalised current key ID from the options.
        _currentKeyId = config.CurrentKeyId
            ?? throw new InvalidOperationException("CurrentKeyId is missing.");

        // Ensure the current key exists.
        if (!_keys.TryGetValue(_currentKeyId, out var currentKey))
            throw new InvalidOperationException(
                $"Current key ID '{_currentKeyId}' not found in the key dictionary.");

        // The array we got from the dictionary is already a private copy,
        // so we can use it directly without another .ToArray().
        _currentKey = currentKey;

        // Cache the key IDs once – the set never changes.
        _keyIds = _keys.Keys.ToArray();
    }

    // ----------------------------------------------------------------
    // IKeyProvider members
    // ----------------------------------------------------------------

    /// <inheritdoc />
    public ReadOnlyMemory<byte> CurrentKey => _currentKey;

    /// <inheritdoc />
    public string CurrentKeyId => _currentKeyId;

    /// <inheritdoc />
    public bool TryGetKey(string keyIdHex, out ReadOnlyMemory<byte> key)
    {
        // Look up the key ID in our immutable dictionary.
        if (_keys.TryGetValue(keyIdHex, out var keyBytes))
        {
            // No copy needed – the array is private and never mutated.
            // We return a read‑only view of it directly.
            key = keyBytes;
            return true;
        }

        // Key not found.  The out parameter must be assigned even on
        // failure, so we assign an empty ReadOnlyMemory<byte>.
        key = default;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetKeyIds()
    {
        // Return the cached snapshot – no allocation, thread‑safe.
        return _keyIds;
    }
}

// Dry‑run:
// 1. Configuration (appsettings.json or Key Vault):
// "Encryption": {
//   "CurrentKeyId": "a3f1b2c4-d5e6-4789-a0b1-c2d3e4f5a6b7",
//   "Keys": {
//     "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6": "base64Encoded32ByteKey...",
//     "b9d8e7f6123456789abcdef012345678": "anotherBase64Key..."
//   }
// }
// 2. Startup: EncryptionOptions validated, KeyProvider constructed as singleton.
// 3. Runtime: CurrentKeyId returns "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6".
//    The AesGcmEncryptionService uses this to embed the key ID in the ciphertext header.
