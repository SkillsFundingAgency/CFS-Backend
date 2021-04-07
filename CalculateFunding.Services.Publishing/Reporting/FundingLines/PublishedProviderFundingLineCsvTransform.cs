using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount, FundingLineCsvGeneratorJobType jobType)
        {
            if (jobType == FundingLineCsvGeneratorJobType.Released)
            {
                return documents.ElementAt(resultCount).Released;
            }

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
            row["Provider Successor"] = publishedProviderVersion.Provider.GetSuccessors().NullSafeJoinWith(";") ?? string.Empty;
            row["Provider Predecessors"] = publishedProviderVersion.Predecessors.NullSafeJoinWith(";") ?? string.Empty;;
            row["Provider Variation Reasons"] = publishedProviderVersion.VariationReasons != null ? string.Join(';', publishedProviderVersion.VariationReasons) : string.Empty;
        }
    }
}