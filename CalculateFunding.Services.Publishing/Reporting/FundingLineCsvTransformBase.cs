using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System.Globalization;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class FundingLineCsvTransformBase : IFundingLineCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(CsvBatchProcessBase.BatchSize, 4);

        public abstract bool IsForJobType(FundingLineCsvGeneratorJobType jobType);

        public virtual IEnumerable<ExpandoObject> Transform(
            IEnumerable<dynamic> documents, 
            FundingLineCsvGeneratorJobType jobType, 
            IEnumerable<ProfilePeriodPattern> profilePatterns = null,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            int resultsCount = documents.Count();

            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                PublishedProviderVersion publishedProviderVersion = GetPublishedProviderVersion(documents, resultCount, jobType);

                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                Provider provider = publishedProviderVersion.Provider;

                row["UKPRN"] = provider.UKPRN;
                row["URN"] = provider.URN;
                row["Establishment Number"] = provider.EstablishmentNumber;
                row["Provider Name"] = provider.Name;
                row["Provider Type"] = provider.ProviderType;
                row["Provider SubType"] = provider.ProviderSubType;
                row["LA Code"] = provider.LACode;
                row["LA Name"] = provider.Authority;
                row["Allocation Status"] = publishedProviderVersion.Status.ToString();
                row["Allocation Major Version"] = publishedProviderVersion.MajorVersion.ToString();
                row["Allocation Minor Version"] = publishedProviderVersion.MinorVersion.ToString();
                row["Allocation Author"] = publishedProviderVersion.Author?.Name;
                row["Allocation DateTime"] = publishedProviderVersion.Date.ToString("s");
                row["Is Indicative"] = publishedProviderVersion.IsIndicative.ToString();

                TransformFundingLine(row, publishedProviderVersion, profilePatterns, distinctFundingLineNames);
                TransformProviderDetails(row, publishedProviderVersion);

                yield return (ExpandoObject)row;
            }
            
            _expandoObjectsPool.Return(resultsBatch);
        }

        protected virtual void TransformFundingLine(IDictionary<string, object> row,
            PublishedProviderVersion publishedProviderVersion,
            IEnumerable<ProfilePeriodPattern> profilePatternColumnHeaders = null,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            foreach (string fundingLineName in distinctFundingLineNames.OrderBy(_ => _))
            {
                row[fundingLineName] = publishedProviderVersion.FundingLines?.SingleOrDefault(_ => _.Name == fundingLineName)?.Value?.ToString(CultureInfo.InvariantCulture);
            }
        }

        protected virtual void TransformProviderDetails(IDictionary<string, object> row, PublishedProviderVersion publishedProviderVersion)
        {
        }

        protected virtual PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount, FundingLineCsvGeneratorJobType jobType) => null;
    }
}