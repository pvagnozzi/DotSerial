// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     IServiceCollection extension methods for registering DotSerial services.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering DotSerial services with
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SerialPortFactory"/> as the <see cref="Abstractions.ISerialPortFactory"/>
    /// singleton and makes it available for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDotSerial(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<Abstractions.ISerialPortFactory, SerialPortFactory>();
        return services;
    }
}
