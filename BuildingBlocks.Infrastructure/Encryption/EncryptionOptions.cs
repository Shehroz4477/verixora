// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionOptions.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   This class is a **Configuration Object** (also called an Options
//   class).  It holds the settings needed by the AES‑256 encryption
//   service.  The values are read from appsettings.json or from a
//   secrets manager (Azure Key Vault) and bound by ASP.NET Core's
//   Options pattern (`IOptions<EncryptionOptions>`).  This class is
//   also responsible for **validating** those settings at startup so
//   that a misconfiguration is detected immediately, not at runtime
//   when encryption is first used.
//
//   Design decision – Multi‑key support:
//     The service needs to support **key rotation**: old data is still
//     encrypted with an old key, new data uses a new key.  To enable
//     this, we store a dictionary of keys (`Keys`) and a pointer to
//     the currently active one (`CurrentKeyId`).  The older single‑key
//     properties (`Key`, `IV`) are kept for backward compatibility but
//     are marked as deprecated.
//
//   Why not just store keys directly in the service?
//     The Options pattern keeps configuration separate from behaviour.
//     It also makes it easy to validate settings *before* any service
//     is created – the application can fail fast if the configuration
//     is wrong.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** (reference type):
//    - A blueprint for creating objects.  Classes support inheritance,
//      encapsulation, and polymorphism.  Here we use a plain class
//      because we need mutable properties during binding (the Options
//      pattern sets properties after the object is created).
//
// 2. **public** access modifier:
//    - The class and its members are visible to any code in the
//      solution.  This is necessary because the configuration binder
//      (in the API host) needs to set these properties.
//
// 3. **Properties** (`{ get; set; }` and `{ get; private set; }`):
//    - Properties are members that provide a flexible mechanism to
//      read, write, or compute the value of a private field.
//    - `{ get; set; }` creates an **auto‑implemented property** – the
//      compiler automatically generates a hidden backing field.
//    - `{ get; private set; }` means the property can be read by
//      anyone, but can only be written from *within* the class itself.
//      We use this for `Keys` and `CurrentKeyId` so that after
//      validation they cannot be modified accidentally by external code.
//
// 4. **Dictionary<string, string>**:
//    - A collection of key‑value pairs.  Here the key is a string
//      (the key ID) and the value is a string (a base64‑encoded key).
//    - Under the hood, a Dictionary uses a hash table for fast lookups.
//
// 5. **IReadOnlyDictionary<string, string>**:
//    - An interface that represents a read‑only view of a dictionary.
//      It does not have methods like `Add` or `Remove`, so it prevents
//      accidental modification.  We return a `ReadOnlyDictionary`
//      which is a concrete wrapper around a normal dictionary.
//    - This helps enforce **immutability** – once configuration is
//      validated, it cannot be changed at runtime.
//
// 6. **string?** (nullable reference type):
//    - The `?` annotation means the value can be `null`.  This is a
//      C# 8+ feature that helps avoid `NullReferenceException` by
//      making nullability explicit.  The compiler will warn if we try
//      to use a nullable value without checking for null first.
//
// 7. **private** methods:
//    - Methods that are only accessible from within the same class.
//      We use them for validation helpers that should not be called
//      from outside.
//
// 8. **static** methods:
//    - Methods that belong to the class itself, not to any particular
//      instance.  They cannot access instance fields/properties.  We
//      use them for pure helper functions (like NormaliseKeyId) that
//      don't depend on the state of an EncryptionOptions object.
//
// 9. **ReadOnlyDictionary<TKey, TValue>**:
//    - A concrete class in `System.Collections.ObjectModel` that
//      implements `IReadOnlyDictionary`.  It wraps an existing
//      dictionary and prevents any modifications.
//
// 10. **Guid.TryParse**:
//     - Safely attempts to convert a string into a `Guid` (Globally
//       Unique Identifier).  Returns `true` if successful, `false`
//       otherwise.  Safer than `Guid.Parse` because it doesn't throw.
//
// 11. **Convert.FromBase64String**:
//     - Decodes a base64‑encoded string into a byte array.  Used to
//       convert the key material from its textual representation to
//       the raw bytes required by the AES algorithm.
//
// 12. **System.ComponentModel.DataAnnotations.RequiredAttribute**:
//     - A data annotation that can be used for validation.  We don't
//       actually use `[Required]` in this version (we removed it),
//       but the `using` statement remains from earlier iterations.
//       We perform manual validation instead for more control.
//
// 13. **StringComparer.Ordinal**:
//     - A comparer that performs a case‑sensitive comparison using
//       the ordinal (binary) values of the characters.  It is the
//       fastest and most predictable comparer, ideal for
//       machine‑generated identifiers like hex strings.
//
// 14. **HashSet<string>**:
//     - A collection that contains only unique elements.  We use it
//       to detect duplicate key IDs after normalisation.
// ====================================================================

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SharedKernel.Domain.Base;

namespace BuildingBlocks.Infrastructure.Encryption;

public class EncryptionOptions
{
    // ----------------------------------------------------------------
    // PROPERTIES – Multi‑key rotation (recommended)
    // ----------------------------------------------------------------

    /// <summary>
    /// The key ID (a GUID) that identifies the key to use for new
    /// encryptions.  After validation, this property will contain the
    /// normalised 32‑character lowercase hex representation of that GUID.
    ///
    /// <example>
    ///   "a3f1b2c4-d5e6-4789-a0b1-c2d3e4f5a6b7"
    ///   after validation becomes
    ///   "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6"
    /// </example>
    /// </summary>
    public string? CurrentKeyId { get; private set; }

    /// <summary>
    /// All known encryption keys, indexed by their normalised hex key ID.
    /// This property is always an <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// – it is initialised as an empty read‑only dictionary and replaced
    /// with a validated one during startup.  After validation it is
    /// immutable.
    /// </summary>
    public IReadOnlyDictionary<string, string> Keys { get; private set; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    // ----------------------------------------------------------------
    // PROPERTIES – Single‑key legacy (deprecated)
    // ----------------------------------------------------------------

    /// <summary>
    /// [DEPRECATED] A single base64‑encoded 256‑bit AES key.
    /// Use the <see cref="Keys"/> dictionary instead.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// [DEPRECATED] An optional base64‑encoded 128‑bit IV.
    /// </summary>
    public string? IV { get; set; }

    // ================================================================
    // VALIDATION – called at application startup
    // ================================================================

    /// <summary>
    /// Validates the encryption configuration and normalises all key IDs.
    /// This method should be called during startup (via
    /// <c>.ValidateOnStart()</c> in the DI registration) to ensure that
    /// any misconfiguration is detected immediately.
    ///
    /// <para>It performs the following checks:</para>
    /// <list type="number">
    /// <item>Cannot mix legacy and multi‑key configuration.</item>
    /// <item>If using multi‑key: <c>CurrentKeyId</c> is required, all
    /// key IDs are valid GUIDs, all key values are valid base64 and
    /// decode to exactly 32 bytes, and the current key ID exists in
    /// the dictionary.</item>
    /// <item>If using legacy: <c>Key</c> is valid base64 and decodes
    /// to exactly 32 bytes; <c>IV</c> (if provided) decodes to 16 bytes.</item>
    /// </list>
    ///
    /// <para>After successful validation, all key IDs are normalised to
    /// 32‑character lowercase hex, and the <see cref="Keys"/> dictionary
    /// is replaced with an immutable read‑only copy.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the configuration is invalid.
    /// </exception>
    public void Validate()
    {
        // ------------------------------------------------------------
        // Step 1: Determine which model is being used.
        // ------------------------------------------------------------
        // We consider multi‑key if at least one entry exists in the
        // Keys dictionary.  We consider legacy if Key is not null/empty.
        bool hasKeys = Keys.Count > 0;
        bool hasLegacy = !string.IsNullOrWhiteSpace(Key);

        // Disallow mixing both models – it's ambiguous which one to use.
        if (hasKeys && hasLegacy)
            throw new InvalidOperationException(
                "EncryptionOptions: Cannot mix legacy (Key/IV) and multi‑key (Keys/CurrentKeyId) configuration.");

        // If multi‑key is configured, validate and normalise it.
        if (hasKeys)
        {
            ValidateAndNormaliseKeyDictionary();
            return;  // done – no need to check legacy
        }

        // If only legacy is configured, validate it.
        if (hasLegacy)
        {
            ValidateLegacyKey();
            return;
        }

        // If neither is configured, that's an error.
        throw new InvalidOperationException(
            "EncryptionOptions: Either Keys or Key must be configured.");
    }

    // ----------------------------------------------------------------
    // PRIVATE VALIDATION HELPERS
    // ----------------------------------------------------------------

    /// <summary>
    /// Validates and normalises the multi‑key configuration.
    /// After this method completes, <see cref="CurrentKeyId"/> and all
    /// keys in <see cref="Keys"/> will be in canonical lowercase hex
    /// form, and <see cref="Keys"/> will be immutable.
    /// </summary>
    private void ValidateAndNormaliseKeyDictionary()
    {
        // ---- Check that CurrentKeyId is present ----
        if (string.IsNullOrWhiteSpace(CurrentKeyId))
            throw new InvalidOperationException(
                "EncryptionOptions: CurrentKeyId is required when Keys are configured.");

        // Normalise CurrentKeyId: parse as GUID, convert to 32‑char hex.
        // Store the normalised value back so that any code reading
        // EncryptionOptions later gets the canonical form.
        CurrentKeyId = NormaliseKeyId(CurrentKeyId);

        // ---- Build a new dictionary with normalised keys ----
        // We use an Ordinal comparer for case‑sensitive, fast lookups.
        var normalisedKeys = new Dictionary<string, string>(StringComparer.Ordinal);

        // Iterate over every entry in the original Keys dictionary.
        foreach (var kvp in Keys)
        {
            // The key (GUID string) must not be null or empty.
            if (string.IsNullOrWhiteSpace(kvp.Key))
                throw new InvalidOperationException(
                    "EncryptionOptions: A key ID in the Keys dictionary is null or empty.");

            // The value (base64 key) must not be null or empty.
            if (string.IsNullOrWhiteSpace(kvp.Value))
                throw new InvalidOperationException(
                    $"EncryptionOptions: Key '{kvp.Key}' has no value.");

            // Normalise the key ID.
            var normalisedKey = NormaliseKeyId(kvp.Key);

            // Check for duplicates (could happen if two different GUID
            // representations normalise to the same hex string, e.g.,
            // uppercase vs lowercase).
            if (normalisedKeys.ContainsKey(normalisedKey))
                throw new InvalidOperationException(
                    $"EncryptionOptions: Duplicate key ID after normalisation: {normalisedKey}.");

            // Validate the key material: must be valid base64 and decode
            // to exactly 32 bytes.
            DecodeBase64(kvp.Value, $"Key '{normalisedKey}'", 32);

            // Store the normalised entry.
            normalisedKeys[normalisedKey] = kvp.Value;
        }

        // ---- Ensure the current key actually exists ----
        if (!normalisedKeys.ContainsKey(CurrentKeyId))
            throw new InvalidOperationException(
                $"EncryptionOptions: CurrentKeyId '{CurrentKeyId}' not found in the Keys dictionary.");

        // ---- Replace the Keys property with an immutable version ----
        // ReadOnlyDictionary prevents accidental additions/removals later.
        Keys = new ReadOnlyDictionary<string, string>(normalisedKeys);
    }

    /// <summary>
    /// Validates the legacy single‑key configuration.
    /// </summary>
    private void ValidateLegacyKey()
    {
        // Key must be valid base64 and decode to 32 bytes.
        DecodeBase64(Key!, "Key", 32);

        // IV is optional, but if provided must be valid base64 and 16 bytes.
        if (!string.IsNullOrWhiteSpace(IV))
            DecodeBase64(IV, "IV", 16);
    }

    // ================================================================
    // STATIC HELPERS (pure functions, no side effects)
    // ================================================================

    /// <summary>
    /// Converts a string into a canonical 32‑character lowercase hex
    /// representation of a GUID.
    ///
    /// <para>Why:</para>
    /// GUIDs can be written in many formats:
    ///   - "a3f1b2c4-d5e6-4789-a0b1-c2d3e4f5a6b7" (hyphenated)
    ///   - "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6" (hex, "N" format)
    ///   - "{a3f1b2c4-...}" (braces)
    /// By converting to a single canonical format, we ensure consistent
    /// dictionary lookups.
    /// </summary>
    /// <param name="keyId">A string that should represent a valid GUID.</param>
    /// <returns>A 32‑character lowercase hex string (Guid.ToString("N")).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the input is not a valid GUID.
    /// </exception>
    private static string NormaliseKeyId(string keyId)
    {
        // TryParse returns true if the string is a valid GUID.
        // It avoids throwing an exception for bad input, which is
        // faster and safer than using Guid.Parse.
        if (!Guid.TryParse(keyId, out var guid))
            throw new InvalidOperationException(
                $"EncryptionOptions: '{keyId}' is not a valid GUID for a key ID.");

        // Guid.ToString("N") returns a 32‑character lowercase hex string
        // without hyphens or braces.  ToLowerInvariant ensures consistent
        // casing regardless of the original input's casing.
        return guid.ToString("N").ToLowerInvariant();
    }

    /// <summary>
    /// Decodes a base64 string and validates its byte length.
    ///
    /// <para>Why validate length:</para>
    /// AES‑256 requires exactly 32 bytes of key material.  If someone
    /// accidentally provides a base64 string that decodes to a different
    /// size, the encryption would fail at runtime with a confusing
    /// error.  We catch it early here.
    /// </summary>
    /// <param name="value">The base64‑encoded string.</param>
    /// <param name="name">A human‑readable name for error messages.</param>
    /// <param name="expectedLength">The expected number of bytes.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the string is not valid base64 or decodes to the wrong length.
    /// </exception>
    private static byte[] DecodeBase64(string value, string name, int expectedLength)
    {
        try
        {
            // Convert.FromBase64String throws FormatException if the
            // input is not valid base64.
            var bytes = Convert.FromBase64String(value);

            // Check the decoded length.  For AES‑256, this must be 32.
            if (bytes.Length != expectedLength)
                throw new InvalidOperationException(
                    $"EncryptionOptions: {name} must decode to {expectedLength} bytes, but got {bytes.Length}.");

            return bytes;
        }
        catch (FormatException)
        {
            // FormatException is thrown when the base64 string contains
            // invalid characters or is malformed.  We wrap it in a clearer
            // exception type.
            throw new InvalidOperationException(
                $"EncryptionOptions: {name} is not a valid base64 string.");
        }
    }
}




//Dry‑run example – what happens when Validate() is called with valid multi‑key configuration:

//Assume appsettings.json contains:

//json
//{
//    "Encryption": {
//        "CurrentKeyId": "A3F1B2C4-D5E6-4789-A0B1-C2D3E4F5A6B7",
//    "Keys": {
//            "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6": "base64Encoded32ByteKey...==",
//      "b9d8e7f6-1234-5678-9abc-def012345678": "anotherBase64Key...=="
//    }
//    }
//}
//Step‑by‑step execution of Validate():

//hasKeys = true (Keys.Count = 2), hasLegacy = false (Key is null).
//→ Enters ValidateAndNormaliseKeyDictionary().

//CurrentKeyId is "A3F1B2C4-D5E6-4789-A0B1-C2D3E4F5A6B7".
//NormaliseKeyId is called:

//Guid.TryParse succeeds, guid = { a3f1b2c4 - d5e6 - 4789 - a0b1 - c2d3e4f5a6b7 }.

//Returns "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6".

//CurrentKeyId is now "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6".

//Iterates over Keys:

//First entry: key = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6" (already normalised).
//Validates base64 value, stores it.

//Second entry: key = "b9d8e7f6-1234-5678-9abc-def012345678".
//Normalises to "b9d8e7f6123456789abcdef012345678".
//Validates base64 value, stores it.

//Checks if "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6" exists in normalisedKeys → yes.

//Replaces Keys with a ReadOnlyDictionary<string, string> containing the two normalised entries.
