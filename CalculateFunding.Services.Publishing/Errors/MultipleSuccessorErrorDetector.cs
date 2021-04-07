using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class MultipleSuccessorErrorDetector : PublishedProviderErrorDetector
    {
        public MultipleSuccessorErrorDetector() 
            : base(PublishedProviderErrorType.MultipleSuccessors)
        {
        }

        public override bool IsAssignProfilePatternCheck => false;
        
        public override string Name => nameof(MultipleSuccessorErrorDetector);

        public override bool IsPreVariationCheck => true;

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider,
            PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            PublishedProviderVersion providerVersion = publishedProvider.Current;
            
            IEnumerable<string> successors = providerVersion.Provider.GetSuccessors();
            
            bool hasMultipleSuccessors = successors.Count() > 1;

            if (hasMultipleSuccessors)
            {
                errorCheck.AddError(new PublishedProviderError
                {
                    Identifier = providerVersion.ProviderId,
                    Type = PublishedProviderErrorType.MultipleSuccessors,
                    SummaryErrorMessage = "The published provider has multiple successors.",
                    DetailedErrorMessage = $"Published provider {providerVersion.ProviderId} has the following successors {successors.JoinWith(',')}",
                    FundingStreamId = providerVersion.FundingStreamId
                });
            }

            return Task.FromResult(errorCheck);   
        }
    }
}