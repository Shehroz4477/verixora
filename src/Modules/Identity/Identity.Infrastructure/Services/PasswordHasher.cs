// ====================================================================
// VERIXORA – Identity.Infrastructure / Services / PasswordHasher.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IPasswordHasher"/> using the Argon2id
//   algorithm from the Konscious.Security.Cryptography.Argon2
//   library.  Argon2id is the winner of the Password Hashing
//   Competition (2015) and is recommended by OWASP.
//
//   WHY ARGON2ID:
//     - Memory‑hard: requires a configurable amount of RAM, making
//       GPU‑based brute‑force attacks expensive.
//     - Combines Argon2d (data‑dependent, GPU‑resistant) and
//       Argon2i (data‑independent, side‑channel resistant).
//     - Configurable parallelism, memory cost, and iteration count.
//
//   WHY ASYNC:
//     - Argon2id is intentionally slow and CPU‑bound.
//     - Running it via `Task.Run` keeps the calling thread free for
//       other requests.
//
//   CONFIGURATION (OWASP recommended minimums):
//     - DegreeOfParallelism = 4
//     - MemorySize = 65536 KB (64 MB)
//     - Iterations = 3
//
//   HASH FORMAT:
//     The returned string is a base64‑encoded concatenation of the
//     salt and the hash, separated by a dot:
//       "{salt}.{hash}"
//     This keeps the implementation simple and avoids external
//     format dependencies.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `PasswordHasher : IPasswordHasher` guarantees the two methods
//      are provided.
//
// 2. **Argon2id** (from Konscious.Security.Cryptography.Argon2):
//    - A managed wrapper around the reference Argon2 implementation.
//
// 3. **RandomNumberGenerator** (System.Security.Cryptography):
//    - A cryptographically secure random number generator.  Used to
//      create a unique 16‑byte salt for each password.
//
// 4. **Task.Run**:
//    - Offloads CPU‑bound work to a background thread, allowing the
//      calling async method to return a Task.
//
// 5. **Convert.ToBase64String / FromBase64String**:
//    - Encodes/decodes binary data as a text string safe for storage.
//
// 6. **Constant‑time comparison**:
//    - We use `CryptographicOperations.FixedTimeEquals` to compare
//      hash bytes.  This prevents timing attacks where an attacker
//      measures how long it takes to reject a wrong password.
//
// 7. **sealed** modifier:
//    - Prevents inheritance.
// ====================================================================

using System.Security.Cryptography;
using Identity.Application.Interfaces;
using Konscious.Security.Cryptography;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Hashes and verifies passwords using Argon2id.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    // OWASP recommended parameters for Argon2id
    private const int DegreeOfParallelism = 4;
    private const int MemorySize = 65536;  // KB (64 MB)
    private const int Iterations = 3;
    private const int SaltSize = 16;       // bytes

    /// <inheritdoc />
    public Task<string> HashAsync(string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        // Offload the CPU‑bound hashing to a background thread.
        return Task.Run(() =>
        {
            // Generate a cryptographically random salt.
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Compute the Argon2id hash.
            var hashBytes = HashPassword(password, salt);

            // Combine salt and hash into a single string.
            // Format: "{base64salt}.{base64hash}"
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hashBytes)}";
        }, ct);
    }

    /// <inheritdoc />
    public Task<bool> VerifyAsync(string password, string hash, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

        return Task.Run(() =>
        {
            // Split the stored hash into salt and hash parts.
            var parts = hash.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);

            // Compute the hash of the provided password with the same salt.
            var computedHash = HashPassword(password, salt);

            // Compare in constant time to prevent timing attacks.
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }, ct);
    }

    /// <summary>
    /// Computes the Argon2id hash for the given password and salt.
    /// </summary>
    private static byte[] HashPassword(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        // GetBytes returns the raw hash bytes (32 bytes by default).
        return argon2.GetBytes(32);
    }
}

//Dry‑run — hashing and verifying a password:
// =========================================================================
// 1. HASHING (during registration)
// =========================================================================
//var hasher = new PasswordHasher();
    //var passwordHash = await hasher.HashAsync("s3cr3t!", ct);

    // passwordHash looks like:
    //   "abc123def456... . xyz789uvw012..."
    //   ^-- 16‑byte salt (base64)    ^-- 32‑byte hash (base64)

    // Store passwordHash in the database (User.PasswordHash).


    // =========================================================================
    // 2. VERIFYING (during login)
    // =========================================================================
    //bool isValid = await hasher.VerifyAsync("s3cr3t!", passwordHash, ct);

    // isValid = true  → password matches
    // isValid = false → wrong password or corrupted hash


    // =========================================================================
    // 3. WRONG PASSWORD
    // =========================================================================
    //bool isWrong = await hasher.VerifyAsync("wrong-password", passwordHash, ct);

// isWrong = false (constant‑time comparison prevents timing leaks)


// =========================================================================
// WHY ASYNC MATTERS:
// =========================================================================
// Without Task.Run:
//   var hash = hasher.Hash("s3cr3t!");
//   → Blocks the calling thread for ~200ms while Argon2id runs.
//   → Under load, the thread pool can be exhausted.
//
// With Task.Run:
//   var hash = await hasher.HashAsync("s3cr3t!", ct);
//   → The thread is freed while hashing runs on a background thread.
//   → The thread pool remains available for other requests.
