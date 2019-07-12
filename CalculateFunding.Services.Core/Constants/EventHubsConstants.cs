using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Constants
{
    public static class EventHubsConstants
    {
        public const string ConnectionStringConfigurationKey = "CosmosDBEventHubSettings:ConnectionString";

        public static class Hubs
        {
            public const string CosmosDbDiagnostics = "cosmosdb-diagnostics";
        }
    }
}
