// -----------------------------------------------------------------------
// <copyright file="SerialPortMonitorIntegrationTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Integration tests for ISerialPortMonitor lifecycle and PortsChanged event
//     on the current platform's desktop monitor implementation.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Integration;

using DotSerial.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

[TestFixture]
[Category("Integration")]
public sealed class SerialPortMonitorIntegrationTests
{
    private SerialPortFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new SerialPortFactory(
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning)));
    }

    [TearDown]
    public void TearDown()
    {
    }

    // ── Good path ─────────────────────────────────────────────────────────

    [Test]
    public void CreateMonitor_ReturnsNonNull()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void IsRunning_BeforeStart_IsFalse()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Start_SetsIsRunning_True()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Start();
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    [Test]
    public void Stop_AfterStart_SetsIsRunning_False()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Start();
        monitor.Stop();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public async Task StartAsync_SetsIsRunning_True()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        await monitor.StartAsync(CancellationToken.None);
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    [Test]
    public void CurrentPorts_ReturnsNonNull()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        Assert.That(monitor.CurrentPorts, Is.Not.Null);
    }

    [Test]
    public void CurrentPorts_IsReadOnlyList()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        Assert.That(monitor.CurrentPorts, Is.InstanceOf<IReadOnlyList<string>>());
    }

    [Test]
    public void CurrentPorts_AfterStart_ReturnsConsistentList()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(100));
        monitor.Start();
        var ports1 = monitor.CurrentPorts;
        var ports2 = monitor.CurrentPorts;
        // May differ between polls but both must be non-null and contain strings.
        Assert.That(ports1, Is.Not.Null);
        Assert.That(ports2, Is.Not.Null);
        monitor.Stop();
    }

    [Test]
    public void PortsChanged_CanSubscribeAndUnsubscribe_WhileRunning()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(100));
        monitor.Start();

        EventHandler<PortsChangedEventArgs> handler = (_, _) => { };
        Assert.DoesNotThrow(() =>
        {
            monitor.PortsChanged += handler;
            monitor.PortsChanged -= handler;
        });

        monitor.Stop();
    }

    // ── Idempotence / edge cases ──────────────────────────────────────────

    [Test]
    public void Start_CalledTwice_IsIdempotent()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Start();
        Assert.DoesNotThrow(() => monitor.Start());
        monitor.Stop();
    }

    [Test]
    public void Stop_WhenNotRunning_IsIdempotent()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        Assert.DoesNotThrow(() => monitor.Stop());
    }

    [Test]
    public void Dispose_StopsMonitor()
    {
        var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Start();
        monitor.Dispose();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Dispose();
        Assert.DoesNotThrow(() => monitor.Dispose());
    }

    [Test]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        monitor.Dispose();
        Assert.Throws<ObjectDisposedException>(() => monitor.Start());
    }

    // ── Bad path ──────────────────────────────────────────────────────────

    [Test]
    public void CreateMonitor_ZeroInterval_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _factory.CreateMonitor(TimeSpan.Zero));
    }

    [Test]
    public void CreateMonitor_NegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _factory.CreateMonitor(TimeSpan.FromMilliseconds(-1)));
    }

    [Test]
    public async Task StartAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        using var monitor = _factory.CreateMonitor(TimeSpan.FromMilliseconds(200));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitor.StartAsync(cts.Token));
    }
}
