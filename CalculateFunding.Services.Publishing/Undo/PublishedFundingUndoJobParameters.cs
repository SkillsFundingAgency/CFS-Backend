using System;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoJobParameters
    {
        public const string ForCorrelationIdPropertyName = "for-correlation-id";
        public const string ForSpecificationIdPropertyName = "specification-id";
        public const string IsHardDeletePropertyName = "is-hard-delete";
        public const string ForApiVersionPropertyName = "api-version";
        public const string ForChannelCodesPropertyName = "channel-codes";
        public const string APIVersion_3 = "v3";
        public const string APIVersion_4 = "v4";

        public PublishedFundingUndoJobParameters()
        {
        }

        public PublishedFundingUndoJobParameters(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            
            IsHardDelete = Convert.ToBoolean(GetUserProperty(message, IsHardDeletePropertyName));
            ForCorrelationId = GetUserProperty(message, ForCorrelationIdPropertyName);
            ForSpecificationId = GetUserProperty(message, ForSpecificationIdPropertyName);
            ForApiVersion = GetUserProperty(message, ForApiVersionPropertyName);
            ForChannelCodes = GetUserProperty(message, ForChannelCodesPropertyName);
            JobId = GetUserProperty(message, "jobId");
        }

        public bool IsHardDelete { get; }
        
        public string ForCorrelationId { get; }

        public string ForSpecificationId { get; }

        public string ForApiVersion { get; }

        public string ForChannelCodes { get; }

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