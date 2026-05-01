// -----------------------------------------------------------------------
// <copyright file="SerialPortSettingsTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for SerialPortSettings validation and default values.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Abstractions;

using DotSerial.Exceptions;
using DotSerial.Models;
using NUnit.Framework;

[TestFixture]
public sealed class SerialPortSettingsTests
{
    [Test]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        var settings = new SerialPortSettings { PortName = "COM1" };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_NullOrEmptyPortName_ThrowsSerialPortException(string? portName)
    {
        var settings = new SerialPortSettings { PortName = portName! };
        Assert.Throws<SerialPortException>(() => settings.Validate());
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_InvalidBaudRate_ThrowsSerialPortException(int baudRate)
    {
        var settings = new SerialPortSettings { PortName = "COM1", BaudRate = baudRate };
        Assert.Throws<SerialPortException>(() => settings.Validate());
    }

    [Test]
    [TestCase(4)]
    [TestCase(9)]
    public void Validate_InvalidDataBits_ThrowsSerialPortException(int dataBits)
    {
        var settings = new SerialPortSettings { PortName = "COM1", DataBits = dataBits };
        Assert.Throws<SerialPortException>(() => settings.Validate());
    }

    [Test]
    [TestCase(-2)]
    [TestCase(-100)]
    public void Validate_InvalidReadTimeout_ThrowsSerialPortException(int timeout)
    {
        var settings = new SerialPortSettings { PortName = "COM1", ReadTimeout = timeout };
        Assert.Throws<SerialPortException>(() => settings.Validate());
    }

    [Test]
    public void Validate_InfiniteReadTimeout_DoesNotThrow()
    {
        var settings = new SerialPortSettings { PortName = "COM1", ReadTimeout = -1 };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    public void WithExpression_CreatesNewInstanceWithOverriddenValue()
    {
        var original = new SerialPortSettings { PortName = "COM1", BaudRate = 9600 };
        var modified = original with { BaudRate = 115200 };
        Assert.That(modified.BaudRate, Is.EqualTo(115200));
        Assert.That(modified.PortName, Is.EqualTo("COM1"));
        Assert.That(original.BaudRate, Is.EqualTo(9600));
    }

    [Test]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void Validate_ValidDataBits_DoesNotThrow(int dataBits)
    {
        var settings = new SerialPortSettings { PortName = "COM1", DataBits = dataBits };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    public void DefaultSettings_HaveExpectedValues()
    {
        var settings = new SerialPortSettings { PortName = "COM1" };
        Assert.That(settings.BaudRate, Is.EqualTo(9600));
        Assert.That(settings.DataBits, Is.EqualTo(8));
        Assert.That(settings.Parity, Is.EqualTo(Enums.Parity.None));
        Assert.That(settings.StopBits, Is.EqualTo(Enums.StopBits.One));
        Assert.That(settings.FlowControl, Is.EqualTo(Enums.FlowControl.None));
        Assert.That(settings.ReadTimeout, Is.EqualTo(500));
        Assert.That(settings.WriteTimeout, Is.EqualTo(500));
        Assert.That(settings.ReadBufferSize, Is.EqualTo(4096));
        Assert.That(settings.WriteBufferSize, Is.EqualTo(2048));
    }
}
