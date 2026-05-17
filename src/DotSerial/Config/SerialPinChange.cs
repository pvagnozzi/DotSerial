// -----------------------------------------------------------------------
// <copyright file="SerialPinChange.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Type of change that occurred on a serial port pin.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Type of change that occurred on a serial port pin.</summary>
public enum SerialPinChange
{
    /// <summary>The CTS (Clear to Send) signal changed state.</summary>
    CtsChanged,
    /// <summary>The DSR (Data Set Ready) signal changed state.</summary>
    DsrChanged,
    /// <summary>The CD (Carrier Detect) signal changed state.</summary>
    CDChanged,
    /// <summary>A ring indicator was detected.</summary>
    Ring,
    /// <summary>A break was detected on input.</summary>
    Break,
}
