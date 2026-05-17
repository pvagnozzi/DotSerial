// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPortLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for DesktopSerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Desktop;

using DotSerial.Config;
using DotSerial.Internal.Windows;
using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="WindowsSerialPort"/>.
/// </summary>
internal static partial class WindowsSerialPortLog
{
    [LoggerMessage(1001, LogLevel.Information, "Opening serial port {PortName} at {BaudRate} bps.")]
    internal static partial void PortOpening(this ILogger logger, string portName, BaudRate baudRate);

    [LoggerMessage(1002, LogLevel.Information, "Serial port {PortName} opened successfully.")]
    internal static partial void PortOpened(this ILogger logger, string portName);

    [LoggerMessage(1003, LogLevel.Information, "Closing serial port {PortName}.")]
    internal static partial void PortClosing(this ILogger logger, string portName);

    [LoggerMessage(1004, LogLevel.Trace, "Writing {Count} bytes to {PortName}.")]
    internal static partial void WritingBytes(this ILogger logger, int count, string portName);

    [LoggerMessage(1005, LogLevel.Trace, "Writing string ({Length} chars) to {PortName}.")]
    internal static partial void WritingString(this ILogger logger, int length, string portName);

    [LoggerMessage(1006, LogLevel.Debug, "Serial port {PortName} disposed.")]
    internal static partial void PortDisposed(this ILogger logger, string portName);
}
