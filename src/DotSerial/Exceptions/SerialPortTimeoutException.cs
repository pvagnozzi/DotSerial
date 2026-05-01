// -----------------------------------------------------------------------
// <copyright file="SerialPortTimeoutException.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Thrown when a serial port read or write operation times out.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Exceptions;

/// <summary>Thrown when a serial port read or write operation times out.</summary>
[Serializable]
public sealed class SerialPortTimeoutException : SerialPortException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public SerialPortTimeoutException() : base("The serial port operation timed out.") { }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The error message.</param>
    public SerialPortTimeoutException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception.</param>
    public SerialPortTimeoutException(string message, Exception inner) : base(message, inner) { }
}
