using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IPublishingJobClashCheck
    {
        Task<bool> PublishingJobsClashWithFundingStreamCoreProviderUpdate(string specificationId);
    }
}