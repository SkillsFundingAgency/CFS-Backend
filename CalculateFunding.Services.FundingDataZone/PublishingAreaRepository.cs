using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class PublishingAreaRepository : SqlRepository, IPublishingAreaRepository, IHealthChecker
    {
        public PublishingAreaRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory)
            : base(connectionFactory, sqlPolicyFactory)
        {
        }

        public Task<IEnumerable<PublishingAreaOrganisation>> GetAllOrganisations(int providerSnapshotId) => throw new NotImplementedException();

        public async Task<object> GetDataForTable(string tableName)
        {
            // Was unable to use @TableName - possible SQL injection
            string sql = $"SELECT * FROM {tableName}";

            return await QuerySql<object>(sql);
        }

        public async Task<IEnumerable<PublishingAreaDatasetMetadata>> GetDatasetMetadata(string fundingStreamId) =>
            await Query<PublishingAreaDatasetMetadata>("sp_getDatasetsByFundingStream",
                new
                {
                    FundingStreamId = fundingStreamId
                });

        public async Task<IEnumerable<string>> GetFundingStreamsWithDatasets() => await Query<string>("sp_getFundingStreamsWithDatasets");

        public Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots() => throw new NotImplementedException();

        public async Task<IEnumerable<PublishingAreaOrganisation>> GetLocalAuthorities(int providerSnapshotId) 
            => await Query<PublishingAreaOrganisation>("sp_GetPaymentOrganisationDetailsBySnapshotId",
            new {
                ProviderSnapshotId = providerSnapshotId,
                PaymentOrganisationType = PaymentOrganisationType.LocalAuthority.ToString()
            });

        public async Task<PublishingAreaProvider> GetProviderInSnapshot(int providerSnapshotId,
            string providerId) =>
            await QuerySingle<PublishingAreaProvider>("sp_getProviderDetailsByProviderId",
                new
                {
                    ProviderSnapshotId = providerSnapshotId,
                    ProviderId = providerId
                });

        public async Task<IEnumerable<PublishingAreaProvider>> GetProvidersInSnapshot(int providerSnapshotId) =>
            await Query<PublishingAreaProvider>("sp_getProviderDetailsBySnapshotId",
                new
                {
                    ProviderSnapshotId = providerSnapshotId
                });

        public async Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshots(string fundingStreamId) =>
            await Query<PublishingAreaProviderSnapshot>("sp_getProviderSnapshotsByFundingStream",
                new
                {
                    FundingStreamId = fundingStreamId
                });

        public async Task<string> GetTableNameForDataset(string datasetCode,
            int version) =>
            await QuerySingle<string>("sp_getDatasetByCode",
                new
                {
                    DatasetCode = datasetCode,
                    Version = version
                });

        public new async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(PublishingAreaRepository)
            };

            (bool Ok, string Message) sqlConnectionHealth = await base.IsHealthOk();

            health.Dependencies.Add(
                new DependencyHealth
                {
                    HealthOk = sqlConnectionHealth.Ok,
                    DependencyName = typeof(PublishingAreaRepository).GetFriendlyName(),
                    Message = sqlConnectionHealth.Message
                });

            return health;
        }
    }
}