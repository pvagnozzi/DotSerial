// -----------------------------------------------------------------------
// <copyright file="iOSSerialPortMonitorLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for iOSSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.iOS;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="iOSSerialPortMonitor"/>.
/// </summary>
internal static partial class iOSSerialPortMonitorLog
{
    [LoggerMessage(6020, LogLevel.Information, "iOS External Accessory serial port monitor started.")]
    internal static partial void MonitorStarted(this ILogger logger);

    [LoggerMessage(6021, LogLevel.Information, "iOS External Accessory serial port monitor stopping.")]
    internal static partial void MonitorStopping(this ILogger logger);

    [LoggerMessage(6022, LogLevel.Information, "iOS serial ports changed — added: [{Added}], removed: [{Removed}].")]
    internal static partial void MonitorPortsChanged(this ILogger logger, string added, string removed);

    [LoggerMessage(6023, LogLevel.Error, "Exception in PortsChanged subscriber.")]
    internal static partial void MonitorSubscriberError(this ILogger logger, Exception exception);
}
