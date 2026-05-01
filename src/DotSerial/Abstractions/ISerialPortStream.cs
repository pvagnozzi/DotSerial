// -----------------------------------------------------------------------
// <copyright file="ISerialPortStream.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Represents a stream backed by a serial port connection.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Abstractions;

/// <summary>
/// Represents a <see cref="Stream"/> that is backed by a serial port connection,
/// enabling stream-based read/write operations over a serial link.
/// </summary>
/// <remarks>
/// Obtain instances via <see cref="ISerialPortFactory.CreateStream"/> or by constructing
/// <see cref="DotSerial.Streams.SerialPortStreamWrapper"/> directly.
/// </remarks>
public interface ISerialPortStream : IDisposable, IAsyncDisposable
{
    /// <summary>Gets the underlying serial port that backs this stream.</summary>
    ISerialPort SerialPort { get; }

    /// <summary>Gets the underlying <see cref="System.IO.Stream"/> for reading and writing data.</summary>
    Stream Stream { get; }
}
