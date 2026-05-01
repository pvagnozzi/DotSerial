// -----------------------------------------------------------------------
// <copyright file="ISerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Defines the contract for a serial port connection.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Abstractions;

/// <summary>
/// Defines the contract for a serial port connection supporting both classic
/// byte-level I/O operations and stream-based communication.
/// </summary>
public interface ISerialPort : IDisposable, IAsyncDisposable
{
    /// <summary>Gets the port name (e.g., "COM1", "/dev/ttyUSB0").</summary>
    string PortName { get; }

    /// <summary>Gets the baud rate in bits per second.</summary>
    int BaudRate { get; }

    /// <summary>Gets the parity bit setting.</summary>
    Enums.Parity Parity { get; }

    /// <summary>Gets the number of data bits per byte (5-8).</summary>
    int DataBits { get; }

    /// <summary>Gets the number of stop bits.</summary>
    Enums.StopBits StopBits { get; }

    /// <summary>Gets the flow-control (handshake) protocol.</summary>
    Enums.FlowControl FlowControl { get; }

    /// <summary>Gets or sets the read timeout in milliseconds. Use -1 for infinite.</summary>
    int ReadTimeout { get; set; }

    /// <summary>Gets or sets the write timeout in milliseconds. Use -1 for infinite.</summary>
    int WriteTimeout { get; set; }

    /// <summary>Gets a value indicating whether the port is open.</summary>
    bool IsOpen { get; }

    /// <summary>Gets the number of bytes available to read from the receive buffer.</summary>
    int BytesToRead { get; }

    /// <summary>Gets the number of bytes waiting to be transmitted.</summary>
    int BytesToWrite { get; }

    /// <summary>Opens the serial port.</summary>
    void Open();

    /// <summary>Opens the serial port asynchronously.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes the serial port.</summary>
    void Close();

    /// <summary>Closes the serial port asynchronously.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes a byte array to the serial port output buffer.</summary>
    /// <param name="buffer">The byte array to write.</param>
    /// <param name="offset">The zero-based offset in the buffer.</param>
    /// <param name="count">The number of bytes to write.</param>
    void Write(byte[] buffer, int offset, int count);

    /// <summary>Writes a string to the serial port output buffer.</summary>
    /// <param name="text">The string to write.</param>
    void Write(string text);

    /// <summary>Writes a string followed by a newline to the serial port.</summary>
    /// <param name="text">The string to write.</param>
    void WriteLine(string text);

    /// <summary>Asynchronously writes a byte array to the serial port output buffer.</summary>
    /// <param name="buffer">The byte array to write.</param>
    /// <param name="offset">The zero-based offset in the buffer.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously writes a memory region to the serial port output buffer.</summary>
    /// <param name="buffer">The memory region to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously writes a string followed by a newline to the serial port.</summary>
    /// <param name="text">The string to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteLineAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Reads a number of bytes from the serial port input buffer.</summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based offset in the buffer.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    int Read(byte[] buffer, int offset, int count);

    /// <summary>Reads a single byte from the serial port input buffer.</summary>
    /// <returns>The byte read, or -1 if no data is available.</returns>
    int ReadByte();

    /// <summary>Reads all immediately available bytes as a string.</summary>
    /// <returns>The contents of the stream and the input buffer.</returns>
    string ReadExisting();

    /// <summary>Reads up to and including the first NewLine character.</summary>
    /// <returns>The line read from the serial port.</returns>
    string ReadLine();

    /// <summary>Reads a string up to the specified value.</summary>
    /// <param name="value">The delimiter to read up to.</param>
    /// <returns>The string read up to the delimiter.</returns>
    string ReadTo(string value);

    /// <summary>Asynchronously reads bytes from the serial port input buffer.</summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based offset in the buffer.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes read.</returns>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously reads bytes into a memory region.</summary>
    /// <param name="buffer">The memory region to read into.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes read.</returns>
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously reads up to and including the first NewLine character.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The line read from the serial port.</returns>
    Task<string> ReadLineAsync(CancellationToken cancellationToken = default);

    /// <summary>Discards data from the serial driver's receive buffer.</summary>
    void DiscardInBuffer();

    /// <summary>Discards data from the serial driver's transmit buffer.</summary>
    void DiscardOutBuffer();

    /// <summary>Gets the underlying <see cref="Stream"/> for stream-based I/O.</summary>
    Stream BaseStream { get; }

    /// <summary>Raised when data is received on the serial port.</summary>
    event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

    /// <summary>Raised when an error condition is detected on the serial port.</summary>
    event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <summary>Raised when the state of the CTS, DSR, CD, or RI pin changes.</summary>
    event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;
}
