// -----------------------------------------------------------------------
// <copyright file="SerialPortConfig.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Configuration settings for a serial port connection.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>
/// Configuration settings for establishing and managing a serial port connection.
/// </summary>
public record SerialPortConfig
{
    /// <summary>Gets or sets the port name (e.g., "COM1", "/dev/ttyUSB0", or MAC address for Bluetooth).</summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>Gets or sets the baud rate in bits per second.</summary>
    public BaudRate BaudRate { get; set; } = BaudRate.Baud115200;

    /// <summary>Gets or sets the parity bit setting.</summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>Gets or sets the number of data bits per byte (5-8).</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>Gets or sets the number of stop bits.</summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>Gets or sets the flow-control (handshake) protocol.</summary>
    public FlowControl FlowControl { get; set; } = FlowControl.None;

    /// <summary>Gets or sets the connection type (Serial, Bluetooth, or Network).</summary>
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Serial;

    /// <summary>Gets the read timeout in milliseconds. Use -1 for infinite.</summary>
    public int ReadTimeout { get; set; } = -1;

    /// <summary>Gets the write timeout in milliseconds. Use -1 for infinite.</summary>
    public int WriteTimeout { get; set; } = -1;

    /// <summary>Gets or sets the read buffer size in bytes. Defaults to 4096.</summary>
    public int ReadBufferSize { get; set; } = 4096;

    /// <summary>Gets or sets the write buffer size in bytes. Defaults to 4096.</summary>
    public int WriteBufferSize { get; set; } = 4096;

    /// <summary>Gets or sets the Bluetooth device MAC address or UUID (for Bluetooth connections).</summary>
    public string? BluetoothAddress { get; set; }

    /// <summary>Validates this configuration.</summary>
    /// <exception cref="ArgumentException">Thrown when the configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PortName))
            throw new ArgumentException("PortName cannot be null or whitespace.", nameof(PortName));

        if (DataBits < 5 || DataBits > 8)
            throw new ArgumentOutOfRangeException(nameof(DataBits), DataBits, "DataBits must be between 5 and 8.");

        if (ReadTimeout < -1)
            throw new ArgumentOutOfRangeException(nameof(ReadTimeout), ReadTimeout, "ReadTimeout must be -1 or non-negative.");

        if (WriteTimeout < -1)
            throw new ArgumentOutOfRangeException(nameof(WriteTimeout), WriteTimeout, "WriteTimeout must be -1 or non-negative.");

        if (ReadBufferSize < 256)
            throw new ArgumentOutOfRangeException(nameof(ReadBufferSize), ReadBufferSize, "ReadBufferSize must be at least 256.");

        if (WriteBufferSize < 256)
            throw new ArgumentOutOfRangeException(nameof(WriteBufferSize), WriteBufferSize, "WriteBufferSize must be at least 256.");

        // Validate Bluetooth-specific requirements
        if (ConnectionType == ConnectionType.Bluetooth)
        {
            if (string.IsNullOrWhiteSpace(BluetoothAddress))
                throw new ArgumentException("BluetoothAddress must be provided for Bluetooth connections.", nameof(BluetoothAddress));
        }
    }
}
