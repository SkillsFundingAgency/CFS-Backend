using CalculateFunding.Services.Core.Options;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Bulkhead;
using Polly.Wrap;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CalculateFunding.Services.Core.Helpers
{
    public static class ResiliencePolicyHelpers
    {
        public static Policy GenerateRedisPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateRedisPolicy(new[] { chainedPolicy });
        }


        public static Policy GenerateRedisPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            Policy redisServerExceptionRetry = Policy.Handle<RedisServerException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(15) });

            Policy circuitBreakerRedisServerException = Policy.Handle<RedisServerException>().CircuitBreakerAsync(1000, TimeSpan.FromMinutes(1));

            Policy connectionExceptionRetry = Policy.Handle<RedisConnectionException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) });

            Policy connectionExceptionCircuitBreaker = Policy.Handle<RedisConnectionException>().CircuitBreakerAsync(250, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                redisServerExceptionRetry,
                connectionExceptionRetry,
                circuitBreakerRedisServerException,
                connectionExceptionCircuitBreaker
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static Policy GenerateRestRepositoryPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateRestRepositoryPolicy(new[] { chainedPolicy });
        }

        public static Policy GenerateRestRepositoryPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            Policy httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

            Policy circuitBreakerRequestException = Policy.Handle<HttpRequestException>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                httpRequestExceptionPolicy,
                circuitBreakerRequestException
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static Policy GenerateMessagingPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateMessagingPolicy(new[] { chainedPolicy });
        }

        public static Policy GenerateMessagingPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            // Not sure of exactly the exception the messaging client throws. There is a RetryExponential.Default retry policy on the QueueClient too 
            Policy circuitBreakerRequestException = Policy.Handle<Exception>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                circuitBreakerRequestException
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            PolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static BulkheadPolicy GenerateTotalNetworkRequestsPolicy(PolicySettings settings)
        {
            return Policy.BulkheadAsync(settings.MaximumSimultaneousNetworkRequests);
        }
    }
}
