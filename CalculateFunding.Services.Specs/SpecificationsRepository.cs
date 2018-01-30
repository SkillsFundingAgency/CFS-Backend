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
        CosmosRepository _repository;

	    private const string FundingStreamData = @"
gag	General Annual Grant
YPLRG	16-19 Bursaries Allocation - 11/12
YPLRA	16-19 Learner Responsive
YPLRB	19-24 Learners with High Cost ALS
YPLRE	Academies General Annual Grant
YPLRP	DSG
YPLRD	Academies 16-18
YPLRC	School Sixth Form
YPLRN	Non Formula Funded Activity
YPLRJ	19+ continuing learners (excluding high cost)
YPLRM	Non Formula Funded Activity Academies
";
	    private const string AllocationLineData = @"
YPG01	16-19 Bursaries - all providers 11/12
YPA01	16 -19 Low Level Learners Programme funding
YPB03	19-24 Lnrs with High Cost ALS Programme Funding
YPE01	School Budget Share
YPE13	Pupil Led Factors
YPE02	Education Services Grant
YPE03	Insurance
YPP01	DSG Allocations
YPA02	16-19 Learner Responsive Low Level ALS
YPA03	16-19 Learner Responsive High Level ALS
YPE16	Minimum Funding Guarantee (MFG)
YPE09	GAG Advances and Abatements
YPE04	Teacher Threshold
YPE05	Mainstreamed Grants
YPE06	Start Up Grant Part a
YPE07	Start Up Grant Part b Formulaic
YPE08	Start Up Grant Part b Assessment
YPE10	Standards Funds
YPE11	Rates Relief
YPE12	CTC Overall Grant
YPE14	Other Factors
YPE15	Exceptional Factors
YPE18	Pre 16 High Needs
YPE19	Hospital Provision
YPA12	16-19 Bursary Funds
YPA14	16-19 Total Programme Funding
YPA15	16-19 Formula Protection Funding
YPA16	16-19 High Needs Element 2
YPA09	16-19 Transitional Protection
YPD05	Academy Transitional Protection
YPD13	Academies Total Programme funding
YPD14	Academies Formula Protection Funding
YPC12	SSF Bursary Funds
YPC13	SSF Total Programme funding
YPC14	SSF Formula Protection Funding
YPC15	SSF High Needs Element 2
YPD12	Academies Bursary Funding
YPD15	Academies High Needs Element 2
YPA23	16-19 Free Meals in FE
YPN40	NMF Pre-16 Funding (NMSS)
YPD16	Academies Free Meals
YPN30	Cadets Vocational Programme
YPN42	SSF Free Meals
YPN43	Core Maths Early Adopters SSF
YPN44	Pre-16 Universal Infant Free School Meals
YPN45	Primary PE and Sport Premium
YPN16	Residential Bursary Fund
YPE20	SEN LACSEG Adjustment
YPE21	Allocation Protection
YPE22	Risk Protection Arrangements (RPA)
YPE23	Pupil Number Adjustment (PNA)
YPE24	GAG Adjustment
YPE25	POG Per Pupil Resources (PPR)
YPE26	POG Leadership Diseconomies (LD)
YPE27	LA Transfer Deficit Recovery
YPN46	Residential Support Scheme
YPJ01	19+ continuing Lnrs programme funding
YPA17	HNS Transitional Protection
YPD17	Core Maths Early Adopters Academies
YPN41	Core Maths Early Adopters 16-19
YPN25	Closing Schools
YPE28	De-Delegation retained by LA
YPE29	Pre16 PNA (Pupil Number Adjustment)
YPE30	Post16 PNA (Pupil Number Adjustment)
YPD01	Academy Programme Funds
YPA20	14-16 Programme Funding
YPA21	14-16 Pupil Premium
YPA22	14-16 service child premium
YPN47	19+ Cont Learners in SFC (Student Support)
YPN48	Area Review Transition Grant
YPE31	PFI Factor Funding
YPA04	16-19 high level learners programme Funding
YPD02	Academy ALS
YPD07	Additional Funding
YPD08	Academy Teacher's Pension
YPE32	Transitional Funding
YPN49	16+ Factor Removal (DSG) ACAD
YPE33	Condition Improvement Fund loans
YPA06	Additional Funding 
YPA07	Diploma Programme Funding
YPA08	Diploma ALS Funding
YPC16	Maintained Special SSF E2
YPC17	Maintained Special SSF Student Support
YPA24	16-19 SPI Element 1
YPE34	Popular Growth
YPN50	16+ Factor Removal (DSG) SSF
YPM01	Residential Bursary Fund ACAD
YPM02	Residential Support Scheme ACAD
YPM03	16+ Factor Removal (DSG) ACAD
YPN51	Alternative completions â€“ AASE sporting excellence
YPN52	Alternative completions â€“ sea fishing
YPN53	Alternative completions â€“ creative sector
YPM05	19+ Cont Learners in ACAD student support
YPA10	Young Parents To Be
YPA13	16-19 funding (apprenticeships pilot)
YPA18	16-19 Reconciliation Recovery
YPM06	Pre-16 Universal Infant Free School Meals
YPM08	Area Review Transition Grant
YPM09	Alternative completions â€“ AASE sporting excellence
YPM10	Alternative completions â€“ sea fishing
YPM11	Alternative completions â€“ creative sector
YPM13	19+ continuing learners ACAD
YPM04	NMF Pre-16 Funding (NMSS) ACAD
YPM07	Primary PE and Sport Premium

";

		public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
        }

        public async Task<IEnumerable<AllocationLine>> GetAllocationLines()
        {
	        var lines = new List<AllocationLine>();
	        using (var reader = new StringReader(AllocationLineData))
	        {
		        var line = reader.ReadLine().Trim();
		        var split = line.Split('\t');
		        if (split.Length == 2 && string.IsNullOrEmpty(split[0]) && string.IsNullOrEmpty(split[1]))
		        {
			        lines.Add(new AllocationLine{ Id = split[0].Trim(), Name = split[1].Trim()});
				}
	        }

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



			var fundingStream = (await GetFundingStreams()).FirstOrDefault(m => m.Id == fundingStreamId);
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

			var fundingStreams = new List<FundingStream>();
	        using (var reader = new StringReader(FundingStreamData))
	        {
		        var line = reader.ReadLine().Trim();
		        var split = line.Split('\t');
		        if (split.Length == 2 && string.IsNullOrEmpty(split[0]) && string.IsNullOrEmpty(split[1]))
		        {
			        fundingStreams.Add(new FundingStream { Id = split[0].Trim(), Name = split[1].Trim() });
		        }
	        }
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
