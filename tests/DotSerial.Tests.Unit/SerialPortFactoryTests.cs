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

using DotSerial.Exceptions;
using DotSerial.Models;
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
    public void Create_InvalidSettings_EmptyPortName_ThrowsSerialPortException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings = new SerialPortSettings { PortName = "" };
        Assert.Throws<SerialPortException>(() => factory.Create(settings));
    }

    [Test]
    public void Create_ValidSettings_ReturnsNonNullISerialPort()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings = new SerialPortSettings { PortName = "COM1" };
        var port = factory.Create(settings);
        Assert.That(port, Is.Not.Null);
        port.Dispose();
    }

    [Test]
    public void Create_MultipleValidSettings_ReturnsIndependentPorts()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var settings1 = new SerialPortSettings { PortName = "COM1" };
        var settings2 = new SerialPortSettings { PortName = "COM2" };
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
}
