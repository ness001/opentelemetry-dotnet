# Prometheus Exporter AspNetCore for OpenTelemetry .NET

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetry.Exporter.Prometheus.AspNetCore.svg)](https://www.nuget.org/packages/OpenTelemetry.Exporter.Prometheus.AspNetCore)
[![NuGet](https://img.shields.io/nuget/dt/OpenTelemetry.Exporter.Prometheus.AspNetCore.svg)](https://www.nuget.org/packages/OpenTelemetry.Exporter.Prometheus.AspNetCore)

An [OpenTelemetry Prometheus exporter](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/metrics/sdk_exporters/prometheus.md)
for configuring an ASP.NET Core application with an endpoint for Prometheus
to scrape.

## Prerequisite

* [Get Prometheus](https://prometheus.io/docs/introduction/first_steps/)

## Steps to enable OpenTelemetry.Exporter.Prometheus.AspNetCore

### Step 1: Install Package

Install

```shell
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

### Step 2: Configure OpenTelemetry MeterProvider

* When using OpenTelemetry.Extensions.Hosting package on .NET Core 3.1+:

    ```csharp
    services.AddOpenTelemetryMetrics(builder =>
    {
        builder.AddPrometheusExporter();
    });
    ```

* Or configure directly:

    Call the `AddPrometheusExporter` `MeterProviderBuilder` extension to
    register the Prometheus exporter.

    ```csharp
    var meterProvider = Sdk.CreateMeterProviderBuilder()
        .AddPrometheusExporter()
        .Build();
    builder.Services.AddSingleton(meterProvider);
    ```

### Step 3: Configure Prometheus Scraping Endpoint

* Register Prometheus scraping middleware using the
  `UseOpenTelemetryPrometheusScrapingEndpoint` extension:

    ```csharp
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
    ```

    Overloads of the `UseOpenTelemetryPrometheusScrapingEndpoint` extension are
    provided to change the path or for more advanced configuration a predicate
    function can be used:

    ```csharp
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint(
            context => context.Request.Path == "/internal/metrics"
                && context.Connection.LocalPort == 5067);
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
    ```

## Configuration

The `PrometheusExporter` can be configured using the `PrometheusExporterOptions`
properties.

### ScrapeEndpointPath

Defines the path for the Prometheus scrape endpoint for the middleware
registered by
`UseOpenTelemetryPrometheusScrapingEndpoint`. Default value: `"/metrics"`.

### ScrapeResponseCacheDurationMilliseconds

Configures scrape endpoint response caching. Multiple scrape requests within the
cache duration time period will receive the same previously generated response.
The default value is `10000` (10 seconds). Set to `0` to disable response
caching.

## Troubleshooting

This component uses an
[EventSource](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventsource)
with the name "OpenTelemetry-Exporter-Prometheus" for its internal logging.
Please refer to [SDK
troubleshooting](../OpenTelemetry/README.md#troubleshooting) for instructions on
seeing these internal logs.

## References

* [OpenTelemetry Project](https://opentelemetry.io/)
* [Prometheus](https://prometheus.io)
