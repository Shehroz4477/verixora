// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionOptions.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   This class is a **Configuration Object** (Options pattern) that
//   holds the encryption settings.  It is bound from appsettings.json
//   (or a secrets manager) and validated at startup.  If validation
//   fails, the application refuses to start – no encryption bug
//   can hide until runtime.
//
//   It supports two models:
//     1. Multi‑key (Keys + CurrentKeyId) – for key rotation.
//     2. Legacy single key (Key + IV) – deprecated but still works.
//
//   After validation, Keys becomes a ReadOnlyDictionary, making it
//   immutable for the rest of the process lifetime.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** (reference type) – holds data + behaviour (validation).
//
// 2. **Properties** (`{ get; set; }` / `{ get; private set; }`):
//    - Auto‑implemented properties with a hidden backing field.
//    - `private set` prevents external code from changing the value
//      after validation (encapsulation).
//
// 3. **IReadOnlyDictionary<TKey,TValue>**:
//    - Interface that only exposes read methods (no Add/Remove).
//    - We store a `ReadOnlyDictionary` implementation to enforce
//      immutability.
//
// 4. **string?** (nullable reference type):
//    - The `?` indicates the property can be `null`.  The compiler
//      helps avoid `NullReferenceException` by warning when we
//      forget to check for null.
//
// 5. **static** helper methods:
//    - Belong to the class itself, not to instances.  They cannot
//      access instance fields.  Used for pure validation logic.
//
// 6. **Guid.TryParse** + **Guid.ToString("N")**:
//    - Safe GUID parsing without exceptions.
//    - "N" format gives a 32‑char lowercase hex string.
//
// 7. **Convert.FromBase64String**:
//    - Decodes base64 text into a byte array.  Used for reading
//      key material from configuration.
//
// 8. **ReadOnlyDictionary<TKey,TValue>** (from System.Collections.ObjectModel):
//    - A concrete wrapper around a normal dictionary that prevents
//      any modifications.
//
// 9. **StringComparer.Ordinal**:
//    - Case‑sensitive, culture‑insensitive string comparer.
//      Fast and predictable for machine‑generated identifiers.
//
// 10. **HashSet<string>** (implicitly via `ContainsKey` logic):
//     - We use `normalisedKeys.ContainsKey` to detect duplicates.
// ====================================================================

using System.Collections.ObjectModel;

namespace BuildingBlocks.Infrastructure.Encryption;

public class EncryptionOptions
{
    // ----------------------------------------------------------------
    // Multi‑key rotation (recommended)
    // ----------------------------------------------------------------

    /// <summary>
    /// The key ID (GUID) to use for new encryptions.
    /// After validation this is a normalised 32‑character lowercase hex string.
    /// </summary>
    public string? CurrentKeyId { get; private set; }

    /// <summary>
    /// All known encryption keys, indexed by normalised key ID.
    /// After validation this is an immutable read‑only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string> Keys { get; private set; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    // ----------------------------------------------------------------
    // Single‑key legacy (deprecated)
    // ----------------------------------------------------------------

    /// <summary>
    /// [DEPRECATED] The base64‑encoded 256‑bit AES key.
    /// Use the Keys dictionary instead.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// [DEPRECATED] Optional base64‑encoded 128‑bit IV.
    /// If not provided, a random IV is generated per operation.
    /// </summary>
    public string? IV { get; set; }

    // ================================================================
    // Validate()
    // Called at startup (via .ValidateOnStart() in DI) to fail fast.
    // ================================================================
    public void Validate()
    {
        bool hasKeys = Keys.Count > 0;
        bool hasLegacy = !string.IsNullOrWhiteSpace(Key);

        // Cannot mix both models.
        if (hasKeys && hasLegacy)
            throw new InvalidOperationException(
                "EncryptionOptions: Cannot mix legacy (Key/IV) and multi‑key (Keys/CurrentKeyId) configuration.");

        if (hasKeys)
        {
            ValidateAndNormaliseKeyDictionary();
            return;
        }

        if (hasLegacy)
        {
            ValidateLegacyKey();
            return;
        }

        // Neither model configured.
        throw new InvalidOperationException(
            "EncryptionOptions: Either Keys or Key must be configured.");
    }

    // ----------------------------------------------------------------
    // Multi‑key validation + normalisation
    // ----------------------------------------------------------------
    private void ValidateAndNormaliseKeyDictionary()
    {
        if (string.IsNullOrWhiteSpace(CurrentKeyId))
            throw new InvalidOperationException(
                "EncryptionOptions: CurrentKeyId is required when Keys are configured.");

        // Normalise the current key ID (parse as GUID, convert to hex).
        CurrentKeyId = NormaliseKeyId(CurrentKeyId);

        // Build a new dictionary with normalised keys.
        var normalisedKeys = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var kvp in Keys)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                throw new InvalidOperationException(
                    "EncryptionOptions: A key ID in the Keys dictionary is null or empty.");
            if (string.IsNullOrWhiteSpace(kvp.Value))
                throw new InvalidOperationException(
                    $"EncryptionOptions: Key '{kvp.Key}' has no value.");

            var normalisedKey = NormaliseKeyId(kvp.Key);
            if (normalisedKeys.ContainsKey(normalisedKey))
                throw new InvalidOperationException(
                    $"EncryptionOptions: Duplicate key ID after normalisation: {normalisedKey}.");

            // Validate the base64-encoded key material.
            DecodeBase64(kvp.Value, $"Key '{normalisedKey}'", 32);
            normalisedKeys[normalisedKey] = kvp.Value;
        }

        // The current key must exist in the dictionary.
        if (!normalisedKeys.ContainsKey(CurrentKeyId))
            throw new InvalidOperationException(
                $"EncryptionOptions: CurrentKeyId '{CurrentKeyId}' not found in the Keys dictionary.");

        // Replace the mutable dictionary with an immutable one.
        Keys = new ReadOnlyDictionary<string, string>(normalisedKeys);
    }

    // ----------------------------------------------------------------
    // Legacy key validation
    // ----------------------------------------------------------------
    private void ValidateLegacyKey()
    {
        DecodeBase64(Key!, "Key", 32);
        if (!string.IsNullOrWhiteSpace(IV))
            DecodeBase64(IV, "IV", 16);
    }

    // ================================================================
    // Static helper methods
    // ================================================================

    /// <summary>
    /// Converts a GUID string to a normalised 32‑char lowercase hex string.
    /// </summary>
    private static string NormaliseKeyId(string keyId)
    {
        if (!Guid.TryParse(keyId, out var guid))
            throw new InvalidOperationException(
                $"EncryptionOptions: '{keyId}' is not a valid GUID for a key ID.");
        return guid.ToString("N").ToLowerInvariant();
    }

    /// <summary>
    /// Decodes a base64 string and validates its byte length.
    /// </summary>
    private static byte[] DecodeBase64(string value, string name, int expectedLength)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length != expectedLength)
                throw new InvalidOperationException(
                    $"EncryptionOptions: {name} must decode to {expectedLength} bytes, but got {bytes.Length}.");
            return bytes;
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                $"EncryptionOptions: {name} is not a valid base64 string.");
        }
    }
}


// appsettings.json (or Key Vault reference):
// {
//   "Encryption": {
//     "CurrentKeyId": "A3F1B2C4-D5E6-4789-A0B1-C2D3E4F5A6B7",
//     "Keys": {
//       "a3f1b2c4-d5e6-4789-a0b1-c2d3e4f5a6b7": "base64Encoded32ByteKey...",
//       "b9d8e7f6-1234-5678-9abc-def012345678": "anotherBase64Key..."
//     }
//   }
// }

// 1. ASP.NET Core binds the "Encryption" section to this class.
// 2. Validate() is called.
//    - CurrentKeyId = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6" (normalised).
//    - Each dictionary key is normalised, base64 values checked.
//    - The Keys dictionary is replaced with an immutable ReadOnlyDictionary.
// 3. The application starts with a fully valid, immutable encryption config.
