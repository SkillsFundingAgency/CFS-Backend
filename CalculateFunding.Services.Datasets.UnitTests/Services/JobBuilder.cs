using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ApiJobBuilder : TestEntityBuilder
    {
        private string _id;
        private string _definitionId;

        public ApiJobBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        public ApiJobBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }
        

        public Job Build()
        {
            return new Job
            {
                Id = _id ?? NewRandomString(),
                JobDefinitionId = _definitionId ?? NewRandomString(),
            };
        }
    }
}