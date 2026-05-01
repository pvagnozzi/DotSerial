// -----------------------------------------------------------------------
// <copyright file="NetworkSerialPortTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for NetworkSerialPort — construction, validation, port properties,
//     and open/close lifecycle via SerialPortFactory.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Internal.Network;

using DotSerial.Enums;
using DotSerial.Exceptions;
using DotSerial.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class NetworkSerialPortTests
{
    private SerialPortFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new SerialPortFactory(NullLoggerFactory.Instance);

    // ── Validation (via SerialPortSettings.Validate) ──────────────────────

    [Test]
    public void Create_ValidNetworkSettings_ReturnsPort()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port, Is.Not.Null);
    }

    [Test]
    [TestCase("nocolon")]
    [TestCase(":9999")]
    [TestCase("192.168.1.1:")]
    [TestCase("192.168.1.1:0")]
    [TestCase("192.168.1.1:65536")]
    [TestCase("192.168.1.1:-1")]
    [TestCase("192.168.1.1:abc")]
    public void Create_InvalidHostPort_ThrowsSerialPortException(string portName)
    {
        var settings = new SerialPortSettings
        {
            PortName = portName,
            ConnectionType = ConnectionType.Network,
        };
        Assert.Throws<SerialPortException>(() => _factory.Create(settings));
    }

    // ── Property delegation ───────────────────────────────────────────────

    [Test]
    public void PortName_ReturnsSettingsPortName()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port.PortName, Is.EqualTo("127.0.0.1:9998"));
    }

    [Test]
    public void BaudRate_ReturnsSettingsBaudRate()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port.BaudRate, Is.EqualTo(9600));
    }

    [Test]
    public void IsOpen_BeforeConnect_IsFalse()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port.IsOpen, Is.False);
    }

    [Test]
    public void BytesToRead_BeforeConnect_IsZero()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port.BytesToRead, Is.EqualTo(0));
    }

    [Test]
    public void BytesToWrite_IsAlwaysZero()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port.BytesToWrite, Is.EqualTo(0));
    }

    // ── Lifecycle (not connected) ─────────────────────────────────────────

    [Test]
    public void Open_WhenServerNotListening_ThrowsSerialPortException()
    {
        // Port 9 (discard service) is almost always not listening on localhost;
        // using a high ephemeral port that is very unlikely to be in use.
        using var port = _factory.Create(new SerialPortSettings
        {
            PortName = "127.0.0.1:59998",
            ConnectionType = ConnectionType.Network,
        });
        Assert.Throws<SerialPortException>(() => port.Open());
    }

    [Test]
    public void Write_WhenNotConnected_ThrowsSerialPortException()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.Throws<SerialPortException>(() =>
            port.Write(new byte[] { 1, 2 }, 0, 2));
    }

    [Test]
    public void ReadByte_WhenNotConnected_ThrowsSerialPortException()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.Throws<SerialPortException>(() => port.ReadByte());
    }

    [Test]
    public void BaseStream_WhenNotConnected_ThrowsSerialPortException()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.Throws<SerialPortException>(() => _ = port.BaseStream);
    }

    // ── Timeout mutability ────────────────────────────────────────────────

    [Test]
    public void ReadTimeout_CanBeSetBeforeOpen()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        port.ReadTimeout = 2000;
        Assert.That(port.ReadTimeout, Is.EqualTo(2000));
    }

    [Test]
    public void WriteTimeout_CanBeSetBeforeOpen()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        port.WriteTimeout = 2000;
        Assert.That(port.WriteTimeout, Is.EqualTo(2000));
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    [Test]
    public void Dispose_WhenNotConnected_IsIdempotent()
    {
        var port = _factory.Create(ValidNetworkSettings());
        Assert.DoesNotThrow(() =>
        {
            port.Dispose();
            port.Dispose();
        });
    }

    [Test]
    public async Task DisposeAsync_WhenNotConnected_IsIdempotent()
    {
        var port = _factory.Create(ValidNetworkSettings());
        await port.DisposeAsync();
        await port.DisposeAsync();
    }

    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        var port = _factory.Create(ValidNetworkSettings());
        port.Dispose();
        Assert.Throws<ObjectDisposedException>(() => port.Write("hello"));
    }

    // ── DiscardBuffers (no-op) ────────────────────────────────────────────

    [Test]
    public void DiscardInBuffer_DoesNotThrow()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.DoesNotThrow(() => port.DiscardInBuffer());
    }

    [Test]
    public void DiscardOutBuffer_DoesNotThrow()
    {
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.DoesNotThrow(() => port.DiscardOutBuffer());
    }

    // ── Routing — Network takes priority over platform ────────────────────

    [Test]
    public void Create_NetworkType_IsNotPlatformNotSupported()
    {
        // On desktop, Bluetooth throws PlatformNotSupportedException but Network must succeed.
        using var port = _factory.Create(ValidNetworkSettings());
        Assert.That(port, Is.Not.Null, "Network connection type must be supported on desktop.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static SerialPortSettings ValidNetworkSettings() =>
        new()
        {
            PortName = "127.0.0.1:9998",
            ConnectionType = ConnectionType.Network,
        };
}
