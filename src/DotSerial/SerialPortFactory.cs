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

using System.Runtime.InteropServices;
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

        // Network is cross-platform — handle first before any platform fork.
        if (settings.ConnectionType == Enums.ConnectionType.Network)
        {
            return new Internal.Network.NetworkSerialPort(
                settings,
                _loggerFactory.CreateLogger<Internal.Network.NetworkSerialPort>());
        }

#if ANDROID
        return settings.ConnectionType == Enums.ConnectionType.Bluetooth
            ? new Internal.Android.AndroidBluetoothSerialPort(
                settings,
                _loggerFactory.CreateLogger<Internal.Android.AndroidBluetoothSerialPort>())
            : new Internal.Android.AndroidSerialPort(
                settings,
                _loggerFactory.CreateLogger<Internal.Android.AndroidSerialPort>());
#elif IOS
        return settings.ConnectionType == Enums.ConnectionType.Bluetooth
            ? new Internal.iOS.iOSBluetoothSerialPort(
                settings,
                _loggerFactory.CreateLogger<Internal.iOS.iOSBluetoothSerialPort>())
            : new Internal.iOS.iOSSerialPort(
                settings,
                _loggerFactory.CreateLogger<Internal.iOS.iOSSerialPort>());
#else
        if (settings.ConnectionType != Enums.ConnectionType.Serial)
        {
            throw new PlatformNotSupportedException(
                $"ConnectionType '{settings.ConnectionType}' is not supported on desktop platforms. " +
                "Use ConnectionType.Serial or ConnectionType.Network.");
        }

        return new Internal.Desktop.DesktopSerialPort(
            settings,
            _loggerFactory.CreateLogger<Internal.Desktop.DesktopSerialPort>());
#endif
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPortNames()
    {
#if ANDROID
        var usbManager = global::Android.App.Application.Context
            .GetSystemService(global::Android.Content.Context.UsbService)
            as global::Android.Hardware.Usb.UsbManager;

        var devices = usbManager?.DeviceList;
        if (devices is null || devices.Count == 0)
        {
            return Array.Empty<string>();
        }

        return devices.Values
            .OrderBy(d => d.DeviceName, StringComparer.Ordinal)
            .Select((_, i) => $"USB{i}")
            .ToList()
            .AsReadOnly();
#elif IOS
        throw new PlatformNotSupportedException("GetPortNames is not supported on iOS.");
#else
        return System.IO.Ports.SerialPort.GetPortNames();
#endif
    }

    /// <inheritdoc/>
    public Abstractions.ISerialPortMonitor CreateMonitor(TimeSpan? pollingInterval = null)
    {
#if ANDROID
        return new Internal.Android.AndroidSerialPortMonitor(
            _loggerFactory.CreateLogger<Internal.Android.AndroidSerialPortMonitor>());
#elif IOS
        return new Internal.iOS.iOSSerialPortMonitor(
            _loggerFactory.CreateLogger<Internal.iOS.iOSSerialPortMonitor>());
#else
        var interval = pollingInterval ?? TimeSpan.FromSeconds(1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), pollingInterval,
                "Polling interval must be a positive TimeSpan.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Internal.Linux.LinuxSerialPortMonitor(
                _loggerFactory.CreateLogger<Internal.Linux.LinuxSerialPortMonitor>());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Internal.MacOS.MacOSSerialPortMonitor(
                _loggerFactory.CreateLogger<Internal.MacOS.MacOSSerialPortMonitor>());
        return new Internal.Desktop.DesktopSerialPortMonitor(
            interval,
            _loggerFactory.CreateLogger<Internal.Desktop.DesktopSerialPortMonitor>());
#endif
    }

    /// <inheritdoc/>
    public Abstractions.ISerialPortStream CreateStream(Models.SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var port = Create(settings);
        return new Streams.SerialPortStreamWrapper(port);
    }
}
