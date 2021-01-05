using System;
using CalculateFunding.Models.Specifications.ViewModels;
using FluentValidation;

namespace CalculateFunding.Services.Specifications.Validators
{
    public class UpdateFundingStructureLastModifiedRequestValidator 
        : AbstractValidator<UpdateFundingStructureLastModifiedRequest>
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