// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / AesGcmEncryptionService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IEncryptionService"/> by orchestrating three
//   components:
//     1. <see cref="IKeyProvider"/>   – supplies the AES‑256 key
//     2. <see cref="IAadProvider"/>   – builds the AAD for tenant/entity binding
//     3. AES‑GCM                       – performs authenticated encryption
//
//   This class assembles the binary ciphertext packet, enforces DoS
//   protection, and routes decryption to the correct version handler.
//   It is the only class that consumers (EF Core value converters,
//   application services) directly depend on.
//
//   BINARY CIPHERTEXT FORMAT (before Base64 encoding):
//     [version : 1 byte]   – 0x01 (enables future algorithm changes)
//     [keyIdHex : 32 bytes] – ASCII hex string of the key used
//     [nonce : 12 bytes]    – unique random value for AES‑GCM
//     [ciphertext : variable] – the encrypted data
//     [tag : 16 bytes]      – authentication tag
//
//   DOS PROTECTION:
//     Decryption rejects payloads larger than 1 MB to prevent memory
//     exhaustion attacks.
//
//   THREAD‑SAFETY:
//     The service is stateless after construction (all state is in the
//     injected providers).  It can be registered as a Singleton.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface (`: IEncryptionService`):
//    - The compiler enforces that all members of the interface are present.
//
// 2. **private readonly fields**:
//    - Store injected dependencies.  `readonly` ensures they are set
//      once (in the constructor) and never changed.
//
// 3. **Constructor injection**:
//    - Dependencies are passed via the constructor and stored in fields.
//      This makes the class easy to test (mock providers can be injected)
//      and follows the Explicit Dependencies Principle.
//
// 4. **const** (compile‑time constants):
//    - `Version`, `TagSize`, `NonceSize`, `KeyIdHexSize` are values that
//      never change.  `const` embeds them directly into the compiled IL.
//
// 5. **RandomNumberGenerator.Fill**:
//    - Fills a byte array with cryptographically secure random bytes.
//      Used to generate the unique nonce for each encryption.
//
// 6. **AesGcm** (from System.Security.Cryptography):
//    - The .NET implementation of AES‑GCM.  It is `IDisposable`, so we
//      use `using var` to ensure cleanup.
//
// 7. **Encoding.ASCII.GetString / GetBytes**:
//    - Converts between the 32‑char hex key ID and its ASCII bytes.
//      ASCII is safe because hex characters are in the ASCII range.
//
// 8. **Buffer.BlockCopy**:
//    - Efficiently copies blocks of bytes between arrays.
//
// 9. **Convert.ToBase64String / FromBase64String**:
//    - Converts between a byte array and a Base64 string, which is safe
//      for storage in text‑based columns (database, JSON).
//
// 10. **switch** on version byte:
//     - Routes decryption to the correct handler.  Future versions can
//       be added without changing the existing logic.
//
// 11. **CryptographicException**:
//     - Thrown when data has been tampered with or the version is
//       unrecognised.  This is the standard .NET exception for crypto
//       failures.
//
// 12. **sealed** modifier:
//     - Prevents subclassing, ensuring the encryption logic cannot be
//       accidentally overridden.
// ====================================================================

using System;
using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// AES‑256‑GCM encryption service with key rotation and tenant‑bound AAD.
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    // ----------------------------------------------------------------
    // Constants – define the binary packet format
    // ----------------------------------------------------------------

    private const byte Version = 0x01;      // current ciphertext version
    private const int TagSize = 16;         // AES‑GCM authentication tag (128 bits)
    private const int NonceSize = 12;       // GCM nonce (96 bits – optimal)
    private const int KeyIdHexSize = 32;    // hex string length of a Guid "N" format
    private const int MaxPayloadSize = 1_048_576; // 1 MB DoS limit

    // ----------------------------------------------------------------
    // Injected dependencies
    // ----------------------------------------------------------------

    private readonly IKeyProvider _keyProvider;
    private readonly IAadProvider _aadProvider;

    /// <summary>
    /// Initialises the service with a key provider and AAD provider.
    /// </summary>
    public AesGcmEncryptionService(IKeyProvider keyProvider, IAadProvider aadProvider)
    {
        _keyProvider = keyProvider;
        _aadProvider = aadProvider;
    }

    // ----------------------------------------------------------------
    // Public API – string‑based encryption/decryption
    // ----------------------------------------------------------------

    /// <inheritdoc />
    public string Encrypt(string plainText, EncryptionContext context)
    {
        // Convert the UTF‑16 string to UTF‑8 bytes.
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Delegate to the core binary encryption.
        byte[] cipherPacket = Encrypt(plainBytes, context);

        // Encode the binary packet as Base64 for safe storage/transport.
        return Convert.ToBase64String(cipherPacket);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText, EncryptionContext context)
    {
        // Decode the Base64 string back into the binary packet.
        byte[] payload = Convert.FromBase64String(cipherText);

        // Delegate to the core binary decryption.
        byte[] plainBytes = Decrypt(payload, context);

        // Convert the UTF‑8 bytes back to a .NET string.
        return Encoding.UTF8.GetString(plainBytes);
    }

    // ----------------------------------------------------------------
    // Core binary encryption
    // ----------------------------------------------------------------

    private byte[] Encrypt(byte[] plainBytes, EncryptionContext context)
    {
        // --- 1. Obtain the current encryption key + its hex ID ---
        ReadOnlySpan<byte> keySpan = _keyProvider.CurrentKey.Span;
        string currentKeyIdHex = _keyProvider.CurrentKeyId;   // 32‑char hex string

        // --- 2. Build the AAD from the encryption context ---
        byte[] aad = _aadProvider.BuildAad(context, Version).ToArray();

        // --- 3. Generate a fresh random nonce ---
        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // --- 4. Encrypt with AES‑GCM ---
        byte[] ciphertext = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aesGcm = new AesGcm(keySpan, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag, aad);

        // --- 5. Assemble the binary packet ---
        // [version: 1] [keyIdHex: 32] [nonce: 12] [ciphertext: N] [tag: 16]

        // Convert the current key ID to ASCII bytes (32 bytes).
        byte[] keyIdBytes = Encoding.ASCII.GetBytes(currentKeyIdHex);

        byte[] packet = new byte[1 + KeyIdHexSize + NonceSize + ciphertext.Length + TagSize];
        int offset = 0;

        packet[offset++] = Version;
        Buffer.BlockCopy(keyIdBytes, 0, packet, offset, KeyIdHexSize);
        offset += KeyIdHexSize;
        Buffer.BlockCopy(nonce, 0, packet, offset, NonceSize);
        offset += NonceSize;
        Buffer.BlockCopy(ciphertext, 0, packet, offset, ciphertext.Length);
        offset += ciphertext.Length;
        Buffer.BlockCopy(tag, 0, packet, offset, TagSize);

        return packet;
    }

    // ----------------------------------------------------------------
    // Core binary decryption
    // ----------------------------------------------------------------

    private byte[] Decrypt(byte[] payload, EncryptionContext context)
    {
        // --- 1. DoS guard – reject excessively large payloads ---
        if (payload.Length > MaxPayloadSize)
            throw new ArgumentException(
                $"Payload size ({payload.Length} bytes) exceeds maximum ({MaxPayloadSize} bytes).");

        // Minimum size: version(1) + keyId(32) + nonce(12) + tag(16) = 61 bytes.
        if (payload.Length < 1 + KeyIdHexSize + NonceSize + TagSize)
            throw new ArgumentException("Invalid ciphertext format.");

        // --- 2. Version check + routing ---
        byte version = payload[0];
        switch (version)
        {
            case Version:
                return DecryptV1(payload, context, version);
            default:
                throw new CryptographicException($"Unsupported ciphertext version: {version}.");
        }
    }

    // ----------------------------------------------------------------
    // Version 1 decryption
    // ----------------------------------------------------------------

    private byte[] DecryptV1(byte[] payload, EncryptionContext context, byte version)
    {
        int offset = 1;

        // --- 1. Extract key ID (32 ASCII hex bytes) ---
        string keyIdHex = Encoding.ASCII.GetString(payload, offset, KeyIdHexSize);
        offset += KeyIdHexSize;

        // --- 2. Resolve the key ---
        if (!_keyProvider.TryGetKey(keyIdHex, out ReadOnlyMemory<byte> keyMemory))
            throw new CryptographicException($"Unknown key ID: {keyIdHex}.");

        ReadOnlySpan<byte> keySpan = keyMemory.Span;

        // --- 3. Build the AAD from the context and version ---
        byte[] aad = _aadProvider.BuildAad(context, version).ToArray();

        // --- 4. Extract nonce ---
        byte[] nonce = new byte[NonceSize];
        Buffer.BlockCopy(payload, offset, nonce, 0, NonceSize);
        offset += NonceSize;

        // --- 5. Extract ciphertext ---
        int cipherLen = payload.Length - offset - TagSize;
        if (cipherLen < 0)
            throw new ArgumentException("Invalid ciphertext format.");

        byte[] ciphertext = new byte[cipherLen];
        Buffer.BlockCopy(payload, offset, ciphertext, 0, cipherLen);
        offset += cipherLen;

        // --- 6. Extract authentication tag ---
        byte[] tag = new byte[TagSize];
        Buffer.BlockCopy(payload, offset, tag, 0, TagSize);

        // --- 7. Decrypt with AES‑GCM ---
        byte[] plaintext = new byte[cipherLen];
        using var aesGcm = new AesGcm(keySpan, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);

        return plaintext;
    }
}





//// SETUP
//var keyProvider = new KeyProvider(encryptionOptions);      // loaded from config
//var aadProvider = new ContextAadProvider();                // stateless
//var service = new AesGcmEncryptionService(keyProvider, aadProvider);

//// ENCRYPT
//var context = new EncryptionContext("tenant-1", "User", "Email");
//string ciphertext = service.Encrypt("user@example.com", context);
//// ciphertext = "AQphM2YxYjJjNGQ1Z..." (Base64)

//// DECRYPT
//string plaintext = service.Decrypt(ciphertext, context);
//// plaintext = "user@example.com"

//// TAMPERING DETECTION
//string tampered = ciphertext.Substring(1); // change first character
//service.Decrypt(tampered, context);        // throws CryptographicException
