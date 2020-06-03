using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.ApplicationInsights.Common;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionsMetadataRepository : IProviderVersionsMetadataRepository, IHealthChecker
    {
        readonly ICosmosRepository _cosmos;

        public ProviderVersionsMetadataRepository(ICosmosRepository cosmosCosmos)
        {
            Guard.ArgumentNotNull(cosmosCosmos, nameof(cosmosCosmos));

            _cosmos = cosmosCosmos;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmos.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionsMetadataRepository)
            };
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = Ok,
                DependencyName = _cosmos.GetType().GetFriendlyName(),
                Message = Message
            });

            return Task.FromResult(health);
        }

        public async Task<HttpStatusCode> UpsertProviderVersionByDate(ProviderVersionByDate providerVersionByDate)
        {
            Guard.ArgumentNotNull(providerVersionByDate, nameof(providerVersionByDate));

            providerVersionByDate.Id = $"{providerVersionByDate.Year}{providerVersionByDate.Month:00}{providerVersionByDate.Day:00}";

            return await _cosmos.UpsertAsync(providerVersionByDate);
        }

        public async Task<HttpStatusCode> UpsertMaster(MasterProviderVersion providerVersionMetadataViewModel)
        {
            Guard.ArgumentNotNull(providerVersionMetadataViewModel, nameof(providerVersionMetadataViewModel));

            return await _cosmos.UpsertAsync(providerVersionMetadataViewModel);
        }

        public async Task<HttpStatusCode> CreateProviderVersion(ProviderVersionMetadata providerVersion)
        {
            Guard.ArgumentNotNull(providerVersion, nameof(providerVersion));

            return await _cosmos.CreateAsync(providerVersion);
        }

        public async Task<MasterProviderVersion> GetMasterProviderVersion()
        {
            IEnumerable<DocumentEntity<MasterProviderVersion>> masterProviderVersion = await _cosmos.GetAllDocumentsAsync<MasterProviderVersion>(query: m => 
                m.Content.Id == "master");
            return masterProviderVersion.Select(x => x.Content).FirstOrDefault();
        }

        public async Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day)
        {
            IEnumerable<DocumentEntity<ProviderVersionByDate>> providerVersionsByDate = await _cosmos.GetAllDocumentsAsync<ProviderVersionByDate>(query: m => 
                m.Content.Id == $"{year}{month:00}{day:00}");
            return providerVersionsByDate.Select(x => x.Content).FirstOrDefault();
        }
        
        public async Task<CurrentProviderVersion> GetCurrentProviderVersion(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            
            return (await _cosmos.GetAllDocumentsAsync<CurrentProviderVersion>(query: document =>
                document.Content.Id == $"Current_{fundingStreamId}"))?
                .Select(document => document.Content)
                .SingleOrDefault();
        }
        
        public async Task<HttpStatusCode> UpsertCurrentProviderVersion(CurrentProviderVersion currentProviderVersion)
        { 
            Guard.ArgumentNotNull(currentProviderVersion, nameof(currentProviderVersion));

            return await _cosmos.UpsertAsync(currentProviderVersion);
        }

        public async Task<IEnumerable<ProviderVersionMetadata>> GetProviderVersions(string fundingStream)
        {
            IEnumerable<DocumentEntity<ProviderVersionMetadata>> providerVersions = await _cosmos.GetAllDocumentsAsync<ProviderVersionMetadata>(query: m => 
                m.Content.FundingStream == fundingStream);
            return providerVersions?.Select(x => x.Content);
        }

        public async Task<bool> Exists(string name, string providerVersionTypeString, int version, string fundingStream)
        {
            IEnumerable<DocumentEntity<ProviderVersion>> results = await _cosmos
                    .GetAllDocumentsAsync<ProviderVersion>(query: m => m.Content.ProviderVersionTypeString == providerVersionTypeString
                                                                         && m.Content.Version == version
                                                                         && m.Content.FundingStream == fundingStream);

            //HACK Ideally this would be done in the previous Linq query, but the operation needs to be case insensitive and the cosmos Linq provider doesn't support .ToLowerInvariant()
            return results.Any(r => r.Content.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public async Task<ProviderVersionMetadata> GetProviderVersionMetadata(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            string cosmosKey = $"providerVersion-{providerVersionId}";

            IEnumerable<DocumentEntity<ProviderVersionMetadata>> providerVersions = await _cosmos.GetAllDocumentsAsync<ProviderVersionMetadata>(query: m => m.Id == cosmosKey);
            return providerVersions?.Select(x => x.Content).FirstOrDefault();
        }
    }
}
