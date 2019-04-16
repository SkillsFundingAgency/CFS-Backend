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
        readonly CosmosRepository _repository;

        public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _repository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task<Period> GetPeriodById(string periodId)
        {
            DocumentEntity<Period> period = await _repository.ReadAsync<Period>(periodId);

            if (period == null || period.Content == null)
            {
                return null;
            }

            return period.Content;
        }

        async public Task<FundingStream> GetFundingStreamById(string fundingStreamId)
        {
            IEnumerable<FundingStream> fundingStreams = await GetFundingStreams(m => m.Id == fundingStreamId);

            return fundingStreams.FirstOrDefault();
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null)
        {
            var fundingStreams = query == null ? _repository.Query<FundingStream>() : _repository.Query<FundingStream>().Where(query);

            return Task.FromResult(fundingStreams.AsEnumerable());
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

        async public Task<Specification> GetSpecificationByQuery(Expression<Func<Specification, bool>> query)
        {
            return (await GetSpecificationsByQuery(query)).FirstOrDefault();
        }

        public async Task<IEnumerable<Specification>> GetSpecifications()
        {
            IEnumerable<DocumentEntity<Specification>> results = await _repository.GetAllDocumentsAsync<Specification>();

            return results.Select(c => c.Content);
        }

        public Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query = null)
        {
            var specifications = query == null ? _repository.Query<Specification>() : _repository.Query<Specification>().Where(query);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(string fundingPeriodId, string fundingStreamId)
        {
            IQueryable<Specification> specificationsQuery =
                _repository.Query<Specification>()
                    .Where(m => m.Current.FundingPeriod.Id == fundingPeriodId && (m.Current.PublishStatus == PublishStatus.Approved || m.Current.PublishStatus == PublishStatus.Updated));

            //This needs fixing either after doc db client uypgrade or add some sql  as cant use Any() to check funding stream ids    

            IEnumerable<Specification> specifications = specificationsQuery.AsEnumerable().ToList();

            specifications = specifications.Where(m => m.Current.FundingStreams.Any(fs => fs.Id == fundingStreamId));

            return Task.FromResult(specifications);
        }

        public Task<IEnumerable<Specification>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            IQueryable<Specification> specificationsQuery =
              _repository.Query<Specification>()
                  .Where(m => m.IsSelectedForFunding && m.Current.FundingPeriod.Id == fundingPeriodId);

            IEnumerable<Specification> specifications = specificationsQuery.AsEnumerable().ToList();

            specifications = specifications.Where(m => m.Current.FundingPeriod.Id == fundingPeriodId);

            return Task.FromResult(specifications);
        }

        public Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql)
        {
            var specifications = _repository.RawQuery<T>(sql);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<Period>> GetPeriods()
        {
            var fundingPeriods = _repository.Query<Period>();

            return Task.FromResult(fundingPeriods.ToList().AsEnumerable());
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
            {
                return null;
            }

            return specification.Current.GetCalculations().FirstOrDefault(m => string.Equals(m.Name.RemoveAllSpaces(), calculationName.RemoveAllSpaces(), StringComparison.CurrentCultureIgnoreCase));
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
            {
                return null;
            }

            return specification.Current.GetCalculations().FirstOrDefault(m => m.Id == calculationId);
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
            {
                return null;
            }

            return specification.Current.GetCalculations();
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyName(string specificationId, string policyByName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
            {
                return null;
            }

            return specification.Current.GetPolicyByName(policyByName);
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyId(string specificationId, string policyId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
            {
                return null;
            }

            return specification.Current.GetPolicy(policyId);
        }

        public Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream)
        {
            return _repository.UpsertAsync<FundingStream>(fundingStream);
        }

        public Task SavePeriods(IEnumerable<Period> periods)
        {
            return _repository.BulkUpsertAsync<Period>(periods.ToList());
        }

        public Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId)
        {
            return Task.FromResult(_repository.QueryDocuments<Specification>().Where(c => c.Id == specificationId).AsEnumerable().FirstOrDefault());
        }
    }
}
