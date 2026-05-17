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

using DotSerial.Abstractions;
using DotSerial.Config;
using DotSerial.Exceptions;
using DotSerial.Models;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cross-platform implementation of <see cref="Abstractions.ISerialPort"/> that tunnels
/// serial communication over a TCP/IP connection to a network serial server
/// (e.g., Moxa, Lantronix, ATEN, or <c>socat</c> on Linux).
/// </summary>
internal sealed class NetworkSerialPort : Abstractions.ISerialPort
{
    private readonly string _host;
    private readonly int _tcpPort;
    private readonly SerialPortConfig _config;
    private readonly ILogger<NetworkSerialPort> _logger;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _readTimeout;
    private int _writeTimeout;
    private bool _disposed;

    internal NetworkSerialPort(SerialPortConfig config, ILogger<NetworkSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        config.Validate();

        var colonIndex = config.PortName.LastIndexOf(':');
        if (colonIndex <= 0)
        {
            throw new SerialPortException(
                $"Invalid network address format '{config.PortName}'. Expected 'host:port' format.");
        }

        _host = config.PortName[..colonIndex];
        
        try
        {
            _tcpPort = int.Parse(config.PortName.AsSpan(colonIndex + 1));
        }
        catch (FormatException ex)
        {
            throw new SerialPortException(
                $"Invalid port number in '{config.PortName}'. Port must be a valid integer.", ex);
        }

        if (_tcpPort < 1 || _tcpPort > 65535)
        {
            throw new SerialPortException(
                $"Invalid port number '{_tcpPort}'. Port must be between 1 and 65535.");
        }

        _config = config;
        _logger = logger;
        _readTimeout = config.ReadTimeout;
        _writeTimeout = config.WriteTimeout;
    }

    /// <inheritdoc/>
    public string PortName => _config.PortName;

    /// <inheritdoc/>
    public BaudRate BaudRate => _config.BaudRate;

    /// <inheritdoc/>
    public Parity Parity => _config.Parity;

    /// <inheritdoc/>
    public int DataBits => _config.DataBits;

    /// <inheritdoc/>
    public StopBits StopBits => _config.StopBits;

    /// <inheritdoc/>
    public FlowControl FlowControl => _config.FlowControl;

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
    public int BytesToRead => _client?.Available ?? 0;

    /// <inheritdoc/>
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
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;
#pragma warning restore CS0067

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
    public byte[] ReadExisting()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();

        var available = _client!.Available;
        if (available == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[available];
        _ = _stream!.Read(buffer, 0, available);
        return buffer;
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

    /// <inheritdoc/>
    public void DiscardInBuffer()
    {
    }

    /// <inheritdoc/>
    public void DiscardOutBuffer()
    {
    }

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

    private void ApplyClient(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        _stream.ReadTimeout = _readTimeout == -1 ? Timeout.Infinite : _readTimeout;
        _stream.WriteTimeout = _writeTimeout == -1 ? Timeout.Infinite : _writeTimeout;
    }

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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NetworkSerialPort));
        }
    }

    private void ThrowIfNotOpen()
    {
        if (!IsOpen)
        {
            throw new Exceptions.SerialPortException($"Network serial port '{PortName}' is not connected.");
        }
    }
}
