using CalculateFunding.Models.Results;
using FluentValidation;

namespace CalculateFunding.Services.Results.Validators
{
	public class MasterProviderModelValidator : AbstractValidator<MasterProviderModel>
	{
		private const string ValidationErrorFormat = "Urn: {0} - {1}";
		public MasterProviderModelValidator()
		{
			RuleFor(mp => mp.MasterProviderName)
				.NotEmpty()
				.WithMessage(mp => string.Format(ValidationErrorFormat, mp.MasterURN, "Establisment name column was empty"));

			RuleFor(mp => mp.MasterLocalAuthorityName)
				.NotEmpty()
				.WithMessage(mp => string.Format(ValidationErrorFormat, mp.MasterURN, "Local Authority name column was empty"));

			RuleFor(mp => mp.MasterProviderTypeGroupName)
				.NotEmpty()
				.WithMessage(mp => string.Format(ValidationErrorFormat, mp.MasterURN, "Provider Type Group Name column was empty"));

			RuleFor(mp => mp.MasterProviderTypeName)
				.NotEmpty()
				.WithMessage(mp => string.Format(ValidationErrorFormat, mp.MasterURN, "Provider Type Name column was empty"));
		}
	}
}
