// -----------------------------------------------------------------------
// <copyright file="SerialPortNotFoundException.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Thrown when the requested serial port is not found on the system.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Exceptions;

/// <summary>Thrown when the requested serial port is not found on the system.</summary>
[Serializable]
public sealed class SerialPortNotFoundException : SerialPortException
{
    /// <summary>Initializes a new instance for the specified port name.</summary>
    /// <param name="portName">The name of the port that was not found.</param>
    public SerialPortNotFoundException(string portName)
        : base($"Serial port '{portName}' was not found on this system.")
    {
        PortName = portName;
    }

    /// <summary>Initializes a new instance for the specified port name with inner exception.</summary>
    /// <param name="portName">The name of the port that was not found.</param>
    /// <param name="inner">The inner exception.</param>
    public SerialPortNotFoundException(string portName, Exception inner)
        : base($"Serial port '{portName}' was not found on this system.", inner)
    {
        PortName = portName;
    }

    /// <summary>Gets the name of the port that was not found.</summary>
    public string PortName { get; }
}
