// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to decorate registered services.
/// </summary>
public static class DecoratorServiceCollectionExtensions
{
    // Extended version of this article <https://andrewlock.net/adding-decorated-classes-to-the-asp.net-core-di-container-using-scrutor/>

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/> with the same type.
    /// </summary>
    /// <typeparam name="TService">The type of service to decorate.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional function to configure the decorated instance.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<TService, TService>? configure = null)
        => services.Decorate<TService, TService>(configure);

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/> with a decorator of type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service to decorate.</typeparam>
    /// <typeparam name="TDecorator">The type of the decorator. Must inherit from or implement <typeparamref name="TService"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional function to configure the decorated instance.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection Decorate<TService, TDecorator>(
        this IServiceCollection services,
        Func<TDecorator, TService>? configure = null)
        where TDecorator : TService
    {
        services.TryDecorateDescriptors(
            typeof(TService),
            x => x.Decorate(configure));

        return services;
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/> using a factory function that has access to the service provider.
    /// </summary>
    /// <typeparam name="TService">The type of service to decorate.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="decoratorFactory">A factory function that creates the decorated service, with access to the service provider and the inner service instance.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService, TService> decoratorFactory)
        => services.Decorate<TService, TService>(decoratorFactory);

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/> with a decorator of type <typeparamref name="TDecorator"/> using a factory function.
    /// </summary>
    /// <typeparam name="TService">The type of service to decorate.</typeparam>
    /// <typeparam name="TDecorator">The type of the decorator. Must inherit from or implement <typeparamref name="TService"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="decoratorFactory">A factory function that creates the decorated service, with access to the service provider and the inner service instance.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection Decorate<TService, TDecorator>(
        this IServiceCollection services,
        Func<IServiceProvider, TDecorator, TService> decoratorFactory)
        where TDecorator : TService
    {
        services.TryDecorateDescriptors(
            typeof(TService),
            x => x.Decorate(decoratorFactory));

        return services;
    }

    #region Core

    private static bool TryDecorateDescriptors(
        this IServiceCollection services,
        Type serviceType,
        Func<ServiceDescriptor, ServiceDescriptor> decorator)
    {
        if (!services.TryGetDescriptors(serviceType, out var descriptors))
        {
            return false;
        }

        foreach (var descriptor in descriptors)
        {
            var index = services.IndexOf(descriptor);

            // To avoid reordering descriptors, in case a specific order is expected.
            services.Insert(index, decorator(descriptor));

            services.Remove(descriptor);
        }

        return true;
    }

    private static ServiceDescriptor Decorate<TService, TDecorator>(
        this ServiceDescriptor descriptor,
        Func<TDecorator, TService>? configure = null)
        where TDecorator : TService
    {
        var decoratorType = typeof(TDecorator);

        return ServiceDescriptor.Describe(
            descriptor.ServiceType,
            ImplementationFactory,
            descriptor.Lifetime);

        object ImplementationFactory(IServiceProvider provider)
        {
            var decoratedInstance =
                (TDecorator)ActivatorUtilities.CreateInstance(
                    provider,
                    decoratorType,
                    provider.GetInstance(descriptor));

            var instance = configure is null ? decoratedInstance : configure(decoratedInstance);
            return instance!;
        }
    }

    private static ServiceDescriptor Decorate<TService, TDecorator>(
        this ServiceDescriptor descriptor,
        Func<IServiceProvider, TDecorator, TService> decoratorFactory,
        Func<TService, TService>? configure = null)
        where TDecorator : TService
    {
        return ServiceDescriptor.Describe(
            descriptor.ServiceType,
            ImplementationFactory,
            descriptor.Lifetime);

        object ImplementationFactory(IServiceProvider provider)
        {
            var innerInstance = (TDecorator)provider.GetInstance(descriptor);
            var decoratedInstance = decoratorFactory(provider, innerInstance)!;
            var instance = configure is null ? decoratedInstance : configure(decoratedInstance);
            return instance!;
        }
    }

    private static bool TryGetDescriptors(
        this IServiceCollection services,
        Type serviceType,
        out ServiceDescriptor[] descriptors)
    {
        descriptors = services
            .Where(d =>
                d.ServiceType == serviceType
                || (serviceType.IsGenericTypeDefinition
                    && d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == serviceType))
            .ToArray();

        return descriptors.Length > 0;
    }

    private static object GetInstance(
        this IServiceProvider provider,
        ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is { } implementationInstance)
            return implementationInstance;

        if (descriptor.ImplementationFactory is { } implementationFactory)
            return implementationFactory(provider);

        if (descriptor.ImplementationType is { } implementationType)
            return ActivatorUtilities.CreateInstance(provider, implementationType);

        throw new InvalidOperationException("Descriptor does not have an implementation to create an instance from.");
    }

    #endregion
}