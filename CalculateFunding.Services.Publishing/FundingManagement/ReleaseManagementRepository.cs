using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Migration;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FundingGroup = CalculateFunding.Services.Publishing.FundingManagement.SqlModels.FundingGroup;
using SqlGroupingReason = CalculateFunding.Services.Publishing.FundingManagement.SqlModels.GroupingReason;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ReleaseManagementRepository : SqlRepository, IReleaseManagementRepository
    {
        private readonly IExternalApiQueryBuilder _externalApiQueryBuilder;
        private readonly IReleaseManagementDataTableImporter _dataTableImporter;
        private ISqlTransaction _transaction;

        public ReleaseManagementRepository(
            ISqlConnectionFactory connectionFactory,
            ISqlPolicyFactory sqlPolicyFactory,
            IExternalApiQueryBuilder externalApiQueryBuilder,
            IReleaseManagementDataTableImporter dataTableImporter
            ) : base(connectionFactory, sqlPolicyFactory)
        {
            Guard.ArgumentNotNull(externalApiQueryBuilder, nameof(externalApiQueryBuilder));
            Guard.ArgumentNotNull(dataTableImporter, nameof(dataTableImporter));

            _externalApiQueryBuilder = externalApiQueryBuilder;
            _dataTableImporter = dataTableImporter;
        }

        public void InitialiseTransaction()
        {
            _transaction = BeginTransaction();
        }

        public void Commit()
        {
            if (_transaction == null)
            {
                throw new ArgumentNullException("Transaction must be initialised before calling Commit");
            }

            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
            }
        }

        public void RollBack()
        {
            if (_transaction == null)
            {
                throw new ArgumentNullException("Transaction must be initialised before calling Rollback");
            }

            _transaction.Rollback();
        }

        public async Task<IEnumerable<SqlGroupingReason>> GetGroupingReasons()
        {
            return await QuerySql<SqlGroupingReason>("SELECT * FROM GroupingReasons");
        }

        public async Task<IEnumerable<SqlGroupingReason>> GetGroupingReasonsUsingAmbientTransaction()
        {
            return await QuerySql<SqlGroupingReason>("SELECT * FROM GroupingReasons", transaction: _transaction);
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
        public async Task<IEnumerable<VariationReason>> GetVariationReasonsUsingAmbientTransaction()
        {
            return await QuerySql<VariationReason>("SELECT * FROM variationreasons", transaction: _transaction);
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
            return await QuerySql<FundingPeriod>("SELECT * FROM FundingPeriods");
        }

        public async Task<IEnumerable<FundingPeriod>> GetFundingPeriodsUsingAmbientTransaction()
        {
            return await QuerySql<FundingPeriod>("SELECT * FROM FundingPeriods", transaction: _transaction);
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreamsUsingAmbientTransaction()
        {
            return await QuerySql<FundingStream>("SELECT * FROM FundingStreams", transaction: _transaction);
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return await QuerySql<FundingStream>("SELECT * FROM FundingStreams");
        }

        public async Task<FundingPeriod> GetFundingPeriodByCode(string code)
        {
            return await QuerySingleSql<FundingPeriod>("SELECT * FROM fundingperiods WHERE FundingPeriodCode=@code",
                new
                {
                    code
                });
        }

        public async Task<FundingStream> GetFundingStreamByCode(string code)
        {
            return await QuerySingleSql<FundingStream>("SELECT * FROM fundingstreams WHERE FundingStreamCode=@code",
                new
                {
                    code
                });
        }

        public async Task<FundingStream> CreateFundingStream(FundingStream fundingStream)
        {
            fundingStream.FundingStreamId = await Insert(fundingStream);

            return fundingStream;
        }

        public async Task<FundingStream> CreateFundingStreamUsingAmbientTransaction(FundingStream fundingStream)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            fundingStream.FundingStreamId = await Insert(fundingStream);

            return fundingStream;
        }

        public async Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod)
        {
            fundingPeriod.FundingPeriodId = await Insert(fundingPeriod);

            return fundingPeriod;
        }

        public async Task<FundingPeriod> CreateFundingPeriodUsingAmbientTransaction(FundingPeriod fundingPeriod)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            fundingPeriod.FundingPeriodId = await Insert(fundingPeriod, _transaction);

            return fundingPeriod;
        }

        public async Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup)
        {
            await Insert(fundingGroup);

            return fundingGroup;
        }

        public async Task<FundingGroup> CreateFundingGroupUsingAmbientTransaction(FundingGroup fundingGroup)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(fundingGroup, _transaction);

            return fundingGroup;
        }

        public async Task<FundingGroup> GetFundingGroupUsingAmbientTransaction(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

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
                            }, _transaction);
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

        public async Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersions()
        {
            return await QuerySql<FundingGroupVersion>(@"SELECT * FROM FundingGroupVersions");
        }

        public async Task<IEnumerable<FundingGroup>> GetFundingGroups()
        {
            return await QuerySql<FundingGroup>(@"SELECT * FROM FundingGroups");
        }

        public async Task<IEnumerable<FundingGroup>> GetFundingGroupsBySpecificationAndChannelUsingAmbientTransaction(string specificationId, int channelId)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            return await QuerySql<FundingGroup>(@"SELECT * FROM FundingGroups WHERE
                                                    SpecificationId = @specificationId
                                                    AND ChannelId = @channelId",
                new
                {
                    specificationId,
                    channelId
                }, _transaction);
        }

        public async Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion)
        {
            await Insert(fundingGroupVersion);

            return fundingGroupVersion;
        }

        public async Task<FundingGroupVersion> CreateFundingGroupVersionUsingAmbientTransaction(FundingGroupVersion fundingGroupVersion)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(fundingGroupVersion, _transaction);

            return fundingGroupVersion;
        }

        public async Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason)
        {
            await Insert<FundingGroupVersionVariationReason>(reason);

            return reason;
        }

        public async Task<ReleasedProviderChannelVariationReason> CreateReleasedProviderChannelVariationReason(ReleasedProviderChannelVariationReason reason)
        {
            await Insert<ReleasedProviderChannelVariationReason>(reason);

            return reason;
        }

        public async Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReasonUsingAmbientTransaction(FundingGroupVersionVariationReason reason)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert<FundingGroupVersionVariationReason>(reason, _transaction);

            return reason;
        }

        public async Task<IEnumerable<Specification>> GetSpecifications()
        {
            return await QuerySql<Specification>("SELECT * FROM Specifications");
        }

        public async Task<IEnumerable<ReleasedProvider>> GetReleasedProviders()
        {
            return await QuerySql<ReleasedProvider>("SELECT * FROM ReleasedProviders");
        }

        public async Task<IEnumerable<ReleasedProviderVersion>> GetReleasedProviderVersions()
        {
            return await QuerySql<ReleasedProviderVersion>("SELECT * FROM ReleasedProviderVersions");
        }

        public async Task<Specification> GetSpecificationById(string id)
        {
            return await QuerySingleSql<Specification>("SELECT * FROM Specifications WHERE SpecificationId=@id",
                new
                {
                    id
                });
        }

        public async Task<Specification> CreateSpecification(Specification specification)
        {
            await Insert(specification);

            return specification;
        }

        public async Task<Specification> CreateSpecificationUsingAmbientTransaction(Specification specification)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(specification, _transaction);

            return specification;
        }

        public async Task<bool> UpdateSpecificationUsingAmbientTransaction(Specification specification)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            bool success = await Update(specification, _transaction);

            if (!success)
            {
                throw new RetriableException("Unknown reason for update specification failure so retriable exception thrown");
            }

            return true;
        }

        public async Task<IEnumerable<ReleasedProvider>> CreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            List<ReleasedProvider> results = new List<ReleasedProvider>(releasedProviders.Count());

            foreach (ReleasedProvider provider in releasedProviders)
            {
                await Insert(provider, _transaction);
                results.Add(provider);
            }

            return results;
        }

        public async Task<ReleasedProvider> CreateReleasedProvider(ReleasedProvider releasedProvider)
        {
            await Insert(releasedProvider);
            return releasedProvider;
        }

        public async Task<ReleasedProviderVersion> CreateReleasedProviderVersion(ReleasedProviderVersion releasedProviderVersion)
        {
            await Insert(releasedProviderVersion);
            return releasedProviderVersion;
        }

        public async Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelsUsingAmbientTransaction(ReleasedProviderVersionChannel providerVersionChannel)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(providerVersionChannel, _transaction);

            return providerVersionChannel;
        }

        public async Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannel(ReleasedProviderVersionChannel providerVersionChannel)
        {
            await Insert(providerVersionChannel);
            return providerVersionChannel;
        }

        public async Task<ProviderVersionInChannel> GetReleasedProvider(string publishedProviderVersion, int channelId)
        {
            return await QuerySingleSql<ProviderVersionInChannel>(@$"
				SELECT RPVC.[ChannelId], C.ChannelCode, C.ChannelName, RPV.MajorVersion, RPV.MinorVersion, RP.ProviderId, RPV.CoreProviderVersionId
                FROM [ReleasedProviders] RP
                INNER JOIN ReleasedProviderVersions RPV ON RP.ReleasedProviderId = RPV.ReleasedProviderId
                INNER JOIN ReleasedProviderVersionChannels RPVC ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
                INNER JOIN Channels C ON C.ChannelId = RPVC.ChannelId
                WHERE RPV.FundingId = @{nameof(publishedProviderVersion)}
				AND RPVC.ChannelId = @{nameof(channelId)}",
                new
                {
                    publishedProviderVersion,
                    channelId,
                });
        }

        public async Task<ReleasedProviderVersionChannel> GetReleasedProviderVersionChannel(int releasedProviderVersionId, int channelId)
        {
            return await QuerySingleSql<ReleasedProviderVersionChannel>(
            @$"
                SELECT *
                FROM ReleasedProviderVersionChannels RPVC
                WHERE RPVC.ReleasedProviderVersionId = @{nameof(releasedProviderVersionId)} AND RPVC.ChannelId = @{nameof(channelId)}",
            new
            {
                releasedProviderVersionId,
                channelId
            });
        }

        public async Task<IEnumerable<ReleasedProviderVersionChannelResult>> GetReleasedProviderVersionChannels(IEnumerable<int> channelIds)
        {
            return await QuerySql<ReleasedProviderVersionChannelResult>(
                @$"
                SELECT RPVC.*, RP.ProviderId
                FROM ReleasedProviderVersionChannels RPVC
                INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
                INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
                WHERE RPVC.ChannelId IN @{nameof(channelIds)}",
                new
                {
                    channelIds
                });
        }

        public async Task<IEnumerable<ReleasedProviderChannelVariationReason>> CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReasons)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            foreach (ReleasedProviderChannelVariationReason variationReason in variationReasons)
            {
                await Insert(variationReason, _transaction);
            }

            return variationReasons;
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

        public async Task<Channel> GetChannelFromUrlKey(string normalisedKey)
        {
            return await QuerySingleSql<Channel>($"SELECT * FROM Channels WHERE UrlKey = @{nameof(normalisedKey)}",
                new
                {
                    normalisedKey,
                });
        }

        public async Task<bool>  ContainsFundingId(int? channelId, string fundingId)
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
				FROM FundingGroupVersions FGV
				INNER JOIN FundingGroupProviders FGP ON FGV.FundingGroupVersionId = FGP.FundingGroupVersionId
				INNER JOIN ReleasedProviderVersionChannels RPVC ON FGP.ReleasedProviderVersionChannelId = RPVC.ReleasedProviderVersionChannelId
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				WHERE RPV.FundingId = @{nameof(publishedProviderVersion)}
				AND FGV.ChannelId = @{nameof(channelId)}",
                new
                {
                    publishedProviderVersion,
                    channelId,
                });
        }

        public async Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds)
        {
            return await GetLatestPublishedProviderVersionsInternal(specificationId, channelIds, null);
        }

        public async Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<int> channelIds)
        {
            return await GetLatestPublishedProviderVersionsInternal(specificationId, channelIds, _transaction);
        }

        private Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsInternal(string specificationId, IEnumerable<int> channelIds, ISqlTransaction sqlTransaction)
        {
            return QuerySql<ProviderVersionInChannel>(
            @$"
				SELECT RPVC.ReleasedProviderVersionChannelId, RPVC.[ChannelId], C.ChannelCode, C.ChannelName, RPV.MajorVersion, RPV.MinorVersion, RP.ProviderId, RPVC.ChannelVersion
				FROM ReleasedProviderVersionChannels RPVC
				INNER JOIN ReleasedProviderVersions RPV on RPV.ReleasedProviderVersionId = RPVC.ReleasedProviderVersionId
				INNER JOIN (
				SELECT Max(MajorVersion) As MajorVersion, RPV.ReleasedProviderId
				FROM ReleasedProviderVersions RPV
				INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
				WHERE RP.SpecificationId = @{nameof(specificationId)}
				GROUP BY RPV.ReleasedProviderId) LatestVersion ON LatestVersion.MajorVersion = RPV.MajorVersion AND LatestVersion.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
                INNER JOIN Channels C ON C.ChannelId = RPVC.ChannelId
				WHERE RPVC.ChannelId IN @{nameof(channelIds)}",
            new
            {
                specificationId,
                channelIds,
            },
            sqlTransaction);
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
				INNER JOIN FundingGroupProviders FGP ON FGP.FundingGroupVersionId = FGV.FundingGroupVersionId
                INNER JOIN ReleasedProviderVersionChannels RPVC ON RPVC.ReleasedProviderVersionChannelId = FGP.ReleasedProviderVersionChannelId
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN GroupingReasons GR ON GR.GroupingReasonId = FGV.GroupingReasonId 
				WHERE FG.SpecificationId = @{nameof(specificationId)} 
				AND FG.ChannelId = @{nameof(channelId)}",
                new
                {
                    specificationId,
                    channelId,
                });
        }

        public async Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionChannelVersionInFundingGroups(string specificationId)
        {
            return await QuerySql<LatestProviderVersionInFundingGroup>($@"
				SELECT RPV.MajorVersion, FGV.ChannelVersion, FGV.ChannelId
					, RP.ProviderId
					, FG.OrganisationGroupTypeCode
					, FG.OrganisationGroupIdentifierValue
					, FG.OrganisationGroupName
					, FG.OrganisationGroupTypeClassification
					, GR.GroupingReasonCode
                    , FGV.FundingId
				FROM FundingGroupVersions FGV
				INNER JOIN 
				(SELECT FundingGroupId, MAX(MajorVersion) AS LatestVersion FROM FundingGroupVersions FGVAgg
				GROUP BY FundingGroupId) LatestFundingGroupVersion ON FGV.FundingGroupId = LatestFundingGroupVersion.FundingGroupId AND FGV.MajorVersion = LatestFundingGroupVersion.LatestVersion
				INNER JOIN FundingGroups FG ON FG.FundingGroupID = FGV.FundingGroupId
				INNER JOIN FundingGroupProviders FGP ON FGP.FundingGroupVersionId = FGV.FundingGroupVersionId
                INNER JOIN ReleasedProviderVersionChannels RPVC ON RPVC.ReleasedProviderVersionChannelId = FGP.ReleasedProviderVersionChannelId
				INNER JOIN ReleasedProviderVersions RPV ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
				INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN GroupingReasons GR ON GR.GroupingReasonId = FGV.GroupingReasonId 
				WHERE FG.SpecificationId = @{nameof(specificationId)}",
                new
                {
                    specificationId
                }, _transaction);
        }

        public async Task<ReleasedProviderVersion> CreateReleasedProviderVersionUsingAmbientTransaction(ReleasedProviderVersion providerVersion)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(providerVersion, _transaction);

            return providerVersion;
        }

        public async Task<FundingGroupProvider> CreateFundingGroupProviderUsingAmbientTransaction(FundingGroupProvider fundingGroupProvider)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            await Insert(fundingGroupProvider, _transaction);

            return fundingGroupProvider;
        }

        public async Task<FundingGroupProvider> CreateFundingGroupProvider(FundingGroupProvider fundingGroupProvider)
        {
            await Insert(fundingGroupProvider);
            return fundingGroupProvider;
        }

        public async Task<IEnumerable<ReleasedDataAllocationHistory>> GetPublishedProviderTransactionHistory(string specificationId, string providerId)
        {
            return await QuerySql<ReleasedDataAllocationHistory>(
            @$"
                SELECT RP.ProviderId, RPV.MajorVersion, RPV.MinorVersion, C.ChannelName, C.ChannelCode, VR.VariationReasonName, RPVC.AuthorId, RPVC.AuthorName, RPVC.StatusChangedDate, RPV.TotalFunding
                FROM ReleasedProviders RP
                INNER JOIN ReleasedProviderVersions RPV ON RP.ReleasedProviderId = RPV.ReleasedProviderId
                INNER JOIN ReleasedProviderVersionChannels RPVC ON RPV.ReleasedProviderVersionId = RPVC.ReleasedProviderVersionId
                INNER JOIN Channels C ON RPVC.ChannelId = C.ChannelId
                INNER JOIN ReleasedProviderChannelVariationReasons RPCVR ON RPVC.ReleasedProviderVersionChannelId = RPCVR.ReleasedProviderVersionChannelId
                INNER JOIN VariationReasons VR ON RPCVR.VariationReasonId = VR.VariationReasonId
                WHERE RP.ProviderId = @{nameof(providerId)} AND RP.SpecificationId = @{nameof(specificationId)}",
            new
            {
                specificationId,
                providerId,
            });
        }

        public async Task ClearDatabase()
        {
            var deleteTask = Task.Run(() =>
            {
                ExecuteNonQuery("DELETE FROM [dbo].[FundingGroupProviders];");
                ExecuteNonQuery("DELETE FROM [dbo].[FundingGroupVersionVariationReasons];");
                ExecuteNonQuery("DELETE FROM [dbo].[ReleasedProviderChannelVariationReasons];");
                ExecuteNonQuery("DELETE FROM [dbo].[ReleasedProviderVersionChannels];");
                ExecuteNonQuery("DELETE FROM [dbo].[FundingGroupVersions];");
                ExecuteNonQuery("DELETE FROM [dbo].[ReleasedProviderVersions];");
                ExecuteNonQuery("DELETE FROM [dbo].[FundingGroups];");
                ExecuteNonQuery("DELETE FROM [dbo].[ReleasedProviders];");
            });

            await deleteTask;
        }

        public async Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionsBySpecificationId(string specificationId)
        {
            return await QuerySql<FundingGroupVersion>($@"
                SELECT *
                FROM FundingGroupVersions FGV
                INNER JOIN FundingGroups FG ON FG.FundingGroupID = FGV.FundingGroupId
                WHERE FG.SpecificationId =  {nameof(specificationId)}",
                new
                {
                    specificationId
                });
        }

        public async Task<IEnumerable<FundingGroupVersion>> GetLatestFundingGroupVersionsBySpecificationId(string specificationId)
        {
            return await QuerySql<FundingGroupVersion>($@"
                SELECT FGV.*
                FROM FundingGroupVersions FGV
                INNER JOIN 
                (SELECT FundingGroupId, MAX(MajorVersion) AS LatestVersion FROM FundingGroupVersions 
                GROUP BY FundingGroupId) LatestFundingGroupVersion ON FGV.FundingGroupId = LatestFundingGroupVersion.FundingGroupId AND FGV.MajorVersion = LatestFundingGroupVersion.LatestVersion
                INNER JOIN FundingGroups FG ON FG.FundingGroupID = FGV.FundingGroupId
				WHERE FG.SpecificationId =  @{nameof(specificationId)}",
                new
                {
                    specificationId
                });
        }

        public async Task<IEnumerable<LatestFundingGroupVersion>> GetLatestFundingGroupMajorVersionsBySpecificationId(string specificationId, int channelId)
        {
            return await QuerySql<LatestFundingGroupVersion>($@"
                    SELECT FGV.FundingGroupId, FGV.FundingGroupVersionId, FGV.MajorVersion, FS.FundingStreamCode, FP.FundingPeriodCode, GR.GroupingReasonCode, FG.OrganisationGroupTypeCode, FG.OrganisationGroupIdentifierValue
                    FROM FundingGroupVersions FGV
                    INNER JOIN
                        (SELECT FundingGroupId, MAX(MajorVersion) AS LatestVersion FROM FundingGroupVersions 
                        GROUP BY FundingGroupId) LatestFundingGroupVersion ON FGV.FundingGroupId = LatestFundingGroupVersion.FundingGroupId AND FGV.MajorVersion = LatestFundingGroupVersion.LatestVersion
                    INNER JOIN FundingStreams FS ON FGV.FundingStreamId = FS.FundingStreamId
                    INNER JOIN FundingPeriods FP ON FGV.FundingPeriodId = FP.FundingPeriodId
                    INNER JOIN GroupingReasons GR ON FGV.GroupingReasonId = GR.GroupingReasonId
                    INNER JOIN FundingGroups FG ON FGV.FundingGroupId = FG.FundingGroupId
				    WHERE FG.ChannelId =  @{nameof(channelId)}
				    AND FG.SpecificationId =  @{nameof(specificationId)}",
                new
                {
                    specificationId,
                    channelId
                });
        }

        public async Task<IEnumerable<ReleasedProviderSummary>> GetReleasedProviderSummaryBySpecificationId(string specificationId)
        {
            return await QuerySql<ReleasedProviderSummary>($@"
                SELECT RPVC.ChannelId, RP.ProviderId, RPV.FundingId, RPVC.ChannelVersion, CH.ChannelName
                FROM ReleasedProviderVersionChannels RPVC
                INNER JOIN ReleasedProviderVersions RPV ON RPV.ReleasedProviderVersionId = RPVC.ReleasedProviderVersionId
                INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN Channels CH ON CH.ChannelId = RPVC.ChannelId
                WHERE RP.SpecificationId =  {nameof(specificationId)}",
                new
                {
                    specificationId
                });
        }

        public async Task<IEnumerable<ReleasedProviderSummary>> GetLatestReleasedProviderSummaryBySpecificationId(string specificationId)
        {
            return await QuerySql<ReleasedProviderSummary>($@"
                SELECT RPVC.ChannelId, RP.ProviderId, RPV.FundingId, RPVC.ChannelVersion, CH.ChannelName
                FROM ReleasedProviderVersionChannels RPVC
                INNER JOIN ReleasedProviderVersions RPV ON RPV.ReleasedProviderVersionId = RPVC.ReleasedProviderVersionId
                INNER JOIN ReleasedProviders RP ON RP.ReleasedProviderId = RPV.ReleasedProviderId
				INNER JOIN Channels CH ON CH.ChannelId = RPVC.ChannelId
                INNER JOIN 
                (SELECT ReleasedProviderId, MAX(MajorVersion) AS LatestVersion FROM ReleasedProviderVersions 
                GROUP BY ReleasedProviderId) LatestReleasedProviderVersion ON RPV.ReleasedProviderId = LatestReleasedProviderVersion.ReleasedProviderId AND RPV.MajorVersion = LatestReleasedProviderVersion.LatestVersion
                WHERE RP.SpecificationId = {nameof(specificationId)}",
                new
                {
                    specificationId
                });
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId)
        {
            return GetReleasedProvidersInternal(specificationId, _transaction);
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId)
        {
            return GetReleasedProvidersInternal(specificationId);
        }

        private async Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersInternal(string specificationId, ISqlTransaction transaction = null)
        {
            return await QuerySql<ReleasedProvider>($"SELECT * FROM ReleasedProviders WHERE SpecificationId = @{nameof(specificationId)}",
                new { specificationId },
                transaction);
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds)
        {
            return GetReleasedProvidersInternal(specificationId, providerIds, _transaction);
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId, IEnumerable<string> providerIds)
        {
            return GetReleasedProvidersInternal(specificationId, providerIds);
        }

        public async Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersInternal(string specificationId, IEnumerable<string> providerIds, ISqlTransaction transaction = null)
        {
            return await QuerySql<ReleasedProvider>(
                $@"SELECT * FROM ReleasedProviders WHERE SpecificationId = @{nameof(specificationId)}
                AND ProviderId IN @{nameof(providerIds)}",
                new { specificationId, providerIds },
                transaction);
        }

        public async Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersions(string specificationId, IEnumerable<string> providerIds)
        {
            return await GetLatestReleasedProviderVersionsInternal(specificationId, providerIds);
        }

        private async Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsInternal(string specificationId, ISqlTransaction transaction = null)
        {
            return await QuerySql<LatestReleasedProviderVersion>(@$"
                SELECT ReleasedProviderVersionId, LRPV.LatestMajorVersion, RP.ProviderId FROM ReleasedProviderVersions RPV
                INNER JOIN (
                SELECT ReleasedProviderId, MAX(MajorVersion) as LatestMajorVersion FROM ReleasedProviderVersions 
                GROUP BY ReleasedProviderId) LRPV ON RPV.ReleasedProviderId = LRPV.ReleasedProviderId AND RPV.MajorVersion = LRPV.LatestMajorVersion
                INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
                WHERE RP.SpecificationId = @{nameof(specificationId)}
                ", new { specificationId },
                transaction);
        }

        public async Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsInternal(string specificationId, IEnumerable<string> providerIds, ISqlTransaction transaction = null)
        {
            return await QuerySql<LatestReleasedProviderVersion>(@$"
                SELECT ReleasedProviderVersionId, LRPV.LatestMajorVersion, RP.ProviderId FROM ReleasedProviderVersions RPV
                INNER JOIN (
                SELECT ReleasedProviderId, MAX(MajorVersion) as LatestMajorVersion FROM ReleasedProviderVersions 
                GROUP BY ReleasedProviderId) LRPV ON RPV.ReleasedProviderId = LRPV.ReleasedProviderId AND RPV.MajorVersion = LRPV.LatestMajorVersion
                INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
                WHERE RP.SpecificationId = @{nameof(specificationId)}
                AND RP.ProviderId IN @{nameof(providerIds)}
                ", new { specificationId, providerIds }, transaction);
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId)
        {
            return GetLatestReleasedProviderVersionsInternal(specificationId, _transaction);
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds)
        {
            return GetLatestReleasedProviderVersionsInternal(specificationId, providerIds, _transaction);
        }

        public async Task<IEnumerable<FundingGroup>> BulkCreateFundingGroupsUsingAmbientTransaction(IEnumerable<FundingGroup> fundingGroups)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            FundingGroupDataTableBuilder builder = new FundingGroupDataTableBuilder();
            builder.AddRows(fundingGroups.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return fundingGroups;
        }

        public async Task<IEnumerable<FundingGroupVersion>> BulkCreateFundingGroupVersionsUsingAmbientTransaction(IEnumerable<FundingGroupVersion> fundingGroupVersions)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            FundingGroupVersionDataTableBuilder builder = new FundingGroupVersionDataTableBuilder();
            builder.AddRows(fundingGroupVersions.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return fundingGroupVersions;
        }

        public async Task<IEnumerable<FundingGroupVersionVariationReason>> BulkCreateFundingGroupVersionVariationReasonsUsingAmbientTransaction(IEnumerable<FundingGroupVersionVariationReason> variationReasons)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            FundingGroupVersionVariationReasonsDataTableBuilder builder = new FundingGroupVersionVariationReasonsDataTableBuilder();
            builder.AddRows(variationReasons.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return variationReasons;
        }

        public async Task<IEnumerable<FundingGroupProvider>> BulkCreateFundingGroupProvidersUsingAmbientTransaction(IEnumerable<FundingGroupProvider> providers)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            FundingGroupProviderDataTableBuilder builder = new FundingGroupProviderDataTableBuilder();
            builder.AddRows(providers.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return providers;
        }

        public async Task<IEnumerable<ReleasedProvider>> BulkCreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            ReleasedProviderDataTableBuilder builder = new ReleasedProviderDataTableBuilder();
            builder.AddRows(releasedProviders.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return releasedProviders;
        }

        public async Task<IEnumerable<ReleasedProviderVersion>> BulkCreateReleasedProviderVersionsUsingAmbientTransaction(IEnumerable<ReleasedProviderVersion> releasedProviderVersions)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            ReleasedProviderVersionDataTableBuilder builder = new ReleasedProviderVersionDataTableBuilder();
            builder.AddRows(releasedProviderVersions.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return releasedProviderVersions;
        }

        public async Task<IEnumerable<ReleasedProviderChannelVariationReason>> BulkCreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReasons)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            ReleasedProviderChannelVariationReasonDataTableBuilder builder = new ReleasedProviderChannelVariationReasonDataTableBuilder();
            builder.AddRows(variationReasons.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return variationReasons;
        }

        public async Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionChannel(Guid fundingGroupId, int channelId,  ISqlTransaction transaction = null)
        {
            return await QuerySql<FundingGroupVersion>($@"
                SELECT  top 1 * 
                FROM FundingGroupVersions 
                WHERE FundingGroupId = @{nameof(fundingGroupId)}
                AND ChannelId = @{nameof(channelId)} order by MajorVersion desc",
                new
                {
                    fundingGroupId,
                    channelId
                }, transaction);
        }

        public async Task<IEnumerable<ReleasedProviderVersionChannel>> BulkCreateReleasedProviderVersionChannelsUsingAmbientTransaction(IEnumerable<ReleasedProviderVersionChannel> releasedProviderVersionChannels)
        {
            Guard.ArgumentNotNull(_transaction, nameof(_transaction));

            ReleasedProviderVersionChannelDataTableBuilder builder = new ReleasedProviderVersionChannelDataTableBuilder();
            builder.AddRows(releasedProviderVersionChannels.ToArray());

            await _dataTableImporter.ImportDataTable(
                builder,
                SqlBulkCopyOptions.KeepIdentity,
                _transaction);

            return releasedProviderVersionChannels;
        }

        public async Task<IEnumerable<int>> GetLatestReleasedProviderVersionsId(string specificationId, string providerIds, int channelId, ISqlTransaction transaction = null)
        {          
            return await QuerySql<int>(@$"
                SELECT RPVC.ChannelVersion FROM ReleasedProviderVersions RPV
                INNER JOIN (
                SELECT ReleasedProviderId, MAX(MajorVersion) as LatestMajorVersion FROM ReleasedProviderVersions 
                GROUP BY ReleasedProviderId) LRPV ON RPV.ReleasedProviderId = LRPV.ReleasedProviderId AND RPV.MajorVersion = LRPV.LatestMajorVersion
                INNER JOIN ReleasedProviders RP ON RPV.ReleasedProviderId = RP.ReleasedProviderId
                INNER JOIN ReleasedProviderVersionChannels RPVC ON RPVC.ReleasedProviderVersionId = RPV.ReleasedProviderVersionId
                WHERE RP.SpecificationId = @{nameof(specificationId)}
                AND RP.ProviderId = @{nameof(providerIds)}
                AND RPVC.ChannelId =  @{ nameof(channelId)} ",
                new { specificationId, providerIds, channelId }, transaction);
        }
    }
}
