namespace CalculateFunding.Services.Profiling.Models
{
	using System.Net;

	public class ProfileValidationResult
    {
        private ProfileValidationResult(HttpStatusCode code)
        {
            Code = code;
        }

        public HttpStatusCode Code { get; }

        public static ProfileValidationResult BadRequest => new ProfileValidationResult(HttpStatusCode.BadRequest);

        public static ProfileValidationResult NotFound => new ProfileValidationResult(HttpStatusCode.NotFound);

        public static ProfileValidationResult Ok => new ProfileValidationResult(HttpStatusCode.OK);
    }
}