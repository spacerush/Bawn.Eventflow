using System;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.ApplicationInsights;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Outputs;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bawn.EventFlowClient
{
    public class DefaultFilter : IFilter
    {
        private IHealthReporter HealthReporter;
        private string MachineName;
        public DefaultFilter(string ServerName, IHealthReporter HealthReporter)
        {
            MachineName = ServerName;
            this.HealthReporter = HealthReporter;
        }

        FilterResult IFilter.Evaluate(EventData eventData)
        {
            // By default, my ApplicationInsightsIngester accepts new telemetry posted to an endpoint that ends with /api/Collect.so i'm not interested in 
            // If I were to record telemetry about this dependency there would be an continuous
            // loop of telemetry recorded about telemetry recorded about telemetry recorded about telemetry... ..you get the idea.
            if (eventData.Payload["TelemetryType"].ToString() == "dependency" && eventData.Payload["Name"].ToString().StartsWith("POST") && eventData.Payload["Name"].ToString().EndsWith("/api/Collect"))
            {
                return FilterResult.DiscardEvent;
            }
            else
            {
                eventData.AddPayloadProperty("ServerName", MachineName, HealthReporter, "CustomFilter");
                return FilterResult.KeepEvent;
            }
        }
    }
}
