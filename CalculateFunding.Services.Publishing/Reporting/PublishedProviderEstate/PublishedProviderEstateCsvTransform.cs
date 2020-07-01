using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate
{
    public class PublishedProviderEstateCsvTransform : IPublishedProviderCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool
            = ArrayPool<ExpandoObject>.Create(PublishedProviderEstateCsvGenerator.BatchSize, 4);

        public bool IsForJobDefinition(string jobDefinitionName)
        {
            return jobDefinitionName == JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;
        }

        public IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents)
        {
            int resultsCount = documents.Count();
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);
            
            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                IGrouping<string, PublishedProviderVersion> publishedProviderVersionGroup = documents
                    .ElementAt(resultCount);

                if(!publishedProviderVersionGroup.Any(v =>v.Status == PublishedProviderStatus.Updated))
                {
                    continue;
                }

                PublishedProviderVersion updatedPublishedProviderVersion = null;
                PublishedProviderVersion releasedPublishedProviderVersion = null;

                foreach (PublishedProviderVersion item in publishedProviderVersionGroup.OrderByDescending(x => x.Version))
                {
                    switch (item.Status)
                    {
                        case PublishedProviderStatus.Released:
                            releasedPublishedProviderVersion = item;
                            break;
                        case PublishedProviderStatus.Updated:
                            updatedPublishedProviderVersion = item;
                            break;
                        default:
                            continue;
                    }

                    if(updatedPublishedProviderVersion == null || releasedPublishedProviderVersion == null)
                    {
                        continue;
                    }

                    IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                    row["UKPRN"] = updatedPublishedProviderVersion.Provider.UKPRN;
                    row["Provider Has Successor"] = (!string.IsNullOrEmpty(updatedPublishedProviderVersion.Provider.Successor) && string.IsNullOrEmpty(releasedPublishedProviderVersion.Provider.Successor)).ToString();
                    row["Provider Data Changed"] = updatedPublishedProviderVersion.VariationReasons != null ? updatedPublishedProviderVersion.VariationReasons.Any().ToString() : false.ToString();
                    row["Provider Has Closed"] = (updatedPublishedProviderVersion.Provider.DateClosed != null && releasedPublishedProviderVersion.Provider.DateClosed == null).ToString();
                    row["Provider Has Opened"] = (updatedPublishedProviderVersion.Provider.DateOpened != null && releasedPublishedProviderVersion.Provider.DateOpened == null).ToString();
                    row["Variation Reason"] = updatedPublishedProviderVersion.VariationReasons != null ? string.Join('|', updatedPublishedProviderVersion.VariationReasons) : string.Empty;
                    
                    row["Current URN"] = updatedPublishedProviderVersion.Provider.URN;
                    row["Current Provider Name"] = updatedPublishedProviderVersion.Provider.Name;
                    row["Current Provider Type"] = updatedPublishedProviderVersion.Provider.ProviderType;
                    row["Current Provider Subtype"] = updatedPublishedProviderVersion.Provider.ProviderSubType;
                    row["Current LA Code"] = updatedPublishedProviderVersion.Provider.LACode;
                    row["Current LA Name"] = updatedPublishedProviderVersion.Provider.Authority;
                    row["Current Open Date"] = updatedPublishedProviderVersion.Provider.DateOpened?.ToString("s");
                    row["Current Open Reason"] = updatedPublishedProviderVersion.Provider.ReasonEstablishmentOpened;
                    row["Current Close Date"] = updatedPublishedProviderVersion.Provider.DateClosed?.ToString("s");
                    row["Current Close Reason"] = updatedPublishedProviderVersion.Provider.ReasonEstablishmentClosed;
                    row["Current Successor Provider ID"] = updatedPublishedProviderVersion.Provider.Successor;
                    row["Current Trust Code"] = updatedPublishedProviderVersion.Provider.TrustCode;
                    row["Current Trust Name"] = updatedPublishedProviderVersion.Provider.TrustName;
                    
                    row["Previous URN"] = releasedPublishedProviderVersion.Provider.URN;
                    row["Previous Provider Name"] = releasedPublishedProviderVersion.Provider.Name;
                    row["Previous Provider Type"] = releasedPublishedProviderVersion.Provider.ProviderType;
                    row["Previous Provider Subtype"] = releasedPublishedProviderVersion.Provider.ProviderSubType;
                    row["Previous LA Code"] = releasedPublishedProviderVersion.Provider.LACode;
                    row["Previous LA Name"] = releasedPublishedProviderVersion.Provider.Authority;
                    row["Previous Close Date"] = releasedPublishedProviderVersion.Provider.DateClosed?.ToString("s");
                    row["Previous Close Reason"] = releasedPublishedProviderVersion.Provider.ReasonEstablishmentClosed;
                    row["Previous Trust Code"] = releasedPublishedProviderVersion.Provider.TrustCode;
                    row["Previous Trust Name"] = releasedPublishedProviderVersion.Provider.TrustName;

                    updatedPublishedProviderVersion = releasedPublishedProviderVersion = null;

                    yield return (ExpandoObject)row;
                }
            }

            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}
