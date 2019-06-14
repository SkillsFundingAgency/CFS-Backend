﻿using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class CalculationCreateModelValidator : AbstractValidator<CalculationCreateModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ICalculationsRepository _calculationsRepository;

        public CalculationCreateModelValidator(ISpecificationsRepository specificationsRepository, ICalculationsRepository calculationsRepository)
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));

            _specificationsRepository = specificationsRepository;
            _calculationsRepository = calculationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the calculation");

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("Null or empty specification Id provided")
               .CustomAsync(async (name, context, cancellationToken) =>
               {
                   CalculationCreateModel model = context.ParentContext.InstanceToValidate as CalculationCreateModel;
                   if (!string.IsNullOrWhiteSpace(model.SpecificationId))
                   {
                       Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);
                       if (specification == null)
                       {
                           context.AddFailure("Specification not found");
                           return;
                       }
                   }
               });

            RuleFor(model => model.PolicyId)
               .NotEmpty()
               .WithMessage("You must select a policy or a sub policy");

            RuleFor(model => model.CalculationType)
                .IsInEnum()
                .WithMessage("You must specify a valid calculation type");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique calculation name")
               .Custom((name, context) =>
               {
                   CalculationCreateModel model = context.ParentContext.InstanceToValidate as CalculationCreateModel;

                   if (!_calculationsRepository.IsCalculationNameValid(model.SpecificationId, model.Name).Result)
                   {
                       context.AddFailure("Calculation with the same generated source code name already exists in this specification");
                   }
               });

            RuleFor(model => model.AllocationLineId)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    CalculationCreateModel model = context.ParentContext.InstanceToValidate as CalculationCreateModel;
                    bool checkForDuplicateBaselineCalcs = false;
                    bool requireAllocationLine = false;

                    if (model.CalculationType == CalculationType.Baseline)
                    {
                        checkForDuplicateBaselineCalcs = true;
                        requireAllocationLine = true;
                    }

                    if (model.CalculationType == CalculationType.Baseline || model.CalculationType == CalculationType.Number)
                    {
                        if (requireAllocationLine && string.IsNullOrWhiteSpace(model.AllocationLineId))
                        {
                            context.AddFailure("Select an allocation line to create this calculation specification");
                            return;
                        }

                        if (!string.IsNullOrWhiteSpace(model.SpecificationId) && !string.IsNullOrWhiteSpace(model.AllocationLineId))
                        {
                            IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams();
                            if (fundingStreams == null)
                            {
                                context.AddFailure("Unable to query funding streams, result returned null");
                                return;
                            }

                            bool foundFundingStream = false;

                            foreach (FundingStream fundingStream in fundingStreams)
                            {
                                foreach (AllocationLine allocationLine in fundingStream.AllocationLines)
                                {
                                    if (allocationLine.Id == model.AllocationLineId)
                                    {
                                        foundFundingStream = true;
                                        break;
                                    }
                                }

                                if (foundFundingStream)
                                {
                                    break;
                                }
                            }

                            if (!foundFundingStream)
                            {
                                context.AddFailure("Unable to find Allocation Line with provided ID");
                                return;
                            }

                            if (checkForDuplicateBaselineCalcs)
                            {
                                Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);
                                if (specification == null)
                                {
                                    context.AddFailure("Specification not found");
                                    return;
                                }

                                bool existingBaselineSpecification = specification.Current.GetAllCalculations().Any(c => c.CalculationType == CalculationType.Baseline && string.Equals(c.AllocationLine?.Id, model.AllocationLineId, StringComparison.InvariantCultureIgnoreCase));
                                if (existingBaselineSpecification)
                                {
                                    context.AddFailure("This specification already has an existing Baseline calculation associated with it. Please choose a different allocation line ID to create a Baseline calculation for.");
                                    return;
                                }
                            }
                        }
                    }
                });
        }
    }
}
