// -----------------------------------------------------------------------
// <copyright file="SerialPortIntegrationTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Integration tests for serial port operations against real or virtual COM ports.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Integration;

using DotSerial.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

[TestFixture]
[Category("Integration")]
public sealed class SerialPortIntegrationTests
{
    private const string SkipMessage = "No serial ports available on this machine. Skipping integration test.";

    [Test]
    public void GetPortNames_ReturnsListOrEmpty()
    {
        var factory = CreateFactory();
        var ports = factory.GetPortNames();
        Assert.That(ports, Is.Not.Null);
    }

    [Test]
    public void Create_WithFirstAvailablePort_ReturnsNonNullPort()
    {
        var factory = CreateFactory();
        var ports = factory.GetPortNames();
        if (ports.Count == 0) Assert.Ignore(SkipMessage);

        var settings = new SerialPortSettings { PortName = ports[0] };
        using var port = factory.Create(settings);
        Assert.That(port, Is.Not.Null);
        Assert.That(port.PortName, Is.EqualTo(ports[0]));
    }

    [Test]
    public void Open_AndClose_WithFirstAvailablePort_Succeeds()
    {
        var factory = CreateFactory();
        var ports = factory.GetPortNames();
        if (ports.Count == 0) Assert.Ignore(SkipMessage);

        var settings = new SerialPortSettings { PortName = ports[0], ReadTimeout = 1000, WriteTimeout = 1000 };
        using var port = factory.Create(settings);
        port.Open();
        Assert.That(port.IsOpen, Is.True);
        port.Close();
        Assert.That(port.IsOpen, Is.False);
    }

    private static SerialPortFactory CreateFactory()
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        return new SerialPortFactory(loggerFactory);
    }
}
