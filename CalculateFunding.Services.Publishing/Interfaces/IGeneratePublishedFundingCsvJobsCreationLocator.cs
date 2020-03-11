using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IGeneratePublishedFundingCsvJobsCreationLocator
    {
        IGeneratePublishedFundingCsvJobsCreation GetService(GeneratePublishingCsvJobsCreationAction action);
    }
}