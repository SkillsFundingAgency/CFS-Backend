namespace CalculateFunding.Services.Core.Options
{
    public class ApplicationInsightsOptions
    {
        public string InstrumentationKey { get; set; }

        public string Url { get; set; } = "https://dc.services.visualstudio.com/v2/track";
    }
}
