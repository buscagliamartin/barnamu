// BarnaMu Web - ServerStatusService.cs
// Probes the Connect server (44405) and each Game server port via raw TCP, with a short
// timeout. Results are cached for a few seconds so the status page is cheap to hit.

using System.Net.Sockets;
using BarnaMu.Web.Data;
using Microsoft.Extensions.Options;

namespace BarnaMu.Web.Services;

public class ServerStatusService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(800);

    private readonly BarnaMuOptions _options;
    private readonly ILogger<ServerStatusService> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _cachedAtUtc = DateTime.MinValue;
    private ServerStatusSnapshot _cached = ServerStatusSnapshot.Empty;

    public ServerStatusService(IOptions<BarnaMuOptions> options, ILogger<ServerStatusService> logger)
    {
        this._options = options.Value;
        this._logger = logger;
    }

    public async Task<ServerStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - this._cachedAtUtc < CacheDuration)
        {
            return this._cached;
        }

        await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (DateTime.UtcNow - this._cachedAtUtc < CacheDuration)
            {
                return this._cached;
            }

            var ports = new List<PortStatus>();
            ports.Add(await this.ProbeAsync(this._options.ConnectHost, this._options.ConnectPort, "Connect Server").ConfigureAwait(false));
            foreach (var port in this._options.GameServerPorts)
            {
                ports.Add(await this.ProbeAsync(this._options.GameServerHost, port, $"Game Server {port}").ConfigureAwait(false));
            }

            this._cached = new ServerStatusSnapshot(ports, DateTime.UtcNow);
            this._cachedAtUtc = DateTime.UtcNow;
            return this._cached;
        }
        finally
        {
            this._gate.Release();
        }
    }

    private async Task<PortStatus> ProbeAsync(string host, int port, string label)
    {
        if (string.IsNullOrWhiteSpace(host) || port <= 0)
        {
            return new PortStatus(label, host, port, false, "No host/port configured");
        }

        try
        {
            using var client = new TcpClient { ReceiveTimeout = (int)ProbeTimeout.TotalMilliseconds, SendTimeout = (int)ProbeTimeout.TotalMilliseconds };
            using var cts = new CancellationTokenSource(ProbeTimeout);
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            return new PortStatus(label, host, port, true, null);
        }
        catch (OperationCanceledException)
        {
            return new PortStatus(label, host, port, false, "timeout");
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(ex, "Probe failed for {Host}:{Port}", host, port);
            return new PortStatus(label, host, port, false, ex.GetType().Name);
        }
    }
}

public record PortStatus(string Label, string Host, int Port, bool IsOnline, string? Error);

public record ServerStatusSnapshot(IReadOnlyList<PortStatus> Ports, DateTime CheckedAtUtc)
{
    public static readonly ServerStatusSnapshot Empty = new(Array.Empty<PortStatus>(), DateTime.MinValue);

    public bool ConnectServerOnline => this.Ports.FirstOrDefault(p => p.Label.StartsWith("Connect"))?.IsOnline ?? false;

    public int GameServersOnline => this.Ports.Count(p => p.Label.StartsWith("Game") && p.IsOnline);

    public int GameServersTotal => this.Ports.Count(p => p.Label.StartsWith("Game"));
}
