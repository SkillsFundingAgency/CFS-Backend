using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SqlGroupingReason = CalculateFunding.Services.Publishing.FundingManagement.SqlModels.GroupingReason;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ReleaseManagementRepository : SqlRepository, IReleaseManagementRepository
    {
        private readonly IExternalApiQueryBuilder _externalApiQueryBuilder;

        public ReleaseManagementRepository(
            ISqlConnectionFactory connectionFactory,
            ISqlPolicyFactory sqlPolicyFactory,
            IExternalApiQueryBuilder externalApiQueryBuilder
            ) : base(connectionFactory, sqlPolicyFactory)
        {
            Guard.ArgumentNotNull(externalApiQueryBuilder, nameof(externalApiQueryBuilder));

            _externalApiQueryBuilder = externalApiQueryBuilder;
        }

        public async Task<IEnumerable<SqlGroupingReason>> GetGroupingReasons()
        {
            return await QuerySql<SqlGroupingReason>("SELECT * FROM groupingreasons");
        }

        public async Task<SqlGroupingReason> CreateGroupingReason(SqlGroupingReason groupingReason)
        {
            groupingReason.GroupingReasonId = await Insert(groupingReason);

            return groupingReason;
        }

        public async Task<IEnumerable<VariationReason>> GetVariationReasons()
        {
            return await QuerySql<VariationReason>("SELECT * FROM variationreasons");
        }

        public async Task<VariationReason> CreateVariationReason(VariationReason variationReason)
        {
            variationReason.VariationReasonId = await Insert(variationReason);

            return variationReason;
        }

        public async Task<IEnumerable<Channel>> GetChannels()
        {
            return await QuerySql<Channel>("SELECT * FROM channels");
        }

        public async Task<Channel> GetChannelByChannelCode(string channelCode)
        {
            return await QuerySingleSql<Channel>("SELECT * FROM channels WHERE ChannelCode=@channelCode",
                                                    new
                                                    {
                                                        channelCode
                                                    });
        }

        public async Task<Channel> CreateChannel(Channel channel)
        {
            channel.ChannelId = await Insert(channel);

            return channel;
        }

        public async Task<bool> UpdateChannel(Channel channel)
        {
            return await Update(channel);
        }

        public async Task<IEnumerable<FundingPeriod>> GetFundingPeriods()
        {
            return await QuerySql<FundingPeriod>("SELECT * FROM fundingperiods");
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return await QuerySql<FundingStream>("SELECT * FROM fundingstreams");
        }

        public async Task<FundingStream> CreateFundingStream(FundingStream fundingStream)
        {
            fundingStream.FundingStreamId = await Insert(fundingStream);

            return fundingStream;
        }

        public async Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod)
        {
            fundingPeriod.FundingPeriodId = await Insert(fundingPeriod);

            return fundingPeriod;
        }

        public async Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup)
        {
            fundingGroup.FundingGroupId = await Insert(fundingGroup);

            return fundingGroup;
        }

        public async Task<FundingGroup> GetFundingGroup(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
        {
            return await QuerySingleSql<FundingGroup>(@"SELECT * FROM FundingGroups WHERE
							ChannelId = @channelId
							AND SpecificationId = @specificationId
							AND GroupingReasonId = @groupingReasonId
							AND OrganisationGroupTypeClassification = @organisationGroupTypeClassification
							AND OrganisationGroupIdentifierValue = @organisationGroupIdentifierValue",
                            new
                            {
                                channelId,
                                specificationId,
                                groupingReasonId,
                                organisationGroupTypeClassification,
                                organisationGroupIdentifierValue
                            });
        }

        public async Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion)
        {
            return await QuerySingleSql<FundingGroupVersion>(@"SELECT * FROM FundingGroupVersions WHERE
										FundingGroupId = @fundingGroupId
										AND MajorVersion = @majorVersion",
                                        new
                                        {
                                            fundingGroupId,
                                            majorVersion,
                                        });
        }

        public async Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion)
        {
            fundingGroupVersion.FundingGroupVersionId = await Insert(fundingGroupVersion);

            return fundingGroupVersion;
        }

        public async Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason)
        {
            reason.FundingGroupVersionVariationReasonId = await Insert<FundingGroupVersionVariationReason>(reason);

            return reason;
        }

        public async Task<IEnumerable<Specification>> GetSpecifications()
        {
            return await QuerySql<Specification>("SELECT * FROM Specifications");
        }

        public async Task<Specification> CreateSpecification(Specification specification)
        {
            await Insert(specification);

            return specification;
        }

        public async Task<IEnumerable<ReleasedProvider>> CreateReleasedProviders(IEnumerable<ReleasedProvider> releasedProviders)
        {
            using ISqlTransaction transaction = BeginTransaction();

            try
            {
                // calling the one which doesn't support transactions internally as I think we are going to need to do more inserting here
                bool success = await BulkInsert(releasedProviders.ToList(), transaction);

                if (success)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                    throw new RetriableException("Unknown reason for insert failure so throw retriable exception.");
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return releasedProviders;
        }

        public async Task<int> QueryPublishedFundingCount(
            int channelId,
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons)
        {
            (string sql, object parameters) = _externalApiQueryBuilder.BuildCountQuery(channelId,
                fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons);

            return await QuerySingleSql<int>(sql, parameters);
        }

        public async Task<IEnumerable<ExternalFeedFundingGroupItem>> QueryPublishedFunding(
            int channelId,
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            int totalCount)
        {
            (string sql, object parameters) = _externalApiQueryBuilder.BuildQuery(
                channelId,
                fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons,
                top,
                pageRef,
                totalCount
                );

            return await QuerySql<ExternalFeedFundingGroupItem>(sql, parameters);
        }

        public async Task<int?> GetChannelIdFromUrlKey(string normalisedKey)
        {
            return await QuerySingleSql<int?>($"SELECT ChannelId FROM Channels WHERE UrlKey = @{nameof(normalisedKey)}",
                new
                {
                    normalisedKey,
                });
        }

        public async Task<bool> ContainsFundingId(int? channelId, string fundingId)
        {
            int totalItems = await QuerySingleSql<int>(
                @$"SELECT COUNT(*) FROM FundingGroupVersions 
					WHERE FundingId = @{nameof(fundingId)}
					AND ChannelId = @{nameof(channelId)}",
                new
                {
                    channelId,
                    fundingId,
                });

            return totalItems > 0;
        }

        public async Task<bool> ContainsProviderVersion(int channelId, string providerFundingVersion)
        {
            int totalItems = await QuerySingleSql<int>(
                @$"SELECT COUNT(*)
				FROM ReleasedProviderVersionChannels RPVC
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				WHERE RPV.FundingId = @{nameof(providerFundingVersion)}
				AND RPVC.ChannelId = @{nameof(channelId)}",
                            new
                            {
                                channelId,
                                providerFundingVersion,
                            });

            return totalItems > 0;
        }

        public async Task<IEnumerable<string>> GetFundingGroupIdsForProviderFunding(int channelId, string publishedProviderVersion)
        {
            return await QuerySql<string>(@$"
				SELECT [FGV].[FundingId]
				FROM [ReleaseManagement].[dbo].[FundingGroupVersions] FGV
				INNER JOIN FundingGroupProviders FGP ON FGV.FundingGroupVersionId = FGP.FundingGroupVersionId
				INNER JOIN ReleasedProviderVersionChannels RPVC ON FGP.ProviderFundingVersionChannelId = RPVC.ReleasedProviderVersionChannelId
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				WHERE RPV.FundingId = @{nameof(publishedProviderVersion)}
				AND FGV.ChannelId = @{nameof(channelId)}",
                new
                {
                    publishedProviderVersion,
                    channelId,
                });
        }

        public Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds)
        {
            return QuerySql<ProviderVersionInChannel>(
            @$"
				SELECT RPVC.[ChannelId], RPV.MajorVersion, RP.ProviderId

	 
				FROM [ReleaseManagement].[dbo].[ReleasedProviderVersionChannels] RPVC
				INNER JOIN ReleasedProviderVersions RPV on RPV.ReleasedProviderVersionId = RPVC.ReleasedProviderVersionId
				INNER JOIN (
				SELECT Max(MajorVersion) As MajorVersion, RPV.ReleasedProviderId
				FROM ReleasedProviderVersions RPV
				INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
				WHERE RP.SpecificationId = @{nameof(specificationId)}
				GROUP BY RPV.ReleasedProviderId) LatestVersion ON LatestVersion.MajorVersion = RPV.MajorVersion AND LatestVersion.ReleasedProviderId = RPV.ReleasedProviderId

				INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				WHERE RPVC.ChannelId IN {nameof(channelIds)}",
            new
            {
                specificationId,
                channelIds,
            });
        }

        public async Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionInFundingGroups(string specificationId, int channelId)
        {
            return await QuerySql<LatestProviderVersionInFundingGroup>($@"
				SELECT RPV.MajorVersion
					, RP.ProviderId
					, FG.OrganisationGroupTypeCode
					, FG.OrganisationGroupIdentifierValue
					, FG.OrganisationGroupName
					, FG.OrganisationGroupTypeClassification
					, GR.GroupingReasonCode
				FROM FundingGroupVersions FGV
				INNER JOIN 
				(SELECT FundingGroupId, MAX(MajorVersion) AS LatestVersion FROM FundingGroupVersions FGVAgg
				GROUP BY FundingGroupId) LatestFundingGroupVersion ON FGV.FundingGroupId = LatestFundingGroupVersion.FundingGroupId AND FGV.MajorVersion = LatestFundingGroupVersion.LatestVersion


				INNER JOIN FundingGroups FG ON FG.FundingGroupID = FGV.FundingGroupId
				INNER JOIN FundingGroupProviders FGP ON FGV.FundingGroupVersionId = FGV.FundingGroupVersionId
				INNER JOIN ReleasedProviderVersionChannels RPVC ON RPVC.FundingGroupVersionId = FGP.FundingGroupVersionId
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN GroupingReasons GR ON GR.GroupingReasonId = FGV.GroupingReasonId 
				WHERE FG.SpecificationId =  {nameof(specificationId)} 
				AND FG.ChannelId = {nameof(channelId)}",
                new
                {
                    specificationId,
                    channelId,
                });
        }
    }
}
