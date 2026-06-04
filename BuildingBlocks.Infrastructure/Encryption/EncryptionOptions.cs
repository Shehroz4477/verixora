// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionOptions.cs
// ====================================================================
// Summary:
//   Configuration options for the AES‑256 encryption service.
//   The encryption key must come from a secrets manager (ADR‑031).
//
//   Validation:
//     - Key is required and must be a valid base64‑encoded 256‑bit value.
//     - IV is optional; if provided, must be a valid base64‑encoded 128‑bit value.
//     - Startup‑time enforcement is done in ApiHost via .ValidateOnStart().
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Infrastructure.Encryption;

public class EncryptionOptions
{
    /// <summary>
    /// The base64‑encoded 256‑bit AES key (32 bytes when decoded).
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Optional base64‑encoded 128‑bit initialisation vector (16 bytes when decoded).
    /// If not provided, a random IV is generated per operation.
    /// </summary>
    public string? IV { get; set; }

    /// <summary>
    /// Validates the options after binding.
    /// Throws <see cref="InvalidOperationException"/> if the key or IV is invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("EncryptionOptions: Key is required.");

        // Safe base64 decoding – catches FormatException and gives a clear message.
        _ = DecodeBase64(Key, "Key", 32);

        if (!string.IsNullOrWhiteSpace(IV))
            _ = DecodeBase64(IV, "IV", 16);
    }

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
