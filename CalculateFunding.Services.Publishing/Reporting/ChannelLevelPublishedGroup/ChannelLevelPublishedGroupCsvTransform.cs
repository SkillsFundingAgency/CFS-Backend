using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class ChannelLevelPublishedGroupCsvTransform : IFundingLineCsvTransform
    {
        private readonly IReleaseCandidateService _releaseCandidateService;

        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool
            = ArrayPool<ExpandoObject>.Create(ChannelLevelPublishedGroupCsvGenerator.BatchSize, 4);

        public ChannelLevelPublishedGroupCsvTransform(IReleaseCandidateService releaseCandidateService)
        {
            _releaseCandidateService = releaseCandidateService;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.ChannelLevelPublishedGroup;
        }

        public IEnumerable<ExpandoObject> Transform(
            IEnumerable<dynamic> documents,
            FundingLineCsvGeneratorJobType jobType,
            IEnumerable<ProfilePeriodPattern> profilePatterns = null,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            IEnumerable<PublishedFundingChannelVersion> publishedFundingsWithProviders = documents.Cast<PublishedFundingChannelVersion>();
            
            int recCount = publishedFundingsWithProviders.Count();

            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(recCount);
            int resultCount = 0;
            for (int publishedFundingIndex = 0; publishedFundingIndex < recCount; publishedFundingIndex++)
            {
                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());
                AddFundingAndProviderData(row, publishedFundingsWithProviders.ElementAt(publishedFundingIndex));
                resultCount++;

                yield return (ExpandoObject)row;
            }

            _expandoObjectsPool.Return(resultsBatch);
        }

        private void AddFundingAndProviderData(IDictionary<string, object> row, PublishedFundingChannelVersion publishedFundingVersion)
        {
            row["Funding ID"] = publishedFundingVersion.FundingId;
            row["Funding Major Version"] = publishedFundingVersion.FundingMajorVersion;
            row["Group Channel Version"] = publishedFundingVersion.GroupChannelVersion;
            row["Grouping Reason"] = publishedFundingVersion.GroupingReason;
            row["Grouping Code"] = publishedFundingVersion.GroupingCode;
            row["Grouping Name"] = publishedFundingVersion.GroupingName;
            row["Grouping Type Identifier"] = publishedFundingVersion.GroupingTypeIdentifier;
            row["Grouping Identifier Value"] = publishedFundingVersion.GroupingIdentifierValue;
            row["Grouping Type Classification"] = publishedFundingVersion.GroupingTypeClassification;
            row["Grouping Total Funding"] = publishedFundingVersion.GroupingTotalFunding;
            row["Author"] = publishedFundingVersion.Author;
            row["Release Date"] = publishedFundingVersion.ReleaseDate.ToString("s");
            row["Provider Count"] = publishedFundingVersion.ProviderCount;


            row["Provider Funding ID"] = publishedFundingVersion.ProviderFundingId;
            row["Provider Id"] = publishedFundingVersion.ProviderId;
            row["Provider Name"] = publishedFundingVersion.ProviderName;
            row["Provider Major Version"] = publishedFundingVersion.ProviderMajorVersion;
            row["Provider Channel Version"] = publishedFundingVersion.ProviderChannelVersion;
            row["Provider Total Funding"] = publishedFundingVersion.ProviderTotalFunding;

            row["Provider UKPRN"] = publishedFundingVersion.ProviderUKPRN;
            row["Provider URN"] = publishedFundingVersion.ProviderURN;
            row["Provider UPIN"] = publishedFundingVersion.ProviderUPIN;
            row["Provider LACode"] = publishedFundingVersion.ProviderLACode;
            row["Provider Status"] = publishedFundingVersion.ProviderStatus;
            row["Provider Successor"] = publishedFundingVersion.ProviderSuccessor;
            row["Provider Predecessors"] = publishedFundingVersion.ProviderPredecessors;
            row["Provider Variation Reasons"] = publishedFundingVersion.ProviderVariationReasons;

        }
    }
}
