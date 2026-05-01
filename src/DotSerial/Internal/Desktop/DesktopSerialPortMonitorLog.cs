// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPortMonitorLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for DesktopSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Desktop;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="DesktopSerialPortMonitor"/>.
/// </summary>
internal static partial class DesktopSerialPortMonitorLog
{
    [LoggerMessage(1007, LogLevel.Information, "Serial port monitor started (polling every {IntervalMs} ms).")]
    internal static partial void MonitorStarted(this ILogger logger, double intervalMs);

    [LoggerMessage(1008, LogLevel.Information, "Serial port monitor stopping.")]
    internal static partial void MonitorStopping(this ILogger logger);

    [LoggerMessage(1009, LogLevel.Information, "Serial ports changed — added: [{Added}], removed: [{Removed}].")]
    internal static partial void MonitorPortsChanged(this ILogger logger, string added, string removed);

    [LoggerMessage(1010, LogLevel.Error, "Unhandled error during serial port monitor poll.")]
    internal static partial void MonitorPollError(this ILogger logger, Exception exception);

    [LoggerMessage(1011, LogLevel.Information, "Serial port monitor stopped.")]
    internal static partial void MonitorStopped(this ILogger logger);

    [LoggerMessage(1012, LogLevel.Error, "Exception in PortsChanged subscriber.")]
    internal static partial void MonitorSubscriberError(this ILogger logger, Exception exception);
}
