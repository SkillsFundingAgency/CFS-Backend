using Polly;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public class ResiliencePolicies : ICalculationsEtlResiliencePolicies
    {
        public AsyncPolicy SpecificationApiClient { get; set; }
        
        public AsyncPolicy CalculationsApiClient { get; set; }
        
        public AsyncPolicy DataSetsApiClient { get; set; }
    }
}