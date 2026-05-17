// -----------------------------------------------------------------------
// <copyright file="WindowsSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Desktop (Windows, Linux, macOS) implementation of ISerialPort via System.IO.Ports.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Windows;

using DotSerial.Config;
using DotSerial.Internal.Desktop;
using Microsoft.Extensions.Logging;
using SysIO = System.IO.Ports;

/// <summary>
/// Desktop (Windows, Linux, macOS) implementation of <see cref="Abstractions.ISerialPort"/>
/// built on top of <see cref="SysIO.SerialPort"/>.
/// </summary>
internal sealed class WindowsSerialPort : Abstractions.ISerialPort
{
    private readonly SysIO.SerialPort _inner;
    private readonly ILogger<WindowsSerialPort> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="WindowsSerialPort"/> from <paramref name="config"/>.
    /// </summary>
    /// <param name="config">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    internal WindowsSerialPort(SerialPortConfig config, ILogger<WindowsSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        config.Validate();

        _logger = logger;
        _inner = new SysIO.SerialPort(
            config.PortName,
            (int)config.BaudRate,
            MapParity(config.Parity),
            config.DataBits,
            MapStopBits(config.StopBits))
        {
            Handshake = MapHandshake(config.FlowControl),
            ReadTimeout = config.ReadTimeout,
            WriteTimeout = config.WriteTimeout,
            ReadBufferSize = config.ReadBufferSize,
            WriteBufferSize = config.WriteBufferSize,
        };

        _inner.DataReceived += OnDataReceived;
        _inner.ErrorReceived += OnErrorReceived;
        _inner.PinChanged += OnPinChanged;
    }

    /// <inheritdoc/>
    public string PortName => _inner.PortName;

    /// <inheritdoc/>
    public BaudRate BaudRate => (BaudRate)_inner.BaudRate;

    /// <inheritdoc/>
    public Parity Parity => MapBackParity(_inner.Parity);

    /// <inheritdoc/>
    public int DataBits => _inner.DataBits;

    /// <inheritdoc/>
    public StopBits StopBits => MapBackStopBits(_inner.StopBits);

    /// <inheritdoc/>
    public FlowControl FlowControl => MapBackHandshake(_inner.Handshake);

    /// <inheritdoc/>
    public int ReadTimeout { get => _inner.ReadTimeout; set => _inner.ReadTimeout = value; }

    /// <inheritdoc/>
    public int WriteTimeout { get => _inner.WriteTimeout; set => _inner.WriteTimeout = value; }

    /// <inheritdoc/>
    public bool IsOpen => _inner.IsOpen;

    /// <inheritdoc/>
    public int BytesToRead => _inner.BytesToRead;

    /// <inheritdoc/>
    public int BytesToWrite => _inner.BytesToWrite;

    /// <inheritdoc/>
    public Stream BaseStream => _inner.BaseStream;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;

    /// <inheritdoc/>
    public void Open()
    {
        ThrowIfDisposed();
        _logger.PortOpening(PortName, BaudRate);
        try
        {
            _inner.Open();
        }
        catch (System.IO.FileNotFoundException ex)
        {
            throw new Exceptions.SerialPortNotFoundException(PortName, ex);
        }
        catch (UnauthorizedAccessException ex) when (ex.InnerException is System.IO.IOException)
        {
            throw new Exceptions.SerialPortNotFoundException(PortName, ex);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException($"Timeout opening port '{PortName}'.", ex);
        }

        _logger.PortOpened(PortName);
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
            _logger.PortClosing(PortName);
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
        _logger.WritingBytes(count, PortName);
        try
        {
            _inner.Write(buffer, offset, count);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("Write timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public void Write(string text)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        _logger.WritingString(text.Length, PortName);
        try
        {
            _inner.Write(text);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("Write timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public void WriteLine(string text)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            _inner.WriteLine(text);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("WriteLine timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            await _inner.BaseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("WriteAsync timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            await _inner.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("WriteAsync timed out.", ex);
        }
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
        try
        {
            return _inner.Read(buffer, offset, count);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("Read timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public int ReadByte()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            return _inner.ReadByte();
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("ReadByte timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public byte[] ReadExisting()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        var data = _inner.ReadExisting();
        return System.Text.Encoding.UTF8.GetBytes(data);
    }

    /// <inheritdoc/>
    public string ReadLine()
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            return _inner.ReadLine();
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("ReadLine timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public string ReadTo(string value)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            return _inner.ReadTo(value);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("ReadTo timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            return await _inner.BaseStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("ReadAsync timed out.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotOpen();
        try
        {
            return await _inner.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new Exceptions.SerialPortTimeoutException("ReadAsync timed out.", ex);
        }
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
            if (read == 0)
            {
                break;
            }

            sb.Append((char)buf[0]);
            if (sb.ToString().EndsWith(newLine, StringComparison.Ordinal))
            {
                return sb.ToString(0, sb.Length - newLine.Length);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return sb.ToString();
    }

    /// <inheritdoc/>
    public void DiscardInBuffer()
    {
        ThrowIfDisposed();
        _inner.DiscardInBuffer();
    }

    /// <inheritdoc/>
    public void DiscardOutBuffer()
    {
        ThrowIfDisposed();
        _inner.DiscardOutBuffer();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inner.DataReceived -= OnDataReceived;
        _inner.ErrorReceived -= OnErrorReceived;
        _inner.PinChanged -= OnPinChanged;

        if (_inner.IsOpen)
        {
            _inner.Close();
        }

        _inner.Dispose();
        _logger.PortDisposed(PortName);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WindowsSerialPort));
        }
    }

    private void ThrowIfNotOpen()
    {
        if (!_inner.IsOpen)
        {
            throw new Exceptions.SerialPortException($"Serial port '{PortName}' is not open.");
        }
    }

    private void OnDataReceived(object? sender, SysIO.SerialDataReceivedEventArgs e)
        => DataReceived?.Invoke(this, new Models.SerialDataReceivedEventArgs(
            e.EventType == SysIO.SerialData.Eof ? SerialData.Eof : SerialData.Chars));

    private void OnErrorReceived(object? sender, SysIO.SerialErrorReceivedEventArgs e)
        => ErrorReceived?.Invoke(this, new Models.SerialErrorReceivedEventArgs(MapBackSerialError(e.EventType)));

    private void OnPinChanged(object? sender, SysIO.SerialPinChangedEventArgs e)
        => PinChanged?.Invoke(this, new Models.SerialPinChangedEventArgs(MapBackPinChange(e.EventType)));

    private static SysIO.Parity MapParity(Parity p) => p switch
    {
        Parity.None => SysIO.Parity.None,
        Parity.Odd => SysIO.Parity.Odd,
        Parity.Even => SysIO.Parity.Even,
        Parity.Mark => SysIO.Parity.Mark,
        Parity.Space => SysIO.Parity.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null),
    };

    private static SysIO.StopBits MapStopBits(StopBits s) => s switch
    {
        StopBits.One => SysIO.StopBits.One,
        StopBits.OnePointFive => SysIO.StopBits.OnePointFive,
        StopBits.Two => SysIO.StopBits.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, null),
    };

    private static SysIO.Handshake MapHandshake(FlowControl fc) => fc switch
    {
        FlowControl.None => SysIO.Handshake.None,
        FlowControl.XOnXOff => SysIO.Handshake.XOnXOff,
        FlowControl.RequestToSend => SysIO.Handshake.RequestToSend,
        FlowControl.RequestToSendXOnXOff => SysIO.Handshake.RequestToSendXOnXOff,
        _ => throw new ArgumentOutOfRangeException(nameof(fc), fc, null),
    };

    private static Parity MapBackParity(SysIO.Parity p) => p switch
    {
        SysIO.Parity.None => Parity.None,
        SysIO.Parity.Odd => Parity.Odd,
        SysIO.Parity.Even => Parity.Even,
        SysIO.Parity.Mark => Parity.Mark,
        SysIO.Parity.Space => Parity.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null),
    };

    private static StopBits MapBackStopBits(SysIO.StopBits s) => s switch
    {
        SysIO.StopBits.None => StopBits.One,
        SysIO.StopBits.One => StopBits.One,
        SysIO.StopBits.OnePointFive => StopBits.OnePointFive,
        SysIO.StopBits.Two => StopBits.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, null),
    };

    private static FlowControl MapBackHandshake(SysIO.Handshake h) => h switch
    {
        SysIO.Handshake.None => FlowControl.None,
        SysIO.Handshake.XOnXOff => FlowControl.XOnXOff,
        SysIO.Handshake.RequestToSend => FlowControl.RequestToSend,
        SysIO.Handshake.RequestToSendXOnXOff => FlowControl.RequestToSendXOnXOff,
        _ => throw new ArgumentOutOfRangeException(nameof(h), h, null),
    };

    private static SerialError MapBackSerialError(SysIO.SerialError e) => e switch
    {
        SysIO.SerialError.RXOver => SerialError.RXOver,
        SysIO.SerialError.Overrun => SerialError.Overrun,
        SysIO.SerialError.RXParity => SerialError.RXParity,
        SysIO.SerialError.Frame => SerialError.Frame,
        SysIO.SerialError.TXFull => SerialError.TXFull,
        _ => throw new ArgumentOutOfRangeException(nameof(e), e, null),
    };

    private static SerialPinChange MapBackPinChange(SysIO.SerialPinChange p) => p switch
    {
        SysIO.SerialPinChange.CtsChanged => SerialPinChange.CtsChanged,
        SysIO.SerialPinChange.DsrChanged => SerialPinChange.DsrChanged,
        SysIO.SerialPinChange.CDChanged => SerialPinChange.CDChanged,
        SysIO.SerialPinChange.Ring => SerialPinChange.Ring,
        SysIO.SerialPinChange.Break => SerialPinChange.Break,
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null),
    };
}
