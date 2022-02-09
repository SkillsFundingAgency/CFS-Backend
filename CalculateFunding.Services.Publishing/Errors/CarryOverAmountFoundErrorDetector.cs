using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class CarryOverAmountFoundErrorDetector : PublishedProviderErrorDetector
    {
        public override string Name => nameof(CarryOverAmountFoundErrorDetector);

        public override bool IsAssignProfilePatternCheck => false;
        public override bool IsPreVariationCheck => false;
        public override bool IsPostVariationCheck => true;

        public CarryOverAmountFoundErrorDetector()
            : base(PublishedProviderErrorType.CarryOverAmountFound)
        {
        }

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            if (publishedProvidersContext.FundingConfiguration.EnableCarryForward)
            {
                return Task.FromResult(errorCheck);
            }

            IEnumerable<ProfilingCarryOver> carryOvers = publishedProvider.Current.CarryOvers.Where(c => c?.Amount != 0);

            if (carryOvers.Any())
            {
                foreach (ProfilingCarryOver carryOver in carryOvers)
                {
                    errorCheck.AddError(new PublishedProviderError
                    {
                        Identifier = carryOver.FundingLineCode,
                        Type = PublishedProviderErrorType.FundingLineValueProfileMismatch,
                        SummaryErrorMessage = "A funding line has carry over amount even though Enable Carry Forward option is not enabled",
                        DetailedErrorMessage = "Fundling line profile doesn't Enable Carry Forward setting. " +
                        $"Carry over amount is £{carryOver.Amount}, but Enable Carry Forward option is set to false",
                        FundingLineCode = carryOver.FundingLineCode,
                        FundingStreamId = publishedProvider.Current.FundingStreamId
                    });
                }
            }

            return Task.FromResult(errorCheck);
        }
    }
}
