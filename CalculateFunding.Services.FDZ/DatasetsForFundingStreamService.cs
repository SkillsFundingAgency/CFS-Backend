using CalculateFunding.Models.FDZ;
using CalculateFunding.Services.FDZ.Interfaces;
using CalculateFunding.Services.FDZ.SqlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ
{
    public class DatasetsForFundingStreamService : IDatasetsForFundingStreamService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public DatasetsForFundingStreamService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<Dataset>> GetDatasetsForFundingStream(string fundingStreamId)
        {
            IEnumerable<PublishingAreaDatasetMetadata> sqlResults = await _publishingAreaRepository.GetDatasetMetadata(fundingStreamId);


            var datasetVersions = sqlResults.GroupBy(c => c.DatasetName);

            List<Dataset> results = new List<Dataset>(datasetVersions.Count());

            foreach (var ds in datasetVersions)
            {
                string createdDateString = ds.SingleOrDefault(f => f.ExtendedProperty == "CreatedDate")?.ExtendedPropertyValue;
                DateTime? createdDate = null;
                if (!string.IsNullOrWhiteSpace(createdDateString))
                {
                    try
                    {
                        createdDate = DateTime.Parse(createdDateString);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                }

                string groupingLevelString = ds.SingleOrDefault(f => f.ExtendedProperty == "GroupingLevel")?.ExtendedPropertyValue;
                if (string.IsNullOrWhiteSpace(groupingLevelString))
                {
                    continue;
                }

                GroupingLevel groupingLevel = Enum.Parse<GroupingLevel>(groupingLevelString);

                string identiferTypeString = ds.SingleOrDefault(f => f.ExtendedProperty == "ProviderIdentifierType")?.ExtendedPropertyValue;
                if (string.IsNullOrWhiteSpace(identiferTypeString))
                {
                    continue;
                }

                IdentifierType identifierType = Enum.Parse<IdentifierType>(identiferTypeString);

                int version = 0;
                string versionString = ds.SingleOrDefault(f => f.ExtendedProperty == "SnapshotVersion")?.ExtendedPropertyValue;
                if (string.IsNullOrWhiteSpace(versionString))
                {
                    continue;
                }

                try
                {
                    version = int.Parse(versionString);
                }
                catch (FormatException)
                {
                    continue;
                }

                var customPropertiesMetadata = ds.Where(p => p.ExtendedProperty.StartsWith("Prop_", StringComparison.InvariantCultureIgnoreCase));

                Dictionary<string, string> customProperties = new Dictionary<string, string>();
                foreach (var property in customPropertiesMetadata)
                {
                    customProperties.Add(property.ExtendedProperty.Substring(5, property.ExtendedProperty.Length - 5), property.ExtendedPropertyValue);
                }

                Dataset dataset = new Dataset()
                {
                    CreatedDate = createdDate,
                    DatasetCode = ds.SingleOrDefault(f => f.ExtendedProperty == "DatasetCode")?.ExtendedPropertyValue,
                    Description = ds.SingleOrDefault(f => f.ExtendedProperty == "Description")?.ExtendedPropertyValue,
                    DisplayName = ds.SingleOrDefault(f => f.ExtendedProperty == "DisplayName")?.ExtendedPropertyValue,
                    FundingStreamId = ds.SingleOrDefault(f => f.ExtendedProperty == "FundingStreamId")?.ExtendedPropertyValue,
                    GroupingLevel = groupingLevel,
                    IdentifierColumnName = ds.SingleOrDefault(f => f.ExtendedProperty == "ProviderIdentifierColumnName")?.ExtendedPropertyValue,
                    IdentifierType = identifierType,
                    OriginatingSystem = ds.SingleOrDefault(f => f.ExtendedProperty == "OriginatingSystem")?.ExtendedPropertyValue,
                    OriginatingSystemVersion = ds.SingleOrDefault(f => f.ExtendedProperty == "OriginatingSystemVersion")?.ExtendedPropertyValue,
                    ProviderSnapshotId = ds.SingleOrDefault(f => f.ExtendedProperty == "ProviderSnapshotId")?.ExtendedPropertyValue,
                    TableName = ds.Key,
                    Version = version,
                    Properties = customProperties,
                };

                results.Add(dataset);
            }

            return results;
        }
    }
}
