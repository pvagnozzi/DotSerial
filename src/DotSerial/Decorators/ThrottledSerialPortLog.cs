// -----------------------------------------------------------------------
// <copyright file="ThrottledSerialPortLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for ThrottledSerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Decorators;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated high-performance log helpers for <see cref="ThrottledSerialPort"/>.
/// </summary>
internal static partial class ThrottledSerialPortLog
{
    [LoggerMessage(7001, LogLevel.Trace, "Throttling write of {Bytes} B on {Port} — sleeping {Ms} ms.")]
    internal static partial void ThrottlingSync(this ILogger logger, int bytes, string port, int ms);

    [LoggerMessage(7002, LogLevel.Trace, "Throttling async write of {Bytes} B on {Port} — delaying {Ms} ms.")]
    internal static partial void ThrottlingAsync(this ILogger logger, int bytes, string port, int ms);
}
