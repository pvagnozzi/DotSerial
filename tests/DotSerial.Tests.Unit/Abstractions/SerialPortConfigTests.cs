// -----------------------------------------------------------------------
// <copyright file="SerialPortConfigTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for SerialPortConfig validation and default values.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Abstractions;

using DotSerial.Config;
using DotSerial.Exceptions;
using NUnit.Framework;

[TestFixture]
public sealed class SerialPortConfigTests
{
    [Test]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        var settings = new SerialPortConfig { PortName = "COM1" };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_NullOrEmptyPortName_ThrowsArgumentException(string? portName)
    {
        var settings = new SerialPortConfig { PortName = portName! };
        Assert.Throws<ArgumentException>(() => settings.Validate());
    }

    [Test]
    [TestCase(4)]
    [TestCase(9)]
    public void Validate_InvalidDataBits_ThrowsArgumentOutOfRangeException(int dataBits)
    {
        var settings = new SerialPortConfig { PortName = "COM1", DataBits = dataBits };
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }

    [Test]
    [TestCase(-2)]
    [TestCase(-100)]
    public void Validate_InvalidReadTimeout_ThrowsArgumentOutOfRangeException(int timeout)
    {
        var settings = new SerialPortConfig { PortName = "COM1", ReadTimeout = timeout };
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }

    [Test]
    public void Validate_InfiniteReadTimeout_DoesNotThrow()
    {
        var settings = new SerialPortConfig { PortName = "COM1", ReadTimeout = -1 };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void Validate_ValidDataBits_DoesNotThrow(int dataBits)
    {
        var settings = new SerialPortConfig { PortName = "COM1", DataBits = dataBits };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    public void DefaultSettings_HaveExpectedValues()
    {
        var settings = new SerialPortConfig { PortName = "COM1" };
        Assert.That(settings.BaudRate, Is.EqualTo(BaudRate.Baud115200));
        Assert.That(settings.DataBits, Is.EqualTo(8));
        Assert.That(settings.Parity, Is.EqualTo(Parity.None));
        Assert.That(settings.StopBits, Is.EqualTo(StopBits.One));
        Assert.That(settings.FlowControl, Is.EqualTo(FlowControl.None));
        Assert.That(settings.ReadTimeout, Is.EqualTo(-1));
        Assert.That(settings.WriteTimeout, Is.EqualTo(-1));
        Assert.That(settings.ReadBufferSize, Is.EqualTo(4096));
        Assert.That(settings.WriteBufferSize, Is.EqualTo(4096));
        Assert.That(settings.ConnectionType, Is.EqualTo(ConnectionType.Serial));
        Assert.That(settings.BluetoothAddress, Is.Null);
    }

    // ── Bluetooth / ConnectionType tests ──────────────────────────────────

    [Test]
    public void Validate_BluetoothWithValidAddress_DoesNotThrow()
    {
        var settings = new SerialPortConfig
        {
            PortName = "BT-Device",
            ConnectionType = ConnectionType.Bluetooth,
            BluetoothAddress = "00:11:22:33:44:55",
        };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_BluetoothWithMissingAddress_ThrowsArgumentException(string? address)
    {
        var settings = new SerialPortConfig
        {
            PortName = "BT-Device",
            ConnectionType = ConnectionType.Bluetooth,
            BluetoothAddress = address,
        };
        Assert.Throws<ArgumentException>(() => settings.Validate());
    }

    [Test]
    public void Validate_SerialConnectionType_DoesNotRequireBluetoothAddress()
    {
        var settings = new SerialPortConfig
        {
            PortName = "COM3",
            ConnectionType = ConnectionType.Serial,
        };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    public void Validate_NetworkConnectionType_DoesNotRequireBluetoothAddress()
    {
        var settings = new SerialPortConfig
        {
            PortName = "192.168.1.100:23",
            ConnectionType = ConnectionType.Network,
        };
        Assert.DoesNotThrow(() => settings.Validate());
    }

    [Test]
    public void WithExpression_PreservesConnectionType()
    {
        var original = new SerialPortConfig
        {
            PortName = "BT-Device",
            ConnectionType = ConnectionType.Bluetooth,
            BluetoothAddress = "AA:BB:CC:DD:EE:FF",
        };
        var modified = original with { BaudRate = BaudRate.Baud115200 };
        Assert.That(modified.ConnectionType, Is.EqualTo(ConnectionType.Bluetooth));
        Assert.That(modified.BluetoothAddress, Is.EqualTo("AA:BB:CC:DD:EE:FF"));
    }

    [Test]
    public void WithExpression_CreatesNewInstanceWithOverriddenValue()
    {
        var original = new SerialPortConfig { PortName = "COM1", BaudRate = BaudRate.Baud9600 };
        var modified = original with { BaudRate = BaudRate.Baud115200 };
        Assert.That(modified.BaudRate, Is.EqualTo(BaudRate.Baud115200));
        Assert.That(modified.PortName, Is.EqualTo("COM1"));
        Assert.That(original.BaudRate, Is.EqualTo(BaudRate.Baud9600));
    }
}
