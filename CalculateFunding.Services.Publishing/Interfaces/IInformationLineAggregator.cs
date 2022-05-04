using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IInformationLineAggregator
    {
        public ProfilePeriod[] Sum(TemplateFundingLine fundingLine, IDictionary<uint, FundingLine> fundingLines);
    }
}
