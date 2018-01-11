using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationCreateModelValidator : AbstractValidator<SpecificationCreateModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;

        public SpecificationCreateModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the specification");

            RuleFor(model => model.AcademicYearId)
               .NotEmpty()
               .WithMessage("Null or empty academic year id provided");

            RuleFor(model => model.FundingStreamId)
              .NotEmpty()
              .WithMessage("You must select at least one funding stream");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique specification name")
               .Custom((name, context) => {
                   PolicyCreateModel model = context.ParentContext.InstanceToValidate as PolicyCreateModel;
                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Name.ToLower() == model.Name.ToLower()).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });
        }
    }
}
