// -----------------------------------------------------------------------
// <copyright file="SerialData.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Type of data received on the serial port.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Type of data received on the serial port.</summary>
public enum SerialData
{
    /// <summary>Regular character data received.</summary>
    Chars,

    /// <summary>End-of-file character received.</summary>
    Eof,
}
