// -----------------------------------------------------------------------
// <copyright file="PortsChangedEventArgs.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Event arguments raised when the set of available serial ports changes.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Models;

/// <summary>
/// Provides data for the <see cref="Abstractions.ISerialPortMonitor.PortsChanged"/> event,
/// describing which serial ports were added or removed since the last scan.
/// </summary>
public sealed class PortsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortsChangedEventArgs"/>.
    /// </summary>
    /// <param name="addedPorts">Ports that became available since the last scan.</param>
    /// <param name="removedPorts">Ports that disappeared since the last scan.</param>
    /// <param name="allPorts">The complete current list of available ports.</param>
    public PortsChangedEventArgs(
        IReadOnlyList<string> addedPorts,
        IReadOnlyList<string> removedPorts,
        IReadOnlyList<string> allPorts)
    {
        ArgumentNullException.ThrowIfNull(addedPorts);
        ArgumentNullException.ThrowIfNull(removedPorts);
        ArgumentNullException.ThrowIfNull(allPorts);

        AddedPorts = addedPorts;
        RemovedPorts = removedPorts;
        AllPorts = allPorts;
    }

    /// <summary>Gets the list of serial port names that were newly detected.</summary>
    public IReadOnlyList<string> AddedPorts { get; }

    /// <summary>Gets the list of serial port names that were removed or disconnected.</summary>
    public IReadOnlyList<string> RemovedPorts { get; }

    /// <summary>Gets the complete list of currently available serial port names.</summary>
    public IReadOnlyList<string> AllPorts { get; }
}
