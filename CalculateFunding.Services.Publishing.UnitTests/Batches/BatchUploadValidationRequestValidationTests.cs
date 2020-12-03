using System;
using System.Linq;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class BatchUploadValidationRequestValidationTests 
    {
        [TestMethod]
        public void FailsValidationForMissingBatchId()
        {
            TheRequestHasTheValidationFailures(NewBatchUploadValidationRequest(_ => _.WithFundingPeriodId(NewRandomString())
                .WithFundingStreamId(NewRandomString())), "BatchId");        
        }
        
        [TestMethod]
        public void FailsValidationForMissingFundingStreamId()
        {
            TheRequestHasTheValidationFailures(NewBatchUploadValidationRequest(_ => _.WithBatchId(NewRandomString())
                .WithFundingPeriodId(NewRandomString())), "FundingStreamId");        
        }
        
        [TestMethod]
        public void FailsValidationForMissingFundingPeriodId()
        {
            TheRequestHasTheValidationFailures(NewBatchUploadValidationRequest(_ => _.WithBatchId(NewRandomString())
                .WithFundingStreamId(NewRandomString())), "FundingPeriodId");   
        }

        private void TheRequestHasTheValidationFailures(BatchUploadValidationRequest request,
            params string[] properties)
        {
            ValidationResult result = WhenTheRequestIsValidated(request);

            result.Errors.Select(_ => _.PropertyName)
                .Should()
                .BeEquivalentTo(properties);
        }

        private ValidationResult WhenTheRequestIsValidated(BatchUploadValidationRequest request)
            => new BatchUploadValidationRequestValidation().Validate(request);
        
        private string NewRandomString() => new RandomString();

        private BatchUploadValidationRequest NewBatchUploadValidationRequest(Action<BatchUploadValidationRequestBuilder> setUp = null)
        {
            BatchUploadValidationRequestBuilder batchUploadValidationRequestBuilder = new BatchUploadValidationRequestBuilder();

            setUp?.Invoke(batchUploadValidationRequestBuilder);
            
            return batchUploadValidationRequestBuilder.Build();
        }
    }
}