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

/// <summary>Provides data for the <see cref="Abstractions.ISerialPort.PinChanged"/> event.</summary>
public sealed class SerialPinChangedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance with the specified pin-change type.</summary>
    /// <param name="eventType">The type of pin change that occurred.</param>
    public SerialPinChangedEventArgs(Enums.SerialPinChange eventType) => EventType = eventType;

    /// <summary>Gets the type of pin change that occurred.</summary>
    public Enums.SerialPinChange EventType { get; }
}
