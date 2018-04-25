using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search;
using Microsoft.Rest.Azure;
using Polly;
using Polly.Wrap;

namespace CalculateFunding.Repositories.Common.Search
{
    public static class SearchResiliencePolicyHelper
    {
        public static Policy GenerateSearchPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateSearchPolicy(new[] { chainedPolicy });
        }

        public static Policy GenerateSearchPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            Policy cloudExceptionRetry = Policy.Handle<CloudException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

            Policy indexBatchExceptionRetry = Policy.Handle<IndexBatchException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) });

            // IndexBatchException inherits CloudException, so additional circuit breaker not needed
            Policy circuitBreaker = Policy.Handle<CloudException>().CircuitBreakerAsync(500, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                cloudExceptionRetry,
                circuitBreaker,
                indexBatchExceptionRetry
            };

            if (chainedPolicies != null && chainedPolicies.Any())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }
    }
}
