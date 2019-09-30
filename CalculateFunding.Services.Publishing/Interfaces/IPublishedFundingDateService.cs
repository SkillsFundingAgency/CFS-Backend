using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingDateService
    {
        Task<PublishedFundingDates> GetDatesForSpecification(string specificationId);
    }
}
