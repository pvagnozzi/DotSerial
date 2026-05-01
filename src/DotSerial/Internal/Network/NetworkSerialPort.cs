// -----------------------------------------------------------------------
// <copyright file="NetworkSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Cross-platform TCP/IP-to-serial bridge implementation of ISerialPort.
//     Connects to network serial servers (Moxa, Lantronix, socat, etc.).
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Network;

using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cross-platform implementation of <see cref="Abstractions.ISerialPort"/> that tunnels
/// serial communication over a TCP/IP connection to a network serial server
/// (e.g., Moxa, Lantronix, ATEN, or <c>socat</c> on Linux).
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Models.SerialPortSettings.PortName"/> must be in <c>"host:port"</c>
/// format, for example <c>"192.168.1.100:4001"</c> or <c>"my-serial-server.local:23"</c>.
/// </para>
/// <para>
/// Baud rate, parity, stop bits and other serial line settings must be configured
/// directly on the network serial server device; they are stored in
/// <see cref="Models.SerialPortSettings"/> for reference only and have no effect on the
/// TCP connection itself.
/// </para>
/// <para>
/// This implementation is available on all platforms (Windows, Linux, macOS, Android, iOS).
/// </para>
/// </remarks>
internal sealed class NetworkSerialPort : Abstractions.ISerialPort
{
    private readonly string _host;
    private readonly int _tcpPort;
    private readonly Models.SerialPortSettings _settings;
    private readonly ILogger<NetworkSerialPort> _logger;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _readTimeout;
    private int _writeTimeout;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="NetworkSerialPort"/> targeting the host and port
    /// encoded in <see cref="Models.SerialPortSettings.PortName"/>.
    /// </summary>
    /// <param name="settings">
    /// Port settings. <see cref="Models.SerialPortSettings.PortName"/> must be in
    /// <c>"host:port"</c> format and must already have passed
    /// <see cref="Models.SerialPortSettings.Validate"/>.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    internal NetworkSerialPort(Models.SerialPortSettings settings, ILogger<NetworkSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        settings.Validate();

        // Parse host and port — Validate() guarantees the format is correct.
        var colonIndex = settings.PortName.LastIndexOf(':');
        _host = settings.PortName[..colonIndex];
        _tcpPort = int.Parse(settings.PortName.AsSpan(colonIndex + 1));

        _settings = settings;
        _logger = logger;
        _readTimeout = settings.ReadTimeout;
        _writeTimeout = settings.WriteTimeout;
    }

    // ── ISerialPort – Properties ───────────────────────────────────────────

    /// <inheritdoc/>
    public string PortName => _settings.PortName;

    /// <inheritdoc/>
    public int BaudRate => _settings.BaudRate;

    /// <inheritdoc/>
    public Enums.Parity Parity => _settings.Parity;

    /// <inheritdoc/>
    public int DataBits => _settings.DataBits;

    /// <inheritdoc/>
    public Enums.StopBits StopBits => _settings.StopBits;

    /// <inheritdoc/>
    public Enums.FlowControl FlowControl => _settings.FlowControl;

    /// <inheritdoc/>
    public int ReadTimeout
    {
        get => _readTimeout;
        set
        {
            _readTimeout = value;
            _stream?.ReadTimeout = value == -1 ? Timeout.Infinite : value;
        }
    }

    /// <inheritdoc/>
    public int WriteTimeout
    {
        get => _writeTimeout;
        set
        {
            _writeTimeout = value;
            _stream?.WriteTimeout = value == -1 ? Timeout.Infinite : value;
        }
    }

    /// <inheritdoc/>
    public bool IsOpen => _client?.Connected == true && _stream is not null;

    /// <inheritdoc/>
    /// <remarks>Returns the number of bytes available in the TCP receive buffer.</remarks>
    public int BytesToRead => _client?.Available ?? 0;

    /// <inheritdoc/>
    /// <remarks>Always returns <c>0</c>; TCP does not expose a pending-write count.</remarks>
    public int BytesToWrite => 0;

    /// <inheritdoc/>
    public Stream BaseStream
    {
        get
        {
            ThrowIfNotOpen();
            return _stream!;
        }
    }

#pragma warning disable CS0067
    /// <inheritdoc/>
    /// <remarks>Not raised by <see cref="NetworkSerialPort"/>; TCP has no data-received interrupt.</remarks>
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

    /// <inheritdoc/>
    /// <remarks>Not raised by <see cref="NetworkSerialPort"/>; TCP has no hardware error signals.</remarks>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <inheritdoc/>
    /// <remarks>Not raised by <see cref="NetworkSerialPort"/>; TCP has no hardware pin signals.</remarks>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;
#pragma warning restore CS0067

    // ── Lifecycle ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Open()
    {
        ThrowIfDisposed();
        _logger.Connecting(_host, _tcpPort);

        var client = new TcpClient();
        try
        {
            client.Connect(_host, _tcpPort);
            ApplyClient(client);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            client.Dispose();
            throw new Exceptions.SerialPortException(
                $"Failed to connect to TCP serial bridge '{_host}:{_tcpPort}': {ex.Message}", ex);
        }

        _logger.Connected(_host, _tcpPort);
    }

    /// <inheritdoc/>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.Connecting(_host, _tcpPort);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(_host, _tcpPort, cancellationToken).ConfigureAwait(false);
            ApplyClient(client);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
        {
            client.Dispose();
            throw new Exceptions.SerialPortException(
                $"Failed to connect to TCP serial bridge '{_host}:{_tcpPort}': {ex.Message}", ex);
        }

        _logger.Connected(_host, _tcpPort);
    }

    /// <inheritdoc/>
    public void Close()
    {
        _stream?.Close();
        _stream = null;
        _client?.Close();
        _client = null;
        _logger.Disconnected(_host, _tcpPort);
    }

    /// <inheritdoc/>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Close();
        return Task.CompletedTask;
    }

    // ── Write ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        _logger.WritingBytes(count, _host, _tcpPort);
        _stream!.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public void Write(string text)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        var bytes = Encoding.UTF8.GetBytes(text);
        _stream!.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    public void WriteLine(string text) => Write(text + "\n");

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        await _stream!.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        await _stream!.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\n");
        await WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    // ── Read ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return _stream!.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public int ReadByte()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return _stream!.ReadByte();
    }

    /// <inheritdoc/>
    public string ReadExisting()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();

        var available = _client!.Available;
        if (available == 0)
        {
            return string.Empty;
        }

        var buffer = new byte[available];
        _ = _stream!.Read(buffer, 0, available);
        return Encoding.UTF8.GetString(buffer);
    }

    /// <inheritdoc/>
    public string ReadLine()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return ReadUntil('\n');
    }

    /// <inheritdoc/>
    public string ReadTo(string value)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();

        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Delimiter must not be null or empty.", nameof(value));
        }

        var sb = new StringBuilder();
        int b;
        while ((b = _stream!.ReadByte()) != -1)
        {
            sb.Append((char)b);
            if (sb.Length >= value.Length &&
                sb.ToString(sb.Length - value.Length, value.Length)
                    .Equals(value, StringComparison.Ordinal))
            {
                return sb.ToString(0, sb.Length - value.Length);
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return await _stream!.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return await _stream!.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();

        var sb = new StringBuilder();
        var buf = new byte[1];

        while (!cancellationToken.IsCancellationRequested)
        {
            int read = await _stream!.ReadAsync(buf.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            var c = (char)buf[0];
            if (c == '\n')
            {
                return sb.ToString();
            }

            sb.Append(c);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return sb.ToString();
    }

    // ── Buffer management ─────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>No-op for TCP connections; TCP has no driver-level receive buffer to discard.</remarks>
    public void DiscardInBuffer() { }

    /// <inheritdoc/>
    /// <remarks>No-op for TCP connections; TCP has no driver-level transmit buffer to discard.</remarks>
    public void DiscardOutBuffer() { }

    // ── IDisposable / IAsyncDisposable ─────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        _logger.PortDisposed(_host, _tcpPort);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Attaches a successfully connected <see cref="TcpClient"/> and configures its stream.</summary>
    private void ApplyClient(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        _stream.ReadTimeout = _readTimeout == -1 ? Timeout.Infinite : _readTimeout;
        _stream.WriteTimeout = _writeTimeout == -1 ? Timeout.Infinite : _writeTimeout;
    }

    /// <summary>Reads bytes from the network stream until the given delimiter character.</summary>
    private string ReadUntil(char delimiter)
    {
        var sb = new StringBuilder();
        int b;
        while ((b = _stream!.ReadByte()) != -1)
        {
            if ((char)b == delimiter)
            {
                return sb.ToString();
            }

            sb.Append((char)b);
        }
        return sb.ToString();
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> if this instance has been disposed.</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NetworkSerialPort));
        }
    }

    /// <summary>Throws <see cref="Exceptions.SerialPortException"/> if the port is not connected.</summary>
    private void ThrowIfNotOpen()
    {
        if (!IsOpen)
        {
            throw new Exceptions.SerialPortException($"Network serial port '{PortName}' is not connected.");
        }
    }
}
