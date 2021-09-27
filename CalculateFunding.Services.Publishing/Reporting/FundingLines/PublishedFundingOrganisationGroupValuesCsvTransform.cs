using CalculateFunding.Models.Publishing;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingOrganisationGroupValuesCsvTransform : FundingLineCsvTransformBase
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool = ArrayPool<ExpandoObject>
            .Create(CsvBatchProcessBase.BatchSize, 4);

        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return  jobType == FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues ||
                    jobType == FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues;
        }

        public override IEnumerable<ExpandoObject> Transform(
            IEnumerable<dynamic> documents, 
            FundingLineCsvGeneratorJobType jobType, 
            IEnumerable<ProfilePeriodPattern> profilePatterns = null,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            int resultsCount = documents.Count();
            IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings = documents.Cast<PublishedFundingOrganisationGrouping>();

            int totalItemCount = organisationGroupings.Sum(x => x.PublishedFundingVersions.Count() + 1);
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(totalItemCount);

            int itemCount = 0;

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                PublishedFundingOrganisationGrouping organisationGrouping = organisationGroupings.ElementAt(resultCount);
                PublishedFundingVersion[] publishedFundingVersions = organisationGrouping.PublishedFundingVersions.ToArray();

                foreach (PublishedFundingVersion publishedFundingVersion in publishedFundingVersions)
                {
                    if(publishedFundingVersion == null)
                    {
                        continue;
                    }

                    IDictionary<string, object> row = resultsBatch[itemCount] ?? (resultsBatch[itemCount] = new ExpandoObject());

                    row["Grouping Reason"] = publishedFundingVersion.GroupingReason.ToString();
                    row["Grouping Code"] = publishedFundingVersion.OrganisationGroupTypeCode;
                    row["Grouping Identifier Value"] = publishedFundingVersion.OrganisationGroupIdentifierValue;
                    row["Grouping Name"] = publishedFundingVersion.OrganisationGroupName;
                    row["Allocation Status"] = publishedFundingVersion.Status.ToString();
                    row["Allocation Major Version"] = publishedFundingVersion.MajorVersion.ToString();
                    row["Allocation Author"] = publishedFundingVersion.Author?.Name;
                    row["Allocation DateTime"] = publishedFundingVersion.Date.ToString("s");

                    row["Provider Count"] = organisationGrouping.OrganisationGroupResult.Providers.Count();

                    foreach (string fundingLineName in distinctFundingLineNames.OrderBy(_ => _))
                    {
                        row[fundingLineName] = publishedFundingVersion.FundingLines.SingleOrDefault(_ => _.Name == fundingLineName)?.Value?.ToString(CultureInfo.InvariantCulture);
                    }
                    
                    itemCount++;
                    yield return (ExpandoObject)row;
                }
            }

            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}
