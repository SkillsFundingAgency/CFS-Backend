using CommandLine;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public interface IApiOptions
    {
        string CalculationsApiUri { get; set; }
        
        string CalculationsApiKey { get; set; }
        
        string SpecificationsApiKey { get; set; }
        
        string SpecificationsApiUri { get; set; }
        
        string DataSetsApiKey { get; set; }
        
        string DataSetsApiUri { get; set; }
    }
}