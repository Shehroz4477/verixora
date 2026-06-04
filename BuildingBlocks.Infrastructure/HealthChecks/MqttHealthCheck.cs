// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / HealthChecks / MqttHealthCheck.cs
// ====================================================================
// Summary:
//   A simple TCP‑level health check for the MQTT broker.
//   It attempts to open a TCP connection to the broker address and
//   immediately closes it.  This verifies network reachability
//   without performing an MQTT protocol handshake.
//
//   Design rules:
//     - Timeout is hard‑coded to 2 seconds to avoid blocking
//       Kubernetes probes.
//     - No retries – the health check must fail fast.
//     - Does not depend on any MQTT client library.
//     - Connection does not use the CancellationToken because
//       TcpClient.ConnectAsync may ignore it; instead we rely on
//       a timeout via Task.WhenAny and a delay.
// ====================================================================

using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Infrastructure.HealthChecks;

public class MqttHealthCheck : IHealthCheck
{
    private readonly string _brokerAddress;
    private readonly int _port;

    /// <summary>
    /// Creates a health check for the MQTT broker.
    /// </summary>
    /// <param name="brokerAddress">Address in the form "host:port" or "host" (default port 1883).</param>
    public MqttHealthCheck(string brokerAddress)
    {
        if (brokerAddress.Contains(':'))
        {
            var parts = brokerAddress.Split(':');
            _brokerAddress = parts[0];
            _port = int.TryParse(parts[1], out var p) ? p : 1883;
        }
        else
        {
            _brokerAddress = brokerAddress;
            _port = 1883;
        }
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            // Intentionally not passing cancellationToken – the token is
            // often ignored by the underlying socket implementation.
            var connectTask = tcpClient.ConnectAsync(_brokerAddress, _port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                return HealthCheckResult.Unhealthy(
                    "MQTT broker connection timed out.",
                    data: new Dictionary<string, object>
                    {
                        ["host"] = _brokerAddress,
                        ["port"] = _port
                    });
            }

            // Observe any exceptions from the connection task.
            await connectTask;
            return HealthCheckResult.Healthy("MQTT broker is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MQTT broker is unreachable.",
                ex,
                data: new Dictionary<string, object>
                {
                    ["host"] = _brokerAddress,
                    ["port"] = _port
                });
        }
    }
}
