using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class FundingGroupService : IFundingGroupService
    {
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly ILogger _logger;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;

        public FundingGroupService(IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            ILogger logger)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseManagementRepository = releaseManagementRepository;
            _logger = logger;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
        }

        public async Task<IEnumerable<FundingGroup>> CreateFundingGroups(string specificationId, int channelId, IEnumerable<OrganisationGroupResult> organisationGroupResults)
        {
            List<FundingGroup> results = new List<FundingGroup>();

            IEnumerable<SqlModels.GroupingReason> groupingReasons = await _releaseManagementRepository.GetGroupingReasons();
            Dictionary<string, int> groupingReasonIdLookupByCode = groupingReasons.ToDictionary(_ => _.GroupingReasonCode, _ => _.GroupingReasonId);

            Dictionary<string, OrganisationGroupResult> orgGroupLookup = organisationGroupResults.ToDictionary(_ =>
                $"{groupingReasonIdLookupByCode[_.GroupReason.ToString()]}-{_.GroupTypeClassification}-{_.IdentifierValue}");

            IEnumerable<FundingGroup> fundingGroups =
                await _releaseManagementRepository.GetFundingGroupsBySpecificationAndChannelUsingAmbientTransaction(specificationId,
                    channelId);

            IEnumerable<FundingGroup> existingFundingGroups = fundingGroups
                .Where(_ => orgGroupLookup.ContainsKey(
                    $"{_.GroupingReasonId}-{_.OrganisationGroupTypeClassification}-{_.OrganisationGroupIdentifierValue}"));

            Dictionary<string, FundingGroup> existingFundingGroupsLookup = existingFundingGroups.ToDictionary(_ => $"{_.GroupingReasonId}-{_.OrganisationGroupTypeClassification}-{_.OrganisationGroupIdentifierValue}");

            results.AddRange(existingFundingGroups);

            IEnumerable<FundingGroup> fundingGroupsToCreate = (from organisationGroupResult in organisationGroupResults
                                                               let groupingReasonId = groupingReasonIdLookupByCode[organisationGroupResult.GroupReason.ToString()]
                                                               where !existingFundingGroupsLookup.ContainsKey(
                                                                   $"{groupingReasonId}-{organisationGroupResult.GroupTypeClassification}-{organisationGroupResult.IdentifierValue}")
                                                               select new FundingGroup
                                                               {
                                                                   SpecificationId = specificationId,
                                                                   ChannelId = channelId,
                                                                   OrganisationGroupIdentifierValue = organisationGroupResult.IdentifierValue,
                                                                   OrganisationGroupName = organisationGroupResult.Name,
                                                                   OrganisationGroupSearchableName = organisationGroupResult.SearchableName,
                                                                   OrganisationGroupTypeClassification = organisationGroupResult.GroupTypeClassification.ToString(),
                                                                   OrganisationGroupTypeCode = organisationGroupResult.GroupTypeCode.ToString(),
                                                                   OrganisationGroupTypeIdentifier = organisationGroupResult.GroupTypeIdentifier.ToString(),
                                                                   GroupingReasonId = groupingReasonId
                                                               });

            IEnumerable<FundingGroup> newFundingGroups =
                await _releaseManagementRepository.BulkCreateFundingGroupsUsingAmbientTransaction(fundingGroupsToCreate);

            results.AddRange(newFundingGroups);

            UpdateMappingContext(specificationId, channelId, organisationGroupResults, results, groupingReasons);

            return results;
        }

        private void UpdateMappingContext(string specificationId, int channelId,
            IEnumerable<OrganisationGroupResult> organisationGroupResults,
            IEnumerable<FundingGroup> fundingGroups, IEnumerable<SqlModels.GroupingReason> groupingReasons)
        {
            Dictionary<string, int> groupingReasonIdLookup = groupingReasons.ToDictionary(_ => _.GroupingReasonCode, _ => _.GroupingReasonId);

            foreach (FundingGroup fundingGroup in fundingGroups)
            {
                OrganisationGroupResult organisationGroupResult =
                    organisationGroupResults
                        .SingleOrDefault(_ =>
                            groupingReasonIdLookup[_.GroupReason.ToString()] == fundingGroup.GroupingReasonId &&
                                                 _.GroupTypeClassification.ToString() == fundingGroup.OrganisationGroupTypeClassification &&
                                                 _.IdentifierValue == fundingGroup.OrganisationGroupIdentifierValue);

                if (organisationGroupResult == null)
                {
                    string message =
                        $"Organisation group result cannot be found for funding group {fundingGroup.FundingGroupId}: specificationId {specificationId}, channelId {channelId}";
                    _logger.Error(message);
                    throw new KeyNotFoundException(message);
                }

                _releaseToChannelSqlMappingContext.FundingGroups.Add(organisationGroupResult, fundingGroup.FundingGroupId);
            }
        }
    }
}
