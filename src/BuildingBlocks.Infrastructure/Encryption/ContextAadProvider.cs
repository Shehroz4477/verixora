// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / ContextAadProvider.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IAadProvider"/> by constructing a
//   deterministic, collision‑resistant AAD byte sequence from an
//   <see cref="EncryptionContext"/> and a ciphertext version byte.
//
//   The AAD is built in two stages:
//     1. **Binary encoding** – the context fields are serialised into
//        a length‑prefixed, version‑tagged binary layout.
//     2. **Cryptographic hashing** – the binary encoding is fed into
//        SHA‑256 to produce a fixed‑size (32‑byte) output that serves
//        as the actual AAD passed to AES‑GCM.
//
//   Why SHA‑256?
//     It acts as a *canonicalization layer*: the output is always
//     32 bytes, independent of field lengths, which eliminates any
//     structural ambiguity and makes the AAD a true cryptographic
//     fingerprint of the context.  Raw binary AAD would tightly
//     couple the ciphertext to the serialisation format, making
//     future layout changes very dangerous.
//
//   BINARY LAYOUT (input to SHA‑256):
//     [domain tag – length‑prefixed, big‑endian ushort]
//     [AAD schema version – 1 byte]
//     [tenant ID – length‑prefixed]
//     [entity type – length‑prefixed]
//     [purpose – length‑prefixed]
//     [ciphertext format version – 1 byte]
//
//   The domain tag ("VERIXORA_v1") is always first for cross‑system
//   isolation.  The AAD schema version follows, making the layout
//   self‑describing so that a future version of this class can
//   decode old AADs correctly.
//
//   MAX‑LENGTH GUARD:
//     Each context field is limited to 1024 bytes when UTF‑8 encoded.
//     This prevents an accidentally huge input from causing memory
//     exhaustion during AAD construction.
//
//   This class is stateless and thread‑safe.  Register as a Singleton.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **sealed** modifier:
//    - Prevents other classes from inheriting.  AAD construction is
//      security‑sensitive; we never want a subclass to accidentally
//      change the logic.
//
// 2. **static readonly byte[]** (pre‑encoded constant):
//    - `DomainBytes` is computed once when the CLR loads the type,
//      not every time `BuildAad` is called.  This avoids repeated
//      allocations.
//
// 3. **Encoding.UTF8.GetBytes**:
//    - Converts a .NET string (internally UTF‑16) into a UTF‑8 byte
//      array.  UTF‑8 is the standard encoding for cryptographic
//      protocols and data interchange.
//
// 4. **Buffer.BlockCopy**:
//    - A fast, low‑level method for copying a block of bytes from
//      one array to another.  It is much faster than a manual loop.
//
// 5. **ushort big‑endian length prefixes**:
//    - Each field's length is written as a 2‑byte big‑endian integer.
//      Big‑endian means the most significant byte comes first.  This
//      is standard in network and crypto protocols and ensures
//      consistent parsing regardless of the platform's native
//      endianness (which is little‑endian on x86/ARM).
//
// 6. **SHA256.HashData**:
//    - A static method (available in .NET 8) that computes a SHA‑256
//      hash in one call.  It returns a new `byte[32]`.  Simple,
//      fast, and cryptographically secure.
//
// 7. **ReadOnlyMemory<byte>** return:
//    - The final hash is returned as a read‑only view.  The caller
//      can read the bytes but cannot modify them.  This keeps the
//      AAD safe after it leaves this method.
//
// 8. **ArgumentNullException.ThrowIfNull**:
//    - A .NET 6+ helper that throws if the argument is null.  It's
//      more concise than writing `if (x == null) throw new …`.
//
// 9. **private static helpers** (`EncodeAndValidate`, `WriteUInt16BigEndian`):
//    - `static` methods belong to the class, not an instance.  They
//      cannot access instance fields, which makes them pure and easy
//      to test.  We use them to keep `BuildAad` clean.
//
// 10. **namespace** declaration:
//     - Keeps the class organised within the shared encryption
//       infrastructure module.
// ====================================================================

using System.Security.Cryptography;
using System.Text;
using SharedKernel.Domain.Base;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// Builds AAD using length‑prefixed binary encoding and SHA‑256 hashing.
/// </summary>
public sealed class ContextAadProvider : IAadProvider
{
    // ----------------------------------------------------------------
    // Constants
    // ----------------------------------------------------------------

    // Version of *this* AAD layout.  Bump if fields are added/reordered.
    private const byte AadSchemaVersion = 0x01;

    // Maximum UTF‑8 bytes allowed for any single context field.
    // Prevents memory exhaustion attacks.
    private const int MaxFieldLength = 1024;

    // Domain separation tag, pre‑encoded to avoid repeated allocations.
    // "VERIXORA_v1" ensures AADs from this system cannot collide with
    // AADs from another system or a future version with a different tag.
    private static readonly byte[] DomainBytes = Encoding.UTF8.GetBytes("VERIXORA_v1");

    // ----------------------------------------------------------------
    // BuildAad – the only public method
    // ----------------------------------------------------------------

    /// <inheritdoc />
    public ReadOnlyMemory<byte> BuildAad(EncryptionContext context, byte cipherVersion)
    {
        // Fail immediately if the context is null – we never want to
        // silently produce a meaningless AAD.
        ArgumentNullException.ThrowIfNull(context);

        // Encode each context field to UTF‑8, with length validation.
        byte[] tenantBytes = EncodeAndValidate(context.TenantId, nameof(context.TenantId));
        byte[] entityBytes = EncodeAndValidate(context.EntityType, nameof(context.EntityType));
        byte[] purposeBytes = EncodeAndValidate(context.Purpose, nameof(context.Purpose));

        // Calculate the total size of the binary input to SHA‑256.
        //
        // Layout:
        //   2 + DomainBytes.Length  → domain tag (length‑prefixed)
        //   1                        → AAD schema version
        //   2 + tenantBytes.Length   → tenant ID
        //   2 + entityBytes.Length   → entity type
        //   2 + purposeBytes.Length  → purpose
        //   1                        → ciphertext format version
        int totalSize = 2 + DomainBytes.Length
                      + 1
                      + 2 + tenantBytes.Length
                      + 2 + entityBytes.Length
                      + 2 + purposeBytes.Length
                      + 1;

        byte[] input = new byte[totalSize];
        int offset = 0;

        // --- Write each field into the buffer ---

        // 1. Domain separation tag (always first).
        WriteUInt16BigEndian(input, ref offset, DomainBytes.Length);
        Buffer.BlockCopy(DomainBytes, 0, input, offset, DomainBytes.Length);
        offset += DomainBytes.Length;

        // 2. AAD schema version – identifies this exact layout.
        input[offset++] = AadSchemaVersion;

        // 3. Tenant ID.
        WriteUInt16BigEndian(input, ref offset, tenantBytes.Length);
        Buffer.BlockCopy(tenantBytes, 0, input, offset, tenantBytes.Length);
        offset += tenantBytes.Length;

        // 4. Entity type.
        WriteUInt16BigEndian(input, ref offset, entityBytes.Length);
        Buffer.BlockCopy(entityBytes, 0, input, offset, entityBytes.Length);
        offset += entityBytes.Length;

        // 5. Purpose.
        WriteUInt16BigEndian(input, ref offset, purposeBytes.Length);
        Buffer.BlockCopy(purposeBytes, 0, input, offset, purposeBytes.Length);
        offset += purposeBytes.Length;

        // 6. Ciphertext format version.
        input[offset] = cipherVersion;

        // Hash the complete binary input → fixed 32‑byte AAD.
        // SHA256.HashData returns a new byte[32]; we return it as a
        // read‑only view so the caller cannot modify it.
        byte[] hash = SHA256.HashData(input);
        return hash;
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Encodes a string to UTF‑8 and validates that its length does
    /// not exceed <see cref="MaxFieldLength"/>.
    /// </summary>
    private static byte[] EncodeAndValidate(string value, string parameterName)
    {
        // Reject null or whitespace – every field must have a meaningful value.
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                $"'{parameterName}' cannot be null or whitespace.", parameterName);

        // Convert to UTF‑8 bytes.
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        // Guard against excessively long values.
        if (bytes.Length > MaxFieldLength)
            throw new ArgumentException(
                $"'{parameterName}' exceeds maximum length of {MaxFieldLength} bytes.");

        return bytes;
    }

    /// <summary>
    /// Writes a 16‑bit unsigned integer in big‑endian order into
    /// <paramref name="buffer"/> starting at <paramref name="offset"/>,
    /// and advances <paramref name="offset"/> by 2.
    /// </summary>
    private static void WriteUInt16BigEndian(byte[] buffer, ref int offset, int value)
    {
        // The high byte (most significant 8 bits) goes first.
        buffer[offset++] = (byte)(value >> 8);
        // The low byte (least significant 8 bits) goes second.
        buffer[offset++] = (byte)(value & 0xFF);
    }
}







//Assume:
//  context = new EncryptionContext("tenant-1", "User", "Email")
//  cipherVersion = 0x01

//Step 1: Validate & encode fields
//  tenantBytes  = [116, 101, 110, 97, 110, 116, 45, 49]   // "tenant-1" in UTF‑8 (8 bytes)
//  entityBytes = [85, 115, 101, 114]                 // "User" (4 bytes)
//  purposeBytes = [69, 109, 97, 105, 108]              // "Email" (5 bytes)
//  DomainBytes = [86, 69, 82, 73, 88, 79, 82, 65, 95, 118, 49]  // "VERIXORA_v1" (12 bytes)

//Step 2: Build binary input buffer(41 bytes total)
//  [0, 12]               ← domain tag length(12)
//  [86,…, 49]            ← domain tag bytes
//  [1]                  ← AAD schema version(0x01)
//  [0, 8]                ← tenant length(8)
//  [116,…, 49]           ← tenant bytes
//  [0, 4]                ← entity length(4)
//  [85,…, 114]           ← entity bytes
//  [0, 5]                ← purpose length(5)
//  [69,…, 108]           ← purpose bytes
//  [1]                  ← ciphertext version(0x01)

//Step 3: SHA256.HashData(buffer) → byte[32]
//  This 32‑byte hash is the final AAD.It will be identical for the same
//  context + version, and completely different if any value changes.
