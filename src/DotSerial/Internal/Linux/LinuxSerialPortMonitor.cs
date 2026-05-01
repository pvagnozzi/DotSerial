// -----------------------------------------------------------------------
// <copyright file="LinuxSerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Linux-specific ISerialPortMonitor implementation using FileSystemWatcher on /dev.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Linux;

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Linux-specific implementation of <see cref="Abstractions.ISerialPortMonitor"/> that uses
/// <see cref="FileSystemWatcher"/> on <c>/dev</c> (and optionally <c>/dev/serial/by-id/</c>)
/// to detect USB and hardware serial port additions and removals in real time.
/// </summary>
/// <remarks>
/// <para>
/// Monitors <c>ttyUSB*</c>, <c>ttyACM*</c>, <c>ttyS*</c>, <c>ttyAMA*</c> device nodes under <c>/dev</c>.
/// If <c>/dev/serial/by-id/</c> exists, a secondary watcher is created for USB symlinks.
/// </para>
/// <para>
/// This monitor is only supported on Linux. Constructing it on any other operating system
/// throws <see cref="PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
internal sealed class LinuxSerialPortMonitor : Abstractions.ISerialPortMonitor
{
    private static readonly Regex TtyPattern =
        new(@"^/dev/tty(USB|ACM|AMA|S\d+)\d*$", RegexOptions.Compiled);

    private readonly ILogger<LinuxSerialPortMonitor> _logger;
    private volatile IReadOnlyList<string> _currentPorts;
    private FileSystemWatcher? _devWatcher;
    private FileSystemWatcher? _byIdWatcher;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="LinuxSerialPortMonitor"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current operating system is not Linux.
    /// </exception>
    internal LinuxSerialPortMonitor(ILogger<LinuxSerialPortMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException(
                "LinuxSerialPortMonitor is only supported on Linux.");

        _logger = logger;
        _currentPorts = GetCurrentPorts();
    }

    /// <inheritdoc/>
    public event EventHandler<Models.PortsChangedEventArgs>? PortsChanged;

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrentPorts => _currentPorts;

    /// <inheritdoc/>
    public bool IsRunning => _devWatcher is not null && _devWatcher.EnableRaisingEvents;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        if (IsRunning) return;

        _devWatcher = new FileSystemWatcher("/dev", "tty*")
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = false,
        };
        _devWatcher.Created += OnDeviceChanged;
        _devWatcher.Deleted += OnDeviceChanged;
        _devWatcher.EnableRaisingEvents = true;

        if (Directory.Exists("/dev/serial/by-id"))
        {
            _byIdWatcher = new FileSystemWatcher("/dev/serial/by-id", "*")
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true,
            };
            _byIdWatcher.Created += OnDeviceChanged;
            _byIdWatcher.Deleted += OnDeviceChanged;
        }

        _logger.LogInformation("Linux serial port monitor started (watching /dev/tty*).");
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
        if (_devWatcher is null) return;
        _logger.LogInformation("Linux serial port monitor stopping.");

        _devWatcher.EnableRaisingEvents = false;
        _devWatcher.Dispose();
        _devWatcher = null;

        if (_byIdWatcher is not null)
        {
            _byIdWatcher.EnableRaisingEvents = false;
            _byIdWatcher.Dispose();
            _byIdWatcher = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ── Event handlers ─────────────────────────────────────────────────

    /// <summary>Handles <see cref="FileSystemWatcher"/> Created/Deleted events.</summary>
    private void OnDeviceChanged(object sender, FileSystemEventArgs e)
    {
        string path = e.FullPath;
        if (!IsSerialPort(path)) return;

        var newPorts = GetCurrentPorts();
        var previous = _currentPorts;

        var added = newPorts.Except(previous).ToList();
        var removed = previous.Except(newPorts).ToList();

        if (added.Count == 0 && removed.Count == 0) return;

        _currentPorts = newPorts;
        _logger.LogInformation(
            "Linux serial ports changed — added: [{Added}], removed: [{Removed}].",
            string.Join(", ", added),
            string.Join(", ", removed));

        RaisePortsChanged(added, removed, newPorts);
    }

    // ── Port enumeration ───────────────────────────────────────────────

    /// <summary>Enumerates the current set of serial port device nodes under <c>/dev</c>.</summary>
    private static IReadOnlyList<string> GetCurrentPorts()
        => Directory.GetFiles("/dev", "tty*")
            .Where(IsSerialPort)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Returns <see langword="true"/> when <paramref name="path"/> represents a serial port device.</summary>
    private static bool IsSerialPort(string path)
        => TtyPattern.IsMatch(path);

    // ── Event raising ──────────────────────────────────────────────────

    /// <summary>Safely raises <see cref="PortsChanged"/> on the thread pool.</summary>
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
            try { d.Invoke(this, args); }
            catch (Exception ex) { _logger.LogError(ex, "Exception in PortsChanged subscriber."); }
        }
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> when already disposed.</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LinuxSerialPortMonitor));
    }
}
