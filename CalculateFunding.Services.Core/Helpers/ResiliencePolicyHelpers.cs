using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Bulkhead;
using Polly.Wrap;
using StackExchange.Redis;

namespace CalculateFunding.Services.Core.Helpers
{
    public static class ResiliencePolicyHelpers
    {
        public static AsyncPolicy GenerateRedisPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateRedisPolicy(new[] { chainedPolicy });
        }


        public static AsyncPolicy GenerateRedisPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            AsyncPolicy redisServerExceptionRetry = Policy.Handle<RedisServerException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(15) });

            AsyncPolicy circuitBreakerRedisServerException = Policy.Handle<RedisServerException>().CircuitBreakerAsync(1000, TimeSpan.FromMinutes(1));

            AsyncPolicy connectionExceptionRetry = Policy.Handle<RedisConnectionException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) });

            AsyncPolicy connectionExceptionCircuitBreaker = Policy.Handle<RedisConnectionException>().CircuitBreakerAsync(250, TimeSpan.FromMinutes(1));

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

            AsyncPolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static AsyncPolicy GenerateRestRepositoryPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateRestRepositoryPolicy(new[] { chainedPolicy });
        }

        public static AsyncPolicy GenerateRestRepositoryPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            AsyncPolicy httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

            AsyncPolicy circuitBreakerRequestException = Policy.Handle<HttpRequestException>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                httpRequestExceptionPolicy,
                circuitBreakerRequestException
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            AsyncPolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static AsyncPolicy GenerateMessagingPolicy(IAsyncPolicy chainedPolicy)
        {
            return GenerateMessagingPolicy(new[] { chainedPolicy });
        }

        public static AsyncPolicy GenerateMessagingPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            // Not sure of exactly the exception the messaging client throws. There is a RetryExponential.Default retry policy on the QueueClient too 
            AsyncPolicy circuitBreakerRequestException = Policy.Handle<Exception>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                circuitBreakerRequestException
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            AsyncPolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static AsyncPolicy CosmosManagementPolicy(IAsyncPolicy[] chainedPolicies = null)
        {
            AsyncPolicy cosmosExceptionRetry = Policy.Handle<CosmosException>()
               .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20) });

            AsyncPolicy circuitBreakerRequestException = Policy.Handle<Exception>().CircuitBreakerAsync(100, TimeSpan.FromMinutes(1));

            List<IAsyncPolicy> policies = new List<IAsyncPolicy>(8)
            {
                cosmosExceptionRetry,
                circuitBreakerRequestException
            };

            if (!chainedPolicies.IsNullOrEmpty())
            {
                policies.AddRange(chainedPolicies);
            }

            AsyncPolicyWrap policyWrap = Policy.WrapAsync(policies.ToArray());

            return policyWrap;
        }

        public static AsyncBulkheadPolicy GenerateTotalNetworkRequestsPolicy(PolicySettings settings)
        {
            return Policy.BulkheadAsync(settings.MaximumSimultaneousNetworkRequests);
        }

        public static BulkheadPolicy GenerateTotalNetworkRequestsNonAsyncPolicy(PolicySettings settings)
        {
            return Policy.Bulkhead(settings.MaximumSimultaneousNetworkRequests);
        }
    }
}
