using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _repository;

        public SpecificationsRepository(ICosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var (Ok, Message) = _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _repository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public Task<DocumentEntity<Specification>> CreateSpecification(Specification specification)
        {
            return _repository.CreateDocumentAsync(specification);
        }

        public Task<HttpStatusCode> UpdateSpecification(Specification specification)
        {
            return _repository.UpdateAsync(specification);
        }

        public Task<Specification> GetSpecificationById(string specificationId)
        {
            return GetSpecificationByQuery(m => m.Id == specificationId);
        }

        public async Task<Specification> GetSpecificationByQuery(Expression<Func<DocumentEntity<Specification>, bool>> query)
        {
            return (await GetSpecificationsByQuery(query)).FirstOrDefault();
        }

        public async Task<IEnumerable<Specification>> GetSpecifications()
        {
            IEnumerable<DocumentEntity<Specification>> results = await _repository.GetAllDocumentsAsync<Specification>();

            return results.Select(c => c.Content);
        }

        public async Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<DocumentEntity<Specification>, bool>> query = null)
        {
            IEnumerable<Specification> specifications;
            if (query == null)
            {
                specifications = await _repository.Query<Specification>();
            }
            else
            {
                specifications = (await _repository.Query<Specification>(query));
            }

            return specifications;
        }

        public async Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(string fundingPeriodId, string fundingStreamId)
        {
            IEnumerable<Specification> specifications =
                await _repository.Query<Specification>(m => m.Content.Current.FundingPeriod.Id == fundingPeriodId && (m.Content.Current.PublishStatus == PublishStatus.Approved || m.Content.Current.PublishStatus == PublishStatus.Updated));

            return specifications.Where(m => m.Current.FundingStreams.Any(fs => fs.Id == fundingStreamId));
        }

        public async Task<IEnumerable<Specification>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            IEnumerable<Specification> specifications =
              await _repository.Query<Specification>(m => m.Content.IsSelectedForFunding && m.Content.Current.FundingPeriod.Id == fundingPeriodId);

            return specifications.Where(m => m.Current.FundingPeriod.Id == fundingPeriodId);
        }

        public async Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(CosmosDbQuery cosmosDbQuery)
        {
            var specifications = await _repository.RawQuery<T>(cosmosDbQuery);

            return specifications;
        }

        public async Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId)
        {
            return (await _repository
                .QueryDocuments<Specification>())
                .Where(c => c.Id == specificationId)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<string>> GetDistinctFundingStreamsForSpecifications()
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT DISTINCT value fs.id
                                 FROM specifications.content.current r
                                JOIN fs IN r.fundingStreams"
            };

            return await _repository.RawQuery<string>(cosmosDbQuery, 1);
        }
    }
}
