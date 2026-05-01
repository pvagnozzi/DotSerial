// -----------------------------------------------------------------------
// <copyright file="MacOSSerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     macOS-specific ISerialPortMonitor implementation using FileSystemWatcher on /dev/cu.*.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.MacOS;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// macOS-specific implementation of <see cref="Abstractions.ISerialPortMonitor"/> that uses
/// <see cref="FileSystemWatcher"/> on <c>/dev</c> to watch for <c>cu.*</c> call-out serial ports.
/// </summary>
/// <remarks>
/// <para>
/// macOS exposes serial ports as <c>/dev/cu.*</c> (call-out) and <c>/dev/tty.*</c> (dial-in).
/// This monitor watches <c>cu.*</c> entries, excluding <c>cu.Bluetooth-Incoming-Port</c>.
/// </para>
/// <para>
/// This monitor is only supported on macOS. Constructing it on any other operating system
/// throws <see cref="PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
internal sealed class MacOSSerialPortMonitor : Abstractions.ISerialPortMonitor
{
    private readonly ILogger<MacOSSerialPortMonitor> _logger;
    private volatile IReadOnlyList<string> _currentPorts;
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="MacOSSerialPortMonitor"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current operating system is not macOS.
    /// </exception>
    internal MacOSSerialPortMonitor(ILogger<MacOSSerialPortMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException(
                "MacOSSerialPortMonitor is only supported on macOS.");
        }

        _logger = logger;
        _currentPorts = GetCurrentPorts();
    }

    /// <inheritdoc/>
    public event EventHandler<Models.PortsChangedEventArgs>? PortsChanged;

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrentPorts => _currentPorts;

    /// <inheritdoc/>
    public bool IsRunning => _watcher is not null && _watcher.EnableRaisingEvents;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        if (IsRunning)
        {
            return;
        }

        _watcher = new FileSystemWatcher("/dev", "cu.*")
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = false,
        };
        _watcher.Created += OnDeviceChanged;
        _watcher.Deleted += OnDeviceChanged;
        _watcher.EnableRaisingEvents = true;

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
        if (_watcher is null)
        {
            return;
        }

        _logger.MonitorStopping();

        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
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

    // ── Event handlers ─────────────────────────────────────────────────

    /// <summary>Handles <see cref="FileSystemWatcher"/> Created/Deleted events.</summary>
    private void OnDeviceChanged(object sender, FileSystemEventArgs e)
    {
        string path = e.FullPath;
        if (!IsSerialPort(path))
        {
            return;
        }

        var newPorts = GetCurrentPorts();
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

    // ── Port enumeration ───────────────────────────────────────────────

    /// <summary>Enumerates the current set of call-out serial port device nodes under <c>/dev</c>.</summary>
    private static IReadOnlyList<string> GetCurrentPorts()
        => [.. Directory.GetFiles("/dev", "cu.*")
            .Where(IsSerialPort)
            .OrderBy(f => f, StringComparer.Ordinal)];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="path"/> is a call-out serial port,
    /// excluding the built-in Bluetooth incoming port.
    /// </summary>
    private static bool IsSerialPort(string path)
        => !path.EndsWith(".Bluetooth-Incoming-Port", StringComparison.Ordinal);

    // ── Event raising ──────────────────────────────────────────────────

    /// <summary>Safely raises <see cref="PortsChanged"/> on the thread pool.</summary>
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
            try { d.Invoke(this, args); }
            catch (Exception ex) { _logger.MonitorSubscriberError(ex); }
        }
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> when already disposed.</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MacOSSerialPortMonitor));
        }
    }
}
