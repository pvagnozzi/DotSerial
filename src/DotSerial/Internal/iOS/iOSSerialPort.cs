// -----------------------------------------------------------------------
// <copyright file="iOSSerialPort.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     iOS External Accessory stub implementation of ISerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.iOS;

using Microsoft.Extensions.Logging;

/// <summary>
/// iOS implementation of <see cref="Abstractions.ISerialPort"/> via the External Accessory framework.
/// </summary>
/// <remarks>
/// iOS serial port access is restricted to MFi (Made for iPhone/iPad) certified accessories
/// using the External Accessory framework. Direct USB serial access is not available on iOS
/// without a MFi-certified USB-to-serial adapter. Full implementation is planned for a future release.
/// </remarks>
internal sealed class iOSSerialPort : Abstractions.ISerialPort
{
    private const string NotSupportedMessage =
        "iOS serial port access requires a MFi-certified External Accessory. " +
        "Direct USB serial access is not available on iOS without MFi certification. " +
        "Full implementation via the External Accessory framework is planned for a future release.";

    private readonly Models.SerialPortSettings _settings;

    /// <summary>Initializes a new instance of <see cref="iOSSerialPort"/>.</summary>
    /// <param name="settings">The serial port configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    internal iOSSerialPort(Models.SerialPortSettings settings, ILogger<iOSSerialPort> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        settings.Validate();
        _settings = settings;
    }

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
    public int ReadTimeout { get => _settings.ReadTimeout; set => throw new PlatformNotSupportedException(NotSupportedMessage); }
    /// <inheritdoc/>
    public int WriteTimeout { get => _settings.WriteTimeout; set => throw new PlatformNotSupportedException(NotSupportedMessage); }
    /// <inheritdoc/>
    public bool IsOpen => false;
    /// <inheritdoc/>
    public int BytesToRead => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public int BytesToWrite => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Stream BaseStream => throw new PlatformNotSupportedException(NotSupportedMessage);

#pragma warning disable CS0067
    /// <inheritdoc/>
    public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;
    /// <inheritdoc/>
    public event EventHandler<Models.SerialErrorReceivedEventArgs>? ErrorReceived;
    /// <inheritdoc/>
    public event EventHandler<Models.SerialPinChangedEventArgs>? PinChanged;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public void Open() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void Close() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task CloseAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void Write(byte[] buffer, int offset, int count) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void Write(string text) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void WriteLine(string text) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task WriteLineAsync(string text, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public int ReadByte() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public string ReadExisting() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public string ReadLine() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public string ReadTo(string value) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public Task<string> ReadLineAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void DiscardInBuffer() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void DiscardOutBuffer() => throw new PlatformNotSupportedException(NotSupportedMessage);
    /// <inheritdoc/>
    public void Dispose() { }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}


