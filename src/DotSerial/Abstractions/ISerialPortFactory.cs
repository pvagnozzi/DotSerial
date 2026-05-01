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

namespace DotSerial.Abstractions;

/// <summary>
/// Factory for creating <see cref="ISerialPort"/> instances using the
/// platform-appropriate implementation.
/// </summary>
public interface ISerialPortFactory
{
    /// <summary>Creates a new <see cref="ISerialPort"/> configured with <paramref name="settings"/>.</summary>
    /// <param name="settings">The serial port configuration settings.</param>
    /// <returns>A new <see cref="ISerialPort"/> instance.</returns>
    ISerialPort Create(Models.SerialPortSettings settings);

    /// <summary>Returns all serial port names available on this platform.</summary>
    /// <returns>A read-only list of available port names.</returns>
    IReadOnlyList<string> GetPortNames();
}
