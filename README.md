# DependencyInjection.Microsoft

> Complementary extensions for `Microsoft.Extensions.DependencyInjection` that enable advanced service composition and extensibility.

[![NuGet](https://img.shields.io/nuget/v/geoder101.Microsoft.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/geoder101.Microsoft.Extensions.DependencyInjection/)

## Overview

This library complements `Microsoft.Extensions.DependencyInjection` with capabilities not provided out of the box. It focuses on service composition and extensibility so you can layer cross-cutting concerns (logging, caching, validation, retries, metrics) onto existing registrations without changing their implementations.

## Installation

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

## Use Cases

The decorator pattern is ideal for adding cross-cutting concerns to your services:

- **Logging** - Track method calls, parameters, and results
- **Caching** - Add response caching to expensive operations
- **Validation** - Validate inputs before calling the actual service
- **Retry Logic** - Add resilience with automatic retries
- **Performance Monitoring** - Track execution time and metrics
- **Authorization** - Add security checks before method execution

## Documentation

📚 **[Decorator Pattern Guide](docs/Decorator.md)** - Comprehensive documentation including:

- Complete API reference
- Real-world examples (logging, caching, retry, validation, metrics)
- Service lifetime behavior
- Best practices and common patterns
- Troubleshooting guide

## Project Structure

```text
DependencyInjection.Microsoft/
├── src/
│   ├── DependencyInjection.Microsoft/          # Main library
│   └── DependencyInjection.Microsoft.UnitTests/ # Unit tests
├── docs/
│   └── Decorator.md                             # Detailed documentation
└── README.md                                    # This file
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

### Co-authored with Artificial Intelligence

This repository is part of an ongoing exploration into human-AI co-creation.  
The code, comments, and structure emerged through dialogue between human intent and LLM reasoning — reviewed, refined, and grounded in human understanding.
