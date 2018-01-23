using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Net;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        CosmosRepository _repository;

        public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public async Task<IEnumerable<AllocationLine>> GetAllocationLines()
        {
            var lines = new[]
            {
                new AllocationLine { Id = "YPE04", Name = "Teacher Threshold" },
                new AllocationLine { Id = "YPE05", Name = "Mainstreamed Grants" },
                new AllocationLine { Id = "YPE06", Name = "Start Up Grant Part a" },
                new AllocationLine { Id = "YPE07", Name = "Start Up Grant Part b Formulaic" },
                new AllocationLine { Id = "YPE08", Name = "Start Up Grant Part b Assessment" },
                new AllocationLine { Id = "YPE09", Name = "GAG Advances and Abatements" },
                new AllocationLine { Id = "YPE10", Name = "Standards Funds" },
                new AllocationLine { Id = "YPE11", Name = "Rates Relief" },
                new AllocationLine { Id = "YPE12", Name = "CTC Overall Grant" },
                new AllocationLine { Id = "YPE13", Name = "Pupil Led Factors" },
                new AllocationLine { Id = "YPE14", Name = "Other Factors" },
                new AllocationLine { Id = "YPE15", Name = "Exceptional Factors" },
                new AllocationLine { Id = "YPE16", Name = "Minimum Funding Guarantee (MFG)" },
                new AllocationLine { Id = "YPE18", Name = "Pre 16 High Needs" },
                new AllocationLine { Id = "YPE19", Name = "Hospital Provision" },
                new AllocationLine { Id = "YPE20", Name = "SEN LACSEG Adjustment" },
                new AllocationLine { Id = "YPE21", Name = "Allocation Protection" },
                new AllocationLine { Id = "YPE22", Name = "Risk Protection Arrangements (RPA)" },
                new AllocationLine { Id = "YPE23", Name = "Pupil Number Adjustment (PNA)" },
                new AllocationLine { Id = "YPE24", Name = "GAG Adjustment" },
                new AllocationLine { Id = "YPE25", Name = "POG Per Pupil Resources (PPR)" },
                new AllocationLine { Id = "YPE26", Name = "POG Leadership Diseconomies (LD)" },
                new AllocationLine { Id = "YPE27", Name = "LA Transfer Deficit Recovery" },


                new AllocationLine { Id = "YPA01", Name = "16-19 Low Level Learners Programme funding" },
                new AllocationLine { Id = "YPA02", Name = "16-19 Learner Responsive Low Level ALS" },
                new AllocationLine { Id = "YPA03", Name = "216-19 Learner Responsive High Level ALS" },

            };

            return lines;
        }

        public async Task<AllocationLine> GetAllocationLineById(string lineId)
        {
            var lines = await GetAllocationLines();

            return lines.FirstOrDefault(m => m.Id == lineId);
        }

        public async Task<AcademicYear> GetAcademicYearById(string academicYearId)
        {
           var years = new[]
           {
                new AcademicYear { Id = "1819", Name = "2018/19" },
                new AcademicYear { Id = "1718", Name = "2017/18" },
                new AcademicYear { Id = "1617", Name = "2016/17" },
            };

            var academicYear = years.FirstOrDefault(m => m.Id == academicYearId);

            //var academicYear = _repository.Query<AcademicYear>().FirstOrDefault(m => m.Id == academicYearId);

            return academicYear;
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

        public Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query)
        {
            var specifications = _repository.Query<Specification>().Where(query);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<AcademicYear>> GetAcademicYears()
        {
            var academicYears = _repository.Query<AcademicYear>();

            return Task.FromResult(academicYears.ToList().AsEnumerable());
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
    }
}
