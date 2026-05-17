// -----------------------------------------------------------------------
// <copyright file="SerialPortFactoryTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for SerialPortFactory creation and configuration.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit;

using DotSerial.Config;
using DotSerial.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class SerialPortFactoryTests
{
    private ILoggerFactory _loggerFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerFactory = new NullLoggerFactory();
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory.Dispose();
    }

    [Test]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SerialPortFactory(null!));
    }

    [Test]
    public void Create_NullSettings_ThrowsArgumentNullException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Test]
    public void Create_InvalidSettings_EmptyPortName_ThrowsArgumentException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings = new SerialPortConfig { PortName = "" };
        Assert.Throws<ArgumentException>(() => factory.Create(settings));
    }

    [Test]
    public void Create_ValidSettings_ReturnsNonNullISerialPort()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings = new SerialPortConfig { PortName = "COM1" };
        var port = factory.Create(settings);
        Assert.That(port, Is.Not.Null);
        port.Dispose();
    }

    [Test]
    public void Create_MultipleValidSettings_ReturnsIndependentPorts()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings1 = new SerialPortConfig { PortName = "COM1" };
        var settings2 = new SerialPortConfig { PortName = "COM2" };
        var port1 = factory.Create(settings1);
        var port2 = factory.Create(settings2);
        Assert.That(port1, Is.Not.SameAs(port2));
        port1.Dispose();
        port2.Dispose();
    }

    [Test]
    public void GetPortNames_ReturnsReadOnlyList()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var ports = factory.GetPortNames();
        Assert.That(ports, Is.Not.Null);
        Assert.That(ports, Is.InstanceOf<IReadOnlyList<string>>());
    }

    [Test]
    public void CreateMonitor_DefaultInterval_ReturnsNonNull()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var monitor = factory.CreateMonitor();
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void CreateMonitor_ExplicitInterval_ReturnsNonNull()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var monitor = factory.CreateMonitor(TimeSpan.FromMilliseconds(250));
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void Create_BluetoothConnectionType_OnDesktop_ThrowsPlatformNotSupportedException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings = new SerialPortConfig
        {
            PortName = "BT-Device",
            ConnectionType = ConnectionType.Bluetooth,
            BluetoothAddress = "00:11:22:33:44:55",
        };
        Assert.Throws<PlatformNotSupportedException>(() => factory.Create(settings));
    }

    // ── CreateStream ──────────────────────────────────────────────────────

    [Test]
    public void CreateStream_NullSettings_ThrowsArgumentNullException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        Assert.Throws<ArgumentNullException>(() => factory.CreateStream(null!));
    }

    [Test]
    public void CreateStream_ValidSettings_ReturnsNonNull()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var stream = factory.CreateStream(new SerialPortConfig { PortName = "COM1" });
        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void CreateStream_ValidSettings_PortNameMatchesSettings()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var stream = factory.CreateStream(new SerialPortConfig { PortName = "COM5" });
        Assert.That(stream.SerialPort.PortName, Is.EqualTo("COM5"));
    }

    [Test]
    public void CreateStream_InvalidSettings_ThrowsArgumentException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        Assert.Throws<ArgumentException>(() =>
            factory.CreateStream(new SerialPortConfig { PortName = "" }));
    }

    // ── OpenAsync / CloseAsync (not connected) ───────────────────────────

    [Test]
    public async Task OpenAsync_WhenPortNotAvailable_ThrowsSerialPortNotFoundException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        // Use a port name that is very unlikely to exist on any machine.
        using var port = factory.Create(new SerialPortConfig { PortName = "COM249" });
        Assert.ThrowsAsync<SerialPortNotFoundException>(async () =>
            await port.OpenAsync(CancellationToken.None));
    }

    [Test]
    public void CloseAsync_WhenNotOpen_DoesNotThrow()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(new SerialPortConfig { PortName = "COM1" });
        Assert.DoesNotThrowAsync(async () =>
            await port.CloseAsync(CancellationToken.None));
    }
}
