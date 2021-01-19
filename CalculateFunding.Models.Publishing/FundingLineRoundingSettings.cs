using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Models.Publishing
{
    public class FundingLineRoundingSettings : IFundingLineRoundingSettings
    {
        private readonly IConfiguration _configuration;

        public FundingLineRoundingSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int DecimalPlaces => int.TryParse(_configuration["FundingLineRoundingSettings:DecimalPlaces"], out int places) ? places : 2;
    }
}