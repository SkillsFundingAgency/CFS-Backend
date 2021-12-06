using CalculateFunding.Common.TemplateMetadata.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class UniqueTemplateContents
    {
        public IEnumerable<FundingLine> FundingLines { get; set; }
        public IEnumerable<Calculation> Calculations { get; set; }
    }
}
