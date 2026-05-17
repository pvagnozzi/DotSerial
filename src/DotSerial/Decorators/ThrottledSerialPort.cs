// -----------------------------------------------------------------------
// <copyright file="ThrottledSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Platform-agnostic write-rate limiter decorator for ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Decorators;

using DotSerial.Abstractions;
using DotSerial.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Platform-agnostic decorator that wraps any <see cref="ISerialPort"/> and applies
/// a token-bucket write-rate limit to control the throughput of data transmitted.
/// </summary>
/// <remarks>
/// <para>
/// This decorator implements a token-bucket algorithm for write throttling:
/// <list type="bullet">
///   <item>Each call to <see cref="Write(byte[], int, int)"/> or <see cref="WriteAsync(byte[], int, int, CancellationToken)"/>
///   consumes tokens equal to the number of bytes written.</item>
///   <item>Tokens are regenerated at a fixed rate (specified in bytes per second).</item>
///   <item>If tokens are insufficient, the write operation blocks or asynchronously waits.</item>
///   <item>Synchronous writes use Thread.Sleep; asynchronous writes use Task.Delay.</item>
/// </list>
/// </para>
/// <para>
/// All read operations, properties, and events are delegated unmodified to the wrapped port.
/// Thread-safety is ensured via a <see cref="SemaphoreSlim"/> for token-bucket state.
/// </para>
/// </remarks>
internal sealed class ThrottledSerialPort : ISerialPort
{
    private readonly ISerialPort _inner;
    private readonly ILogger<ThrottledSerialPort> _logger;
    private readonly int _maxBytesPerSecond;
    private readonly SemaphoreSlim _bucketLock;

    private double _tokens;
    private DateTime _lastRefillTime;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="ThrottledSerialPort"/> that wraps the specified port
    /// with a write-rate limit.
    /// </summary>
    /// <param name="inner">The underlying serial port implementation to wrap.</param>
    /// <param name="maxBytesPerSecond">The maximum write rate in bytes per second.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxBytesPerSecond"/> is less than or equal to zero.
    /// </exception>
    internal ThrottledSerialPort(ISerialPort inner, int maxBytesPerSecond, ILogger<ThrottledSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        if (maxBytesPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytesPerSecond), maxBytesPerSecond, "Must be greater than zero.");

        _inner = inner;
        _maxBytesPerSecond = maxBytesPerSecond;
        _logger = logger;
        _bucketLock = new SemaphoreSlim(1, 1);
        _tokens = _maxBytesPerSecond; // Start with a full bucket.
        _lastRefillTime = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public bool IsOpen => _inner.IsOpen;

    /// <inheritdoc/>
    public int BytesToRead => _inner.BytesToRead;

    /// <inheritdoc/>
    public int BytesToWrite => _inner.BytesToWrite;

    /// <inheritdoc/>
    public Stream BaseStream => _inner.BaseStream;

    /// <inheritdoc/>
    public int ReadTimeout
    {
        get => _inner.ReadTimeout;
        set => _inner.ReadTimeout = value;
    }

    /// <inheritdoc/>
    public int WriteTimeout
    {
        get => _inner.WriteTimeout;
        set => _inner.WriteTimeout = value;
    }

    /// <inheritdoc/>
    public event EventHandler<SerialDataReceivedEventArgs>? DataReceived
    {
        add => _inner.DataReceived += value;
        remove => _inner.DataReceived -= value;
    }

    /// <inheritdoc/>
    public event EventHandler<SerialErrorReceivedEventArgs>? ErrorReceived
    {
        add => _inner.ErrorReceived += value;
        remove => _inner.ErrorReceived -= value;
    }

    /// <inheritdoc/>
    public event EventHandler<SerialPinChangedEventArgs>? PinChanged
    {
        add => _inner.PinChanged += value;
        remove => _inner.PinChanged -= value;
    }

    /// <inheritdoc/>
    public void Open()
    {
        _inner.Open();
    }

    /// <inheritdoc/>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await _inner.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Close()
    {
        _inner.Close();
    }

    /// <inheritdoc/>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _inner.CloseAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Write(byte[] buffer, int offset, int count)
    {
        ThrottleWrite(count, waitMs: -1);
        _inner.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        await ThrottleWriteAsync(count, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleWriteAsync(buffer.Length, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count)
    {
        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public int ReadByte()
    {
        return _inner.ReadByte();
    }

    /// <inheritdoc/>
    public byte[] ReadExisting()
    {
        return _inner.ReadExisting();
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void DiscardInBuffer()
    {
        _inner.DiscardInBuffer();
    }

    /// <inheritdoc/>
    public void DiscardOutBuffer()
    {
        _inner.DiscardOutBuffer();
    }

    /// <summary>
    /// Synchronously waits until sufficient tokens are available to transmit <paramref name="byteCount"/> bytes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to consume from the token bucket.</param>
    /// <param name="waitMs">The maximum time to wait in milliseconds. Use -1 for infinite wait.</param>
    /// <remarks>
    /// This method blocks the current thread using Thread.Sleep if tokens are insufficient.
    /// </remarks>
    private void ThrottleWrite(int byteCount, int waitMs = -1)
    {
        var remaining = byteCount;
        var deadline = waitMs < 0 ? DateTime.MaxValue : DateTime.UtcNow.AddMilliseconds(waitMs);

        while (remaining > 0)
        {
            _bucketLock.Wait();
            try
            {
                RefillTokens();

                if (_tokens >= remaining)
                {
                    _tokens -= remaining;
                    remaining = 0;
                }
                else
                {
                    remaining -= (int)_tokens;
                    _tokens = 0;

                    // Calculate how long to sleep.
                    var tokensNeeded = remaining;
                    var sleepSeconds = (double)tokensNeeded / _maxBytesPerSecond;
                    var sleepMs = Math.Max(1, (int)(sleepSeconds * 1000));

                    if (DateTime.UtcNow.AddMilliseconds(sleepMs) > deadline)
                        throw new TimeoutException("Write throttle timeout.");

                    _logger.LogTrace("Throttling write: {ByteCount} bytes, sleeping {SleepMs}ms.",
                        byteCount, sleepMs);

                    Thread.Sleep(sleepMs);
                }
            }
            finally
            {
                _bucketLock.Release();
            }
        }
    }

    /// <summary>
    /// Asynchronously waits until sufficient tokens are available to transmit <paramref name="byteCount"/> bytes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to consume from the token bucket.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// This method asynchronously waits using Task.Delay if tokens are insufficient.
    /// </remarks>
    private async Task ThrottleWriteAsync(int byteCount, CancellationToken cancellationToken)
    {
        var remaining = byteCount;

        while (remaining > 0)
        {
            await _bucketLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                RefillTokens();

                if (_tokens >= remaining)
                {
                    _tokens -= remaining;
                    remaining = 0;
                }
                else
                {
                    remaining -= (int)_tokens;
                    _tokens = 0;

                    var tokensNeeded = remaining;
                    var sleepSeconds = (double)tokensNeeded / _maxBytesPerSecond;
                    var sleepMs = Math.Max(1, (int)(sleepSeconds * 1000));

                    _logger.LogTrace("Throttling async write: {ByteCount} bytes, delaying {DelayMs}ms.",
                        byteCount, sleepMs);

                    await Task.Delay(sleepMs, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _bucketLock.Release();
            }
        }
    }

    /// <summary>
    /// Refills the token bucket based on elapsed time since the last refill.
    /// </summary>
    /// <remarks>
    /// Caller must hold <see cref="_bucketLock"/>.
    /// </remarks>
    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefillTime;
        var tokensToAdd = elapsed.TotalSeconds * _maxBytesPerSecond;
        _tokens = Math.Min(_tokens + tokensToAdd, _maxBytesPerSecond);
        _lastRefillTime = now;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if the port has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThrottledSerialPort));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _bucketLock.Dispose();
        _inner.Dispose();
        _disposed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _bucketLock.Dispose();
        await _inner.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
