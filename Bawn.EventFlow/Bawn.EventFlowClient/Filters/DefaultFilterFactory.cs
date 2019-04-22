using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bawn.EventFlowClient
{
    public class DefaultFilterFactory : IPipelineItemFactory<DefaultFilter>
    {
        public DefaultFilter CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            DefaultFilter filter = new DefaultFilter(System.Environment.MachineName, healthReporter);
            return filter;
        }
    }
}
