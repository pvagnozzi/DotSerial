// -----------------------------------------------------------------------
// <copyright file="SerialPortStreamWrapperTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for SerialPortStreamWrapper — construction, property delegation,
//     stream access, disposal, and factory integration.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Streams;

using DotSerial.Abstractions;
using DotSerial.Models;
using DotSerial.Streams;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public sealed class SerialPortStreamWrapperTests
{
    private ISerialPort _mockPort = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPort = Substitute.For<ISerialPort>();
        _mockPort.PortName.Returns("COM1");
        _mockPort.IsOpen.Returns(true);
        _mockPort.BaseStream.Returns(new MemoryStream());
    }

    [TearDown]
    public void TearDown() => _mockPort?.Dispose();

    // ── Construction ──────────────────────────────────────────────────────

    [Test]
    public void Constructor_NullPort_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SerialPortStreamWrapper(null!));
    }

    [Test]
    public void Constructor_ValidPort_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            using var wrapper = new SerialPortStreamWrapper(_mockPort);
        });
    }

    // ── Property delegation ───────────────────────────────────────────────

    [Test]
    public void SerialPort_ReturnsWrappedPort()
    {
        using var wrapper = new SerialPortStreamWrapper(_mockPort);
        Assert.That(wrapper.SerialPort, Is.SameAs(_mockPort));
    }

    [Test]
    public void Stream_DelegatesToBaseStream()
    {
        using var wrapper = new SerialPortStreamWrapper(_mockPort);
        var stream = wrapper.Stream;
        Assert.That(stream, Is.SameAs(_mockPort.BaseStream));
    }

    // ── ISerialPortFactory.CreateStream ───────────────────────────────────

    [Test]
    public void CreateStream_ValidSettings_ReturnsNonNull()
    {
        var factory = new SerialPortFactory(NullLoggerFactory.Instance);
        var settings = new SerialPortSettings { PortName = "COM1" };
        using var streamWrapper = factory.CreateStream(settings);
        Assert.That(streamWrapper, Is.Not.Null);
        Assert.That(streamWrapper, Is.InstanceOf<ISerialPortStream>());
    }

    [Test]
    public void CreateStream_PortIsAccessibleViaSerialPort()
    {
        var factory = new SerialPortFactory(NullLoggerFactory.Instance);
        var settings = new SerialPortSettings { PortName = "COM2" };
        using var streamWrapper = factory.CreateStream(settings);
        Assert.That(streamWrapper.SerialPort, Is.Not.Null);
        Assert.That(streamWrapper.SerialPort.PortName, Is.EqualTo("COM2"));
    }

    [Test]
    public void CreateStream_NullSettings_ThrowsArgumentNullException()
    {
        var factory = new SerialPortFactory(NullLoggerFactory.Instance);
        Assert.Throws<ArgumentNullException>(() => factory.CreateStream(null!));
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    [Test]
    public void Dispose_DisposesUnderlyingPort()
    {
        var wrapper = new SerialPortStreamWrapper(_mockPort);
        wrapper.Dispose();
        _mockPort.Received(1).Dispose();
    }

    [Test]
    public void Dispose_CalledTwice_DisposesPortOnlyOnce()
    {
        var wrapper = new SerialPortStreamWrapper(_mockPort);
        wrapper.Dispose();
        wrapper.Dispose();
        _mockPort.Received(1).Dispose();
    }

    [Test]
    public async Task DisposeAsync_DisposesUnderlyingPort()
    {
        _mockPort.DisposeAsync().Returns(ValueTask.CompletedTask);
        var wrapper = new SerialPortStreamWrapper(_mockPort);
        await wrapper.DisposeAsync();
        await _mockPort.Received(1).DisposeAsync();
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_DisposesPortOnlyOnce()
    {
        _mockPort.DisposeAsync().Returns(ValueTask.CompletedTask);
        var wrapper = new SerialPortStreamWrapper(_mockPort);
        await wrapper.DisposeAsync();
        await wrapper.DisposeAsync();
        await _mockPort.Received(1).DisposeAsync();
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Test]
    public void Stream_AccessedMultipleTimes_ReturnsSameInstance()
    {
        using var wrapper = new SerialPortStreamWrapper(_mockPort);
        var s1 = wrapper.Stream;
        var s2 = wrapper.Stream;
        Assert.That(s1, Is.SameAs(s2));
    }
}
