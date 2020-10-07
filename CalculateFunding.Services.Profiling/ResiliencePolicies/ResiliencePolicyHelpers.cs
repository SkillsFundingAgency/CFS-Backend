using System;
using System.Collections.Generic;
using System.Net.Http;
using CalculateFunding.Common.Extensions;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Bulkhead;
using Polly.Wrap;
using StackExchange.Redis;

namespace CalculateFunding.Services.Profiling.ResiliencePolicies
{
    public static class ResiliencePolicyHelpers
    {
        public static AsyncPolicy GenerateRedisPolicy(params IAsyncPolicy[] chainedPolicies)
        {
            AsyncPolicy redisServerExceptionRetry = Policy.Handle<RedisServerException>()
                .WaitAndRetryAsync(new[] {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(15)});

            AsyncPolicy circuitBreakerRedisServerException = Policy.Handle<RedisServerException>().CircuitBreakerAsync(1000, TimeSpan.FromMinutes(1));

            AsyncPolicy connectionExceptionRetry = Policy.Handle<RedisConnectionException>()
                .WaitAndRetryAsync(new[] {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)});

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

        public static AsyncPolicy GenerateRestRepositoryPolicy(params IAsyncPolicy[] chainedPolicies)
        {
            AsyncPolicy httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[] {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)});

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

        public static AsyncBulkheadPolicy GenerateTotalNetworkRequestsPolicy(PolicySettings settings)
        {
            return Policy.BulkheadAsync(settings.MaximumSimultaneousNetworkRequests);
        }
    }
}