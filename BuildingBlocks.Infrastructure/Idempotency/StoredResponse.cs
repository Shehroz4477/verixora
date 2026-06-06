using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Infrastructure.Idempotency;
/// <summary>
/// Lightweight DTO for the stored response.
/// </summary>
internal sealed class StoredResponse
{
    public int StatusCode { get; }
    public string Body { get; }
    public string ContentType { get; }

    public StoredResponse(int statusCode, string body, string contentType)
    {
        StatusCode = statusCode;
        Body = body;
        ContentType = contentType;
    }
}
