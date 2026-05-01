// -----------------------------------------------------------------------
// <copyright file="ThrottledSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Decorator that limits write throughput on any ISerialPort to a configurable
//     maximum bytes-per-second using a token-bucket algorithm.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Decorators;

using Microsoft.Extensions.Logging;

/// <summary>
/// A decorator for <see cref="Abstractions.ISerialPort"/> that throttles write
/// operations to a configurable maximum throughput (bytes per second), preventing
/// buffer overflow on slow receivers or hardware-constrained devices.
/// </summary>
/// <remarks>
/// The throttle is applied to all synchronous and asynchronous write methods.
/// Read operations and all other members are delegated to the wrapped port unchanged.
/// The implementation uses a token-bucket algorithm: tokens are replenished at
/// <c>maxBytesPerSecond</c> per second, and each write consumes tokens equal to
/// the number of bytes being written. If insufficient tokens are available the
/// call sleeps for the exact duration needed before proceeding.
/// </remarks>
public sealed class ThrottledSerialPort : Abstractions.ISerialPort
{
    private readonly Abstractions.ISerialPort _inner;
    private readonly int _maxBytesPerSecond;
    private readonly ILogger<ThrottledSerialPort> _logger;

    // Token-bucket state — all access serialised through _bucketLock.
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private double _availableTokens;
    private DateTime _lastRefill;

    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="ThrottledSerialPort"/> that wraps
    /// <paramref name="inner"/> and limits writes to <paramref name="maxBytesPerSecond"/>.
    /// </summary>
    /// <param name="inner">The underlying serial port to wrap. Must not be <see langword="null"/>.</param>
    /// <param name="maxBytesPerSecond">
    /// Maximum write throughput in bytes per second. Must be greater than zero.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxBytesPerSecond"/> is less than or equal to zero.
    /// </exception>
    public ThrottledSerialPort(
        Abstractions.ISerialPort inner,
        int maxBytesPerSecond,
        ILogger<ThrottledSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);

        if (maxBytesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBytesPerSecond),
                maxBytesPerSecond,
                "Maximum bytes per second must be greater than zero.");
        }

        _inner = inner;
        _maxBytesPerSecond = maxBytesPerSecond;
        _logger = logger;
        _availableTokens = maxBytesPerSecond;
        _lastRefill = DateTime.UtcNow;
    }

    // ── ISerialPort – Properties (all delegated) ───────────────────────────

    /// <inheritdoc/>
    public string PortName => _inner.PortName;

    /// <inheritdoc/>
    public int BaudRate => _inner.BaudRate;

    /// <inheritdoc/>
    public Enums.Parity Parity => _inner.Parity;

    /// <inheritdoc/>
    public int DataBits => _inner.DataBits;

    /// <inheritdoc/>
    public Enums.StopBits StopBits => _inner.StopBits;

    /// <inheritdoc/>
    public Enums.FlowControl FlowControl => _inner.FlowControl;

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

    // ── ISerialPort – Events (all delegated) ──────────────────────────────

    /// <inheritdoc/>
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived
    {
        add => _inner.DataReceived += value;
        remove => _inner.DataReceived -= value;
    }

    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived
    {
        add => _inner.ErrorReceived += value;
        remove => _inner.ErrorReceived -= value;
    }

    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged
    {
        add => _inner.PinChanged += value;
        remove => _inner.PinChanged -= value;
    }

    // ── Lifecycle (delegated) ─────────────────────────────────────────────

    /// <inheritdoc/>
    public void Open()
    {
        ThrowIfDisposed();
        _inner.Open();
    }

    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _inner.OpenAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Close() => _inner.Close();

    /// <inheritdoc/>
    public Task CloseAsync(CancellationToken cancellationToken = default)
        => _inner.CloseAsync(cancellationToken);

    // ── Write (throttled) ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrottleSync(count);
        _inner.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public void Write(string text)
    {
        ThrowIfDisposed();
        ThrottleSync(System.Text.Encoding.UTF8.GetByteCount(text));
        _inner.Write(text);
    }

    /// <inheritdoc/>
    public void WriteLine(string text)
    {
        ThrowIfDisposed();
        ThrottleSync(System.Text.Encoding.UTF8.GetByteCount(text) + Environment.NewLine.Length);
        _inner.WriteLine(text);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ThrottleAsync(count, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ThrottleAsync(buffer.Length, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ThrottleAsync(
            System.Text.Encoding.UTF8.GetByteCount(text) + Environment.NewLine.Length,
            cancellationToken).ConfigureAwait(false);
        await _inner.WriteLineAsync(text, cancellationToken).ConfigureAwait(false);
    }

    // ── Read (all delegated unchanged) ────────────────────────────────────

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    /// <inheritdoc/>
    public int ReadByte() => _inner.ReadByte();

    /// <inheritdoc/>
    public string ReadExisting() => _inner.ReadExisting();

    /// <inheritdoc/>
    public string ReadLine() => _inner.ReadLine();

    /// <inheritdoc/>
    public string ReadTo(string value) => _inner.ReadTo(value);

    /// <inheritdoc/>
    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc/>
    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc/>
    public Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
        => _inner.ReadLineAsync(cancellationToken);

    // ── Buffer Management (delegated) ────────────────────────────────────

    /// <inheritdoc/>
    public void DiscardInBuffer() => _inner.DiscardInBuffer();

    /// <inheritdoc/>
    public void DiscardOutBuffer() => _inner.DiscardOutBuffer();

    // ── IDisposable / IAsyncDisposable ─────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bucketLock.Dispose();
        _inner.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _bucketLock.Dispose();
        await _inner.DisposeAsync().ConfigureAwait(false);
    }

    // ── Token-bucket internals ─────────────────────────────────────────────

    /// <summary>
    /// Synchronous throttle: blocks the calling thread for the exact duration needed
    /// to honour the configured rate limit before the write proceeds.
    /// </summary>
    /// <param name="byteCount">Number of bytes about to be written.</param>
    private void ThrottleSync(int byteCount)
    {
        if (byteCount <= 0) return;

        _bucketLock.Wait();
        try
        {
            var delayMs = ComputeDelayAndConsumeTokens(byteCount);
            if (delayMs > 0)
            {
                _logger.LogTrace(
                    "Throttling write of {Bytes} B on {Port} — sleeping {Ms} ms.",
                    byteCount, PortName, delayMs);
                Thread.Sleep(delayMs);
            }
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    /// <summary>
    /// Asynchronous throttle: awaits <c>Task.Delay</c> for the exact duration
    /// needed to honour the configured rate limit before the write proceeds.
    /// </summary>
    /// <param name="byteCount">Number of bytes about to be written.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    private async Task ThrottleAsync(int byteCount, CancellationToken cancellationToken)
    {
        if (byteCount <= 0) return;

        await _bucketLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var delayMs = ComputeDelayAndConsumeTokens(byteCount);
            if (delayMs > 0)
            {
                _logger.LogTrace(
                    "Throttling async write of {Bytes} B on {Port} — delaying {Ms} ms.",
                    byteCount, PortName, delayMs);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    /// <summary>
    /// Refills the token bucket based on elapsed time, computes how long to wait
    /// (if the bucket cannot cover <paramref name="byteCount"/> immediately), and
    /// deducts the tokens. Must be called while <c>_bucketLock</c> is held.
    /// </summary>
    /// <param name="byteCount">Number of tokens (bytes) to consume.</param>
    /// <returns>Milliseconds to wait before writing; zero if tokens are sufficient.</returns>
    private int ComputeDelayAndConsumeTokens(int byteCount)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;

        // Refill tokens proportionally to elapsed time, capped at the bucket size.
        _availableTokens = Math.Min(
            _maxBytesPerSecond,
            _availableTokens + elapsed * _maxBytesPerSecond);
        _lastRefill = now;

        if (_availableTokens >= byteCount)
        {
            _availableTokens -= byteCount;
            return 0;
        }

        // Calculate the delay required for sufficient tokens to accumulate.
        var deficit = byteCount - _availableTokens;
        var delayMs = (int)Math.Ceiling(deficit / _maxBytesPerSecond * 1000.0);
        _availableTokens = 0;
        return delayMs;
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> if this instance has been disposed.</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ThrottledSerialPort));
    }
}
