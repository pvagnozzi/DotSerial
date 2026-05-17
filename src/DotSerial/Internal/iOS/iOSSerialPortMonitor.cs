// -----------------------------------------------------------------------
// <copyright file="iOSSerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     iOS ISerialPortMonitor implementation using ExternalAccessory connect/disconnect notifications.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

#if IOS
namespace DotSerial.Internal.iOS;

using ExternalAccessory;
using Foundation;
using Microsoft.Extensions.Logging;

/// <summary>
/// iOS implementation of <see cref="Abstractions.ISerialPortMonitor"/> that subscribes to
/// <see cref="EAAccessoryManager.DidConnectNotification"/> and
/// <see cref="EAAccessoryManager.DidDisconnectNotification"/> notifications from the
/// External Accessory framework to detect MFi accessory additions and removals in real time.
/// </summary>
/// <remarks>
/// <para>
/// Port names are derived from the accessory serial number (e.g. <c>"EXT12345678"</c>)
/// as reported by <see cref="EAAccessory.SerialNumber"/>, with connected accessories
/// sorted by serial number for a stable ordering.
/// </para>
/// <para>
/// <b>Prerequisites:</b> <c>Info.plist</c> must declare the protocol strings in
/// <c>UISupportedExternalAccessoryProtocols</c>.
/// </para>
/// </remarks>
internal sealed class iOSSerialPortMonitor : Abstractions.ISerialPortMonitor
{
    private readonly ILogger<iOSSerialPortMonitor> _logger;
    private volatile IReadOnlyList<string> _currentPorts;
    private NSObject? _connectObserver;
    private NSObject? _disconnectObserver;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="iOSSerialPortMonitor"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    internal iOSSerialPortMonitor(ILogger<iOSSerialPortMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _currentPorts = ScanPorts();
    }

    /// <inheritdoc/>
    public event EventHandler<Models.PortsChangedEventArgs>? PortsChanged;

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrentPorts => _currentPorts;

    /// <inheritdoc/>
    public bool IsRunning => _connectObserver is not null;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        if (IsRunning)
        {
            return;
        }

        // Begin generating connect/disconnect notifications.
        EAAccessoryManager.SharedAccessoryManager.RegisterForLocalNotifications();

        _connectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            new NSString(EAAccessoryManager.DidConnectNotification),
            OnAccessoryEvent,
            null);

        _disconnectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            new NSString(EAAccessoryManager.DidDisconnectNotification),
            OnAccessoryEvent,
            null);

        _logger.MonitorStarted();
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
        if (_connectObserver is null)
        {
            return;
        }

        _logger.MonitorStopping();

        NSNotificationCenter.DefaultCenter.RemoveObserver(_connectObserver);
        NSNotificationCenter.DefaultCenter.RemoveObserver(_disconnectObserver!);
        _connectObserver = null;
        _disconnectObserver = null;

        EAAccessoryManager.SharedAccessoryManager.UnregisterForLocalNotifications();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    // ── Notification handler ──────────────────────────────────────────────

    private void OnAccessoryEvent(NSNotification notification)
    {
        var newPorts = ScanPorts();
        var previous = _currentPorts;

        var added = newPorts.Except(previous).ToList();
        var removed = previous.Except(newPorts).ToList();

        if (added.Count == 0 && removed.Count == 0)
        {
            return;
        }

        _currentPorts = newPorts;
        _logger.MonitorPortsChanged(string.Join(", ", added), string.Join(", ", removed));
        RaisePortsChanged(added, removed, newPorts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Returns a stable list of currently connected accessory serial numbers.</summary>
    private static IReadOnlyList<string> ScanPorts()
    {
        var accessories = EAAccessoryManager.SharedAccessoryManager.ConnectedAccessories;
        if (accessories is null || accessories.Length == 0)
        {
            return Array.Empty<string>();
        }

        return accessories
            .Select(a => a.SerialNumber)
            .Where(s => !string.IsNullOrEmpty(s))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Safely raises <see cref="PortsChanged"/>, catching and logging any subscriber exceptions.</summary>
    private void RaisePortsChanged(
        IReadOnlyList<string> added,
        IReadOnlyList<string> removed,
        IReadOnlyList<string> all)
    {
        var handler = PortsChanged;
        if (handler is null)
        {
            return;
        }

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
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(iOSSerialPortMonitor));
        }
    }
}
#endif
