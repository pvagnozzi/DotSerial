// -----------------------------------------------------------------------
// <copyright file="StopBits.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Number of stop bits used in serial communication.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Number of stop bits used in serial communication.</summary>
public enum StopBits
{
    /// <summary>One stop bit.</summary>
    One,
    /// <summary>One and a half stop bits.</summary>
    OnePointFive,
    /// <summary>Two stop bits.</summary>
    Two,
}
