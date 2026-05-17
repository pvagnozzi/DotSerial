// -----------------------------------------------------------------------
// <copyright file="SerialError.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Type of error that occurred on the serial port.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Type of error that occurred on the serial port.</summary>
public enum SerialError
{
    /// <summary>Receive buffer overflow.</summary>
    RXOver,
    /// <summary>Character buffer overrun.</summary>
    Overrun,
    /// <summary>Receive parity error.</summary>
    RXParity,
    /// <summary>Framing error.</summary>
    Frame,
    /// <summary>Transmit buffer full.</summary>
    TXFull,
}
