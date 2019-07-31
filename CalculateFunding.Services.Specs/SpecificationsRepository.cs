using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.Documents;

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
            var (Ok, Message) = await _repository.IsHealthOk();

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

        public async Task<Specification> GetSpecificationByQuery(Expression<Func<Specification, bool>> query)
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
            var specifications = query == null 
                ? _repository.Query<Specification>() 
                : _repository.Query<Specification>().Where(query);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(string fundingPeriodId, string fundingStreamId)
        {
            IQueryable<Specification> specificationsQuery =
                _repository.Query<Specification>()
                    .Where(m => m.Current.FundingPeriod.Id == fundingPeriodId && (m.Current.PublishStatus == PublishStatus.Approved || m.Current.PublishStatus == PublishStatus.Updated));

            //This needs fixing either after doc db client upgrade or add some sql as can't use Any() to check funding stream ids    

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

        [Obsolete]
        public Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql)
        {
            var specifications = _repository.RawQuery<T>(sql);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(SqlQuerySpec sqlQuerySpec)
        {
            var specifications = _repository.RawQuery<T>(sqlQuerySpec);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<Period>> GetPeriods()
        {
            var fundingPeriods = _repository.Query<Period>();

            return Task.FromResult(fundingPeriods.ToList().AsEnumerable());
        }

        public Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId)
        {
            return Task.FromResult(_repository.QueryDocuments<Specification>().Where(c => c.Id == specificationId).AsEnumerable().FirstOrDefault());
        }

        public async Task<TemplateMapping> GetTemplateMappingForSpecificationId(string specificationId)
        {
            TemplateMapping templateMapping = new TemplateMapping
            {
                TemplateMappingItems = new List<TemplateMappingItem>()
            };

            IEnumerable<Calculation> calculations = null; // await GetCalculationsBySpecificationId(specificationId);

            templateMapping.TemplateMappingItems.AddRange(
                calculations
                    .Select(calculation => new TemplateMappingItem
                    {
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = calculation.Name,
                        CalculationSpecificationId = specificationId
                    }));

            return templateMapping;
        }
    }
}
