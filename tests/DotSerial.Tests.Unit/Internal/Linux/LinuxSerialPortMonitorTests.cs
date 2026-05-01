// -----------------------------------------------------------------------
// <copyright file="LinuxSerialPortMonitorTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for LinuxSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Internal.Linux;

using System.Runtime.InteropServices;
using DotSerial.Internal.Linux;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class LinuxSerialPortMonitorTests
{
    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new LinuxSerialPortMonitor(null!));
    }

    [Test]
    public void Constructor_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on non-Linux platforms.");

        Assert.Throws<PlatformNotSupportedException>(() =>
            _ = new LinuxSerialPortMonitor(
                NullLogger<LinuxSerialPortMonitor>.Instance));
    }

    [Test]
    public void Constructor_OnLinux_DoesNotThrow()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        Assert.DoesNotThrow(() =>
        {
            using var monitor = new LinuxSerialPortMonitor(
                NullLogger<LinuxSerialPortMonitor>.Instance);
        });
    }

    [Test]
    public void IsRunning_BeforeStart_IsFalse_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Start_SetsIsRunningTrue_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        monitor.Start();
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    [Test]
    public void Stop_AfterStart_SetsIsRunningFalse_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        monitor.Start();
        monitor.Stop();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void CurrentPorts_ReturnsNonNull_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        Assert.That(monitor.CurrentPorts, Is.Not.Null);
    }

    [Test]
    public void Start_CalledTwice_IsIdempotent_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        monitor.Start();
        Assert.DoesNotThrow(() => monitor.Start());
        monitor.Stop();
    }

    [Test]
    public void Stop_WhenNotRunning_IsIdempotent_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        Assert.DoesNotThrow(() => monitor.Stop());
    }

    [Test]
    public void Dispose_WhenNotRunning_IsIdempotent_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        Assert.DoesNotThrow(() =>
        {
            monitor.Dispose();
            monitor.Dispose();
        });
    }

    [Test]
    public void Start_AfterDispose_ThrowsObjectDisposedException_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        monitor.Dispose();
        Assert.Throws<ObjectDisposedException>(() => monitor.Start());
    }

    [Test]
    public void PortsChanged_CanSubscribeAndUnsubscribe_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Ignore("This test only runs on Linux.");

        using var monitor = new LinuxSerialPortMonitor(
            NullLogger<LinuxSerialPortMonitor>.Instance);
        EventHandler<DotSerial.Models.PortsChangedEventArgs> handler = (_, _) => { };

        Assert.DoesNotThrow(() =>
        {
            monitor.PortsChanged += handler;
            monitor.PortsChanged -= handler;
        });
    }
}
