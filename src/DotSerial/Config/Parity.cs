// -----------------------------------------------------------------------
// <copyright file="Parity.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Parity-checking protocol for serial communication.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Parity-checking protocol.</summary>
public enum Parity
{
    /// <summary>No parity check.</summary>
    None,
    /// <summary>Odd parity.</summary>
    Odd,
    /// <summary>Even parity.</summary>
    Even,
    /// <summary>Mark parity.</summary>
    Mark,
    /// <summary>Space parity.</summary>
    Space,
}
