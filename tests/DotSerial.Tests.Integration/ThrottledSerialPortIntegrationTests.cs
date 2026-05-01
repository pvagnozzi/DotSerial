// -----------------------------------------------------------------------
// <copyright file="ThrottledSerialPortIntegrationTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Integration tests for ThrottledSerialPort wrapping a real NetworkSerialPort
//     over a TCP loopback connection.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Integration;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using DotSerial.Decorators;
using DotSerial.Enums;
using DotSerial.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
[Category("Integration")]
public sealed class ThrottledSerialPortIntegrationTests
{
    private TcpListener _listener = null!;
    private int _port;
    private SerialPortFactory _factory = null!;
    private Task<TcpClient>? _acceptTask;

    [SetUp]
    public void SetUp()
    {
        _factory = new SerialPortFactory(
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning)));
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptTask = _listener.AcceptTcpClientAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _listener.Stop();
    }

    private SerialPortSettings NetworkSettings() =>
        new()
        {
            PortName = $"127.0.0.1:{_port}",
            ConnectionType = ConnectionType.Network,
            ReadTimeout = 3000,
            WriteTimeout = 3000,
        };

    // ── Good path ─────────────────────────────────────────────────────────

    [Test]
    public void Open_AndWrite_OverThrottledPort_Succeeds()
    {
        var inner = _factory.Create(NetworkSettings());
        using var throttled = new ThrottledSerialPort(
            inner, 1_000_000, NullLogger<ThrottledSerialPort>.Instance);

        throttled.Open();
        Assert.That(throttled.IsOpen, Is.True);

        var serverClient = _acceptTask!.GetAwaiter().GetResult();
        throttled.Write(new byte[] { 0x01, 0x02, 0x03 }, 0, 3);

        // Server verifies receipt.
        var buf = new byte[3];
        int n = serverClient.GetStream().Read(buf, 0, 3);
        Assert.That(n, Is.EqualTo(3));
        Assert.That(buf, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));

        throttled.Close();
        serverClient.Dispose();
    }

    [Test]
    public void Write_LargePayload_AtLowRate_InducesDelay()
    {
        var inner = _factory.Create(NetworkSettings());
        using var throttled = new ThrottledSerialPort(
            inner, 200, NullLogger<ThrottledSerialPort>.Instance);

        throttled.Open();
        _ = _acceptTask!.GetAwaiter().GetResult(); // accept but don't read for now

        var payload = new byte[400]; // 400 bytes at 200 B/s ≈ 2 s
        var sw = Stopwatch.StartNew();
        throttled.Write(payload, 0, payload.Length);
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1500),
            "Writing 400 bytes at 200 B/s should take at least ~1.5 s.");

        throttled.Close();
    }

    [Test]
    public void PortName_DelegatesToInner()
    {
        var inner = _factory.Create(NetworkSettings());
        using var throttled = new ThrottledSerialPort(
            inner, 1024, NullLogger<ThrottledSerialPort>.Instance);
        Assert.That(throttled.PortName, Is.EqualTo($"127.0.0.1:{_port}"));
    }

    [Test]
    public void Dispose_DisposesInnerAndClosesConnection()
    {
        var inner = _factory.Create(NetworkSettings());
        var throttled = new ThrottledSerialPort(
            inner, 1024, NullLogger<ThrottledSerialPort>.Instance);
        throttled.Open();
        _ = _acceptTask!.GetAwaiter().GetResult();
        throttled.Dispose();
        Assert.That(throttled.IsOpen, Is.False);
    }

    // ── Bad path ──────────────────────────────────────────────────────────

    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        var inner = _factory.Create(NetworkSettings());
        var throttled = new ThrottledSerialPort(
            inner, 1024, NullLogger<ThrottledSerialPort>.Instance);
        throttled.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            throttled.Write(new byte[] { 1 }, 0, 1));
    }

    [Test]
    public void Write_WhenNotOpen_ThrowsSerialPortException()
    {
        var inner = _factory.Create(NetworkSettings());
        using var throttled = new ThrottledSerialPort(
            inner, 1024, NullLogger<ThrottledSerialPort>.Instance);
        Assert.Throws<Exceptions.SerialPortException>(() =>
            throttled.Write(new byte[] { 1 }, 0, 1));
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Test]
    public void Write_EmptyPayload_NoDelay()
    {
        var inner = _factory.Create(NetworkSettings());
        using var throttled = new ThrottledSerialPort(
            inner, 100, NullLogger<ThrottledSerialPort>.Instance); // very slow

        throttled.Open();
        _ = _acceptTask!.GetAwaiter().GetResult();

        var sw = Stopwatch.StartNew();
        throttled.Write(Array.Empty<byte>(), 0, 0);
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(200),
            "Writing zero bytes should never throttle.");

        throttled.Close();
    }

    [Test]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var inner = _factory.Create(NetworkSettings());
        var throttled = new ThrottledSerialPort(
            inner, 1024, NullLogger<ThrottledSerialPort>.Instance);
        throttled.Dispose();
        Assert.DoesNotThrow(() => throttled.Dispose());
    }
}
