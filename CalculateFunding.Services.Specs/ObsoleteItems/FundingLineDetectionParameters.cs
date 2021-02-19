using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Specs.ObsoleteItems
{
    public class FundingLineDetectionParameters
    {
        public const string SpecificationIdKey = "specification-id";
        public const string FundingStreamIdKey = "funding-stream-id";
        public const string FundingPeriodIdKey = "funding-period-id";
        public const string PreviousTemplateVersionIdKey = "previous-template-version-id";
        public const string TemplateVersionIdKey = "template-version-id";
            
        public FundingLineDetectionParameters(Message message)
        {
            SpecificationId = MessageProperty(message, SpecificationIdKey);
            FundingStreamId = MessageProperty(message, FundingStreamIdKey);
            FundingPeriodId = MessageProperty(message, FundingPeriodIdKey);
            PreviousTemplateVersionId = MessageProperty(message, PreviousTemplateVersionIdKey);
            TemplateVersionId = MessageProperty(message, TemplateVersionIdKey);
        }
            
        public string SpecificationId { get; }
            
        public string FundingStreamId { get; }
            
        public string FundingPeriodId { get; }
            
        public string PreviousTemplateVersionId { get; }
            
        public string TemplateVersionId { get; }

        public static implicit operator FundingLineDetectionParameters(Message message)
        {
            return new FundingLineDetectionParameters(message);
        }

        private string MessageProperty(Message message,
            string key)
            => message.GetUserProperty<string>(key)
               ?? throw new NonRetriableException(
                   $"Expected FundingLineDetectionParameters to contain a value for {key}");
    }
}