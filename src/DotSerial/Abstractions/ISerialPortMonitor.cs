// -----------------------------------------------------------------------
// <copyright file="ISerialPortMonitor.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Defines a background service that monitors serial port availability in real time.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

using DotSerial.Models;

namespace DotSerial.Abstractions;

/// <summary>
/// Monitors the system for serial port additions and removals by periodically
/// polling the available port list and raising <see cref="PortsChanged"/> when
/// the set of ports changes.
/// </summary>
/// <remarks>
/// Call <see cref="StartAsync"/> (or <see cref="Start"/>) to begin monitoring,
/// and <see cref="Stop"/> (or <see langword="Dispose"/>) to halt it.
/// The monitor is safe to start and stop multiple times.
/// </remarks>
public interface ISerialPortMonitor : IDisposable
{
    /// <summary>
    /// Raised on the thread-pool whenever the set of available serial ports changes.
    /// The event is not raised on the UI thread; marshal to the UI thread if needed.
    /// </summary>
    event EventHandler<PortsChangedEventArgs>? PortsChanged;

    /// <summary>Gets the current snapshot of available serial port names.</summary>
    IReadOnlyList<string> CurrentPorts { get; }

    /// <summary>Gets a value indicating whether the monitor is currently active.</summary>
    bool IsRunning { get; }

    /// <summary>Starts the monitoring loop synchronously.</summary>
    void Start();

    /// <summary>Starts the monitoring loop asynchronously.</summary>
    /// <param name="cancellationToken">A token to cancel the start operation.</param>
    /// <returns>A <see cref="Task"/> that completes once monitoring has started.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the monitoring loop. Safe to call even if not running.</summary>
    void Stop();
}
