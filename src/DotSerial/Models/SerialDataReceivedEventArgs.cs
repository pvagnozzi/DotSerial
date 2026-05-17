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

using DotSerial.Config;

/// <summary>Provides data for the <see cref="Abstractions.ISerialPort.DataReceived"/> event.</summary>
/// <remarks>Initializes a new instance with the specified event type.</remarks>
/// <param name="eventType">The type of data-received event.</param>
public sealed class SerialDataReceivedEventArgs(SerialData eventType) : EventArgs
{
    /// <summary>Gets the type of data-received event.</summary>
    public SerialData EventType { get; } = eventType;
}
