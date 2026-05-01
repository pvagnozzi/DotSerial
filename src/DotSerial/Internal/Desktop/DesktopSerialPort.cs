// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Desktop (Windows, Linux, macOS) implementation of ISerialPort via System.IO.Ports.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Desktop;

using Microsoft.Extensions.Logging;
using SysIO = System.IO.Ports;

/// <summary>
/// Desktop (Windows, Linux, macOS) implementation of <see cref="Abstractions.ISerialPort"/>
/// built on top of <see cref="System.IO.Ports.SerialPort"/>.
/// </summary>
internal sealed class DesktopSerialPort : Abstractions.ISerialPort
{
    private readonly SysIO.SerialPort _inner;
    private readonly ILogger<DesktopSerialPort> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="DesktopSerialPort"/> from <paramref name="settings"/>.
    /// </summary>
    /// <param name="settings">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    internal DesktopSerialPort(Models.SerialPortSettings settings, ILogger<DesktopSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        settings.Validate();

        _logger = logger;
        _inner = new SysIO.SerialPort(
            settings.PortName,
            settings.BaudRate,
            MapParity(settings.Parity),
            settings.DataBits,
            MapStopBits(settings.StopBits))
        {
            Handshake = MapHandshake(settings.FlowControl),
            ReadTimeout = settings.ReadTimeout,
            WriteTimeout = settings.WriteTimeout,
            ReadBufferSize = settings.ReadBufferSize,
            WriteBufferSize = settings.WriteBufferSize,
        };

        _inner.DataReceived  += OnDataReceived;
        _inner.ErrorReceived += OnErrorReceived;
        _inner.PinChanged    += OnPinChanged;
    }

    /// <inheritdoc/>
    public string PortName  => _inner.PortName;
    /// <inheritdoc/>
    public int BaudRate     => _inner.BaudRate;
    /// <inheritdoc/>
    public Enums.Parity Parity => MapBackParity(_inner.Parity);
    /// <inheritdoc/>
    public int DataBits     => _inner.DataBits;
    /// <inheritdoc/>
    public Enums.StopBits StopBits => MapBackStopBits(_inner.StopBits);
    /// <inheritdoc/>
    public Enums.FlowControl FlowControl => MapBackHandshake(_inner.Handshake);
    /// <inheritdoc/>
    public int ReadTimeout  { get => _inner.ReadTimeout;  set => _inner.ReadTimeout  = value; }
    /// <inheritdoc/>
    public int WriteTimeout { get => _inner.WriteTimeout; set => _inner.WriteTimeout = value; }
    /// <inheritdoc/>
    public bool IsOpen      => _inner.IsOpen;
    /// <inheritdoc/>
    public int BytesToRead  => _inner.BytesToRead;
    /// <inheritdoc/>
    public int BytesToWrite => _inner.BytesToWrite;
    /// <inheritdoc/>
    public Stream BaseStream => _inner.BaseStream;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialDataReceivedEventArgs>?  DataReceived;
    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;
    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>?    PinChanged;

    /// <inheritdoc/>
    public void Open()
    {
        ThrowIfDisposed();
        _logger.LogInformation("Opening serial port {PortName} at {BaudRate} bps.", PortName, BaudRate);
        try { _inner.Open(); }
        catch (System.IO.FileNotFoundException ex)
        {
            throw new Exceptions.SerialPortNotFoundException(PortName, ex);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException($"Timeout opening port '{PortName}'.", ex);
        }
        _logger.LogInformation("Serial port {PortName} opened successfully.", PortName);
    }

    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Close()
    {
        if (_inner.IsOpen)
        {
            _logger.LogInformation("Closing serial port {PortName}.", PortName);
            _inner.Close();
        }
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
        _logger.LogTrace("Writing {Count} bytes to {PortName}.", count, PortName);
        try { _inner.Write(buffer, offset, count); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("Write timed out.", ex); }
    }

    /// <inheritdoc/>
    public void Write(string text)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        _logger.LogTrace("Writing string ({Length} chars) to {PortName}.", text.Length, PortName);
        try { _inner.Write(text); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("Write timed out.", ex); }
    }

    /// <inheritdoc/>
    public void WriteLine(string text)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { _inner.WriteLine(text); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("WriteLine timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { await _inner.BaseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("WriteAsync timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { await _inner.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("WriteAsync timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text + _inner.NewLine);
        await WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return _inner.Read(buffer, offset, count); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("Read timed out.", ex); }
    }

    /// <inheritdoc/>
    public int ReadByte()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return _inner.ReadByte(); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("ReadByte timed out.", ex); }
    }

    /// <inheritdoc/>
    public string ReadExisting()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        return _inner.ReadExisting();
    }

    /// <inheritdoc/>
    public string ReadLine()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return _inner.ReadLine(); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("ReadLine timed out.", ex); }
    }

    /// <inheritdoc/>
    public string ReadTo(string value)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return _inner.ReadTo(value); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("ReadTo timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return await _inner.BaseStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("ReadAsync timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try { return await _inner.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false); }
        catch (TimeoutException ex) { throw new Exceptions.SerialPortTimeoutException("ReadAsync timed out.", ex); }
    }

    /// <inheritdoc/>
    public async Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        var sb = new System.Text.StringBuilder();
        var buf = new byte[1];
        var newLine = _inner.NewLine;
        while (!cancellationToken.IsCancellationRequested)
        {
            int read = await _inner.BaseStream.ReadAsync(buf.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            sb.Append((char)buf[0]);
            if (sb.ToString().EndsWith(newLine, StringComparison.Ordinal))
                return sb.ToString(0, sb.Length - newLine.Length);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return sb.ToString();
    }

    /// <inheritdoc/>
    public void DiscardInBuffer()  { ThrowIfDisposed(); _inner.DiscardInBuffer(); }
    /// <inheritdoc/>
    public void DiscardOutBuffer() { ThrowIfDisposed(); _inner.DiscardOutBuffer(); }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _inner.DataReceived  -= OnDataReceived;
        _inner.ErrorReceived -= OnErrorReceived;
        _inner.PinChanged    -= OnPinChanged;
        if (_inner.IsOpen) _inner.Close();
        _inner.Dispose();
        _logger.LogDebug("Serial port {PortName} disposed.", PortName);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DesktopSerialPort));
    }

    private void ThrowIfNotOpen()
    {
        if (!_inner.IsOpen)
            throw new Exceptions.SerialPortException($"Serial port '{PortName}' is not open.");
    }

    private void OnDataReceived(object sender, SysIO.SerialDataReceivedEventArgs e)
        => DataReceived?.Invoke(this, new Models.SerialDataReceivedEventArgs(
            e.EventType == SysIO.SerialData.Eof ? Enums.SerialData.Eof : Enums.SerialData.Chars));

    private void OnErrorReceived(object sender, SysIO.SerialErrorReceivedEventArgs e)
        => ErrorReceived?.Invoke(this, new Models.SerialErrorReceivedEventArgs(MapBackSerialError(e.EventType)));

    private void OnPinChanged(object sender, SysIO.SerialPinChangedEventArgs e)
        => PinChanged?.Invoke(this, new Models.SerialPinChangedEventArgs(MapBackPinChange(e.EventType)));

    private static SysIO.Parity MapParity(Enums.Parity p) => p switch
    {
        Enums.Parity.None  => SysIO.Parity.None,
        Enums.Parity.Odd   => SysIO.Parity.Odd,
        Enums.Parity.Even  => SysIO.Parity.Even,
        Enums.Parity.Mark  => SysIO.Parity.Mark,
        Enums.Parity.Space => SysIO.Parity.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
    };

    private static SysIO.StopBits MapStopBits(Enums.StopBits s) => s switch
    {
        Enums.StopBits.One          => SysIO.StopBits.One,
        Enums.StopBits.OnePointFive => SysIO.StopBits.OnePointFive,
        Enums.StopBits.Two          => SysIO.StopBits.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
    };

    private static SysIO.Handshake MapHandshake(Enums.FlowControl fc) => fc switch
    {
        Enums.FlowControl.None                    => SysIO.Handshake.None,
        Enums.FlowControl.XOnXOff                 => SysIO.Handshake.XOnXOff,
        Enums.FlowControl.RequestToSend           => SysIO.Handshake.RequestToSend,
        Enums.FlowControl.RequestToSendXOnXOff    => SysIO.Handshake.RequestToSendXOnXOff,
        _ => throw new ArgumentOutOfRangeException(nameof(fc), fc, null)
    };

    private static Enums.Parity MapBackParity(SysIO.Parity p) => p switch
    {
        SysIO.Parity.None  => Enums.Parity.None,
        SysIO.Parity.Odd   => Enums.Parity.Odd,
        SysIO.Parity.Even  => Enums.Parity.Even,
        SysIO.Parity.Mark  => Enums.Parity.Mark,
        SysIO.Parity.Space => Enums.Parity.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
    };

    private static Enums.StopBits MapBackStopBits(SysIO.StopBits s) => s switch
    {
        SysIO.StopBits.None         => Enums.StopBits.One,
        SysIO.StopBits.One          => Enums.StopBits.One,
        SysIO.StopBits.OnePointFive => Enums.StopBits.OnePointFive,
        SysIO.StopBits.Two          => Enums.StopBits.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
    };

    private static Enums.FlowControl MapBackHandshake(SysIO.Handshake h) => h switch
    {
        SysIO.Handshake.None                 => Enums.FlowControl.None,
        SysIO.Handshake.XOnXOff              => Enums.FlowControl.XOnXOff,
        SysIO.Handshake.RequestToSend        => Enums.FlowControl.RequestToSend,
        SysIO.Handshake.RequestToSendXOnXOff => Enums.FlowControl.RequestToSendXOnXOff,
        _ => throw new ArgumentOutOfRangeException(nameof(h), h, null)
    };

    private static Enums.SerialError MapBackSerialError(SysIO.SerialError e) => e switch
    {
        SysIO.SerialError.RXOver   => Enums.SerialError.RXOver,
        SysIO.SerialError.Overrun  => Enums.SerialError.Overrun,
        SysIO.SerialError.RXParity => Enums.SerialError.RXParity,
        SysIO.SerialError.Frame    => Enums.SerialError.Frame,
        SysIO.SerialError.TXFull   => Enums.SerialError.TXFull,
        _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
    };

    private static Enums.SerialPinChange MapBackPinChange(SysIO.SerialPinChange p) => p switch
    {
        SysIO.SerialPinChange.CtsChanged => Enums.SerialPinChange.CtsChanged,
        SysIO.SerialPinChange.DsrChanged => Enums.SerialPinChange.DsrChanged,
        SysIO.SerialPinChange.CDChanged  => Enums.SerialPinChange.CDChanged,
        SysIO.SerialPinChange.Ring       => Enums.SerialPinChange.Ring,
        SysIO.SerialPinChange.Break      => Enums.SerialPinChange.Break,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
    };
}


