// -----------------------------------------------------------------------
// <copyright file="AndroidSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Android USB Host Mode (CDC-ACM) implementation of ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Android;

using global::Android.Hardware.Usb;
using Microsoft.Extensions.Logging;

/// <summary>
/// Android implementation of <see cref="Abstractions.ISerialPort"/> using USB Host Mode
/// targeting CDC-ACM class devices (most common USB-to-serial adapters).
/// </summary>
/// <remarks>
/// <para>
/// Communicates with CDC-ACM USB serial devices (class 0x02, subclass 0x02) via the
/// <c>Android.Hardware.Usb</c> API. Supports baud rate, parity, stop bits, and data bits
/// configuration via the SET_LINE_CODING control request.
/// </para>
/// <para>
/// <b>Prerequisites:</b>
/// <list type="bullet">
///   <item><c>AndroidManifest.xml</c> must declare the <c>android.hardware.usb.host</c> feature.</item>
///   <item>The <c>android.permission.USB_PERMISSION</c> must have been granted by the user.</item>
/// </list>
/// </para>
/// <para>
/// <b>PortName convention:</b>
/// <list type="bullet">
///   <item><c>"USB0"</c>, <c>"USB1"</c>, … → index into <see cref="UsbManager.DeviceList"/> sorted by device name.</item>
///   <item>Any other string → exact match on <see cref="UsbDevice.DeviceName"/>.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class AndroidSerialPort : SerialPortBase
{
    private const int SetLineCodingRequest = 0x20;
    private const int SetControlLineStateRequest = 0x22;
    private const int ClassInterfaceHostToDevice = 0x21;
    private const int UsbSubclassAcm = 0x02;
    private const int DataReceivedPollIntervalMs = 20;

    private UsbManager? _usbManager;
    private UsbDeviceConnection? _connection;
    private UsbEndpoint? _bulkIn;
    private UsbEndpoint? _bulkOut;
    private int _controlInterfaceNumber;
    private int _dataInterfaceNumber;
    private bool _isOpen;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private UsbSerialStream? _stream;

    /// <summary>
    /// Initializes a new instance of <see cref="AndroidSerialPort"/>.
    /// </summary>
    /// <param name="settings">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    internal AndroidSerialPort(
        Models.SerialPortSettings settings,
        ILogger<AndroidSerialPort> logger)
        : base(settings, logger)
    {
    }

    /// <inheritdoc/>
    public override bool IsOpen => _isOpen;

    /// <inheritdoc/>
    public override int BytesToRead => 0; // USB bulk — buffer managed in UsbSerialStream

    /// <inheritdoc/>
    public override int BytesToWrite => 0;

    /// <inheritdoc/>
    public override Stream BaseStream
    {
        get
        {
            ThrowIfNotOpen();
            return _stream!;
        }
    }

    // ── Open / Close ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Open()
    {
        ThrowIfDisposed();
        if (_isOpen) return;

        _logger.UsbOpening(PortName);

        _usbManager = (UsbManager)global::Android.App.Application.Context
            .GetSystemService(global::Android.Content.Context.UsbService)!;

        var device = ResolveDevice(_usbManager);
        FindCdcAcmInterfaces(device, out var controlIface, out var dataIface);

        _controlInterfaceNumber = controlIface.Id;
        _dataInterfaceNumber = dataIface.Id;

        _connection = _usbManager.OpenDevice(device)
            ?? throw new Exceptions.SerialPortException($"Failed to open USB device '{device.DeviceName}'.");

        if (!_connection.ClaimInterface(controlIface, true))
            throw new Exceptions.SerialPortException("Failed to claim CDC control interface.");
        if (!_connection.ClaimInterface(dataIface, true))
            throw new Exceptions.SerialPortException("Failed to claim CDC data interface.");

        FindBulkEndpoints(dataIface, out _bulkIn, out _bulkOut);

        SendSetLineCoding();
        SendSetControlLineState();

        _stream = new UsbSerialStream(_connection, _bulkIn, _bulkOut, this);
        _isOpen = true;

        StartPolling();
        _logger.UsbOpened(PortName);
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
        _logger.UsbClosing(PortName);
        StopPolling();
        _connection?.Close();
        _connection = null;
        _isOpen = false;
        _logger.UsbClosed(PortName);
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
        _logger.UsbWritingBytes(count, PortName);
        var data = new byte[count];
        Buffer.BlockCopy(buffer, offset, data, 0, count);
        int transferred = _connection!.BulkTransfer(_bulkOut, data, count, WriteTimeout);
        if (transferred < 0)
            throw new Exceptions.SerialPortException($"USB bulk write to '{PortName}' failed.");
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfNotOpen();
        var tmp = new byte[count];
        int n = _connection!.BulkTransfer(_bulkIn, tmp, count, ReadTimeout);
        if (n < 0) return 0;
        Buffer.BlockCopy(tmp, 0, buffer, offset, n);
        _logger.UsbReadBytes(n, PortName);
        return n;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        ThrowIfNotOpen();
        var buf = new byte[1];
        int n = _connection!.BulkTransfer(_bulkIn, buf, 1, ReadTimeout);
        return n == 1 ? buf[0] : -1;
    }

    /// <inheritdoc/>
    public override void DiscardInBuffer() { /* No direct API on USB */ }

    /// <inheritdoc/>
    public override void DiscardOutBuffer() { /* No direct API on USB */ }

    // ── Dispose ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void DisposeManaged()
    {
        Close();
        _stream?.Dispose();
        _stream = null;
    }

    // ── USB helpers ───────────────────────────────────────────────────────

    /// <summary>Resolves the <see cref="UsbDevice"/> to open based on <see cref="SerialPortBase.PortName"/>.</summary>
    private UsbDevice ResolveDevice(UsbManager usbManager)
    {
        var deviceList = usbManager.DeviceList
            ?? throw new Exceptions.SerialPortNotFoundException(PortName);

        if (deviceList.Count == 0)
            throw new Exceptions.SerialPortNotFoundException(PortName);

        // Try USB0, USB1, … index-based naming
        if (PortName.StartsWith("USB", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(PortName.AsSpan(3), out int idx))
        {
            var sorted = deviceList.Values.OrderBy(d => d.DeviceName).ToList();
            if (idx >= 0 && idx < sorted.Count)
                return sorted[idx];
        }

        // Try exact device name match
        var match = deviceList.Values.FirstOrDefault(d => d.DeviceName == PortName);
        if (match is not null) return match;

        var available = string.Join(", ", deviceList.Values.Select(d => d.DeviceName));
        throw new Exceptions.SerialPortNotFoundException(PortName,
            new Exceptions.SerialPortException(
                $"Available USB devices: [{available}]"));
    }

    /// <summary>Finds the CDC control and data interfaces on the given <paramref name="device"/>.</summary>
    private static void FindCdcAcmInterfaces(
        UsbDevice device,
        out UsbInterface controlIface,
        out UsbInterface dataIface)
    {
        UsbInterface? ctrl = null;
        UsbInterface? data = null;

        for (int i = 0; i < device.InterfaceCount; i++)
        {
            var iface = device.GetInterface(i);
            if (iface.InterfaceClass == UsbClass.Comm && (int)iface.InterfaceSubclass == UsbSubclassAcm)
                ctrl = iface;
            else if (iface.InterfaceClass == UsbClass.CdcData)
                data = iface;
        }

        controlIface = ctrl ?? throw new Exceptions.SerialPortException(
            "CDC ACM control interface not found on USB device.");
        dataIface = data ?? throw new Exceptions.SerialPortException(
            "CDC ACM data interface not found on USB device.");
    }

    /// <summary>Finds the bulk-in and bulk-out endpoints in the CDC data <paramref name="iface"/>.</summary>
    private static void FindBulkEndpoints(
        UsbInterface iface,
        out UsbEndpoint bulkIn,
        out UsbEndpoint bulkOut)
    {
        UsbEndpoint? input = null;
        UsbEndpoint? output = null;

        for (int i = 0; i < iface.EndpointCount; i++)
        {
            var ep = iface.GetEndpoint(i);
            if (ep is null) continue;
            if (ep.Type == UsbAddressing.XferBulk)
            {
                if (ep.Direction == UsbAddressing.In)
                    input = ep;
                else
                    output = ep;
            }
        }

        bulkIn = input ?? throw new Exceptions.SerialPortException("CDC bulk-in endpoint not found.");
        bulkOut = output ?? throw new Exceptions.SerialPortException("CDC bulk-out endpoint not found.");
    }

    /// <summary>Sends the CDC SET_LINE_CODING control request to configure baud rate, stop bits, parity, and data bits.</summary>
    private void SendSetLineCoding()
    {
        byte stopBitsByte = _settings.StopBits switch
        {
            Enums.StopBits.One => 0,
            Enums.StopBits.OnePointFive => 1,
            Enums.StopBits.Two => 2,
            _ => 0
        };

        byte parityByte = _settings.Parity switch
        {
            Enums.Parity.None => 0,
            Enums.Parity.Odd => 1,
            Enums.Parity.Even => 2,
            Enums.Parity.Mark => 3,
            Enums.Parity.Space => 4,
            _ => 0
        };

        var lineCoding = new byte[7];
        var baud = (uint)BaudRate;
        lineCoding[0] = (byte)(baud & 0xFF);
        lineCoding[1] = (byte)((baud >> 8) & 0xFF);
        lineCoding[2] = (byte)((baud >> 16) & 0xFF);
        lineCoding[3] = (byte)((baud >> 24) & 0xFF);
        lineCoding[4] = stopBitsByte;
        lineCoding[5] = parityByte;
        lineCoding[6] = (byte)DataBits;

        _connection!.ControlTransfer(
            (UsbAddressing)ClassInterfaceHostToDevice,
            SetLineCodingRequest,
            0,
            _dataInterfaceNumber,
            lineCoding,
            lineCoding.Length,
            WriteTimeout);
    }

    /// <summary>Sends the CDC SET_CONTROL_LINE_STATE control request (asserts RTS and DTR).</summary>
    private void SendSetControlLineState()
    {
        _connection!.ControlTransfer(
            (UsbAddressing)ClassInterfaceHostToDevice,
            SetControlLineStateRequest,
            0x03, // RTS | DTR
            _controlInterfaceNumber,
            null!,
            0,
            WriteTimeout);
    }

    /// <summary>Starts the background data-received polling task.</summary>
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

    /// <summary>Background loop that fires <see cref="SerialPortBase.DataReceived"/> when bytes are available.</summary>
    private async Task PollLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[_settings.ReadBufferSize];
        while (!ct.IsCancellationRequested && _isOpen)
        {
            try
            {
                int n = _connection?.BulkTransfer(_bulkIn, buffer, buffer.Length, DataReceivedPollIntervalMs) ?? -1;
                if (n > 0)
                {
                    _stream?.EnqueueReceived(buffer, n);
                    OnDataReceived(new Models.SerialDataReceivedEventArgs(Enums.SerialData.Chars));
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.UsbPollError(ex, PortName);
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(DataReceivedPollIntervalMs, ct).ConfigureAwait(false);
        }
    }

    // ── Inner stream class ────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="Stream"/> implementation that wraps USB bulk transfers
    /// for CDC-ACM serial communication.
    /// </summary>
    private sealed class UsbSerialStream : Stream
    {
        private readonly UsbDeviceConnection _connection;
        private readonly UsbEndpoint _bulkIn;
        private readonly UsbEndpoint _bulkOut;
        private readonly AndroidSerialPort _owner;
        private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> _receiveQueue = new();
        private byte[]? _partial;
        private int _partialOffset;

        /// <summary>
        /// Initializes a new instance of <see cref="UsbSerialStream"/>.
        /// </summary>
        /// <param name="connection">The active USB device connection.</param>
        /// <param name="bulkIn">The bulk-in endpoint for reading.</param>
        /// <param name="bulkOut">The bulk-out endpoint for writing.</param>
        /// <param name="owner">The owning <see cref="AndroidSerialPort"/> for timeout access.</param>
        public UsbSerialStream(
            UsbDeviceConnection connection,
            UsbEndpoint bulkIn,
            UsbEndpoint bulkOut,
            AndroidSerialPort owner)
        {
            _connection = connection;
            _bulkIn = bulkIn;
            _bulkOut = bulkOut;
            _owner = owner;
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

        /// <summary>Enqueues data received from the polling task into the internal receive queue.</summary>
        /// <param name="buffer">The buffer containing received bytes.</param>
        /// <param name="count">The number of bytes received.</param>
        public void EnqueueReceived(byte[] buffer, int count)
        {
            var copy = new byte[count];
            Buffer.BlockCopy(buffer, 0, copy, 0, count);
            _receiveQueue.Enqueue(copy);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Drain from receive queue first
            int total = 0;
            while (total < count)
            {
                if (_partial is null || _partialOffset >= _partial.Length)
                {
                    if (!_receiveQueue.TryDequeue(out _partial))
                        break;
                    _partialOffset = 0;
                }
                int toCopy = Math.Min(count - total, _partial.Length - _partialOffset);
                Buffer.BlockCopy(_partial, _partialOffset, buffer, offset + total, toCopy);
                _partialOffset += toCopy;
                total += toCopy;
            }

            if (total > 0) return total;

            // Fall back to direct bulk transfer
            var tmp = new byte[count];
            int n = _connection.BulkTransfer(_bulkIn, tmp, count, _owner.ReadTimeout);
            if (n <= 0) return 0;
            Buffer.BlockCopy(tmp, 0, buffer, offset, n);
            return n;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            int n = _connection.BulkTransfer(_bulkOut, data, count, _owner.WriteTimeout);
            if (n < 0)
                throw new Exceptions.SerialPortException("USB bulk stream write failed.");
        }

        /// <inheritdoc/>
        public override void Flush() { }
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
