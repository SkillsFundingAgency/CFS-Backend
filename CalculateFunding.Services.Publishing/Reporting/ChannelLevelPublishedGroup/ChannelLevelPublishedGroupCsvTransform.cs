using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System;
using CalculateFunding.Common.ApiClient.Profiling.Models;

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
            IEnumerable<PublishedFundingWithProvider> publishedFundingsWithProviders = documents.Cast<PublishedFundingWithProvider>();
            int resultsCount = publishedFundingsWithProviders.Sum(x => x.PublishedProviders.Any() ? x.PublishedProviders.Count() : 1);
            int publishedFungingsWithProvidersCount = publishedFundingsWithProviders.Count();

            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);
            int resultCount = 0;

            for (int publishedFundingIndex = 0; publishedFundingIndex < publishedFungingsWithProvidersCount; publishedFundingIndex++)
            {
                var publishedFunding = publishedFundingsWithProviders.ElementAt(publishedFundingIndex).PublishedFunding;
                var publishedProviders = publishedFundingsWithProviders.ElementAt(publishedFundingIndex).PublishedProviders;

                PublishedFundingVersion publishedFundingVersion = publishedFunding.Current;

                if (publishedProviders.Any())
                {
                    foreach (var publishedProvider in publishedProviders)
                    {
                        IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());
                        AddFundingAndProviderData(row, publishedFundingVersion, publishedProvider.Released);
                        resultCount++;

                        yield return (ExpandoObject)row;
                    }
                }
                else
                {
                    IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());
                    AddFundingAndProviderData(row, publishedFundingVersion);
                    resultCount++;

                    yield return (ExpandoObject)row;
                }
            }

            _expandoObjectsPool.Return(resultsBatch);
        }

        private void AddFundingAndProviderData(IDictionary<string, object> row, PublishedFundingVersion publishedFundingVersion, PublishedProviderVersion publishedProvider = null)
        {
            row["Funding ID"] = publishedFundingVersion.FundingId;
            row["Funding Major Version"] = publishedFundingVersion.MajorVersion;
            row["Group Channel Version"] = publishedFundingVersion.ChannelVersions.FirstOrDefault().value;
            row["Grouping Reason"] = publishedFundingVersion.GroupingReason.ToString();
            row["Grouping Code"] = publishedFundingVersion.OrganisationGroupTypeCode;
            row["Grouping Name"] = publishedFundingVersion.OrganisationGroupName;
            row["Grouping Type Identifier"] = publishedFundingVersion.OrganisationGroupTypeIdentifier;
            row["Grouping Identifier Value"] = publishedFundingVersion.OrganisationGroupIdentifierValue;
            row["Grouping Type Classification"] = publishedFundingVersion.OrganisationGroupTypeClassification;
            row["Grouping Total Funding"] = publishedFundingVersion.TotalFunding;
            row["Author"] = publishedFundingVersion.Author?.Name;
            row["Release Date"] = publishedFundingVersion.StatusChangedDate.ToString("s");
            row["Provider Count"] = publishedFundingVersion.ProviderFundings == null ? 0 : publishedFundingVersion.ProviderFundings.Count();

            if (publishedProvider == null)
            {
                row["Provider Funding ID"] = string.Empty;
                row["Provider Id"] = string.Empty;
                row["Provider Name"] = string.Empty;
                row["Provider Major Version"] = string.Empty;
                row["Provider Channel Version"] = string.Empty;
                row["Provider Total Funding"] = string.Empty;
                row["Provider UKPRN"] = string.Empty;
                row["Provider UPN"] = string.Empty;
                row["Provider UPIN"] = string.Empty;
                row["Provider LACode"] = string.Empty;
                row["Provider Status"] = string.Empty;
                row["Provider Successor"] = string.Empty;
                row["Provider Predecessors"] = string.Empty;
                row["Provider Variation Reasons"] = string.Empty;
            }
            else
            {
                row["Provider Funding ID"] = publishedProvider.FundingId;
                row["Provider Id"] = publishedProvider.ProviderId;
                row["Provider Name"] = publishedProvider.Provider?.Name;
                row["Provider Major Version"] = publishedProvider.MajorVersion;
                row["Provider Channel Version"] = publishedProvider.ChannelVersions.FirstOrDefault().value;
                row["Provider Total Funding"] = publishedProvider.TotalFunding;
                row["Provider UKPRN"] = publishedProvider.Provider?.UKPRN;
                row["Provider URN"] = publishedProvider.Provider?.URN;
                row["Provider UPIN"] = publishedProvider.Provider?.UPIN;
                row["Provider LACode"] = publishedProvider.Provider?.LACode;
                row["Provider Status"] = publishedProvider.Provider?.Status;
                row["Provider Successor"] = publishedProvider.Provider?.GetSuccessors().NullSafeJoinWith(";") ?? string.Empty;
                row["Provider Predecessors"] = publishedProvider.Predecessors.NullSafeJoinWith(";") ?? string.Empty;
                row["Provider Variation Reasons"] = publishedProvider.VariationReasons != null ? string.Join(';', publishedProvider.VariationReasons) : string.Empty;
            }
        }

    }
}
