// -----------------------------------------------------------------------
// <copyright file="ConnectionType.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Defines the transport layer used to establish a serial communication link.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>
/// Specifies the underlying transport layer used for a serial communication link.
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// A classic wired serial connection via a physical COM port or USB-to-serial adapter
    /// (e.g., FTDI, CP210x, CH340). This is the default connection type.
    /// </summary>
    Serial = 0,

    /// <summary>
    /// A Bluetooth RFCOMM (Serial Port Profile) connection.
    /// On Android, <see cref="SerialPortConfig.PortName"/> must be
    /// the remote device MAC address (e.g., <c>"00:11:22:33:44:55"</c>).
    /// On iOS, it must be the Core Bluetooth peripheral UUID or device name.
    /// </summary>
    Bluetooth = 1,

    /// <summary>
    /// A TCP/IP-to-serial bridge connection.
    /// <see cref="SerialPortConfig.PortName"/> must be in
    /// <c>"host:port"</c> format (e.g., <c>"192.168.1.100:23"</c>).
    /// </summary>
    Network = 2,
}
