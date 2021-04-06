using CalculateFunding.Services.Graph.Interfaces;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Graph
{
    public class GraphResiliencePolicies : IGraphResiliencePolicies
    {
        public AsyncPolicy CacheProviderPolicy { get; set; }
    }
}
