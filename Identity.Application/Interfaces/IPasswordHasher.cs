// ====================================================================
// VERIXORA – Identity.Application / Interfaces / IPasswordHasher.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for hashing and verifying passwords using
//   Argon2id.  This interface lives in the Application layer and
//   is implemented in the Infrastructure layer, following the
//   Dependency Inversion Principle.
//
//   WHY A SEPARATE ABSTRACTION:
//     - Password hashing is a security‑critical operation that
//       should be decoupled from the application logic.
//     - The implementation may change over time (e.g., switching
//       from Argon2id to bcrypt, or adjusting memory parameters).
//     - Enables unit testing of command handlers without actually
//       running the expensive hashing algorithm.
//     - The raw password is never stored; only the hash is
//       persisted in the database.
//
//   WHY ARGON2ID:
//     - Argon2id is the winner of the Password Hashing Competition
//       (2015) and is recommended by OWASP.
//     - It is memory‑hard, making it resistant to GPU‑based
//       brute‑force attacks.
//     - It combines Argon2d (data‑dependent, resistant to GPU)
//       and Argon2i (data‑independent, resistant to side‑channel).
//
//   WHY ASYNC:
//     - Argon2id is intentionally slow and computationally
//       expensive.  Running it synchronously would block the
//       current thread, reducing the server's ability to handle
//       other requests.
//     - `HashAsync` returns `Task<string>`, allowing the thread
//       to be freed while the hashing runs.
//
//   METHODS:
//     - HashAsync    – hash a plain‑text password
//     - VerifyAsync  – compare a plain‑text password against a hash
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide both methods.
//
// 2. **public** access modifier:
//    - The interface is accessible from any referencing project.
//
// 3. **Task<string>** return type:
//    - `HashAsync` returns the hashed password string asynchronously.
//      The hash typically looks like:
//        $argon2id$v=19$m=65536,t=3,p=4$salt$hash
//
// 4. **Task<bool>** return type:
//    - `VerifyAsync` returns true if the password matches the hash,
//      false otherwise.  This is a constant‑time comparison to
//      prevent timing attacks.
//
// 5. **CancellationToken** parameter:
//    - Allows the caller to cancel the hashing operation if the
//      request times out or the server shuts down.
//
// 6. **namespace** declaration:
//    - `Identity.Application.Interfaces` places this in the
//      Application layer's abstraction space.
// ====================================================================

using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

/// <summary>
/// Contract for hashing and verifying passwords using Argon2id.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain‑text password using Argon2id.
    /// The returned string contains the algorithm parameters, salt,
    /// and hash in a standard format (e.g., PHC string format).
    /// </summary>
    /// <param name="password">
    /// The plain‑text password to hash.  Must not be null or empty.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The hashed password string, including embedded salt and
    /// parameters.
    /// </returns>
    Task<string> HashAsync(string password, CancellationToken ct = default);

    /// <summary>
    /// Verifies a plain‑text password against a previously stored
    /// hash.  Uses constant‑time comparison to prevent timing
    /// attacks.
    /// </summary>
    /// <param name="password">The plain‑text password to check.</param>
    /// <param name="hash">
    /// The stored hash string (from <see cref="HashAsync"/>).
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the password matches the hash; otherwise
    /// <c>false</c>.
    /// </returns>
    Task<bool> VerifyAsync(string password, string hash, CancellationToken ct = default);
}

////Dry‑run — how the password hasher is used in different scenarios:
//// ====================================================================
//// SCENARIO 1: Registration — Hash the user's password
//// ====================================================================
//// The handler calls:
//var passwordHash = await _passwordHasher.HashAsync("s3cr3t!", ct);

//    // What happens internally (in the Infrastructure implementation):
//    //   1. Generate a 16‑byte cryptographically random salt.
//    //   2. Run Argon2id with parameters:
//    //        - Memory: 65536 KB (64 MB)
//    //        - Iterations: 3
//    //        - Parallelism: 4
//    //   3. Produce a 32‑byte hash.
//    //   4. Encode the result in the PHC string format.

//    // Return value:
//    //   "$argon2id$v=19$m=65536,t=3,p=4$abc123def456...$hashbytes..."
//    //
//    // This string is stored in User.PasswordHash.
//    // The raw password "s3cr3t!" is discarded immediately.


//    // ====================================================================
//    // SCENARIO 2: Login — Verify the user's password
//    // ====================================================================
//    // The handler loads the user from the repository:
//    var user = await _users.GetByEmailAsync("alice@example.com", ct);

//    // The handler calls:
//    bool isValid = await _passwordHasher.VerifyAsync(
//        "s3cr3t!",                    // plain‑text from login form
//        user.PasswordHash,            // stored hash from database
//        ct);

//    // What happens internally:
//    //   1. Parse the PHC string to extract algorithm, parameters, salt.
//    //   2. Run Argon2id with the EXACT same parameters as during HashAsync.
//    //   3. Compute the hash of "s3cr3t!" with the extracted salt.
//    //   4. Compare the result with the stored hash using a constant‑time
//    //      comparison (no early exit — prevents timing attacks).

//    // Result:
//    //   true  → password is correct, login proceeds
//    //   false → password is wrong, handler returns Result.Failure(...)


//    // ====================================================================
//    // SCENARIO 3: Password change — Hash the new password
//    // ====================================================================
//    // The handler calls:
//    var newHash = await _passwordHasher.HashAsync("newS3cr3t!", ct);

//// Then updates the user:
//user.ChangePassword(newHash);

//// And persists:
//await _unitOfWork.SaveChangesAsync(ct);


//// ====================================================================
//// WHY ASYNC MATTERS (performance comparison):
//// ====================================================================
//// Synchronous version (BAD):
////   var hash = _passwordHasher.Hash("s3cr3t!");
////   → Blocks the current thread for ~200ms while Argon2id runs.
////   → During that time, the thread cannot handle other requests.
////   → Under load, the thread pool can be exhausted.
////
//// Asynchronous version (GOOD):
////   var hash = await _passwordHasher.HashAsync("s3cr3t!", ct);
////   → The thread is freed while the hashing runs on a background
////     thread or uses a hardware‑accelerated implementation.
////   → The thread can handle other requests in the meantime.
////   → When hashing completes, execution resumes.
