using Polly;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public interface ICalculationsEtlResiliencePolicies
    {
        AsyncPolicy SpecificationApiClient { get; set; }
        AsyncPolicy CalculationsApiClient { get; set; }
        AsyncPolicy DataSetsApiClient { get; set; }
    }
}