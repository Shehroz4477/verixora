// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / Ulid.cs
// ====================================================================
// Summary:
//   Immutable 128-bit ULID (Universally Unique Lexicographically
//   Sortable Identifier).  Self‑contained; no external dependencies.
//
//   Format:
//     - 26‑character Crockford Base32 string (canonical).
//     - 16 raw bytes for efficient storage (PostgreSQL BYTEA / UUID).
//
//   Encoding detail (per ULID spec):
//     128 bits + 2 leading zeros → 130 bits → 26 groups of 5.
//     The first character carries only 3 data bits; the top 2 are
//     implicitly zero.  This keeps the string 26 chars exactly.
//
//   Why a class:
//     A struct's default would produce a null internal array.
//     A sealed class with private constructor guarantees every
//     instance is valid – essential for domain identity.
//
//   FUTURE OPTIMISATION:
//     For dictionary‑heavy workloads (Identity, Auth, AuditLogs),
//     consider caching the hash code in a private field to avoid
//     recomputation.  This is not needed at current scale.
// ====================================================================

using System.Security.Cryptography;

namespace SharedKernel.Domain.Base;

public sealed class Ulid : IEquatable<Ulid>, IComparable<Ulid>
{
    // Crockford Base32 alphabet – excludes I, L, O, U to prevent
    // confusion with digits 1 and 0 in human‑readable contexts.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    // Lazy‑initialised lookup table for O(1) decoding.
    // Lazy<T> avoids static initialisation ordering risks in the
    // large 73‑project modular monolith.
    private static readonly Lazy<int[]> DecodeMap = new(BuildDecodeMap);

    private static int[] BuildDecodeMap()
    {
        var map = new int[128];
        Array.Fill(map, -1);
        for (int i = 0; i < Alphabet.Length; i++)
        {
            map[Alphabet[i]] = i;
            // Also map lowercase → same digit (case‑insensitive parse).
            char lower = char.ToLowerInvariant(Alphabet[i]);
            if (lower != Alphabet[i])
                map[lower] = i;
        }
        return map;
    }

    private readonly byte[] _bytes; // 16 bytes, never null

    // ---- Private constructor ----
    private Ulid(byte[] bytes)
    {
        // Defensive copy – caller cannot mutate our identity.
        _bytes = (byte[])bytes.Clone();
    }

    // ================================================================
    // Factory Methods
    // ================================================================

    /// <summary>
    /// Creates a new ULID with the current UTC timestamp (48‑bit,
    /// milliseconds) and 80 bits of cryptographic randomness.
    /// </summary>
    public static Ulid NewUlid()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        byte[] random = new byte[10];
        RandomNumberGenerator.Fill(random);

        byte[] bytes = new byte[16];
        // Timestamp: big‑endian into bytes [0..5]
        bytes[0] = (byte)(ts >> 40);
        bytes[1] = (byte)(ts >> 32);
        bytes[2] = (byte)(ts >> 24);
        bytes[3] = (byte)(ts >> 16);
        bytes[4] = (byte)(ts >> 8);
        bytes[5] = (byte)ts;
        // Random: bytes [6..15]
        Array.Copy(random, 0, bytes, 6, 10);

        return new Ulid(bytes);
    }

    /// <summary>
    /// Rehydrates a ULID from a 16‑byte array (e.g., database value).
    /// The array is copied defensively.
    /// </summary>
    public static Ulid FromBytes(byte[] bytes)
    {
        if (bytes is null || bytes.Length != 16)
            throw new ArgumentException("ULID requires exactly 16 bytes.", nameof(bytes));
        return new Ulid(bytes);
    }

    // ================================================================
    // Parse (strict domain method, case‑insensitive)
    // ================================================================
    /// <summary>
    /// Parses a 26‑character Crockford Base32 ULID string.
    /// The input is accepted in either upper‑ or lower‑case; the
    /// domain layer normalises it internally to avoid fragile
    /// dependencies on the caller.
    /// Throws <see cref="FormatException"/> if the string is invalid.
    /// </summary>
    public static Ulid Parse(string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));
        if (input.Length != 26)
            throw new FormatException("ULID string must be exactly 26 characters.");

        var map = DecodeMap.Value;  // Lazy initialised lookup table
        byte[] bytes = new byte[16];
        int bitBuffer = 0;
        int bitsReady = 0;
        int byteIndex = 0;

        for (int i = 0; i < 26; i++)
        {
            char c = input[i];
            // Only ASCII characters are valid; our map has 128 entries.
            int digit = c < 128 ? map[c] : -1;
            if (digit < 0)
                throw new FormatException($"Invalid ULID character: '{input[i]}' at position {i}.");

            // First character must have its top 2 bits zero (digit < 8).
            if (i == 0 && digit >= 8)
                throw new FormatException("ULID first character must be 0–7 (top 2 bits zero).");

            bitBuffer = (bitBuffer << 5) | digit;
            bitsReady += 5;

            while (bitsReady >= 8 && byteIndex < 16)
            {
                bitsReady -= 8;
                bytes[byteIndex++] = (byte)(bitBuffer >> bitsReady);
            }
        }

        if (byteIndex != 16)
            throw new FormatException("ULID string did not decode to 16 bytes.");

        return new Ulid(bytes);
    }

    // ---- Conversions ----

    /// <summary>
    /// Returns a DEFENSIVE COPY of the 16‑byte representation.
    /// </summary>
    public byte[] ToByteArray() => (byte[])_bytes.Clone();

    /// <summary>
    /// Returns a read‑only view of the internal bytes without copying.
    /// Use for zero‑allocation access in hot paths (EF Core, caching).
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => _bytes;

    // ---- Equality ----
    public bool Equals(Ulid? other)
    {
        if (other is null) return false;
        for (int i = 0; i < 16; i++)
            if (_bytes[i] != other._bytes[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Ulid other && Equals(other);

    // ---- Hash Code (architecture‑independent, deterministic) ----
    /// <summary>
    /// Polynomial byte‑fold hash.  Uses a classic 31‑multiplier over
    /// all 16 bytes.  This is deterministic on all CPU architectures
    /// (no endian dependency) and produces a well‑distributed 32‑bit
    /// hash for dictionaries and caches.
    /// </summary>
    public override int GetHashCode()
    {
        // unchecked to avoid overflow exceptions – we rely on modulo 2³².
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < 16; i++)
                hash = (hash * 31) ^ _bytes[i];
            return hash;
        }
    }

    public static bool operator ==(Ulid? l, Ulid? r) => l?.Equals(r) ?? r is null;
    public static bool operator !=(Ulid? l, Ulid? r) => !(l == r);

    // ---- Comparison (lexicographic by bytes = timestamp order) ----
    public int CompareTo(Ulid? other)
    {
        if (other is null) return 1;
        for (int i = 0; i < 16; i++)
        {
            if (_bytes[i] < other._bytes[i]) return -1;
            if (_bytes[i] > other._bytes[i]) return 1;
        }
        return 0;
    }

    // ---- String Representation (Crockford Base32, 26 chars) ----
    /// <summary>
    /// Returns the canonical 26‑character Crockford Base32 string.
    /// </summary>
    public override string ToString()
    {
        char[] result = new char[26];
        int idx = 0;
        int buffer = 0;
        int bitsReady = 2;   // 2 leading zero bits

        for (int i = 0; i < 16; i++)
        {
            buffer = (buffer << 8) | _bytes[i];
            bitsReady += 8;

            while (bitsReady >= 5)
            {
                bitsReady -= 5;
                result[idx++] = Alphabet[(buffer >> bitsReady) & 0x1F];
            }
        }

        // Safety guard: ensure encoding produced exactly 26 characters.
        if (idx != 26)
            throw new InvalidOperationException("ULID encoding produced incorrect number of characters.");

        return new string(result);
    }
}
