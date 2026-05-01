// -----------------------------------------------------------------------
// <copyright file="AndroidSerialPortMonitorLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for AndroidSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Android;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="AndroidSerialPortMonitor"/>.
/// </summary>
internal static partial class AndroidSerialPortMonitorLog
{
    [LoggerMessage(5020, LogLevel.Information, "Android USB serial port monitor started.")]
    internal static partial void MonitorStarted(this ILogger logger);

    [LoggerMessage(5021, LogLevel.Information, "Android USB serial port monitor stopping.")]
    internal static partial void MonitorStopping(this ILogger logger);

    [LoggerMessage(5022, LogLevel.Information, "Android USB serial ports changed — added: [{Added}], removed: [{Removed}].")]
    internal static partial void MonitorPortsChanged(this ILogger logger, string added, string removed);

    [LoggerMessage(5023, LogLevel.Error, "Exception in PortsChanged subscriber.")]
    internal static partial void MonitorSubscriberError(this ILogger logger, Exception exception);
}
