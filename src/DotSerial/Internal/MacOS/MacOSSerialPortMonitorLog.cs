// -----------------------------------------------------------------------
// <copyright file="MacOSSerialPortMonitorLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for MacOSSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.MacOS;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="MacOSSerialPortMonitor"/>.
/// </summary>
internal static partial class MacOSSerialPortMonitorLog
{
    [LoggerMessage(3001, LogLevel.Information, "macOS serial port monitor started (watching /dev/cu.*).")]
    internal static partial void MonitorStarted(this ILogger logger);

    [LoggerMessage(3002, LogLevel.Information, "macOS serial port monitor stopping.")]
    internal static partial void MonitorStopping(this ILogger logger);

    [LoggerMessage(3003, LogLevel.Information, "macOS serial ports changed — added: [{Added}], removed: [{Removed}].")]
    internal static partial void MonitorPortsChanged(this ILogger logger, string added, string removed);

    [LoggerMessage(3004, LogLevel.Error, "Exception in PortsChanged subscriber.")]
    internal static partial void MonitorSubscriberError(this ILogger logger, Exception exception);
}
