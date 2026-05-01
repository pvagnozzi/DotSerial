// -----------------------------------------------------------------------
// <copyright file="ThrottledSerialPortTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for ThrottledSerialPort — construction, throttle behaviour,
//     delegation, and disposal.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Decorators;

using System.Diagnostics;
using DotSerial.Abstractions;
using DotSerial.Decorators;
using DotSerial.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public sealed class ThrottledSerialPortTests
{
    private ISerialPort _mockPort = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPort = Substitute.For<ISerialPort>();
        _mockPort.PortName.Returns("COM1");
        _mockPort.IsOpen.Returns(true);
    }

    // ── Construction ──────────────────────────────────────────────────────

    [Test]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ThrottledSerialPort(null!, 1024, NullLogger<ThrottledSerialPort>.Instance));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ThrottledSerialPort(_mockPort, 1024, null!));
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-100)]
    public void Constructor_InvalidMaxBytesPerSecond_ThrowsArgumentOutOfRangeException(int maxBps)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ThrottledSerialPort(_mockPort, maxBps, NullLogger<ThrottledSerialPort>.Instance));
    }

    [Test]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            using var throttled = new ThrottledSerialPort(
                _mockPort, 1024, NullLogger<ThrottledSerialPort>.Instance);
        });
    }

    // ── Property delegation ───────────────────────────────────────────────

    [Test]
    public void PortName_DelegatesToInner()
    {
        using var throttled = MakeThrottled(1024);
        Assert.That(throttled.PortName, Is.EqualTo("COM1"));
    }

    [Test]
    public void IsOpen_DelegatesToInner()
    {
        using var throttled = MakeThrottled(1024);
        Assert.That(throttled.IsOpen, Is.True);
    }

    [Test]
    public void ReadTimeout_GetAndSet_DelegatesToInner()
    {
        _mockPort.ReadTimeout.Returns(500);
        using var throttled = MakeThrottled(1024);
        _ = throttled.ReadTimeout;
        throttled.ReadTimeout = 1000;

        _mockPort.Received(1).ReadTimeout = 1000;
    }

    // ── Write delegation ──────────────────────────────────────────────────

    [Test]
    public void Write_ByteArray_DelegatesToInner()
    {
        using var throttled = MakeThrottled(1_000_000); // high limit — no real delay
        var data = new byte[] { 1, 2, 3 };
        throttled.Write(data, 0, data.Length);
        _mockPort.Received(1).Write(data, 0, data.Length);
    }

    [Test]
    public void Write_String_DelegatesToInner()
    {
        using var throttled = MakeThrottled(1_000_000);
        throttled.Write("hello");
        _mockPort.Received(1).Write("hello");
    }

    [Test]
    public void WriteLine_DelegatesToInner()
    {
        using var throttled = MakeThrottled(1_000_000);
        throttled.WriteLine("AT");
        _mockPort.Received(1).WriteLine("AT");
    }

    // ── Read delegation (unthrottled) ─────────────────────────────────────

    [Test]
    public void ReadByte_DelegatesToInner()
    {
        _mockPort.ReadByte().Returns(42);
        using var throttled = MakeThrottled(1024);
        var result = throttled.ReadByte();
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ReadLine_DelegatesToInner()
    {
        _mockPort.ReadLine().Returns("response");
        using var throttled = MakeThrottled(1024);
        var result = throttled.ReadLine();
        Assert.That(result, Is.EqualTo("response"));
    }

    // ── Throttle behaviour ────────────────────────────────────────────────

    [Test]
    public void Write_SmallPayload_CompletesWithoutMeasurableDelay()
    {
        // 1 MB/s limit, writing 10 bytes — should complete almost instantly.
        using var throttled = MakeThrottled(1_048_576);
        var data = new byte[10];
        var sw = Stopwatch.StartNew();
        throttled.Write(data, 0, data.Length);
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(50),
            "Small writes under the rate limit should not be delayed.");
    }

    [Test]
    public void Write_ExceedsRateLimit_InducesDelay()
    {
        // 100 bytes/s limit, writing 200 bytes — must wait ~1 s.
        using var throttled = MakeThrottled(100);
        var data = new byte[200];
        var sw = Stopwatch.StartNew();
        throttled.Write(data, 0, data.Length);
        sw.Stop();

        // Should take at least ~900 ms (with 100 ms tolerance for CI jitter)
        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(900),
            "Writing 200 bytes at 100 B/s should delay approximately 1 second.");
    }

    // ── Disposal ──────────────────────────────────────────────────────────

    [Test]
    public void Dispose_DisposesInner()
    {
        var throttled = MakeThrottled(1024);
        throttled.Dispose();
        _mockPort.Received(1).Dispose();
    }

    [Test]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var throttled = MakeThrottled(1024);
        throttled.Dispose();
        Assert.DoesNotThrow(() => throttled.Dispose());
        // Inner Dispose called only once
        _mockPort.Received(1).Dispose();
    }

    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        var throttled = MakeThrottled(1024);
        throttled.Dispose();
        Assert.Throws<ObjectDisposedException>(() => throttled.Write("test"));
    }

    [Test]
    public void Open_AfterDispose_ThrowsObjectDisposedException()
    {
        var throttled = MakeThrottled(1024);
        throttled.Dispose();
        Assert.Throws<ObjectDisposedException>(() => throttled.Open());
    }

    // ── Event passthrough ─────────────────────────────────────────────────

    [Test]
    public void DataReceived_AddAndRemoveHandler_PassedToInner()
    {
        using var throttled = MakeThrottled(1024);
        EventHandler<SerialDataReceivedEventArgs> handler = (_, _) => { };

        throttled.DataReceived += handler;
        throttled.DataReceived -= handler;

        _mockPort.Received(1).DataReceived += handler;
        _mockPort.Received(1).DataReceived -= handler;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ThrottledSerialPort MakeThrottled(int maxBytesPerSecond)
        => new(_mockPort, maxBytesPerSecond, NullLogger<ThrottledSerialPort>.Instance);
}
