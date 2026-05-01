// -----------------------------------------------------------------------
// <copyright file="iOSBluetoothSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     iOS CoreBluetooth BLE Nordic UART Service (NUS) implementation of ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.iOS;

using System.Threading.Channels;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

/// <summary>
/// iOS implementation of <see cref="Abstractions.ISerialPort"/> using Core Bluetooth
/// for BLE communication via the Nordic UART Service (NUS).
/// </summary>
/// <remarks>
/// <para>
/// NUS UUIDs:
/// <list type="bullet">
///   <item>Service: <c>6E400001-B5A3-F393-E0A9-E50E24DCCA9E</c></item>
///   <item>RX (write to peripheral): <c>6E400002-B5A3-F393-E0A9-E50E24DCCA9E</c></item>
///   <item>TX (notify from peripheral): <c>6E400003-B5A3-F393-E0A9-E50E24DCCA9E</c></item>
/// </list>
/// </para>
/// <para>
/// <see cref="Models.SerialPortSettings.BluetoothAddress"/> (or
/// <see cref="Models.SerialPortSettings.PortName"/>) must be the CBPeripheral UUID string
/// obtained from a prior Bluetooth scan.
/// </para>
/// </remarks>
internal sealed class iOSBluetoothSerialPort : SerialPortBase
{
    /// <summary>Nordic UART Service UUID.</summary>
    private const string NusServiceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";

    /// <summary>NUS RX characteristic UUID (write to peripheral).</summary>
    private const string NusRxUuid = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";

    /// <summary>NUS TX characteristic UUID (notify from peripheral).</summary>
    private const string NusTxUuid = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

    private const int ScanTimeoutMs = 10000;

    private CBCentralManager? _central;
    private CBPeripheral? _peripheral;
    private CBCharacteristic? _rxCharacteristic;
    private CBCharacteristic? _txCharacteristic;

    private NusCentralDelegate? _centralDelegate;
    private NusPeripheralDelegate? _peripheralDelegate;

    private readonly Channel<byte[]> _receiveChannel =
        Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleWriter = true });

    private BleNusStream? _baseStream;
    private bool _isOpen;

    /// <summary>
    /// Initializes a new instance of <see cref="iOSBluetoothSerialPort"/>.
    /// </summary>
    /// <param name="settings">
    /// Serial port settings. <see cref="Models.SerialPortSettings.BluetoothAddress"/> (or
    /// <see cref="Models.SerialPortSettings.PortName"/>) must be the CBPeripheral UUID string.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    internal iOSBluetoothSerialPort(
        Models.SerialPortSettings settings,
        ILogger<iOSBluetoothSerialPort> logger)
        : base(settings, logger)
    {
    }

    /// <inheritdoc/>
    public override bool IsOpen => _isOpen;

    /// <inheritdoc/>
    public override int BytesToRead => 0;

    /// <inheritdoc/>
    public override int BytesToWrite => 0;

    /// <inheritdoc/>
    public override Stream BaseStream
    {
        get
        {
            ThrowIfNotOpen();
            return _baseStream!;
        }
    }

    // ── Open / Close ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Open() => OpenAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public override async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_isOpen) return;

        string peripheralUuid = _settings.BluetoothAddress ?? _settings.PortName;
        _logger.LogInformation(
            "Opening iOS CoreBluetooth NUS serial port to peripheral '{Uuid}'.", peripheralUuid);

        // ── Step 1: Power-on wait ─────────────────────────────────────────
        var powerOnTcs = new TaskCompletionSource<bool>();
        _centralDelegate = new NusCentralDelegate(this, powerOnTcs);
        _central = new CBCentralManager(_centralDelegate, null);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ScanTimeoutMs);

        await powerOnTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);

        // ── Step 2: Scan for the peripheral ──────────────────────────────
        var scanTcs = new TaskCompletionSource<CBPeripheral>();
        _centralDelegate.SetScanTarget(peripheralUuid, scanTcs);
        _central.ScanForPeripherals(new[] { CBUUID.FromString(NusServiceUuid) });

        _peripheral = await scanTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        _central.StopScan();

        // ── Step 3: Connect ───────────────────────────────────────────────
        var connectTcs = new TaskCompletionSource<bool>();
        _centralDelegate.SetConnectTarget(connectTcs);
        _central.ConnectPeripheral(_peripheral);

        await connectTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);

        // ── Step 4: Discover services ─────────────────────────────────────
        var servicesTcs = new TaskCompletionSource<bool>();
        _peripheralDelegate = new NusPeripheralDelegate(this, servicesTcs);
        _peripheral.Delegate = _peripheralDelegate;
        _peripheral.DiscoverServices(new[] { CBUUID.FromString(NusServiceUuid) });

        await servicesTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);

        // ── Step 5: Enable TX notifications ───────────────────────────────
        if (_txCharacteristic is null || _rxCharacteristic is null)
            throw new Exceptions.SerialPortException("NUS characteristics not found after service discovery.");

        _peripheral.SetNotifyValue(true, _txCharacteristic);

        _baseStream = new BleNusStream(_peripheral, _rxCharacteristic, _receiveChannel.Reader, this);
        _isOpen = true;

        _logger.LogInformation(
            "iOS CoreBluetooth NUS serial port to '{Uuid}' opened.", peripheralUuid);
    }

    /// <inheritdoc/>
    public override void Close()
    {
        if (!_isOpen) return;
        _logger.LogInformation("Closing iOS CoreBluetooth NUS serial port.");

        if (_peripheral is not null && _txCharacteristic is not null)
        {
            try { _peripheral.SetNotifyValue(false, _txCharacteristic); }
            catch (Exception ex) { _logger.LogError(ex, "Error disabling TX notification."); }
        }

        if (_peripheral is not null)
        {
            try { _central?.CancelPeripheralConnection(_peripheral); }
            catch (Exception ex) { _logger.LogError(ex, "Error cancelling BLE connection."); }
        }

        _isOpen = false;
        _logger.LogInformation("iOS CoreBluetooth NUS serial port closed.");
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
        _logger.LogTrace("Writing {Count} byte(s) via BLE NUS RX.", count);
        var data = new byte[count];
        Buffer.BlockCopy(buffer, offset, data, 0, count);
        var nsData = NSData.FromArray(data);
        _peripheral!.WriteValue(nsData, _rxCharacteristic!,
            CBCharacteristicWriteType.WithResponse);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfNotOpen();

        if (_receiveChannel.Reader.TryRead(out var chunk))
        {
            int toCopy = Math.Min(count, chunk.Length);
            Buffer.BlockCopy(chunk, 0, buffer, offset, toCopy);
            _logger.LogTrace("Read {Count} byte(s) from BLE NUS channel.", toCopy);
            return toCopy;
        }
        return 0;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        ThrowIfNotOpen();
        if (_receiveChannel.Reader.TryRead(out var chunk) && chunk.Length > 0)
            return chunk[0];
        return -1;
    }

    /// <inheritdoc/>
    public override void DiscardInBuffer()
    {
        while (_receiveChannel.Reader.TryRead(out _)) { }
    }

    /// <inheritdoc/>
    public override void DiscardOutBuffer() { /* Not supported for BLE */ }

    // ── Dispose ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void DisposeManaged()
    {
        Close();
        _receiveChannel.Writer.TryComplete();
        _baseStream?.Dispose();
        _baseStream = null;
    }

    // ── Internal: called from delegates ──────────────────────────────────

    /// <summary>Sets the discovered NUS RX and TX characteristics.</summary>
    /// <param name="rx">The RX characteristic (write to peripheral).</param>
    /// <param name="tx">The TX characteristic (notify from peripheral).</param>
    internal void SetCharacteristics(CBCharacteristic rx, CBCharacteristic tx)
    {
        _rxCharacteristic = rx;
        _txCharacteristic = tx;
    }

    /// <summary>Called by the peripheral delegate when new NUS TX data arrives.</summary>
    /// <param name="data">The received data bytes.</param>
    internal void OnNusDataReceived(byte[] data)
    {
        _receiveChannel.Writer.TryWrite(data);
        OnDataReceived(new Models.SerialDataReceivedEventArgs(Enums.SerialData.Chars));
    }

    /// <summary>Called by the central delegate on BLE errors.</summary>
    /// <param name="message">The error message.</param>
    internal void OnBleError(string message)
    {
        _logger.LogError("BLE error: {Message}", message);
        OnErrorReceived(new Models.SerialErrorReceivedEventArgs(Enums.SerialError.Frame));
    }

    // ── CBCentralManager delegate ─────────────────────────────────────────

    /// <summary>
    /// CBCentralManager delegate that handles power-on, scan results, and connection events.
    /// </summary>
    private sealed class NusCentralDelegate : CBCentralManagerDelegate
    {
        private readonly iOSBluetoothSerialPort _owner;
        private TaskCompletionSource<bool>? _powerOnTcs;
        private TaskCompletionSource<CBPeripheral>? _scanTcs;
        private TaskCompletionSource<bool>? _connectTcs;
        private string? _targetUuid;

        /// <summary>Initializes a new <see cref="NusCentralDelegate"/>.</summary>
        /// <param name="owner">The owning port.</param>
        /// <param name="powerOnTcs">TCS completed when Bluetooth powers on.</param>
        public NusCentralDelegate(iOSBluetoothSerialPort owner, TaskCompletionSource<bool> powerOnTcs)
        {
            _owner = owner;
            _powerOnTcs = powerOnTcs;
        }

        /// <summary>Sets the scan target peripheral UUID and completion source.</summary>
        /// <param name="uuid">The UUID to search for.</param>
        /// <param name="tcs">TCS completed when the peripheral is found.</param>
        public void SetScanTarget(string uuid, TaskCompletionSource<CBPeripheral> tcs)
        {
            _targetUuid = uuid;
            _scanTcs = tcs;
        }

        /// <summary>Sets the connection completion source.</summary>
        /// <param name="tcs">TCS completed on successful connection.</param>
        public void SetConnectTarget(TaskCompletionSource<bool> tcs)
            => _connectTcs = tcs;

        /// <inheritdoc/>
        public override void UpdatedState(CBCentralManager central)
        {
            if (central.State == CBManagerState.PoweredOn)
                _powerOnTcs?.TrySetResult(true);
            else if (central.State != CBManagerState.Unknown &&
                     central.State != CBManagerState.Resetting)
                _powerOnTcs?.TrySetException(
                    new Exceptions.SerialPortException(
                        $"Bluetooth is not available (state: {central.State})."));
        }

        /// <inheritdoc/>
        public override void DiscoveredPeripheral(
            CBCentralManager central,
            CBPeripheral peripheral,
            NSDictionary advertisementData,
            NSNumber rssi)
        {
            if (_targetUuid is not null &&
                peripheral.Identifier.AsString() == _targetUuid)
            {
                _scanTcs?.TrySetResult(peripheral);
            }
        }

        /// <inheritdoc/>
        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
            => _connectTcs?.TrySetResult(true);

        /// <inheritdoc/>
        public override void FailedToConnectPeripheral(
            CBCentralManager central,
            CBPeripheral peripheral,
            NSError? error)
        {
            _connectTcs?.TrySetException(
                new Exceptions.SerialPortException(
                    $"Failed to connect to BLE peripheral: {error?.LocalizedDescription}"));
        }

        /// <inheritdoc/>
        public override void DisconnectedPeripheral(
            CBCentralManager central,
            CBPeripheral peripheral,
            NSError? error)
        {
            if (error is not null)
                _owner.OnBleError($"BLE peripheral disconnected: {error.LocalizedDescription}");
        }
    }

    // ── CBPeripheral delegate ─────────────────────────────────────────────

    /// <summary>
    /// CBPeripheral delegate that handles service/characteristic discovery
    /// and incoming NUS TX notifications.
    /// </summary>
    private sealed class NusPeripheralDelegate : CBPeripheralDelegate
    {
        private readonly iOSBluetoothSerialPort _owner;
        private readonly TaskCompletionSource<bool> _servicesTcs;
        private int _discoveredCount;

        /// <summary>Initializes a new <see cref="NusPeripheralDelegate"/>.</summary>
        /// <param name="owner">The owning port.</param>
        /// <param name="servicesTcs">TCS completed once NUS characteristics are ready.</param>
        public NusPeripheralDelegate(
            iOSBluetoothSerialPort owner,
            TaskCompletionSource<bool> servicesTcs)
        {
            _owner = owner;
            _servicesTcs = servicesTcs;
        }

        /// <inheritdoc/>
        public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
        {
            if (error is not null)
            {
                _servicesTcs.TrySetException(
                    new Exceptions.SerialPortException($"Service discovery failed: {error.LocalizedDescription}"));
                return;
            }

            var service = peripheral.Services?
                .FirstOrDefault(s => s.UUID.Uuid == NusServiceUuid.ToUpperInvariant());

            if (service is null)
            {
                _servicesTcs.TrySetException(
                    new Exceptions.SerialPortException("NUS service not found on peripheral."));
                return;
            }

            peripheral.DiscoverCharacteristics(
                new[]
                {
                    CBUUID.FromString(NusRxUuid),
                    CBUUID.FromString(NusTxUuid)
                },
                service);
        }

        /// <inheritdoc/>
        public override void DiscoveredCharacteristics(
            CBPeripheral peripheral,
            CBService service,
            NSError? error)
        {
            if (error is not null)
            {
                _servicesTcs.TrySetException(
                    new Exceptions.SerialPortException(
                        $"Characteristic discovery failed: {error.LocalizedDescription}"));
                return;
            }

            CBCharacteristic? rx = null;
            CBCharacteristic? tx = null;

            foreach (var ch in service.Characteristics ?? Array.Empty<CBCharacteristic>())
            {
                if (ch.UUID.Uuid.Equals(NusRxUuid, StringComparison.OrdinalIgnoreCase)) rx = ch;
                else if (ch.UUID.Uuid.Equals(NusTxUuid, StringComparison.OrdinalIgnoreCase)) tx = ch;
            }

            if (rx is not null && tx is not null)
            {
                _owner.SetCharacteristics(rx, tx);
                _servicesTcs.TrySetResult(true);
            }
            else if (++_discoveredCount > 5)
            {
                _servicesTcs.TrySetException(
                    new Exceptions.SerialPortException("NUS RX or TX characteristic not found."));
            }
        }

        /// <inheritdoc/>
        public override void UpdatedCharacterteristicValue(
            CBPeripheral peripheral,
            CBCharacteristic characteristic,
            NSError? error)
        {
            if (error is not null || characteristic.Value is null) return;
            if (!characteristic.UUID.Uuid.Equals(NusTxUuid, StringComparison.OrdinalIgnoreCase)) return;

            var data = characteristic.Value.ToArray();
            if (data.Length > 0)
                _owner.OnNusDataReceived(data);
        }
    }

    // ── BLE NUS stream ────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="Stream"/> that bridges BLE NUS communication:
    /// writes go to the RX characteristic; reads drain from the notification channel.
    /// </summary>
    private sealed class BleNusStream : Stream
    {
        private readonly CBPeripheral _peripheral;
        private readonly CBCharacteristic _rxCharacteristic;
        private readonly ChannelReader<byte[]> _reader;
        private readonly iOSBluetoothSerialPort _owner;

        /// <summary>
        /// Initializes a new instance of <see cref="BleNusStream"/>.
        /// </summary>
        /// <param name="peripheral">The connected CBPeripheral.</param>
        /// <param name="rxCharacteristic">The NUS RX characteristic for writing.</param>
        /// <param name="reader">The channel reader for incoming data.</param>
        /// <param name="owner">The owning port for timeout access.</param>
        public BleNusStream(
            CBPeripheral peripheral,
            CBCharacteristic rxCharacteristic,
            ChannelReader<byte[]> reader,
            iOSBluetoothSerialPort owner)
        {
            _peripheral = peripheral;
            _rxCharacteristic = rxCharacteristic;
            _reader = reader;
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

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_reader.TryRead(out var chunk))
            {
                int toCopy = Math.Min(count, chunk.Length);
                Buffer.BlockCopy(chunk, 0, buffer, offset, toCopy);
                return toCopy;
            }
            return 0;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            var nsData = NSData.FromArray(data);
            _peripheral.WriteValue(nsData, _rxCharacteristic,
                CBCharacteristicWriteType.WithResponse);
        }

        /// <inheritdoc/>
        public override void Flush() { }
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
