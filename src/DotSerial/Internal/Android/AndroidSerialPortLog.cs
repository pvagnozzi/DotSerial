// -----------------------------------------------------------------------
// <copyright file="AndroidSerialPortLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for Android serial port implementations.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Android;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for Android serial port implementations.
/// </summary>
internal static partial class AndroidSerialPortLog
{
    // AndroidSerialPort (USB)
    [LoggerMessage(5001, LogLevel.Information, "Opening Android USB serial port '{PortName}'.")]
    internal static partial void UsbOpening(this ILogger logger, string portName);

    [LoggerMessage(5002, LogLevel.Information, "USB serial port '{PortName}' opened successfully.")]
    internal static partial void UsbOpened(this ILogger logger, string portName);

    [LoggerMessage(5003, LogLevel.Information, "Closing Android USB serial port '{PortName}'.")]
    internal static partial void UsbClosing(this ILogger logger, string portName);

    [LoggerMessage(5004, LogLevel.Information, "USB serial port '{PortName}' closed.")]
    internal static partial void UsbClosed(this ILogger logger, string portName);

    [LoggerMessage(5005, LogLevel.Trace, "Writing {Count} byte(s) to USB serial port '{PortName}'.")]
    internal static partial void UsbWritingBytes(this ILogger logger, int count, string portName);

    [LoggerMessage(5006, LogLevel.Trace, "Read {Count} byte(s) from USB serial port '{PortName}'.")]
    internal static partial void UsbReadBytes(this ILogger logger, int count, string portName);

    [LoggerMessage(5007, LogLevel.Error, "Error in USB poll loop for '{PortName}'.")]
    internal static partial void UsbPollError(this ILogger logger, Exception exception, string portName);

    // AndroidBluetoothSerialPort
    [LoggerMessage(5008, LogLevel.Information, "Opening Android Bluetooth SPP serial port to '{MacAddress}'.")]
    internal static partial void BtOpening(this ILogger logger, string macAddress);

    [LoggerMessage(5009, LogLevel.Information, "Bluetooth RFCOMM serial port to '{MacAddress}' opened.")]
    internal static partial void BtOpened(this ILogger logger, string macAddress);

    [LoggerMessage(5010, LogLevel.Information, "Closing Bluetooth RFCOMM serial port to '{MacAddress}'.")]
    internal static partial void BtClosing(this ILogger logger, string macAddress);

    [LoggerMessage(5011, LogLevel.Error, "Error closing Bluetooth socket.")]
    internal static partial void BtCloseError(this ILogger logger, Exception exception);

    [LoggerMessage(5012, LogLevel.Trace, "Writing {Count} byte(s) to Bluetooth serial port.")]
    internal static partial void BtWritingBytes(this ILogger logger, int count);

    [LoggerMessage(5013, LogLevel.Trace, "Read {Count} byte(s) from Bluetooth serial port.")]
    internal static partial void BtReadBytes(this ILogger logger, int count);

    [LoggerMessage(5014, LogLevel.Error, "Error in Bluetooth poll loop.")]
    internal static partial void BtPollError(this ILogger logger, Exception exception);
}
