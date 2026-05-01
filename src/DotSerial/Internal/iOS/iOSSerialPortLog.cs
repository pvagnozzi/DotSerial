// -----------------------------------------------------------------------
// <copyright file="iOSSerialPortLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for iOS serial port implementations.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.iOS;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for iOS serial port implementations.
/// </summary>
internal static partial class iOSSerialPortLog
{
    // iOSSerialPort (ExternalAccessory)
    [LoggerMessage(6001, LogLevel.Information, "Opening iOS External Accessory serial port '{PortName}'.")]
    internal static partial void EaOpening(this ILogger logger, string portName);

    [LoggerMessage(6002, LogLevel.Information, "iOS External Accessory serial port '{PortName}' opened.")]
    internal static partial void EaOpened(this ILogger logger, string portName);

    [LoggerMessage(6003, LogLevel.Information, "Closing iOS External Accessory serial port '{PortName}'.")]
    internal static partial void EaClosing(this ILogger logger, string portName);

    [LoggerMessage(6004, LogLevel.Information, "iOS External Accessory serial port '{PortName}' closed.")]
    internal static partial void EaClosed(this ILogger logger, string portName);

    [LoggerMessage(6005, LogLevel.Trace, "Writing {Count} byte(s) to EA serial port '{PortName}'.")]
    internal static partial void EaWritingBytes(this ILogger logger, int count, string portName);

    [LoggerMessage(6006, LogLevel.Trace, "Read {Count} byte(s) from EA serial port '{PortName}'.")]
    internal static partial void EaReadBytes(this ILogger logger, int count, string portName);

    // iOSBluetoothSerialPort (CoreBluetooth NUS)
    [LoggerMessage(6007, LogLevel.Information, "Opening iOS CoreBluetooth NUS serial port to peripheral '{Uuid}'.")]
    internal static partial void BleOpening(this ILogger logger, string uuid);

    [LoggerMessage(6008, LogLevel.Information, "iOS CoreBluetooth NUS serial port to '{Uuid}' opened.")]
    internal static partial void BleOpened(this ILogger logger, string uuid);

    [LoggerMessage(6009, LogLevel.Information, "Closing iOS CoreBluetooth NUS serial port.")]
    internal static partial void BleClosing(this ILogger logger);

    [LoggerMessage(6010, LogLevel.Error, "Error disabling TX notification.")]
    internal static partial void BleTxNotifyError(this ILogger logger, Exception exception);

    [LoggerMessage(6011, LogLevel.Error, "Error cancelling BLE connection.")]
    internal static partial void BleCancelError(this ILogger logger, Exception exception);

    [LoggerMessage(6012, LogLevel.Information, "iOS CoreBluetooth NUS serial port closed.")]
    internal static partial void BleClosed(this ILogger logger);

    [LoggerMessage(6013, LogLevel.Trace, "Writing {Count} byte(s) via BLE NUS RX.")]
    internal static partial void BleWritingBytes(this ILogger logger, int count);

    [LoggerMessage(6014, LogLevel.Trace, "Read {Count} byte(s) from BLE NUS channel.")]
    internal static partial void BleReadBytes(this ILogger logger, int count);

    [LoggerMessage(6015, LogLevel.Error, "BLE error: {Message}")]
    internal static partial void BleError(this ILogger logger, string message);
}
