using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class PublishedFundingUndoJobParametersBuilder : TestEntityBuilder
    {
        private bool? _isHardDelete;
        private string _forCorrelationId;

        public PublishedFundingUndoJobParametersBuilder WithIsHardDelete(bool isHardDelete)
        {
            _isHardDelete = isHardDelete;

            return this;
        }

        public PublishedFundingUndoJobParametersBuilder WithForCorrelationId(string forCorrelationId)
        {
            _forCorrelationId = forCorrelationId;

            return this;
        }

        public PublishedFundingUndoJobParameters Build()
        {
            return new PublishedFundingUndoJobParameters(new Message
            {
                UserProperties =
                {
                    {PublishedFundingUndoJobParameters.ForCorrelationIdPropertyName, _forCorrelationId ?? NewRandomString()},
                    {PublishedFundingUndoJobParameters.IsHardDeletePropertyName, _isHardDelete.GetValueOrDefault(NewRandomFlag())},
                    {"jobId", NewRandomString()},
                }
            });
        }
    }
}