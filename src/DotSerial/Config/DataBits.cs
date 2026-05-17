// -----------------------------------------------------------------------
// <copyright file="DataBits.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Number of data bits per byte in serial communication.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Number of data bits per byte.</summary>
public enum DataBits
{
    /// <summary>5 data bits.</summary>
    Five = 5,
    /// <summary>6 data bits.</summary>
    Six = 6,
    /// <summary>7 data bits.</summary>
    Seven = 7,
    /// <summary>8 data bits.</summary>
    Eight = 8,
}
