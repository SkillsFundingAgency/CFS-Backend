using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.UnitTests.ApiClientHelpers.Jobs
{
    public class JobBuilder : TestEntityBuilder
    {
        private string _id;
        private string _definitionId;

        public JobBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public JobBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }

        public Job Build()
        {
            return new Job
            {
                Id = _id ?? NewRandomString(),
                JobDefinitionId = _definitionId,
            };
        }
    }
}
