using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Validators
{
    public class CalculationEditModelValidator : AbstractValidator<CalculationEditModel>
    {
        private readonly IPreviewService _previewService;
        private readonly ICalculationsRepository _calculationRepository;

        public CalculationEditModelValidator(
            IPreviewService previewService,
            ICalculationsRepository calculationRepository)
        {
            Guard.ArgumentNotNull(previewService, nameof(previewService));
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));

            _previewService = previewService;
            _calculationRepository = calculationRepository;

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .NotNull()
              .WithMessage("Null or empty specification id provided.");

            RuleFor(model => model.ValueType)
             .NotNull()
             .WithMessage("Null value type was provided.");

            RuleFor(model => model.CalculationId)
             .NotEmpty()
             .NotNull()
             .WithMessage("Null or empty calculation id provided.");

            RuleFor(model => model.Name)
             .Custom((name, context) =>
             {
                 CalculationEditModel calculationEditModel = context.ParentContext.InstanceToValidate as CalculationEditModel;
                 if (string.IsNullOrWhiteSpace(calculationEditModel.Name))
                 {
                     context.AddFailure("Null or empty calculation name provided.");
                 }
                 else
                 {
                     if (!string.IsNullOrWhiteSpace(calculationEditModel.SpecificationId))
                     {
                         Calculation calculation = _calculationRepository.GetCalculationsBySpecificationIdAndCalculationName(calculationEditModel.SpecificationId, calculationEditModel.Name).Result;

                         if (calculation != null)
                             context.AddFailure($"A calculation already exists with the name: '{calculationEditModel.Name}' for this specification");
                     }
                 }
             });

            RuleFor(model => model.SourceCode)
             .Custom((sc, context) =>
             {
                 CalculationEditModel calculationEditModel = context.ParentContext.InstanceToValidate as CalculationEditModel;
                 if (string.IsNullOrWhiteSpace(calculationEditModel.SourceCode))
                 {
                     context.AddFailure("Null or empty source code provided.");
                 }
                 else
                 {
                     PreviewRequest previewRequest = new PreviewRequest
                     {
                         SpecificationId = calculationEditModel.SpecificationId,
                         CalculationId = calculationEditModel.CalculationId,
                         Name = calculationEditModel.Name,
                         SourceCode = calculationEditModel.SourceCode
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

        }
    }
}
