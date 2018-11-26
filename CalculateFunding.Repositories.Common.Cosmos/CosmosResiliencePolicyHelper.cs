using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents;
using Polly;
using Polly.Wrap;

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
            Policy documentClientExceptionRetry = Policy.Handle<DocumentClientException>(e => (int)e.StatusCode != 429)
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) });

            Policy requestRateTooLargeExceptionRetry = Policy.Handle<DocumentClientException>(e=> (int)e.StatusCode == 429)
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120) });


            Policy circuitBreaker = Policy.Handle<DocumentClientException>().CircuitBreakerAsync(1000, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                documentClientExceptionRetry,
                requestRateTooLargeExceptionRetry,
                circuitBreaker,
            };

            if (chainedPolicies != null && chainedPolicies.Any())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static Policy GenerateNonAsyncCosmosPolicy(Policy chainedPolicy)
        {
            return GenerateNonAsyncCosmosPolicy(new[] { chainedPolicy });
        }

        public static Policy GenerateNonAsyncCosmosPolicy(Policy[] chainedPolicies = null)
        {
            Policy documentClientExceptionRetry = Policy.Handle<DocumentClientException>(e => (int)e.StatusCode != 429)
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) });

            Policy requestRateTooLargeExceptionRetry = Policy.Handle<DocumentClientException>(e => (int)e.StatusCode == 429)
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120) });


            Policy circuitBreaker = Policy.Handle<DocumentClientException>().CircuitBreaker(1000, TimeSpan.FromMinutes(1));

            List<Policy> policies = new List<Policy>(8)
            { 
                documentClientExceptionRetry,
                requestRateTooLargeExceptionRetry,
                circuitBreaker,
            };

            if (chainedPolicies != null && chainedPolicies.Any())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.Wrap(policies.ToArray());

            return policyWrap;
        }
    }
}
