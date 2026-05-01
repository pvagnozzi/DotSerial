// -----------------------------------------------------------------------
// <copyright file="MacOSSerialPortMonitorTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for MacOSSerialPortMonitor.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Internal.MacOS;

using System.Runtime.InteropServices;
using DotSerial.Internal.MacOS;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public sealed class MacOSSerialPortMonitorTests
{
    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new MacOSSerialPortMonitor(null!));
    }

    [Test]
    public void Constructor_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on non-macOS platforms.");

        Assert.Throws<PlatformNotSupportedException>(() =>
            _ = new MacOSSerialPortMonitor(
                NullLogger<MacOSSerialPortMonitor>.Instance));
    }

    [Test]
    public void Constructor_OnMacOS_DoesNotThrow()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        Assert.DoesNotThrow(() =>
        {
            using var monitor = new MacOSSerialPortMonitor(
                NullLogger<MacOSSerialPortMonitor>.Instance);
        });
    }

    [Test]
    public void IsRunning_BeforeStart_IsFalse_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void Start_SetsIsRunningTrue_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        monitor.Start();
        Assert.That(monitor.IsRunning, Is.True);
        monitor.Stop();
    }

    [Test]
    public void Stop_AfterStart_SetsIsRunningFalse_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        monitor.Start();
        monitor.Stop();
        Assert.That(monitor.IsRunning, Is.False);
    }

    [Test]
    public void CurrentPorts_ReturnsNonNull_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        Assert.That(monitor.CurrentPorts, Is.Not.Null);
    }

    [Test]
    public void Start_CalledTwice_IsIdempotent_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        monitor.Start();
        Assert.DoesNotThrow(() => monitor.Start());
        monitor.Stop();
    }

    [Test]
    public void Stop_WhenNotRunning_IsIdempotent_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        Assert.DoesNotThrow(() => monitor.Stop());
    }

    [Test]
    public void Dispose_WhenNotRunning_IsIdempotent_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        Assert.DoesNotThrow(() =>
        {
            monitor.Dispose();
            monitor.Dispose();
        });
    }

    [Test]
    public void Start_AfterDispose_ThrowsObjectDisposedException_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        monitor.Dispose();
        Assert.Throws<ObjectDisposedException>(() => monitor.Start());
    }

    [Test]
    public void PortsChanged_CanSubscribeAndUnsubscribe_OnMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Ignore("This test only runs on macOS.");

        using var monitor = new MacOSSerialPortMonitor(
            NullLogger<MacOSSerialPortMonitor>.Instance);
        EventHandler<DotSerial.Models.PortsChangedEventArgs> handler = (_, _) => { };

        Assert.DoesNotThrow(() =>
        {
            monitor.PortsChanged += handler;
            monitor.PortsChanged -= handler;
        });
    }
}
