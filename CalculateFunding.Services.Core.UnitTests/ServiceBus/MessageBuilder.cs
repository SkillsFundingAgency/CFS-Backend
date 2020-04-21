using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;
using System;
using System.Reflection;

namespace CalculateFunding.Services.Core.UnitTests
{
    public class MessageBuilder : TestEntityBuilder
    {
        private readonly Message _message = new Message();
        
        public MessageBuilder WithUserProperty(string key, string value)
        {
            _message.UserProperties.Add(key, value);

            return this;
        }

        public MessageBuilder WithoutUserProperty(string key)
        {
            _message.UserProperties.Remove(key);

            return this;
        }

        public MessageBuilder WithBody(byte[] body)
        {
            _message.Body = body;

            return this;
        }

        public MessageBuilder WithLockToken(Guid lockToken)
        {
            Message.SystemPropertiesCollection systemProperties = _message.SystemProperties;
            Type type = systemProperties.GetType();
            type.GetMethod("set_LockTokenGuid", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(systemProperties, new object[] { lockToken });
            type.GetMethod("set_SequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(systemProperties, new object[] { 0 });

            return this;
        }

        public Message Build()
        {
            return _message;
        }
    }
}