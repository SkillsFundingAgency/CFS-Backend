using FluentValidation;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelModelValidator : AbstractValidator<ChannelRequest>
    {
        public ChannelModelValidator()
        {
            RuleFor(_ => _)
                .NotNull();
            RuleFor(_ => _.ChannelCode)
                .NotEmpty();
            RuleFor(_ => _.ChannelName)
                .NotEmpty();
            RuleFor(_ => _.UrlKey)
                .NotEmpty();
        }
    }
}
