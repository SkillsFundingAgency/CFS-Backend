using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Newtonsoft.Json;
using Serilog;
using System;
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

        public FundingGroupService(IReleaseManagementRepository releaseManagementRepository, IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext, ILogger logger)
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

            foreach (OrganisationGroupResult organisationGroupResult in organisationGroupResults)
            {
                int groupingReasonId = GetGroupingReasonId(specificationId, channelId, groupingReasons, organisationGroupResult);

                FundingGroup existingFundingGroup = await _releaseManagementRepository.GetFundingGroup(
                    channelId,
                    specificationId,
                    groupingReasonId,
                    organisationGroupResult.GroupTypeClassification.ToString(),
                    organisationGroupResult.IdentifierValue.ToString());

                if (existingFundingGroup != null)
                {
                    results.Add(existingFundingGroup);
                    continue;
                }

                FundingGroup fundingGroupToBeCreated = new FundingGroup
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
                };

                FundingGroup fundingGroup = await _releaseManagementRepository.CreateFundingGroup(fundingGroupToBeCreated);
                results.Add(fundingGroup);
                _releaseToChannelSqlMappingContext.FundingGroups.Add(organisationGroupResult, fundingGroup.FundingGroupId);
            }

            return results;
        }

        private int GetGroupingReasonId(string specificationId, int channelId, IEnumerable<SqlModels.GroupingReason> groupingReasons, OrganisationGroupResult organisationGroupResult)
        {
            SqlModels.GroupingReason groupingReason = groupingReasons.SingleOrDefault(g => g.GroupingReasonCode == organisationGroupResult.GroupReason.ToString());

            if (groupingReason == null)
            {
                string message = $"Grouping reason {organisationGroupResult.GroupReason} not found in sql for entity: {JsonConvert.SerializeObject(organisationGroupResult, Formatting.None)}. SpecificationId: {specificationId} and ChannelId {channelId}";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }

            return groupingReason.GroupingReasonId;
        }
    }
}
