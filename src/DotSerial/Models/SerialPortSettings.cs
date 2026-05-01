// -----------------------------------------------------------------------
// <copyright file="SerialPortSettings.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Immutable configuration record for a serial port connection.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Models;

/// <summary>
/// Immutable configuration record for a serial port connection.
/// Supports object initializer and <c>with</c> expressions for easy customisation.
/// </summary>
public sealed record SerialPortSettings
{
    /// <summary>Gets the port name (e.g., "COM3", "/dev/ttyUSB0").</summary>
    public required string PortName { get; init; }

    /// <summary>Gets the baud rate. Default is 9600.</summary>
    public int BaudRate { get; init; } = 9600;

    /// <summary>Gets the parity. Default is <see cref="Enums.Parity.None"/>.</summary>
    public Enums.Parity Parity { get; init; } = Enums.Parity.None;

    /// <summary>Gets the number of data bits. Default is 8.</summary>
    public int DataBits { get; init; } = 8;

    /// <summary>Gets the stop bits. Default is <see cref="Enums.StopBits.One"/>.</summary>
    public Enums.StopBits StopBits { get; init; } = Enums.StopBits.One;

    /// <summary>Gets the flow-control protocol. Default is <see cref="Enums.FlowControl.None"/>.</summary>
    public Enums.FlowControl FlowControl { get; init; } = Enums.FlowControl.None;

    /// <summary>Gets the read timeout in milliseconds. Default is 500 ms.</summary>
    public int ReadTimeout { get; init; } = 500;

    /// <summary>Gets the write timeout in milliseconds. Default is 500 ms.</summary>
    public int WriteTimeout { get; init; } = 500;

    /// <summary>Gets the size of the read buffer in bytes. Default is 4096.</summary>
    public int ReadBufferSize { get; init; } = 4096;

    /// <summary>Gets the size of the write buffer in bytes. Default is 2048.</summary>
    public int WriteBufferSize { get; init; } = 2048;

    /// <summary>
    /// Validates the settings, throwing <see cref="Exceptions.SerialPortException"/>
    /// if any value is out of range.
    /// </summary>
    /// <exception cref="Exceptions.SerialPortException">Thrown when a setting value is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PortName))
            throw new Exceptions.SerialPortException("PortName must not be null or empty.");
        if (BaudRate <= 0)
            throw new Exceptions.SerialPortException($"BaudRate must be positive; got {BaudRate}.");
        if (DataBits is < 5 or > 8)
            throw new Exceptions.SerialPortException($"DataBits must be between 5 and 8; got {DataBits}.");
        if (ReadTimeout < -1)
            throw new Exceptions.SerialPortException($"ReadTimeout must be -1 (infinite) or a positive value; got {ReadTimeout}.");
        if (WriteTimeout < -1)
            throw new Exceptions.SerialPortException($"WriteTimeout must be -1 (infinite) or a positive value; got {WriteTimeout}.");
    }
}
