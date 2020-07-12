using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedGroupsFundingLineCsvTransform : IFundingLineCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool
            = ArrayPool<ExpandoObject>.Create(CsvBatchProcessBase.BatchSize, 4);

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.PublishedGroups;
        }

        public IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents)
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
                        AddFundingAndProviderData(row, publishedFundingVersion, publishedProvider);
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

        private void AddFundingAndProviderData(IDictionary<string, object> row, PublishedFundingVersion publishedFundingVersion, PublishedProvider publishedProvider = null)
        {
            row["Funding ID"] = publishedFundingVersion.FundingId;
            row["Funding Major Version"] = publishedFundingVersion.MajorVersion;
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

            if(publishedProvider == null)
            {
                row["Provider Funding ID"] = string.Empty;
                row["Provider Id"] = string.Empty;
                row["Provider Name"] = string.Empty;
                row["Provider Major Version"] = string.Empty;
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
                row["Provider Funding ID"] = publishedProvider.Released.FundingId;
                row["Provider Id"] = publishedProvider.Released.ProviderId;
                row["Provider Name"] = publishedProvider.Released.Provider?.Name;
                row["Provider Major Version"] = publishedProvider.Released.MajorVersion;
                row["Provider Total Funding"] = publishedProvider.Released.TotalFunding;
                row["Provider UKPRN"] = publishedProvider.Released.Provider?.UKPRN;
                row["Provider URN"] = publishedProvider.Released.Provider?.URN;
                row["Provider UPIN"] = publishedProvider.Released.Provider?.UPIN;
                row["Provider LACode"] = publishedProvider.Released.Provider?.LACode;
                row["Provider Status"] = publishedProvider.Released.Provider?.Status;
                row["Provider Successor"] = publishedProvider.Released.Provider?.Successor;
                row["Provider Predecessors"] = publishedProvider.Released.Predecessors != null ? string.Join(';', publishedProvider.Released.Predecessors) : string.Empty;
                row["Provider Variation Reasons"] = publishedProvider.Released.VariationReasons != null ? string.Join(';', publishedProvider.Released.VariationReasons) : string.Empty;
            }
        }
    }
}
