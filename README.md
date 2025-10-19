# Microsoft.Extensions.DependencyInjection.Decorator

Extension methods for `Microsoft.Extensions.DependencyInjection` that enable the **Decorator pattern**. Easily wrap registered services with decorators while preserving service lifetimes (Transient, Scoped, Singleton).

## Features

- ✅ **Simple API** - Intuitive extension methods for `IServiceCollection`
- ✅ **Lifetime Preservation** - Maintains the original service lifetime (Transient, Scoped, Singleton)
- ✅ **Multiple Decorators** - Chain multiple decorators on the same service
- ✅ **Factory Support** - Access `IServiceProvider` for advanced scenarios
- ✅ **Generic Support** - Works with open generic types
- ✅ **Zero Dependencies** - Only requires `Microsoft.Extensions.DependencyInjection`

## Installation

[![NuGet](https://img.shields.io/nuget/v/geoder101.Microsoft.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/geoder101.Microsoft.Extensions.DependencyInjection/)

```bash
dotnet add package geoder101.Microsoft.Extensions.DependencyInjection
```

## Quick Start

### Basic Decorator

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your service
services.AddTransient<INotificationService, EmailNotificationService>();

// Decorate it with logging
services.Decorate<INotificationService, LoggingNotificationDecorator>();

var provider = services.BuildServiceProvider();
var notificationService = provider.GetRequiredService<INotificationService>();
// Returns LoggingNotificationDecorator wrapping EmailNotificationService
```

### Decorator with Factory

```csharp
services.AddScoped<IOrderProcessor, OrderProcessor>();

// Use factory to access IServiceProvider
services.Decorate<IOrderProcessor>((provider, inner) =>
{
    var logger = provider.GetRequiredService<ILogger<OrderProcessorDecorator>>();
    return new OrderProcessorDecorator(inner, logger);
});
```

### Chaining Multiple Decorators

```csharp
services.AddTransient<ICalculator, Calculator>();

// Add multiple decorators - they wrap in order
services.Decorate<ICalculator, CachingCalculatorDecorator>();
services.Decorate<ICalculator, LoggingCalculatorDecorator>();
services.Decorate<ICalculator, ValidationCalculatorDecorator>();

// Result: ValidationCalculatorDecorator -> LoggingCalculatorDecorator -> CachingCalculatorDecorator -> Calculator
```

## API Reference

### `Decorate<TService>()`

Decorates all registered services of type `TService` with the same type.

```csharp
public static IServiceCollection Decorate<TService>(
    this IServiceCollection services,
    Func<TService, TService>? configure = null)
```

### `Decorate<TService, TDecorator>()`

Decorates all registered services of type `TService` with a decorator of type `TDecorator`.

```csharp
public static IServiceCollection Decorate<TService, TDecorator>(
    this IServiceCollection services,
    Func<TDecorator, TService>? configure = null)
    where TDecorator : TService
```

### `Decorate<TService>()` with Factory

Decorates using a factory function that has access to the service provider.

```csharp
public static IServiceCollection Decorate<TService>(
    this IServiceCollection services,
    Func<IServiceProvider, TService, TService> decoratorFactory)
```

### `Decorate<TService, TDecorator>()` with Factory

Decorates with a decorator type using a factory function.

```csharp
public static IServiceCollection Decorate<TService, TDecorator>(
    this IServiceCollection services,
    Func<IServiceProvider, TDecorator, TService> decoratorFactory)
    where TDecorator : TService
```

## Real-World Examples

### Logging Decorator

```csharp
public interface IOrderService
{
    Task<Order> PlaceOrderAsync(OrderRequest request);
}

public class OrderService : IOrderService
{
    public async Task<Order> PlaceOrderAsync(OrderRequest request)
    {
        // Implementation
    }
}

public class LoggingOrderServiceDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderServiceDecorator> _logger;

    public LoggingOrderServiceDecorator(IOrderService inner, ILogger<LoggingOrderServiceDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Order> PlaceOrderAsync(OrderRequest request)
    {
        _logger.LogInformation("Placing order for customer {CustomerId}", request.CustomerId);
        try
        {
            var result = await _inner.PlaceOrderAsync(request);
            _logger.LogInformation("Order {OrderId} placed successfully", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }
}

// Registration
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService>((provider, inner) =>
{
    var logger = provider.GetRequiredService<ILogger<LoggingOrderServiceDecorator>>();
    return new LoggingOrderServiceDecorator(inner, logger);
});
```

### Caching Decorator

```csharp
public class CachingProductServiceDecorator : IProductService
{
    private readonly IProductService _inner;
    private readonly IMemoryCache _cache;

    public CachingProductServiceDecorator(IProductService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Product> GetProductAsync(int id)
    {
        var cacheKey = $"product_{id}";

        if (_cache.TryGetValue(cacheKey, out Product? cachedProduct))
            return cachedProduct!;

        var product = await _inner.GetProductAsync(id);
        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(5));
        return product;
    }
}

// Registration
services.AddScoped<IProductService, ProductService>();
services.Decorate<IProductService>((provider, inner) =>
{
    var cache = provider.GetRequiredService<IMemoryCache>();
    return new CachingProductServiceDecorator(inner, cache);
});
```

### Retry Decorator with Polly

```csharp
public class RetryPaymentServiceDecorator : IPaymentService
{
    private readonly IPaymentService _inner;
    private readonly IAsyncPolicy _retryPolicy;

    public RetryPaymentServiceDecorator(IPaymentService inner, IAsyncPolicy retryPolicy)
    {
        _inner = inner;
        _retryPolicy = retryPolicy;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        return await _retryPolicy.ExecuteAsync(() => _inner.ProcessPaymentAsync(request));
    }
}

// Registration
services.AddScoped<IPaymentService, PaymentService>();
services.Decorate<IPaymentService>((provider, inner) =>
{
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    return new RetryPaymentServiceDecorator(inner, retryPolicy);
});
```

## Service Lifetime Behavior

The decorator pattern respects and preserves the original service's lifetime:

| Original Lifetime | Decorator Behavior                            |
| ----------------- | --------------------------------------------- |
| **Transient**     | New instance created each time                |
| **Scoped**        | Same instance within a scope                  |
| **Singleton**     | Same instance throughout application lifetime |

```csharp
// Singleton example
services.AddSingleton<ICache, MemoryCache>();
services.Decorate<ICache, LoggingCacheDecorator>();

var provider = services.BuildServiceProvider();
var cache1 = provider.GetRequiredService<ICache>();
var cache2 = provider.GetRequiredService<ICache>();
// cache1 and cache2 are the same instance
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Credits

- Extended from Andrew Lock's article on [Adding decorated classes to the ASP.NET Core DI container](https://andrewlock.net/adding-decorated-classes-to-the-asp.net-core-di-container-using-scrutor/)
