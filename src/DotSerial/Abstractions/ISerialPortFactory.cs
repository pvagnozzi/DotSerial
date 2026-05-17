// -----------------------------------------------------------------------
// <copyright file="ISerialPortFactory.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Factory interface for creating platform-appropriate ISerialPort instances.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

using DotSerial.Config;
using DotSerial.Models;

namespace DotSerial.Abstractions;

/// <summary>
/// Factory for creating <see cref="ISerialPort"/> instances using the
/// platform-appropriate implementation.
/// </summary>
public interface ISerialPortFactory
{
    /// <summary>Creates a new <see cref="ISerialPort"/> configured with <paramref name="config"/>.</summary>
    /// <param name="config">The serial port configuration settings.</param>
    /// <returns>A new <see cref="ISerialPort"/> instance.</returns>
    ISerialPort Create(SerialPortConfig config);

    /// <summary>Returns all serial port names available on this platform.</summary>
    /// <returns>A read-only list of available port names.</returns>
    IReadOnlyList<string> GetPortNames();

    /// <summary>
    /// Creates a new <see cref="ISerialPortMonitor"/> that watches for serial port
    /// additions and removals on this platform.
    /// </summary>
    /// <param name="pollingInterval">
    /// How often to poll for port changes. Defaults to one second when <see langword="null"/>.
    /// </param>
    /// <returns>A new, stopped <see cref="ISerialPortMonitor"/> instance.</returns>
    ISerialPortMonitor CreateMonitor(TimeSpan? pollingInterval = null);

    /// <summary>
    /// Creates a new <see cref="ISerialPortStream"/> wrapping a port configured with
    /// <paramref name="config"/>. The underlying port is created via
    /// <see cref="Create"/> and is owned by the returned stream wrapper.
    /// </summary>
    /// <param name="config">The serial port configuration settings.</param>
    /// <returns>A new <see cref="ISerialPortStream"/> backed by the configured port.</returns>
    ISerialPortStream CreateStream(SerialPortConfig config);
}
