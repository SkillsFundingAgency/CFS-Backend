using System;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoJobParameters
    {
        public const string ForCorrelationIdPropertyName = "for-correlation-id";
        public const string IsHardDeletePropertyName = "is-hard-delete";

        public PublishedFundingUndoJobParameters()
        {
        }

        public PublishedFundingUndoJobParameters(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            
            IsHardDelete = Convert.ToBoolean(GetUserProperty(message, IsHardDeletePropertyName));
            ForCorrelationId = GetUserProperty(message, ForCorrelationIdPropertyName);
            JobId = GetUserProperty(message, "jobId");
        }

        public bool IsHardDelete { get; }
        
        public string ForCorrelationId { get; }
        
        public string JobId { get; }

        public static implicit operator PublishedFundingUndoJobParameters(Message message)
        {
            return new PublishedFundingUndoJobParameters(message);
        }

        public override string ToString() => $"JobId: {JobId}, ForCorrelationId: {ForCorrelationId}, IsHardDelete: {IsHardDelete}";

        protected static string GetUserProperty(Message message, string name) 
            => message.UserProperties.TryGetValue(name, out object property) ? property?.ToString() : 
               throw new ArgumentOutOfRangeException(name, $"Did not locate user property {name}");
    }
}