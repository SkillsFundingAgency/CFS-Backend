using System.Collections.Generic;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class ScalingConfigurationUpdateModel
    {
        public CosmosCollectionType RepositoryType { get; set; }

        public int BaseRequestUnits { get; set; }

        public int MaxRequestUnits { get; set; }

        public IEnumerable<CosmosDbScalingJobConfig> JobRequestUnitConfigs { get; set; }
    }
}
