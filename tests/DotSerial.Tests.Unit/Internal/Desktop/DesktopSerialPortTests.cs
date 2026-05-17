// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPortTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for DesktopSerialPort behavior when port is not open or disposed.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Internal.Desktop;

using DotSerial.Config;
using DotSerial.Exceptions;
using DotSerial.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class DesktopSerialPortTests
{
    private NullLoggerFactory _loggerFactory = null!;

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

    private static SerialPortConfig ValidSettings(string portName = "COM1") =>
        new() { PortName = portName, BaudRate = BaudRate.Baud9600 };

    [Test]
    public void Create_ValidSettings_PortIsNotOpen()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings());
        Assert.That(port.IsOpen, Is.False);
    }

    [Test]
    public void Create_ValidSettings_HasCorrectPortName()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings("COM3"));
        Assert.That(port.PortName, Is.EqualTo("COM3"));
    }

    [Test]
    public void Write_WhenPortNotOpen_ThrowsSerialPortException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings());
        Assert.Throws<SerialPortException>(() => port.Write("hello"));
    }

    [Test]
    public void WriteBytesArray_WhenPortNotOpen_ThrowsSerialPortException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings());
        Assert.Throws<SerialPortException>(() => port.Write(new byte[10], 0, 5));
    }

    [Test]
    public void Read_WhenPortNotOpen_ThrowsSerialPortException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings());
        Assert.Throws<SerialPortException>(() => port.Read(new byte[10], 0, 10));
    }

    [Test]
    public void Dispose_DoubleDispose_DoesNotThrow()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var port = factory.Create(ValidSettings());
        port.Dispose();
        Assert.DoesNotThrow(() => port.Dispose());
    }

    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var port = factory.Create(ValidSettings());
        port.Dispose();
        Assert.Throws<ObjectDisposedException>(() => port.Write("hello"));
    }

    [Test]
    public void Read_AfterDispose_ThrowsObjectDisposedException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        var port = factory.Create(ValidSettings());
        port.Dispose();
        Assert.Throws<ObjectDisposedException>(() => port.Read(new byte[10], 0, 10));
    }

    [Test]
    public void Create_NullSettings_ThrowsArgumentNullException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Test]
    public void Create_EmptyPortName_ThrowsArgumentException()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        Assert.Throws<ArgumentException>(() => factory.Create(new SerialPortConfig { PortName = "" }));
    }

    [Test]
    public void IsOpen_BeforeOpen_ReturnsFalse()
    {
        var factory = new SerialPortFactory(_loggerFactory);
        using var port = factory.Create(ValidSettings());
        Assert.That(port.IsOpen, Is.False);
    }
}
