using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        CosmosRepository _repository;

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
            var fundingStreams = new[]
           {
                new FundingStream
                {
                    Id = "gag",
                    Name = "General Annual Grant"
                }
            };

            var fundingStream = fundingStreams.FirstOrDefault(m => m.Id == fundingStreamId);
            //var fundingStream = await _repository.SingleOrDefaultAsync<FundingStream>(m => m.Id == fundingStreamId);

            return fundingStream;

        }

        public Task CreateSpecification(Specification specification)
        {
            return _repository.CreateAsync(specification);
        }

        public async Task<Specification> GetSpecification(string specificationId)
        {
            var documentEntity = await _repository.ReadAsync<Specification>(specificationId);

            return documentEntity.Content;
        }

        public Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query)
        {
            var specifications = _repository.Query<Specification>().Where(query);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<AcademicYear>> GetAcademicYears()
        {
            var academeicYears = _repository.Query<AcademicYear>();

            return Task.FromResult(academeicYears.AsEnumerable());
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            //var fundingStreams = _repository.Query<FundingStream>();
            //return Task.FromResult(fundingStreams.AsEnumerable());

            var fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "gag",
                    Name = "General Annual Grant"
                }
            };

            return Task.FromResult(fundingStreams.AsEnumerable());
        }
    }
}
