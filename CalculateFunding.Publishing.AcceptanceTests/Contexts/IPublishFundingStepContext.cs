using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishFundingStepContext
    {
        bool PublishSuccessful { get; set; }

        Task PublishFunding(string specificationId, string jobId, string userId, string userName);
    }
}
