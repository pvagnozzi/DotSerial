// -----------------------------------------------------------------------
// <copyright file="AndroidBluetoothSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Android Bluetooth RFCOMM (Serial Port Profile) implementation of ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Android;

using DotSerial.Config;
using global::Android.Bluetooth;
using global::Android.Content;
using Microsoft.Extensions.Logging;

/// <summary>
/// Android implementation of <see cref="Abstractions.ISerialPort"/> using Bluetooth
/// RFCOMM via the Serial Port Profile (SPP).
/// </summary>
/// <remarks>
/// <para>
/// Uses the Android Bluetooth API (<c>Android.Bluetooth</c>) to connect to a remote
/// Bluetooth device that exposes SPP. The MAC address is read from
/// <see cref="SerialPortConfig.BluetoothAddress"/> (falling back to
/// <see cref="SerialPortConfig.PortName"/>).
/// </para>
/// <para>
/// <b>Prerequisites:</b> <c>BLUETOOTH</c>, <c>BLUETOOTH_ADMIN</c>, and
/// (API ≥ 31) <c>BLUETOOTH_CONNECT</c> permissions must be declared and granted.
/// </para>
/// </remarks>
internal sealed class AndroidBluetoothSerialPort : SerialPortBase
{
    /// <summary>The Bluetooth Serial Port Profile (SPP) service UUID.</summary>
    private const string SppUuid = "00001101-0000-1000-8000-00805F9B34FB";

    private const int DataReceivedPollIntervalMs = 50;

    private BluetoothSocket? _socket;
    private Stream? _inputStream;
    private Stream? _outputStream;
    private BluetoothDuplexStream? _duplexStream;
    private bool _isOpen;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    /// <summary>
    /// Initializes a new <see cref="AndroidBluetoothSerialPort"/>.
    /// </summary>
    /// <param name="config">
    /// Serial port configuration. <see cref="SerialPortConfig.BluetoothAddress"/> (or
    /// <see cref="SerialPortConfig.PortName"/>) must be the remote device MAC address,
    /// e.g. <c>"00:11:22:33:44:55"</c>.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    internal AndroidBluetoothSerialPort(
        SerialPortConfig config,
        ILogger<AndroidBluetoothSerialPort> logger)
        : base(config, logger)
    {
    }

    /// <inheritdoc/>
    public override bool IsOpen => _isOpen;

    /// <inheritdoc/>
    public override int BytesToRead => 0; // Not exposed by BluetoothSocket

    /// <inheritdoc/>
    public override int BytesToWrite => 0;

    /// <inheritdoc/>
    public override Stream BaseStream
    {
        get
        {
            ThrowIfNotOpen();
            return _duplexStream!;
        }
    }

    // ── Open / Close ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Open()
    {
        ThrowIfDisposed();
        if (_isOpen) return;

        string macAddress = _config.BluetoothAddress ?? _config.PortName;
        _logger.BtOpening(macAddress);

        BluetoothAdapter adapter = GetBluetoothAdapter()
            ?? throw new Exceptions.SerialPortException("Bluetooth adapter not available.");

        if (!adapter.IsEnabled)
            throw new Exceptions.SerialPortException("Bluetooth adapter is not enabled.");

        BluetoothDevice device = adapter.GetRemoteDevice(macAddress)
            ?? throw new Exceptions.SerialPortNotFoundException(macAddress);

        var uuid = Java.Util.UUID.FromString(SppUuid)!;

        try
        {
            _socket = device.CreateRfcommSocketToServiceRecord(uuid)!;
        }
        catch (Java.IO.IOException ex)
        {
            throw new Exceptions.SerialPortException(
                $"Failed to create RFCOMM socket to '{macAddress}'.", ex);
        }

        adapter.CancelDiscovery();

        try
        {
            _socket.Connect();
        }
        catch (Java.IO.IOException ex)
        {
            _socket.Close();
            _socket = null;
            throw new Exceptions.SerialPortException(
                $"Failed to connect Bluetooth RFCOMM socket to '{macAddress}'.", ex);
        }

        _inputStream = _socket.InputStream!;
        _outputStream = _socket.OutputStream!;
        _duplexStream = new BluetoothDuplexStream(_inputStream, _outputStream);
        _isOpen = true;

        StartPolling();
        _logger.BtOpened(macAddress);
    }

    /// <inheritdoc/>
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(Open, cancellationToken);
    }

    /// <inheritdoc/>
    public override void Close()
    {
        if (!_isOpen) return;
        string macAddress = _config.BluetoothAddress ?? _config.PortName;
        _logger.BtClosing(macAddress);
        StopPolling();
        try { _socket?.Close(); } catch (Java.IO.IOException ex) { _logger.BtCloseError(ex); }
        _socket = null;
        _inputStream = null;
        _outputStream = null;
        _isOpen = false;
    }

    /// <inheritdoc/>
    public override Task CloseAsync(CancellationToken cancellationToken = default)
        => Task.Run(Close, cancellationToken);

    // ── Read / Write ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfNotOpen();
        _logger.BtWritingBytes(count);
        try
        {
            _outputStream!.Write(buffer, offset, count);
        }
        catch (Java.IO.IOException ex)
        {
            throw new Exceptions.SerialPortException("Bluetooth write failed.", ex);
        }
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfNotOpen();
        try
        {
            int n = _inputStream!.Read(buffer, offset, count);
            _logger.BtReadBytes(n);
            return n;
        }
        catch (Java.IO.IOException ex)
        {
            throw new Exceptions.SerialPortException("Bluetooth read failed.", ex);
        }
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        ThrowIfNotOpen();
        try
        {
            return _inputStream!.ReadByte();
        }
        catch (Java.IO.IOException ex)
        {
            throw new Exceptions.SerialPortException("Bluetooth ReadByte failed.", ex);
        }
    }

    /// <inheritdoc/>
    public override void DiscardInBuffer() { /* Not directly supported by BluetoothSocket */ }

    /// <inheritdoc/>
    public override void DiscardOutBuffer() { /* Not directly supported by BluetoothSocket */ }

    // ── Dispose ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void DisposeManaged()
    {
        Close();
        _duplexStream?.Dispose();
        _duplexStream = null;
    }

    // ── Polling ───────────────────────────────────────────────────────────

    /// <summary>Starts the background data-availability polling task.</summary>
    private void StartPolling()
    {
        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(() => PollLoopAsync(_pollCts.Token), _pollCts.Token);
    }

    /// <summary>Stops the background polling task.</summary>
    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
        _pollTask = null;
    }

    /// <summary>Background loop that fires <see cref="SerialPortBase.DataReceived"/> when data is available on the input stream.</summary>
    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isOpen)
        {
            try
            {
                if (_inputStream is not null && _inputStream.IsDataAvailable())
                    OnDataReceived(new Models.SerialDataReceivedEventArgs(SerialData.Chars));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.BtPollError(ex);
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(DataReceivedPollIntervalMs, ct).ConfigureAwait(false);
        }
    }

    // ── Inner duplex stream ───────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="BluetoothAdapter"/> using the recommended API:
    /// <see cref="BluetoothManager"/> on API 31+ (avoids the deprecated <c>BluetoothAdapter.DefaultAdapter</c>).
    /// </summary>
    private static BluetoothAdapter? GetBluetoothAdapter()
    {
        var context = global::Android.App.Application.Context;
        var manager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
        return manager?.Adapter;
    }

    /// <summary>
    /// A read/write <see cref="Stream"/> that delegates reads to the Bluetooth input stream
    /// and writes to the Bluetooth output stream.
    /// </summary>
    private sealed class BluetoothDuplexStream : Stream
    {
        private readonly Stream _input;
        private readonly Stream _output;

        /// <summary>
        /// Initializes a new instance of <see cref="BluetoothDuplexStream"/>.
        /// </summary>
        /// <param name="input">The Bluetooth input stream for reading.</param>
        /// <param name="output">The Bluetooth output stream for writing.</param>
        public BluetoothDuplexStream(Stream input, Stream output)
        {
            _input = input;
            _output = output;
        }

        /// <inheritdoc/>
        public override bool CanRead => true;
        /// <inheritdoc/>
        public override bool CanWrite => true;
        /// <inheritdoc/>
        public override bool CanSeek => false;
        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => _input.Read(buffer, offset, count);
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => _output.Write(buffer, offset, count);
        /// <inheritdoc/>
        public override void Flush() => _output.Flush();
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
