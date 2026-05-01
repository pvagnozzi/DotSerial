// -----------------------------------------------------------------------
// <copyright file="SerialPortException.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Base exception for all DotSerial serial-port errors.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Exceptions;

/// <summary>Base exception for all DotSerial serial-port errors.</summary>
[Serializable]
public class SerialPortException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public SerialPortException() : base("A serial port error occurred.") { }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The error message.</param>
    public SerialPortException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SerialPortException(string message, Exception innerException) : base(message, innerException) { }
}
