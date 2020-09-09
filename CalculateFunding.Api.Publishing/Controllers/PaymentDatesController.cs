using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PaymentDatesController : ControllerBase
    {
        /// <summary>
        /// Save payment dates for funding stream
        /// </summary>
        /// <param name="fundingStreamId"></param>
        /// <param name="fundingPeriodId"></param>
        /// <param name="fundingStreamPaymentDatesIngestion"></param>
        /// <returns></returns>
        [HttpPost("api/fundingstreams/{fundingStreamId}/fundingperiods/{fundingPeriodId}/paymentdates")]
        [ProducesResponseType(200)]
        [SwaggerOperation(Description = "Body of POST is CSV with payment dates")]
        public async Task<IActionResult> SaveFundingStreamPaymentDates(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromServices] IFundingStreamPaymentDatesIngestion fundingStreamPaymentDatesIngestion)
        {
            string paymentDatesCsv = await Request.GetRawBodyStringAsync();

            return await fundingStreamPaymentDatesIngestion.IngestFundingStreamPaymentDates(paymentDatesCsv,
                fundingStreamId,
                fundingPeriodId);
        }

        /// <summary>
        /// Get the actual dates payments are made on for a funding stream
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingPeriodId">Funding PeriodId</param>
        /// <param name="fundingStreamPaymentDatesQuery"></param>
        /// <returns></returns>
        [HttpGet("api/fundingstreams/{fundingStreamId}/fundingperiods/{fundingPeriodId}/paymentdates")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> QueryFundingStreamPaymentDates(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromServices] IFundingStreamPaymentDatesQuery fundingStreamPaymentDatesQuery)
        {
            return await fundingStreamPaymentDatesQuery.GetFundingStreamPaymentDates(fundingStreamId,
                fundingPeriodId);
        }
    }
}