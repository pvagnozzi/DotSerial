// -----------------------------------------------------------------------
// <copyright file="DesktopSerialPortMonitorTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for DesktopSerialPortMonitor — construction, start/stop lifecycle,
//     PortsChanged event, and disposal.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Internal.Desktop;

using DotSerial.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class DesktopSerialPortMonitorTests
{
    // ── Factory helper via the public SerialPortFactory ───────────────────

    private static ISerialPortMonitor CreateMonitor(TimeSpan? interval = null)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var factory = new SerialPortFactory(loggerFactory);
        return factory.CreateMonitor(interval ?? TimeSpan.FromMilliseconds(100));
    }

    // ── Construction ──────────────────────────────────────────────────────

    [Test]
    public void CreateMonitor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SerialPortFactory(null!));
    }

    [Test]
    public void CreateMonitor_ZeroInterval_ThrowsArgumentOutOfRangeException()
    {
        var factory = new SerialPortFactory(NullLoggerFactory.Instance);
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateMonitor(TimeSpan.Zero));
    }

    [Test]
    public void CreateMonitor_NegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        var factory = new SerialPortFactory(NullLoggerFactory.Instance);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.CreateMonitor(TimeSpan.FromMilliseconds(-1)));
    }

    [Test]
    public void CreateMonitor_ValidInterval_ReturnsNonNull()
    {
        using var monitor = CreateMonitor();
        Assert.That(monitor, Is.Not.Null);
    }

    // ── Initial state ─────────────────────────────────────────────────────

    [Test]
    public void IsRunning_BeforeStart_IsFalse()
    {
        using var monitor = CreateMonitor();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void CurrentPorts_BeforeStart_ReturnsNonNull()
    {
        using var monitor = CreateMonitor();
        Assert.That(monitor.CurrentPorts, Is.Not.Null);
    }

    [Test]
    public void CurrentPorts_BeforeStart_ReturnsReadOnlyList()
    {
        using var monitor = CreateMonitor();
        Assert.That(monitor.CurrentPorts, Is.InstanceOf<IReadOnlyList<string>>());
    }

    // ── Start / Stop lifecycle ────────────────────────────────────────────

    [Test]
    public void Start_SetsIsRunningTrue()
    {
        using var monitor = CreateMonitor();
        monitor.Start();
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    [Test]
    public void Stop_AfterStart_SetsIsRunningFalse()
    {
        using var monitor = CreateMonitor();
        monitor.Start();
        monitor.Stop();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Start_CalledTwice_IsIdempotent()
    {
        using var monitor = CreateMonitor();
        monitor.Start();
        Assert.DoesNotThrow(() => monitor.Start());
        monitor.Stop();
    }

    [Test]
    public void Stop_WhenNotRunning_IsIdempotent()
    {
        using var monitor = CreateMonitor();
        Assert.DoesNotThrow(() => monitor.Stop());
    }

    [Test]
    public async Task StartAsync_SetsIsRunningTrue()
    {
        using var monitor = CreateMonitor();
        await monitor.StartAsync();
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    [Test]
    public void Dispose_StopsMonitor()
    {
        var monitor = CreateMonitor();
        monitor.Start();
        monitor.Dispose();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Dispose_WhenNotRunning_IsIdempotent()
    {
        var monitor = CreateMonitor();
        Assert.DoesNotThrow(() =>
        {
            monitor.Dispose();
            monitor.Dispose();
        });
    }

    [Test]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        var monitor = CreateMonitor();
        monitor.Dispose();
        Assert.Throws<ObjectDisposedException>(() => monitor.Start());
    }

    // ── PortsChanged event (observable) ───────────────────────────────────

    [Test]
    public void PortsChanged_CanSubscribeAndUnsubscribe()
    {
        using var monitor = CreateMonitor();
        EventHandler<DotSerial.Models.PortsChangedEventArgs> handler = (_, _) => { };

        Assert.DoesNotThrow(() =>
        {
            monitor.PortsChanged += handler;
            monitor.PortsChanged -= handler;
        });
    }
}
