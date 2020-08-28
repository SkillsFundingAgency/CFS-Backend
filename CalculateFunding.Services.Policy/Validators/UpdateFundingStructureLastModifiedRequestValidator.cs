using System;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class UpdateFundingStructureLastModifiedRequestValidator : AbstractValidator<UpdateFundingStructureLastModifiedRequest>
    {
        public UpdateFundingStructureLastModifiedRequestValidator()
        {
            RuleFor(_ => _.LastModified).GreaterThan(DateTimeOffset.MinValue);
            RuleFor(_ => _.SpecificationId).NotEmpty();
            RuleFor(_ => _.FundingPeriodId).NotEmpty();
            RuleFor(_ => _.FundingStreamId).NotEmpty();
        }
    }
}