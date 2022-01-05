using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingDateService
    {
        PublishedFundingDates GetDatesForSpecification();
    }
}
