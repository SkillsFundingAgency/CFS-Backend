using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common.Extensions;
using Newtonsoft.Json;
using AutoMapper;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Models;
using System.Linq;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specifcationsRepository;

        public SpecificationsService(IMapper mapper, ISpecificationsRepository specifcationsRepository)
        {
            _mapper = mapper;
            _specifcationsRepository = specifcationsRepository;
        }

        public async Task<IActionResult> GetSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
                return new BadRequestObjectResult("Null or empty specification Id provided");

            Specification specification = await _specifcationsRepository.GetSpecification(specificationId);

            if (specification == null)
                return new NotFoundResult();

            return new OkObjectResult(specification);
        }

        public async Task<IActionResult> GetSpecificationByAcademicYearId(HttpRequest request)
        {
            request.Query.TryGetValue("academicYearId", out var yearId);

            var academicYearId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(academicYearId))
                return new BadRequestObjectResult("Null or empty academicYearId provided");

            IEnumerable<Specification> specifications = await _specifcationsRepository.GetSpecificationsByQuery(m => m.AcademicYear.Id == academicYearId);

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationByName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationName", out var specName);

            var specificationName = specName.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specName))
                return new BadRequestObjectResult("Null or empty specification name provided");

            IEnumerable<Specification> specifications = await _specifcationsRepository.GetSpecificationsByQuery(m => m.Name == specificationName);

            if (!specifications.Any())
                return new NotFoundResult();

            return new OkObjectResult(specifications.FirstOrDefault());
        }

        public async Task<IActionResult> GetAcademicYears(HttpRequest request)
        {
            IEnumerable<AcademicYear> academicYears = await _specifcationsRepository.GetAcademicYears();
            return new OkObjectResult(academicYears);
        }
        public async Task<IActionResult> GetFundingStreams(HttpRequest request)
        {
            IEnumerable<FundingStream> fundingStreams = await _specifcationsRepository.GetFundingStreams();
            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> CreateSpecification(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationCreateModel createModel = JsonConvert.DeserializeObject<SpecificationCreateModel>(json);

            AcademicYear academicYear = await _specifcationsRepository.GetAcademicYearById(createModel.AcademicYearId);

            FundingStream fundingStream = await _specifcationsRepository.GetFundingStreamById(createModel.FundingStreamId);

            Specification specification = _mapper.Map<Specification>(createModel);

            specification.AcademicYear = new Reference(academicYear.Id, academicYear.Name);

            specification.FundingStream = new Reference(fundingStream.Id, fundingStream.Name);

            await _specifcationsRepository.CreateSpecification(specification);

            //var restMethods = new RestCommandMethods<Specification, SpecificationCommand>("spec-events");

            return new OkObjectResult(specification);
        }

       
    }


}
