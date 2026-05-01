// -----------------------------------------------------------------------
// <copyright file="SerialPortExceptionTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for SerialPortException, SerialPortNotFoundException, and
//     SerialPortTimeoutException — constructors, message, inheritance, and properties.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Exceptions;

using DotSerial.Exceptions;
using NUnit.Framework;

// ── SerialPortException ───────────────────────────────────────────────────

[TestFixture]
public sealed class SerialPortExceptionTests
{
    [Test]
    public void DefaultConstructor_HasDefaultMessage()
    {
        var ex = new SerialPortException();
        Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new SerialPortException("test error");
        Assert.That(ex.Message, Is.EqualTo("test error"));
    }

    [Test]
    public void MessageAndInnerConstructor_SetsBoth()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SerialPortException("outer", inner);
        Assert.That(ex.Message, Is.EqualTo("outer"));
        Assert.That(ex.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void IsAssignableFrom_Exception()
    {
        var ex = new SerialPortException();
        Assert.That(ex, Is.InstanceOf<Exception>());
    }
}

// ── SerialPortNotFoundException ───────────────────────────────────────────

[TestFixture]
public sealed class SerialPortNotFoundExceptionTests
{
    [Test]
    public void Constructor_SetsPortName()
    {
        var ex = new SerialPortNotFoundException("COM1");
        Assert.That(ex.PortName, Is.EqualTo("COM1"));
    }

    [Test]
    public void Constructor_MessageContainsPortName()
    {
        var ex = new SerialPortNotFoundException("COM99");
        Assert.That(ex.Message, Does.Contain("COM99"));
    }

    [Test]
    public void ConstructorWithInner_SetsPortNameAndInner()
    {
        var inner = new Exception("hw error");
        var ex = new SerialPortNotFoundException("COM2", inner);
        Assert.That(ex.PortName, Is.EqualTo("COM2"));
        Assert.That(ex.InnerException, Is.SameAs(inner));
        Assert.That(ex.Message, Does.Contain("COM2"));
    }

    [Test]
    public void IsAssignableFrom_SerialPortException()
    {
        var ex = new SerialPortNotFoundException("COM1");
        Assert.That(ex, Is.InstanceOf<SerialPortException>());
    }

    [Test]
    public void IsAssignableFrom_Exception()
    {
        var ex = new SerialPortNotFoundException("COM1");
        Assert.That(ex, Is.InstanceOf<Exception>());
    }

    // Bad path — empty port name is still accepted (exception is about the port not being found)
    [Test]
    public void Constructor_EmptyPortName_StillConstructs()
    {
        var ex = new SerialPortNotFoundException(string.Empty);
        Assert.That(ex.PortName, Is.EqualTo(string.Empty));
    }
}

// ── SerialPortTimeoutException ─────────────────────────────────────────────

[TestFixture]
public sealed class SerialPortTimeoutExceptionTests
{
    [Test]
    public void DefaultConstructor_HasDefaultMessage()
    {
        var ex = new SerialPortTimeoutException();
        Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new SerialPortTimeoutException("timed out reading");
        Assert.That(ex.Message, Is.EqualTo("timed out reading"));
    }

    [Test]
    public void MessageAndInnerConstructor_SetsBoth()
    {
        var inner = new TimeoutException("hw timeout");
        var ex = new SerialPortTimeoutException("timed out", inner);
        Assert.That(ex.Message, Is.EqualTo("timed out"));
        Assert.That(ex.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void IsAssignableFrom_SerialPortException()
    {
        var ex = new SerialPortTimeoutException();
        Assert.That(ex, Is.InstanceOf<SerialPortException>());
    }

    [Test]
    public void IsAssignableFrom_Exception()
    {
        var ex = new SerialPortTimeoutException();
        Assert.That(ex, Is.InstanceOf<Exception>());
    }
}
