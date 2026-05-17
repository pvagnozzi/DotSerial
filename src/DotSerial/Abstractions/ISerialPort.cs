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

using DotSerial.Config;
using DotSerial.Models;

namespace DotSerial.Abstractions;

/// <summary>
/// Defines the contract for a serial port connection supporting both classic
/// byte-level I/O operations and stream-based communication.
/// </summary>
public interface ISerialPort : IDisposable, IAsyncDisposable
{
    /// <summary>Gets a value indicating whether the port is open.</summary>
    bool IsOpen { get; }

    /// <summary>Gets the port name (e.g., "COM1", "/dev/ttyUSB0", or MAC address for Bluetooth).</summary>
    string PortName { get; }

    /// <summary>Gets the baud rate in bits per second.</summary>
    BaudRate BaudRate { get; }

    /// <summary>Gets the parity bit setting.</summary>
    Parity Parity { get; }

    /// <summary>Gets the number of data bits per byte (5-8).</summary>
    int DataBits { get; }

    /// <summary>Gets the number of stop bits.</summary>
    StopBits StopBits { get; }

    /// <summary>Gets the flow-control (handshake) protocol.</summary>
    FlowControl FlowControl { get; }

    /// <summary>Gets the number of bytes available to read from the receive buffer.</summary>
    int BytesToRead { get; }

    /// <summary>Gets the number of bytes waiting to be transmitted.</summary>
    int BytesToWrite { get; }

    /// <summary>Gets the underlying stream for advanced usage.</summary>
    Stream BaseStream { get; }

    /// <summary>Gets or sets the read timeout in milliseconds. Use -1 for infinite timeout.</summary>
    int ReadTimeout { get; set; }

    /// <summary>Gets or sets the write timeout in milliseconds. Use -1 for infinite timeout.</summary>
    int WriteTimeout { get; set; }

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

    /// <summary>Reads a number of bytes from the serial port input buffer.</summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based offset in the buffer.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    int Read(byte[] buffer, int offset, int count);

    /// <summary>Reads a single byte from the serial port input buffer.</summary>
    /// <returns>The byte read, or -1 if no data is available.</returns>
    int ReadByte();

    /// <summary>Reads all immediately available bytes as a byte array.</summary>
    /// <returns>A byte array containing the contents of the stream and the input buffer.</returns>
    byte[] ReadExisting();

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

    /// <summary>Discards data from the serial driver's receive buffer.</summary>
    void DiscardInBuffer();

    /// <summary>Discards data from the serial driver's transmit buffer.</summary>
    void DiscardOutBuffer();

    /// <summary>Reads a line of text (up to newline or carriage return) from the serial port.</summary>
    /// <returns>The text read, or an empty string if no data is available.</returns>
    /// <remarks>
    /// This method reads bytes until it encounters a newline (\n) character or carriage return (\r).
    /// The line terminator is NOT included in the returned string.
    /// Default implementation reads byte-by-byte using <see cref="ReadByte()"/>.
    /// </remarks>
    string ReadLine()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            int b = ReadByte();
            if (b < 0) break;
            char c = (char)b;
            if (c == '\n' || c == '\r')
            {
                if (c == '\r' && ReadByte() is int next && (char)next == '\n')
                { }
                break;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Writes a string to the serial port, followed by the system newline.</summary>
    /// <param name="line">The text to write.</param>
    /// <remarks>
    /// Default implementation encodes the string to UTF-8 bytes and writes them, then writes a newline.
    /// </remarks>
    void WriteLine(string line)
    {
        Write(line);
        Write("\n");
    }

    /// <summary>Writes a string to the serial port.</summary>
    /// <param name="text">The text to write.</param>
    /// <remarks>
    /// Default implementation encodes the string to UTF-8 and writes the bytes.
    /// </remarks>
    void Write(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        Write(bytes, 0, bytes.Length);
    }

    /// <summary>Raised when data is received on the serial port.</summary>
    event EventHandler<SerialDataReceivedEventArgs>? DataReceived;

    /// <summary>Raised when an error condition is detected on the serial port.</summary>
    event EventHandler<SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <summary>Raised when the state of the CTS, DSR, CD, or RI pin changes.</summary>
    event EventHandler<SerialPinChangedEventArgs>? PinChanged;
}
