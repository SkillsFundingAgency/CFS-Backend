using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        readonly CosmosRepository _repository;

        public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public async Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId)
        {
            DocumentEntity<FundingPeriod> fundingPeriod = await _repository.ReadAsync<FundingPeriod>(fundingPeriodId);

            if(fundingPeriod == null || fundingPeriod.Content == null)
            {
                return null;
            }

            return fundingPeriod.Content;
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

            IEnumerable<Specification> specifications = specificationsQuery.AsEnumerable().Where(m => m.Current.FundingStreams.Any(fs => fs.Id == fundingStreamId));

            //specifications = specifications.Where(m => m.Current.FundingStreams.Any(fs => fs.Id == fundingStreamId));

            return Task.FromResult(specifications);   
        }

        public Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql)
        {
            var specifications = _repository.RawQuery<T>(sql);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<FundingPeriod>> GetFundingPeriods()
        {
            var fundingPeriods = _repository.Query<FundingPeriod>();

            return Task.FromResult(fundingPeriods.ToList().AsEnumerable());
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.Current.GetCalculations().FirstOrDefault(m => m.Name == calculationName);
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.Current.GetCalculations().FirstOrDefault(m => m.Id == calculationId);
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.Current.GetCalculations();
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyName(string specificationId, string policyByName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.Current.GetPolicyByName(policyByName);
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyId(string specificationId, string policyId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.Current.GetPolicy(policyId);
        }

        public Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream)
        {
            return _repository.CreateAsync<FundingStream>(fundingStream);
        }

        public Task SaveFundingPeriods(IEnumerable<FundingPeriod> fundingPeriods)
        {
            return _repository.BulkCreateAsync<FundingPeriod>(fundingPeriods.ToList());
        }

        public Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId)
        {
            return Task.FromResult(_repository.QueryDocuments<Specification>().Where(c => c.Id == specificationId).AsEnumerable().FirstOrDefault());
        }
    }
}
