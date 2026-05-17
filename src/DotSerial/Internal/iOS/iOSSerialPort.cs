// -----------------------------------------------------------------------
// <copyright file="iOSSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     iOS External Accessory framework implementation of ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

#if IOS
namespace DotSerial.Internal.iOS;

using ExternalAccessory;
using Foundation;
using Microsoft.Extensions.Logging;

/// <summary>
/// iOS implementation of <see cref="Abstractions.ISerialPort"/> via the External Accessory framework.
/// </summary>
/// <remarks>
/// <para>
/// Communicates with MFi-certified serial accessories via the External Accessory framework.
/// <see cref="SerialPortConfig.PortName"/> can be either the accessory serial number or
/// a protocol string supported by the accessory.
/// </para>
/// <para>
/// <b>Prerequisites:</b>
/// <list type="bullet">
///   <item><c>Info.plist</c> must declare <c>UISupportedExternalAccessoryProtocols</c> with the protocol string.</item>
///   <item>The accessory must be physically connected and powered.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class iOSSerialPort : SerialPortBase
{
    private EASession? _session;
    private NSInputStream? _inputStream;
    private NSOutputStream? _outputStream;
    private EASerialStream? _baseStream;
    private bool _isOpen;

    private EAInputDelegate? _inputDelegate;

    /// <summary>
    /// Initializes a new instance of <see cref="iOSSerialPort"/>.
    /// </summary>
    /// <param name="config">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    internal iOSSerialPort(
        SerialPortConfig config,
        ILogger<iOSSerialPort> logger)
        : base(config, logger)
    {
    }

    /// <inheritdoc/>
    public override bool IsOpen => _isOpen;

    /// <inheritdoc/>
    public override int BytesToRead => _baseStream?.Available ?? 0;

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
    public override void Open()
    {
        ThrowIfDisposed();
        if (_isOpen) return;

        _logger.EaOpening(PortName);

        var manager = EAAccessoryManager.SharedAccessoryManager;
        var accessories = manager.ConnectedAccessories;

        if (accessories is null || accessories.Length == 0)
            throw new Exceptions.SerialPortNotFoundException(PortName);

        // Try to find by serial number first, then by protocol string
        EAAccessory? accessory = accessories.FirstOrDefault(a => a.SerialNumber == PortName)
            ?? accessories.FirstOrDefault(a =>
                a.ProtocolStrings?.Contains(PortName) == true);

        if (accessory is null)
            throw new Exceptions.SerialPortNotFoundException(PortName);

        string protocolString = PortName;
        if (accessory.SerialNumber == PortName)
        {
            // Use first available protocol
            protocolString = accessory.ProtocolStrings?.FirstOrDefault()
                ?? throw new Exceptions.SerialPortException(
                    $"No protocol string found for accessory '{PortName}'.");
        }

        try
        {
            _session = new EASession(accessory, protocolString);
        }
        catch (Exception ex)
        {
            throw new Exceptions.SerialPortException(
                $"Failed to create EASession for '{PortName}'.", ex);
        }

        _inputStream = _session.InputStream
            ?? throw new Exceptions.SerialPortException("EASession returned null input stream.");
        _outputStream = _session.OutputStream
            ?? throw new Exceptions.SerialPortException("EASession returned null output stream.");

        _inputDelegate = new EAInputDelegate(this);
        _inputStream.Delegate = _inputDelegate;

        _inputStream.Open();
        _outputStream.Open();

        _baseStream = new EASerialStream(_inputStream, _outputStream);
        _isOpen = true;

        _logger.EaOpened(PortName);
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
        _logger.EaClosing(PortName);

        if (_inputStream is not null)
        {
            _inputStream.Close();
        }
        _outputStream?.Close();
        _session?.Dispose();

        _inputStream = null;
        _outputStream = null;
        _session = null;
        _baseStream = null;
        _isOpen = false;

        _logger.EaClosed(PortName);
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
        _logger.EaWritingBytes(count, PortName);
        nint written = _outputStream!.Write(buffer, offset, (nuint)count);
        if (written < 0)
            throw new Exceptions.SerialPortException(
                $"NSOutputStream write to '{PortName}' failed (result={written})");
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfNotOpen();
        int n = (int)_inputStream!.Read(buffer, offset, (nuint)count);
        _logger.EaReadBytes(n, PortName);
        return Math.Max(0, n);
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        ThrowIfNotOpen();
        var buf = new byte[1];
        int n = Read(buf, 0, 1);
        return n == 1 ? buf[0] : -1;
    }

    /// <inheritdoc/>
    public override void DiscardInBuffer() { /* No direct API */ }

    /// <inheritdoc/>
    public override void DiscardOutBuffer() { /* No direct API */ }

    // ── Dispose ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void DisposeManaged()
    {
        Close();
        _baseStream?.Dispose();
        _baseStream = null;
    }

    // ── NSStream delegate ─────────────────────────────────────────────────

    /// <summary>
    /// NSStream delegate that fires <see cref="SerialPortBase.DataReceived"/> and
    /// <see cref="SerialPortBase.ErrorReceived"/> events for the owning port.
    /// </summary>
    private sealed class EAInputDelegate : NSStreamDelegate
    {
        private readonly iOSSerialPort _owner;

        /// <summary>Initializes a new <see cref="EAInputDelegate"/>.</summary>
        /// <param name="owner">The owning port that receives events.</param>
        public EAInputDelegate(iOSSerialPort owner) => _owner = owner;

        /// <inheritdoc/>
        public override void HandleEvent(NSStream theStream, NSStreamEvent streamEvent)
        {
            switch (streamEvent)
            {
                case NSStreamEvent.HasBytesAvailable:
                    _owner.OnDataReceived(
                        new Models.SerialDataReceivedEventArgs(Enums.SerialData.Chars));
                    break;
                case NSStreamEvent.ErrorOccurred:
                    _owner.OnErrorReceived(
                        new Models.SerialErrorReceivedEventArgs(Enums.SerialError.Frame));
                    break;
            }
        }
    }

    // ── Inner stream class ────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="Stream"/> that wraps <see cref="NSInputStream"/> and <see cref="NSOutputStream"/>
    /// for External Accessory serial I/O.
    /// </summary>
    private sealed class EASerialStream : Stream
    {
        private readonly NSInputStream _input;
        private readonly NSOutputStream _output;

        /// <summary>Gets the number of bytes currently available in the NSInputStream.</summary>
        public int Available => _input.HasBytesAvailable() ? 1 : 0; // NSInputStream doesn't expose exact count

        /// <summary>
        /// Initializes a new instance of <see cref="EASerialStream"/>.
        /// </summary>
        /// <param name="input">The NSInputStream for reading.</param>
        /// <param name="output">The NSOutputStream for writing.</param>
        public EASerialStream(NSInputStream input, NSOutputStream output)
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = (int)_input.Read(buffer, offset, (nuint)count);
            return Math.Max(0, n);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            nint written = _output.Write(buffer, offset, (nuint)count);
            if (written < 0)
                throw new Exceptions.SerialPortException("NSOutputStream write failed.");
        }

        /// <inheritdoc/>
        public override void Flush() { }
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
#endif
