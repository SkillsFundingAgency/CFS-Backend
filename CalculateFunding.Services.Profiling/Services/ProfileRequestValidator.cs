using FluentValidation;

namespace CalculateFunding.Services.Profiling.Services
{
	using System.Collections.Generic;
	using System.Linq;
	using Models;

	public static class ProfileRequestValidator
    {
	    public static ProfileValidationResult ValidateRequestAgainstPattern(ProfileRequest request,
		    FundingStreamPeriodProfilePattern profilePattern)
	    {
		    if (profilePattern == null)
		    {
			    if (string.IsNullOrEmpty(request.FundingPeriodId))
			    {
				    return ProfileValidationResult.BadRequest;
			    }

			    return ProfileValidationResult.NotFound;
		    }
            return ProfileValidationResult.Ok;
	    }

	    
    }

	public class ProfileBatchRequestValidator : AbstractValidator<ProfileBatchRequest>
	{
			//TODO; rules for request validation
	}
}