using Polly;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public interface ICalculationsEtlResiliencePolicies
    {
        Policy SpecificationApiClient { get; set; }
        Policy CalculationsApiClient { get; set; }
        Policy DataSetsApiClient { get; set; }
    }
}