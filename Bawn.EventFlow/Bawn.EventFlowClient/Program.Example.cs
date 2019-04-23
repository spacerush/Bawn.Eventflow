using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.ApplicationInsights;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Outputs;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore;
using System.Collections.Generic;
using System.IO;
using Bawn.EventFlowClient;

namespace ReportWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var eventFlow = CreateEventFlow(args))
            {
                BuildWebHost(args, eventFlow).Run();
            }
        }

        public static IWebHost BuildWebHost(string[] args, DiagnosticPipeline eventFlow) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddSingleton<ITelemetryProcessorFactory>(sp => new EventFlowTelemetryProcessorFactory(eventFlow)))
            .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

        private static DiagnosticPipeline CreateEventFlow(string[] args)
        {
            // Create configuration instance to access configuration information for EventFlow pipeline
            // To learn about common configuration sources take a peek at https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs (CreateDefaultBuilder method). 
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            var devEnvironmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                    devEnvironmentVariable.ToLower() == "development";

            if (isDevelopment)
            {
                configBuilder.AddUserSecrets<Program>();
            }
            else
            {
                configBuilder.SetBasePath(Directory.GetCurrentDirectory());
                configBuilder.AddJsonFile("appsettings.json", false, false);
            }

            if (args != null)
            {
                configBuilder.AddCommandLine(args);
            }
            var config = configBuilder.Build();

            // SEE https://github.com/Azure/diagnostics-eventflow#http
            var httpConfig = config.GetSection("HttpEventSinkConfig");
            var filterConfig = config.GetSection("FilterConfig");
            var pipelineConfig = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 5000, // Specifies the maximum time that events are held in a batch before the batch gets pushed through the pipeline to filters and outputs. The batch is pushed down when it reaches the maxEventBatchSize, or its oldest event has been in the batch for more than maxBatchDelayMsec milliseconds.
                PipelineCompletionTimeoutMsec = 10000, // Specifies the timeout to wait for the pipeline to shutdown and clean up. The shutdown process starts when the DiagnosePipeline object is disposed, which usually happens on application exit.
                MaxEventBatchSize = 50, // Specifies the maximum number of events to be batched before the batch gets pushed through the pipeline to filters and outputs. The batch is pushed down when it reaches the maxEventBatchSize, or its oldest event has been in the batch for more than maxBatchDelayMsec milliseconds.
                PipelineBufferSize = 1000 // Specifies how many events the pipeline can buffer if the events cannot flow through the pipeline fast enough. This buffer protects loss of data in cases where there is a sudden burst of data.
            };
            var healthReporter = new CsvHealthReporter(new CsvHealthReporterConfiguration());
            var aiInput = new ApplicationInsightsInputFactory().CreateItem(null, healthReporter);
            var aiFilters = new DefaultFilterFactory().CreateItem(filterConfig, healthReporter);
            var inputs = new IObservable<EventData>[] { aiInput };
            var filters = new IFilter[] { aiFilters };

            var sinks = new EventSink[]
            {
                new EventSink(new StdOutput(healthReporter), null),
                new EventSink(new HttpOutput(httpConfig, healthReporter), filters) // again, see https://github.com/Azure/diagnostics-eventflow#http
            };

            return new DiagnosticPipeline(healthReporter, inputs, filters, sinks, pipelineConfig, disposeDependencies: true);
        }
    }
}
