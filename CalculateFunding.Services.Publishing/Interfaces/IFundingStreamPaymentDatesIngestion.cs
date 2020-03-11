using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingStreamPaymentDatesIngestion
    {
        /// <summary>
        /// Push the csv stuff into pocos then push into cosmos repo (also validate)
        /// </summary>
        Task<IActionResult> IngestFundingStreamPaymentDates(string paymentDatesCsv, 
            string fundingStreamId, 
            string fundingPeriodId);
    }
}