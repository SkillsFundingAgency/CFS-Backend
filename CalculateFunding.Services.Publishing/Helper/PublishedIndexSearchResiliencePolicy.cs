using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Rest.Azure;
using Polly;
using Polly.Wrap;

namespace CalculateFunding.Services.Publishing.Helper
{
    public static class PublishedIndexSearchResiliencePolicy
    {
        public static AsyncPolicy GeneratePublishedIndexSearch(IAsyncPolicy chainedPolicy)
        {
            return GeneratePublishedIndexSearch(new[] { chainedPolicy });
        }

        public static AsyncPolicy GeneratePublishedIndexSearch(IAsyncPolicy[] chainedPolicies = null)
        {      
            var waitAndRetryPolicy =
                Policy.Handle<CloudException>(c => c.Message == "Another indexer invocation is currently in progress; concurrent invocations not allowed.")
                 .WaitAndRetryAsync(15, i=>  TimeSpan.FromSeconds(20) );

            var fault = Policy.Handle<CloudException>()
                .FallbackAsync((cancellationToken) => {                    
                    return Task.CompletedTask; },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);                        
                    });

            AsyncPolicyWrap policyWrap = fault.WrapAsync(waitAndRetryPolicy);
            return policyWrap;
        }
    }
}
