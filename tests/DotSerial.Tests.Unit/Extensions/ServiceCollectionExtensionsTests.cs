// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Unit tests for ServiceCollectionExtensions.AddDotSerial DI registration.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Tests.Unit.Extensions;

using DotSerial.Abstractions;
using DotSerial.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public sealed class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddDotSerial_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddDotSerial());
    }

    [Test]
    public void AddDotSerial_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddDotSerial();
        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddDotSerial_RegistersISerialPortFactoryAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDotSerial();

        var provider = services.BuildServiceProvider();
        var factory1 = provider.GetRequiredService<ISerialPortFactory>();
        var factory2 = provider.GetRequiredService<ISerialPortFactory>();

        Assert.That(factory1, Is.Not.Null);
        Assert.That(factory1, Is.SameAs(factory2));
        Assert.That(factory1, Is.InstanceOf<SerialPortFactory>());
    }
}
