// -----------------------------------------------------------------------
// <copyright file="SerialPortBase.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Abstract base class providing shared boilerplate for all platform serial port implementations.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base class that provides shared boilerplate for all platform-specific
/// <see cref="Abstractions.ISerialPort"/> implementations.
/// </summary>
/// <remarks>
/// Derived classes must implement the core low-level operations (<see cref="Open"/>,
/// <see cref="Close"/>, <see cref="Write(byte[], int, int)"/>, <see cref="Read(byte[], int, int)"/>,
/// etc.). This base class provides default implementations of higher-level methods
/// (<see cref="ReadLine"/>, <see cref="ReadExisting"/>, <see cref="Write(string)"/>, etc.)
/// built on top of those primitives.
/// </remarks>
internal abstract class SerialPortBase : Abstractions.ISerialPort
{
    private static readonly System.Text.Encoding Encoding = System.Text.Encoding.UTF8;
    private static readonly string NewLine = "\n";

    /// <summary>The port configuration settings.</summary>
    protected readonly Models.SerialPortSettings _settings;

    /// <summary>The logger instance for diagnostic output.</summary>
    protected readonly ILogger _logger;

    private bool _disposed;
    private int _readTimeout;
    private int _writeTimeout;

    /// <summary>
    /// Initializes a new instance of <see cref="SerialPortBase"/> with the given settings and logger.
    /// </summary>
    /// <param name="settings">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    protected SerialPortBase(Models.SerialPortSettings settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        settings.Validate();
        _settings = settings;
        _logger = logger;
        _readTimeout = settings.ReadTimeout;
        _writeTimeout = settings.WriteTimeout;
    }

    // ── ISerialPort – Properties ──────────────────────────────────────────

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
        set => _readTimeout = value;
    }

    /// <inheritdoc/>
    public int WriteTimeout
    {
        get => _writeTimeout;
        set => _writeTimeout = value;
    }

    /// <inheritdoc/>
    public abstract bool IsOpen { get; }

    /// <inheritdoc/>
    public abstract int BytesToRead { get; }

    /// <inheritdoc/>
    public abstract int BytesToWrite { get; }

    /// <inheritdoc/>
    public abstract Stream BaseStream { get; }

    // ── Events ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;

    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;

    // ── Abstract operations ───────────────────────────────────────────────

    /// <inheritdoc/>
    public abstract void Open();

    /// <inheritdoc/>
    public abstract Task OpenAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract void Close();

    /// <inheritdoc/>
    public abstract Task CloseAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract void Write(byte[] buffer, int offset, int count);

    /// <inheritdoc/>
    public abstract int Read(byte[] buffer, int offset, int count);

    /// <inheritdoc/>
    public abstract int ReadByte();

    /// <inheritdoc/>
    public abstract void DiscardInBuffer();

    /// <inheritdoc/>
    public abstract void DiscardOutBuffer();

    // ── Default Write implementations ─────────────────────────────────────

    /// <inheritdoc/>
    public void Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfNotOpen();
        var bytes = Encoding.GetBytes(text);
        Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    public void WriteLine(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Write(text + NewLine);
    }

    /// <inheritdoc/>
    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Task.Run(() => Write(buffer, offset, count), cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            var bytes = buffer.ToArray();
            Write(bytes, 0, bytes.Length);
        }, cancellationToken);

    /// <inheritdoc/>
    public Task WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Task.Run(() => WriteLine(text), cancellationToken);
    }

    // ── Default Read implementations ──────────────────────────────────────

    /// <inheritdoc/>
    public string ReadExisting()
    {
        ThrowIfNotOpen();
        int available = BytesToRead;
        if (available <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[available];
        int read = Read(buffer, 0, available);
        return Encoding.GetString(buffer, 0, read);
    }

    /// <inheritdoc/>
    public string ReadLine()
    {
        ThrowIfNotOpen();
        return ReadTo(NewLine);
    }

    /// <inheritdoc/>
    public string ReadTo(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            throw new ArgumentException("Value must not be empty.", nameof(value));
        }

        ThrowIfNotOpen();

        var sb = new System.Text.StringBuilder();
        while (true)
        {
            int b = ReadByte();
            if (b == -1)
            {
                break;
            }

            sb.Append((char)b);
            if (sb.Length >= value.Length &&
                sb.ToString(sb.Length - value.Length, value.Length) == value)
            {
                return sb.ToString(0, sb.Length - value.Length);
            }
        }
        return sb.ToString();
    }

    /// <inheritdoc/>
    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Task.Run(() => Read(buffer, offset, count), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            var tmp = new byte[buffer.Length];
            int n = Read(tmp, 0, tmp.Length);
            tmp.AsMemory(0, n).CopyTo(buffer);
            return n;
        }, cancellationToken);

    /// <inheritdoc/>
    public Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
        => Task.Run(ReadLine, cancellationToken);

    // ── Event helpers ─────────────────────────────────────────────────────

    /// <summary>Raises the <see cref="DataReceived"/> event.</summary>
    /// <param name="args">The event arguments.</param>
    protected void OnDataReceived(Models.SerialDataReceivedEventArgs args)
        => DataReceived?.Invoke(this, args);

    /// <summary>Raises the <see cref="ErrorReceived"/> event.</summary>
    /// <param name="args">The event arguments.</param>
    protected void OnErrorReceived(Models.SerialErrorReceivedEventArgs args)
        => ErrorReceived?.Invoke(this, args);

    /// <summary>Raises the <see cref="PinChanged"/> event.</summary>
    /// <param name="args">The event arguments.</param>
    protected void OnPinChanged(Models.SerialPinChangedEventArgs args)
        => PinChanged?.Invoke(this, args);

    // ── Guard helpers ─────────────────────────────────────────────────────

    /// <summary>Throws <see cref="ObjectDisposedException"/> when this instance has been disposed.</summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>Throws <see cref="Exceptions.SerialPortException"/> when the port is not open.</summary>
    protected void ThrowIfNotOpen()
    {
        if (!IsOpen)
        {
            throw new Exceptions.SerialPortException($"Serial port '{PortName}' is not open.");
        }
    }

    // ── IDisposable / IAsyncDisposable ────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeManaged();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases managed resources. Called by <see cref="Dispose"/>.</summary>
    protected abstract void DisposeManaged();

    /// <summary>Releases managed resources asynchronously. Called by <see cref="DisposeAsync"/>.</summary>
    /// <returns>A <see cref="ValueTask"/> representing the async disposal.</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        DisposeManaged();
        return ValueTask.CompletedTask;
    }
}
