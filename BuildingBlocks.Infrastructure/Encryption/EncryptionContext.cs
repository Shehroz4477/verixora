// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionContext.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   An immutable record that carries the **binding metadata** for
//   authenticated encryption.  It tells the AAD provider *which*
//   tenant, entity type, and purpose the ciphertext belongs to.
//   Because AES‑GCM uses this to verify the ciphertext, the same
//   context MUST be used for both encryption and decryption.
//
//   Why a record:
//     - `record` (C# 9+) is a reference type designed for immutable
//       data.  It provides value‑based equality (two records with the
//       same values are equal) and a `with` expression to create a
//       modified copy without changing the original.
//     - `sealed` prevents further inheritance.
//
//   Why immutable:
//     - The encryption service must be guaranteed that the context
//       does not change between encryption and decryption.  An
//       immutable type enforces this at the compiler level.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+):
//    - A reference type that provides value‑based equality,
//      immutability, and a concise syntax for defining data types.
//
// 2. **sealed** modifier:
//    - Prevents other records/classes from inheriting from this one.
//
// 3. **Primary constructor** (C# 12):
//    - The parameters `string TenantId, string EntityType, string Purpose`
//      are declared directly in the type definition.  They become
//      public properties automatically.
//
// 4. **string** (immutable reference type):
//    - Each field is a string, which is itself immutable in .NET.
//      This means the entire object graph is deeply immutable.
//
// 5. **namespace** declaration:
//    - Organises types.  This record lives in the encryption
//      infrastructure namespace because it is consumed by the
//      crypto layer.
// ====================================================================

namespace BuildingBlocks.Infrastructure.Encryption;

/// <summary>
/// Immutable context for authenticated encryption binding.
/// </summary>
public sealed record EncryptionContext(
    string TenantId,
    string EntityType,
    string Purpose)
{



    //// Create a context for encrypting a user's email:
    //var ctx = new EncryptionContext("tenant-1", "User", "Email");

    //// The same context must be used for decryption.
    //// Because it's an immutable record, you can create a modified copy
    //// without changing the original:
    //var ctx2 = ctx with { Purpose = "PhoneNumber" };
    //// ctx is still "Email", ctx2 is "PhoneNumber".
};
