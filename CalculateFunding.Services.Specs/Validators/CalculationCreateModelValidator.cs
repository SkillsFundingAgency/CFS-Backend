using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Specs.Validators
{
    public class CalculationCreateModelValidator : AbstractValidator<CalculationCreateModel>
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;

        public CalculationCreateModelValidator(IMapper mapper, ISpecificationsRepository specificationsRepository, ICalculationsRepository calculationsRepository, IPoliciesApiClient policiesApiClient)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            _specificationsRepository = specificationsRepository;
            _calculationsRepository = calculationsRepository;
            _policiesApiClient = policiesApiClient;
            _mapper = mapper;

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
                            ApiResponse<IEnumerable<PolicyModels.FundingStream>> fundingStreamsResponse = await _policiesApiClient.GetFundingStreams();
                            IEnumerable<FundingStream> fundingStreams = _mapper.Map<IEnumerable<FundingStream>>(fundingStreamsResponse?.Content);
                            if (fundingStreams == null)
                            {
                                context.AddFailure("Unable to query funding streams, result returned null");
                                return;
                            }

                            bool foundFundingStream = false;

                            // TODO: Add new API method to GetFundingStream by Allocation Line Id, to avoid 
                            // 1. Retrieving All Funding Streams from Cosmos DB - can save lot of time. But first we need to ensure if there is index for ghis type of query
                            // 2. In-memory O(n*m) operation to match allocation line with funding streams
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

                                bool existingBaselineSpecification = !specification.Current.Calculations.IsNullOrEmpty() &&
                                    specification.Current.Calculations.Any(c => 
                                        c.CalculationType == CalculationType.Baseline && 
                                        string.Equals(c.AllocationLine?.Id, model.AllocationLineId, StringComparison.InvariantCultureIgnoreCase));

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
