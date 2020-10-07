using System;
using CalculateFunding.Models.Result.ViewModels;
using FluentValidation;

namespace CalculateFunding.Services.Results.Validators
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