using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionsMetadataRepository : IProviderVersionsMetadataRepository, IHealthChecker
    {
        readonly CosmosRepository _repository;

        public ProviderVersionsMetadataRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _repository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionsMetadataRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _repository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task<HttpStatusCode> UpsertProviderVersionByDate(ProviderVersionByDate providerVersionByDate)
        {
            Guard.ArgumentNotNull(providerVersionByDate, nameof(providerVersionByDate));

            providerVersionByDate.Id = string.Concat(providerVersionByDate.Year, providerVersionByDate.Month.ToString("00"), providerVersionByDate.Day.ToString("00"));

            return await _repository.UpsertAsync(providerVersionByDate);
        }

        public async Task<HttpStatusCode> UpsertMaster(MasterProviderVersion providerVersionMetadataViewModel)
        {
            Guard.ArgumentNotNull(providerVersionMetadataViewModel, nameof(providerVersionMetadataViewModel));

            return await _repository.UpsertAsync(providerVersionMetadataViewModel);
        }

        public async Task<MasterProviderVersion> GetMasterProviderVersion()
        {
            IEnumerable<DocumentEntity<MasterProviderVersion>> masterProviderVersion = await _repository.GetAllDocumentsAsync<MasterProviderVersion>(query: m => m.Content.Id == "master");
            return masterProviderVersion.Select(x => x.Content).FirstOrDefault();
        }

        public async Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day)
        {
            IEnumerable<DocumentEntity<ProviderVersionByDate>> providerVersionsByDate = await _repository.GetAllDocumentsAsync<ProviderVersionByDate>(query: m => m.Content.Id == string.Concat(year, month.ToString("00"), day.ToString("00")));
            return providerVersionsByDate.Select(x => x.Content).FirstOrDefault();
        }
    }
}
