// -----------------------------------------------------------------------
// <copyright file="SerialPortFactory.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Platform-aware factory for creating ISerialPort instances.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial;

using Microsoft.Extensions.Logging;

/// <summary>
/// Platform-aware factory that creates the correct <see cref="Abstractions.ISerialPort"/>
/// implementation for the current runtime platform.
/// </summary>
public sealed class SerialPortFactory : Abstractions.ISerialPortFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initialises the factory with a <paramref name="loggerFactory"/>.</summary>
    /// <param name="loggerFactory">The logger factory used to create loggers for port instances.</param>
    public SerialPortFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public Abstractions.ISerialPort Create(Models.SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

#if ANDROID
        return new Internal.Android.AndroidSerialPort(
            settings,
            _loggerFactory.CreateLogger<Internal.Android.AndroidSerialPort>());
#elif IOS
        return new Internal.iOS.iOSSerialPort(
            settings,
            _loggerFactory.CreateLogger<Internal.iOS.iOSSerialPort>());
#else
        return new Internal.Desktop.DesktopSerialPort(
            settings,
            _loggerFactory.CreateLogger<Internal.Desktop.DesktopSerialPort>());
#endif
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPortNames()
    {
#if ANDROID || IOS
        throw new PlatformNotSupportedException("GetPortNames is not supported on this platform.");
#else
        return System.IO.Ports.SerialPort.GetPortNames();
#endif
    }
}
