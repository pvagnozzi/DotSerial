// -----------------------------------------------------------------------
// <copyright file="NetworkSerialPortLog.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Source-generated LoggerMessage definitions for NetworkSerialPort.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Internal.Network;

using Microsoft.Extensions.Logging;

#pragma warning disable CS1574
/// <summary>
/// Source-generated high-performance log helpers for <see cref="DotSerial.Internal.Network.NetworkSerialPort"/>.
/// </summary>
#pragma warning restore CS1574
internal static partial class NetworkSerialPortLog
{
    [LoggerMessage(4001, LogLevel.Information, "Connecting to TCP serial bridge {Host}:{Port}.")]
    internal static partial void Connecting(this ILogger logger, string host, int port);

    [LoggerMessage(4002, LogLevel.Information, "Connected to TCP serial bridge {Host}:{Port}.")]
    internal static partial void Connected(this ILogger logger, string host, int port);

    [LoggerMessage(4003, LogLevel.Information, "Disconnected from TCP serial bridge {Host}:{Port}.")]
    internal static partial void Disconnected(this ILogger logger, string host, int port);

    [LoggerMessage(4004, LogLevel.Trace, "Writing {Count} bytes to {Host}:{Port}.")]
    internal static partial void WritingBytes(this ILogger logger, int count, string host, int port);

    [LoggerMessage(4005, LogLevel.Debug, "NetworkSerialPort {Host}:{Port} disposed.")]
    internal static partial void PortDisposed(this ILogger logger, string host, int port);
}
