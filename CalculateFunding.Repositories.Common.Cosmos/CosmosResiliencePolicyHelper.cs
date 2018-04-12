using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Documents;
using Polly;
using Polly.Wrap;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Cosmos
{
    public static class CosmosResiliencePolicyHelper
    {
        public static Policy GenerateCosmosPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateCosmosPolicy(new[] { chainedPolicy });
        }

        public static Policy GenerateCosmosPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            Policy documentClientExceptionRetry = Policy.Handle<DocumentClientException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) });

            Policy circuitBreaker = Policy.Handle<DocumentClientException>().CircuitBreakerAsync(1000, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                documentClientExceptionRetry,
                circuitBreaker,
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
