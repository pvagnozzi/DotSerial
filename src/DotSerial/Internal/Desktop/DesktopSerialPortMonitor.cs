// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Desktop (Windows, Linux, macOS) implementation of ISerialPortMonitor
//     using periodic polling of System.IO.Ports.SerialPort.GetPortNames().
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Desktop;

using Microsoft.Extensions.Logging;

/// <summary>
/// Desktop (Windows, Linux, macOS) implementation of <see cref="Abstractions.ISerialPortMonitor"/>
/// that polls <see cref="System.IO.Ports.SerialPort.GetPortNames"/> at a configurable
/// interval and raises <see cref="Abstractions.ISerialPortMonitor.PortsChanged"/> when the
/// set of available ports changes.
/// </summary>
internal sealed class DesktopSerialPortMonitor : Abstractions.ISerialPortMonitor
{
    private readonly TimeSpan _pollingInterval;
    private readonly ILogger<DesktopSerialPortMonitor> _logger;

    private volatile IReadOnlyList<string> _currentPorts;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="DesktopSerialPortMonitor"/> that scans every
    /// <paramref name="pollingInterval"/> for serial port changes.
    /// </summary>
    /// <param name="pollingInterval">How often to poll for port changes. Must be positive.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pollingInterval"/> is not positive.
    /// </exception>
    internal DesktopSerialPortMonitor(
        TimeSpan pollingInterval,
        ILogger<DesktopSerialPortMonitor> logger)
    {
        if (pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pollingInterval),
                "Polling interval must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(logger);

        _pollingInterval = pollingInterval;
        _logger = logger;
        _currentPorts = ScanPorts();
    }

    /// <inheritdoc/>
    public event EventHandler<Models.PortsChangedEventArgs>? PortsChanged;

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrentPorts => _currentPorts;

    /// <inheritdoc/>
    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _monitorTask = RunMonitorLoopAsync(_cts.Token);

        _logger.MonitorStarted(_pollingInterval.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (_cts is null) return;

        _logger.MonitorStopping();
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _monitorTask = null;
    }

    // ── Monitor loop ──────────────────────────────────────────────────────

    /// <summary>
    /// Background loop that polls <see cref="System.IO.Ports.SerialPort.GetPortNames"/>
    /// at the configured interval and fires <see cref="PortsChanged"/> on differences.
    /// </summary>
    private async Task RunMonitorLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, ct).ConfigureAwait(false);

                var newPorts = ScanPorts();
                var previous = _currentPorts;

                var added = newPorts.Except(previous).ToList();
                var removed = previous.Except(newPorts).ToList();

                if (added.Count > 0 || removed.Count > 0)
                {
                    _currentPorts = newPorts;

                    _logger.MonitorPortsChanged(string.Join(", ", added), string.Join(", ", removed));

                    RaisePortsChanged(added, removed, newPorts);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.MonitorPollError(ex);
            }
        }

        _logger.MonitorStopped();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Retrieves the current list of port names, sorted for stable comparison.</summary>
    private static IReadOnlyList<string> ScanPorts()
        => System.IO.Ports.SerialPort.GetPortNames()
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Raises <see cref="PortsChanged"/> safely, catching and logging any subscriber exceptions.
    /// </summary>
    private void RaisePortsChanged(
        IReadOnlyList<string> added,
        IReadOnlyList<string> removed,
        IReadOnlyList<string> all)
    {
        var handler = PortsChanged;
        if (handler is null) return;

        var args = new Models.PortsChangedEventArgs(added, removed, all);
        foreach (var d in handler.GetInvocationList().Cast<EventHandler<Models.PortsChangedEventArgs>>())
        {
            try
            {
                d.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.MonitorSubscriberError(ex);
            }
        }
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> when already disposed.</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DesktopSerialPortMonitor));
    }
}
