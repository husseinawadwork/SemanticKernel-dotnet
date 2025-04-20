using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace TelemetryConsoleQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Replace the connection string with your Application Insights connection string
            var connectionString = "%APPLICATION_INSIGHTS_CONNECTION_STRING%";

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService("TelemetryApplicationInsightsQuickstart");

            // Enable model diagnostics with sensitive data.
            AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

            using var traceProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Microsoft.SemanticKernel*")
                .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("Microsoft.SemanticKernel*")
                .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
                .Build();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Add OpenTelemetry as a logging provider
                builder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(resourceBuilder);
                    options.AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString);
                    // Format log messages. This is default to false.
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            IKernelBuilder builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton(loggerFactory);
            builder.AddAzureOpenAIChatCompletion(
                "%DEPLOYMENT_NAME%",
                "%AZURE_OPENAI_ENDPOINT%",
                "%AZURE_OPENAI_API_KEY%"
            );

            Kernel kernel = builder.Build();

            var answer = await kernel.InvokePromptAsync(
                "Why is the sky blue in one sentence?"
            );

            Console.WriteLine(answer);
        }
    }
}