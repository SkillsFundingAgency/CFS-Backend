using System;
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

                    Provider updatedProvider = updatedPublishedProviderVersion.Provider;
                    
                    row["UKPRN"] = updatedProvider.UKPRN;
                    row["Provider Has Successor"] = updatedProvider.GetSuccessors().Any().ToString();
                    row["Provider Data Changed"] = updatedPublishedProviderVersion.VariationReasons != null ? updatedPublishedProviderVersion.VariationReasons.Any().ToString() : false.ToString();
                    
                    Provider releasedProvider = releasedPublishedProviderVersion.Provider;
                    
                    row["Provider Has Closed"] = (updatedProvider.DateClosed != null && releasedProvider.DateClosed == null).ToString();
                    row["Provider Has Opened"] = (updatedProvider.DateOpened != null && releasedProvider.DateOpened == null).ToString();
                    row["Variation Reason"] = updatedPublishedProviderVersion.VariationReasons != null ? string.Join('|', updatedPublishedProviderVersion.VariationReasons) : string.Empty;
                    
                    row["Current URN"] = updatedProvider.URN;
                    row["Current Provider Name"] = updatedProvider.Name;
                    row["Current Provider Type"] = updatedProvider.ProviderType;
                    row["Current Provider Subtype"] = updatedProvider.ProviderSubType;
                    row["Current LA Code"] = updatedProvider.LACode;
                    row["Current LA Name"] = updatedProvider.Authority;
                    row["Current Open Date"] = updatedProvider.DateOpened?.ToString("s");
                    row["Current Open Reason"] = updatedProvider.ReasonEstablishmentOpened;
                    row["Current Close Date"] = updatedProvider.DateClosed?.ToString("s");
                    row["Current Close Reason"] = updatedProvider.ReasonEstablishmentClosed;
                    row["Current Successor Provider ID"] = updatedProvider.GetSuccessors().NullSafeJoinWith(";");
                    row["Current Trust Code"] = updatedProvider.TrustCode;
                    row["Current Trust Name"] = updatedProvider.TrustName;
                    
                    row["Previous URN"] = releasedProvider.URN;
                    row["Previous Provider Name"] = releasedProvider.Name;
                    row["Previous Provider Type"] = releasedProvider.ProviderType;
                    row["Previous Provider Subtype"] = releasedProvider.ProviderSubType;
                    row["Previous LA Code"] = releasedProvider.LACode;
                    row["Previous LA Name"] = releasedProvider.Authority;
                    row["Previous Close Date"] = releasedProvider.DateClosed?.ToString("s");
                    row["Previous Close Reason"] = releasedProvider.ReasonEstablishmentClosed;
                    row["Previous Trust Code"] = releasedProvider.TrustCode;
                    row["Previous Trust Name"] = releasedProvider.TrustName;

                    updatedPublishedProviderVersion = releasedPublishedProviderVersion = null;

                    yield return (ExpandoObject)row;
                }
            }

            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}
