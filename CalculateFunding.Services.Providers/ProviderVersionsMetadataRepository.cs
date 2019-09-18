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

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionsMetadataRepository : IProviderVersionsMetadataRepository, IHealthChecker
    {
        readonly ICosmosRepository _repository;

        public ProviderVersionsMetadataRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _repository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionsMetadataRepository)
            };
            health.Dependencies.Add(new DependencyHealth
                {
                    HealthOk = Ok,
                    DependencyName = _repository.GetType().GetFriendlyName(),
                    Message = Message
                });

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

        public async Task<HttpStatusCode> CreateProviderVersion(ProviderVersionMetadata providerVersion)
        {
            Guard.ArgumentNotNull(providerVersion, nameof(providerVersion));

            return await _repository.CreateAsync(providerVersion);
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

        public async Task<IEnumerable<ProviderVersionMetadata>> GetProviderVersions(string fundingStream)
        {
            IEnumerable<DocumentEntity<ProviderVersionMetadata>> providerVersions = await _repository.GetAllDocumentsAsync<ProviderVersionMetadata>(query: m => m.Content.FundingStream == fundingStream);
            return providerVersions?.Select(x => x.Content);
        }

        public async Task<bool> Exists(string name, string providerVersionTypeString, int version, string fundingStream)
        {
            IEnumerable<DocumentEntity<ProviderVersion>> results = await _repository
                    .GetAllDocumentsAsync<ProviderVersion>(query: m => m.Content.ProviderVersionTypeString == providerVersionTypeString
                                                                         && m.Content.Version == version
                                                                         && m.Content.FundingStream == fundingStream);

            //HACK Ideally this would be done in the previous Linq query, but the operation needs to be case insensitive and the cosmos Linq provider doesn't support .ToLowerInvariant()
            return results.Any(r => r.Content.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }
    }
}
