using CommandLine;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public class ApiOptions : IApiOptions
    {
        public string CalculationsApiUri { get; set; }
        public string CalculationsApiKey { get; set; }
        public string SpecificationsApiKey { get; set; }
        public string SpecificationsApiUri { get; set; }
        public string DataSetsApiKey { get; set; }
        public string DataSetsApiUri { get; set; }
    }
}