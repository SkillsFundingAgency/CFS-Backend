
using CalculateFunding.Models.Jobs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Jobs.Services
{
    public class JobLogUpdateModelBuilder : TestEntityBuilder
    {
        private bool? _completedSuccessfully;
        private string _outcome;
        private OutcomeType? _outcomeType;

        public JobLogUpdateModelBuilder WithCompletedSuccessfully(bool completedSuccessfully)
        {
            _completedSuccessfully = completedSuccessfully;

            return this;
        }

        public JobLogUpdateModelBuilder WithOutcome(string outcome)
        {
            _outcome = outcome;

            return this;
        }

        public JobLogUpdateModelBuilder WithOutcomeType(OutcomeType outcomeType)
        {
            _outcomeType = outcomeType;

            return this;
        }
        
        public JobLogUpdateModel Build()
        {
            return new JobLogUpdateModel
            {
                CompletedSuccessfully = _completedSuccessfully,
                Outcome = _outcome,
                OutcomeType = _outcomeType
            };
        }
    }
}