using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.IntegrationTests.Common.Data;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace CalculateFunding.Api.Publishing.IntegrationTests.Data
{
    public class FundingStreamPaymentDatesDataContext
        : NoPartitionKeyCosmosBulkDataContext
    {
        public FundingStreamPaymentDatesDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "profiling",
            "CalculateFunding.Api.Publishing.IntegrationTests.Resources.FundingStreamPaymentDatesTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                NOW = now,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                PAYMENTDATES = 
                    ((FundingStreamPaymentDate[])documentData.PaymentDates)
                        .AsJson()
                        .Prettify()
            };
    }
}
