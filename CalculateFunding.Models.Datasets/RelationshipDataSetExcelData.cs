using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class RelationshipDataSetExcelData
    {
        public RelationshipDataSetExcelData(string ukprn)
        {
            Ukprn = ukprn;
            FundingLines = new Dictionary<string, decimal?>();
            Calculations = new Dictionary<string, decimal?>();
        }

        public string Ukprn { get; }

        public IDictionary<string, decimal?> FundingLines { get; set; }

        public IDictionary<string, decimal?> Calculations { get; set; }
    }
}
