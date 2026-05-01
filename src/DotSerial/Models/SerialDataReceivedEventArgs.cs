// -----------------------------------------------------------------------
// <copyright file="SerialDataReceivedEventArgs.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Provides data for the ISerialPort.DataReceived event.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Models;

/// <summary>Provides data for the <see cref="Abstractions.ISerialPort.DataReceived"/> event.</summary>
public sealed class SerialDataReceivedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance with the specified event type.</summary>
    /// <param name="eventType">The type of data-received event.</param>
    public SerialDataReceivedEventArgs(Enums.SerialData eventType) => EventType = eventType;

    /// <summary>Gets the type of data-received event.</summary>
    public Enums.SerialData EventType { get; }
}
