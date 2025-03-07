// <copyright file="OpenTelemetryLoggingExtensionsTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OpenTelemetry.Logs.Tests;

public sealed class OpenTelemetryLoggingExtensionsTests
{
    [Fact]
    public void ServiceCollectionAddOpenTelemetryNoParametersTest()
    {
        bool optionsCallbackInvoked = false;

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(configure =>
        {
            configure.AddOpenTelemetry();
        });

        serviceCollection.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            optionsCallbackInvoked = true;
        });

        using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        Assert.NotNull(loggerFactory);

        Assert.True(optionsCallbackInvoked);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void ServiceCollectionAddOpenTelemetryConfigureActionTests(int numberOfBuilderRegistrations, int numberOfOptionsRegistrations)
    {
        int configureCallbackInvocations = 0;
        int optionsCallbackInvocations = 0;
        OpenTelemetryLoggerOptions? optionsInstance = null;

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(configure =>
        {
            for (int i = 0; i < numberOfBuilderRegistrations; i++)
            {
                configure.AddOpenTelemetry(ConfigureCallback);
            }
        });

        for (int i = 0; i < numberOfOptionsRegistrations; i++)
        {
            serviceCollection.Configure<OpenTelemetryLoggerOptions>(OptionsCallback);
        }

        using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        Assert.NotNull(loggerFactory);

        Assert.NotNull(optionsInstance);

        Assert.Equal(numberOfBuilderRegistrations, configureCallbackInvocations);
        Assert.Equal(numberOfOptionsRegistrations, optionsCallbackInvocations);

        void ConfigureCallback(OpenTelemetryLoggerOptions options)
        {
            if (optionsInstance == null)
            {
                optionsInstance = options;
            }
            else
            {
                Assert.Equal(optionsInstance, options);
            }

            configureCallbackInvocations++;
        }

        void OptionsCallback(OpenTelemetryLoggerOptions options)
        {
            if (optionsInstance == null)
            {
                optionsInstance = options;
            }
            else
            {
                Assert.Equal(optionsInstance, options);
            }

            optionsCallbackInvocations++;
        }
    }

    [Fact]
    public void ServiceCollectionAddOpenTelemetryWithProviderTest()
    {
        var provider = new WrappedOpenTelemetryLoggerProvider();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(configure =>
        {
            configure.AddOpenTelemetry(provider);
        });

        using (ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider())
        {
            ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            Assert.NotNull(loggerFactory);

            loggerFactory!.Dispose();

            // Note: Provider disposal does not actually happen until serviceProvider is disposed
            Assert.False(provider.Disposed);
        }

        Assert.True(provider.Disposed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServiceCollectionAddOpenTelemetryWithProviderAndDisposeSpecifiedTests(bool dispose)
    {
        var provider = new WrappedOpenTelemetryLoggerProvider();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(configure =>
        {
            configure.AddOpenTelemetry(provider, disposeProvider: dispose);
        });

        using (ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider())
        {
            ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            Assert.NotNull(loggerFactory);

            loggerFactory!.Dispose();

            // Note: Provider disposal does not actually happen until serviceProvider is disposed
            Assert.False(provider.Disposed);
        }

        Assert.Equal(dispose, provider.Disposed);

        provider.Dispose();

        Assert.True(provider.Disposed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoggerFactoryCreateAddOpenTelemetryWithProviderAndDisposeSpecifiedTests(bool dispose)
    {
        var provider = new WrappedOpenTelemetryLoggerProvider();

        using (var factory = LoggerFactory.Create(configure =>
        {
            configure.AddOpenTelemetry(provider, disposeProvider: dispose);
        }))
        {
            Assert.False(provider.Disposed);
        }

        Assert.Equal(dispose, provider.Disposed);

        provider.Dispose();

        Assert.True(provider.Disposed);
    }

    private sealed class WrappedOpenTelemetryLoggerProvider : OpenTelemetryLoggerProvider
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            this.Disposed = true;

            base.Dispose(disposing);
        }
    }
}
