# Decorator Pattern with `Decorate` Extension Methods

This guide provides comprehensive documentation for the `Decorate` extension methods, which enable the **Decorator pattern** for services registered in `Microsoft.Extensions.DependencyInjection`.

## Overview

The decorator pattern allows you to wrap registered services with additional functionality while preserving service lifetimes (Transient, Scoped, Singleton). This is useful for adding cross-cutting concerns like logging, caching, validation, or retry logic.

## Key Features

- ✅ **Simple API** - Intuitive extension methods for `IServiceCollection`
- ✅ **Lifetime Preservation** - Maintains the original service lifetime (Transient, Scoped, Singleton)
- ✅ **Multiple Decorators** - Chain multiple decorators on the same service
- ✅ **Factory Support** - Access `IServiceProvider` for advanced dependency resolution
- ✅ **Generic Support** - Works with open generic types
- ✅ **Zero Dependencies** - Only requires `Microsoft.Extensions.DependencyInjection`

## API Reference

### `Decorate<TService>()`

Decorates all registered services of type `TService` with the same type.

```csharp
public static IServiceCollection Decorate<TService>(
    this IServiceCollection services,
    Func<TService, TService>? configure = null)
```

**Parameters:**

- `services`: The service collection
- `configure`: Optional function to configure the decorated service

**Usage:**

```csharp
services.AddTransient<INotificationService, EmailNotificationService>();
services.Decorate<INotificationService>();
```

---

### `Decorate<TService, TDecorator>()`

Decorates all registered services of type `TService` with a decorator of type `TDecorator`.

```csharp
public static IServiceCollection Decorate<TService, TDecorator>(
    this IServiceCollection services,
    Func<TDecorator, TService>? configure = null)
    where TDecorator : TService
```

**Parameters:**

- `services`: The service collection
- `configure`: Optional function to configure the decorator

**Usage:**

```csharp
services.AddTransient<INotificationService, EmailNotificationService>();
services.Decorate<INotificationService, LoggingNotificationDecorator>();
```

---

### `Decorate<TService>()` with Factory

Decorates using a factory function that has access to the service provider.

```csharp
public static IServiceCollection Decorate<TService>(
    this IServiceCollection services,
    Func<IServiceProvider, TService, TService> decoratorFactory)
```

**Parameters:**

- `services`: The service collection
- `decoratorFactory`: Factory function receiving the service provider and the inner service

**Usage:**

```csharp
services.AddScoped<IOrderProcessor, OrderProcessor>();
services.Decorate<IOrderProcessor>((provider, inner) =>
{
    var logger = provider.GetRequiredService<ILogger<OrderProcessorDecorator>>();
    return new OrderProcessorDecorator(inner, logger);
});
```

---

### `Decorate<TService, TDecorator>()` with Factory

Decorates with a decorator type using a factory function.

```csharp
public static IServiceCollection Decorate<TService, TDecorator>(
    this IServiceCollection services,
    Func<IServiceProvider, TDecorator, TService> decoratorFactory)
    where TDecorator : TService
```

**Parameters:**

- `services`: The service collection
- `decoratorFactory`: Factory function receiving the service provider and the decorator instance

**Usage:**

```csharp
services.AddScoped<IPaymentService, PaymentService>();
services.Decorate<IPaymentService, RetryPaymentDecorator>((provider, decorator) =>
{
    // Additional configuration if needed
    return decorator;
});
```

## Common Scenarios

### Chaining Multiple Decorators

Decorators are applied in the order they are registered, with each decorator wrapping the previous one:

```csharp
services.AddTransient<ICalculator, Calculator>();

// Add multiple decorators - they wrap in order
services.Decorate<ICalculator, CachingCalculatorDecorator>();
services.Decorate<ICalculator, LoggingCalculatorDecorator>();
services.Decorate<ICalculator, ValidationCalculatorDecorator>();

// Result: ValidationCalculatorDecorator -> LoggingCalculatorDecorator -> CachingCalculatorDecorator -> Calculator
```

### Using the Service Provider

Access other services from the DI container in your decorator:

```csharp
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService>((provider, inner) =>
{
    var logger = provider.GetRequiredService<ILogger<OrderServiceDecorator>>();
    var metrics = provider.GetRequiredService<IMetricsCollector>();
    return new OrderServiceDecorator(inner, logger, metrics);
});
```

## Service Lifetime Behavior

The decorator pattern respects and preserves the original service's lifetime:

| Original Lifetime | Decorator Behavior                            |
| ----------------- | --------------------------------------------- |
| **Transient**     | New instance created each time                |
| **Scoped**        | Same instance within a scope                  |
| **Singleton**     | Same instance throughout application lifetime |

**Example:**

```csharp

**Example:**
```csharp
// Singleton example
services.AddSingleton<ICache, MemoryCache>();
services.Decorate<ICache, LoggingCacheDecorator>();

var provider = services.BuildServiceProvider();
var cache1 = provider.GetRequiredService<ICache>();
var cache2 = provider.GetRequiredService<ICache>();
// cache1 and cache2 are the same instance
```

## Real-World Examples

### Logging Decorator

Add logging to track method calls and exceptions:

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

Add caching to improve performance:

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

Add retry logic for resilient operations:

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

### Validation Decorator

Add input validation before calling the actual service:

```csharp
public class ValidationOrderServiceDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IValidator<OrderRequest> _validator;

    public ValidationOrderServiceDecorator(IOrderService inner, IValidator<OrderRequest> validator)
    {
        _inner = inner;
        _validator = validator;
    }

    public async Task<Order> PlaceOrderAsync(OrderRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await _inner.PlaceOrderAsync(request);
    }
}

// Registration
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService>((provider, inner) =>
{
    var validator = provider.GetRequiredService<IValidator<OrderRequest>>();
    return new ValidationOrderServiceDecorator(inner, validator);
});
```

### Performance Monitoring Decorator

Track execution time and metrics:

```csharp
public class MetricsOrderServiceDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IMetricsCollector _metrics;

    public MetricsOrderServiceDecorator(IOrderService inner, IMetricsCollector metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }

    public async Task<Order> PlaceOrderAsync(OrderRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _inner.PlaceOrderAsync(request);
            stopwatch.Stop();
            _metrics.RecordDuration("order.place", stopwatch.Elapsed);
            _metrics.Increment("order.placed.success");
            return result;
        }
        catch
        {
            stopwatch.Stop();
            _metrics.RecordDuration("order.place", stopwatch.Elapsed);
            _metrics.Increment("order.placed.failure");
            throw;
        }
    }
}

// Registration
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService>((provider, inner) =>
{
    var metrics = provider.GetRequiredService<IMetricsCollector>();
    return new MetricsOrderServiceDecorator(inner, metrics);
});
```

## Best Practices

### 1. Keep Decorators Focused

Each decorator should have a single responsibility. Don't mix logging, caching, and validation in one decorator.

```csharp
// Good: Separate decorators
services.Decorate<IOrderService, ValidationOrderServiceDecorator>();
services.Decorate<IOrderService, LoggingOrderServiceDecorator>();
services.Decorate<IOrderService, CachingOrderServiceDecorator>();

// Bad: One decorator doing everything
services.Decorate<IOrderService, MonolithicOrderServiceDecorator>();
```

### 2. Order Matters

The order in which you register decorators affects the execution order. Generally:

1. **Validation** - Check inputs first
2. **Logging** - Log the operation
3. **Caching** - Check cache before expensive operations
4. **Retry** - Apply retry logic for transient failures
5. **Metrics** - Track performance

```csharp
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService, ValidationOrderServiceDecorator>();
services.Decorate<IOrderService, LoggingOrderServiceDecorator>();
services.Decorate<IOrderService, CachingOrderServiceDecorator>();
services.Decorate<IOrderService, RetryOrderServiceDecorator>();
services.Decorate<IOrderService, MetricsOrderServiceDecorator>();
```

### 3. Use Factory Functions for Complex Dependencies

When your decorator needs multiple dependencies from the service provider, use the factory function overload:

```csharp
services.Decorate<IOrderService>((provider, inner) =>
{
    var logger = provider.GetRequiredService<ILogger<OrderServiceDecorator>>();
    var cache = provider.GetRequiredService<IMemoryCache>();
    var metrics = provider.GetRequiredService<IMetricsCollector>();
    return new OrderServiceDecorator(inner, logger, cache, metrics);
});
```

### 4. Preserve Async Context

When decorating async methods, ensure you properly await the inner service call to preserve async context:

```csharp
public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    // Good: Properly awaiting
    var result = await _inner.PlaceOrderAsync(request);
    return result;

    // Bad: Blocking async call
    // return _inner.PlaceOrderAsync(request).Result;
}
```

### 5. Consider Performance

Be aware of the performance impact of chaining many decorators, especially in hot paths. Profile your application to ensure decorators don't become a bottleneck.

## Troubleshooting

### Decorator Not Being Applied

**Problem:** The decorator doesn't seem to be wrapping the service.

**Solution:** Ensure you're calling `Decorate` **after** registering the service:

```csharp
// Correct order
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService, LoggingOrderServiceDecorator>();

// Wrong order - won't work
services.Decorate<IOrderService, LoggingOrderServiceDecorator>();
services.AddScoped<IOrderService, OrderService>();
```

### Multiple Service Registrations

**Problem:** When you have multiple implementations registered, `Decorate` affects all of them.

**Solution:** This is by design. If you need different decorators for different implementations, consider using named services or keyed services.

### Circular Dependencies

**Problem:** Decorator introduces circular dependencies.

**Solution:** Avoid injecting services that depend on the service being decorated. Use factory functions with care.

## Additional Resources

- [Main README](../README.md) - Project overview and quick start guide
- [Credits: Andrew Lock's Article](https://andrewlock.net/adding-decorated-classes-to-the-asp.net-core-di-container-using-scrutor/) - Original inspiration for this implementation
