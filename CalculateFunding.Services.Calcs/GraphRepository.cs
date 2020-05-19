using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Common.ApiClient.Models;
using Polly;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using GraphEntity = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;

namespace CalculateFunding.Services.Calcs
{
    public class GraphRepository : IGraphRepository
    {
        private readonly IGraphApiClient _graphApiClient;
        private readonly AsyncPolicy _resilience;
        private readonly ICalculationsFeatureFlag _calculationsFeatureFlag;
        private bool? _graphEnabled;

        public async Task<bool> GraphEnabled()
        {
            _graphEnabled ??= await _calculationsFeatureFlag.IsGraphEnabled();

            return _graphEnabled.Value;
        }

        public GraphRepository(IGraphApiClient graphApiClient,
            ICalcsResiliencePolicies resiliencePolicies,
            ICalculationsFeatureFlag calculationsFeatureFlag)
        {
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.GraphApiClientPolicy, nameof(resiliencePolicies.GraphApiClientPolicy));
            Guard.ArgumentNotNull(calculationsFeatureFlag, nameof(calculationsFeatureFlag));

            _graphApiClient = graphApiClient;
            _resilience = resiliencePolicies.GraphApiClientPolicy;
            _calculationsFeatureFlag = calculationsFeatureFlag;
        }

        public async Task<IEnumerable<CalculationEntity>> GetCircularDependencies(string specificationId)
        {
            if (!(await GraphEnabled()))
            {
                return null;
            }

            ApiResponse<IEnumerable<GraphEntity>> entities = await _resilience.ExecuteAsync(() => _graphApiClient.GetCircularDependencies(specificationId));

            return entities?.Content?.Select(_ =>
                new CalculationEntity
                {
                    Node = _.Node.AsJson().AsPoco<GraphCalculation>(),
                    Relationships = _.Relationships
                });
        }
    }
}
