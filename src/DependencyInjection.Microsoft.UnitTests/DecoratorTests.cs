// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DependencyInjection.Microsoft.UnitTests;

public class DecoratorTests
{
    [Fact]
    public void Transient_Registration_IsDecorated_And_ReturnsDistinctInstances()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICalculator, Calculator1>();
        services.Decorate<ICalculator, Calculator2>();

        var provider = services.BuildServiceProvider();

        var calc1 = provider.GetRequiredService<ICalculator>();
        var calc2 = provider.GetRequiredService<ICalculator>();

        Assert.Equal(6, calc1.Add(2, 3));
        Assert.Equal(6, calc2.Add(2, 3));
        Assert.NotSame(calc1, calc2);
    }

    [Fact]
    public void Scoped_Registration_IsDecorated_And_PreservesScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalculator, Calculator1>();
        services.Decorate<ICalculator, Calculator2>();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var calcA = scope.ServiceProvider.GetRequiredService<ICalculator>();
            var calcB = scope.ServiceProvider.GetRequiredService<ICalculator>();

            Assert.Equal(6, calcA.Add(2, 3));
            Assert.Same(calcA, calcB);
        }

        using (var scope2 = provider.CreateScope())
        {
            var calcC = scope2.ServiceProvider.GetRequiredService<ICalculator>();
            Assert.Equal(6, calcC.Add(2, 3));
        }
    }

    [Fact]
    public void Singleton_Registration_IsDecorated_And_SharedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICalculator, Calculator1>();
        services.Decorate<ICalculator, Calculator2>();

        var provider = services.BuildServiceProvider();

        var calcRoot = provider.GetRequiredService<ICalculator>();
        Assert.Equal(6, calcRoot.Add(2, 3));

        using var scope = provider.CreateScope();
        var calcScoped = scope.ServiceProvider.GetRequiredService<ICalculator>();
        Assert.Same(calcRoot, calcScoped);
        Assert.Equal(6, calcScoped.Add(2, 3));
    }

    [Fact]
    public void Factory_Registration_IsDecorated()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalculator>(sp => new Calculator1());
        services.Decorate<ICalculator, Calculator2>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var calc = scope.ServiceProvider.GetRequiredService<ICalculator>();
        Assert.Equal(6, calc.Add(2, 3));
    }

    [Fact]
    public void Instance_Registration_IsDecorated()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICalculator>(new Calculator1());
        services.Decorate<ICalculator, Calculator2>();

        var provider = services.BuildServiceProvider();
        var calc = provider.GetRequiredService<ICalculator>();
        Assert.Equal(6, calc.Add(2, 3));
    }

    [Fact]
    public void Factory_Overload_AllowsCustomDecoratorLogic()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalculator, Calculator1>();

        // Use factory overload to create decorator with custom logic
        services.Decorate<ICalculator, Calculator1>((_, inner) =>
            new Calculator2(inner));

        var provider = services.BuildServiceProvider();
        var calc = provider.GetRequiredService<ICalculator>();
        Assert.Equal(6, calc.Add(2, 3));
    }

    [Fact]
    public void Factory_Overload_CanAccessServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalculator, Calculator1>();
        services.Configure<CalculatorConfig>(c => c.Multiplier = 10);

        services.Decorate<ICalculator>((provider, inner) =>
        {
            var config = provider.GetRequiredService<IOptions<CalculatorConfig>>().Value;
            return new CustomCalculatorDecorator(inner, config.Multiplier);
        });

        var provider = services.BuildServiceProvider();
        var calc = provider.GetRequiredService<ICalculator>();
        Assert.Equal(50, calc.Add(2, 3)); // (2 + 3) * 10
    }

    [Fact]
    public void Factory_Overload_PreservesLifetime()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICalculator, Calculator1>();

        services.Decorate<ICalculator>((_, inner) =>
            new Calculator2(inner));

        var provider = services.BuildServiceProvider();

        var calc1 = provider.GetRequiredService<ICalculator>();
        var calc2 = provider.GetRequiredService<ICalculator>();

        Assert.Same(calc1, calc2); // Singleton behavior preserved
    }

    [Fact]
    public void Factory_Overload_CanChainMultipleDecorators()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalculator, Calculator1>();

        // Chain multiple decorators
        services.Decorate<ICalculator>((_, inner) =>
            new Calculator2(inner)); // adds 1
        services.Decorate<ICalculator>((_, inner) =>
            new Calculator2(inner)); // adds another 1

        var provider = services.BuildServiceProvider();
        var calc = provider.GetRequiredService<ICalculator>();
        Assert.Equal(7, calc.Add(2, 3)); // 2 + 3 + 1 + 1
    }

    #region Helpers

    private interface ICalculator
    {
        int Add(int x, int y);
    }

    private class Calculator1 : ICalculator
    {
        public int Add(int x, int y) => x + y;
    }

    private class Calculator2(ICalculator calc) : ICalculator
    {
        public int Add(int x, int y) => calc.Add(x, y) + 1;
    }

    private class CustomCalculatorDecorator(ICalculator inner, int multiplier) : ICalculator
    {
        public int Add(int x, int y) => inner.Add(x, y) * multiplier;
    }

    private class CalculatorConfig
    {
        public int Multiplier { get; set; }
    }

    #endregion
}