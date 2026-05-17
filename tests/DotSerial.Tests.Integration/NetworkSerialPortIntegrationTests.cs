// -----------------------------------------------------------------------
// <copyright file="NetworkSerialPortIntegrationTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Integration tests for NetworkSerialPort using a real TCP loopback server
//     — good path, bad path, and edge cases.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Integration;

using System.Net;
using System.Net.Sockets;
using DotSerial.Config;
using DotSerial.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

/// <summary>
/// Integration tests for <see cref="DotSerial.Internal.Network.NetworkSerialPort"/> using a
/// real in-process TCP loopback server to exercise actual network I/O.
/// </summary>
[TestFixture]
[Category("Integration")]
public sealed class NetworkSerialPortIntegrationTests
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

    private SerialPortConfig NetworkSettings() =>
        new()
        {
            PortName = $"127.0.0.1:{_port}",
            ConnectionType = ConnectionType.Network,
            ReadTimeout = 2000,
            WriteTimeout = 2000,
        };

    // ── Good path ─────────────────────────────────────────────────────────

    [Test]
    public void Open_ToListeningServer_Succeeds()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.That(port.IsOpen, Is.True);
        port.Close();
    }

    [Test]
    public async Task OpenAsync_ToListeningServer_Succeeds()
    {
        using var port = _factory.Create(NetworkSettings());
        await port.OpenAsync(CancellationToken.None);
        Assert.That(port.IsOpen, Is.True);
        await port.CloseAsync(CancellationToken.None);
    }

    [Test]
    public void Open_Close_IsOpen_TransitionsCorrectly()
    {
        using var port = _factory.Create(NetworkSettings());
        Assert.That(port.IsOpen, Is.False);
        port.Open();
        Assert.That(port.IsOpen, Is.True);
        port.Close();
        Assert.That(port.IsOpen, Is.False);
    }

    [Test]
    public void Write_AndRead_Roundtrip_Succeeds()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();

        // Server side: echo back.
        var serverClient = _acceptTask!.GetAwaiter().GetResult();
        var serverStream = serverClient.GetStream();

        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        port.Write(payload, 0, payload.Length);

        // Server reads and echoes.
        var serverBuf = new byte[payload.Length];
        _ = serverStream.Read(serverBuf, 0, serverBuf.Length);
        serverStream.Write(serverBuf, 0, serverBuf.Length);
        serverStream.Flush();

        // Client reads echo.
        var readBuf = new byte[payload.Length];
        int read = port.Read(readBuf, 0, readBuf.Length);
        Assert.That(read, Is.EqualTo(payload.Length));
        Assert.That(readBuf, Is.EqualTo(payload));

        port.Close();
        serverClient.Dispose();
    }

    [Test]
    public void WriteLine_AndReadLine_Roundtrip_Succeeds()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();

        var serverClient = _acceptTask!.GetAwaiter().GetResult();
        var serverStream = serverClient.GetStream();
        var serverReader = new System.IO.StreamReader(serverStream, System.Text.Encoding.UTF8, leaveOpen: true);
        var serverWriter = new System.IO.StreamWriter(serverStream, System.Text.Encoding.UTF8, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n",
        };

        port.WriteLine("HELLO");

        var received = serverReader.ReadLine();
        serverWriter.WriteLine(received);

        var reply = port.ReadLine();
        Assert.That(reply, Is.EqualTo("HELLO"));

        port.Close();
        serverClient.Dispose();
    }

    [Test]
    public void BaseStream_WhenOpen_IsNotNull()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.That(port.BaseStream, Is.Not.Null);
        port.Close();
    }

    [Test]
    public void DiscardInBuffer_WhenOpen_DoesNotThrow()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.DoesNotThrow(() => port.DiscardInBuffer());
        port.Close();
    }

    [Test]
    public void DiscardOutBuffer_WhenOpen_DoesNotThrow()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.DoesNotThrow(() => port.DiscardOutBuffer());
        port.Close();
    }

    [Test]
    public void Open_CalledTwice_IsIdempotent()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.DoesNotThrow(() => port.Open());
        port.Close();
    }

    [Test]
    public void Close_CalledTwice_IsIdempotent()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        port.Close();
        Assert.DoesNotThrow(() => port.Close());
    }

    // ── Bad path ──────────────────────────────────────────────────────────

    [Test]
    public void Open_WhenNoServerListening_ThrowsSerialPortException()
    {
        _listener.Stop(); // close server before connecting
        using var port = _factory.Create(NetworkSettings());
        Assert.Throws<Exceptions.SerialPortException>(() => port.Open());
    }

    [Test]
    public void Write_WhenNotOpen_ThrowsSerialPortException()
    {
        using var port = _factory.Create(NetworkSettings());
        Assert.Throws<Exceptions.SerialPortException>(() =>
            port.Write(new byte[] { 1 }, 0, 1));
    }

    [Test]
    public void Read_WhenNotOpen_ThrowsSerialPortException()
    {
        using var port = _factory.Create(NetworkSettings());
        Assert.Throws<Exceptions.SerialPortException>(() =>
            port.Read(new byte[10], 0, 10));
    }

    [Test]
    public void Write_NullBuffer_ThrowsArgumentNullException()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.Throws<ArgumentNullException>(() => port.Write(null!, 0, 1));
        port.Close();
    }

    [Test]
    public void Read_NullBuffer_ThrowsArgumentNullException()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.Throws<ArgumentNullException>(() => port.Read(null!, 0, 1));
        port.Close();
    }

    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        var port = _factory.Create(NetworkSettings());
        port.Dispose();
        Assert.Throws<ObjectDisposedException>(() => port.Write("test"));
    }

    [Test]
    public async Task OpenAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        using var port = _factory.Create(NetworkSettings());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await port.OpenAsync(cts.Token));
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Test]
    public void Write_EmptyArray_DoesNotThrow()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.DoesNotThrow(() => port.Write(Array.Empty<byte>(), 0, 0));
        port.Close();
    }

    [Test]
    public void DataReceived_CanSubscribeAndUnsubscribe()
    {
        using var port = _factory.Create(NetworkSettings());
        EventHandler<SerialDataReceivedEventArgs> handler = (_, _) => { };
        Assert.DoesNotThrow(() =>
        {
            port.DataReceived += handler;
            port.DataReceived -= handler;
        });
    }

    [Test]
    public void ErrorReceived_CanSubscribeAndUnsubscribe()
    {
        using var port = _factory.Create(NetworkSettings());
        EventHandler<SerialErrorReceivedEventArgs> handler = (_, _) => { };
        Assert.DoesNotThrow(() =>
        {
            port.ErrorReceived += handler;
            port.ErrorReceived -= handler;
        });
    }

    [Test]
    public void Dispose_WhenOpen_ClosesPort()
    {
        var port = _factory.Create(NetworkSettings());
        port.Open();
        port.Dispose();
        Assert.That(port.IsOpen, Is.False);
    }

    [Test]
    public async Task DisposeAsync_WhenOpen_ClosesPort()
    {
        var port = _factory.Create(NetworkSettings());
        port.Open();
        await port.DisposeAsync();
        Assert.That(port.IsOpen, Is.False);
    }

    [Test]
    public void ReadTimeout_CanBeChangedWhileOpen()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        port.ReadTimeout = 3000;
        Assert.That(port.ReadTimeout, Is.EqualTo(3000));
        port.Close();
    }

    [Test]
    public void WriteTimeout_CanBeChangedWhileOpen()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        port.WriteTimeout = 3000;
        Assert.That(port.WriteTimeout, Is.EqualTo(3000));
        port.Close();
    }

    [Test]
    public void BytesToRead_WhenDataAvailable_IsNonNegative()
    {
        using var port = _factory.Create(NetworkSettings());
        port.Open();
        Assert.That(port.BytesToRead, Is.GreaterThanOrEqualTo(0));
        port.Close();
    }
}
