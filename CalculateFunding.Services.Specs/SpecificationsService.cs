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
using CalculateFunding.Functions.Common.Interfaces.Logging;
using System.Net;
using System;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specifcationsRepository;
        private readonly ILoggingService _logs;
        private readonly IValidator<PolicyCreateModel> _policyCreateModelValidator;

        public SpecificationsService(IMapper mapper, 
            ISpecificationsRepository specifcationsRepository, ILoggingService logs, IValidator<PolicyCreateModel> policyCreateModelValidator)
        {
            _mapper = mapper;
            _specifcationsRepository = specifcationsRepository;
            _logs = logs;
            _policyCreateModelValidator = policyCreateModelValidator;
        }

        public async Task<IActionResult> GetSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
                return new BadRequestObjectResult("Null or empty specification Id provided");

            Specification specification = await _specifcationsRepository.GetSpecificationById(specificationId);

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

            IEnumerable<Specification> specifications = await _specifcationsRepository.GetSpecificationsByQuery(m => m.Name.ToLower() == specificationName.ToLower());

            if (!specifications.Any())
                return new NotFoundResult();

            return new OkObjectResult(specifications.FirstOrDefault());
        }

        public async Task<IActionResult> GetPolicyByName(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyGetModel model = JsonConvert.DeserializeObject<PolicyGetModel>(json);

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
                return new BadRequestObjectResult("Null or empty specification id provided");

            if (string.IsNullOrWhiteSpace(model.Name))
                return new BadRequestObjectResult("Null or empty policy name provided");

            Specification specification = await _specifcationsRepository.GetSpecificationById(model.SpecificationId);

            if (specification == null)
                return new NotFoundResult();

            Policy policy = specification.GetPolicyByName(model.Name);

            if (policy != null)
                return new OkObjectResult(policy);

            return new NotFoundResult();
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

        public async Task<IActionResult> CreatePolicy(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyCreateModel createModel = JsonConvert.DeserializeObject<PolicyCreateModel>(json);

            if (createModel == null)
                return new BadRequestObjectResult("Null policy create model provided");
            
            var validationResult = await _policyCreateModelValidator.ValidateAsync(createModel);

            if (!validationResult.IsValid)
            {
                ModelStateDictionary modelStateDictionary = new ModelStateDictionary();

                validationResult.PopulateModelState(modelStateDictionary);

                return new BadRequestObjectResult(modelStateDictionary);
            }

            //Policy existingPolicy = await GetPolicyByName()
            Specification specification = await _specifcationsRepository.GetSpecificationById(createModel.SpecificationId);

            if (specification == null)
                return new NotFoundResult();

            Policy policy = _mapper.Map<Policy>(createModel);

            if (!string.IsNullOrWhiteSpace(createModel.ParentPolicyId))
            {
                Policy parentPolicy = specification.GetPolicy(createModel.ParentPolicyId);

                if (parentPolicy == null)
                    return new NotFoundResult();

                parentPolicy.SubPolicies = (parentPolicy.SubPolicies == null
                    ? new[] { policy }
                    : parentPolicy.SubPolicies.Concat(new[] { policy }));
            }
            else
            {
                specification.Policies = (specification.Policies == null
                   ? new[] { policy }
                   : specification.Policies.Concat(new[] { policy }));
            }

            var statusCode = await _specifcationsRepository.UpdateSpecification(specification);

            if(statusCode != HttpStatusCode.OK)
                 return new StatusCodeResult((int)statusCode);

            return new OkObjectResult(specification);
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

            var statusCode = await _specifcationsRepository.CreateSpecification(specification);

            if (statusCode != HttpStatusCode.OK)
                return new StatusCodeResult((int)statusCode);

            return new OkObjectResult(specification);
        }

    }


}
