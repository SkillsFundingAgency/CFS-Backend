﻿using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Validators
{
    public class CalculationCreateModelValidator : AbstractValidator<CalculationCreateModel>
    {
        private readonly ICalculationsRepository _calculationRepository;
        private readonly IPreviewService _previewService;
        private readonly ISpecificationRepository _specificationRepository;

        public CalculationCreateModelValidator(
            ICalculationsRepository calculationRepository,
            IPreviewService previewService,
            ISpecificationRepository specificationRepository)
        {
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));
            Guard.ArgumentNotNull(previewService, nameof(previewService));
            Guard.ArgumentNotNull(specificationRepository, nameof(specificationRepository));

            _calculationRepository = calculationRepository;
            _previewService = previewService;
            _specificationRepository = specificationRepository;

            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .NotNull()
              .WithMessage("Null or empty specification id provided.");


            RuleFor(model => model.Name)
              .Custom((name, context) => {
                  CalculationCreateModel calculationCreateModel = context.ParentContext.InstanceToValidate as CalculationCreateModel;
                  if (string.IsNullOrWhiteSpace(calculationCreateModel.Name))
                  {
                      context.AddFailure("Null or empty calculation name provided.");
                  }
                  else
                  {
                      if (!string.IsNullOrWhiteSpace(calculationCreateModel.SpecificationId))
                      {
                          Calculation calculation = _calculationRepository.GetCalculationsBySpecificationIdAndCalculationName(calculationCreateModel.SpecificationId, calculationCreateModel.Name).Result;

                          if (calculation != null)
                              context.AddFailure($"A calculation already exists with the name: '{calculationCreateModel.Name}' for this specification");
                      }
                  }
              });

            RuleFor(model => model.SourceCode)
             .Custom((sc, context) => {
                 CalculationCreateModel calculationCreateModel = context.ParentContext.InstanceToValidate as CalculationCreateModel;
                 if (string.IsNullOrWhiteSpace(calculationCreateModel.SourceCode))
                 {
                     context.AddFailure("Null or empty source code provided.");
                 }
                 else
                 {
                     PreviewRequest previewRequest = new PreviewRequest
                     {
                         SpecificationId = calculationCreateModel.SpecificationId,
                         CalculationId = calculationCreateModel.Id,
                         Name = calculationCreateModel.Name,
                         SourceCode = calculationCreateModel.SourceCode
                     };

                     IActionResult result = _previewService.Compile(previewRequest).Result;

                     OkObjectResult okObjectResult = result as OkObjectResult;

                     PreviewResponse response = okObjectResult.Value as PreviewResponse;

                     if (response != null)
                     {
                         if (!response.CompilerOutput.CompilerMessages.IsNullOrEmpty())
                         {
                             context.AddFailure("There are errors in the source code provided");
                         }
                     }
                     
                 }
             });

            RuleFor(model => model.FundingStreamId)
             .Custom((fs, context) => {
                 CalculationCreateModel calculationCreateModel = context.ParentContext.InstanceToValidate as CalculationCreateModel;
                 if (string.IsNullOrWhiteSpace(calculationCreateModel.FundingStreamId))
                 {
                     context.AddFailure("Null or empty source code provided.");
                 }
                 else
                 {
                     Models.Specs.SpecificationSummary specificationSummary =  _specificationRepository.GetSpecificationSummaryById(calculationCreateModel.SpecificationId).Result;

                     if (specificationSummary == null)
                     {
                         context.AddFailure("Failed to find specification for provided specification id.");
                     }
                     else
                     {
                         bool specContainsFundingStream = specificationSummary.FundingStreams.Any(m => m.Id == calculationCreateModel.FundingStreamId);

                         if (!specContainsFundingStream)
                         {
                             context.AddFailure("The funding stream id provided is not associated with the provided specification.");
                         }
                     }
                 }
             });
        }
    }
}