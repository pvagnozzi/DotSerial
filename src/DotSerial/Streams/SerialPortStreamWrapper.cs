// -----------------------------------------------------------------------
// <copyright file="SerialPortStreamWrapper.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Concrete implementation of ISerialPortStream that wraps an ISerialPort
//     and exposes its BaseStream for stream-based I/O.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

using DotSerial.Abstractions;

namespace DotSerial.Streams;

/// <summary>
/// Concrete implementation of <see cref="Abstractions.ISerialPortStream"/> that wraps
/// any <see cref="Abstractions.ISerialPort"/> and exposes its
/// <see cref="Abstractions.ISerialPort.BaseStream"/> for stream-based I/O.
/// </summary>
/// <remarks>
/// <para>
/// Disposing this wrapper also disposes the underlying <see cref="Abstractions.ISerialPort"/>.
/// If you want to reuse the port after closing the stream, do not dispose the wrapper.
/// </para>
/// <para>
/// Obtain an instance via <see cref="Abstractions.ISerialPortFactory.CreateStream"/> or by
/// constructing directly:
/// <code>
/// using var wrapper = new SerialPortStreamWrapper(port);
/// using var reader  = new StreamReader(wrapper.Stream, leaveOpen: true);
/// string line = await reader.ReadLineAsync();
/// </code>
/// </para>
/// </remarks>
public sealed class SerialPortStreamWrapper : ISerialPortStream
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="SerialPortStreamWrapper"/> around
    /// <paramref name="serialPort"/>.
    /// </summary>
    /// <param name="serialPort">
    /// The serial port to wrap. Must not be <see langword="null"/>.
    /// The port does not need to be open at construction time.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serialPort"/> is <see langword="null"/>.
    /// </exception>
    public SerialPortStreamWrapper(Abstractions.ISerialPort serialPort)
    {
        ArgumentNullException.ThrowIfNull(serialPort);
        SerialPort = serialPort;
    }

    /// <inheritdoc/>
    public Abstractions.ISerialPort SerialPort { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Accessing this property while the port is not open throws
    /// <see cref="Exceptions.SerialPortException"/> (or platform-equivalent).
    /// </remarks>
    public Stream Stream => SerialPort.BaseStream;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SerialPort.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await SerialPort.DisposeAsync().ConfigureAwait(false);
    }
}
