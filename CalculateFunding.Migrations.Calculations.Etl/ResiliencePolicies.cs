using Polly;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public class ResiliencePolicies : ICalculationsEtlResiliencePolicies
    {
        public Policy SpecificationApiClient { get; set; }
        
        public Policy CalculationsApiClient { get; set; }
        
        public Policy DataSetsApiClient { get; set; }
    }
}