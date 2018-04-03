using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner
{
    public class TestEngine
    {
        private readonly IGherkinExecutor _gherkinExecutor;
        private readonly CosmosRepository _cosmosRepository;

        public TestEngine(IGherkinExecutor gherkinExecutor, CosmosRepository cosmosRepository)
        {
            _gherkinExecutor = gherkinExecutor;
            _cosmosRepository = cosmosRepository;
        }
    }
}