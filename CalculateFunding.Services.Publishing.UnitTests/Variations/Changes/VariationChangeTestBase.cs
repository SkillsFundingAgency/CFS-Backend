using System;
using System.Collections.Generic;
using System.Net;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class VariationChangeTestBase : ProviderVariationContextTestBase
    {
        protected IApplyProviderVariations VariationsApplication;
        protected ISpecificationsApiClient SpecificationsApiClient;

        [TestInitialize]
        public void VariationChangeTestBaseSetUp()
        {
            SpecificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            VariationsApplication = Substitute.For<IApplyProviderVariations>();

            VariationsApplication.SpecificationsApiClient
                .Returns(SpecificationsApiClient);

            VariationsApplication.ResiliencePolicies
                .Returns(new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync()
                });
        }

        protected void GivenTheVariationPointersForTheSpecification(params ProfileVariationPointer[] variationPointers)
        {
            SpecificationsApiClient
                .GetProfileVariationPointers(VariationContext.RefreshState.SpecificationId)
                .Returns(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, variationPointers));
        }

        protected void AndTheVariationPointersForTheSpecification(params ProfileVariationPointer[] variationPointers)
        {
            GivenTheVariationPointersForTheSpecification(variationPointers);
        }

        protected ProfileVariationPointer NewVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder variationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(variationPointerBuilder);
            
            return variationPointerBuilder.Build();
        }

        protected void AndTheFundingLines(params FundingLine[] fundingLines)
        {
            GivenTheFundingLines(fundingLines);
        }

        protected void AndTheSuccessorFundingLines(params FundingLine[] fundingLines)
        {
            GivenTheSuccessorFundingLines(fundingLines);
        }
        
        protected void GivenTheFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.RefreshState.FundingLines = fundingLines;
        }
    }
}