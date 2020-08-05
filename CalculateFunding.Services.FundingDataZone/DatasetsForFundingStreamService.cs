using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class DatasetsForFundingStreamService : IDatasetsForFundingStreamService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public DatasetsForFundingStreamService(IPublishingAreaRepository publishingAreaRepository)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));

            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<Dataset>> GetDatasetsForFundingStream(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            IEnumerable<IGrouping<string, PublishingAreaDatasetMetadata>> datasetVersions = (await _publishingAreaRepository.GetDatasetMetadata(fundingStreamId))
                .GroupBy(_ => _.DatasetName);

            return datasetVersions.Select(MapToDataset).Where(_ => _ != null).ToArray();
        }

        private Dataset MapToDataset(IGrouping<string, PublishingAreaDatasetMetadata> datasetVersion)
        {
            //I'm not certain about just skipping the bad rows, we should maybe throw an exception to block the extract and let some one fix the data
            
            string createdDateString = ExtendedPropertyValue(datasetVersion, "CreatedDate");

            if (!DateTime.TryParse(createdDateString, out DateTime createdDate))
            {
                return null;
            }

            if (!TryGetExtendedProperty(datasetVersion, "GroupingLevel", out string groupingLevelString))
            {
                return null;
            }

            if (!TryGetExtendedProperty(datasetVersion, "ProviderIdentifierType", out string providerIdentifierType))
            {
                return null;
            }

            string versionLiteral = ExtendedPropertyValue(datasetVersion, "SnapshotVersion");

            if (!int.TryParse(versionLiteral, out int version))
            {
                return null;
            }

            Dictionary<string, string> customProperties = datasetVersion.Where(_ =>
                    _.ExtendedProperty.StartsWith("Prop_", StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(_ => _.ExtendedProperty.Remove(0, 5), _ => _.ExtendedPropertyValue);

            return new Dataset
            {
                CreatedDate = createdDate,
                DatasetCode = ExtendedPropertyValue(datasetVersion, "DatasetCode"),
                Description = ExtendedPropertyValue(datasetVersion, "Description"),
                DisplayName = ExtendedPropertyValue(datasetVersion, "DisplayName"),
                FundingStreamId = ExtendedPropertyValue(datasetVersion, "FundingStreamId"),
                GroupingLevel = groupingLevelString.AsEnum<GroupingLevel>(),
                IdentifierColumnName = ExtendedPropertyValue(datasetVersion, "ProviderIdentifierColumnName"),
                IdentifierType = providerIdentifierType.AsEnum<IdentifierType>(),
                OriginatingSystem = ExtendedPropertyValue(datasetVersion, "OriginatingSystem"),
                OriginatingSystemVersion = ExtendedPropertyValue(datasetVersion, "OriginatingSystemVersion"),
                ProviderSnapshotId = ExtendedPropertyValue(datasetVersion, "ProviderSnapshotId"),
                TableName = datasetVersion.Key,
                Version = version,
                Properties = customProperties
            };
        }

        private bool TryGetExtendedProperty(IGrouping<string, PublishingAreaDatasetMetadata> datasetVersion,
            string extendedPropertyName,
            out string extendedPropertyValue)
        {
            extendedPropertyValue = ExtendedPropertyValue(datasetVersion, extendedPropertyName);

            return extendedPropertyValue.IsNotNullOrWhitespace();
        }

        private static string ExtendedPropertyValue(IGrouping<string, PublishingAreaDatasetMetadata> datasetVersion,
            string extendedPropertyName) =>
            datasetVersion.SingleOrDefault(_ => _.ExtendedProperty == extendedPropertyName)?.ExtendedPropertyValue;
    }
}