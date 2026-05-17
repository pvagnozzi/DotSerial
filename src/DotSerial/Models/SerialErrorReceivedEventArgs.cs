// -----------------------------------------------------------------------
// <copyright file="SerialErrorReceivedEventArgs.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Provides data for the ISerialPort.ErrorReceived event.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Models;

using DotSerial.Config;

/// <summary>Provides data for the <see cref="Abstractions.ISerialPort.ErrorReceived"/> event.</summary>
/// <remarks>Initializes a new instance with the specified error type.</remarks>
/// <param name="errorType">The type of error that occurred.</param>
public sealed class SerialErrorReceivedEventArgs(SerialError errorType) : EventArgs
{
    /// <summary>Gets the type of error that occurred.</summary>
    public SerialError ErrorType { get; } = errorType;
}
