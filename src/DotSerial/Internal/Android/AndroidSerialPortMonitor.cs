// -----------------------------------------------------------------------
// <copyright file="AndroidSerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Android ISerialPortMonitor implementation using USB attach/detach broadcasts.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Android;

using global::Android.App;
using global::Android.Content;
using global::Android.Hardware.Usb;
using Microsoft.Extensions.Logging;

/// <summary>
/// Android implementation of <see cref="Abstractions.ISerialPortMonitor"/> that listens to
/// <see cref="UsbManager.ActionUsbDeviceAttached"/> and
/// <see cref="UsbManager.ActionUsbDeviceDetached"/> broadcast intents to detect USB serial
/// port additions and removals in real time.
/// </summary>
/// <remarks>
/// <para>
/// Each attached USB device is exposed as a port name of the form <c>"USB&lt;index&gt;"</c>
/// (e.g. <c>"USB0"</c>, <c>"USB1"</c>) ordered by <see cref="UsbDevice.DeviceName"/>.
/// </para>
/// <para>
/// <b>Prerequisites:</b> the <c>android.hardware.usb.host</c> feature and the
/// <c>android.permission.USB_PERMISSION</c> must be declared in
/// <c>AndroidManifest.xml</c>.
/// </para>
/// </remarks>
internal sealed class AndroidSerialPortMonitor : Abstractions.ISerialPortMonitor
{
    private readonly ILogger<AndroidSerialPortMonitor> _logger;
    private volatile IReadOnlyList<string> _currentPorts;
    private UsbBroadcastReceiver? _receiver;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="AndroidSerialPortMonitor"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    internal AndroidSerialPortMonitor(ILogger<AndroidSerialPortMonitor> logger)
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
    public bool IsRunning => _receiver is not null;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        if (IsRunning)
        {
            return;
        }

        _receiver = new UsbBroadcastReceiver(this);

        var filter = new IntentFilter();
        filter.AddAction(UsbManager.ActionUsbDeviceAttached);
        filter.AddAction(UsbManager.ActionUsbDeviceDetached);

        Application.Context.RegisterReceiver(_receiver, filter);

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
        if (_receiver is null)
        {
            return;
        }

        _logger.MonitorStopping();

        try
        {
            Application.Context.UnregisterReceiver(_receiver);
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            // Receiver was never registered or already unregistered — ignore.
        }

        _receiver = null;
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

    // ── Internal: called by the broadcast receiver ────────────────────────

    /// <summary>Called by <see cref="UsbBroadcastReceiver"/> when a USB device is attached or detached.</summary>
    internal void OnUsbEvent()
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

    /// <summary>Returns a stable list of attached USB device names sorted by device name.</summary>
    private static IReadOnlyList<string> ScanPorts()
    {
        var usbManager = Application.Context.GetSystemService(Context.UsbService) as UsbManager;
        if (usbManager is null)
        {
            return Array.Empty<string>();
        }

        var devices = usbManager.DeviceList;
        if (devices is null || devices.Count == 0)
        {
            return Array.Empty<string>();
        }

        return devices.Values
            .OrderBy(d => d.DeviceName, StringComparer.Ordinal)
            .Select((_, i) => $"USB{i}")
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
            throw new ObjectDisposedException(nameof(AndroidSerialPortMonitor));
        }
    }

    // ── Broadcast receiver ────────────────────────────────────────────────

    /// <summary>
    /// <see cref="BroadcastReceiver"/> that forwards USB attach/detach intents to
    /// the owning <see cref="AndroidSerialPortMonitor"/>.
    /// </summary>
    private sealed class UsbBroadcastReceiver : BroadcastReceiver
    {
        private readonly AndroidSerialPortMonitor _owner;

        /// <summary>Initializes a new <see cref="UsbBroadcastReceiver"/>.</summary>
        /// <param name="owner">The owning monitor.</param>
        public UsbBroadcastReceiver(AndroidSerialPortMonitor owner) => _owner = owner;

        /// <inheritdoc/>
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action is UsbManager.ActionUsbDeviceAttached
                or UsbManager.ActionUsbDeviceDetached)
            {
                _owner.OnUsbEvent();
            }
        }
    }
}
