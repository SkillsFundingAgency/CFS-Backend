using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount)
        {
            return documents.ElementAt(resultCount).Current;
        }

        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentState ||
                   jobType == FundingLineCsvGeneratorJobType.Released;
        }

        protected override void TransformProviderDetails(IDictionary<string, object> row, PublishedProviderVersion publishedProviderVersion)
        {
            row["Provider Status"] = publishedProviderVersion.Provider.Status;
            row["Provider Successor"] = publishedProviderVersion.Provider.Successor;
            row["Provider Predecessors"] = publishedProviderVersion.Predecessors != null ? string.Join(';', publishedProviderVersion.Predecessors) : string.Empty;
            row["Provider Variation Reasons"] = publishedProviderVersion.VariationReasons != null ? string.Join(';', publishedProviderVersion.VariationReasons) : string.Empty;
        }
    }
}