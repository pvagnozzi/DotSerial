// -----------------------------------------------------------------------
// <copyright file="LinuxSerialPortMonitorLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for LinuxSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Linux;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="LinuxSerialPortMonitor"/>.
/// </summary>
internal static partial class LinuxSerialPortMonitorLog
{
    [LoggerMessage(2001, LogLevel.Information, "Linux serial port monitor started (watching /dev/tty*).")]
    internal static partial void MonitorStarted(this ILogger logger);

    [LoggerMessage(2002, LogLevel.Information, "Linux serial port monitor stopping.")]
    internal static partial void MonitorStopping(this ILogger logger);

    [LoggerMessage(2003, LogLevel.Information, "Linux serial ports changed — added: [{Added}], removed: [{Removed}].")]
    internal static partial void MonitorPortsChanged(this ILogger logger, string added, string removed);

    [LoggerMessage(2004, LogLevel.Error, "Exception in PortsChanged subscriber.")]
    internal static partial void MonitorSubscriberError(this ILogger logger, Exception exception);
}
