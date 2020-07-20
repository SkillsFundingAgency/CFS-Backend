using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.FDZ.Interfaces;
using CalculateFunding.Services.FDZ.Settings;
using CalculateFunding.Services.FDZ.SqlModels;
using Dapper;

namespace CalculateFunding.Services.FDZ
{
    public class PublishingAreaRepository : IPublishingAreaRepository, IHealthChecker
    {
        private readonly FDZSqlStorageSettings _fDZSqlStorageSettings;

        public PublishingAreaRepository(FDZSqlStorageSettings fDZSqlStorageSettings)
        {
            Guard.ArgumentNotNull(fDZSqlStorageSettings, nameof(fDZSqlStorageSettings));

            _fDZSqlStorageSettings = fDZSqlStorageSettings;
        }

        public Task<IEnumerable<PublishingAreaOrganisation>> GetAllOrganisations(int providerSnapshotId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<object> GetDataForTable(string tableName)
        {
            // Was unable to use @TableName - possible SQL injection
            string sql = $"SELECT * FROM {tableName}";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QueryAsync(sql,
                new { TableName = tableName },
                commandType: CommandType.Text)).ToList();
        }

        public async Task<IEnumerable<PublishingAreaDatasetMetadata>> GetDatasetMetadata(string fundingStreamId)
        {
            const string sql = "sp_getDatasetsByFundingStream";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QueryAsync<PublishingAreaDatasetMetadata>(sql,
                new { FundingStreamId = fundingStreamId },
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<IEnumerable<string>> GetFundingStreamsWithDatasets()
        {
            const string sql = "sp_getFundingStreamsWithDatasets";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QueryAsync<string>(sql,
                new { },
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<PublishingAreaOrganisation>> GetLocalAuthorities(int providerSnapshotId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<PublishingAreaProvider> GetProviderInSnapshot(int providerSnapshotId, string providerId)
        {
            const string sql = "sp_getProviderDetailsByProviderId";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return await connection.QuerySingleOrDefaultAsync<PublishingAreaProvider>(sql,
                new { ProviderSnapshotId = providerSnapshotId, ProviderId = providerId, },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PublishingAreaProvider>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            const string sql = "sp_getProviderDetailsBySnapshotId";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QueryAsync<PublishingAreaProvider>(sql,
                new { ProviderSnapshotId = providerSnapshotId },
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshots(string fundingStreamId)
        {
            const string sql = "sp_getProviderSnapshotsByFundingStream";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QueryAsync<PublishingAreaProviderSnapshot>(sql,
                new { FundingStreamId = fundingStreamId },
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<string> GetTableNameForDataset(string datasetCode, int version)
        {
            const string sql = "sp_getDatasetByCode";

            using SqlConnection connection = GetSqlConnection();
            connection.Open();

            return (await connection.QuerySingleOrDefaultAsync<string>(sql,
                new { DatasetCode = datasetCode, Version = version },
                commandType: CommandType.StoredProcedure));
        }

        // TODO: 
        // Split Dapper calls into a new SQL Repository
        // Implement (bool Ok, string Message) IsHealthOk() on that new class
        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishingAreaRepository)
            };

            (bool Ok, string Message) sqlConnectionHealth = await IsConnectionHealthOk();

            health.Dependencies.Add(
                new DependencyHealth 
                { 
                    HealthOk = sqlConnectionHealth.Ok, 
                    DependencyName = typeof(PublishingAreaRepository).GetFriendlyName(), 
                    Message = sqlConnectionHealth.Message 
                });

            return health;
        }

        private async Task<(bool Ok, string Message)> IsConnectionHealthOk()
        {
            try
            {
                using SqlConnection connection = GetSqlConnection();
                await connection.OpenAsync();

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }

        private SqlConnection GetSqlConnection()
        {
            return new SqlConnection(_fDZSqlStorageSettings.ConnectionString);
        }
    }
}
