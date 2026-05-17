// -----------------------------------------------------------------------
// <copyright file="SerialPinChangedEventArgs.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Provides data for the ISerialPort.PinChanged event.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Models;

using DotSerial.Config;

/// <summary>Provides data for the <see cref="Abstractions.ISerialPort.PinChanged"/> event.</summary>
/// <remarks>Initializes a new instance with the specified pin-change type.</remarks>
/// <param name="eventType">The type of pin change that occurred.</param>
public sealed class SerialPinChangedEventArgs(SerialPinChange eventType) : EventArgs
{
    /// <summary>Gets the type of pin change that occurred.</summary>
    public SerialPinChange EventType { get; } = eventType;
}
