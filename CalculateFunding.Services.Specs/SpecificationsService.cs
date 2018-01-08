using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common.Extensions;
using Newtonsoft.Json;
using AutoMapper;
using CalculateFunding.Functions.Common;
using Microsoft.Extensions.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Models;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specifcationsRepository;

        public SpecificationsService(IMapper mapper, ILogger logger, ISpecificationsRepository specifcationsRepository)
        {
            _mapper = mapper;
            _logger = logger;
            _specifcationsRepository = specifcationsRepository;
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
