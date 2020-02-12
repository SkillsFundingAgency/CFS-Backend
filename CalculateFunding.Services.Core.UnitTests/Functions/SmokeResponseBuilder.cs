using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Functions
{
    public class SmokeResponseBuilder : TestEntityBuilder
    {
        private string _listener;
        private string _invocationId;
        private string _serviceName;

        public SmokeResponseBuilder WithListener(string listener)
        {
            _listener = listener;

            return this;
        }

        public SmokeResponseBuilder WithInvocationId(string invocationId)
        {
            _invocationId = invocationId;

            return this;
        }

        public SmokeResponseBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;

            return this;
        }

        public SmokeResponse Build()
        {
            return new SmokeResponse
            { 
                InvocationId = _invocationId ?? new RandomString(),
                Listener = _listener ?? new RandomString(),
                Service = _serviceName ?? new RandomString()
            };
        }
    }
}
