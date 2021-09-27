using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class FundingLineGroupingCsvTransformBase : IFundingLineCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(CsvBatchProcessBase.BatchSize, 4);

        public abstract bool IsForJobType(FundingLineCsvGeneratorJobType jobType);

        public IEnumerable<ExpandoObject> Transform(
            IEnumerable<dynamic> documents, 
            FundingLineCsvGeneratorJobType jobType, 
            IEnumerable<ProfilePeriodPattern> profilePatterns = null,
            IEnumerable<string> distinctFundingLineNames = null)
        { 
            int resultsCount = documents.Count();
            
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                PublishedFundingVersion publishedFundingVersion = GetPublishedFundingVersion(documents, resultCount, jobType);

                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                row["Grouping Reason"] = publishedFundingVersion.GroupingReason.ToString();
                row["Grouping Code"] = publishedFundingVersion.OrganisationGroupTypeCode;
                row["Grouping Name"] = publishedFundingVersion.OrganisationGroupName;
                row["Allocation Status"] = publishedFundingVersion.Status.ToString();
                row["Allocation Major Version"] = publishedFundingVersion.MajorVersion.ToString();
                row["Allocation Author"] = publishedFundingVersion.Author?.Name;
                row["Allocation DateTime"] = publishedFundingVersion.Date.ToString("s");
                row["Provider Count"] = publishedFundingVersion.ProviderFundings?.Count() ?? 0;
                
                TransformFundingLine(row, publishedFundingVersion, distinctFundingLineNames);

                yield return (ExpandoObject) row;
            }
            
            _expandoObjectsPool.Return(resultsBatch);
        }
        
        protected virtual void TransformFundingLine(
            IDictionary<string, object> row, 
            PublishedFundingVersion publishedProviderVersion,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            foreach (string fundingLineName in distinctFundingLineNames.OrderBy(_ => _))
            {
                row[fundingLineName] = publishedProviderVersion.FundingLines.SingleOrDefault(_ => _.Name == fundingLineName)?.Value?.ToString(CultureInfo.InvariantCulture);
            }
        }

        protected abstract PublishedFundingVersion GetPublishedFundingVersion(IEnumerable<dynamic> documents,
            int resultCount,
            FundingLineCsvGeneratorJobType jobType);
    }
}