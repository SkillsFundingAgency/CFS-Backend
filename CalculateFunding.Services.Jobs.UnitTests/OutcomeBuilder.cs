using CalculateFunding.Models.Jobs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Jobs.Services
{
    public class OutcomeBuilder : TestEntityBuilder
    {
        private string _jobDefinitionId;
        private string _description;
        private OutcomeType? _type;
        private bool? _isSuccessful;

        public OutcomeBuilder WithJobDefinitionId(string jobDefinitionId)
        {
            _jobDefinitionId = jobDefinitionId;

            return this;
        }

        public OutcomeBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public OutcomeBuilder WithType(OutcomeType type)
        {
            _type = type;

            return this;
        }

        public OutcomeBuilder WithIsSuccessful(bool isSuccessful)
        {
            _isSuccessful = isSuccessful;

            return this;
        }
        
        public Outcome Build()
        {
            return new Outcome
            {
                JobDefinitionId    = _jobDefinitionId ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                IsSuccessful = _isSuccessful.GetValueOrDefault(NewRandomFlag()),
                Type = _type.GetValueOrDefault(NewRandomEnum<OutcomeType>())
            };
        }
    }
}