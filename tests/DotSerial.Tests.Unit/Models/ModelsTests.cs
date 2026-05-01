// -----------------------------------------------------------------------
// <copyright file="ModelsTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for model event-args types: PortsChangedEventArgs,
//     SerialDataReceivedEventArgs, SerialErrorReceivedEventArgs, SerialPinChangedEventArgs.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Models;

using DotSerial.Enums;
using DotSerial.Models;
using NUnit.Framework;

// ── PortsChangedEventArgs ─────────────────────────────────────────────────

[TestFixture]
public sealed class PortsChangedEventArgsTests
{
    private static readonly IReadOnlyList<string> Empty = Array.Empty<string>();

    [Test]
    public void Constructor_SetsAllProperties()
    {
        var added = new[] { "COM3" };
        var removed = new[] { "COM1" };
        var all = new[] { "COM2", "COM3" };

        var args = new PortsChangedEventArgs(added, removed, all);

        Assert.That(args.AddedPorts, Is.EqualTo(added));
        Assert.That(args.RemovedPorts, Is.EqualTo(removed));
        Assert.That(args.AllPorts, Is.EqualTo(all));
    }

    [Test]
    public void Constructor_NullAddedPorts_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new PortsChangedEventArgs(null!, Empty, Empty));
    }

    [Test]
    public void Constructor_NullRemovedPorts_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new PortsChangedEventArgs(Empty, null!, Empty));
    }

    [Test]
    public void Constructor_NullAllPorts_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new PortsChangedEventArgs(Empty, Empty, null!));
    }

    [Test]
    public void Constructor_EmptyLists_IsValid()
    {
        var args = new PortsChangedEventArgs(Empty, Empty, Empty);
        Assert.That(args.AddedPorts, Is.Empty);
        Assert.That(args.RemovedPorts, Is.Empty);
        Assert.That(args.AllPorts, Is.Empty);
    }

    [Test]
    public void Constructor_AllPortsCanDifferFromAddedAndRemoved()
    {
        // All can contain more ports than just the delta.
        var added = new[] { "COM4" };
        var removed = Empty;
        var all = new[] { "COM2", "COM3", "COM4" };

        var args = new PortsChangedEventArgs(added, removed, all);

        Assert.That(args.AllPorts.Count, Is.EqualTo(3));
        Assert.That(args.AddedPorts.Count, Is.EqualTo(1));
        Assert.That(args.RemovedPorts.Count, Is.EqualTo(0));
    }

    [Test]
    public void IsAssignableFrom_EventArgs()
    {
        var args = new PortsChangedEventArgs(Empty, Empty, Empty);
        Assert.That(args, Is.InstanceOf<EventArgs>());
    }
}

// ── SerialDataReceivedEventArgs ───────────────────────────────────────────

[TestFixture]
public sealed class SerialDataReceivedEventArgsTests
{
    [Test]
    [TestCase(SerialData.Chars)]
    [TestCase(SerialData.Eof)]
    public void Constructor_SetsEventType(SerialData eventType)
    {
        var args = new SerialDataReceivedEventArgs(eventType);
        Assert.That(args.EventType, Is.EqualTo(eventType));
    }

    [Test]
    public void IsAssignableFrom_EventArgs()
    {
        var args = new SerialDataReceivedEventArgs(SerialData.Chars);
        Assert.That(args, Is.InstanceOf<EventArgs>());
    }
}

// ── SerialErrorReceivedEventArgs ──────────────────────────────────────────

[TestFixture]
public sealed class SerialErrorReceivedEventArgsTests
{
    [Test]
    [TestCase(SerialError.Frame)]
    [TestCase(SerialError.Overrun)]
    [TestCase(SerialError.RXOver)]
    [TestCase(SerialError.RXParity)]
    [TestCase(SerialError.TXFull)]
    public void Constructor_SetsErrorType(SerialError errorType)
    {
        var args = new SerialErrorReceivedEventArgs(errorType);
        Assert.That(args.ErrorType, Is.EqualTo(errorType));
    }

    [Test]
    public void IsAssignableFrom_EventArgs()
    {
        var args = new SerialErrorReceivedEventArgs(SerialError.Frame);
        Assert.That(args, Is.InstanceOf<EventArgs>());
    }
}

// ── SerialPinChangedEventArgs ─────────────────────────────────────────────

[TestFixture]
public sealed class SerialPinChangedEventArgsTests
{
    [Test]
    [TestCase(SerialPinChange.Break)]
    [TestCase(SerialPinChange.CDChanged)]
    [TestCase(SerialPinChange.CtsChanged)]
    [TestCase(SerialPinChange.DsrChanged)]
    [TestCase(SerialPinChange.Ring)]
    public void Constructor_SetsEventType(SerialPinChange eventType)
    {
        var args = new SerialPinChangedEventArgs(eventType);
        Assert.That(args.EventType, Is.EqualTo(eventType));
    }

    [Test]
    public void IsAssignableFrom_EventArgs()
    {
        var args = new SerialPinChangedEventArgs(SerialPinChange.Break);
        Assert.That(args, Is.InstanceOf<EventArgs>());
    }
}
