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

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        readonly CosmosRepository _repository;

		public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public Task<AcademicYear> GetAcademicYearById(string academicYearId)
        {
           var years = new[]
           {
                new AcademicYear { Id = "1819", Name = "2018/19" },
                new AcademicYear { Id = "1718", Name = "2017/18" },
                new AcademicYear { Id = "1617", Name = "2016/17" },
            };

            var academicYear = years.FirstOrDefault(m => m.Id == academicYearId);

            //var academicYear = _repository.Query<AcademicYear>().FirstOrDefault(m => m.Id == academicYearId);

            return Task.FromResult(academicYear);
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

        public Task<HttpStatusCode> CreateSpecification(Specification specification)
        {
            return _repository.CreateAsync(specification);
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

        public Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql)
        {
            var specifications = _repository.RawQuery<T>(sql);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<AcademicYear>> GetAcademicYears()
        {
            var academicYears = _repository.Query<AcademicYear>();

            return Task.FromResult(academicYears.ToList().AsEnumerable());
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.GetCalculations().FirstOrDefault(m => m.Name == calculationName);
        }

        async public Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.GetCalculations().FirstOrDefault(m => m.Id == calculationId);
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.GetCalculations();
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyName(string specificationId, string policyByName)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.GetPolicyByName(policyByName);
        }

        async public Task<Policy> GetPolicyBySpecificationIdAndPolicyId(string specificationId, string policyId)
        {
            var specification = await GetSpecificationById(specificationId);
            if (specification == null)
                return null;

            return specification.GetPolicy(policyId);
        }

        public Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream)
        {
            return _repository.CreateAsync<FundingStream>(fundingStream);
        }
    }
}
