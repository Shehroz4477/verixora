// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / KeyProvider.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IKeyProvider"/> by storing AES‑256 keys
//   in memory, loaded from application configuration.  This is the
//   default key provider for the modular monolith.  It supports
//   key rotation by holding a dictionary of keys, and it is designed
//   to be replaced later by a vault‑backed implementation without
//   affecting the encryption service.
//
//   Keys are loaded once at startup from <see cref="EncryptionOptions"/>
//   and NEVER change after construction.  The provider is **deeply
//   immutable**: every byte array is defensively copied when loaded,
//   so no internal reference is shared with the outside world.
//
//   THREAD‑SAFETY:
//     Because the provider is fully immutable after construction,
//     all reads are inherently thread‑safe.  No locks are required.
//
//   DI LIFETIME:
//     This class MUST be registered as a **Singleton** in the DI
//     container.  Its entire state is constructed once and shared
//     across all requests.  Registering it as Scoped or Transient
//     would waste memory and CPU (re‑loading and re‑copying keys).
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** (reference type):
//    - A blueprint for creating objects.  This class implements an
//      interface and provides concrete behaviour.
//
// 2. **implements interface** (`: IKeyProvider`):
//    - The class must provide implementations for all members
//      declared in the interface.  The compiler enforces this.
//
// 3. **private readonly fields**:
//    - `_currentKey`: stores the current key's bytes.  `readonly`
//      means it can only be assigned in the constructor or field
//      initializer, ensuring it never changes after construction.
//    - `_keys`: dictionary mapping hex key ID to key bytes.
//      `readonly` prevents reassigning the dictionary reference.
//      The dictionary itself is never modified after construction.
//    - `_currentKeyId`: the normalised hex ID of the current key.
//
// 4. **Dictionary<string, byte[]>** with **StringComparer.Ordinal**:
//    - Maps a unique string key to a byte array using a hash table.
//    - `StringComparer.Ordinal` performs case‑sensitive, culture‑
//      insensitive comparisons, which are fast and predictable for
//      machine‑generated identifiers.
//
// 5. **IOptions<EncryptionOptions>**:
//    - The Options pattern in ASP.NET Core.  `IOptions<T>` provides
//      access to configuration values after they have been bound
//      and validated.  The values are read‑only for the application's
//      lifetime.
//
// 6. **Convert.FromBase64String**:
//    - Decodes a base64‑encoded string back to its original byte
//      array.  Used to convert the key material from configuration
//      into raw bytes.
//
// 7. **Defensive copying** (`.ToArray()`):
//    - When we load key bytes, we call `.ToArray()` to create a
//      new array that is independent of the original.  This ensures
//      that no other code (e.g., the options binder) can modify our
//      key material through a shared reference.
//    - The dictionary values are already independent arrays, so
//      `_currentKey` can safely point to the same array without
//      another copy – it is already a private, unreachable copy.
//
// 8. **ReadOnlyMemory<byte>** (return type):
//    - Wraps the internal byte array into a read‑only view for the
//      consumer.  The consumer cannot modify the key through this
//      view.  Combined with the defensive copy, the key material
//      is fully protected.
//
// 9. **GetKeyIds() snapshot**:
//    - Returns a new `List<string>` containing all dictionary keys
//      at the time of the call.  This is a fresh copy; modifying it
//      does not affect the provider.
//    - If this method is called very frequently (e.g., in a health‑
//      check endpoint), consider caching the result in a private
//      field because the set of keys never changes.
//
// 10. **Constructor validation**:
//     - The constructor reads the normalised options and builds the
//       key dictionary.  It validates that at least one key exists
//       and that the current key is present.  All validation was
//       already done by `EncryptionOptions.Validate()`, but we
//       duplicate the check for defence‑in‑depth.
// ====================================================================

using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Channels;
using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using SharedKernel.Domain.Results;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// In‑memory key provider that loads keys from <see cref="EncryptionOptions"/>
/// and holds them as deeply immutable byte arrays.
/// </summary>
/// <remarks>
/// This class is designed to be registered as a <b>Singleton</b>.
/// </remarks>
public sealed class KeyProvider : IKeyProvider
{
    // The currently active key.  Points to the same array stored in
    // _keys (which is already a defensive copy), so no further copy needed.
    private readonly byte[] _currentKey;

    // All known keys, indexed by normalised hex key ID.
    // The dictionary is built once in the constructor and never modified.
    private readonly Dictionary<string, byte[]> _keys;

    // The normalised hex ID of the current key (for diagnostics only).
    private readonly string _currentKeyId;

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

        // Build the key dictionary from the normalised configuration.
        _keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var kvp in config.Keys)
        {
            // Decode the base64-encoded key material.
            // The string is guaranteed valid base64 because validation
            // already checked it.
            var decodedBytes = Convert.FromBase64String(kvp.Value);

            // AES‑256 requires exactly 32 bytes.  This check is already
            // done by EncryptionOptions, but we repeat it for safety.
            if (decodedBytes.Length != 32)
                throw new InvalidOperationException(
                    $"Key '{kvp.Key}' is {decodedBytes.Length} bytes; AES‑256 requires 32.");

            // DEFENSIVE COPY: create a new array that is independent
            // of the decoded bytes.  No external code can modify our
            // key material through a shared reference.
            _keys[kvp.Key] = decodedBytes.ToArray();
        }

        // Retrieve the normalised current key ID.
        _currentKeyId = config.CurrentKeyId
            ?? throw new InvalidOperationException("CurrentKeyId is missing.");

        // Ensure the current key exists.
        if (!_keys.TryGetValue(_currentKeyId, out var currentKey))
            throw new InvalidOperationException(
                $"Current key ID '{_currentKeyId}' not found in the key dictionary.");

        // The array returned by TryGetValue is already a private,
        // defensive copy (made above).  We can safely use it directly
        // without an extra .ToArray().
        _currentKey = currentKey;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> CurrentKey => _currentKey;

    /// <inheritdoc />
    public bool TryGetKey(string keyIdHex, out ReadOnlyMemory<byte> key)
    {
        if (_keys.TryGetValue(keyIdHex, out var keyBytes))
        {
            // The stored array is a private copy, safe to expose as ReadOnlyMemory.
            key = keyBytes;
            return true;
        }

        key = default;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetKeyIds()
    {
        // Return a snapshot: a new list containing all keys at this moment.
        // Since the dictionary never changes, this is both thread‑safe
        // and correct.  For high‑frequency calls, consider caching this
        // list in a field because the set of keys is immutable.
        return new List<string>(_keys.Keys);
    }
}



//Dry‑run – from configuration to runtime:

//appsettings.json / Key Vault provides a dictionary of base64 keys and a current key ID.

//Startup: EncryptionOptions is bound, validated, and normalised.

//KeyProvider constructor:

//Each base64 string is decoded to a byte[32].

//_keys[id] = decodedBytes.ToArray() – a fresh, independent copy.

//_currentKey is assigned directly from that dictionary entry (no extra copy).

//Runtime:

//CurrentKey returns a read‑only view of the in‑memory array.

//TryGetKey looks up old keys in O(1).

//GetKeyIds() returns a new List<string> copy of the IDs.
