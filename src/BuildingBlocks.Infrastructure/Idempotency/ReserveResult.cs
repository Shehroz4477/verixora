using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Infrastructure.Idempotency;
/// <summary>
/// Result of a reservation attempt.
/// </summary>
public enum ReserveResult
{
    /// <summary>Key was new or expired – caller owns execution.</summary>
    Acquired,

    /// <summary>A non‑expired record already exists – caller must skip.</summary>
    AlreadyExists,

    /// <summary>Reserved for future use (e.g., lease collision).</summary>
    Conflict
}
