using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Infrastructure.Idempotency;

/// <summary>
/// Internal persistence model – not exposed outside the store.
/// </summary>
internal class IdempotencyRecord
{
    /// <summary>The client‑supplied idempotency key (unique).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The original response body (null until command completes).</summary>
    public string? Response { get; set; }

    /// <summary>When the record was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the record expires (24 hours after creation).</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
