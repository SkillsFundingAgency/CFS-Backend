using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class FundingLineCsvTransformBase : IFundingLineCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(FundingLineCsvGenerator.BatchSize, 4);

        public abstract bool IsForJobType(FundingLineCsvGeneratorJobType jobType);

        public IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents)
        {
            int resultsCount = documents.Count();
            
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                PublishedProviderVersion publishedProviderVersion = GetPublishedProviderVersion(documents, resultCount);
                
                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                Provider provider = publishedProviderVersion.Provider;

                row["UKPRN"] = provider.UKPRN;
                row["URN"] = provider.URN;
                row["Establishment Number"] = provider.EstablishmentNumber;
                row["Provider Name"] = provider.Name;
                row["Provider Type"] = provider.ProviderType;
                row["Provider SubType"] = provider.ProviderSubType;
                row["LA Code"] = provider.LACode;
                row["LA Name"] = provider.LocalAuthorityName;
                row["Allocation Status"] = publishedProviderVersion.Status.ToString();
                row["Allocation Major Version"] = publishedProviderVersion.MajorVersion.ToString();
                row["Allocation Minor Version"] = publishedProviderVersion.MinorVersion.ToString();
                row["Allocation Author"] = publishedProviderVersion.Author?.Name;
                row["Allocation DateTime"] = publishedProviderVersion.Date.ToString("s");

                foreach (FundingLine fundingLine in publishedProviderVersion.FundingLines.OrderBy(_ => _.Name))
                {
                    row[fundingLine.Name] = fundingLine.Value?.ToString();
                }

                yield return (ExpandoObject) row;
            }
            
            _expandoObjectsPool.Return(resultsBatch);
        }

        protected abstract PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount);
    }
}